using System.Text;
using Microsoft.Extensions.Logging;

namespace Katey.Journal;

/// <summary>
/// Polls a journal folder, replays its history into a set of <see cref="IJournalSink"/>s, then follows
/// the newest journal and the live <c>NavRoute.json</c> and <c>Status.json</c> files.
/// </summary>
/// <remarks>
/// On <see cref="Start"/> the monitor resets every sink and replays every journal file, oldest first,
/// reporting progress as it goes. A line is parsed only when some sink wants its event, which is what
/// keeps a multi-year history to a few seconds. It then polls the newest file for new bytes on a fixed
/// interval, one second by default, and re-reads a live file whenever its write time changes. Polling,
/// rather than file-system notifications, is what the game's write pattern needs: it appends without
/// signalling, and a notification would still have to be followed by a read. Partial trailing lines are
/// left until the next poll completes them.
/// </remarks>
public sealed class JournalMonitor : IJournalMonitor
{
    private const string EventMarker = "\"event\":\"";

    private readonly ILogger<JournalMonitor> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly IReadOnlyList<IJournalSink> _sinks;
    private readonly HashSet<string>? _wantedEvents;
    private readonly Lock _gate = new();

    private CancellationTokenSource? _cancellation;
    private Task? _loop;
    private string? _currentFile;
    private int _linesRead;
    private int _linesSkipped;
    private int _routeUnreadable;
    private int _statusUnreadable;
    private long _offset;
    private DateTime _routeWritten;
    private DateTime _statusWritten;

