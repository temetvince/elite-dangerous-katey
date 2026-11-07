namespace Katey.Journal;

/// <summary>
/// Something that keeps state from journal events. The monitor replays every journal file into each sink
/// on start, then feeds new events as the game writes them.
/// </summary>
/// <remarks>
/// <see cref="Apply"/> and <see cref="Reset"/> are called from the monitor's thread, one call at a time.
/// Anything a sink exposes for reading must therefore be safe to read from another thread while
/// <see cref="Apply"/> runs: either an immutable snapshot swapped in whole, or a lock.
/// </remarks>
public interface IJournalSink
{
    /// <summary>
    /// Gets the event names this sink wants, or <see langword="null"/> for every event. The monitor skips
    /// parsing a line whose event no sink wants, which is what keeps replaying a long history fast.
    /// </summary>
    IReadOnlySet<string>? InterestedEvents { get; }

    /// <summary>Forgets everything, before a replay starts.</summary>
    void Reset();

    /// <summary>Applies one event.</summary>
    /// <param name="journalEvent">The event, in chronological order.</param>
    /// <returns><see langword="true"/> when the sink's state changed.</returns>
    bool Apply(JournalEvent journalEvent);
}
