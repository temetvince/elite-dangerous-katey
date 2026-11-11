using Katey.App.Backup;
using Katey.App.Exploration;
using Katey.App.Navigation;
using Katey.App.Network;
using Katey.App.Pages;
using Katey.App.Persistence;
using Katey.App.Routing;
using Katey.App.Text;
using Katey.App.Trading;
using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Exploration;
using Katey.Core.Fleet;
using Katey.Core.Planning;
using Katey.Core.Trading;
using Katey.Journal;
using Microsoft.Extensions.Logging;

namespace Katey.App;

/// <summary>
/// Everything the pages, the rail and the search share: the catalogs, the trackers, the plan, the
/// engineer selection, the saved fits, and the navigator. It raises one event per kind of change so
/// the shell can save and the pages can refresh.
/// </summary>
/// <remarks>
/// The monitor's updates arrive on a background thread; the workspace runs them through the dispatcher
/// it was given, so every event here is raised on the UI thread.
/// </remarks>
public sealed class Workspace
{
    private readonly HashSet<string> _unknownNoted = new(StringComparer.Ordinal);
    private Navigator? _navigator;
    private ItemResolver? _resolver;
    private Func<string, bool>? _stateMover;

    /// <summary>Creates the workspace and subscribes to the monitor.</summary>
    /// <param name="catalog">The engineering catalog.</param>
    /// <param name="modules">The module catalog.</param>
    /// <param name="predictor">The species predictor.</param>
    /// <param name="monitor">The journal monitor. Not disposed here.</param>
    /// <param name="sinks">The trackers the monitor feeds.</param>
    /// <param name="dispatch">Runs an action on the UI thread.</param>
    /// <param name="plan">The plan.</param>
    /// <param name="engineers">The engineer selection.</param>
    /// <param name="fleet">The saved fits.</param>
    /// <param name="preferences">Remembered view choices.</param>
    /// <param name="plot">The plotted route and the plotter switch.</param>
    /// <param name="backup">The journal backup.</param>
    /// <param name="reserves">The material reserves.</param>
    /// <param name="store">Where the saved data lives.</param>
    /// <param name="network">What may leave the machine.</param>
    /// <param name="systemBodies">The bodies a catalogue knows for the systems visited this run.</param>
    /// <param name="logger">Where the view models log decisions and what the tables lack.</param>
    public Workspace(
        Catalog catalog,
        ModuleCatalog modules,
        ExobiologyPredictor predictor,
        IJournalMonitor monitor,
        JournalSinks sinks,
        Action<Action> dispatch,
        PlanViewModel plan,
        EngineerSelection engineers,
        FleetState fleet,
        UiPreferences preferences,
        PlotState plot,
        BackupState backup,
        ReserveState reserves,
        IStateStore store,
        NetworkAccess network,
        SystemBodiesState systemBodies,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(predictor);
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(sinks);
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(engineers);
        ArgumentNullException.ThrowIfNull(fleet);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(plot);
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(reserves);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(systemBodies);
        ArgumentNullException.ThrowIfNull(logger);
        Logger = logger;

        Network = network;
        SystemBodies = systemBodies;
        Catalog = catalog;
        Store = store;
        Reserves = reserves;
        reserves.Changed += (_, _) => SettingsChanged?.Invoke(this, EventArgs.Empty);
        Plot = plot;
        Backup = backup;
        Preferences = preferences;
        Modules = modules;
        Predictor = predictor;
        Monitor = monitor;
        Sinks = sinks;
        Plan = plan;
        Engineers = engineers;
        Fleet = fleet;
        monitor.Updated += (_, _) => dispatch(() => LiveChanged?.Invoke(this, EventArgs.Empty));
        monitor.StateChanged += (_, _) => dispatch(() => WatchChanged?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>Raised on the UI thread after any tracker or live file changed.</summary>
    public event EventHandler? LiveChanged;

    /// <summary>Raised on the UI thread after the monitor's state or replay progress changed.</summary>
    public event EventHandler? WatchChanged;

    /// <summary>Raised after a setting that is saved changes.</summary>
    public event EventHandler? SettingsChanged;

    /// <summary>Gets the engineering catalog.</summary>
    public Catalog Catalog { get; }

    /// <summary>Gets the module catalog.</summary>
    public ModuleCatalog Modules { get; }

    /// <summary>Gets the species predictor.</summary>
    public ExobiologyPredictor Predictor { get; }

    /// <summary>Gets the journal monitor.</summary>
    public IJournalMonitor Monitor { get; }

    /// <summary>Gets the trackers.</summary>
    public JournalSinks Sinks { get; }

    /// <summary>Gets the plan.</summary>
    public PlanViewModel Plan { get; }

    /// <summary>Gets the engineers the commander will visit.</summary>
    public EngineerSelection Engineers { get; }

    /// <summary>Gets the saved fits and their filters.</summary>
    public FleetState Fleet { get; }

    /// <summary>Gets the remembered view choices: sort orders, open groups, filters.</summary>
    public UiPreferences Preferences { get; }

    /// <summary>Gets what the indexes and pickers may offer: everything, or only what the selected engineers can roll while the remembered switch is on.</summary>
    public EngineerScope Scope => field ??= new EngineerScope(Catalog, Engineers, Preferences);

    /// <summary>An engineer's name as Katey shows it everywhere: their page, whether you ticked them, and whether the engineers switch narrows the lists.</summary>
    /// <param name="name">The engineer's name, as the catalog spells it. Must exist in the catalog.</param>
    /// <returns>The link, keyed on the engineer's id; see <see cref="EngineerLink"/>.</returns>
    public EngineerLink EngineerLinkFor(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new EngineerLink(Navigator.LinkTo(name, PageKind.Engineer, EngineerPage.KeyOf(Catalog.Engineers[name])), Engineers.Selected.Contains(name), Scope.IsNarrowed);
    }

    /// <summary>Gets the logger the view models write decisions to.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets what the player allows to leave the machine.</summary>
    public NetworkAccess Network { get; }

    /// <summary>Logs a symbol the tables do not know, once per symbol for the run, so a bug report shows what the catalog lacks without flooding the log.</summary>
    /// <param name="kind">What kind of thing it is, such as "material".</param>
    /// <param name="symbol">The symbol as the journal spells it.</param>
    public void NoteUnknown(string kind, string symbol)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(symbol);
        if (_unknownNoted.Add(kind + ":" + symbol))
        {
            Log.UnknownSymbol(Logger, kind, symbol);
        }
    }

    /// <summary>Gets the bodies a catalogue knows for the systems visited this run.</summary>
    public SystemBodiesState SystemBodies { get; }

    /// <summary>Gets the route Katey asked a plotter for, and whether asking is allowed.</summary>
    public PlotState Plot { get; }

    /// <summary>Gets the journal backup: where it goes and whether it runs on close.</summary>
    public BackupState Backup { get; }

    /// <summary>Gets where the saved data lives.</summary>
    public IStateStore Store { get; }

    /// <summary>Gets the material reserves: how many of each to keep, and which never to trade.</summary>
    public ReserveState Reserves { get; }

    /// <summary>Gets the navigator. Set once by <see cref="Attach"/>; reading it before that throws.</summary>
    /// <exception cref="InvalidOperationException">No navigator is attached yet.</exception>
    public Navigator Navigator => _navigator ?? throw new InvalidOperationException("The workspace has no navigator yet.");

    /// <summary>Gets or sets the journal folder to watch. Empty means none chosen.</summary>
    public string JournalFolder
    {
        get;
        set
        {
            var next = (value ?? "").Trim();
            if (!string.Equals(field, next, StringComparison.Ordinal))
            {
                field = next;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    } = "";

    /// <summary>
    /// Gets whether the monitor is still replaying the journal history. While it is, every snapshot
    /// below reads as empty, so nothing on screen shows a position, a system, a fleet or a figure from
    /// partway through the history; they all appear together once the replay is done.
    /// </summary>
    public bool IsCatchingUp => Monitor.State == MonitorState.CatchingUp;

    /// <summary>Gets how far the replay has come, 0 to 1 by files read; 0 before the files are counted.</summary>
    public double CatchUpFraction => Monitor.Progress.FileCount == 0 ? 0 : (double)Monitor.Progress.FilesDone / Monitor.Progress.FileCount;

    /// <summary>Gets whether the replay is running but its files are not counted yet, so a bar can only sweep.</summary>
    public bool IsCatchUpIndeterminate => IsCatchingUp && Monitor.Progress.FileCount == 0;

    /// <summary>Gets one line on the replay, such as "Reading the journal history, 12 of 40 files.", or empty when not replaying.</summary>
    public string CatchUpText
        => IsCatchingUp
            ? Strings.Format(Keys.Shell_CatchingUp, ("done", Monitor.Progress.FilesDone), ("total", Monitor.Progress.FileCount))
            : "";

    /// <summary>
    /// Gets the ship's position as the journal last reported it, corrected by the live route file when that is
    /// newer and starts elsewhere: the game can rewrite the day's journal when it is relaunched, losing the
    /// arrivals it held, and the route file's first stop is then the system the ship is really in. Empty while
    /// the history is being replayed.
    /// </summary>
    public RouteProgress Route
    {
        get
        {
            if (IsCatchingUp)
            {
                return RouteProgress.Empty;
            }

            var progress = Sinks.Route.Snapshot();
            return PositionFromRoute(progress) is { } here
                ? progress with { CurrentSystem = here.StarSystem, CurrentSystemAddress = here.SystemAddress, CurrentPosition = new StarPosition(here.X, here.Y, here.Z) }
                : progress;
        }
    }

    /// <summary>
    /// Gets the current system's bodies. Empty while the history is being replayed, and empty but for the
    /// system's name when <see cref="Route"/> has taken the position from the route file, since the journal's
    /// bodies belong to the system it last recorded.
    /// </summary>
    public SystemSnapshot System
    {
        get
        {
            if (IsCatchingUp)
            {
                return SystemSnapshot.Empty;
            }

            var snapshot = Sinks.Exploration.Snapshot();
            if (PositionFromRoute(Sinks.Route.Snapshot()) is { } here && here.SystemAddress != snapshot.SystemAddress)
            {
                snapshot = SystemSnapshot.Empty with { SystemName = here.StarSystem, SystemAddress = here.SystemAddress, LearnedSpecies = snapshot.LearnedSpecies };
            }

            return WithCatalogued(snapshot);
        }
    }

    /// <summary>
    /// Adds the bodies a catalogue knows for the system to the ones the player scanned, while the lookup is
    /// allowed. The player's own scan always wins: a body is the player's when the journal has it by id or by
    /// name, and the catalogue's otherwise, marked as such so nothing is claimed about discovery, mapping or
    /// landing. Turning the lookup off hides what was fetched; turning it back on shows it again without asking.
    /// </summary>
    private SystemSnapshot WithCatalogued(SystemSnapshot snapshot)
    {
        if (snapshot.SystemAddress == 0 || !Network.AllowSystemBodies || SystemBodies.BodiesFor(snapshot.SystemAddress) is not { Count: > 0 } catalogued)
        {
            return snapshot;
        }

        var ids = snapshot.Bodies.Select(body => body.BodyId).ToHashSet();
        var names = snapshot.Bodies.Select(body => body.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = catalogued
            .Where(body => !ids.Contains(body.BodyId) && !names.Contains(body.Name))
            .Select(body => new ScannedBody(
                body.BodyId,
                body.Name,
                body.IsStar,
                body.ParentStarId,
                body.Class,
                body.DistanceLs,
                body.Landable,
                body.Atmosphere,
                "",
                body.Volcanism,
                body.GravityG,
                body.TemperatureK,
                body.PressurePa,
                body.Mass,
                body.Terraformable,
                body.HasRings,
                body.Materials,
                WasDiscovered: true,
                WasMapped: true,
                WasFootfalled: true,
                MappedByYou: false,
                LandedByYou: false,
                body.BiologicalSignals,
                body.GeologicalSignals,
                [],
                [.. body.Genera.Select(genus => new GenusSignal(genus, GenusName(genus)))],
                [],
                FromCatalogue: true))
            .ToList();
        return added.Count == 0
            ? snapshot
            : snapshot with { Bodies = [.. snapshot.Bodies.Concat(added).OrderBy(body => body.DistanceLs).ThenBy(body => body.BodyId)] };
    }

    /// <summary>A genus symbol's display name: the word the symbol carries, such as "Bacterial" from <c>$Codex_Ent_Bacterial_Genus_Name;</c>.</summary>
    private static string GenusName(string genus)
    {
        var trimmed = genus.Replace("$Codex_Ent_", "", StringComparison.Ordinal).Replace("_Genus_Name;", "", StringComparison.Ordinal).Replace("_Name;", "", StringComparison.Ordinal);
        return trimmed.Length == 0 ? genus : trimmed;
    }

    /// <summary>The route file's first stop when it is newer than the journal's last arrival and names a different system, else <see langword="null"/>.</summary>
    private RouteStop? PositionFromRoute(RouteProgress progress)
    {
        var live = Monitor.Route;
        return live is not { Stops.Count: > 0 } || live.Stops[0] is not { SystemAddress: not 0 } here || here.SystemAddress == progress.CurrentSystemAddress
            ? null
            : progress.ArrivedAt is not { } arrived || live.Timestamp > arrived ? here : null;
    }

    /// <summary>Gets the ships and stored modules. Empty while the history is being replayed.</summary>
    public FleetSnapshot FleetSnapshot => IsCatchingUp ? FleetSnapshot.Empty : Sinks.Fleet.Snapshot();

    /// <summary>Gets the material inventory. Empty while the history is being replayed.</summary>
    public MaterialInventorySnapshot Inventory => IsCatchingUp ? MaterialInventorySnapshot.Empty : Sinks.Materials.Snapshot();

    /// <summary>Gets recent journal activity, newest first. Empty while the history is being replayed.</summary>
    public IReadOnlyList<ActivityItem> Activity => IsCatchingUp ? [] : Sinks.Activity.Snapshot();

    /// <summary>Gets the lifetime figures, the income ledger and the Powerplay standing. Empty while the history is being replayed.</summary>
    public StatisticsSnapshot Statistics => IsCatchingUp ? StatisticsSnapshot.Empty : Sinks.Statistics.Snapshot();

    /// <summary>Gets the technology broker purchases. Empty while the history is being replayed.</summary>
    public UnlocksSnapshot Unlocks => IsCatchingUp ? UnlocksSnapshot.Empty : Sinks.Unlocks.Snapshot();

    /// <summary>Gets the engineering rolls. Empty while the history is being replayed.</summary>
    public CraftsSnapshot Crafts => IsCatchingUp ? CraftsSnapshot.Empty : Sinks.Crafts.Snapshot();

    /// <summary>Gets the exploration and organic data aboard, unsold; empty while the monitor is catching up.</summary>
    public CarriedData Carried => IsCatchingUp ? CarriedData.Empty : Sinks.Carried.Snapshot();

    /// <summary>Gets every exploration and organic sale in the history; empty while the monitor is catching up.</summary>
    public SoldData Sold => IsCatchingUp ? SoldData.Empty : Sinks.Carried.Sold();

    /// <summary>
    /// Moves the plan on by the engineering the game has reported since each entry last moved: rolls count
    /// down at the grade rolled, and an entry whose target grade and experimental effect are on the module
    /// leaves the plan. Nothing happens while the history is being replayed, so an old roll never reaches
    /// an entry before its anchor is known. The plan raises its change when anything moves, which is what
    /// saves it.
    /// </summary>
    /// <returns>The entries that left the plan, in plan order. Empty when nothing moved.</returns>
    public IReadOnlyList<PlanEntry> AdvancePlan()
    {
        if (IsCatchingUp)
        {
            return [];
        }

        var plan = Plan.Snapshot();
        if (PlanProgress.Floor(plan) is not { } floor)
        {
            return [];
        }

        var crafts = Crafts.After(floor).ToList();
        if (crafts.Count == 0)
        {
            return [];
        }

        var resolver = _resolver ??= new ItemResolver(Catalog);
        var advance = PlanProgress.Apply(
            Catalog,
            plan,
            crafts.Select(craft => new Craft(craft.At, resolver.Resolve(craft.Module), craft.Blueprint, craft.Level, craft.Quality, craft.Experimental)));
        if (advance.Changed)
        {
            Plan.Update(advance.Plan);
        }

        return advance.Completed;
    }

    /// <summary>
    /// Why an unlock counts as bought, or <see langword="null"/> when it does not: the journal records the
    /// purchase at the broker, or a module it makes available was bought at outfitting, or is fitted to one
    /// of your ships or in storage. The last two prove the purchase even when it predates the journal
    /// history the game kept.
    /// </summary>
    /// <param name="unlock">The unlock.</param>
    /// <returns>One sentence, or <see langword="null"/>.</returns>
    public string? UnlockedBecause(Unlock unlock)
    {
        ArgumentNullException.ThrowIfNull(unlock);
        switch (Unlocks.Find(unlock.Items)?.Evidence)
        {
            case UnlockEvidence.Broker:
                return Strings.Get(Keys.Unlock_JournalRecordsPurchase);
            case UnlockEvidence.Outfitting:
                return Strings.Get(Keys.Unlock_BoughtAtOutfitting);
            case null:
                break;
            default:
                break;
        }

        var fleet = FleetSnapshot;
        var owned = fleet.Ships.SelectMany(ship => ship.Modules.Select(module => module.Item))
            .Concat(fleet.StoredModules.Select(module => module.Item));
        return owned.Any(unlock.Covers) ? Strings.Get(Keys.Unlock_OwnOne) : null;
    }

    /// <summary>Gets journal symbol → count, or <see langword="null"/> before the first Materials snapshot.</summary>
    public IReadOnlyDictionary<string, int>? InventoryCounts
    {
        get
        {
            var inventory = Inventory;
            return inventory.HasSnapshot ? inventory.Counts : null;
        }
    }

    /// <summary>Gets where the ship is, or <see langword="null"/> when the journal has not said.</summary>
    public GalacticPosition? CurrentPosition => Route.CurrentPosition is { } position ? new GalacticPosition(position.X, position.Y, position.Z) : null;

    /// <summary>Gives the workspace its navigator. Call once, before any page is built.</summary>
    /// <param name="navigator">The navigator.</param>
    /// <exception cref="InvalidOperationException">A navigator is already attached.</exception>
    public void Attach(Navigator navigator)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        if (_navigator is not null)
        {
            throw new InvalidOperationException("The workspace already has a navigator.");
        }

        _navigator = navigator;
    }

    /// <summary>Gives the workspace the shell's way of moving the saved data. Call once.</summary>
    /// <param name="mover">Moves the saved data to a folder and returns whether a saved state was already there and has been loaded.</param>
    /// <exception cref="InvalidOperationException">A mover is already attached.</exception>
    public void AttachStateMover(Func<string, bool> mover)
    {
        ArgumentNullException.ThrowIfNull(mover);
        if (_stateMover is not null)
        {
            throw new InvalidOperationException("The workspace already has a state mover.");
        }

        _stateMover = mover;
    }

    /// <summary>Keeps the saved data in a folder from now on, loading what is already there or writing the current data.</summary>
    /// <param name="folder">The folder.</param>
    /// <returns><see langword="true"/> when a saved state was already in the folder and has been loaded.</returns>
    /// <exception cref="InvalidOperationException">No mover is attached.</exception>
    public bool MoveStateTo(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        return (_stateMover ?? throw new InvalidOperationException("The workspace has no state mover.")).Invoke(folder);
    }

    /// <summary>Where an engineer's system is: the journal's coordinates when the player has been there, else the table's.</summary>
    /// <param name="engineer">The engineer.</param>
    /// <returns>The position.</returns>
    public GalacticPosition PositionOf(EngineerProfile engineer)
    {
        ArgumentNullException.ThrowIfNull(engineer);
        return Route.FindByName(engineer.System)?.Position is { } visited
            ? new GalacticPosition(visited.X, visited.Y, visited.Z)
            : engineer.Position;
    }

    /// <summary>Distance from the ship to a point.</summary>
    /// <param name="position">The point.</param>
    /// <returns>Light years, or <see langword="null"/> when the ship's position is unknown.</returns>
    public double? DistanceFromHere(GalacticPosition position) => CurrentPosition?.DistanceTo(position);

    /// <summary>
    /// How far an owned module is from the ship, for the fit matcher's tie-break: light years when the
    /// module's system is one the journal has coordinates for and the ship's position is known, 0 when the
    /// module is in the ship's own system, and <see cref="double.PositiveInfinity"/> when its place is
    /// unknown or has never been visited.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns>Lower is nearer.</returns>
    public double Farness(OwnedModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (module.System is null)
        {
            return double.PositiveInfinity;
        }

        var route = Route;
        return string.Equals(module.System, route.CurrentSystem, StringComparison.OrdinalIgnoreCase)
            ? 0
            : CurrentPosition is { } here && route.FindByName(module.System)?.Position is { } there
            ? here.DistanceTo(new GalacticPosition(there.X, there.Y, there.Z))
            : double.PositiveInfinity;
    }

    /// <summary>The plan's material totals with the live inventory laid against them.</summary>
    /// <returns>Every material an enabled entry needs, in totals order.</returns>
    public IReadOnlyList<MaterialLine> PlanLines()
        => MaterialCalculator.WithInventory(MaterialCalculator.Totals(Catalog, Plan.Snapshot()), InventoryCounts);

    /// <summary>What to hand over at the material traders to cover the plan's shortfalls and restore the reserves.</summary>
    /// <returns>The plan, or <see langword="null"/> before a Materials snapshot.</returns>
    public TradePlan? Trades()
    {
        if (InventoryCounts is not { } counts)
        {
            return null;
        }

        var needs = MaterialCalculator.Totals(Catalog, Plan.Snapshot()).ToDictionary(total => total.Material.Symbol, total => total.Needed, StringComparer.OrdinalIgnoreCase);
        return MaterialTrader.Plan(Catalog, needs, counts, Reserves.All);
    }

    /// <summary>Journal symbols of the materials the plan is short of, given the live inventory. Empty before a snapshot.</summary>
    /// <returns>A set compared case-insensitively.</returns>
    public IReadOnlySet<string> ShortMaterials()
        => InventoryCounts is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : PlanLines().Where(line => !line.IsCovered).Select(line => line.Material.Symbol).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>A module's name in the language in force; see <see cref="Names.Module"/>.</summary>
    /// <param name="symbol">The item symbol, any letter case.</param>
    /// <returns>Never empty for a non-empty symbol.</returns>
    public string ModuleName(string symbol) => Names.Module(Modules, Catalog, symbol);
}
