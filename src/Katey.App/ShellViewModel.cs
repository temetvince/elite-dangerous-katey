using System.ComponentModel;
using Katey.App.Backup;
using Katey.App.Exploration;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Network;
using Katey.App.Pages;
using Katey.App.Persistence;
using Katey.App.Rail;
using Katey.App.Routing;
using Katey.App.Search;
using Katey.App.Text;
using Katey.App.Trading;
using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Exploration;
using Katey.Core.Fleet;
using Katey.Journal;
using Katey.Localization;
using Microsoft.Extensions.Logging;

namespace Katey.App;

/// <summary>
/// The window's state: the search box, the workbench and its history, the live rail, and the plan
/// drawer. Every edit to the plan, the engineer selection, the saved fits or the settings is saved as it
/// happens.
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
    private readonly IStateStore _store;
    private readonly ILogger<ShellViewModel> _logger;

    /// <summary>Creates the shell and restores the saved state.</summary>
    /// <param name="catalog">The engineering catalog.</param>
    /// <param name="modules">The module catalog.</param>
    /// <param name="predictor">The species predictor.</param>
    /// <param name="store">Where the state is saved.</param>
    /// <param name="monitor">The journal monitor, already built over <paramref name="sinks"/>. The shell drives it but does not dispose it.</param>
    /// <param name="sinks">The trackers the monitor feeds.</param>
    /// <param name="plotter">Who plots routes when the user asks and has allowed it.</param>
    /// <param name="systems">Who knows a system's bodies, asked as the ship arrives when the user has allowed it.</param>
    /// <param name="releases">Who knows Katey's latest release, asked once at startup and on request when the user has allowed it.</param>
    /// <param name="backup">What copies the journal folder to the backup folder.</param>
    /// <param name="dispatch">Runs an action on the UI thread.</param>
    /// <param name="logger">Where lifecycle messages go.</param>
    public ShellViewModel(
        Catalog catalog,
        ModuleCatalog modules,
        ExobiologyPredictor predictor,
        IStateStore store,
        IJournalMonitor monitor,
        JournalSinks sinks,
        IRoutePlotter plotter,
        ISystemCatalog systems,
        IReleaseFeed releases,
        IJournalBackup backup,
        Action<Action> dispatch,
        ILogger<ShellViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(predictor);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(sinks);
        ArgumentNullException.ThrowIfNull(plotter);
        ArgumentNullException.ThrowIfNull(systems);
        ArgumentNullException.ThrowIfNull(releases);
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _logger = logger;
        var saved = store.Load();
        Log.StateLoaded(logger, saved.Plan.Count, saved.EngineerIds.Count);

        var engineers = new EngineerSelection(catalog, saved.EngineerIds);
        // The plan asks for an engineer's link only when it refreshes, by which time the workspace below exists.
        var plan = new PlanViewModel(catalog, () => engineers.Selected, name => Workspace!.EngineerLinkFor(name));
        var preferences = new UiPreferences(saved.Preferences);
        LanguageLabel = Languages.NativeName(Languages.Apply(preferences));
        TextScale = TextSize.Read(preferences);
        var fleet = new FleetState(saved.Fittings, saved.Policy, preferences);
        var network = new NetworkAccess(saved, logger);
        var plot = new PlotState(plotter, dispatch, logger, network, saved.Plot);
        var systemBodies = new SystemBodiesState(systems, dispatch, logger, network);
        var updates = new UpdateState(releases, dispatch, logger, network, AppVersion.Current);
        var backupState = new BackupState(backup, dispatch, logger, saved.BackupFolder ?? "", saved.BackupOnClose);
        var reserves = new ReserveState(saved.Reserves);
        Workspace = new Workspace(catalog, modules, predictor, monitor, sinks, dispatch, plan, engineers, fleet, preferences, plot, backupState, reserves, store, network, systemBodies, updates, logger)
        {
            JournalFolder = saved.JournalFolder ?? "",
        };
        Navigator = new Navigator(key => PageFactory.Build(key, Workspace));
        Workspace.Attach(Navigator);
        Workspace.AttachStateMover(MoveStateTo);
        Navigator.Open(PageKey.Home);
        Search = new SearchViewModel(Workspace);
        Rail = new RailViewModel(Workspace, saved.RailDetached);
        PlanDrawer = new PlanDrawerViewModel(Workspace, (entries, ship) => Log.BuildImported(logger, entries.Count, ship));

        plan.Load(saved.Plan);
        PlanDrawer.Update();

        plan.Changed += (_, _) => OnPlanChanged();
        engineers.Changed += (_, _) => OnEngineersChanged();
        fleet.Changed += (_, _) => OnFleetChanged();
        fleet.FilterChanged += (_, _) => Navigator.Refresh();
        Workspace.LiveChanged += (_, _) => OnLiveChanged();
        Workspace.WatchChanged += (_, _) => OnWatchChanged();
        Workspace.SettingsChanged += (_, _) => Save();
        Workspace.Reserves.Changed += (_, _) => PlanDrawer.Update();
        plot.Changed += (_, _) => OnPlotChanged();
        network.Changed += (_, _) =>
        {
            RequestSystemBodies();
            updates.CheckOnce();
            OnPlotChanged();
        };
        systemBodies.Changed += (_, _) =>
        {
            Navigator.Refresh();
            Rail.Refresh();
        };
        updates.Changed += (_, _) => Navigator.Refresh();
        backupState.Changed += (_, _) => OnPlotChanged();
        preferences.Changed += (_, _) =>
        {
            Save();
            TextScale = TextSize.Read(preferences);
        };
        HomeLink = Navigator.LinkTo("Katey", PageKind.Home);
        Nav = BuildNav();
        SettingsNav = BuildSettingsNav();
        StatsNav = BuildStatsNav();
        LanguageOptions = Languages.Options(preferences.Get(Languages.PreferenceKey));
        SetLanguageCommand = new RelayCommand<LanguageOption>(SetLanguage);
        UpdateNav();
        Navigator.Navigated += (_, _) =>
        {
            UpdateNav();
            Log.PageOpened(logger, Navigator.Current.Key.Kind, Navigator.Current.Key.Id);
        };
        Rail.PropertyChanged += OnRailChanged;
        // Asked last, once everything that shows the answer exists; the feed answers in the background.
        updates.CheckOnce();
    }

    /// <summary>Gets the window title.</summary>
    public static string Title => "Elite Dangerous - Katey";

    /// <summary>Gets the shared state.</summary>
    public Workspace Workspace { get; }

    /// <summary>Gets the workbench history and the page showing.</summary>
    public Navigator Navigator { get; }

    /// <summary>Gets the search box.</summary>
    public SearchViewModel Search { get; }

    /// <summary>Gets the link the wordmark carries: the start page.</summary>
    public Link HomeLink { get; }

    /// <summary>Gets the navbar's sections, in display order. Settings and Stats are not among them; see <see cref="SettingsNav"/> and <see cref="StatsNav"/>. Rebuilt when the language changes.</summary>
    public IReadOnlyList<NavItem> Nav { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the language picker's choices: the machine's language first, then every language Katey ships in under its own name; the one in force is marked current.</summary>
    public IReadOnlyList<LanguageOption> LanguageOptions { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the name the language in force gives itself, for the picker's button: "English", "Deutsch".</summary>
    public string LanguageLabel { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the factor the windows draw their content at, from the text size setting. 1 is the design size.</summary>
    public double TextScale { get; private set => SetProperty(ref field, value); } = 1;

    /// <summary>Gets the command that switches to a <see cref="LanguageOption"/>: every page, the rail, the drawer, the search and the chrome change at once, and the choice is saved.</summary>
    public RelayCommand<LanguageOption> SetLanguageCommand { get; }

    /// <summary>Gets the settings link, shown in the bottom bar rather than among the sections. Rebuilt when the language changes.</summary>
    public NavItem SettingsNav { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the bottom bar's Stats link, between Settings and the search box, which keeps the top bar's page links on one line in the longest language. Rebuilt when the language changes.</summary>
    public NavItem StatsNav { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the live rail.</summary>
    public RailViewModel Rail { get; }

    /// <summary>Gets the plan drawer.</summary>
    public PlanDrawerViewModel PlanDrawer { get; }

    /// <summary>Gets whether the journal history is still being replayed, during which no live figure is shown.</summary>
    public bool IsCatchingUp { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the replay's progress line, or empty once it is done.</summary>
    public string CatchUpText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets how far the replay has come, 0 to 1 by files read.</summary>
    public double CatchUpFraction { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets whether the replay runs without a file count yet, so the bar sweeps instead of filling.</summary>
    public bool IsCatchUpIndeterminate { get; private set => SetProperty(ref field, value); }

    /// <summary>Runs the journal backup when it is set to run on close. Call once, as the app exits, before the monitor is disposed.</summary>
    /// <returns>What the backup did, or <see langword="null"/> when none ran.</returns>
    public BackupResult? Shutdown()
    {
        var result = Workspace.Backup.BackUpOnClose(Workspace.JournalFolder);
        if (result is not null)
        {
            Log.BackedUpOnClose(_logger, result.Copied, result.Failures.Count);
        }

        return result;
    }

    /// <summary>Starts watching the saved journal folder, or the detected one. Call once the window is up.</summary>
    public void Start()
    {
        if (Workspace.JournalFolder.Length == 0)
        {
            Workspace.JournalFolder = JournalFolder.FindDefault() ?? "";
        }

        if (Workspace.JournalFolder.Length > 0)
        {
            Log.JournalFolderChosen(_logger);
            Workspace.Monitor.Start(Workspace.JournalFolder);
        }
    }

    private bool MoveStateTo(string folder)
    {
        // The data at the old location is left current, so it can be picked up again later.
        Save();
        var found = _store.Relocate(folder);
        Log.StateMoved(_logger, found);
        if (found)
        {
            Apply(_store.Load());
        }
        else
        {
            Save();
        }

        return found;
    }

    private void Apply(PersistedState saved)
    {
        Workspace.Preferences.Replace(saved.Preferences);
        Workspace.Engineers.Replace(saved.EngineerIds);
        Workspace.Fleet.Replace(saved.Fittings, saved.Policy);
        Workspace.Reserves.Replace(saved.Reserves);
        Workspace.Plan.Load(saved.Plan);
        var folder = saved.JournalFolder ?? "";
        if (!string.Equals(folder, Workspace.JournalFolder, StringComparison.Ordinal))
        {
            Workspace.JournalFolder = folder;
            if (folder.Length > 0)
            {
                Workspace.Monitor.Start(folder);
            }
        }

        Workspace.Network.Load(saved);
        Workspace.Plot.Restore(saved.Plot);
        Workspace.Backup.Folder = saved.BackupFolder ?? "";
        Workspace.Backup.OnClose = saved.BackupOnClose;
        Rail.IsDetached = saved.RailDetached;
        Navigator.Open(Navigator.Current.Key);
        Save();
    }

    private IReadOnlyList<NavItem> BuildNav()

        => [
            new(Navigator.LinkTo(Strings.Get(Keys.Nav_Engineers), PageKind.Engineers), new HashSet<PageKind> { PageKind.Engineers, PageKind.Engineer }, Navigator),
            new(Navigator.LinkTo(Strings.Get(Keys.Nav_Modules), PageKind.Modules), new HashSet<PageKind> { PageKind.Modules, PageKind.ModuleType, PageKind.Blueprint }, Navigator),
            new(Navigator.LinkTo(Strings.Get(Keys.Nav_Materials), PageKind.Materials), new HashSet<PageKind> { PageKind.Materials, PageKind.Material }, Navigator),
            new(Navigator.LinkTo(Strings.Get(Keys.Nav_Unlocks), PageKind.Unlocks), new HashSet<PageKind> { PageKind.Unlocks }, Navigator),
            new(Navigator.LinkTo(Strings.Get(Keys.Nav_Fleet), PageKind.Fleet), new HashSet<PageKind> { PageKind.Fleet, PageKind.Ship, PageKind.Fit }, Navigator),
            new(Navigator.LinkTo(Strings.Get(Keys.Nav_Route), PageKind.Route), new HashSet<PageKind> { PageKind.Route }, Navigator),
            new(Navigator.LinkTo(Strings.Get(Keys.Nav_System), PageKind.System), new HashSet<PageKind> { PageKind.System }, Navigator),
        ];

    private NavItem BuildSettingsNav() => new(Navigator.LinkTo(Strings.Get(Keys.Nav_Settings), PageKind.Settings), new HashSet<PageKind> { PageKind.Settings }, Navigator);

    private NavItem BuildStatsNav() => new(Navigator.LinkTo(Strings.Get(Keys.Nav_Stats), PageKind.Statistics), new HashSet<PageKind> { PageKind.Statistics }, Navigator);

    private void SetLanguage(LanguageOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (option.Saved.Length == 0)
        {
            Workspace.Preferences.Remove(Languages.PreferenceKey);
        }
        else
        {
            Workspace.Preferences.Set(Languages.PreferenceKey, option.Saved);
        }

        var before = Language.Current;
        var culture = Languages.Apply(Workspace.Preferences);
        Log.LanguageChanged(_logger, before.Name, culture.Name);
        Nav = BuildNav();
        SettingsNav = BuildSettingsNav();
        StatsNav = BuildStatsNav();
        LanguageOptions = Languages.Options(option.Saved);
        LanguageLabel = Languages.NativeName(culture);
        Navigator.Rebuild();
        UpdateNav();
        Rail.Refresh();
        Workspace.Plan.Reword();
        PlanDrawer.Update();
        Search.Rebuild();
    }

    private void UpdateNav()
    {
        foreach (var item in Nav)
        {
            item.Update(Navigator.Current.Key.Kind);
        }

        SettingsNav.Update(Navigator.Current.Key.Kind);
        StatsNav.Update(Navigator.Current.Key.Kind);
    }

    private void OnPlanChanged()
    {
        PlanDrawer.Update();
        Navigator.Refresh();
        Rail.Refresh();
        Save();
    }

    private void OnEngineersChanged()
    {
        Workspace.Plan.RefreshEngineers();
        PlanDrawer.Update();
        Navigator.Refresh();
        Save();
    }

    private void OnFleetChanged()
    {
        Navigator.Refresh();
        Search.RefreshJournalIndex();
        Save();
    }

    private void OnLiveChanged()
    {
        foreach (var done in Workspace.AdvancePlan())
        {
            Log.PlanEntryCompleted(_logger, done.Blueprint, done.TargetGrade, done.Experimental ?? "none");
        }

        RequestSystemBodies();

        PlanDrawer.Update();
        Navigator.Refresh();
        Rail.Refresh();
        Search.RefreshJournalIndex();
    }

    private void OnWatchChanged()
    {
        IsCatchingUp = Workspace.IsCatchingUp;
        CatchUpText = Workspace.CatchUpText;
        CatchUpFraction = Workspace.CatchUpFraction;
        IsCatchUpIndeterminate = Workspace.IsCatchUpIndeterminate;
        Navigator.Refresh();
        Rail.Refresh();
    }

    /// <summary>Asks the catalogue about the system the ship is in, which does nothing unless the player allows it and the system is new to this run.</summary>
    private void RequestSystemBodies()
    {
        if (!Workspace.IsCatchingUp)
        {
            Workspace.SystemBodies.Request(Workspace.Route.CurrentSystemAddress);
        }
    }

    private void OnPlotChanged()
    {
        Navigator.Refresh();
        Rail.Refresh();
        Save();
    }

    private void OnRailChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(RailViewModel.IsDetached))
        {
            Log.RailMoved(_logger, Rail.IsDetached);
            Save();
        }
    }

    private void Save()
        => _store.Save(new PersistedState(
            Workspace.Plan.Snapshot(),
            Workspace.Engineers.SelectedIds,
            Workspace.JournalFolder.Length == 0 ? null : Workspace.JournalFolder,
            Workspace.Fleet.Fittings,
            Workspace.Fleet.Policy,
            Rail.IsDetached,
            Workspace.Preferences.Snapshot(),
            Workspace.Network.AllowRoutes,
            Workspace.Plot.Route,
            Workspace.Backup.Folder.Length == 0 ? null : Workspace.Backup.Folder,
            Workspace.Backup.OnClose,
            Workspace.Reserves.All,
            Workspace.Network.AllowSystemBodies,
            Workspace.Network.AllowUpdateCheck));
}
