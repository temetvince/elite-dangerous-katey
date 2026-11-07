using System.Text.Json;

namespace Katey.Journal;

/// <summary>Engineering on a module, as the journal reports it.</summary>
/// <param name="Blueprint">Blueprint journal name such as <c>FSD_LongRange</c>.</param>
/// <param name="Level">Grade 1–5.</param>
/// <param name="Experimental">Experimental effect journal name, <see langword="null"/> for none, or <see langword="null"/> with <paramref name="ExperimentalKnown"/> false when the source does not say.</param>
/// <param name="ExperimentalKnown">Whether the source reports experimental effects at all. <c>Loadout</c> does; <c>StoredModules</c> does not.</param>
/// <param name="Quality">Progress within the grade, 0 to 1, as the game's <c>Quality</c> field gives it, or 1 when the source has none.</param>
public sealed record JournalEngineering(string Blueprint, int Level, string? Experimental, bool ExperimentalKnown, double Quality = 1);

/// <summary>One module fitted to a ship.</summary>
/// <param name="Slot">Slot name such as <c>FrameShiftDrive</c>.</param>
/// <param name="Item">Item symbol in canonical form: lower case, without the <c>$</c> and <c>_name;</c> wrapper.</param>
/// <param name="Engineering">Engineering, or <see langword="null"/> for a stock module.</param>
/// <param name="Modifiers">The engineering's modified statistics by label, such as <c>MaxFuelPerJump</c>, as <c>Loadout</c> reports them. Empty for a stock module.</param>
/// <param name="IsPrebuilt">Whether it is one the ship came with when redeemed from a bundle and still in the slot it came in. A slot bought into, retrieved into, swapped, stored or sold since holds something else, however the game values it.</param>
public sealed record FittedModule(string Slot, string Item, JournalEngineering? Engineering, IReadOnlyDictionary<string, double> Modifiers, bool IsPrebuilt = false);

/// <summary>One ship the player owns.</summary>
/// <param name="ShipId">The game's ship id.</param>
/// <param name="Type">Ship type symbol, lower case, such as <c>anaconda</c>.</param>
/// <param name="TypeName">Localised ship type.</param>
/// <param name="Name">The player's name for it, or empty.</param>
/// <param name="Ident">The player's ident for it, or empty.</param>
/// <param name="System">System it is in, or <see langword="null"/> when unknown.</param>
/// <param name="Station">Station it is stored at, or <see langword="null"/> when it is the ship being flown or unknown.</param>
/// <param name="IsCurrent">Whether the player is flying it.</param>
/// <param name="InTransit">Whether it is being transferred.</param>
/// <param name="Modules">Its last-known fit, or empty when the player has never flown it since the journal began.</param>
/// <param name="FitAsOf">When the fit was last seen, or <see langword="null"/>.</param>
/// <param name="FuelCapacity">The main tank's size in tonnes, or <see langword="null"/> before a <c>Loadout</c>.</param>
/// <param name="LoadoutJson">The last <c>Loadout</c> event for this ship, exactly as the game wrote it, or <see langword="null"/> before one. Engineering applied since that event is in <paramref name="Modules"/> but not here, until the game writes the next <c>Loadout</c>.</param>
/// <param name="UnladenMass">The hull and modules without fuel or cargo in tonnes, or <see langword="null"/> before a <c>Loadout</c>.</param>
/// <param name="ReserveCapacity">The reserve tank's size in tonnes, or <see langword="null"/> before a <c>Loadout</c>.</param>
/// <param name="CargoCapacity">Tonnes of cargo the ship can carry, or <see langword="null"/> before a <c>Loadout</c>.</param>
/// <param name="MaxJumpRange">The longest jump the game reported in light years, or <see langword="null"/> before a <c>Loadout</c>.</param>
/// <param name="IsPrebuilt">Whether the journal saw it redeemed from a bundle. False when the redemption predates the journal history.</param>
/// <param name="RedeemedModules">The fit it had when redeemed, from the first <c>Loadout</c> after <c>ShipRedeemed</c>, or <see langword="null"/> when not prebuilt or not yet flown.</param>
public sealed record OwnedShip(
    int ShipId,
    string Type,
    string TypeName,
    string Name,
    string Ident,
    string? System,
    string? Station,
    bool IsCurrent,
    bool InTransit,
    IReadOnlyList<FittedModule> Modules,
    DateTimeOffset? FitAsOf,
    double? FuelCapacity,
    string? LoadoutJson,
    double? UnladenMass = null,
    double? ReserveCapacity = null,
    double? CargoCapacity = null,
    double? MaxJumpRange = null,
    bool IsPrebuilt = false,
    IReadOnlyList<FittedModule>? RedeemedModules = null);

