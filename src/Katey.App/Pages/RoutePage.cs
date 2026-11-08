using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Routing;
using Katey.App.ViewModels;
using Katey.Core.Exploration;
using Katey.Core.Fleet;
using Katey.Core.Routing;
using Katey.Journal;

namespace Katey.App.Pages;

/// <summary>What the journal history says about a route stop, from least to most known.</summary>
public enum VisitState
{
    /// <summary>The player has never arrived there, as far as the history goes.</summary>
    New,

    /// <summary>The player has arrived there at least once before.</summary>
    Visited,

    /// <summary>The player has honked, read the nav beacon or found every body there.</summary>
    Surveyed,
}

/// <summary>Where a route stop stands relative to the ship.</summary>
public enum StopStage
{
    /// <summary>Already passed.</summary>
    Done,

    /// <summary>The system the ship is in.</summary>
    Here,

    /// <summary>The next jump.</summary>
    Next,

    /// <summary>Further along.</summary>
    Ahead,

    /// <summary>The last plot waypoint the ship reached before leaving the plot.</summary>
    LastKnown,
}

/// <summary>One star on the plotted route, ready to print.</summary>
/// <param name="Index">Position on the route, 1-based.</param>
/// <param name="Link">The system's page.</param>
/// <param name="StarClass">Spectral class as the game writes it.</param>
/// <param name="Kind">What the class means for the traveller.</param>
/// <param name="Visit">What the journal history says about the stop: never visited, visited, or surveyed.</param>
/// <param name="Stage">Where it stands relative to the ship.</param>
/// <param name="JumpLy">Distance from the previous stop in light years, or 0 for the first.</param>
/// <param name="CumulativeLy">Distance from the route's start to this stop in light years.</param>
/// <param name="Engineer">An engineer based here, or <see langword="null"/>.</param>
/// <param name="Fuel">Projected fuel on arrival, or <see langword="null"/> when there is no estimate or the stop is behind the ship.</param>
public sealed record RouteStopRow(int Index, Link Link, string StarClass, StarKind Kind, VisitState Visit, StopStage Stage, double JumpLy, double CumulativeLy, Link? Engineer, StopFuel? Fuel)
{
    /// <summary>Gets the fuel on arrival as text, or empty.</summary>
    public string FuelText => Fuel?.Text ?? "";

    /// <summary>Gets whether the projection says the ship cannot reach this stop.</summary>
    public bool IsOutOfFuel => Fuel is { Reachable: false };

    /// <summary>Gets whether the projection refuels here.</summary>
    public bool ScoopHere => Fuel is { ScoopHere: true };

    /// <summary>Gets the system name.</summary>
    public string System => Link.Label;

    /// <summary>Gets a short description of the star: class and what it means.</summary>
    public string StarText => $"{StarClasses.Brief(StarClass)} · {StarClasses.Label(Kind)}";

    /// <summary>Gets whether the drive can refuel here.</summary>
    public bool IsScoopable => Kind == StarKind.Scoopable;

    /// <summary>Gets whether the star supercharges the drive.</summary>
    public bool IsSupercharger => Kind is StarKind.Neutron or StarKind.WhiteDwarf;

    /// <summary>Gets "surveyed", "visited" or "new to you".</summary>
    public string VisitedText => RoutePage.VisitLabel(Visit);

    /// <summary>Gets "here", "next", "done" or empty.</summary>
    public string StageText => RoutePage.StageLabel(Stage);

    /// <summary>Gets the jump distance as text, or empty for the first stop.</summary>
    public string JumpText => JumpLy > 0 ? string.Create(CultureInfo.InvariantCulture, $"{JumpLy:0.00} ly") : "";

    /// <summary>Gets the distance from the start as text.</summary>
    public string CumulativeText => Display.LightYears(CumulativeLy);

    /// <summary>Gets whether this stop is the ship's position.</summary>
    public bool IsHere => Stage == StopStage.Here;

    /// <summary>Gets whether this stop is the next jump.</summary>
    public bool IsNext => Stage == StopStage.Next;

    /// <summary>Gets whether this stop is behind the ship.</summary>
    public bool IsDone => Stage == StopStage.Done;

    /// <summary>Gets whether an engineer is based here.</summary>
    public bool HasEngineer => Engineer is not null;
}

/// <summary>One thing a route can be plotted for: an owned ship whose loadout is known, or a saved fit with its masses.</summary>
/// <param name="Key">Unique in the picker: the kind and the id, as <see cref="ShipKey"/> and <see cref="FitKey"/> make it.</param>
/// <param name="Kind"><see cref="ShipKind"/> or <see cref="FitKind"/>.</param>
/// <param name="Name">The ship's title or the fit's name.</param>
/// <param name="Hull">Hull symbol, lower case.</param>
/// <param name="HullName">Hull display name.</param>
/// <param name="Range">Full-tank range with no cargo, light years.</param>
/// <param name="IsFlown">Whether this is the ship being flown, which the picker colours apart.</param>
public sealed record PlotTarget(string Key, string Kind, string Name, string Hull, string HullName, double Range, bool IsFlown = false)
{
    /// <summary>The kind of an owned ship.</summary>
    public const string ShipKind = "Ship";

    /// <summary>The kind of a saved fit.</summary>
    public const string FitKind = "Fit";

    /// <summary>Gets the id after the kind: the ship id, or the fit's id.</summary>
    public string Id => Key[(Key.IndexOf(':', StringComparison.Ordinal) + 1)..];

    /// <summary>Gets the line the picker shows and searches, such as "Borrowed Time · Anaconda Mk II · 42.1 ly".</summary>
    public string Label => string.Create(CultureInfo.InvariantCulture, $"{Name} · {HullName} · {Range:0.0} ly");

