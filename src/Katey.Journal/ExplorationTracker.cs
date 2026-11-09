using System.Collections.Frozen;
using System.Text.Json;

namespace Katey.Journal;

/// <summary>A life form found on a body and how far its scan has got.</summary>
/// <param name="Genus">Genus symbol such as <c>$Codex_Ent_Bacterial_Genus_Name;</c>.</param>
/// <param name="Species">Species symbol such as <c>$Codex_Ent_Bacterial_01_Name;</c>.</param>
/// <param name="SpeciesName">Localised species name.</param>
/// <param name="VariantName">Localised variant name, or <see langword="null"/>.</param>
/// <param name="Stage">1 after the first sample, 2 after the second, 3 when analysed.</param>
/// <param name="WasLogged">Whether someone had logged the species on this body before the player sampled it, as the game says with each sample; <see langword="false"/> means the first-logged bonus is the player's at the sale.</param>
/// <param name="Sold">Whether the player has sold it: sampled in full and then included in a sale at Vista Genomics.</param>
public sealed record OrganicScan(string Genus, string Species, string SpeciesName, string? VariantName, int Stage, bool WasLogged = false, bool Sold = false);

/// <summary>A genus the surface scanner reports on a body.</summary>
/// <param name="Genus">Genus symbol.</param>
/// <param name="Name">Localised genus name.</param>
public sealed record GenusSignal(string Genus, string Name);

/// <summary>One scanned body in the current system.</summary>
/// <param name="BodyId">The journal's body id, unique within the system.</param>
/// <param name="Name">Full body name.</param>
/// <param name="IsStar">Whether it is a star.</param>
/// <param name="ParentStarId">
/// Body id of the star it orbits, the nearest star among its parents, or <see langword="null"/> for a star
/// and for a body orbiting the barycentre of several stars.
/// </param>
/// <param name="Class">Star type or planet class as the journal spells it, or <see langword="null"/> for a belt cluster.</param>
/// <param name="DistanceLs">Distance from the arrival point in light seconds.</param>
/// <param name="Landable">Whether a ship can land on it.</param>
/// <param name="Atmosphere">Atmosphere description, or empty.</param>
/// <param name="AtmosphereType">Atmosphere type symbol, or empty.</param>
/// <param name="Volcanism">Volcanism description, or empty.</param>
/// <param name="SurfaceGravityG">Surface gravity in g, or <see langword="null"/> for stars.</param>
/// <param name="SurfaceTemperatureK">Surface temperature in kelvin, or <see langword="null"/>.</param>
/// <param name="SurfacePressurePa">Surface pressure in pascals, or <see langword="null"/>.</param>
/// <param name="MassEarths">Mass in Earth masses for planets, solar masses for stars, or <see langword="null"/>.</param>
/// <param name="Terraformable">Whether the body can be terraformed.</param>
/// <param name="HasRings">Whether it has rings or belts.</param>
/// <param name="Materials">Raw material symbol → surface percentage.</param>
/// <param name="WasDiscovered">Whether someone had discovered it before this scan.</param>
/// <param name="WasMapped">Whether someone had mapped it before this scan.</param>
/// <param name="WasFootfalled">Whether someone had set foot on it before this scan. The game writes false in a populated system, where footfall cannot be claimed; <see cref="SystemSnapshot.Populated"/> says when that applies.</param>
/// <param name="MappedByYou">Whether the player mapped it this visit.</param>
/// <param name="LandedByYou">Whether the player has ever landed on it.</param>
/// <param name="BiologicalSignals">Number of biological signals, or 0.</param>
/// <param name="GeologicalSignals">Number of geological signals, or 0.</param>
/// <param name="OtherSignals">Localised names of other signal types, such as Human or Guardian.</param>
/// <param name="Genuses">Genera the surface scanner found.</param>
/// <param name="Organics">Life forms sampled on this body, by species.</param>
/// <param name="ScannedAt">When the player scanned it, on the visit that did.</param>
/// <param name="FromCatalogue">Whether the body comes from a catalogue rather than the player's own scan, so its discovery and mapping flags say nothing about the player.</param>
/// <param name="FromNavBeacon">Whether the body's last scan came from a nav beacon rather than the player's scanner: the body is known, but the data is worth nothing to sell.</param>
/// <param name="Sold">Whether the player sold the system's exploration data after scanning this body, so the scan is no longer aboard.</param>
public sealed record ScannedBody(
    int BodyId,
    string Name,
    bool IsStar,
    int? ParentStarId,
    string? Class,
    double DistanceLs,
    bool Landable,
    string Atmosphere,
    string AtmosphereType,
    string Volcanism,
    double? SurfaceGravityG,
    double? SurfaceTemperatureK,
    double? SurfacePressurePa,
    double? MassEarths,
    bool Terraformable,
    bool HasRings,
    IReadOnlyDictionary<string, double> Materials,
    bool WasDiscovered,
    bool WasMapped,
    bool WasFootfalled,
    bool MappedByYou,
    bool LandedByYou,
    int BiologicalSignals,
    int GeologicalSignals,
    IReadOnlyList<string> OtherSignals,
    IReadOnlyList<GenusSignal> Genuses,
    IReadOnlyList<OrganicScan> Organics,
    DateTimeOffset ScannedAt = default,
    bool FromCatalogue = false,
    bool FromNavBeacon = false,
    bool Sold = false)
{
    /// <summary>Gets whether the player has sold exobiology data from this body: any species sampled here in full has been through a sale.</summary>
    public bool ExobiologySold => Organics.Any(organic => organic.Sold);
}

