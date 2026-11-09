using System.Text.Json;

namespace Katey.Journal;

/// <summary>One body whose scan is aboard and unsold.</summary>
/// <param name="BodyId">The journal's body id, unique within the system.</param>
/// <param name="Name">Full body name.</param>
/// <param name="IsStar">Whether it is a star.</param>
/// <param name="Class">Star type or planet class as the journal spells it.</param>
/// <param name="Mass">Earth masses for a planet, solar masses for a star, or <see langword="null"/>.</param>
/// <param name="Terraformable">Whether the body can be terraformed.</param>
/// <param name="WasDiscovered">Whether someone had discovered it before the scan.</param>
/// <param name="WasMapped">Whether someone had mapped it before the scan.</param>
/// <param name="Mapped">Whether the player has mapped it since scanning it.</param>
public sealed record CarriedBody(int BodyId, string Name, bool IsStar, string? Class, double? Mass, bool Terraformable, bool WasDiscovered, bool WasMapped, bool Mapped);

/// <summary>One system with scans aboard and unsold.</summary>
/// <param name="Name">System name.</param>
/// <param name="Address">System id, or 0 when the scan did not say.</param>
/// <param name="Bodies">The bodies scanned there, in scan order.</param>
public sealed record CarriedSystem(string Name, long Address, IReadOnlyList<CarriedBody> Bodies);

/// <summary>One completed organic sample aboard and unsold.</summary>
/// <param name="Genus">Genus symbol.</param>
/// <param name="GenusName">Localised genus name.</param>
/// <param name="Species">Species symbol.</param>
/// <param name="SpeciesName">Localised species name.</param>
/// <param name="SystemName">The system it was sampled in, or empty when unknown.</param>
/// <param name="BodyName">The body it was sampled on, or empty when the journal never named the body.</param>
/// <param name="WasLogged">
/// Whether someone had logged the species on that body when it was sampled. <see langword="false"/> means
/// the first-logged bonus is likely at the sale; another commander can still sell it first.
/// </param>
public sealed record CarriedSample(string Genus, string GenusName, string Species, string SpeciesName, string SystemName, string BodyName, bool WasLogged);

/// <summary>Everything scanned and sampled since it was last sold, which a death loses.</summary>
/// <param name="Systems">Systems with unsold scans, in the order they were first scanned.</param>
/// <param name="Samples">Completed organic samples not yet sold, in sample order.</param>
public sealed record CarriedData(IReadOnlyList<CarriedSystem> Systems, IReadOnlyList<CarriedSample> Samples)
{
    /// <summary>Gets nothing aboard.</summary>
    public static CarriedData Empty { get; } = new([], []);

    /// <summary>Gets how many bodies' scans are aboard.</summary>
    public int BodyCount => Systems.Sum(system => system.Bodies.Count);

    /// <summary>Gets how many of those bodies the player has mapped.</summary>
    public int MappedCount => Systems.Sum(system => system.Bodies.Count(body => body.Mapped));

    /// <summary>Gets whether nothing is aboard.</summary>
    public bool IsEmpty => Systems.Count == 0 && Samples.Count == 0;
}

/// <summary>One sale at Universal Cartographics.</summary>
/// <param name="At">When it was sold.</param>
/// <param name="Systems">The systems sold that were aboard, with the bodies scanned in each; a system the journal history never saw scanned is not listed.</param>
/// <param name="BaseValue">What the data was worth before bonuses, credits, as the game reported for the whole sale.</param>
/// <param name="Bonus">The bonuses paid on the whole sale, credits.</param>
/// <param name="TotalEarnings">What the whole sale paid, credits. The game does not say how much each system earned.</param>
public sealed record ExplorationSale(DateTimeOffset At, IReadOnlyList<CarriedSystem> Systems, long BaseValue, long Bonus, long TotalEarnings);

/// <summary>One species sold at Vista Genomics, matched to the sample it came from.</summary>
/// <param name="At">When it was sold.</param>
/// <param name="Sample">The sample, or one with an empty system and body when no sample aboard matched the species.</param>
/// <param name="Value">The base payout, credits.</param>
/// <param name="Bonus">The first-logged bonus paid, credits, 0 when none.</param>
public sealed record OrganicSale(DateTimeOffset At, CarriedSample Sample, long Value, long Bonus)
{
    /// <summary>Gets what the species paid in all, credits.</summary>
    public long Credits => Value + Bonus;
}

/// <summary>Everything sold across the journal history, which a death does not touch.</summary>
/// <param name="Exploration">The cartographics sales, oldest first.</param>
/// <param name="Organic">The species sold, oldest first.</param>
public sealed record SoldData(IReadOnlyList<ExplorationSale> Exploration, IReadOnlyList<OrganicSale> Organic)
{
    /// <summary>Gets nothing sold.</summary>
    public static SoldData Empty { get; } = new([], []);
}

