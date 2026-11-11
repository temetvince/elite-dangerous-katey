using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Pages;
using Katey.App.ViewModels;
using Katey.Journal;

namespace Katey.App.Rail;

/// <summary>One stop in the rail's route window.</summary>
/// <param name="Index">Position on the route, 1-based.</param>
/// <param name="Link">The system's page.</param>
/// <param name="StarText">Star class and what it means.</param>
/// <param name="Stage">Where it stands relative to the ship.</param>
/// <param name="IsScoopable">Whether the drive can refuel here.</param>
/// <param name="Engineer">An engineer based here, or <see langword="null"/>.</param>
/// <param name="StarClass">The star class as the journal writes it, for colouring, or empty when unknown.</param>
public sealed record RailStopRow(int Index, Link Link, string StarText, StopStage Stage, bool IsScoopable, EngineerLink? Engineer, string StarClass = "")
{
    /// <summary>Gets whether this is the ship's position.</summary>
    public bool IsHere => Stage == StopStage.Here;

    /// <summary>Gets whether this is the next jump.</summary>
    public bool IsNext => Stage == StopStage.Next;

    /// <summary>Gets "here", "next" or the stop number.</summary>
    public string Marker => Stage switch
    {
        StopStage.Here => "here",
        StopStage.Next => "next",
        StopStage.Done => Index.ToString(CultureInfo.InvariantCulture),
        StopStage.Ahead => Index.ToString(CultureInfo.InvariantCulture),
        StopStage.LastKnown => "last",
        _ => Index.ToString(CultureInfo.InvariantCulture),
    };

    /// <summary>Gets whether an engineer is based here.</summary>
    public bool HasEngineer => Engineer is not null;
}

/// <summary>What the rail's body list is for, which decides which bodies it lists and what each row shows.</summary>
public enum RailBodyView
{
    /// <summary>Every landable body, nearest first, with its life and its materials.</summary>
    Landable,

    /// <summary>Bodies with life, nearest first, their life only; the filter choice applies here.</summary>
    Biology,

    /// <summary>Bodies with surface materials, nearest first, their materials only.</summary>
    Minerals,
}

/// <summary>Which bodies with life the rail leaves out.</summary>
public enum BodyFilter
{
    /// <summary>Every body with life or materials is listed.</summary>
    All,

    /// <summary>Bodies whose life is bacteria alone are left out.</summary>
    SkipBacteria,
}

/// <summary>One body in the rail's list: a body with surface materials or biological signals.</summary>
/// <param name="Name">Short body name.</param>
/// <param name="Signals">Signal counts, or empty.</param>
/// <param name="Life">Each genus with its likeliest species, or empty before the surface scan.</param>
/// <param name="Materials">Surface materials, the ones the plan is short of first and marked, or empty.</param>
/// <param name="Landable">Whether a ship can land there.</param>
/// <param name="HasShortMaterial">Whether a material the plan is short of is on the surface.</param>
/// <param name="IsBacteriaOnly">Whether its only known life is bacteria.</param>
/// <param name="ExobiologyCredits">The expected exobiology payout still to collect: the prediction's figure, or zero once you have landed there.</param>
/// <param name="MaterialCount">How many surface materials it carries.</param>
/// <param name="IsLanded">Whether you have landed there since the journal began.</param>
/// <param name="Gravity">Surface gravity as text, such as "1.60g", or empty when unknown.</param>
/// <param name="IsHighGravity">Whether the gravity is high but not extreme, from 1.0 g.</param>
/// <param name="IsExtremeGravity">Whether the gravity is extreme, from 1.5 g, where a heavy ship may not arrest its descent.</param>
/// <param name="HasBiology">Whether the body has biological signals.</param>
/// <param name="DistanceLs">Distance from arrival in light seconds, for the nearest-first order.</param>
/// <param name="IsValuePredicted">Whether <paramref name="ExobiologyCredits"/> is predicted from the body's conditions rather than from the scanner's genera.</param>
/// <param name="BiologicalSignals">Number of biological signals.</param>
/// <param name="ShortMaterialCount">How many surface materials the plan is short of.</param>
/// <param name="TopMaterialGrade">The highest grade among the surface materials, 1 to 5, or 0 with none.</param>
/// <param name="HasFirstLogBonus">Whether <paramref name="ExobiologyCredits"/> is five times the base throughout: the first-logged bonus applies to every genus, because nobody has set foot on the body or the game said so at each sample.</param>
/// <param name="BaseCredits">The payout before that bonus, credits.</param>
/// <param name="Genera">Once probed, every genus of the body, richest first; <see langword="null"/> before the probes.</param>
/// <param name="ShowsLife">Whether the view shows the body's life: every view but Minerals.</param>
/// <param name="ShowsMaterials">Whether the view shows the body's materials: every view but Biology.</param>
public sealed record RailBodyRow(string Name, string Signals, string Life, string Materials, bool Landable, bool HasShortMaterial, bool IsBacteriaOnly, long ExobiologyCredits, int MaterialCount, bool IsLanded, string Gravity, bool IsHighGravity, bool IsExtremeGravity, bool HasBiology, double DistanceLs, bool IsValuePredicted, int BiologicalSignals, int ShortMaterialCount, int TopMaterialGrade, bool HasFirstLogBonus, long BaseCredits, IReadOnlyList<RailGenusRow>? Genera = null, bool ShowsLife = true, bool ShowsMaterials = true)
{
    /// <summary>Gets the genus rows to show, none before the probes.</summary>
    public IReadOnlyList<RailGenusRow> GenusRows => Genera ?? [];

