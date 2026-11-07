using System.Collections.Frozen;
using System.Text.Json;

namespace Katey.Journal;

/// <summary>A point-in-time copy of the inventory.</summary>
/// <param name="Counts">Material symbol → count, compared case-insensitively. Only materials the player holds or has held since the snapshot.</param>
/// <param name="HasSnapshot">Whether a <c>Materials</c> event has been seen. Until then <paramref name="Counts"/> is empty and means nothing.</param>
/// <param name="AsOf">Timestamp of the last event applied, or <see langword="null"/> before any.</param>
/// <param name="Commander">The commander name from the last <c>Commander</c> or <c>LoadGame</c> event, or <see langword="null"/>.</param>
public sealed record MaterialInventorySnapshot(
    IReadOnlyDictionary<string, int> Counts,
    bool HasSnapshot,
    DateTimeOffset? AsOf,
    string? Commander)
{
    /// <summary>Gets an inventory nothing has been read into.</summary>
    public static MaterialInventorySnapshot Empty { get; } =
        new(FrozenDictionary<string, int>.Empty, HasSnapshot: false, null, null);
}

/// <summary>
/// Replays journal events into the player's material counts.
/// </summary>
/// <remarks>
/// A <c>Materials</c> event, written every time the game loads a commander, is the server's full
/// inventory and replaces everything. Every later event that adds or removes materials adjusts the
/// counts: collecting, discarding, trading, engineering, synthesis, technology broker unlocks,
/// mission rewards, scientific research and engineer contributions. Adjustments before the first
/// <c>Materials</c> event are ignored, because there is nothing to adjust. Not thread-safe.
/// </remarks>
public sealed class MaterialInventory : IJournalSink
{
    private static readonly HashSet<string> Events = new(StringComparer.Ordinal)
    {
        "Materials", "Commander", "LoadGame", "MaterialCollected", "MaterialDiscarded", "ScientificResearch",
        "MaterialTrade", "EngineerCraft", "Synthesis", "TechnologyBroker", "MissionCompleted",
        "EngineerContribution",
    };

    private readonly Dictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();

    /// <summary>Gets whether a <c>Materials</c> event has been applied.</summary>
    public bool HasSnapshot { get; private set; }

    /// <summary>Gets the timestamp of the last event applied, or <see langword="null"/> before any.</summary>
    public DateTimeOffset? AsOf { get; private set; }

    /// <summary>Gets the commander name, or <see langword="null"/> before a <c>Commander</c> or <c>LoadGame</c> event.</summary>
    public string? Commander { get; private set; }

    /// <summary>Gets the live counts by symbol, case-insensitively. Read only from the applying thread; others use <see cref="Snapshot"/>.</summary>
    public IReadOnlyDictionary<string, int> Counts => _counts;

    /// <inheritdoc/>
    public IReadOnlySet<string>? InterestedEvents => Events;

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_gate)
        {
            _counts.Clear();
            HasSnapshot = false;
            AsOf = null;
            Commander = null;
        }
    }

    /// <summary>Applies one event.</summary>
    /// <param name="journalEvent">The event.</param>
    /// <returns><see langword="true"/> when the event changed the counts, the commander, or took the first snapshot.</returns>
    public bool Apply(JournalEvent journalEvent)
    {
        ArgumentNullException.ThrowIfNull(journalEvent);

        lock (_gate)
        {
            return ApplyLocked(journalEvent);
        }
    }

    /// <summary>Copies the current state.</summary>
    /// <returns>A snapshot that no later <see cref="Apply"/> changes.</returns>
    public MaterialInventorySnapshot Snapshot()
    {
        lock (_gate)
        {
            return new(_counts.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase), HasSnapshot, AsOf, Commander);
        }
    }

    private bool ApplyLocked(JournalEvent journalEvent)
    {
        var payload = journalEvent.Payload;
        var changed = journalEvent.Name switch
        {
            "Materials" => ApplySnapshot(payload),
            "Commander" => SetCommander(StringOrNull(payload, "Name")),
            "LoadGame" => SetCommander(StringOrNull(payload, "Commander")),
            "MaterialCollected" => Adjust(StringOrNull(payload, "Name"), IntOrZero(payload, "Count")),
            "MaterialDiscarded" => Adjust(StringOrNull(payload, "Name"), -IntOrZero(payload, "Count")),
            "ScientificResearch" => Adjust(StringOrNull(payload, "Name"), -IntOrZero(payload, "Count")),
            "MaterialTrade" => ApplyTrade(payload),
            "EngineerCraft" => AdjustAll(payload, "Ingredients", "Name", "Count", -1),
            "Synthesis" => AdjustAll(payload, "Materials", "Name", "Count", -1),
            "TechnologyBroker" => AdjustAll(payload, "Materials", "Name", "Count", -1),
            "MissionCompleted" => AdjustAll(payload, "MaterialsReward", "Name", "Count", +1),
            "EngineerContribution" => ApplyContribution(payload),
            _ => false,
        };

        if (changed)
        {
            AsOf = journalEvent.Timestamp;
        }

        return changed;
    }

    private static string? StringOrNull(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int IntOrZero(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : 0;

    private bool ApplySnapshot(JsonElement payload)
    {
        _counts.Clear();
        foreach (var category in new[] { "Raw", "Manufactured", "Encoded" })
        {
            if (!payload.TryGetProperty(category, out var list) || list.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in list.EnumerateArray())
            {
                var name = StringOrNull(item, "Name");
                var count = IntOrZero(item, "Count");
                if (name is not null && count > 0)
                {
                    _counts[name] = count;
                }
            }
        }

        HasSnapshot = true;
        return true;
    }

    private bool SetCommander(string? name)
    {
        if (name is null || string.Equals(name, Commander, StringComparison.Ordinal))
        {
            return false;
        }

        Commander = name;
        return true;
    }

    private bool Adjust(string? symbol, int delta)
    {
        if (!HasSnapshot || symbol is null || delta == 0)
        {
            return false;
        }

        var next = Math.Max(0, _counts.GetValueOrDefault(symbol, 0) + delta);
        if (next == 0)
        {
            return _counts.Remove(symbol);
        }

        _counts[symbol] = next;
        return true;
    }

    private bool AdjustAll(JsonElement payload, string listProperty, string nameProperty, string countProperty, int sign)
    {
        if (!payload.TryGetProperty(listProperty, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var changed = false;
        foreach (var item in list.EnumerateArray())
        {
            changed |= Adjust(StringOrNull(item, nameProperty), sign * IntOrZero(item, countProperty));
        }

        return changed;
    }

    private bool ApplyTrade(JsonElement payload)
    {
        var changed = false;
        if (payload.TryGetProperty("Paid", out var paid))
        {
            changed |= Adjust(StringOrNull(paid, "Material"), -IntOrZero(paid, "Quantity"));
        }

        if (payload.TryGetProperty("Received", out var received))
        {
            changed |= Adjust(StringOrNull(received, "Material"), IntOrZero(received, "Quantity"));
        }

        return changed;
    }

    private bool ApplyContribution(JsonElement payload)
        => string.Equals(StringOrNull(payload, "Type"), "Materials", StringComparison.OrdinalIgnoreCase)
            && Adjust(StringOrNull(payload, "Material"), -IntOrZero(payload, "Quantity"));
}