/// <summary>What the player has learned about a species from selling it.</summary>
/// <param name="Species">Species symbol.</param>
/// <param name="SpeciesName">Localised name.</param>
/// <param name="Value">Base value paid, credits.</param>
/// <param name="Bonus">First-discovery bonus paid, credits, or 0.</param>
public sealed record LearnedSpeciesValue(string Species, string SpeciesName, long Value, long Bonus);

/// <summary>The current system as the journal describes it.</summary>
/// <param name="SystemName">System name, or <see langword="null"/> before arrival.</param>
/// <param name="SystemAddress">System id, or 0.</param>
/// <param name="BodyCount">Bodies the discovery scanner reports, or <see langword="null"/> before a honk.</param>
/// <param name="NonBodyCount">Non-body signals the discovery scanner reports, or <see langword="null"/>.</param>
/// <param name="AllBodiesFound">Whether every body has been found.</param>
/// <param name="BodiesKnown">
/// Bodies the game counts as scanned, or <see langword="null"/> before a honk. A honk whose progress is 1
/// found every body already in the player's data, scanned on an earlier visit or before the journal history
/// begins, so the whole <see cref="BodyCount"/> is known; so is it once <see cref="AllBodiesFound"/>.
/// Otherwise this is <see cref="BodiesScanned"/>: a partial progress figure is weighted, not a count, and is
/// not used. Never below <see cref="BodiesScanned"/>, never above <see cref="BodyCount"/>.
/// </param>
/// <param name="CurrentBody">The body the ship is approaching or on, or <see langword="null"/>.</param>
/// <param name="Bodies">
/// Bodies the player has scanned in this system, on this visit or an earlier one the journal history holds,
/// by distance from arrival. A body scanned before the history began is not here: the journal describes a
/// body only when it is first scanned.
/// </param>
/// <param name="LearnedSpecies">Species values learned from the whole journal history, by species symbol.</param>
/// <param name="ArrivedAt">When the player arrived in the system this visit, or <see langword="null"/> before any arrival.</param>
/// <param name="Populated">Whether the system has a population, in which case footfall cannot be claimed and the scans' footfall flag says nothing.</param>
public sealed record SystemSnapshot(
    string? SystemName,
    long SystemAddress,
    int? BodyCount,
    int? NonBodyCount,
    bool AllBodiesFound,
    int? BodiesKnown,
    string? CurrentBody,
    IReadOnlyList<ScannedBody> Bodies,
    IReadOnlyDictionary<string, LearnedSpeciesValue> LearnedSpecies,
    DateTimeOffset? ArrivedAt = null,
    bool Populated = false)
{
    /// <summary>Gets nothing known yet.</summary>
    public static SystemSnapshot Empty { get; } =
        new(null, 0, null, null, false, null, null, [], FrozenDictionary<string, LearnedSpeciesValue>.Empty);

    /// <summary>Gets the bodies scanned this visit that the game's body count includes: stars and planets, not belt clusters.</summary>
    public int BodiesScanned => Bodies.Count(body => body.Class is not null);

    /// <summary>Gets how many bodies the game counts as scanned that this visit did not scan: known from an earlier visit. 0 before a honk.</summary>
    public int BodiesKnownEarlier => Math.Max(0, (BodiesKnown ?? 0) - BodiesScanned);
}

