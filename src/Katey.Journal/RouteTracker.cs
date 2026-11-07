using System.Text.Json;

namespace Katey.Journal;

/// <summary>A point in the game's galactic frame, light years from Sol along each axis.</summary>
/// <param name="X">Galactic x.</param>
/// <param name="Y">Galactic y.</param>
/// <param name="Z">Galactic z.</param>
public readonly record struct StarPosition(double X, double Y, double Z);

/// <summary>A system the player has arrived in at some point in the journal history.</summary>
/// <param name="Name">System name as the journal spells it.</param>
/// <param name="Address">The game's 64-bit system id.</param>
/// <param name="Position">Where it is, or <see langword="null"/> when the event carried no coordinates.</param>
/// <param name="LastVisit">When the player last arrived there.</param>
/// <param name="Visits">How many arrivals the history holds.</param>
/// <param name="Surveyed">Whether the player has ever run the discovery scanner here, read the nav beacon, or found every body.</param>
public sealed record VisitedSystem(string Name, long Address, StarPosition? Position, DateTimeOffset LastVisit, int Visits, bool Surveyed = false);

/// <summary>One hyperspace jump the ship made, for fuel estimates.</summary>
/// <param name="At">When it arrived.</param>
/// <param name="DistanceLy">Jump distance, light years.</param>
/// <param name="FuelUsed">Tonnes burned.</param>
/// <param name="Boost">The range multiplier in effect: 4 after a neutron star, 1.5 after a white dwarf, 1 otherwise.</param>
public sealed record JumpRecord(DateTimeOffset At, double DistanceLy, double FuelUsed, double Boost);