    /// <summary>Gets whether there are genus rows to show.</summary>
    public bool HasGenera => Genera is { Count: > 0 };

    /// <summary>Gets whether the one-line life estimate shows: there is one, and the view shows life.</summary>
    public bool LifeVisible => ShowsLife && HasLife;

    /// <summary>Gets whether the genus rows show: there are some, and the view shows life.</summary>
    public bool GeneraVisible => ShowsLife && HasGenera;

    /// <summary>Gets whether the materials line shows: there is one, and the view shows materials.</summary>
    public bool MaterialsVisible => ShowsMaterials && HasMaterials;

    /// <summary>Gets the biological signal count as a short tag, such as "3 bio", or empty with none.</summary>
    public string BiologyTag => BiologicalSignals > 0 ? string.Create(CultureInfo.InvariantCulture, $"{BiologicalSignals} bio") : "";

    /// <summary>Gets whether there is a life line.</summary>
    public bool HasLife => Life.Length > 0;

    /// <summary>Gets whether there is a materials line.</summary>
    public bool HasMaterials => Materials.Length > 0;

    /// <summary>Gets the biology still to collect as short text once the scanner has named it, the first-logged bonus spelt out as "4.7M Cr x5 = 23.4M Cr"; the bio tag beside it says what it is. Empty before that, when the life line carries the prediction, and when there is none.</summary>
    public string ValueText => ExobiologyCredits > 0 && !IsValuePredicted
        ? HasFirstLogBonus ? $"{Display.Credits(BaseCredits)} x5 = {Display.Credits(ExobiologyCredits)}" : Display.Credits(ExobiologyCredits)
        : "";
}

/// <summary>
/// The live rail: where the ship is, the next few route stops, the bodies worth a look in this system,
/// what nearby covers a shortfall, and what the journal just said. Shown docked in the window or in a
/// window of its own, over the same instance.
/// </summary>
/// <summary>One genus on a probed body, as the rail lists it.</summary>
/// <param name="Name">The genus, such as "Stratum".</param>
/// <param name="Species">The sampled species with its stage, else the likeliest with its odds, without the genus word; "no prediction" when the conditions give none.</param>
/// <param name="ValueText">What the species pays, bonus included where it applies, or "value unknown".</param>
/// <param name="Stage">Samples taken of it here, 0 to 3.</param>
public sealed record RailGenusRow(string Name, string Species, string ValueText, int Stage)
{
    /// <summary>Gets whether exactly one of the three samples is taken: the species is named, the work has begun.</summary>
    public bool IsFirstSample => Stage == 1;

    /// <summary>Gets whether two of the three samples are taken: one more and it is logged.</summary>
    public bool IsSecondSample => Stage == 2;

    /// <summary>Gets whether all three samples are taken, so the species is logged and ready to sell.</summary>
    public bool IsComplete => Stage >= 3;
}