/// <summary>
/// Follows the current system's bodies as the player scans them, and remembers across the whole history
/// every system's scanned bodies, which bodies the player has landed on, and what each species sold for.
/// Returning to a system brings its bodies back, since the game writes no scan for a body already known,
/// their sampling stages with them; a sale leaves the stages as they are, as the game does.
/// A first, once the player's, stays theirs: a scan after the player sold the system's data keeps a body
/// undiscovered or unmapped when the earlier scan said so, and a scan after the player landed keeps it
/// unfootfalled, whatever the later flags say, since by then the flags report the player's own record.
/// </summary>
public sealed class ExplorationTracker : IJournalSink
{
    private const double StandardGravity = 9.80665;

    private static readonly HashSet<string> Events = new(StringComparer.Ordinal)
    {
        "Location", "FSDJump", "CarrierJump", "FSSDiscoveryScan", "FSSAllBodiesFound", "Scan",
        "FSSBodySignals", "SAASignalsFound", "SAAScanComplete", "Touchdown", "Liftoff", "ApproachBody",
        "LeaveBody", "ScanOrganic", "SellOrganicData", "SellExplorationData", "MultiSellExplorationData",
    };

    private readonly Lock _gate = new();
    private readonly Dictionary<long, SystemMemory> _systems = [];
    private readonly Dictionary<string, long> _addressesByName = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<int, MutableBody> _bodies = [];
    private DateTimeOffset? _arrivedAt;
    private bool _populated;
    private readonly HashSet<string> _landed = new(StringComparer.OrdinalIgnoreCase);
    private (long Address, int BodyId, string Species)? _sampling;
    private readonly Dictionary<string, LearnedSpeciesValue> _learned = new(StringComparer.Ordinal);
    private string? _systemName;
    private long _systemAddress;
    private int? _bodyCount;
    private int? _nonBodyCount;
    private double? _honkProgress;
    private bool _allFound;
    private string? _currentBody;