/// <summary>One module in station storage.</summary>
/// <param name="Item">Item symbol in canonical form: lower case, without the <c>$</c> and <c>_name;</c> wrapper.</param>
/// <param name="Name">Localised name, or the symbol.</param>
/// <param name="System">System it is stored in, or <see langword="null"/> while in transit.</param>
/// <param name="MarketId">Station market id, or 0.</param>
/// <param name="Engineering">Engineering, or <see langword="null"/> for a stock module.</param>
/// <param name="Hot">Whether it is flagged hot.</param>
/// <param name="InTransit">Whether it is being transferred.</param>
public sealed record StoredModule(string Item, string Name, string? System, long MarketId, JournalEngineering? Engineering, bool Hot, bool InTransit);

/// <summary>Everything the journal says the player owns.</summary>
/// <param name="CurrentShipId">The ship being flown, or <see langword="null"/>.</param>
/// <param name="CurrentSystem">Where the player is, or <see langword="null"/>.</param>
/// <param name="CurrentStation">The station the player is docked at, or <see langword="null"/>.</param>
/// <param name="Ships">Every owned ship, current first then by name.</param>
/// <param name="StoredModules">Every stored module.</param>
/// <param name="ShipsAsOf">When the shipyard last listed the fleet, or <see langword="null"/>.</param>
/// <param name="ModulesAsOf">When outfitting last listed stored modules, or <see langword="null"/>.</param>
public sealed record FleetSnapshot(
    int? CurrentShipId,
    string? CurrentSystem,
    string? CurrentStation,
    IReadOnlyList<OwnedShip> Ships,
    IReadOnlyList<StoredModule> StoredModules,
    DateTimeOffset? ShipsAsOf,
    DateTimeOffset? ModulesAsOf)
{
    /// <summary>Gets nothing known yet.</summary>
    public static FleetSnapshot Empty { get; } = new(null, null, null, [], [], null, null);
}

/// <summary>
/// Follows the player's ships and stored modules. Fits come from <c>Loadout</c>, the fleet list from
/// <c>StoredShips</c>, and stored modules from <c>StoredModules</c>, each refreshed whenever the game
/// writes them; purchases, sales, swaps and renames are applied in between. Item symbols are read through
/// <see cref="JsonRead.Symbol"/>, so a module spelt <c>$int_cargorack_size3_class1_name;</c> by one event
/// and <c>Int_CargoRack_Size3_Class1</c> by another is the same module.
/// </summary>
public sealed class FleetTracker : IJournalSink
{
    private static readonly HashSet<string> Events = new(StringComparer.Ordinal)
    {
        "LoadGame", "Loadout", "StoredShips", "StoredModules", "ShipyardSwap", "ShipyardNew", "ShipyardBuy", "ShipRedeemed",
        "ShipyardSell", "SetUserShipName", "Docked", "Undocked", "Location", "FSDJump", "CarrierJump",
        "ModuleStore", "ModuleRetrieve", "ModuleSellRemote", "ModuleBuy", "ModuleSell", "ModuleSwap", "MassModuleStore",
        "FetchRemoteModule", "ShipyardTransfer", "ModuleBuyAndStore", "EngineerCraft",
    };

    private readonly Lock _gate = new();
    private readonly Dictionary<int, MutableShip> _ships = [];
    private readonly List<StoredModule> _stored = [];
    private readonly List<(string Item, long MarketId, JournalEngineering Engineering)> _learnedEffects = [];
    private int? _currentShipId;
    private string? _currentSystem;
    private string? _currentStation;
    private DateTimeOffset? _shipsAsOf;
    private DateTimeOffset? _modulesAsOf;