public sealed class RailViewModel : ObservableObject
{
    private const int RouteWindow = 4;
    private const string BodyViewKey = "rail.bodyView";
    private const string BodyFilterKey = "rail.bodyFilter";
    private const string OverlayKey = "rail.overlay";
    private const string ClickThroughKey = "rail.clickThrough";
    private const string BoundsKey = "rail.bounds";

    private readonly Workspace _workspace;
    private readonly BodyRowBuilder _bodies;

    /// <summary>Creates the rail.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <param name="detached">Whether the rail starts in its own window.</param>
    public RailViewModel(Workspace workspace, bool detached)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        _workspace = workspace;
        _bodies = new BodyRowBuilder(workspace.Catalog, workspace.Predictor);
        IsDetached = detached;
        SystemLink = workspace.Navigator.LinkTo("Open this system", PageKind.System);
        RouteLink = workspace.Navigator.LinkTo("Full route", PageKind.Route);
        PlotLink = workspace.Navigator.LinkTo("Spansh plot", PageKind.Route, RoutePage.PlotSection);
        DetachCommand = new RelayCommand(() => IsDetached = true);
        AttachCommand = new RelayCommand(() => IsDetached = false);
        ResetWindowCommand = new RelayCommand(() => WindowBounds = "");
        ShowViewCommand = new RelayCommand<RailBodyView>(view => View = view);
        FilterCommand = new RelayCommand<BodyFilter>(filter => Filter = filter);
        Refresh();
    }

    /// <summary>Gets or sets whether the rail lives in its own window. The window layer reacts to this.</summary>
    public bool IsDetached
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(ShowsControls));
            }
        }
    }

    /// <summary>Gets the command that moves the rail to its own window.</summary>
    public RelayCommand DetachCommand { get; }

    /// <summary>Gets the command that brings the rail back into the main window.</summary>
    public RelayCommand AttachCommand { get; }

    /// <summary>
    /// Gets or sets whether the rail's own window is an overlay: a mostly transparent ground with bright
    /// text, for sitting over the game. Remembered. Has no effect while the rail is docked.
    /// </summary>
    public bool IsOverlay
    {
        get => _workspace.Preferences.GetBool(OverlayKey, false);
        set
        {
            if (value != IsOverlay)
            {
                _workspace.Preferences.SetBool(OverlayKey, value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the rail's own window lets clicks pass through to the game under it. While it does,
    /// the rail's buttons, links and switches hide, since none could be pressed; the main window's strip turns
    /// it off again. Remembered. Has no effect while the rail is docked.
    /// </summary>
    public bool IsClickThrough
    {
        get => _workspace.Preferences.GetBool(ClickThroughKey, false);
        set
        {
            if (value != IsClickThrough)
            {
                _workspace.Preferences.SetBool(ClickThroughKey, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowsControls));
            }
        }
    }

    /// <summary>Gets whether the rail's buttons, links and switches show: always while docked, and while its window takes clicks.</summary>
    public bool ShowsControls => !IsDetached || !IsClickThrough;

    /// <summary>
    /// Gets or sets where the rail's own window last was, as <c>x,y,width,height</c> with the position in screen
    /// pixels and the size in layout units, or empty for the default place. Remembered. The window writes it
    /// after it moves or resizes and reads it when it opens.
    /// </summary>
    public string WindowBounds
    {
        get => _workspace.Preferences.Get(BoundsKey) ?? "";
        set
        {
            var next = (value ?? "").Trim();
            if (!string.Equals(next, WindowBounds, StringComparison.Ordinal))
            {
                _workspace.Preferences.Set(BoundsKey, next);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets the command that forgets where the rail's window was, which puts it back at the default place on the primary screen.</summary>
    public RelayCommand ResetWindowCommand { get; }

    /// <summary>Gets the command that picks what the body list is for; the parameter is the <see cref="RailBodyView"/>.</summary>
    public RelayCommand<RailBodyView> ShowViewCommand { get; }

    /// <summary>Gets the command that picks which bodies with life to leave out in the Biology view; the parameter is the <see cref="BodyFilter"/>.</summary>
    public RelayCommand<BodyFilter> FilterCommand { get; }

    /// <summary>Reads a <see cref="WindowBounds"/> value.</summary>
    /// <param name="text">The value.</param>
    /// <param name="x">The left edge in screen pixels.</param>
    /// <param name="y">The top edge in screen pixels.</param>
    /// <param name="width">The width in layout units.</param>
    /// <param name="height">The height in layout units.</param>
    /// <returns><see langword="false"/> for empty or unreadable text, or a size that is not positive.</returns>
    public static bool TryParseBounds(string text, out int x, out int y, out int width, out int height)
    {
        x = y = width = height = 0;
        var parts = (text ?? "").Split(',');
        return parts.Length == 4
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out y)
            && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out width)
            && int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out height)
            && width > 0
            && height > 0;
    }

    /// <summary>Gets whether the journal is being followed.</summary>
    public bool IsLive { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets a note shown when the journal is not being followed.</summary>
    public string NotLiveText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether the history is still being replayed, during which the rail shows its progress and nothing else.</summary>
    public bool IsCatchingUp { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the replay's progress line, or empty once it is done.</summary>
    public string CatchUpText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets how far the replay has come, 0 to 1 by files read.</summary>
    public double CatchUpFraction { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets whether the replay runs without a file count yet, so the bar sweeps instead of filling.</summary>
    public bool IsCatchUpIndeterminate { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the system the ship is in, or a placeholder.</summary>
    public string SystemName { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the current system's page.</summary>
    public Link SystemLink { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets ship, state and time, such as "Ironic · Mandalay · in supercruise · 19:42".</summary>
    public string PositionDetail { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether a route is plotted.</summary>
    public bool HasRoute { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets whether the rail is showing the route rather than this system's bodies: a route is plotted, and
    /// the drive is charging to jump or the ship is in hyperspace. From arrival until the next jump the
    /// bodies show, with the route kept to <see cref="RouteSummaryParts"/> above them. The rail switches by
    /// itself, so the useful section is up without a click.
    /// </summary>
    public bool ShowsRoute { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets whether the rail is showing this system's bodies and what is short here; the opposite of <see cref="ShowsRoute"/>.</summary>
    public bool ShowsBodies { get; private set => SetProperty(ref field, value); } = true;

    /// <summary>Gets the route heading, such as "Route · 3 jumps left".</summary>
    public string RouteHeading { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the route page.</summary>
    public Link RouteLink { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the stop you are at and the next few.</summary>
    public IReadOnlyList<RailStopRow> RouteStops { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets one line on the rest of the route: how much more, the next scoopable star, engineer stops, the end.</summary>
    public IReadOnlyList<PhrasePart> RouteRestParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>
    /// Gets the one line on the route shown above the bodies: the next jump as a link and how many jumps
    /// are left to the destination, or that the route is done or not yet joined. Empty without a route.
    /// </summary>
    public IReadOnlyList<PhrasePart> RouteSummaryParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets fuel and the next scoop.</summary>
    public IReadOnlyList<PhrasePart> FuelParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets how far along the route the ship is, such as "4,002 ly of 7,320 ly travelled".</summary>
    public string TravelledText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the fuel picture: the tank, what the next jump burns, and how far the route gets without a scoop. <see cref="FuelEstimate.None"/> until a refresh.</summary>
    public FuelEstimate Fuel { get; private set => SetProperty(ref field, value); } = FuelEstimate.None;

    /// <summary>Gets whether a Spansh plot is held.</summary>
    public bool HasPlot { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets one line on the plot: the next waypoint and how many remain, or that the ship has arrived.</summary>
    public string PlotText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the next waypoint's name, for the clipboard, or empty.</summary>
    public string NextWaypoint { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether there is a next waypoint to copy.</summary>
    public bool HasNextWaypoint => NextWaypoint.Length > 0;

    /// <summary>Gets the route page opened on its plot section.</summary>
    public Link PlotLink { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the system heading, such as "This system · 9 of 12 scanned".</summary>
    public string SystemHeading { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets this system's scan progress alone, as <see cref="ScanProgress.Heading"/> words it, or empty before anything is known.</summary>
    public string ScanText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the share of this system's bodies the game knows, 0 to 1, for the scan ring. 0 until the body count is known.</summary>
    public double ScanFraction { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets whether every body in this system is scanned, which turns the heading and the ring green.</summary>
    public bool IsFullyScanned { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets what the system's data still aboard would pay, the first-discovery and first-logged bonuses counted where they apply, or empty; see <see cref="SystemValue.UnsoldText"/>.</summary>
    public string ValueText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>
    /// Gets or sets what the body list is for: every landable body, the bodies with life, or the bodies with
    /// surface materials, each nearest first. Landable by default. Remembered.
    /// </summary>
    public RailBodyView View
    {
        get => _workspace.Preferences.Get(BodyViewKey) switch
        {
            "biology" => RailBodyView.Biology,
            "minerals" => RailBodyView.Minerals,
            _ => RailBodyView.Landable,
        };
        set
        {
            if (View != value)
            {
                var chosen = value switch
                {
                    RailBodyView.Landable => "landable",
                    RailBodyView.Minerals => "minerals",
                    RailBodyView.Biology => "biology",
                    _ => "biology",
                };
                _workspace.Preferences.Set(BodyViewKey, chosen);
                Log.RailViewChanged(_workspace.Logger, chosen);
                OnPropertyChanged();
                RefreshSystem();
            }
        }
    }

    /// <summary>Gets whether every landable body is listed; <see cref="ShowViewCommand"/> picks the view.</summary>
    public bool ShowsLandable => View == RailBodyView.Landable;

    /// <summary>Gets whether the bodies with life are listed; <see cref="ShowViewCommand"/> picks the view.</summary>
    public bool ShowsBiology => View == RailBodyView.Biology;

    /// <summary>Gets whether the bodies with surface materials are listed; <see cref="ShowViewCommand"/> picks the view.</summary>
    public bool ShowsMinerals => View == RailBodyView.Minerals;

    /// <summary>Gets whether the filter choice shows: only in the Biology view, where it applies.</summary>
    public bool IsBiologyView => View == RailBodyView.Biology;

    /// <summary>Gets the bodies the view is for, nearest first, after the filter in the Biology view.</summary>
    public IReadOnlyList<RailBodyRow> Bodies { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a note on what the list leaves out, or empty.</summary>
    public string BodiesNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>
    /// Gets or sets which bodies with life the list leaves out: none, or the bacteria-only ones. Applies in the
    /// Biology view. Remembered; a saved value that names neither reads as <see cref="BodyFilter.All"/>.
    /// </summary>
    public BodyFilter Filter
    {
        get => _workspace.Preferences.Get(BodyFilterKey) switch
        {
            "bacteria" => BodyFilter.SkipBacteria,
            _ => BodyFilter.All,
        };
        set
        {
            if (Filter != value)
            {
                _workspace.Preferences.Set(BodyFilterKey, value switch
                {
                    BodyFilter.SkipBacteria => "bacteria",
                    BodyFilter.All => "all",
                    _ => "all",
                });
                var chosen = value.ToString();
                Log.RailFilterChanged(_workspace.Logger, chosen);
                OnPropertyChanged();
                RefreshSystem();
            }
        }
    }

    /// <summary>Gets whether every body with life is listed; <see cref="FilterCommand"/> picks the choice.</summary>
    public bool ShowsAll => Filter == BodyFilter.All;

    /// <summary>Gets whether bodies whose life is bacteria alone are left out; <see cref="FilterCommand"/> picks the choice.</summary>
    public bool SkipsBacteria => Filter == BodyFilter.SkipBacteria;

    /// <summary>Rebuilds every section from the trackers.</summary>
    public void Refresh()
    {
        IsCatchingUp = _workspace.IsCatchingUp;
        CatchUpText = _workspace.CatchUpText;
        CatchUpFraction = _workspace.CatchUpFraction;
        IsCatchUpIndeterminate = _workspace.IsCatchUpIndeterminate;
        RefreshPosition();
        RefreshRoute();
        RefreshSystem();
        RefreshFocus();
    }

    /// <summary>
    /// The one route line above the bodies. The destination is the route file's last stop when there is a
    /// file, since a re-plot through a targeted system keeps the destination while the stops to it are not
    /// written yet; otherwise it is the built route's end, the one targeted jump.
    /// </summary>
    private IReadOnlyList<PhrasePart> RouteSummary(IReadOnlyList<RouteStopRow> stops, int hereIndex, int remaining)
    {
        Link System(string name) => _workspace.Navigator.LinkTo(name, PageKind.System, name);
        var end = _workspace.Monitor.Route is { Stops.Count: > 0 } live ? live.Stops[^1].StarSystem : stops[^1].System;
        if (hereIndex < 0)
        {
            return Phrase.Of($"Route of {Display.Plural(stops.Count - 1, "jump")} to ", System(end), "; you are not on it yet.");
        }

        if (remaining == 0 || hereIndex + 1 >= stops.Count)
        {
            return Phrase.Text("At the end of the route.");
        }

        var next = stops[hereIndex + 1].System;
        return remaining == 1
            ? Phrase.Of("Next jump ", System(next), ", the last.")
            : Phrase.Of("Next jump ", System(next), $", {Display.Plural(remaining, "jump")} left to ", System(end), ".");
    }

    /// <summary>
    /// Picks the route or the bodies. The route while the drive is charging to jump or the ship is in
    /// hyperspace; the bodies otherwise, from the moment of arrival. A charge counts only in supercruise,
    /// where it can be nothing but a jump: in normal space the same flag charges for supercruise, so there
    /// the rail waits for the journal's <c>StartJump</c> with a hyperspace jump type, written as the
    /// countdown begins, and that holds until its <c>FSDJump</c>. What is targeted does not count: the game
    /// targets the next jump by itself on every arrival along a route, so a targeted system says nothing
    /// about whether you are exploring here. The route stays in one line above the bodies meanwhile.
    /// </summary>
    private void RefreshFocus()
    {
        var progress = _workspace.Route;
        var status = _workspace.Monitor.Status;
        var charging = status is { IsFsdCharging: true, IsInSupercruise: true };
        var jumping = status is { IsInHyperspace: true } || progress.InHyperspace;
        var before = ShowsRoute;
        ShowsRoute = HasRoute && (charging || jumping);
        ShowsBodies = !ShowsRoute;
        if (ShowsRoute != before)
        {
            var reason = !HasRoute ? "no route" : charging ? "the drive is charging" : jumping ? "in hyperspace" : "arrived";
            Log.RailSectionChanged(_workspace.Logger, ShowsRoute ? "route" : "bodies", reason);
        }
    }

    private void RefreshPosition()
    {
        var state = _workspace.Monitor.State;
        IsLive = state is MonitorState.Watching or MonitorState.CatchingUp;
        NotLiveText = state switch
        {
            MonitorState.FolderMissing => "The journal folder does not exist. Fix it in Settings.",
            MonitorState.Stopped => "Not watching the journal. Start it in Settings.",
            MonitorState.CatchingUp => "",
            MonitorState.Watching => "",
            _ => "",
        };

        var route = _workspace.Route;
        var fleet = _workspace.FleetSnapshot;
        var status = _workspace.Monitor.Status;
        SystemName = route.CurrentSystem ?? "No position yet";
        SystemLink = _workspace.Navigator.LinkTo("Open this system", PageKind.System);
        var parts = new List<string>();
        var ship = fleet.Ships.FirstOrDefault(ship => ship.IsCurrent);
        if (ship is not null)
        {
            var hull = Core.Fleet.ShipNames.Prefer(ship.Type, ship.TypeName);
            parts.Add(ship.Name.Length > 0 ? ship.Name : hull);
            if (ship.Name.Length > 0)
            {
                parts.Add(hull);
            }
        }

        if (route.InHyperspace)
        {
            parts.Add("in hyperspace");
        }
        else if (status is not null)
        {
            parts.Add(status.IsDocked ? "docked" : status.IsLanded ? "landed" : status.IsInSupercruise ? (status.IsScooping ? "scooping" : "in supercruise") : "in normal space");
        }

        if (status is not null)
        {
            parts.Add(Display.Clock(status.Timestamp));
        }

        PositionDetail = string.Join(" · ", parts);
    }

    private void RefreshRoute()
    {
        RouteLink = _workspace.Navigator.LinkTo("Full route", PageKind.Route);
        var stops = RoutePage.BuildStops(_workspace);
        HasRoute = stops.Count > 0;
        FuelParts = RoutePage.FuelLine(_workspace, stops);
        TravelledText = RoutePage.TravelledLine(stops, RoutePage.HereIndex(_workspace));
        Fuel = FuelEstimate.For(_workspace, stops);
        PlotLink = _workspace.Navigator.LinkTo("Spansh plot", PageKind.Route, RoutePage.PlotSection);
        var progress = RoutePage.PlotProgress(_workspace);
        HasPlot = progress is not null;
        PlotText = progress?.Text ?? "";
        NextWaypoint = progress?.NextWaypoint ?? "";
        OnPropertyChanged(nameof(HasNextWaypoint));
        if (stops.Count == 0)
        {
            RouteHeading = "Route";
            RouteStops = [];
            RouteSummaryParts = Phrase.Empty;
            RouteRestParts = Phrase.Text(_workspace.Route.CurrentSystem is null ? "No route plotted." : "No route plotted. Plot one in the galaxy map and it appears here.");
            return;
        }

        var hereIndex = RoutePage.HereIndex(_workspace);
        // Off the route file, the game's own count stands in for the file's: it has re-plotted and the file lags.
        var remaining = RoutePage.RerouteJumps(_workspace) ?? (hereIndex < 0 ? stops.Count - 1 : stops.Count - 1 - hereIndex);
        RouteHeading = remaining == 0 ? "Route · arrived" : $"Route · {Display.Plural(remaining, "jump")} left";
        RouteSummaryParts = RouteSummary(stops, hereIndex, remaining);
        var start = Math.Max(hereIndex, 0);
        var window = stops.Skip(start).Take(RouteWindow).ToList();
        RouteStops = [.. window.Select(stop => new RailStopRow(stop.Index, stop.Link, stop.StarText, stop.Stage, stop.IsScoopable, stop.Engineer, stop.StarClass))];

        var rest = stops.Skip(start + window.Count).ToList();
        var notes = new List<IReadOnlyList<PhrasePart>>();
        var reroute = RoutePage.RerouteLine(_workspace);
        if (reroute.Count > 0)
        {
            notes.Add(reroute);
        }

        Link System(string name) => _workspace.Navigator.LinkTo(name, PageKind.System, name);
        if (hereIndex < 0)
        {
            notes.Add(Phrase.Text("You are not on the route yet."));
        }

        if (rest.Count > 0)
        {
            notes.Add(Phrase.Of($"{Display.Plural(rest.Count, "more jump")} to ", System(stops[^1].System), "."));
            var scoop = rest.FirstOrDefault(stop => stop.IsScoopable);
            if (scoop is not null && !window.Any(stop => stop.IsScoopable && stop.Stage != StopStage.Here))
            {
                notes.Add(Phrase.Of("Next scoopable star is ", System(scoop.System), $", stop {scoop.Index}."));
            }

            foreach (var stop in rest.Where(stop => stop.HasEngineer))
            {
                notes.Add(Phrase.Of(stop.Engineer, $" is at stop {stop.Index}, ", System(stop.System), "."));
            }
        }

        RouteRestParts = Phrase.Join(" ", notes);
    }

    private void RefreshSystem()
    {
        // The choices are read from the preferences, so every view of them is told whenever the system is refreshed,
        // whether or not a system is known yet: a choice restored at start, before the journal is read, must show.
        OnPropertyChanged(nameof(ShowsLandable));
        OnPropertyChanged(nameof(ShowsBiology));
        OnPropertyChanged(nameof(ShowsMinerals));
        OnPropertyChanged(nameof(IsBiologyView));
        OnPropertyChanged(nameof(ShowsAll));
        OnPropertyChanged(nameof(SkipsBacteria));
        var snapshot = _workspace.System;
        if (snapshot.SystemName is null)
        {
            SystemHeading = "This system";
            ScanText = "";
            ScanFraction = 0;
            IsFullyScanned = false;
            ValueText = "";
            Bodies = [];
            BodiesNote = "Bodies appear as you scan them.";
            return;
        }

        ScanText = ScanProgress.Heading(snapshot);
        SystemHeading = ScanText.Length == 0 ? "This system" : $"This system · {ScanText}";
        ScanFraction = ScanProgress.Fraction(snapshot);
        IsFullyScanned = snapshot.BodyCount is > 0 && ScanFraction >= 1;
        var rows = _bodies.Build(snapshot);
        ValueText = rows.Count == 0 ? "" : SystemValue.From(snapshot, rows, _workspace.Sold).UnsoldText();

        var shortMaterials = _workspace.ShortMaterials();
        var view = View;
        var candidates = view switch
        {
            RailBodyView.Landable => rows.Where(row => row.Landable).ToList(),
            RailBodyView.Minerals => [.. rows.Where(row => row.HasMaterials)],
            RailBodyView.Biology => [.. rows.Where(row => row.HasBiology)],
            _ => [.. rows.Where(row => row.HasBiology)],
        };
        var shown = view == RailBodyView.Biology ? [.. candidates.Where(row => !Hidden(row))] : candidates;
        Bodies = [.. shown.OrderBy(row => row.DistanceLs).Select(row => RailRow(row, shortMaterials, view))];
        var hiddenRows = candidates.Except(shown).ToList();
        var earlier = ScanProgress.EarlierNote(snapshot);
        BodiesNote = rows.Count == 0
            ? earlier.Length > 0 ? earlier : "Nothing scanned yet."
            : candidates.Count == 0
                ? view switch
                {
                    RailBodyView.Landable => "No scanned body is landable.",
                    RailBodyView.Minerals => "No scanned body has surface materials.",
                    RailBodyView.Biology => "No scanned body has life.",
                    _ => "No scanned body has life.",
                }
                : HiddenNote(hiddenRows);
    }

    /// <summary>Whether the filter hides the body: bacteria alone under <see cref="BodyFilter.SkipBacteria"/>.</summary>
    private bool Hidden(BodyRow row) => Filter == BodyFilter.SkipBacteria && row.IsBacteriaOnly;

    /// <summary>One line on what the filter hid, or empty.</summary>
    private static string HiddenNote(List<BodyRow> hiddenRows)
    {
        if (hiddenRows.Count == 0)
        {
            return "";
        }

        var byPrediction = hiddenRows.Count(row => row.IsBacteriaOnlyByPrediction);
        return $"{Display.Plural(hiddenRows.Count, "body", "bodies")} with only bacteria hidden{(byPrediction > 0 ? $", {byPrediction} of them unmapped but with conditions only bacteria has been found under" : "")}.";
    }

    private static RailBodyRow RailRow(BodyRow row, IReadOnlySet<string> shortMaterials, RailBodyView view)
    {
        // Before the scanner names the genera, one short line: what could live here and what it is likely worth.
        // Once it has, a list: each genus richest first with its likeliest species, and the pace under it.
        var life = row.Genera.Count == 0 && row.PredictedExobiologyCredits > 0
            ? $"{Display.Plural(row.PossibleGenera, "genus", "genera")} could live here, likely about {Display.Credits(row.PredictedExobiologyCredits)}{(row.FirstLogLikely ? $" x{BodyRow.FirstLogMultiplier} = {Display.Credits(row.ExobiologyRank)} with the first-logged bonus" : "")}."
            : "";
        var ordered = row.Genera.OrderByDescending(genus => genus.ExpectedCredits).ThenBy(genus => genus.Name, StringComparer.Ordinal).ToList();
        var listed = ordered.Select(genus => new RailGenusRow(
            genus.Name,
            genus.Stage > 0
                ? string.Create(CultureInfo.InvariantCulture, $"{genus.TopSpecies}, {genus.Stage} of 3 sampled")
                : genus.TopSpecies.Length == 0 ? "No prediction" : string.Create(CultureInfo.InvariantCulture, $"{genus.TopSpecies} {genus.TopProbability * 100:0}%"),
            genus.ExpectedCredits > 0 ? Display.Credits(genus.CreditsWithBonus) : "value unknown",
            genus.Stage)).ToList();
        var shortHere = row.MaterialNames.Where(shortMaterials.Contains).ToList();
        var others = row.MaterialNames.Where(name => !shortMaterials.Contains(name)).ToList();
        var materials = string.Join(", ", shortHere.Select(name => name + " (needed)").Concat(others));
        return new RailBodyRow(
            row.Name,
            row.Signals,
            life,
            materials,
            row.Landable,
            shortHere.Count > 0,
            row.IsBacteriaOnly,
            row.ExobiologyRank,
            row.MaterialNames.Count,
            row.LandedByYou,
            row.GravityText,
            row.IsHighGravity,
            row.IsExtremeGravity,
            row.HasBiology,
            row.DistanceLs,
            row.IsExobiologyPredicted,
            row.BiologicalSignals,
            shortHere.Count,
            row.TopMaterialGrade,
            row.HasFirstLogBonusThroughout,
            row.ExobiologyBaseCredits,
            row.Genera.Count > 0 ? listed : null,
            view != RailBodyView.Minerals,
            view != RailBodyView.Biology);
    }
}
