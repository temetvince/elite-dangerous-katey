namespace Katey.Journal;

/// <summary>What a monitor is doing.</summary>
public enum MonitorState
{
    /// <summary>Not reading anything.</summary>
    Stopped,

    /// <summary>The folder does not exist; nothing is read until <see cref="IJournalMonitor.Start"/> is called with one that does.</summary>
    FolderMissing,

    /// <summary>Replaying the journal history into the sinks.</summary>
    CatchingUp,

    /// <summary>Following the newest journal and the live files.</summary>
    Watching,
}

/// <summary>How far a replay has got.</summary>
/// <param name="FilesDone">Journal files fully replayed.</param>
/// <param name="FileCount">Journal files in the folder.</param>
public sealed record CatchUpProgress(int FilesDone, int FileCount)
{
    /// <summary>Gets nothing started.</summary>
    public static CatchUpProgress None { get; } = new(0, 0);

    /// <summary>Gets whether every file has been replayed.</summary>
    public bool IsComplete => FilesDone >= FileCount;
}

/// <summary>
/// Follows a journal folder: replays its history into a set of <see cref="IJournalSink"/>s, then feeds
/// them new events as the game writes them, and keeps the live <c>NavRoute.json</c> and
/// <c>Status.json</c> files current.
/// </summary>
/// <remarks>
/// Events are raised from a background thread; a UI must marshal them itself. Disposing stops the
/// monitor.
/// </remarks>
public interface IJournalMonitor : IDisposable
{
    /// <summary>Raised after any sink or live file changed, at most once per poll.</summary>
    event EventHandler? Updated;

    /// <summary>Raised when <see cref="State"/> or <see cref="Progress"/> changes.</summary>
    event EventHandler? StateChanged;

    /// <summary>Gets the folder being watched, or <see langword="null"/> before <see cref="Start"/>.</summary>
    string? Folder { get; }

    /// <summary>Gets what the monitor is doing.</summary>
    MonitorState State { get; }

    /// <summary>Gets how far the replay has got. Complete once <see cref="State"/> is <see cref="MonitorState.Watching"/>.</summary>
    CatchUpProgress Progress { get; }

    /// <summary>Gets the plotted route from <c>NavRoute.json</c>, or <see langword="null"/> when the file is absent or empty.</summary>
    NavRoute? Route { get; }

    /// <summary>Gets the ship status from <c>Status.json</c>, or <see langword="null"/> when the file is absent.</summary>
    ShipStatus? Status { get; }

    /// <summary>
    /// Starts following a folder, stopping any previous one. Resets every sink, replays every journal file
    /// oldest first, then follows the newest journal and the live files.
    /// </summary>
    /// <param name="folder">The journal folder. May not exist yet; the state then says so.</param>
    void Start(string folder);

    /// <summary>Stops following and waits for the loop to end, so no event of this watch reaches a sink after the call returns. The sinks keep their last state.</summary>
    /// <remarks>Not from an <see cref="Updated"/> or <see cref="StateChanged"/> handler: they are raised on the loop's thread, which the call would wait for.</remarks>
    void Stop();
}