    /// <summary>The key of an owned ship.</summary>
    /// <param name="shipId">The game's ship id.</param>
    /// <returns>The key.</returns>
    public static string ShipKey(int shipId) => string.Create(CultureInfo.InvariantCulture, $"ship:{shipId}");

    /// <summary>The key of a saved fit.</summary>
    /// <param name="fittingId">The fit's id.</param>
    /// <returns>The key.</returns>
    public static string FitKey(string fittingId) => "fit:" + fittingId;
}

/// <summary>One waypoint on the Spansh plot, ready to print.</summary>
/// <param name="Index">Position on the plot, 1-based.</param>
/// <param name="Link">The system's page.</param>
/// <param name="Stage">Where it stands relative to the ship.</param>
/// <param name="Visit">What the journal history says about the stop: never visited, visited, or surveyed.</param>
/// <param name="JumpLy">Distance from the previous waypoint in light years, or 0 for the first.</param>
/// <param name="RemainingLy">Straight-line distance from here to the destination.</param>
/// <param name="FuelInTank">Tonnes in the tank on arrival as the plotter reckons it.</param>
/// <param name="HasNeutron">Whether the arrival star is a neutron star.</param>
/// <param name="IsScoopable">Whether the ship can refuel here.</param>
/// <param name="MustRefuel">Whether the plot depends on refuelling here.</param>
public sealed record PlotStopRow(int Index, Link Link, StopStage Stage, VisitState Visit, double JumpLy, double RemainingLy, double FuelInTank, bool HasNeutron, bool IsScoopable, bool MustRefuel)
{
    /// <summary>Gets the system name.</summary>
    public string System => Link.Label;

    /// <summary>Gets "here", "next", "done" or empty.</summary>
    public string StageText => RoutePage.StageLabel(Stage);

    /// <summary>Gets the jump distance as text, or empty for the first waypoint.</summary>
    public string JumpText => JumpLy > 0 ? string.Create(CultureInfo.InvariantCulture, $"{JumpLy:0.00} ly") : "";

    /// <summary>Gets the distance left as text.</summary>
    public string RemainingText => Display.LightYears(RemainingLy);

    /// <summary>Gets the tank on arrival as text.</summary>
    public string FuelText => string.Create(CultureInfo.InvariantCulture, $"{FuelInTank:0.0} t");

    /// <summary>Gets the waypoint's notes: "neutron", "scoop", "refuel here", joined.</summary>
    public string NotesText => string.Join(" · ", Notes());

    /// <summary>Gets "surveyed", "visited" or "new to you".</summary>
    public string VisitedText => RoutePage.VisitLabel(Visit);

    /// <summary>Gets whether this waypoint is the ship's position.</summary>
    public bool IsHere => Stage == StopStage.Here;

    /// <summary>Gets whether this waypoint is the next jump.</summary>
    public bool IsNext => Stage == StopStage.Next;

    /// <summary>Gets whether this waypoint is behind the ship.</summary>
    public bool IsDone => Stage == StopStage.Done;

    private IEnumerable<string> Notes()
    {
        if (HasNeutron)
        {
            yield return "neutron";
        }

        if (MustRefuel)
        {
            yield return "refuel here";
        }
        else if (IsScoopable)
        {
            yield return "scoop";
        }
    }
}

/// <summary>Where the ship stands on the Spansh plot.</summary>
/// <param name="Text">One line: the next waypoint and how many remain, or that the ship has arrived or is off the plot.</param>
/// <param name="NextWaypoint">The next waypoint's name, or empty at the destination. Off the plot, the waypoint after the last one reached, so a detour still has its target.</param>
/// <param name="HereIndex">The ship's index on the plot, or -1 when it is not on it.</param>
/// <param name="LastKnownIndex">Off the plot, the index of the waypoint the ship reached most recently, or -1 on the plot or when it has reached none.</param>
public sealed record PlotProgressLine(string Text, string NextWaypoint, int HereIndex, int LastKnownIndex = -1);

/// <summary>
/// The route: the game's own plot from the galaxy map with the ship's progress along it, and the plot
/// Katey asks Spansh for, with the form that asks. Also computes the rows the rail windows.
/// </summary>
public sealed class RoutePage : Page
{
    /// <summary>The key of the Spansh section, which a page id of the same value opens.</summary>
    public const string PlotSection = "plot";

    private const string GameSection = "game";
    private const string ToKey = "plot.to";
    private const string AlgorithmKey = "plot.algorithm";
    private const string SuperchargeKey = "plot.supercharge";
    private const string InjectionsKey = "plot.injections";
    private const string SecondaryKey = "plot.secondary";
    private const string RefuelKey = "plot.refuelEveryScoopable";
    private const string TargetKey = "plot.target";
    private const string CargoKey = "plot.cargo";