    /// <summary>Creates a monitor.</summary>
    /// <param name="logger">Where lifecycle and parse trouble go.</param>
    /// <param name="sinks">What receives the events. Each is reset on <see cref="Start"/>.</param>
    /// <param name="pollInterval">How often to look for new bytes, or <see langword="null"/> for one second. Must be positive.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> or <paramref name="sinks"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pollInterval"/> is not positive.</exception>
    public JournalMonitor(ILogger<JournalMonitor> logger, IReadOnlyList<IJournalSink> sinks, TimeSpan? pollInterval = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(sinks);
        var interval = pollInterval ?? TimeSpan.FromSeconds(1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        _logger = logger;
        _sinks = sinks;
        _pollInterval = interval;
        _wantedEvents = sinks.Any(sink => sink.InterestedEvents is null)
            ? null
            : sinks.SelectMany(sink => sink.InterestedEvents!).ToHashSet(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public event EventHandler? Updated;

    /// <inheritdoc/>
    public event EventHandler? StateChanged;

    /// <inheritdoc/>
    public string? Folder { get; private set; }

    /// <inheritdoc/>
    public MonitorState State
    {
        get
        {
            lock (_gate)
            {
                return field;
            }
        }

        private set
        {
            lock (_gate)
            {
                field = value;
            }
        }
    }

    /// <inheritdoc/>
    public CatchUpProgress Progress
    {
        get
        {
            lock (_gate)
            {
                return field;
            }
        }

        private set
        {
            lock (_gate)
            {
                field = value;
            }
        }
    } = CatchUpProgress.None;

    /// <inheritdoc/>
    public NavRoute? Route
    {
        get
        {
            lock (_gate)
            {
                return field;
            }
        }

        private set
        {
            lock (_gate)
            {
                field = value;
            }
        }
    }

    /// <inheritdoc/>
    public ShipStatus? Status
    {
        get
        {
            lock (_gate)
            {
                return field;
            }
        }

        private set
        {
            lock (_gate)
            {
                field = value;
            }
        }
    }

    /// <inheritdoc/>
    public void Start(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        Stop();
        Folder = folder;
        if (!Directory.Exists(folder))
        {
            Log.FolderMissing(_logger);
            SetState(MonitorState.FolderMissing, CatchUpProgress.None);
            return;
        }

        Log.Started(_logger);
        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        SetState(MonitorState.CatchingUp, CatchUpProgress.None);
        _loop = Task.Run(() => RunAsync(folder, cancellation.Token), CancellationToken.None);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        var cancellation = _cancellation;
        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        try
        {
            // The reader checks for cancellation before every line, so the loop is out within one line's work;
            // the wait is generous only so that a stall shows up in the log instead of a corrupted replay.
            if (_loop?.Wait(TimeSpan.FromSeconds(30)) == false)
            {
                Log.StopTimedOut(_logger);
            }
        }
        catch (AggregateException exception) when (exception.InnerExceptions.All(inner => inner is OperationCanceledException))
        {
            // Cancellation is the expected way out of the loop.
        }

        cancellation.Dispose();
        _cancellation = null;
        _loop = null;
        _currentFile = null;
        _offset = 0;
        _routeWritten = default;
        _statusWritten = default;
        Log.Stopped(_logger);
        SetState(MonitorState.Stopped, Progress);
    }

    /// <inheritdoc/>
    public void Dispose() => Stop();

    private static FileStream OpenShared(string path)
        => new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

    private static int IndexOf(IReadOnlyList<string> files, string path)
    {
        for (var index = 0; index < files.Count; index++)
        {
            if (string.Equals(files[index], path, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>Reads the event name out of a line without parsing it as JSON.</summary>
    private static string? PeekEventName(string line)
    {
        var start = line.IndexOf(EventMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += EventMarker.Length;
        var end = line.IndexOf('"', start);
        return end < 0 ? null : line[start..end];
    }

    private async Task RunAsync(string folder, CancellationToken cancellationToken)
    {
        try
        {
            CatchUp(folder, cancellationToken);
            SetState(MonitorState.Watching, Progress);
            using var timer = new PeriodicTimer(_pollInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                Poll(folder, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped.
        }
        catch (IOException exception)
        {
            Log.LoopFailed(_logger, exception);
            SetState(MonitorState.Stopped, Progress);
        }
        catch (UnauthorizedAccessException exception)
        {
            Log.LoopFailed(_logger, exception);
            SetState(MonitorState.Stopped, Progress);
        }
    }

    private void CatchUp(string folder, CancellationToken cancellationToken)
    {
        foreach (var sink in _sinks)
        {
            sink.Reset();
        }

        var files = JournalFolder.JournalFiles(folder);
        if (files.Count == 0)
        {
            Log.NoJournals(_logger);
        }

        var started = System.Diagnostics.Stopwatch.StartNew();
        _linesRead = 0;
        _linesSkipped = 0;
        var changed = false;
        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _currentFile = files[index];
            _offset = 0;
            changed |= ReadNewLines(cancellationToken);
            SetState(MonitorState.CatchingUp, new CatchUpProgress(index + 1, files.Count));
        }

        changed |= ReadLiveFiles(folder);
        var seconds = Math.Round(started.Elapsed.TotalSeconds, 1);
        Log.CaughtUp(_logger, files.Count, _linesRead, _linesSkipped, seconds);
        if (changed)
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Poll(string folder, CancellationToken cancellationToken)
    {
        var files = JournalFolder.JournalFiles(folder);
        var newest = files.Count > 0 ? files[^1] : null;
        var changed = false;

        if (_currentFile is not null && newest is not null && !string.Equals(newest, _currentFile, StringComparison.OrdinalIgnoreCase))
        {
            changed |= ReadNewLines(cancellationToken);
            var currentIndex = IndexOf(files, _currentFile);
            foreach (var file in files.Skip(currentIndex + 1))
            {
                var switchedTo = Path.GetFileName(file);
                Log.SwitchedJournal(_logger, switchedTo);
                _currentFile = file;
                _offset = 0;
                changed |= ReadNewLines(cancellationToken);
            }
        }
        else if (_currentFile is null && newest is not null)
        {
            var newestName = Path.GetFileName(newest);
            Log.SwitchedJournal(_logger, newestName);
            _currentFile = newest;
            _offset = 0;
            changed |= ReadNewLines(cancellationToken);
        }
        else if (_currentFile is not null)
        {
            changed |= ReadNewLines(cancellationToken);
        }

        changed |= ReadLiveFiles(folder);
        if (changed)
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool ReadLiveFiles(string folder)
    {
        var changed = false;
        var routePath = Path.Combine(folder, "NavRoute.json");
        if (ReadIfChanged(routePath, ref _routeWritten) is { } routeText)
        {
            Route = NavRoute.Parse(routeText);
            if (Route is null && !string.IsNullOrWhiteSpace(routeText))
            {
                NoteUnreadableLiveFile(routePath, ref _routeUnreadable);
            }

            changed = true;
        }

        var statusPath = Path.Combine(folder, "Status.json");
        if (ReadIfChanged(statusPath, ref _statusWritten) is { } statusText)
        {
            var status = ShipStatus.Parse(statusText);
            if (status is not null)
            {
                Status = status;
                changed = true;
            }
            else if (!string.IsNullOrWhiteSpace(statusText))
            {
                NoteUnreadableLiveFile(statusPath, ref _statusUnreadable);
            }
        }

        return changed;
    }

    /// <summary>Logs one unreadable journal line at Debug, computing the file name only when Debug is on, since this sits in the replay loop.</summary>
    private void NoteUnreadableLine()
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var file = Path.GetFileName(_currentFile) ?? "";
            Log.UnreadableLine(_logger, file);
        }
    }

    /// <summary>Logs a live file the game wrote unreadably: the first time, then every hundredth, since the game rewrites these files many times a minute and a half-written one is read now and then.</summary>
    private void NoteUnreadableLiveFile(string path, ref int count)
    {
        count++;
        if (count == 1 || count % 100 == 0)
        {
            Log.LiveFileUnreadable(_logger, Path.GetFileName(path), count);
        }
    }

    private static string? ReadIfChanged(string path, ref DateTime lastWritten)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var written = File.GetLastWriteTimeUtc(path);
        if (written == lastWritten)
        {
            return null;
        }

        lastWritten = written;
        using var stream = OpenShared(path);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd();
        return text.Length == 0 ? null : text;
    }

    /// <summary>Reads what the current file holds past the offset and applies it, checking for cancellation before every line so a stop never leaves an old replay feeding the sinks.</summary>
    private bool ReadNewLines(CancellationToken cancellationToken)
    {
        if (_currentFile is null || !File.Exists(_currentFile))
        {
            return false;
        }

        using var stream = OpenShared(_currentFile);
        if (stream.Length < _offset)
        {
            // The game relaunched from a launcher left open reuses the file name and writes it again from the
            // start. What it held is gone; what it writes now is new and must not be waited for at the old offset.
            Log.JournalRewritten(_logger, Path.GetFileName(_currentFile), _offset, stream.Length);
            _offset = 0;
        }

        if (stream.Length <= _offset)
        {
            return false;
        }

        stream.Seek(_offset, SeekOrigin.Begin);
        var buffer = new byte[stream.Length - _offset];
        var read = stream.Read(buffer, 0, buffer.Length);
        var lastNewline = Array.LastIndexOf(buffer, (byte)'\n', read - 1);
        if (lastNewline < 0)
        {
            return false;
        }

        var text = Encoding.UTF8.GetString(buffer, 0, lastNewline + 1);
        _offset += lastNewline + 1;

        var changed = false;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            _linesRead++;
            var name = PeekEventName(line);
            if (name is null)
            {
                _linesSkipped++;
                NoteUnreadableLine();
                continue;
            }

            if (_wantedEvents is not null && !_wantedEvents.Contains(name))
            {
                continue;
            }

            if (JournalEvent.Parse(line) is not { } journalEvent)
            {
                _linesSkipped++;
                NoteUnreadableLine();
                continue;
            }

            foreach (var sink in _sinks)
            {
                if (sink.InterestedEvents is null || sink.InterestedEvents.Contains(journalEvent.Name))
                {
                    changed |= sink.Apply(journalEvent);
                }
            }
        }

        return changed;
    }

    private void SetState(MonitorState state, CatchUpProgress progress)
    {
        if (State == state && Progress == progress)
        {
            return;
        }

        State = state;
        Progress = progress;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
