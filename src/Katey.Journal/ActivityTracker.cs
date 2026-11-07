using System.Globalization;
using System.Text.Json;

namespace Katey.Journal;

/// <summary>What kind of thing just happened.</summary>
public enum ActivityKind
{
    /// <summary>Arrived in a system. Subject is the system.</summary>
    Arrived,

    /// <summary>The next jump was targeted. Subject is the system; detail is its star class.</summary>
    Targeted,

    /// <summary>Docked. Subject is the station; detail is the system.</summary>
    Docked,

    /// <summary>Landed on a body. Subject is the body.</summary>
    Landed,

    /// <summary>The discovery scanner was fired. Detail is the body count.</summary>
    Honked,

    /// <summary>Every body in the system has been found.</summary>
    AllBodiesFound,

    /// <summary>A body was scanned. Subject is the body.</summary>
    Scanned,

    /// <summary>The surface scanner reported signals. Subject is the body; detail counts them.</summary>
    SignalsFound,

    /// <summary>A body was fully mapped. Subject is the body.</summary>
    Mapped,

    /// <summary>A life form was sampled or analysed. Subject is the species; detail is the stage.</summary>
    OrganicScanned,

    /// <summary>Exobiology data was sold. Detail is the credits.</summary>
    OrganicSold,

    /// <summary>Cartographic data was sold. Detail is the credits.</summary>
    ExplorationSold,

    /// <summary>A material was picked up. Subject is the material; detail is the count.</summary>
    MaterialCollected,

    /// <summary>A module was engineered. Subject is the blueprint; detail is the grade and engineer.</summary>
    Engineered,

    /// <summary>Outfitting listed the stored modules. Detail is the count.</summary>
    StorageListed,

    /// <summary>The player switched ships. Subject is the new ship type.</summary>
    ShipSwapped,
}

/// <summary>One thing that happened, for a recent-activity feed.</summary>
/// <param name="At">When the journal says it happened.</param>
/// <param name="Kind">What it was.</param>
/// <param name="Subject">The main noun, localised where the journal offers it. Empty when the kind has none.</param>
/// <param name="Detail">A short qualifier, or empty.</param>
public sealed record ActivityItem(DateTimeOffset At, ActivityKind Kind, string Subject, string Detail);

/// <summary>
/// Keeps the most recent things worth telling the player about: arrivals, targets, scans, signals,
/// landings, materials picked up, engineering done, and sales.
/// </summary>
/// <remarks>Holds at most <see cref="Capacity"/> items; older ones fall off. Thread-safe.</remarks>
public sealed class ActivityTracker : IJournalSink
{
    /// <summary>How many items the tracker keeps.</summary>
    public const int Capacity = 60;

    private static readonly HashSet<string> Events = new(StringComparer.Ordinal)
    {
        "FSDJump", "CarrierJump", "FSDTarget", "Docked", "Touchdown", "FSSDiscoveryScan",
        "FSSAllBodiesFound", "Scan", "SAASignalsFound", "SAAScanComplete", "ScanOrganic", "SellOrganicData",
        "SellExplorationData", "MultiSellExplorationData", "MaterialCollected", "EngineerCraft",
        "StoredModules", "ShipyardSwap",
    };

    private readonly Lock _gate = new();
    private readonly LinkedList<ActivityItem> _items = [];

