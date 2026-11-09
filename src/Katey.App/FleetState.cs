using System.Globalization;
using Katey.Core.Edsy;
using Katey.Core.Fleet;
using Katey.Core.Routing;
using Katey.Journal;

namespace Katey.App;

/// <summary>Which owned modules a list shows.</summary>
public enum EngineeringFilter
{
    /// <summary>Every module.</summary>
    All,

    /// <summary>Only modules with no engineering.</summary>
    UnengineeredOnly,

    /// <summary>Only modules carrying engineering.</summary>
    EngineeredOnly,
}

/// <summary>Where an owned module may be to count in a list.</summary>
public enum PlaceFilter
{
    /// <summary>Fitted to a ship or in storage.</summary>
    Anywhere,

    /// <summary>Only modules in station storage, not fitted to any ship.</summary>
    StoredOnly,

    /// <summary>Only modules fitted to a ship.</summary>
    FittedOnly,
}

/// <summary>
/// The saved fits and how they are checked against what the player owns: the sharing policy, the
/// location filter, the engineering filter and the place filter.
/// </summary>
/// <remarks>
/// Not thread-safe. Raises <see cref="Changed"/> after a fit or the policy changes, so the state can be
/// saved; the filters are remembered through <see cref="UiPreferences"/>.
/// </remarks>
public sealed class FleetState
{
    private const string FilterKey = "fleet.engineering";
    private const string PlaceKey = "fleet.place";
    private const string LocationKey = "fleet.location";

    private readonly List<Fitting> _fittings;
    private readonly UiPreferences _preferences;
    private readonly Dictionary<string, JumpProfile?> _profiles = new(StringComparer.Ordinal);

    /// <summary>Creates the state.</summary>
    /// <param name="fittings">Saved fits in priority order.</param>
    /// <param name="policy">The sharing policy.</param>
    /// <param name="preferences">Where the filters are remembered.</param>
    public FleetState(IEnumerable<Fitting> fittings, SharingPolicy policy, UiPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(fittings);
        ArgumentNullException.ThrowIfNull(preferences);
        _fittings = [.. fittings];
        _preferences = preferences;
        Policy = policy;
        Filter = Enum.TryParse<EngineeringFilter>(preferences.Get(FilterKey), out var filter) ? filter : EngineeringFilter.All;
        Place = Enum.TryParse<PlaceFilter>(preferences.Get(PlaceKey), out var place) ? place : PlaceFilter.Anywhere;
        Location = preferences.Get(LocationKey) is { Length: > 0 } location ? location : null;
    }

    /// <summary>Raised after a fit is added or removed or the policy changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised after a filter changes.</summary>
    public event EventHandler? FilterChanged;

    /// <summary>Gets the saved fits in priority order.</summary>
    public IReadOnlyList<Fitting> Fittings => _fittings;

