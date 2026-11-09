using System.Globalization;
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
    public string Status { get; private set; } = "";

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
        Status = $"Backing up to {Folder}…";
        Changed?.Invoke(this, EventArgs.Empty);
        var destination = Folder;
        _ = Task.Run(() =>
        {
            var status = Run(sourceFolder, destination);
            _dispatch(() =>
            {
                // The status lands before the busy flag drops, so anything waiting on the flag reads the finished status.
                Status = status;
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
        var line = $"Backed up at {when}: {Display.Plural(result.Copied, "file")} copied, {result.Skipped} already current.";
        return result.IsComplete ? line : $"{line} {Display.Plural(result.Failures.Count, "file")} failed: {result.Failures[0]}";
    }

    private string Run(string sourceFolder, string destination)
    {
        try
        {
            return sourceFolder.Length == 0
                ? "No journal folder to back up. Set one above first."
                : Describe(_backup.Copy(sourceFolder, destination), DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Log.BackupFailed(_logger, exception);
            return $"Backup failed: {exception.Message}";
        }
    }
}