    /// <inheritdoc/>
    public IReadOnlySet<string>? InterestedEvents => Events;

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_gate)
        {
            _ships.Clear();
            _stored.Clear();
            _learnedEffects.Clear();
            _currentShipId = null;
            _currentSystem = null;
            _currentStation = null;
            _shipsAsOf = null;
            _modulesAsOf = null;
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
                case "LoadGame":
                    return ApplyCurrentShip(payload, journalEvent.Timestamp, withModules: false);
                case "Loadout":
                    return ApplyCurrentShip(payload, journalEvent.Timestamp, withModules: true);
                case "StoredShips":
                    return ApplyStoredShips(payload, journalEvent.Timestamp);
                case "StoredModules":
                    return ApplyStoredModules(payload, journalEvent.Timestamp);
                case "ShipyardSwap":
                    return ApplySwap(payload);
                case "ShipyardNew":
                    return ApplyNewShip(payload);
                case "ShipRedeemed":
                    return ApplyRedeemed(payload);
                case "ShipyardBuy":
                    return false;
                case "ShipyardSell":
                    return _ships.Remove(JsonRead.Count(payload, "SellShipID"));
                case "SetUserShipName":
                    return ApplyRename(payload);
                case "Docked":
                    _currentSystem = JsonRead.Text(payload, "StarSystem") ?? _currentSystem;
                    _currentStation = JsonRead.Text(payload, "StationName");
                    return true;
                case "Undocked":
                    _currentStation = null;
                    return true;
                case "Location":
                    _currentSystem = JsonRead.Text(payload, "StarSystem") ?? _currentSystem;
                    _currentStation = JsonRead.Flag(payload, "Docked") ? JsonRead.Text(payload, "StationName") : null;
                    return true;
                case "FSDJump":
                case "CarrierJump":
                    _currentSystem = JsonRead.Text(payload, "StarSystem") ?? _currentSystem;
                    _currentStation = null;
                    return true;
                case "ModuleStore":
                    return StoreFromShip(payload, "StoredItem") | ForgetPrebuilt(payload, "Slot");
                case "ModuleRetrieve":
                    return RemoveStored(JsonRead.Symbol(payload, "RetrievedItem"), JsonRead.Identifier(payload, "MarketID")) | StoreFromShip(payload, "SwapOutItem") | ForgetPrebuilt(payload, "Slot");
                case "ModuleSellRemote":
                    return RemoveStored(JsonRead.Symbol(payload, "SellItem"), 0);
                case "ModuleBuy":
                    return StoreFromShip(payload, "StoredItem") | ForgetPrebuilt(payload, "Slot");
                case "ModuleSell":
                    return ForgetPrebuilt(payload, "Slot");
                case "MassModuleStore":
                    return ApplyMassStore(payload);
                case "ModuleBuyAndStore":
                    return ApplyBuyAndStore(payload);
                case "FetchRemoteModule":
                    return ApplyFetch(payload);
                case "ShipyardTransfer":
                    return ApplyTransfer(payload);
                case "ModuleSwap":
                    return SwapPrebuilt(payload);
                case "EngineerCraft":
                    return ApplyEngineerCraft(payload, journalEvent.Timestamp);
                default:
                    return false;
            }
        }
    }

    /// <summary>Copies the current state.</summary>
    /// <returns>A snapshot no later <see cref="Apply"/> changes.</returns>
    public FleetSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new FleetSnapshot(
                _currentShipId,
                _currentSystem,
                _currentStation,
                [
                    .. _ships.Values
                        .OrderByDescending(ship => ship.ShipId == _currentShipId)
                        .ThenBy(ship => ship.Name, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(ship => ship.ShipId)
                        .Select(ship => ship.ToRecord(ship.ShipId == _currentShipId, _currentSystem, _currentStation)),
                ],
                [.. _stored],
                _shipsAsOf,
                _modulesAsOf);
        }
    }

    /// <summary>
    /// Applies an <c>EngineerCraft</c> to the module in the slot it names on the ship being flown. The game writes
    /// no <c>Loadout</c> after engineering, so this is the only word of a new blueprint, grade or experimental
    /// effect until the next login or ship swap. The event carries the module's whole engineering block at its
    /// top level, in the shape <c>Loadout</c> nests under <c>Engineering</c>.
    /// </summary>
    /// <returns>Whether a fitted module changed. Nothing changes before the ship's first <c>Loadout</c>, since there is no fit to amend.</returns>
    private bool ApplyEngineerCraft(JsonElement payload, DateTimeOffset timestamp)
    {
        if (_currentShipId is not { } shipId || !_ships.TryGetValue(shipId, out var ship) || ship.Modules.Count == 0)
        {
            return false;
        }

        var slot = JsonRead.Text(payload, "Slot");
        var item = JsonRead.Symbol(payload, "Module");
        if (slot is null || item is null || ReadEngineering(payload) is not { } engineering)
        {
            return false;
        }

        var index = ship.Modules.FindIndex(module => string.Equals(module.Slot, slot, StringComparison.OrdinalIgnoreCase));
        var existing = index >= 0 ? ship.Modules[index] : null;
        var crafted = new FittedModule(slot, item, engineering, ReadModifiers(payload), existing?.IsPrebuilt ?? false);
        if (index >= 0)
        {
            ship.Modules[index] = crafted;
        }
        else
        {
            ship.Modules.Add(crafted);
        }

        ship.FitAsOf = timestamp;
        return true;
    }

    /// <summary>Reads an engineering block: the one <c>Loadout</c> nests under a module, or an <c>EngineerCraft</c> event itself.</summary>
    private static JournalEngineering? ReadEngineering(JsonElement? block)
    {
        if (block is not { } engineering)
        {
            return null;
        }

        var blueprint = JsonRead.Text(engineering, "BlueprintName");
        return blueprint is null ? null : new JournalEngineering(blueprint, JsonRead.Count(engineering, "Level"), JsonRead.Text(engineering, "ExperimentalEffect"), ExperimentalKnown: true, ReadQuality(engineering));
    }

    /// <summary>Reads the modified statistics of an engineering block by label. Empty without a block.</summary>
    private static Dictionary<string, double> ReadModifiers(JsonElement? block)
    {
        var modifiers = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (block is { } engineering)
        {
            foreach (var modifier in JsonRead.Items(engineering, "Modifiers"))
            {
                if (JsonRead.Text(modifier, "Label") is { Length: > 0 } label && JsonRead.Measure(modifier, "Value") is { } value)
                {
                    modifiers[label] = value;
                }
            }
        }

        return modifiers;
    }

    private static JournalEngineering? ReadStoredEngineering(JsonElement item)
    {
        var blueprint = JsonRead.Text(item, "EngineerModifications");
        return string.IsNullOrEmpty(blueprint) ? null : new JournalEngineering(blueprint, JsonRead.Count(item, "Level"), null, ExperimentalKnown: false, ReadQuality(item));
    }

    /// <summary>The game's <c>Quality</c> figure, progress within the grade, clamped to 0–1; 1 when absent.</summary>
    private static double ReadQuality(JsonElement element) => JsonRead.Measure(element, "Quality") is { } quality ? Math.Clamp(quality, 0, 1) : 1;

    private MutableShip ShipFor(int shipId, string? type)
    {
        if (!_ships.TryGetValue(shipId, out var ship))
        {
            ship = new MutableShip { ShipId = shipId };
            _ships[shipId] = ship;
        }

        if (!string.IsNullOrEmpty(type))
        {
            ship.Type = type.ToUpperInvariant().ToLowerInvariant();
        }

        return ship;
    }

    private bool ApplyCurrentShip(JsonElement payload, DateTimeOffset timestamp, bool withModules)
    {
        if (!payload.TryGetProperty("ShipID", out _) || JsonRead.Text(payload, "Ship") is not { } type)
        {
            return false;
        }

        var shipId = JsonRead.Count(payload, "ShipID");
        var ship = ShipFor(shipId, type);
        ship.TypeName = JsonRead.Localised(payload, "Ship") ?? ship.TypeName;
        ship.Name = JsonRead.Text(payload, "ShipName") ?? ship.Name;
        ship.Ident = JsonRead.Text(payload, "ShipIdent") ?? ship.Ident;
        ship.System = null;
        ship.Station = null;
        ship.InTransit = false;
        _currentShipId = shipId;
        if (withModules)
        {
            // The first Loadout after a redemption shows the bundle: the game lists what came with the ship without a value. From then on a slot
            // stays prebuilt while it holds that same item, still without a value, and no outfitting event has touched it. A value proves a bought
            // module; a missing value alone proves nothing, since the game also omits it for modules worth nothing, such as a surface scanner.
            var firstAfterRedemption = ship.Redeemed && ship.RedeemedModules is null;
            var modules = new List<FittedModule>();
            foreach (var module in JsonRead.Items(payload, "Modules"))
            {
                if (JsonRead.Text(module, "Slot") is not { } slot || JsonRead.Symbol(module, "Item") is not { } item)
                {
                    continue;
                }

                var valueless = !module.TryGetProperty("Value", out _);
                var prebuilt = valueless && (firstAfterRedemption || (ship.PrebuiltSlots.TryGetValue(slot, out var came) && string.Equals(came, item, StringComparison.Ordinal)));
                if (!prebuilt && !firstAfterRedemption)
                {
                    ship.PrebuiltSlots.Remove(slot);
                }

                modules.Add(new FittedModule(slot, item, ReadEngineering(JsonRead.Child(module, "Engineering")), ReadModifiers(JsonRead.Child(module, "Engineering")), prebuilt));
            }

            ship.Modules = modules;
            if (firstAfterRedemption)
            {
                ship.RedeemedModules = [.. modules];
                ship.PrebuiltSlots.Clear();
                foreach (var module in modules.Where(module => module.IsPrebuilt))
                {
                    ship.PrebuiltSlots[module.Slot] = module.Item;
                }
            }

            ship.FitAsOf = timestamp;
            if (JsonRead.Child(payload, "FuelCapacity") is { } tank)
            {
                ship.FuelCapacity = JsonRead.Measure(tank, "Main") ?? ship.FuelCapacity;
                ship.ReserveCapacity = JsonRead.Measure(tank, "Reserve") ?? ship.ReserveCapacity;
            }

            ship.UnladenMass = JsonRead.Measure(payload, "UnladenMass") ?? ship.UnladenMass;
            ship.CargoCapacity = JsonRead.Measure(payload, "CargoCapacity") ?? ship.CargoCapacity;
            ship.MaxJumpRange = JsonRead.Measure(payload, "MaxJumpRange") ?? ship.MaxJumpRange;
            ship.LoadoutJson = payload.GetRawText();
        }

        return true;
    }

    /// <summary>Marks a ship redeemed from a bundle, so the modules it came with can be told apart from ones bought later.</summary>
    private bool ApplyRedeemed(JsonElement payload)
    {
        if (!payload.TryGetProperty("NewShipID", out _))
        {
            return false;
        }

        ShipFor(JsonRead.Count(payload, "NewShipID"), JsonRead.Text(payload, "ShipType")).Redeemed = true;
        return true;
    }

    private bool ApplyStoredShips(JsonElement payload, DateTimeOffset timestamp)
    {
        var hereSystem = JsonRead.Text(payload, "StarSystem");
        var hereStation = JsonRead.Text(payload, "StationName");
        var seen = new HashSet<int>();
        foreach (var entry in JsonRead.Items(payload, "ShipsHere"))
        {
            var ship = ShipFor(JsonRead.Count(entry, "ShipID"), JsonRead.Text(entry, "ShipType"));
            seen.Add(ship.ShipId);
            ship.TypeName = JsonRead.Localised(entry, "ShipType") ?? ship.TypeName;
            ship.Name = JsonRead.Text(entry, "Name") ?? ship.Name;
            ship.System = hereSystem;
            ship.Station = hereStation;
            ship.InTransit = false;
        }

        foreach (var entry in JsonRead.Items(payload, "ShipsRemote"))
        {
            var ship = ShipFor(JsonRead.Count(entry, "ShipID"), JsonRead.Text(entry, "ShipType"));
            seen.Add(ship.ShipId);
            ship.TypeName = JsonRead.Localised(entry, "ShipType") ?? ship.TypeName;
            ship.Name = JsonRead.Text(entry, "Name") ?? ship.Name;
            ship.System = JsonRead.Text(entry, "StarSystem");
            ship.Station = null;
            ship.InTransit = JsonRead.Flag(entry, "InTransit");
        }

        if (_currentShipId is { } current)
        {
            seen.Add(current);
        }

        foreach (var gone in _ships.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            _ships.Remove(gone);
        }

        _shipsAsOf = timestamp;
        return true;
    }

    private bool ApplyStoredModules(JsonElement payload, DateTimeOffset timestamp)
    {
        _stored.Clear();
        foreach (var item in JsonRead.Items(payload, "Items"))
        {
            var symbol = JsonRead.Symbol(item, "Name");
            if (symbol is null)
            {
                continue;
            }

            var marketId = JsonRead.Identifier(item, "MarketID");
            _stored.Add(new StoredModule(
                symbol,
                JsonRead.Localised(item, "Name") ?? symbol,
                JsonRead.Text(item, "StarSystem"),
                marketId,
                Recall(symbol, marketId, ReadStoredEngineering(item)),
                JsonRead.Flag(item, "Hot"),
                JsonRead.Flag(item, "InTransit")));
        }

        _modulesAsOf = timestamp;
        return true;
    }

    private bool ApplySwap(JsonElement payload)
    {
        var newId = JsonRead.Count(payload, "ShipID");
        var ship = ShipFor(newId, JsonRead.Text(payload, "ShipType"));
        ship.System = null;
        ship.Station = null;
        if (_currentShipId is { } oldId && _ships.TryGetValue(oldId, out var old) && oldId != newId)
        {
            if (payload.TryGetProperty("StoreShipID", out _))
            {
                old.System = _currentSystem;
                old.Station = _currentStation;
            }
            else
            {
                _ships.Remove(oldId);
            }
        }

        _currentShipId = newId;
        return true;
    }

    private bool ApplyNewShip(JsonElement payload)
    {
        var ship = ShipFor(JsonRead.Count(payload, "NewShipID"), JsonRead.Text(payload, "ShipType"));
        ship.TypeName = JsonRead.Localised(payload, "ShipType") ?? ship.TypeName;
        ship.Modules = [];
        ship.FitAsOf = null;
        return true;
    }

    private bool ApplyRename(JsonElement payload)
    {
        var ship = ShipFor(JsonRead.Count(payload, "ShipID"), JsonRead.Text(payload, "Ship"));
        ship.Name = JsonRead.Text(payload, "UserShipName") ?? ship.Name;
        ship.Ident = JsonRead.Text(payload, "UserShipId") ?? ship.Ident;
        return true;
    }

    /// <summary>The ship an outfitting event names by <c>ShipID</c>, or the ship being flown when it names none.</summary>
    private MutableShip? ShipOf(JsonElement payload)
    {
        return payload.TryGetProperty("ShipID", out _) && _ships.TryGetValue(JsonRead.Count(payload, "ShipID"), out var named)
            ? named
            : _currentShipId is { } id && _ships.TryGetValue(id, out var current) ? current : null;
    }

    /// <summary>An outfitting event touched a slot, so whatever it holds now did not come with the ship.</summary>
    private bool ForgetPrebuilt(JsonElement payload, string slotProperty)
        => ShipOf(payload) is { } ship && JsonRead.Text(payload, slotProperty) is { } slot && ship.PrebuiltSlots.Remove(slot);

    /// <summary>A <c>ModuleSwap</c> moves two slots' modules past each other; the prebuilt marks move with them.</summary>
    private bool SwapPrebuilt(JsonElement payload)
    {
        if (ShipOf(payload) is not { } ship || JsonRead.Text(payload, "FromSlot") is not { } from || JsonRead.Text(payload, "ToSlot") is not { } to)
        {
            return false;
        }

        var fromCame = ship.PrebuiltSlots.GetValueOrDefault(from);
        var toCame = ship.PrebuiltSlots.GetValueOrDefault(to);
        if (fromCame is null && toCame is null)
        {
            return false;
        }

        ship.PrebuiltSlots.Remove(from);
        ship.PrebuiltSlots.Remove(to);
        if (fromCame is not null)
        {
            ship.PrebuiltSlots[to] = fromCame;
        }

        if (toCame is not null)
        {
            ship.PrebuiltSlots[from] = toCame;
        }

        return true;
    }

    /// <summary>
    /// A module leaving a ship's slot for the station's storage, named by an outfitting event: the one
    /// <c>ModuleStore</c> stores, the one <c>ModuleBuy</c> puts away when a bought module takes its slot,
    /// or the one <c>ModuleRetrieve</c> swaps out. The game writes <c>StoredModules</c> again only when the
    /// storage screen is opened, so without this the module would be missing from storage until then. Its
    /// engineering is taken from the ship's record of the slot, which knows the experimental effect, and
    /// falls back to what the event itself says.
    /// </summary>
    /// <returns>Whether a module was added to storage.</returns>
    private bool StoreFromShip(JsonElement payload, string itemProperty)
    {
        var symbol = JsonRead.Symbol(payload, itemProperty);
        if (symbol is null)
        {
            return false;
        }

        var slot = JsonRead.Text(payload, "Slot");
        var fitted = slot is null ? null : ShipOf(payload)?.Modules.FirstOrDefault(module => string.Equals(module.Slot, slot, StringComparison.OrdinalIgnoreCase) && string.Equals(module.Item, symbol, StringComparison.Ordinal));
        var blueprint = JsonRead.Text(payload, "EngineerModifications");
        var marketId = JsonRead.Identifier(payload, "MarketID");
        Learn(symbol, marketId, fitted?.Engineering);
        _stored.Add(new StoredModule(
            symbol,
            JsonRead.Localised(payload, itemProperty) ?? symbol,
            _currentSystem,
            marketId,
            fitted?.Engineering ?? (string.IsNullOrEmpty(blueprint) ? null : new JournalEngineering(blueprint, JsonRead.Count(payload, "Level"), null, ExperimentalKnown: false, ReadQuality(payload))),
            JsonRead.Flag(payload, "Hot"),
            InTransit: false));
        return true;
    }

    /// <summary>
    /// The engineering a storage snapshot gives a module, completed from what was learned when the module
    /// left a ship: the snapshot records blueprint and grade but never the experimental effect, so a stored
    /// module whose blueprint and grade match one seen leaving a ship takes that module's effect. The same
    /// market is preferred; a module fetched from elsewhere matches by item, blueprint and grade alone.
    /// </summary>
    private JournalEngineering? Recall(string item, long marketId, JournalEngineering? fromSnapshot)
    {
        if (fromSnapshot is null || fromSnapshot.ExperimentalKnown)
        {
            return fromSnapshot;
        }

        bool Same((string Item, long MarketId, JournalEngineering Engineering) learned)
            => learned.Item == item
                && string.Equals(learned.Engineering.Blueprint, fromSnapshot.Blueprint, StringComparison.OrdinalIgnoreCase)
                && learned.Engineering.Level == fromSnapshot.Level;
        var match = _learnedEffects.FirstOrDefault(learned => Same(learned) && learned.MarketId == marketId);
        if (match.Engineering is null)
        {
            match = _learnedEffects.FirstOrDefault(Same);
        }

        return match.Engineering ?? fromSnapshot;
    }

    /// <summary>Remembers the engineering a module carried as it left a ship for storage, for the storage snapshots that cannot say.</summary>
    private void Learn(string item, long marketId, JournalEngineering? engineering)
    {
        if (engineering is { ExperimentalKnown: true })
        {
            _learnedEffects.Add((item, marketId, engineering));
        }
    }

    /// <summary>Every module a <c>MassModuleStore</c> moves off the ship goes to the station's storage, each with the engineering the ship's record of its slot carries.</summary>
    private bool ApplyMassStore(JsonElement payload)
    {
        var ship = ShipOf(payload);
        var marketId = JsonRead.Identifier(payload, "MarketID");
        var changed = false;
        foreach (var item in JsonRead.Items(payload, "Items"))
        {
            var symbol = JsonRead.Symbol(item, "Name");
            if (symbol is null)
            {
                continue;
            }

            var slot = JsonRead.Text(item, "Slot");
            var fitted = slot is null ? null : ship?.Modules.FirstOrDefault(module => string.Equals(module.Slot, slot, StringComparison.OrdinalIgnoreCase) && string.Equals(module.Item, symbol, StringComparison.Ordinal));
            Learn(symbol, marketId, fitted?.Engineering);
            _stored.Add(new StoredModule(symbol, JsonRead.Localised(item, "Name") ?? symbol, _currentSystem, marketId, fitted?.Engineering ?? ReadStoredEngineering(item), JsonRead.Flag(item, "Hot"), InTransit: false));
            if (slot is not null)
            {
                ship?.PrebuiltSlots.Remove(slot);
            }

            changed = true;
        }

        return changed;
    }

    /// <summary>A module bought straight into the station's storage, never fitted: stock, here, at this market.</summary>
    private bool ApplyBuyAndStore(JsonElement payload)
    {
        if (JsonRead.Symbol(payload, "BuyItem") is not { } symbol)
        {
            return false;
        }

        _stored.Add(new StoredModule(symbol, JsonRead.Localised(payload, "BuyItem") ?? symbol, _currentSystem, JsonRead.Identifier(payload, "MarketID"), null, Hot: false, InTransit: false));
        return true;
    }

    /// <summary>A stored module sent for from another station is on its way here: in transit, to this system, with no market yet.</summary>
    private bool ApplyFetch(JsonElement payload)
    {
        var symbol = JsonRead.Symbol(payload, "StoredItem");
        if (symbol is null)
        {
            return false;
        }

        var index = _stored.FindIndex(module => module.Item == symbol && !module.InTransit && !string.Equals(module.System, _currentSystem, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            index = _stored.FindIndex(module => module.Item == symbol && !module.InTransit);
        }

        if (index < 0)
        {
            return false;
        }

        _stored[index] = _stored[index] with { System = _currentSystem, MarketId = 0, InTransit = true };
        return true;
    }

    /// <summary>A ship sent for from another station is on its way here: in transit, to this system and station.</summary>
    private bool ApplyTransfer(JsonElement payload)
    {
        if (JsonRead.Text(payload, "ShipType") is not { } type)
        {
            return false;
        }

        var ship = ShipFor(JsonRead.Count(payload, "ShipID"), type);
        ship.System = _currentSystem;
        ship.Station = _currentStation;
        ship.InTransit = true;
        return true;
    }

    private bool RemoveStored(string? symbol, long marketId)
    {
        if (symbol is null)
        {
            return false;
        }

        var index = _stored.FindIndex(module => module.Item == symbol && (marketId == 0 || module.MarketId == marketId));
        if (index < 0)
        {
            index = _stored.FindIndex(module => module.Item == symbol);
        }

        if (index < 0)
        {
            return false;
        }

        _stored.RemoveAt(index);
        return true;
    }

    private sealed class MutableShip
    {
        public int ShipId { get; init; }

        public string Type { get; set; } = "";

        public string TypeName { get; set; } = "";

        public string Name { get; set; } = "";

        public string Ident { get; set; } = "";

        public string? System { get; set; }

        public string? Station { get; set; }

        public bool InTransit { get; set; }

        public List<FittedModule> Modules { get; set; } = [];

        public DateTimeOffset? FitAsOf { get; set; }

        public double? FuelCapacity { get; set; }

        public string? LoadoutJson { get; set; }

        public double? UnladenMass { get; set; }

        public double? ReserveCapacity { get; set; }

        public double? CargoCapacity { get; set; }

        public double? MaxJumpRange { get; set; }

        public bool Redeemed { get; set; }

        public List<FittedModule>? RedeemedModules { get; set; }

        public Dictionary<string, string> PrebuiltSlots { get; } = new(StringComparer.OrdinalIgnoreCase);

        public OwnedShip ToRecord(bool isCurrent, string? currentSystem, string? currentStation) => new(
            ShipId,
            Type,
            TypeName.Length > 0 ? TypeName : Type,
            Name,
            Ident,
            isCurrent ? currentSystem : System,
            isCurrent ? currentStation : Station,
            isCurrent,
            InTransit,
            [.. Modules],
            FitAsOf,
            FuelCapacity,
            LoadoutJson,
            UnladenMass,
            ReserveCapacity,
            CargoCapacity,
            MaxJumpRange,
            Redeemed,
            RedeemedModules is null ? null : [.. RedeemedModules]);
    }
}