/// <summary>
/// Follows the exploration and organic data the player carries: every scanned body until its system is
/// sold at Universal Cartographics, every completed sample until Vista Genomics buys them, all of it lost
/// on death. The game writes a scan only for a body new to the player's data, so a body is never counted
/// twice. What is sold moves to a ledger, <see cref="Sold"/>, with what each sale paid.
/// </summary>
public sealed class CarriedDataTracker : IJournalSink
{
    private static readonly HashSet<string> Events = new(StringComparer.Ordinal)
    {
        "Location", "FSDJump", "CarrierJump", "Scan", "SAAScanComplete", "SellExplorationData",
        "MultiSellExplorationData", "ScanOrganic", "SellOrganicData", "Died",
    };

    private readonly Lock _gate = new();
    private readonly Dictionary<long, MutableSystem> _systems = [];
    private readonly List<CarriedSample> _samples = [];
    private readonly HashSet<string> _sampled = new(StringComparer.Ordinal);
    private readonly List<ExplorationSale> _explorationSales = [];
    private readonly List<OrganicSale> _organicSales = [];
    private long _currentAddress;
    private string _currentName = "";

    /// <inheritdoc/>
    public IReadOnlySet<string>? InterestedEvents => Events;

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_gate)
        {
            _systems.Clear();
            _samples.Clear();
            _sampled.Clear();
            _explorationSales.Clear();
            _organicSales.Clear();
            _currentAddress = 0;
            _currentName = "";
        }
    }

    /// <inheritdoc/>
    public bool Apply(JournalEvent journalEvent)
    {
        ArgumentNullException.ThrowIfNull(journalEvent);
        var payload = journalEvent.Payload;
        lock (_gate)
        {
            switch (journalEvent.Name)
            {
                case "Location" or "FSDJump" or "CarrierJump":
                    _currentAddress = JsonRead.Identifier(payload, "SystemAddress");
                    _currentName = JsonRead.Text(payload, "StarSystem") ?? "";
                    return false;
                case "Scan":
                    return ApplyScan(payload);
                case "SAAScanComplete":
                    return ApplyMapped(payload);
                case "SellExplorationData":
                    return Sell(payload, journalEvent.Timestamp, JsonRead.Items(payload, "Systems").Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null));
                case "MultiSellExplorationData":
                    return Sell(payload, journalEvent.Timestamp, JsonRead.Items(payload, "Discovered").Select(item => JsonRead.Text(item, "SystemName")));
                case "ScanOrganic":
                    return ApplySample(payload);
                case "SellOrganicData":
                    return SellOrganic(payload, journalEvent.Timestamp);
                case "Died":
                    _systems.Clear();
                    _samples.Clear();
                    _sampled.Clear();
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>Copies the current state.</summary>
    /// <returns>A snapshot no later <see cref="Apply"/> changes.</returns>
    public CarriedData Snapshot()
    {
        lock (_gate)
        {
            return new CarriedData(
                [.. _systems.Values.Select(system => new CarriedSystem(system.Name, system.Address, [.. system.Bodies.Values]))],
                [.. _samples]);
        }
    }

    /// <summary>Copies the sales ledger.</summary>
    /// <returns>Every sale across the history, oldest first; a snapshot no later <see cref="Apply"/> changes.</returns>
    public SoldData Sold()
    {
        lock (_gate)
        {
            return new SoldData([.. _explorationSales], [.. _organicSales]);
        }
    }

    private bool ApplyScan(JsonElement payload)
    {
        var starType = JsonRead.Text(payload, "StarType");
        var planetClass = JsonRead.Text(payload, "PlanetClass");
        if (starType is null && planetClass is null)
        {
            // A belt cluster: scanned on arrival, worth nothing, not a body.
            return false;
        }

        if (IsNavBeaconScan(payload))
        {
            // A nav beacon hands over the system's bodies; the game pays nothing for data it gave away.
            return false;
        }

        var system = SystemFor(JsonRead.Identifier(payload, "SystemAddress"), JsonRead.Text(payload, "StarSystem"));
        var id = JsonRead.Count(payload, "BodyID");
        var mapped = system.Bodies.TryGetValue(id, out var known) && known.Mapped;
        system.Bodies[id] = new CarriedBody(
            id,
            JsonRead.Text(payload, "BodyName") ?? $"Body {id}",
            starType is not null,
            starType ?? planetClass,
            JsonRead.Measure(payload, "MassEM") ?? JsonRead.Measure(payload, "StellarMass"),
            string.Equals(JsonRead.Text(payload, "TerraformState"), "Terraformable", StringComparison.OrdinalIgnoreCase),
            JsonRead.Flag(payload, "WasDiscovered"),
            JsonRead.Flag(payload, "WasMapped"),
            mapped);
        return true;
    }

    private bool ApplyMapped(JsonElement payload)
    {
        var address = JsonRead.Identifier(payload, "SystemAddress");
        var id = JsonRead.Count(payload, "BodyID");
        if (!_systems.TryGetValue(address != 0 ? address : _currentAddress, out var system) || !system.Bodies.TryGetValue(id, out var body))
        {
            return false;
        }

        system.Bodies[id] = body with { Mapped = true };
        return true;
    }

    private bool ApplySample(JsonElement payload)
    {
        if (!string.Equals(JsonRead.Text(payload, "ScanType"), "Analyse", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var species = JsonRead.Text(payload, "Species") ?? "";
        var address = JsonRead.Identifier(payload, "SystemAddress");
        var bodyId = JsonRead.Count(payload, "Body");
        if (!_sampled.Add($"{address}|{bodyId}|{species}"))
        {
            return false;
        }

        var system = _systems.GetValueOrDefault(address != 0 ? address : _currentAddress);
        _samples.Add(new CarriedSample(
            JsonRead.Text(payload, "Genus") ?? "",
            JsonRead.Localised(payload, "Genus") ?? "",
            species,
            JsonRead.Localised(payload, "Species") ?? species,
            system?.Name ?? (address == 0 || address == _currentAddress ? _currentName : ""),
            system?.Bodies.GetValueOrDefault(bodyId)?.Name ?? "",
            JsonRead.Flag(payload, "WasLogged")));
        return true;
    }

    /// <summary>Whether a scan came from a nav beacon, <c>ScanType</c> <c>NavBeacon</c> or <c>NavBeaconDetail</c>, rather than the player's own scanner.</summary>
    internal static bool IsNavBeaconScan(JsonElement payload)
        => JsonRead.Text(payload, "ScanType") is { } type && type.StartsWith("NavBeacon", StringComparison.OrdinalIgnoreCase);

    private bool Sell(JsonElement payload, DateTimeOffset at, IEnumerable<string?> systemNames)
    {
        var sold = new HashSet<string>(systemNames.Where(name => name is not null)!, StringComparer.OrdinalIgnoreCase);
        var removed = _systems.Where(pair => sold.Contains(pair.Value.Name)).Select(pair => pair.Key).ToList();
        var systems = new List<CarriedSystem>();
        foreach (var key in removed)
        {
            var system = _systems[key];
            systems.Add(new CarriedSystem(system.Name, system.Address, [.. system.Bodies.Values]));
            _systems.Remove(key);
        }

        _explorationSales.Add(new ExplorationSale(
            at,
            systems,
            JsonRead.Identifier(payload, "BaseValue"),
            JsonRead.Identifier(payload, "Bonus"),
            JsonRead.Identifier(payload, "TotalEarnings")));
        return true;
    }

    /// <summary>
    /// Vista Genomics buys every sample at once. Each species sold is matched to the oldest unmatched sample of
    /// that species aboard, which is how a sale is attributed to a system and a body; a species with no sample
    /// aboard, from before the journal history say, is kept with neither.
    /// </summary>
    private bool SellOrganic(JsonElement payload, DateTimeOffset at)
    {
        var unmatched = new List<CarriedSample>(_samples);
        foreach (var item in JsonRead.Items(payload, "BioData"))
        {
            var species = JsonRead.Text(item, "Species") ?? "";
            var index = unmatched.FindIndex(sample => string.Equals(sample.Species, species, StringComparison.Ordinal));
            var sample = index >= 0 ? unmatched[index] : new CarriedSample(JsonRead.Text(item, "Genus") ?? "", JsonRead.Localised(item, "Genus") ?? "", species, JsonRead.Localised(item, "Species") ?? species, "", "", true);
            if (index >= 0)
            {
                unmatched.RemoveAt(index);
            }

            _organicSales.Add(new OrganicSale(at, sample, JsonRead.Identifier(item, "Value"), JsonRead.Identifier(item, "Bonus")));
        }

        _samples.Clear();
        _sampled.Clear();
        return true;
    }

    private MutableSystem SystemFor(long address, string? name)
    {
        var key = address != 0 ? address : _currentAddress;
        if (!_systems.TryGetValue(key, out var system))
        {
            system = new MutableSystem { Address = key, Name = name ?? _currentName };
            _systems[key] = system;
        }

        return system;
    }

    private sealed class MutableSystem
    {
        public long Address { get; init; }

        public string Name { get; init; } = "";

        public Dictionary<int, CarriedBody> Bodies { get; } = [];
    }
}