    private PlotTarget? _selectedPlotTarget;
    private string? _currentShipKey;
    private List<PlotTarget> _plotTargets = [];

    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <param name="section">The section to open on, <see cref="PlotSection"/> for the Spansh plot, or empty for the remembered one.</param>
    public RoutePage(Workspace workspace, string section = "")
        : base(new PageKey(PageKind.Route), workspace)
    {
        ArgumentNullException.ThrowIfNull(section);
        Title = "Route";
        Kicker = "Live";
        Sections = new SectionSwitch([(GameSection, "Game route"), (PlotSection, "Spansh plot")], GameSection, workspace.Preferences, "section.route");
        if (section == PlotSection)
        {
            Sections.Show(PlotSection);
        }

        Sort = new TableSort<RouteStopRow>(
            [
                new("stage", "Stage", row => row.Index),
                new("index", "Stop", row => row.Index),
                new("system", "System", row => row.System),
                new("star", "Star", row => row.StarText),
                new("visited", "Visited", row => -(int)row.Visit),
                new("jump", "Jump", row => row.JumpLy, DescendingFirst: true),
                new("travelled", "From start", row => row.CumulativeLy),
            ],
            "index",
            workspace.Preferences,
            "sort.route");
        PlotSort = new TableSort<PlotStopRow>(
            [
                new("stage", "Stage", row => row.Index),
                new("index", "Stop", row => row.Index),
                new("system", "System", row => row.System),
                new("jump", "Jump", row => row.JumpLy, DescendingFirst: true),
                new("left", "To go", row => row.RemainingLy),
                new("fuel", "Fuel", row => row.FuelInTank),
                new("notes", "Notes", row => row.NotesText),
            ],
            "index",
            workspace.Preferences,
            "sort.plot");
        Sort.Changed += (_, _) => Refresh();
        PlotSort.Changed += (_, _) => Refresh();
        SettingsLink = LinkTo("Settings", PageKind.Settings);
        SelectedAlgorithm = Algorithms.FirstOrDefault(choice => choice.Key == workspace.Preferences.Get(AlgorithmKey)) ?? Algorithms[0];
        PlotTo = workspace.Preferences.Get(ToKey) ?? "";
        UseSupercharge = workspace.Preferences.GetBool(SuperchargeKey, true);
        UseInjections = workspace.Preferences.GetBool(InjectionsKey, false);
        ExcludeSecondary = workspace.Preferences.GetBool(SecondaryKey, false);
        RefuelEveryScoopable = workspace.Preferences.GetBool(RefuelKey, false);
        PlotFrom = workspace.Route.CurrentSystem ?? workspace.Plot.Route?.Source ?? "";
        PlotCommand = new RelayCommand(Plot, CanPlot);
        CancelPlotCommand = new RelayCommand(workspace.Plot.Cancel, () => workspace.Plot.IsBusy);
        ClearPlotCommand = new RelayCommand(ForgetPlot, () => workspace.Plot.Route is not null);
        Refresh();
    }

    /// <summary>Gets the page's sections: the game's route and the Spansh plot.</summary>
    public SectionSwitch Sections { get; }

    /// <summary>Gets the game route table's sort.</summary>
    public TableSort<RouteStopRow> Sort { get; }

    /// <summary>Gets the plot table's sort.</summary>
    public TableSort<PlotStopRow> PlotSort { get; }