    /// <summary>Gets or sets how fits share owned modules.</summary>
    public SharingPolicy Policy
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Gets or sets the system owned modules must be in to count, or <see langword="null"/> for everywhere. Remembered.</summary>
    public string? Location
    {
        get;
        set
        {
            if (!string.Equals(field, value, StringComparison.OrdinalIgnoreCase))
            {
                field = value;
                _preferences.Set(LocationKey, value ?? "");
                FilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Gets or sets which unused modules are listed by engineering. Remembered.</summary>
    public EngineeringFilter Filter
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _preferences.Set(FilterKey, value.ToString());
                FilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Gets or sets which unused modules are listed by place. Remembered.</summary>
    public PlaceFilter Place
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _preferences.Set(PlaceKey, value.ToString());
                FilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Every owned module the journal knows, fitted or stored. Cosmetic items are left out.</summary>
    /// <param name="fleet">The fleet snapshot.</param>
    /// <returns>Installed modules first, ship by ship, then stored modules.</returns>
    public static IReadOnlyList<OwnedModule> OwnedModules(FleetSnapshot fleet)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        var owned = new List<OwnedModule>();
        foreach (var ship in fleet.Ships)
        {
            var holder = ship.Name.Length > 0 ? ship.Name : ShipNames.Prefer(ship.Type, ship.TypeName);
            foreach (var module in ship.Modules.Where(module => !ModuleCatalog.IsCosmetic(module.Item)))
            {
                owned.Add(new OwnedModule(
                    string.Create(CultureInfo.InvariantCulture, $"ship:{ship.ShipId}:{module.Slot}"),
                    module.Item,
                    Convert(module.Engineering),
                    ModulePlace.Installed,
                    ship.System,
                    holder,
                    ship.Type,
                    module.IsPrebuilt));
            }
        }

        for (var index = 0; index < fleet.StoredModules.Count; index++)
        {
            var module = fleet.StoredModules[index];
            if (ModuleCatalog.IsCosmetic(module.Item))
            {
                continue;
            }

            owned.Add(new OwnedModule(
                string.Create(CultureInfo.InvariantCulture, $"stored:{index}"),
                module.Item,
                Convert(module.Engineering),
                ModulePlace.Stored,
                module.System,
                module.InTransit ? "in transit" : module.System ?? "storage",
                null));
        }

        return owned;
    }

    /// <summary>A ship's fitted modules in the shape the fit comparer takes. Cosmetic items are left out.</summary>
    /// <param name="ship">The ship.</param>
    /// <returns>One module per slot, in journal order.</returns>
    public static IReadOnlyList<FitModule> FittedModules(OwnedShip ship)
    {
        ArgumentNullException.ThrowIfNull(ship);
        return [.. ship.Modules.Where(module => !ModuleCatalog.IsCosmetic(module.Item)).Select(module => new FitModule(module.Slot, module.Item, Convert(module.Engineering)))];
    }

    /// <summary>Checks every saved fit against the fleet under the current policy and location.</summary>
    /// <param name="fleet">The fleet snapshot.</param>
    /// <param name="farness">How far a module is from the player, lower nearer, so the nearer of two equal modules is named; <see langword="null"/> when distance should not count.</param>
    /// <returns>The report. Its unused list is not yet narrowed by <see cref="Filter"/>; see <see cref="Unused"/>.</returns>
    public MatchReport Match(FleetSnapshot fleet, Func<OwnedModule, double>? farness = null)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        var hulls = fleet.Ships.Select(ship => ship.Type).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return FitMatcher.Match(_fittings, OwnedModules(fleet), hulls, Policy, Location, farness);
    }

    /// <summary>The unused modules of a report, narrowed by <see cref="Filter"/> and <see cref="Place"/>. Fixtures every ship carries, the cargo hatch and the approach suite, are left out.</summary>
    /// <param name="report">A report from <see cref="Match"/>.</param>
    /// <returns>In report order.</returns>
    public IReadOnlyList<OwnedModule> Unused(MatchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return
        [
            .. report.Unused
                .Where(module => !ModuleCatalog.IsFixture(module.Item))
                .Where(module => Filter switch
                {
                    EngineeringFilter.All => true,
                    EngineeringFilter.UnengineeredOnly => !module.IsEngineered,
                    EngineeringFilter.EngineeredOnly => module.IsEngineered,
                    _ => true,
                })
                .Where(module => Place switch
                {
                    PlaceFilter.Anywhere => true,
                    PlaceFilter.StoredOnly => module.Place == ModulePlace.Stored,
                    PlaceFilter.FittedOnly => module.Place == ModulePlace.Installed,
                    _ => true,
                }),
        ];
    }

    /// <summary>Appends a fit.</summary>
    /// <param name="fitting">The fit. Its id must be new.</param>
    public void Add(Fitting fitting)
    {
        ArgumentNullException.ThrowIfNull(fitting);
        _fittings.Add(fitting);
        _profiles.Remove(fitting.Id);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The jump numbers a fit's export carries, read from its <see cref="Fitting.LoadoutJson"/> once and
    /// kept until the fit changes.
    /// </summary>
    /// <param name="fitting">A fit in the list.</param>
    /// <returns>The profile, or <see langword="null"/> when the fit has no export or the export lacks the masses or a frame shift drive.</returns>
    public JumpProfile? ProfileOf(Fitting fitting)
    {
        ArgumentNullException.ThrowIfNull(fitting);
        if (!_profiles.TryGetValue(fitting.Id, out var profile))
        {
            profile = fitting.LoadoutJson is { } text ? EdsyImport.ProfileOf(text) : null;
            _profiles[fitting.Id] = profile;
        }

        return profile;
    }

    /// <summary>Replaces every fit and the policy.</summary>
    /// <param name="fittings">The fits, in priority order.</param>
    /// <param name="policy">The sharing policy.</param>
    public void Replace(IEnumerable<Fitting> fittings, SharingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(fittings);
        _fittings.Clear();
        _fittings.AddRange(fittings);
        _profiles.Clear();
        Policy = policy;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Replaces a fit in place, keeping its position in the list.</summary>
    /// <param name="fitting">The fit. Its id must be that of a fit already in the list; otherwise nothing changes.</param>
    public void Update(Fitting fitting)
    {
        ArgumentNullException.ThrowIfNull(fitting);
        var index = _fittings.FindIndex(existing => existing.Id == fitting.Id);
        if (index < 0)
        {
            return;
        }

        _fittings[index] = fitting;
        _profiles.Remove(fitting.Id);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Finds the fit a pasted build would stand in for: the same name on the same hull.</summary>
    /// <param name="name">The fit's name, compared case-insensitively.</param>
    /// <param name="hull">The hull symbol, compared case-insensitively.</param>
    /// <returns>The first such fit, or <see langword="null"/>.</returns>
    public Fitting? Find(string name, string hull)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(hull);
        return _fittings.FirstOrDefault(fitting =>
            string.Equals(fitting.Name, name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(fitting.Hull, hull, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Removes a fit.</summary>
    /// <param name="id">The fit's id. Unknown ids are ignored.</param>
    public void Remove(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (_fittings.RemoveAll(fitting => fitting.Id == id) > 0)
        {
            _profiles.Remove(id);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Finds a fit.</summary>
    /// <param name="id">The fit's id.</param>
    /// <returns>The fit, or <see langword="null"/>.</returns>
    public Fitting? Find(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _fittings.FirstOrDefault(fitting => fitting.Id == id);
    }

    private static Engineering? Convert(JournalEngineering? engineering)
        => engineering is null ? null : new Engineering(engineering.Blueprint, engineering.Level, engineering.Experimental, engineering.ExperimentalKnown, engineering.Quality);
}