/// <summary>Where the ship is and where it is going, as far as the journal says.</summary>
/// <param name="CurrentSystem">The system the ship is in, or <see langword="null"/> before the first <c>Location</c> or jump.</param>
/// <param name="CurrentSystemAddress">The current system's id, or 0.</param>
/// <param name="CurrentPosition">Where the current system is, or <see langword="null"/> when unknown.</param>
/// <param name="TargetSystem">The next jump's destination from <c>FSDTarget</c>, or <see langword="null"/>.</param>
/// <param name="TargetStarClass">The next destination's star class, or <see langword="null"/>.</param>
/// <param name="RemainingJumps">Jumps left on the plotted route after the next one, or <see langword="null"/> when no route is plotted.</param>
/// <param name="InHyperspace">Whether a jump has started and not yet arrived.</param>
/// <param name="KnownSystems">Every system the player has ever arrived in, by id, from the whole journal history.</param>
/// <param name="RecentJumps">The last jumps in the ship being flown, newest first. Cleared when the player changes ship or loads the game.</param>
/// <param name="ArrivedAt">When the journal last placed the ship in a system, by a jump or a <c>Location</c>, or <see langword="null"/> before any.</param>
/// <param name="Supercharged">Whether the drive holds a jet-cone boost not yet spent: a <c>JetConeBoost</c> since the last jump.</param>
public sealed record RouteProgress(
    string? CurrentSystem,
    long CurrentSystemAddress,
    StarPosition? CurrentPosition,
    string? TargetSystem,
    string? TargetStarClass,
    int? RemainingJumps,
    bool InHyperspace,
    IReadOnlyDictionary<long, VisitedSystem> KnownSystems,
    IReadOnlyList<JumpRecord> RecentJumps,
    DateTimeOffset? ArrivedAt = null,
    bool Supercharged = false)
{
    /// <summary>Gets nothing known yet.</summary>
    public static RouteProgress Empty { get; } = new(null, 0, null, null, null, null, false, new Dictionary<long, VisitedSystem>(), []);

    /// <summary>Whether the player has ever arrived in a system.</summary>
    /// <param name="systemAddress">The system id.</param>
    /// <returns><see langword="true"/> when the history holds an arrival there.</returns>
    public bool HasVisited(long systemAddress) => KnownSystems.ContainsKey(systemAddress);

    /// <summary>Finds a known system by name.</summary>
    /// <param name="name">System name, compared case-insensitively.</param>
    /// <returns>The system, or <see langword="null"/> when the player has never arrived there.</returns>
    public VisitedSystem? FindByName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return KnownSystems.Values.FirstOrDefault(system => string.Equals(system.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Follows the ship's position along a plotted route and remembers every system ever visited, with its
/// coordinates when the arrival event carried them.
/// </summary>
public sealed class RouteTracker : IJournalSink
{
    /// <summary>How many recent jumps are kept.</summary>
    public const int RecentJumpCount = 12;

    private static readonly HashSet<string> Events = new(StringComparer.Ordinal)
    {
        "Location", "FSDJump", "CarrierJump", "FSDTarget", "StartJump", "NavRouteClear", "SupercruiseExit",
        "LoadGame", "ShipyardSwap", "ShipyardNew", "Loadout", "FSSDiscoveryScan", "NavBeaconScan", "FSSAllBodiesFound", "JetConeBoost",
    };

    private readonly Lock _gate = new();
    private readonly Dictionary<long, VisitedSystem> _known = [];
    private readonly List<JumpRecord> _recentJumps = [];
    private string? _drive;
    private string? _currentSystem;
    private long _currentAddress;
    private StarPosition? _currentPosition;
    private string? _targetSystem;
    private string? _targetClass;
    private DateTimeOffset? _arrivedAt;
    private bool _supercharged;
    private int? _remaining;
    private bool _inHyperspace;

    /// <inheritdoc/>
    public IReadOnlySet<string>? InterestedEvents => Events;

    /// <summary>Reads a <c>StarPos</c> array.</summary>
    /// <param name="payload">An event with an optional <c>StarPos</c> of three numbers.</param>
    /// <returns>The position, or <see langword="null"/> when absent or malformed.</returns>
    public static StarPosition? ReadStarPos(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty("StarPos", out var array)
            || array.ValueKind != JsonValueKind.Array
            || array.GetArrayLength() != 3)
        {
            return null;
        }

        var values = new double[3];
        var index = 0;
        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            values[index++] = element.GetDouble();
        }

        return new StarPosition(values[0], values[1], values[2]);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_gate)
        {
            _known.Clear();
            _recentJumps.Clear();
            _drive = null;
            _currentSystem = null;
            _currentAddress = 0;
            _arrivedAt = null;
            _supercharged = false;
            _currentPosition = null;
            _targetSystem = null;
            _targetClass = null;
            _remaining = null;
            _inHyperspace = false;
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
                case "Location":
                case "FSDJump":
                case "CarrierJump":
                    Arrive(payload, journalEvent.Timestamp, journalEvent.Name == "FSDJump");
                    if (journalEvent.Name == "FSDJump")
                    {
                        RecordJump(payload, journalEvent.Timestamp);
                    }

                    return true;
                case "LoadGame":
                case "ShipyardSwap":
                case "ShipyardNew":
                    _recentJumps.Clear();
                    return true;
                case "Loadout":
                    return ReadDrive(payload);
                case "FSSDiscoveryScan":
                case "NavBeaconScan":
                case "FSSAllBodiesFound":
                    return MarkSurveyed(payload);
                case "JetConeBoost":
                    _supercharged = true;
                    return true;
                case "FSDTarget":
                    _targetSystem = JsonRead.Text(payload, "Name");
                    _targetClass = JsonRead.Text(payload, "StarClass");
                    _remaining = payload.TryGetProperty("RemainingJumpsInRoute", out _) ? JsonRead.Count(payload, "RemainingJumpsInRoute") : null;
                    return true;
                case "StartJump":
                    _inHyperspace = string.Equals(JsonRead.Text(payload, "JumpType"), "Hyperspace", StringComparison.OrdinalIgnoreCase);
                    if (_inHyperspace)
                    {
                        // The jump under way has a destination whatever was targeted; it is the target until arrival.
                        _targetSystem = JsonRead.Text(payload, "StarSystem") ?? _targetSystem;
                        _targetClass = JsonRead.Text(payload, "StarClass") ?? _targetClass;
                    }

                    return _inHyperspace;
                case "NavRouteClear":
                    // The game clears the route in the middle of a route's last jump. The ship is still going
                    // somewhere, so the destination stands until arrival; a clear outside a jump forgets it.
                    if (!_inHyperspace)
                    {
                        _targetSystem = null;
                        _targetClass = null;
                    }

                    _remaining = null;
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>Copies the current state.</summary>
    /// <returns>A snapshot no later <see cref="Apply"/> changes.</returns>
    public RouteProgress Snapshot()
    {
        lock (_gate)
        {
            return new RouteProgress(
                _currentSystem,
                _currentAddress,
                _currentPosition,
                _targetSystem,
                _targetClass,
                _remaining,
                _inHyperspace,
                new Dictionary<long, VisitedSystem>(_known),
                [.. _recentJumps],
                _arrivedAt,
                _supercharged);
        }
    }

    /// <summary>Forgets the recent jumps when the loadout shows a different frame shift drive, or the same drive engineered differently.</summary>
    private bool ReadDrive(JsonElement payload)
    {
        var drive = JsonRead.Items(payload, "Modules")
            .Where(module => JsonRead.Text(module, "Item")?.StartsWith("int_hyperdrive", StringComparison.OrdinalIgnoreCase) == true)
            .Select(module => module.GetRawText())
            .FirstOrDefault();
        if (drive is null)
        {
            return false;
        }

        var changed = _drive is not null && !string.Equals(_drive, drive, StringComparison.Ordinal);
        _drive = drive;
        if (changed)
        {
            _recentJumps.Clear();
        }

        return changed;
    }

    /// <summary>Marks the system a survey event names, or the current one when it names none, as surveyed.</summary>
    private bool MarkSurveyed(JsonElement payload)
    {
        var address = JsonRead.Identifier(payload, "SystemAddress");
        if (address == 0)
        {
            address = _currentAddress;
        }

        if (address == 0 || !_known.TryGetValue(address, out var known) || known.Surveyed)
        {
            return false;
        }

        _known[address] = known with { Surveyed = true };
        return true;
    }

    private void RecordJump(JsonElement payload, DateTimeOffset timestamp)
    {
        var distance = JsonRead.Measure(payload, "JumpDist");
        var fuel = JsonRead.Measure(payload, "FuelUsed");
        if (distance is not > 0 || fuel is not > 0)
        {
            return;
        }

        var boost = JsonRead.Measure(payload, "BoostUsed") is > 1 and var used ? used : 1;
        _recentJumps.Insert(0, new JumpRecord(timestamp, distance.Value, fuel.Value, boost));
        if (_recentJumps.Count > RecentJumpCount)
        {
            _recentJumps.RemoveAt(_recentJumps.Count - 1);
        }
    }

    private void Arrive(JsonElement payload, DateTimeOffset timestamp, bool byJump)
    {
        _currentSystem = JsonRead.Text(payload, "StarSystem");
        _currentAddress = JsonRead.Identifier(payload, "SystemAddress");
        _currentPosition = ReadStarPos(payload);
        _arrivedAt = timestamp;
        _inHyperspace = false;
        if (byJump)
        {
            _supercharged = false;
        }

        if (_currentAddress != 0 && _currentSystem is not null)
        {
            var visits = _known.TryGetValue(_currentAddress, out var known) ? known.Visits + 1 : 1;
            _known[_currentAddress] = new VisitedSystem(_currentSystem, _currentAddress, _currentPosition ?? known?.Position, timestamp, visits, known?.Surveyed ?? false);
        }

        if (byJump && string.Equals(_currentSystem, _targetSystem, StringComparison.OrdinalIgnoreCase))
        {
            _targetSystem = null;
            _targetClass = null;
            _remaining = _remaining is > 0 ? _remaining : null;
        }
    }
}