    /// <inheritdoc/>
    public IReadOnlySet<string>? InterestedEvents => Events;

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_gate)
        {
            _items.Clear();
        }
    }

    /// <inheritdoc/>
    public bool Apply(JournalEvent journalEvent)
    {
        ArgumentNullException.ThrowIfNull(journalEvent);
        var item = Describe(journalEvent);
        if (item is null)
        {
            return false;
        }

        lock (_gate)
        {
            _items.AddFirst(item);
            while (_items.Count > Capacity)
            {
                _items.RemoveLast();
            }
        }

        return true;
    }

    /// <summary>Copies the feed.</summary>
    /// <returns>Newest first, at most <see cref="Capacity"/> items.</returns>
    public IReadOnlyList<ActivityItem> Snapshot()
    {
        lock (_gate)
        {
            return [.. _items];
        }
    }

    private static ActivityItem? Describe(JournalEvent journalEvent)
    {
        var payload = journalEvent.Payload;
        var at = journalEvent.Timestamp;
        return journalEvent.Name switch
        {
            "FSDJump" or "CarrierJump" => Item(at, ActivityKind.Arrived, JsonRead.Text(payload, "StarSystem"), ""),
            "FSDTarget" => Item(at, ActivityKind.Targeted, JsonRead.Text(payload, "Name"), JsonRead.Text(payload, "StarClass") ?? ""),
            "Docked" => Item(at, ActivityKind.Docked, JsonRead.Text(payload, "StationName"), JsonRead.Text(payload, "StarSystem") ?? ""),
            "Touchdown" => JsonRead.Flag(payload, "PlayerControlled") || !payload.TryGetProperty("PlayerControlled", out _)
                                ? Item(at, ActivityKind.Landed, JsonRead.Text(payload, "Body"), "")
                                : null,
            "FSSDiscoveryScan" => new ActivityItem(at, ActivityKind.Honked, JsonRead.Text(payload, "SystemName") ?? "", Count(payload, "BodyCount")),
            "FSSAllBodiesFound" => new ActivityItem(at, ActivityKind.AllBodiesFound, JsonRead.Text(payload, "SystemName") ?? "", Count(payload, "Count")),
            "Scan" => string.Equals(JsonRead.Text(payload, "ScanType"), "Detailed", StringComparison.OrdinalIgnoreCase)
                                ? Item(at, ActivityKind.Scanned, JsonRead.Text(payload, "BodyName"), JsonRead.Text(payload, "PlanetClass") ?? JsonRead.Text(payload, "StarType") ?? "")
                                : null,
            "SAASignalsFound" => Item(at, ActivityKind.SignalsFound, JsonRead.Text(payload, "BodyName"), Signals(payload)),
            "SAAScanComplete" => Item(at, ActivityKind.Mapped, JsonRead.Text(payload, "BodyName"), ""),
            "ScanOrganic" => Item(at, ActivityKind.OrganicScanned, JsonRead.Localised(payload, "Species"), JsonRead.Text(payload, "ScanType") ?? ""),
            "SellOrganicData" => new ActivityItem(at, ActivityKind.OrganicSold, "", Credits(JsonRead.Items(payload, "BioData").Sum(bio => JsonRead.Identifier(bio, "Value") + JsonRead.Identifier(bio, "Bonus")))),
            "SellExplorationData" or "MultiSellExplorationData" => new ActivityItem(at, ActivityKind.ExplorationSold, "", Credits(JsonRead.Identifier(payload, "TotalEarnings"))),
            "MaterialCollected" => Item(at, ActivityKind.MaterialCollected, JsonRead.Localised(payload, "Name"), Count(payload, "Count")),
            "EngineerCraft" => Item(
                                at,
                                ActivityKind.Engineered,
                                JsonRead.Text(payload, "BlueprintName"),
                                string.Create(CultureInfo.InvariantCulture, $"G{JsonRead.Count(payload, "Level")} {JsonRead.Text(payload, "Engineer")}").Trim()),
            "StoredModules" => new ActivityItem(at, ActivityKind.StorageListed, JsonRead.Text(payload, "StationName") ?? "", JsonRead.Items(payload, "Items").Count().ToString(CultureInfo.InvariantCulture)),
            "ShipyardSwap" => Item(at, ActivityKind.ShipSwapped, JsonRead.Localised(payload, "ShipType"), ""),
            _ => null,
        };
    }

    private static ActivityItem? Item(DateTimeOffset at, ActivityKind kind, string? subject, string detail)
        => subject is null ? null : new ActivityItem(at, kind, subject, detail);

    private static string Count(JsonElement payload, string property)
        => JsonRead.Count(payload, property).ToString(CultureInfo.InvariantCulture);

    private static string Credits(long credits) => credits.ToString(CultureInfo.InvariantCulture);

    private static string Signals(JsonElement payload)
    {
        var parts = new List<string>();
        foreach (var signal in JsonRead.Items(payload, "Signals"))
        {
            var type = JsonRead.Localised(signal, "Type") ?? "";
            var count = JsonRead.Count(signal, "Count");
            if (type.Length > 0)
            {
                parts.Add(string.Create(CultureInfo.InvariantCulture, $"{count} {type}"));
            }
        }

        return string.Join(", ", parts);
    }
}
