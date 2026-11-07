namespace Katey.Journal;

/// <summary>One engineering roll the game reported: an <c>EngineerCraft</c> event.</summary>
/// <param name="At">When the journal says it happened.</param>
/// <param name="Module">The module's item symbol in canonical form.</param>
/// <param name="Slot">The slot the module sits in on the ship being flown.</param>
/// <param name="Blueprint">The blueprint's journal name, such as <c>PowerPlant_Boosted</c>.</param>
/// <param name="Level">The grade the roll was at, 1 to 5.</param>
/// <param name="Quality">The grade's progress after the roll, 0 to 1. 1 means the grade is complete.</param>
/// <param name="Experimental">The experimental effect on the module after the roll, or <see langword="null"/> for none.</param>
public sealed record CraftRecord(DateTimeOffset At, string Module, string Slot, string Blueprint, int Level, double Quality, string? Experimental);

/// <summary>Every engineering roll in the history.</summary>
/// <param name="Records">Oldest first.</param>
public sealed record CraftsSnapshot(IReadOnlyList<CraftRecord> Records)
{
    /// <summary>Gets no rolls yet.</summary>
    public static CraftsSnapshot Empty { get; } = new([]);

    /// <summary>The rolls strictly after an instant, oldest first.</summary>
    /// <param name="after">The instant. Rolls at exactly this time are left out.</param>
    /// <returns>The rolls, or none.</returns>
    public IEnumerable<CraftRecord> After(DateTimeOffset after) => Records.Where(record => record.At > after);
}

/// <summary>
/// Keeps every <c>EngineerCraft</c> event: which module was rolled, at which blueprint and grade, how far
/// the grade got, and which experimental effect the module then carried. The game writes no
/// <c>Loadout</c> after engineering, so these events are the only record of engineering as it happens.
/// </summary>
/// <remarks>Thread-safe: <see cref="Apply"/> and <see cref="Snapshot"/> may run on different threads. Snapshots are immutable.</remarks>
public sealed class CraftTracker : IJournalSink
{
    private static readonly HashSet<string> Events = new(StringComparer.Ordinal) { "EngineerCraft" };

    private readonly Lock _gate = new();
    private readonly List<CraftRecord> _records = [];

    /// <inheritdoc/>
    public IReadOnlySet<string>? InterestedEvents => Events;

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_gate)
        {
            _records.Clear();
        }
    }

    /// <inheritdoc/>
    public bool Apply(JournalEvent journalEvent)
    {
        ArgumentNullException.ThrowIfNull(journalEvent);
        if (journalEvent.Name != "EngineerCraft")
        {
            return false;
        }

        var payload = journalEvent.Payload;
        var module = JsonRead.Symbol(payload, "Module");
        var blueprint = JsonRead.Text(payload, "BlueprintName");
        if (module is null || blueprint is null)
        {
            return false;
        }

        var record = new CraftRecord(
            journalEvent.Timestamp,
            module,
            JsonRead.Text(payload, "Slot") ?? "",
            blueprint,
            JsonRead.Count(payload, "Level"),
            Math.Clamp(JsonRead.Measure(payload, "Quality") ?? 0, 0, 1),
            JsonRead.Text(payload, "ExperimentalEffect") ?? JsonRead.Text(payload, "ApplyExperimentalEffect"));
        lock (_gate)
        {
            _records.Add(record);
        }

        return true;
    }

    /// <summary>Copies the current state.</summary>
    /// <returns>A snapshot no later <see cref="Apply"/> changes.</returns>
    public CraftsSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new CraftsSnapshot([.. _records]);
        }
    }
}
