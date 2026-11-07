namespace Katey.Journal;

/// <summary>Where the evidence that an item is unlocked came from.</summary>
public enum UnlockEvidence
{
    /// <summary>A <c>TechnologyBroker</c> event: the purchase itself.</summary>
    Broker,

    /// <summary>A <c>ModuleBuy</c> event: the item was bought at outfitting, which the game only allows once it is unlocked.</summary>
    Outfitting,
}

/// <summary>One piece of evidence that items are unlocked: a technology broker purchase, or a module bought at outfitting.</summary>
/// <param name="At">When the journal says it happened.</param>
/// <param name="Evidence">What kind of event it was.</param>
/// <param name="Broker">For a broker purchase, the broker type as the journal spells it, lower case: <c>guardian</c> or <c>human</c>. Empty for an outfitting purchase.</param>
/// <param name="Items">The item symbols made available or bought, lower case.</param>
public sealed record UnlockRecord(DateTimeOffset At, UnlockEvidence Evidence, string Broker, IReadOnlyList<string> Items);

/// <summary>Every technology broker purchase, and every outfitting purchase, in the history.</summary>
/// <param name="Records">Oldest first.</param>
public sealed record UnlocksSnapshot(IReadOnlyList<UnlockRecord> Records)
{
    /// <summary>Gets nothing bought yet.</summary>
    public static UnlocksSnapshot Empty { get; } = new([]);

    /// <summary>Whether any record names an item whose symbol starts with one of the given prefixes.</summary>
    /// <param name="prefixes">Item symbol prefixes, lower case. Empty means the answer is <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when a recorded item matches.</returns>
    public bool HasItem(IEnumerable<string> prefixes) => Find(prefixes) is not null;

    /// <summary>The first record naming an item whose symbol starts with one of the given prefixes, broker purchases first.</summary>
    /// <param name="prefixes">Item symbol prefixes, lower case. Empty means nothing is found.</param>
    /// <returns>The record, or <see langword="null"/>.</returns>
    public UnlockRecord? Find(IEnumerable<string> prefixes)
    {
        ArgumentNullException.ThrowIfNull(prefixes);
        var list = prefixes.ToList();
        return list.Count == 0
            ? null
            : Records
                .OrderBy(record => record.Evidence)
                .FirstOrDefault(record => record.Items.Any(item => list.Any(prefix => item.StartsWith(prefix, StringComparison.Ordinal))));
    }
}

/// <summary>
/// Keeps every <c>TechnologyBroker</c> event, what was unlocked, where and when, and every
/// <c>ModuleBuy</c>, since buying a module at outfitting proves it was unlocked even when the unlock
/// itself predates the journal history the game kept.
/// </summary>
/// <remarks>Thread-safe: <see cref="Apply"/> and <see cref="Snapshot"/> may run on different threads. Snapshots are immutable.</remarks>
public sealed class UnlockTracker : IJournalSink
{
    private static readonly HashSet<string> Events = new(StringComparer.Ordinal) { "TechnologyBroker", "ModuleBuy", "ModuleBuyAndStore" };

    private readonly Lock _gate = new();
    private readonly List<UnlockRecord> _records = [];

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
        var payload = journalEvent.Payload;
        switch (journalEvent.Name)
        {
            case "TechnologyBroker":
                {
                    var broker = (JsonRead.Text(payload, "BrokerType") ?? "").Trim().ToLowerInvariant();
                    var items = JsonRead.Items(payload, "ItemsUnlocked")
                        .Select(item => JsonRead.Symbol(item, "Name"))
                        .OfType<string>()
                        .ToList();
                    Add(new UnlockRecord(journalEvent.Timestamp, UnlockEvidence.Broker, broker, items));
                    return true;
                }

            case "ModuleBuy":
            case "ModuleBuyAndStore":
                {
                    if (JsonRead.Symbol(payload, "BuyItem") is not { } bought)
                    {
                        return false;
                    }

                    Add(new UnlockRecord(journalEvent.Timestamp, UnlockEvidence.Outfitting, "", [bought]));
                    return true;
                }

            default:
                return false;
        }
    }

    /// <summary>The purchases so far.</summary>
    /// <returns>An immutable snapshot.</returns>
    public UnlocksSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new UnlocksSnapshot([.. _records]);
        }
    }

    private void Add(UnlockRecord record)
    {
        lock (_gate)
        {
            _records.Add(record);
        }
    }
}