    /// <inheritdoc/>
    public IReadOnlySet<string>? InterestedEvents => Events;

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_gate)
        {
            _systems.Clear();
            _addressesByName.Clear();
            _bodies = [];
            _arrivedAt = null;
            _landed.Clear();
            _sampling = null;
            _learned.Clear();
            _systemName = null;
            _systemAddress = 0;
            _bodyCount = null;
            _nonBodyCount = null;
            _honkProgress = null;
            _allFound = false;
            _currentBody = null;
        }
    }

    /// <inheritdoc/>
    public bool Apply(JournalEvent journalEvent)
    {
        ArgumentNullException.ThrowIfNull(journalEvent);
        var payload = journalEvent.Payload;

        lock (_gate)
        {
            return journalEvent.Name switch
            {
                "Location" or "FSDJump" or "CarrierJump" => EnterSystem(payload, journalEvent.Timestamp),
                "FSSDiscoveryScan" => Honk(payload),
                "FSSAllBodiesFound" => SetAllFound(),
                "Scan" => ApplyScan(payload, journalEvent.Timestamp),
                "FSSBodySignals" or "SAASignalsFound" => ApplySignals(payload),
                "SAAScanComplete" => ApplyMapped(payload),
                "Touchdown" => ApplyTouchdown(payload),
                "ApproachBody" => SetCurrentBody(JsonRead.Text(payload, "Body")),
                "Liftoff" => SetCurrentBody(JsonRead.Text(payload, "Body")),
                "LeaveBody" => SetCurrentBody(null),
                "ScanOrganic" => ApplyOrganic(payload),
                "SellOrganicData" => ApplySale(payload),
                "SellExplorationData" => MarkSold(JsonRead.Items(payload, "Systems").Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null)),
                "MultiSellExplorationData" => MarkSold(JsonRead.Items(payload, "Discovered").Select(item => JsonRead.Text(item, "SystemName"))),
                _ => false,
            };
        }
    }

    /// <summary>Copies the current state.</summary>
    /// <returns>A snapshot no later <see cref="Apply"/> changes.</returns>
    public SystemSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new SystemSnapshot(
                _systemName,
                _systemAddress,
                _bodyCount,
                _nonBodyCount,
                _allFound,
                BodiesKnown(),
                _currentBody,
                [.. _bodies.Values.OrderBy(body => body.DistanceLs).ThenBy(body => body.BodyId).Select(body => body.ToRecord(_landed.Contains(LandingKey(_systemAddress, body.Name))))],
                _learned.ToFrozenDictionary(StringComparer.Ordinal),
                _arrivedAt,
                _populated);
        }
    }

    private static string LandingKey(long systemAddress, string body) => $"{systemAddress}|{body}";

    /// <summary>
    /// Bodies the game counts as scanned: the whole body count once every body is found or when the honk
    /// found everything already in the player's data, otherwise the stars and planets scanned this visit.
    /// Belt clusters are scanned on arrival but are not in the game's body count, so they are left out.
    /// </summary>
    private int? BodiesKnown()
    {
        return _bodyCount is not { } count
            ? null
            : _allFound || _honkProgress >= 1 ? count : Math.Min(count, _bodies.Values.Count(body => body.Class is not null));
    }

    private bool EnterSystem(JsonElement payload, DateTimeOffset timestamp)
    {
        var address = JsonRead.Identifier(payload, "SystemAddress");
        var name = JsonRead.Text(payload, "StarSystem");
        if (address != _systemAddress)
        {
            // The system being left keeps its bodies and its honk; the one entered gets its own back, or starts fresh.
            if (_systemAddress != 0)
            {
                _systems[_systemAddress] = new SystemMemory(_bodies, _bodyCount, _nonBodyCount, _honkProgress, _allFound, _populated);
            }

            var memory = _systems.GetValueOrDefault(address);
            _bodies = memory?.Bodies ?? [];
            _bodyCount = memory?.BodyCount;
            _nonBodyCount = memory?.NonBodyCount;
            _honkProgress = memory?.HonkProgress;
            _allFound = memory?.AllFound ?? false;
            _populated = memory?.Populated ?? false;
            _currentBody = null;
            _arrivedAt = timestamp;
        }

        if (JsonRead.Measure(payload, "Population") is { } population)
        {
            _populated = population > 0;
        }

        _arrivedAt ??= timestamp;
        _systemAddress = address;
        _systemName = name;
        if (name is not null && address != 0)
        {
            _addressesByName[name] = address;
        }

        return true;
    }

    /// <summary>
    /// An exploration sale: every scanned body in each named system is marked sold, so a later scan of it
    /// cannot take a first discovery or first mapping away from the player.
    /// </summary>
    private bool MarkSold(IEnumerable<string?> systemNames)
    {
        var changed = false;
        foreach (var name in systemNames)
        {
            if (name is null || !_addressesByName.TryGetValue(name, out var address))
            {
                continue;
            }

            var bodies = address == _systemAddress ? _bodies : _systems.GetValueOrDefault(address)?.Bodies;
            foreach (var body in bodies?.Values.Where(body => body.Scanned && !body.Sold) ?? [])
            {
                body.Sold = true;
                changed = true;
            }
        }

        return changed;
    }

    private bool Honk(JsonElement payload)
    {
        _bodyCount = JsonRead.Count(payload, "BodyCount");
        _nonBodyCount = JsonRead.Count(payload, "NonBodyCount");
        _honkProgress = JsonRead.Measure(payload, "Progress");
        return true;
    }

    private bool SetAllFound()
    {
        _allFound = true;
        return true;
    }

    private bool SetCurrentBody(string? body)
    {
        _currentBody = body;
        return true;
    }

    private MutableBody BodyFor(JsonElement payload)
    {
        var id = JsonRead.Count(payload, "BodyID");
        if (!_bodies.TryGetValue(id, out var body))
        {
            body = new MutableBody { BodyId = id, Name = JsonRead.Text(payload, "BodyName") ?? JsonRead.Text(payload, "Body") ?? $"Body {id}" };
            _bodies[id] = body;
        }

        return body;
    }

    private bool ApplyScan(JsonElement payload, DateTimeOffset timestamp)
    {
        if (JsonRead.Identifier(payload, "SystemAddress") is var address && address != 0 && address != _systemAddress)
        {
            return false;
        }

        var body = BodyFor(payload);
        body.ScannedAt = timestamp;
        body.FromNavBeacon = CarriedDataTracker.IsNavBeaconScan(payload);
        body.Name = JsonRead.Text(payload, "BodyName") ?? body.Name;
        body.IsStar = JsonRead.Text(payload, "StarType") is not null;
        body.ParentStarId = body.IsStar ? null : ParentStar(payload);
        body.Class = JsonRead.Text(payload, "StarType") ?? JsonRead.Text(payload, "PlanetClass");
        body.DistanceLs = JsonRead.Measure(payload, "DistanceFromArrivalLS") ?? 0;
        body.Landable = JsonRead.Flag(payload, "Landable");
        body.Atmosphere = JsonRead.Text(payload, "Atmosphere") ?? "";
        body.AtmosphereType = JsonRead.Text(payload, "AtmosphereType") ?? "";
        body.Volcanism = JsonRead.Text(payload, "Volcanism") ?? "";
        body.SurfaceGravityG = JsonRead.Measure(payload, "SurfaceGravity") is { } gravity ? gravity / StandardGravity : null;
        body.SurfaceTemperatureK = JsonRead.Measure(payload, "SurfaceTemperature");
        body.SurfacePressurePa = JsonRead.Measure(payload, "SurfacePressure");
        body.MassEarths = JsonRead.Measure(payload, "MassEM") ?? JsonRead.Measure(payload, "StellarMass");
        body.Terraformable = string.Equals(JsonRead.Text(payload, "TerraformState"), "Terraformable", StringComparison.OrdinalIgnoreCase);
        body.HasRings = JsonRead.Items(payload, "Rings").Any();
        body.Materials = JsonRead.Items(payload, "Materials")
            .Select(material => (Name: JsonRead.Text(material, "Name"), Percent: JsonRead.Measure(material, "Percent") ?? 0))
            .Where(material => material.Name is not null)
            .ToDictionary(material => material.Name!, material => material.Percent, StringComparer.OrdinalIgnoreCase);
        var wasDiscovered = JsonRead.Flag(payload, "WasDiscovered");
        var wasMapped = JsonRead.Flag(payload, "WasMapped");
        var wasFootfalled = JsonRead.Flag(payload, "WasFootfalled");
        if (body.Scanned)
        {
            // A first, once the player's, stays theirs: after the sale the flags report the player's own record,
            // and a footfall is the player's from the landing on.
            wasDiscovered = wasDiscovered && (body.WasDiscovered || !body.Sold);
            wasMapped = wasMapped && (body.WasMapped || !body.Sold || !body.MappedByYou);
            wasFootfalled = wasFootfalled && (body.WasFootfalled || !_landed.Contains(LandingKey(_systemAddress, body.Name)));
        }

        body.WasDiscovered = wasDiscovered;
        body.WasMapped = wasMapped;
        body.WasFootfalled = wasFootfalled;
        body.Scanned = true;
        return true;
    }

    /// <summary>The nearest star among a scan's parents: the first <c>Star</c> entry of <c>Parents</c>, which lists them innermost first.</summary>
    private static int? ParentStar(JsonElement payload)
    {
        foreach (var parent in JsonRead.Items(payload, "Parents"))
        {
            if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty("Star", out var star) && star.TryGetInt32(out var id))
            {
                return id;
            }
        }

        return null;
    }

    private bool ApplySignals(JsonElement payload)
    {
        if (JsonRead.Identifier(payload, "SystemAddress") is var address && address != 0 && address != _systemAddress)
        {
            return false;
        }

        var body = BodyFor(payload);
        foreach (var signal in JsonRead.Items(payload, "Signals"))
        {
            var type = JsonRead.Text(signal, "Type") ?? "";
            var count = JsonRead.Count(signal, "Count");
            if (type.Contains("Biological", StringComparison.OrdinalIgnoreCase))
            {
                body.BiologicalSignals = Math.Max(body.BiologicalSignals, count);
            }
            else if (type.Contains("Geological", StringComparison.OrdinalIgnoreCase))
            {
                body.GeologicalSignals = Math.Max(body.GeologicalSignals, count);
            }
            else
            {
                var label = JsonRead.Localised(signal, "Type") ?? type;
                if (!body.OtherSignals.Contains(label, StringComparer.Ordinal))
                {
                    body.OtherSignals.Add(label);
                }
            }
        }

        var genuses = JsonRead.Items(payload, "Genuses")
            .Select(genus => new GenusSignal(JsonRead.Text(genus, "Genus") ?? "", JsonRead.Localised(genus, "Genus") ?? ""))
            .Where(genus => genus.Genus.Length > 0)
            .ToList();
        if (genuses.Count > 0)
        {
            body.Genuses = genuses;
        }

        return true;
    }

    private bool ApplyMapped(JsonElement payload)
    {
        var body = BodyFor(payload);
        body.MappedByYou = true;
        return true;
    }

    private bool ApplyTouchdown(JsonElement payload)
    {
        var bodyName = JsonRead.Text(payload, "Body");
        var address = JsonRead.Identifier(payload, "SystemAddress");
        if (bodyName is not null)
        {
            _landed.Add(LandingKey(address != 0 ? address : _systemAddress, bodyName));
            _currentBody = bodyName;
        }

        return true;
    }

    private bool ApplyOrganic(JsonElement payload)
    {
        if (JsonRead.Identifier(payload, "SystemAddress") is var address && address != 0 && address != _systemAddress)
        {
            return false;
        }

        var bodyId = JsonRead.Count(payload, "Body");
        if (!_bodies.TryGetValue(bodyId, out var body))
        {
            body = new MutableBody { BodyId = bodyId, Name = _currentBody ?? $"Body {bodyId}" };
            _bodies[bodyId] = body;
        }

        var species = JsonRead.Text(payload, "Species") ?? "";
        var stage = JsonRead.Text(payload, "ScanType") switch
        {
            "Log" => 1,
            "Sample" => 2,
            "Analyse" => 3,
            _ => 1,
        };
        DropPartialSample(species);
        _sampling = stage >= 3 ? null : (_systemAddress, bodyId, species);
        var existing = body.Organics.GetValueOrDefault(species);
        body.Organics[species] = new OrganicScan(
            JsonRead.Text(payload, "Genus") ?? "",
            species,
            JsonRead.Localised(payload, "Species") ?? species,
            JsonRead.Localised(payload, "Variant") ?? existing?.VariantName,
            Math.Max(existing?.Stage ?? 0, stage),
            (existing?.WasLogged ?? false) || JsonRead.Flag(payload, "WasLogged"),
            existing?.Sold ?? false);
        return true;
    }

    /// <summary>
    /// The genetic sampler holds one species at a time: starting another before the third sample throws the
    /// partial one away, so a species left at stage 1 or 2 is forgotten when a different species is sampled.
    /// </summary>
    private void DropPartialSample(string species)
    {
        if (_sampling is not { } held || string.Equals(held.Species, species, StringComparison.Ordinal))
        {
            return;
        }

        var bodies = held.Address == _systemAddress ? _bodies : _systems.GetValueOrDefault(held.Address)?.Bodies;
        if (bodies?.GetValueOrDefault(held.BodyId) is { } body && body.Organics.GetValueOrDefault(held.Species) is { Stage: < 3 })
        {
            body.Organics.Remove(held.Species);
        }

        _sampling = null;
    }

    /// <summary>
    /// Vista Genomics buys every sample aboard at once, so every species sampled in full anywhere in the history
    /// and not yet sold is marked sold; what each species paid is learned as well.
    /// </summary>
    private bool ApplySale(JsonElement payload)
    {
        var changed = false;
        foreach (var body in _systems.Values.SelectMany(system => system.Bodies.Values).Concat(_bodies.Values))
        {
            foreach (var organic in body.Organics.Values.Where(organic => organic.Stage >= 3 && !organic.Sold).ToList())
            {
                body.Organics[organic.Species] = organic with { Sold = true };
                changed = true;
            }
        }

        foreach (var item in JsonRead.Items(payload, "BioData"))
        {
            var species = JsonRead.Text(item, "Species");
            if (species is null)
            {
                continue;
            }

            var value = JsonRead.Identifier(item, "Value");
            var bonus = JsonRead.Identifier(item, "Bonus");
            if (!_learned.TryGetValue(species, out var known) || known.Value < value || known.Bonus < bonus)
            {
                _learned[species] = new LearnedSpeciesValue(
                    species,
                    JsonRead.Localised(item, "Species") ?? species,
                    Math.Max(known?.Value ?? 0, value),
                    Math.Max(known?.Bonus ?? 0, bonus));
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>A system's bodies and honk as they stood when the player left it.</summary>
    private sealed record SystemMemory(Dictionary<int, MutableBody> Bodies, int? BodyCount, int? NonBodyCount, double? HonkProgress, bool AllFound, bool Populated);

    private sealed class MutableBody
    {
        public int BodyId { get; init; }

        public DateTimeOffset ScannedAt { get; set; }

        public string Name { get; set; } = "";

        public bool IsStar { get; set; }

        public int? ParentStarId { get; set; }

        public string? Class { get; set; }

        public double DistanceLs { get; set; }

        public bool Landable { get; set; }

        public string Atmosphere { get; set; } = "";

        public string AtmosphereType { get; set; } = "";

        public string Volcanism { get; set; } = "";

        public double? SurfaceGravityG { get; set; }

        public double? SurfaceTemperatureK { get; set; }

        public double? SurfacePressurePa { get; set; }

        public double? MassEarths { get; set; }

        public bool Terraformable { get; set; }

        public bool HasRings { get; set; }

        public Dictionary<string, double> Materials { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public bool WasDiscovered { get; set; }

        public bool WasMapped { get; set; }

        public bool WasFootfalled { get; set; }

        public bool FromNavBeacon { get; set; }

        /// <summary>Whether a <c>Scan</c> has described it; a body can exist from its signals or an organic scan alone.</summary>
        public bool Scanned { get; set; }

        /// <summary>Whether the player sold the system's exploration data after scanning it.</summary>
        public bool Sold { get; set; }

        public bool MappedByYou { get; set; }

        public int BiologicalSignals { get; set; }

        public int GeologicalSignals { get; set; }

        public List<string> OtherSignals { get; } = [];

        public List<GenusSignal> Genuses { get; set; } = [];

        public Dictionary<string, OrganicScan> Organics { get; } = new(StringComparer.Ordinal);

        public ScannedBody ToRecord(bool landed) => new(
            BodyId,
            Name,
            IsStar,
            ParentStarId,
            Class,
            DistanceLs,
            Landable,
            Atmosphere,
            AtmosphereType,
            Volcanism,
            SurfaceGravityG,
            SurfaceTemperatureK,
            SurfacePressurePa,
            MassEarths,
            Terraformable,
            HasRings,
            Materials.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
            WasDiscovered,
            WasMapped,
            WasFootfalled,
            MappedByYou,
            landed,
            BiologicalSignals,
            GeologicalSignals,
            [.. OtherSignals],
            [.. Genuses],
            [.. Organics.Values.OrderBy(organic => organic.SpeciesName, StringComparer.Ordinal)],
            ScannedAt,
            FromNavBeacon: FromNavBeacon,
            Sold: Sold);
    }
}
