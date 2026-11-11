using System.Globalization;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Journal;
using Microsoft.Extensions.Logging;

namespace Katey.App.Backup;

/// <summary>
/// The journal backup: the folder it goes to, whether it runs when the app closes, and the run in
/// progress. Every change is raised on the UI thread through the dispatcher given.
/// </summary>
/// <remarks>One backup runs at a time; a request while one is running is ignored.</remarks>
public sealed class BackupState
{
    private readonly IJournalBackup _backup;
    private readonly Action<Action> _dispatch;
    private readonly ILogger _logger;
    private Report _report = new(ReportKind.None);

    /// <summary>Creates the state.</summary>
    /// <param name="backup">What copies the folder.</param>
    /// <param name="dispatch">Runs an action on the UI thread.</param>
    /// <param name="logger">Where failures go.</param>
    /// <param name="folder">The backup folder, or empty for none.</param>
    /// <param name="onClose">Whether a backup runs when the app closes.</param>
    public BackupState(IJournalBackup backup, Action<Action> dispatch, ILogger logger, string folder, bool onClose)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(folder);
        _backup = backup;
        _dispatch = dispatch;
        _logger = logger;
        Folder = folder.Trim();
        OnClose = onClose;
    }

    /// <summary>Raised on the UI thread after the folder, the switch, the status or the busy flag changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets or sets the backup folder. Empty means no backup. Saved.</summary>
    public string Folder
    {
        get;
        set
        {
            var next = (value ?? "").Trim();
            if (!string.Equals(field, next, StringComparison.Ordinal))
            {
                field = next;
                Log.BackupFolderSet(_logger, next.Length == 0 ? "cleared" : "set");
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    } = "";

    /// <summary>Gets or sets whether a backup runs when the app closes. Needs a folder to do anything. Saved.</summary>
    public bool OnClose
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Gets whether a folder is set.</summary>
    public bool IsConfigured => Folder.Length > 0;

    /// <summary>Gets one line on the last run, or what is happening now. Empty before any run.</summary>
    public string Status => _report.Kind switch
    {
        ReportKind.Running => Strings.Format(Keys.Backup_Running, ("folder", _report.Folder)),
        ReportKind.Done => Describe(_report.Result!, _report.At),
        ReportKind.NoFolder => Strings.Get(Keys.Backup_NoFolder),
        ReportKind.Error => Strings.Format(Keys.Backup_Error, ("message", _report.Error)),
        ReportKind.None => "",
        _ => "",
    };

    /// <summary>Gets whether a backup is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Starts a backup in the background. Does nothing without a folder or while one is running.</summary>
    /// <param name="sourceFolder">The journal folder to copy.</param>
    public void BackUpNow(string sourceFolder)
    {
        ArgumentNullException.ThrowIfNull(sourceFolder);
        if (!IsConfigured || IsBusy)
        {
            return;
        }

        IsBusy = true;
        _report = new(ReportKind.Running, Folder: Folder);
        Changed?.Invoke(this, EventArgs.Empty);
        var destination = Folder;
        _ = Task.Run(() =>
        {
            var report = Run(sourceFolder, destination);
            _dispatch(() =>
            {
                // The status lands before the busy flag drops, so anything waiting on the flag reads the finished status.
                _report = report;
                IsBusy = false;
                Changed?.Invoke(this, EventArgs.Empty);
            });
        });
    }

    /// <summary>Runs the backup on the calling thread, for the moment the app closes. Does nothing unless <see cref="OnClose"/> is on and a folder is set.</summary>
    /// <param name="sourceFolder">The journal folder to copy.</param>
    /// <returns>What was done, or <see langword="null"/> when nothing ran.</returns>
    public BackupResult? BackUpOnClose(string sourceFolder)
    {
        ArgumentNullException.ThrowIfNull(sourceFolder);
        if (!OnClose || !IsConfigured || sourceFolder.Length == 0)
        {
            return null;
        }

        try
        {
            return _backup.Copy(sourceFolder, Folder);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Log.BackupFailed(_logger, exception);
            return null;
        }
    }

    /// <summary>One line describing a run.</summary>
    /// <param name="result">The run.</param>
    /// <param name="at">When it finished.</param>
    /// <returns>Counts and the time, with the first failure when there is one.</returns>
    public static string Describe(BackupResult result, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(result);
        var when = at.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);
        var line = Strings.Format(Keys.Backup_Done, ("when", when), ("files", Display.Plural(Keys.Plural_Files, result.Copied)), ("skipped", result.Skipped));
        return result.IsComplete ? line : line + " " + Strings.Format(Keys.Backup_Failed, ("files", Display.Plural(Keys.Plural_Files, result.Failures.Count)), ("first", result.Failures[0]));
    }

    private Report Run(string sourceFolder, string destination)
    {
        try
        {
            return sourceFolder.Length == 0
                ? new Report(ReportKind.NoFolder)
                : new Report(ReportKind.Done, _backup.Copy(sourceFolder, destination), DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Log.BackupFailed(_logger, exception);
            return new Report(ReportKind.Error, Error: exception.Message);
        }
    }

    private enum ReportKind
    {
        None,
        Running,
        Done,
        NoFolder,
        Error,
    }

    /// <summary>What the last backup did, kept as facts so <see cref="Status"/> can be worded in whatever language is in force when it is read.</summary>
    private sealed record Report(ReportKind Kind, BackupResult? Result = null, DateTimeOffset At = default, string? Error = null, string Folder = "");
}