    /// <summary>Gets whether a route is plotted in the game.</summary>
    public bool HasRoute { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the game route's stops in sort order.</summary>
    public IReadOnlyList<RouteStopRow> Stops { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets one line on progress, such as "Jump 3 of 11, 8 to go".</summary>
    public string Summary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets a line on fuel and the next scoopable star, or empty.</summary>
    public string FuelText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets how far along the route the ship is, such as "4,002 ly of 7,320 ly travelled".</summary>
    public string TravelledText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the line on the plotted route a targeted detour leaves behind, as <see cref="DetourLine"/> words it, or empty.</summary>
    public string DetourText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the fuel picture: the tank, what the next jump burns, and how far the route gets without a scoop. <see cref="FuelEstimate.None"/> until a refresh.</summary>
    public FuelEstimate Fuel { get; private set => SetProperty(ref field, value); } = FuelEstimate.None;

    /// <summary>Gets whether Katey may ask Spansh. Turned on in settings.</summary>
    public bool PlotEnabled => Workspace.Plot.Enabled;

    /// <summary>Gets the settings page, where the Spansh switch is.</summary>
    public Link SettingsLink { get; }

    /// <summary>Gets or sets the departure system. Starts on the system the ship is in.</summary>
    public string PlotFrom
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? ""))
            {
                PlotCommand?.RaiseCanExecuteChanged();
            }
        }
    } = "";

    /// <summary>Gets or sets the destination system. Saved as it changes, like every option on the form, and back on the next visit.</summary>
    public string PlotTo
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? ""))
            {
                Workspace.Preferences.Set(ToKey, field.Trim());
                PlotCommand?.RaiseCanExecuteChanged();
            }
        }
    } = "";

    /// <summary>
    /// Gets or sets the cargo aboard in tonnes, as typed. Empty or unreadable text counts as none. Starts
    /// as what the ship being flown carries, then stays as it is, typed over or not, across pages and
    /// restarts until the plot is forgotten; switching ships or fits leaves it alone.
    /// </summary>
    public string PlotCargo
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? ""))
            {
                Workspace.Preferences.Set(CargoKey, field.Trim());
                RefreshRange();
            }
        }
    } = "";

    /// <summary>Gets the routing algorithms to choose from.</summary>
    public IReadOnlyList<Choice> Algorithms { get; } =
    [
        new(nameof(PlotAlgorithm.Optimistic), "Optimistic: fewest jumps, favours neutron stars"),
        new(nameof(PlotAlgorithm.Pessimistic), "Pessimistic: the sure route"),
    ];

    /// <summary>Gets or sets the algorithm. Must be one of <see cref="Algorithms"/>.</summary>
    public Choice SelectedAlgorithm
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                Workspace.Preferences.Set(AlgorithmKey, value.Key);
            }
        }
    }

    /// <summary>Gets or sets whether the plot may take neutron stars for the boost. Saved as it changes.</summary>
    public bool UseSupercharge
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Workspace.Preferences.SetBool(SuperchargeKey, value);
            }
        }
    }

    /// <summary>Gets or sets whether the plot may count on FSD injections. Saved as it changes.</summary>
    public bool UseInjections
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Workspace.Preferences.SetBool(InjectionsKey, value);
            }
        }
    }

    /// <summary>Gets or sets whether the plot avoids systems whose scoopable star is not the arrival star. Saved as it changes.</summary>
    public bool ExcludeSecondary
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Workspace.Preferences.SetBool(SecondaryKey, value);
            }
        }
    }

    /// <summary>Gets or sets whether the plot fills the tank at every scoopable star it passes. Saved as it changes.</summary>
    public bool RefuelEveryScoopable
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Workspace.Preferences.SetBool(RefuelKey, value);
            }
        }
    }

    /// <summary>
    /// Gets what the plot can be made for, as the picker lists it: the chosen target first, then the ship
    /// being flown, then the saved fits for the chosen target's hull, then the owned ships whose loadout
    /// the journal has read, then the other saved fits, each with its range. <see cref="PlotFilter"/>
    /// narrows the list to the entries whose line contains the text; the chosen target always stays in
    /// it. Fits saved without their masses are left out.
    /// </summary>
    public IReadOnlyList<PlotTarget> PlotTargets { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether anything can be plotted for.</summary>
    public bool HasPlotTargets => _plotTargets.Count > 0;

    /// <summary>Gets or sets the ship or fit to plot for. Must be one of <see cref="PlotTargets"/>. Starts on the ship being flown, and then stays as chosen across pages and restarts until the plot is forgotten. Choosing one reorders the list around it.</summary>
    public PlotTarget? SelectedPlotTarget
    {
        get => _selectedPlotTarget;
        set
        {
            if (value is not null && SetProperty(ref _selectedPlotTarget, value))
            {
                Workspace.Preferences.Set(TargetKey, value.Key);
                ArrangePlotTargets();
                RefreshRange();
                PlotCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets or sets the text that narrows <see cref="PlotTargets"/>, matched case-insensitively against each entry's line. Empty shows everything. Not saved.</summary>
    public string PlotFilter
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? ""))
            {
                ArrangePlotTargets();
            }
        }
    } = "";

    /// <summary>Gets a note on saved fits the plot cannot use, such as "2 saved fits have no masses: paste them again from EDSY to plot for them.", or empty.</summary>
    public string PlotTargetsNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the chosen ship's range with the cargo typed, or why there is none.</summary>
    public string PlotRangeText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the command that asks Spansh for the plot.</summary>
    public RelayCommand PlotCommand { get; }

    /// <summary>Gets the command that stops a running plot.</summary>
    public RelayCommand CancelPlotCommand { get; }

    /// <summary>Gets the command that forgets the plot.</summary>
    public RelayCommand ClearPlotCommand { get; }

    /// <summary>Gets what the last plot is doing or why it failed, or empty.</summary>
    public string PlotStatus { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether a plot is running.</summary>
    public bool IsPlotting { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets whether a Spansh plot is held.</summary>
    public bool HasPlot { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets one line on the plot: ends, jumps, length, neutron and refuel stops, and when it was made.</summary>
    public string PlotSummary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets where the ship stands on the plot.</summary>
    public string PlotProgressText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the next waypoint's name, for the clipboard, or empty.</summary>
    public string NextWaypoint { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether there is a next waypoint to copy.</summary>
    public bool HasNextWaypoint => NextWaypoint.Length > 0;

    /// <summary>Gets the plot's waypoints in sort order.</summary>
    public IReadOnlyList<PlotStopRow> PlotStops { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>The word for a stage.</summary>
    /// <param name="stage">The stage.</param>
    /// <returns>"here", "next", "done" or empty.</returns>
    public static string StageLabel(StopStage stage) => stage switch
    {
        StopStage.Here => "here",
        StopStage.Next => "next",
        StopStage.Done => "done",
        StopStage.Ahead => "",
        StopStage.LastKnown => "last known",
        _ => "",
    };

    /// <summary>Builds the stop rows for the current route and position, with the fuel projection laid on the stops ahead.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <returns>Every stop, departure first; empty without a route.</returns>
    public static IReadOnlyList<RouteStopRow> BuildStops(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var bare = BuildBareStops(workspace);
        if (bare.Count == 0)
        {
            return bare;
        }

        var fuel = FuelEstimate.For(workspace, bare);
        return [.. bare.Select(stop => stop with { Fuel = fuel.Stops.GetValueOrDefault(stop.Index) })];
    }

    /// <summary>
    /// What the history says about a stop. Surveyed when the player ever honked, read the nav beacon or found
    /// every body there, this visit included. Visited when the history knows the system, by address or by
    /// name, and, for the stop the player is at now, holds an arrival earlier than the current one. New
    /// otherwise.
    /// </summary>
    /// <param name="progress">The route progress, with the systems the history has seen.</param>
    /// <param name="address">The stop's system address, or 0 when unknown.</param>
    /// <param name="name">The stop's system name, for a history entry without a matching address.</param>
    /// <param name="stage">Where the stop stands on the route.</param>
    /// <returns>The state.</returns>
    public static VisitState VisitOf(RouteProgress progress, long address, string name, StopStage stage)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(name);
        var known = progress.KnownSystems.GetValueOrDefault(address) ?? progress.FindByName(name);
        return known is null
            ? VisitState.New
            : known.Surveyed
                ? VisitState.Surveyed
                : stage != StopStage.Here || known.Visits > 1
                    ? VisitState.Visited
                    : VisitState.New;
    }

    /// <summary>A visit state as the route tables print it.</summary>
    /// <param name="visit">The state.</param>
    /// <returns>"surveyed", "visited" or "new to you".</returns>
    public static string VisitLabel(VisitState visit) => visit switch
    {
        VisitState.Surveyed => "surveyed",
        VisitState.Visited => "visited",
        VisitState.New => "new to you",
        _ => "new to you",
    };

    /// <summary>The index of the ship's position on the route, or -1 when it is not on it.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <returns>A zero-based index into the route's stops, or -1.</returns>
    public static int HereIndex(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return RouteOf(workspace) is { } route ? IndexOfCurrent(route, workspace.Route) : -1;
    }

    /// <summary>Where the ship stands on the Spansh plot.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <returns>The line, or <see langword="null"/> without a plot.</returns>
    public static PlotProgressLine? PlotProgress(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (workspace.Plot.Route is not { } plot)
        {
            return null;
        }

        var progress = workspace.Route;
        var hereIndex = plot.IndexOf(progress.CurrentSystemAddress, progress.CurrentSystem);
        var last = plot.Stops.Count - 1;
        if (hereIndex < 0)
        {
            var lastKnown = LastKnownIndex(plot, progress);
            if (lastKnown < 0)
            {
                return new PlotProgressLine($"Off the plot. First waypoint {plot.Stops[0].Name}, {Display.Plural(plot.Jumps, "jump")} to {plot.Destination}.", plot.Stops[0].Name, -1);
            }

            if (lastKnown >= last)
            {
                return new PlotProgressLine($"Arrived at {plot.Destination}, now off the plot.", "", -1, lastKnown);
            }

            var resume = plot.Stops[lastKnown + 1];
            // One jump back to the next waypoint, then the plot's own count from there.
            return new PlotProgressLine($"Off the plot. Last known waypoint {plot.Stops[lastKnown].Name}, next {resume.Name}. ~{Display.Plural(last - lastKnown, "jump")} to {plot.Destination}.", resume.Name, -1, lastKnown);
        }

        if (hereIndex >= last)
        {
            return new PlotProgressLine($"Arrived at {plot.Destination}.", "", hereIndex);
        }

        var next = plot.Stops[hereIndex + 1];
        var remaining = last - hereIndex;
        return new PlotProgressLine($"Next waypoint {next.Name}, {Display.Plural(remaining, "jump")} to {plot.Destination}.", next.Name, hereIndex);
    }

    /// <summary>
    /// The plot waypoint the ship reached most recently since the plot was made, or -1 when it has reached none
    /// since then. A visit from before the plot does not count: a plot made from somewhere the ship is not has
    /// not been followed at all, whatever the ship saw of those systems in the past.
    /// </summary>
    private static int LastKnownIndex(PlottedRoute plot, RouteProgress progress)
    {
        var lastKnown = -1;
        var latest = DateTimeOffset.MinValue;
        for (var index = 0; index < plot.Stops.Count; index++)
        {
            var stop = plot.Stops[index];
            var visited = (stop.Id64 != 0 && progress.KnownSystems.TryGetValue(stop.Id64, out var byId) ? byId : null) ?? progress.FindByName(stop.Name);
            if (visited is not null && visited.LastVisit >= plot.PlottedAt && visited.LastVisit >= latest)
            {
                latest = visited.LastVisit;
                lastKnown = index;
            }
        }

        return lastKnown;
    }

    /// <summary>A line on fuel and the next scoopable star.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <param name="stops">The stop rows, in route order.</param>
    /// <returns>Text, or empty with neither fuel nor a route.</returns>
    public static string FuelLine(Workspace workspace, IReadOnlyList<RouteStopRow> stops)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(stops);
        var fuel = workspace.Monitor.Status?.FuelMain;
        if (stops.Count == 0)
        {
            return fuel is { } alone ? string.Create(CultureInfo.InvariantCulture, $"Fuel {alone:0.0} t.") : "";
        }

        var hereIndex = HereIndex(workspace);
        // The stop the ship is at, or the route's start when its place on the route is unknown.
        var here = stops.FirstOrDefault(row => row.Stage == StopStage.Here) ?? (hereIndex < 0 ? stops[0] : null);
        var scoopHere = here is { IsScoopable: true };
        var nextScoop = stops.Where(row => row.Stage is StopStage.Next or StopStage.Ahead).FirstOrDefault(row => row.IsScoopable);
        // Stop indexes are one-based. With the ship's place on the route unknown, the stops count from its start.
        var jumpsOn = nextScoop is null ? 0 : nextScoop.Index - Math.Max(hereIndex + 1, 1);
        var ahead = nextScoop is null
            ? scoopHere ? "no scoopable star further on this route." : "No scoopable star ahead on this route."
            : jumpsOn <= 1
                ? scoopHere ? "the next star is scoopable too." : "Next star is scoopable."
                : scoopHere
                    ? $"the next scoopable star after it is {nextScoop.System}, {Display.Plural(jumpsOn, "jump")} on."
                    : $"Next scoopable star is {nextScoop.System}, {Display.Plural(jumpsOn, "jump")} on.";
        var scoopText = scoopHere ? $"This system's arrival star is scoopable; {ahead}" : ahead;
        return fuel is { } now ? string.Create(CultureInfo.InvariantCulture, $"Fuel {now:0.0} t. {scoopText}") : scoopText;
    }

    /// <summary>How far along the route the ship is.</summary>
    /// <param name="stops">The stop rows, in route order.</param>
    /// <param name="hereIndex">The ship's index on the route, or -1.</param>
    /// <returns>"a ly of b ly travelled (n%)", the percentage rounded to a whole number, or the total alone when the ship is not on the route. Empty without a route.</returns>
    public static string TravelledLine(IReadOnlyList<RouteStopRow> stops, int hereIndex)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Count == 0)
        {
            return "";
        }

        var total = stops[^1].CumulativeLy;
        if (hereIndex < 0)
        {
            return $"{Display.LightYears(total)} in all.";
        }

        var done = stops[hereIndex].CumulativeLy;
        var percent = total > 0 ? (int)Math.Round(100 * done / total) : 100;
        return $"{Display.LightYears(done)} of {Display.LightYears(total)} travelled ({percent}%).";
    }

    /// <inheritdoc/>
    public override void Refresh()
    {
        RefreshGameRoute();
        RefreshPlotForm();
        RefreshPlot();
    }

    /// <summary>
    /// The route the ship is on, with the system the player has targeted as the next jump. With no target
    /// elsewhere this is the game's route file. A target that is the route's next stop changes nothing. One
    /// further along the route drops the stops between, since the ship will jump straight to it. One off the
    /// route, or any target when the route file is empty, makes a one-jump route from the ship to it; the
    /// plotted route it leaves is described by <see cref="DetourLine"/>. A targeted stop's position comes
    /// from the history or the Spansh plot; unknown, it is the ship's own, so its distance reads as 0.
    /// </summary>
    /// <param name="workspace">The shared state.</param>
    /// <returns>The route, or <see langword="null"/> with neither a plotted route nor a targeted system elsewhere.</returns>
    public static NavRoute? RouteOf(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var live = workspace.Monitor.Route is { Stops.Count: > 0 } plotted ? plotted : null;
        var progress = workspace.Route;
        var target = TargetElsewhere(progress);
        if (target is null)
        {
            return live;
        }

        if (live is not null)
        {
            var hereIndex = IndexOfCurrent(live, progress);
            var targetIndex = IndexOfName(live, target);
            if (targetIndex == hereIndex + 1)
            {
                return live;
            }

            if (targetIndex > hereIndex + 1)
            {
                return live with { Stops = [.. live.Stops.Take(hereIndex + 1), .. live.Stops.Skip(targetIndex)] };
            }
        }

        var here = progress.CurrentSystem!;
        var at = progress.CurrentPosition ?? new StarPosition(0, 0, 0);
        var to = PositionOf(workspace, progress, target) ?? at;
        var hereClass = workspace.System.Bodies.FirstOrDefault(body => body.IsStar && body.DistanceLs == 0)?.Class ?? "";
        return new NavRoute(
            progress.ArrivedAt ?? DateTimeOffset.MinValue,
            [
                new RouteStop(here, progress.CurrentSystemAddress, hereClass, at.X, at.Y, at.Z),
                new RouteStop(target, progress.FindByName(target)?.Address ?? 0, progress.TargetStarClass ?? "", to.X, to.Y, to.Z),
            ]);
    }

    /// <summary>
    /// One line on the plotted route a targeted detour leaves behind: where it ends and how many jumps remain,
    /// counting the ship rejoining it at its next stop after the detour.
    /// </summary>
    /// <param name="workspace">The shared state.</param>
    /// <returns>The line, or empty when the target is on the plotted route or there is no plotted route.</returns>
    public static string DetourLine(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var progress = workspace.Route;
        if (workspace.Monitor.Route is not { Stops.Count: > 1 } live || TargetElsewhere(progress) is not { } target || IndexOfName(live, target) >= 0)
        {
            return "";
        }

        var remaining = live.Stops.Count - 1 - Math.Max(IndexOfCurrent(live, progress), 0);
        return $"Detour to {target}. The plotted route to {live.Stops[^1].StarSystem} resumes after it, {Display.Plural(remaining, "jump")}.";
    }

    /// <summary>The targeted system when it is not the one the ship is in, else <see langword="null"/>.</summary>
    private static string? TargetElsewhere(RouteProgress progress)
        => progress.TargetSystem is { Length: > 0 } target && progress.CurrentSystem is { } here && !string.Equals(target, here, StringComparison.OrdinalIgnoreCase) ? target : null;

    /// <summary>Where a system is, from the history of visits or from the Spansh plot's waypoints, or <see langword="null"/>.</summary>
    private static StarPosition? PositionOf(Workspace workspace, RouteProgress progress, string name)
    {
        if (progress.FindByName(name)?.Position is { } visited)
        {
            return visited;
        }

        var plotted = workspace.Plot.Route?.Stops.FirstOrDefault(stop => string.Equals(stop.Name, name, StringComparison.OrdinalIgnoreCase));
        return plotted is null ? null : new StarPosition(plotted.X, plotted.Y, plotted.Z);
    }

    /// <summary>The last stop whose name is the given one, or -1.</summary>
    private static int IndexOfName(NavRoute route, string name)
    {
        for (var index = route.Stops.Count - 1; index >= 0; index--)
        {
            if (string.Equals(route.Stops[index].StarSystem, name, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>The last stop that is the ship's current system, by address or by name, or -1.</summary>
    private static int IndexOfCurrent(NavRoute route, RouteProgress progress)
    {
        var hereIndex = -1;
        for (var index = 0; index < route.Stops.Count; index++)
        {
            var stop = route.Stops[index];
            if ((stop.SystemAddress != 0 && stop.SystemAddress == progress.CurrentSystemAddress)
                || string.Equals(stop.StarSystem, progress.CurrentSystem, StringComparison.OrdinalIgnoreCase))
            {
                hereIndex = index;
            }
        }

        return hereIndex;
    }

    private static List<RouteStopRow> BuildBareStops(Workspace workspace)
    {
        var route = RouteOf(workspace);
        if (route is null || route.Stops.Count == 0)
        {
            return [];
        }

        var progress = workspace.Route;
        var hereIndex = HereIndex(workspace);
        var engineers = workspace.Catalog.Engineers.Values.ToLookup(engineer => engineer.System, StringComparer.OrdinalIgnoreCase);
        var rows = new List<RouteStopRow>();
        var cumulative = 0.0;
        for (var index = 0; index < route.Stops.Count; index++)
        {
            var stop = route.Stops[index];
            var jump = index > 0 ? stop.DistanceTo(route.Stops[index - 1]) : 0;
            cumulative += jump;
            var stage = StageOf(index, hereIndex);
            var engineer = engineers[stop.StarSystem].FirstOrDefault();
            rows.Add(new RouteStopRow(
                index + 1,
                workspace.Navigator.LinkTo(stop.StarSystem, PageKind.System, stop.StarSystem),
                stop.StarClass,
                StarClasses.Classify(stop.StarClass),
                VisitOf(progress, stop.SystemAddress, stop.StarSystem, stage),
                stage,
                jump,
                cumulative,
                engineer is null ? null : workspace.Navigator.LinkTo(engineer.Name, PageKind.Engineer, engineer.Name),
                null));
        }

        return rows;
    }

    private static StopStage StageOf(int index, int hereIndex, int lastKnownIndex = -1)
    {
        return hereIndex < 0
            ? lastKnownIndex < 0 ? StopStage.Ahead
                : index < lastKnownIndex ? StopStage.Done
                : index == lastKnownIndex ? StopStage.LastKnown
                : index == lastKnownIndex + 1 ? StopStage.Next
                : StopStage.Ahead
            : index < hereIndex ? StopStage.Done
            : index == hereIndex ? StopStage.Here
            : index == hereIndex + 1 ? StopStage.Next
            : StopStage.Ahead;
    }

    private static double ParseCargo(string text)
        => double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var cargo) && cargo >= 0 ? cargo : 0;

    private void RefreshGameRoute()
    {
        var stops = BuildStops(Workspace);
        Stops = Sort.Apply(stops);
        HasRoute = stops.Count > 0;
        FuelText = FuelLine(Workspace, stops);
        var hereIndex = HereIndex(Workspace);
        TravelledText = TravelledLine(stops, hereIndex);
        DetourText = DetourLine(Workspace);
        Fuel = FuelEstimate.For(Workspace, stops);
        if (stops.Count == 0)
        {
            Summary = Workspace.Route.CurrentSystem is null ? "No route plotted, and no position yet." : "No route plotted.";
            return;
        }

        var remaining = hereIndex < 0 ? stops.Count - 1 : stops.Count - 1 - hereIndex;
        Summary = hereIndex < 0
            ? $"{Display.Plural(stops.Count - 1, "jump")} plotted; the ship is not on the route yet."
            : remaining == 0
                ? "Arrived at the end of the route."
                : $"Jump {hereIndex + 1} of {stops.Count}, {remaining} to go.";
    }

    private void RefreshPlotForm()
    {
        OnPropertyChanged(nameof(PlotEnabled));
        if (PlotFrom.Length == 0 && Workspace.Route.CurrentSystem is { } here)
        {
            PlotFrom = here;
        }

        // The cargo is what was last set, typed or filled, until the plot is forgotten; with nothing set, it starts as what the ship being flown carries.
        if (PlotCargo.Length == 0 && Workspace.Preferences.Get(CargoKey) is { Length: > 0 } remembered)
        {
            PlotCargo = remembered;
        }

        if (PlotCargo.Length == 0 && Workspace.Monitor.Status?.Cargo is { } cargo)
        {
            PlotCargo = cargo.ToString("0.#", CultureInfo.InvariantCulture);
        }

        var fleet = Workspace.FleetSnapshot;
        var ships = fleet.Ships
            .Select(ship => (Ship: ship, Profile: JumpProfiles.For(ship)))
            .Where(pair => pair.Profile is not null)
            .OrderByDescending(pair => pair.Ship.IsCurrent)
            .ThenBy(pair => pair.Ship.Name, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new PlotTarget(
                PlotTarget.ShipKey(pair.Ship.ShipId),
                PlotTarget.ShipKind,
                FleetPage.ShipTitle(pair.Ship),
                pair.Ship.Type,
                ShipNames.Prefer(pair.Ship.Type, pair.Ship.TypeName),
                pair.Profile!.Range(),
                pair.Ship.IsCurrent));
        var fits = Workspace.Fleet.Fittings
            .Select(fitting => (Fitting: fitting, Profile: Workspace.Fleet.ProfileOf(fitting)))
            .Where(pair => pair.Profile is not null)
            .OrderBy(pair => pair.Fitting.Name, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new PlotTarget(PlotTarget.FitKey(pair.Fitting.Id), PlotTarget.FitKind, pair.Fitting.Name, pair.Fitting.Hull, FleetPage.HullName(fleet, pair.Fitting.Hull), pair.Profile!.Range()));
        _plotTargets = [.. ships, .. fits];
        _currentShipKey = fleet.CurrentShipId is { } currentShipId ? PlotTarget.ShipKey(currentShipId) : null;
        var withoutMasses = Workspace.Fleet.Fittings.Count(fitting => Workspace.Fleet.ProfileOf(fitting) is null);
        PlotTargetsNote = withoutMasses == 0 ? "" : $"{Display.Plural(withoutMasses, "saved fit")} {(withoutMasses == 1 ? "has" : "have")} no masses: paste {(withoutMasses == 1 ? "it" : "them")} again from EDSY to plot for {(withoutMasses == 1 ? "it" : "them")}.";
        OnPropertyChanged(nameof(HasPlotTargets));
        var wanted = _selectedPlotTarget?.Key ?? Workspace.Preferences.Get(TargetKey);
        var keep = wanted is null ? null : _plotTargets.FirstOrDefault(target => target.Key == wanted);
        _selectedPlotTarget = keep ?? _plotTargets.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedPlotTarget));
        ArrangePlotTargets();
        RefreshRange();
        PlotCommand.RaiseCanExecuteChanged();
    }

    /// <summary>Forgets the plot, and with it the chosen target and the cargo, so the form opens again on the ship being flown with what it carries.</summary>
    private void ForgetPlot()
    {
        _selectedPlotTarget = null;
        PlotCargo = "";
        Workspace.Preferences.Remove(TargetKey);
        Workspace.Preferences.Remove(CargoKey);
        Workspace.Plot.Clear();
        RefreshPlotForm();
    }

    /// <summary>Orders the picker, the chosen target, the ship being flown, then the chosen hull's fits, the ships and the other fits, and applies the filter, keeping the chosen target in.</summary>
    private void ArrangePlotTargets()
    {
        var hull = _selectedPlotTarget?.Hull ?? "";
        var filter = PlotFilter.Trim();
        PlotTargets =
        [
            .. _plotTargets
                .Where(target => filter.Length == 0 || target.Key == _selectedPlotTarget?.Key || target.Label.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(target =>
                target.Key == _selectedPlotTarget?.Key ? 0
                : target.Key == _currentShipKey ? 1
                : target.Kind == PlotTarget.FitKind && string.Equals(target.Hull, hull, StringComparison.OrdinalIgnoreCase) ? 2
                : target.Kind == PlotTarget.ShipKind ? 3
                : 4),
        ];
    }

    private void RefreshRange()
    {
        var profile = SelectedProfile();
        PlotRangeText = profile is null
            ? (Workspace.FleetSnapshot.Ships.Count == 0 && Workspace.Fleet.Fittings.Count == 0
                ? "The plot needs a ship or a fit: fly a ship with the journal watched, or save a fit from EDSY, and it appears here."
                : "The journal has not reported this ship's loadout, mass and tank yet.")
            : string.Create(CultureInfo.InvariantCulture, $"Full-tank range {profile.Range(ParseCargo(PlotCargo)):0.00} ly with {ParseCargo(PlotCargo):0.#} t of cargo. Neutron boost ×{profile.SuperchargeMultiplier}.");
    }

    private JumpProfile? SelectedProfile()
    {
        if (SelectedPlotTarget is not { } chosen)
        {
            return null;
        }

        if (chosen.Kind == PlotTarget.FitKind)
        {
            return Workspace.Fleet.Find(chosen.Id) is { } fitting ? Workspace.Fleet.ProfileOf(fitting) : null;
        }

        var ship = ShipPage.Find(Workspace.FleetSnapshot, chosen.Id);
        return ship is null ? null : JumpProfiles.For(ship);
    }

    private void RefreshPlot()
    {
        var state = Workspace.Plot;
        PlotStatus = state.Status;
        IsPlotting = state.IsBusy;
        CancelPlotCommand.RaiseCanExecuteChanged();
        ClearPlotCommand.RaiseCanExecuteChanged();
        PlotCommand.RaiseCanExecuteChanged();
        if (state.Route is not { } plot)
        {
            HasPlot = false;
            PlotSummary = "";
            PlotProgressText = "";
            NextWaypoint = "";
            OnPropertyChanged(nameof(HasNextWaypoint));
            PlotStops = [];
            return;
        }

        var progress = PlotProgress(Workspace)!;
        HasPlot = true;
        PlotSummary = string.Join(" · ",
        [
            $"{plot.Source} to {plot.Destination}",
            Display.Plural(plot.Jumps, "jump"),
            Display.LightYears(plot.TotalLy),
            Display.Plural(plot.NeutronStops, "neutron stop"),
            Display.Plural(plot.RefuelStops, "refuel stop"),
            $"plotted {plot.PlottedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)}",
        ]);
        PlotProgressText = progress.Text;
        NextWaypoint = progress.NextWaypoint;
        OnPropertyChanged(nameof(HasNextWaypoint));
        var visited = Workspace.Route;
        var rows = new List<PlotStopRow>();
        for (var index = 0; index < plot.Stops.Count; index++)
        {
            var stop = plot.Stops[index];
            var stage = StageOf(index, progress.HereIndex, progress.LastKnownIndex);
            rows.Add(new PlotStopRow(
                index + 1,
                LinkTo(stop.Name, PageKind.System, stop.Name),
                stage,
                VisitOf(visited, stop.Id64, stop.Name, stage),
                stop.JumpLy,
                stop.RemainingLy,
                stop.FuelInTank,
                stop.HasNeutron,
                stop.IsScoopable,
                stop.MustRefuel));
        }

        PlotStops = PlotSort.Apply(rows);
    }

    private bool CanPlot()
        => Workspace.Plot.Enabled && !Workspace.Plot.IsBusy && PlotFrom.Trim().Length > 0 && PlotTo.Trim().Length > 0 && SelectedPlotTarget is not null;

    private void Plot()
    {
        if (!CanPlot() || SelectedProfile() is not { } profile)
        {
            return;
        }

        Workspace.Plot.Plot(new PlotRequest(
            PlotFrom.Trim(),
            PlotTo.Trim(),
            profile,
            ParseCargo(PlotCargo),
            Enum.Parse<PlotAlgorithm>(SelectedAlgorithm.Key),
            UseSupercharge,
            UseInjections,
            ExcludeSecondary,
            RefuelEveryScoopable,
            IsSupercharged: StartsSupercharged()));
    }

    /// <summary>
    /// Whether the plot's first jump is a boosted one: the drive already holds a jet-cone boost, or the ship
    /// sits at a neutron star and the plot may use neutron stars, so the player will take the boost before
    /// leaving. The arrival star's class comes from this system's scan or from the route's stop for it.
    /// </summary>
    private bool StartsSupercharged()
    {
        var progress = Workspace.Route;
        if (progress.Supercharged)
        {
            return true;
        }

        if (!UseSupercharge)
        {
            return false;
        }

        var arrivalStar = Workspace.System.Bodies.FirstOrDefault(body => body.IsStar && body.DistanceLs == 0)?.Class
            ?? RouteOf(Workspace)?.Stops.FirstOrDefault(stop => stop.SystemAddress == progress.CurrentSystemAddress || string.Equals(stop.StarSystem, progress.CurrentSystem, StringComparison.OrdinalIgnoreCase))?.StarClass;
        return StarClasses.Classify(arrivalStar) == StarKind.Neutron;
    }
}
