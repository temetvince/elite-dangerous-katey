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
public sealed record RailStopRow(int Index, Link Link, string StarText, StopStage Stage, bool IsScoopable, Link? Engineer, string StarClass = "")
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
/// <param name="IsLanded">Whether you have landed there since the journal began, which counts its biology as collected.</param>
/// <param name="Gravity">Surface gravity as text, such as "1.60 g", or empty when unknown.</param>
/// <param name="IsHighGravity">Whether the gravity is high but not extreme, from 1.0 g.</param>
/// <param name="IsExtremeGravity">Whether the gravity is extreme, from 1.5 g, where a heavy ship may not arrest its descent.</param>
/// <param name="HasBiology">Whether the body has biological signals.</param>
/// <param name="DistanceLs">Distance from arrival in light seconds, for the nearest-first order.</param>
/// <param name="IsValuePredicted">Whether <paramref name="ExobiologyCredits"/> is predicted from the body's conditions rather than from the scanner's genera.</param>
/// <param name="BiologicalSignals">Number of biological signals.</param>
/// <param name="ShortMaterialCount">How many surface materials the plan is short of.</param>
/// <param name="TopMaterialGrade">The highest grade among the surface materials, 1 to 5, or 0 with none.</param>
/// <param name="HasFirstLogBonus">Whether <paramref name="ExobiologyCredits"/> includes the first-logged bonus, five times the base, because nobody had mapped the body.</param>
/// <param name="BaseCredits">The payout before that bonus, credits, or 0 once you have landed there.</param>
public sealed record RailBodyRow(string Name, string Signals, string Life, string Materials, bool Landable, bool HasShortMaterial, bool IsBacteriaOnly, long ExobiologyCredits, int MaterialCount, bool IsLanded, string Gravity, bool IsHighGravity, bool IsExtremeGravity, bool HasBiology, double DistanceLs, bool IsValuePredicted, int BiologicalSignals, int ShortMaterialCount, int TopMaterialGrade, bool HasFirstLogBonus, long BaseCredits)
{
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
public sealed class RailViewModel : ObservableObject
{
    private const int RouteWindow = 4;
    private const string BodySortKey = "rail.bodySort";
    private const string BiologySort = "biology";
    private const string DistanceSort = "distance";
    private const string OverlayKey = "rail.overlay";
    private const string ClickThroughKey = "rail.clickThrough";
    private const string BoundsKey = "rail.bounds";

    private readonly Workspace _workspace;
    private readonly BodyRowBuilder _bodies;
    private string? _nextStop;

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
    /// a jump on it is targeted, or the drive is charging to jump, or the ship is in hyperspace, or this
    /// system has not been surveyed yet, by honk, nav beacon or scan. A targeted jump is one the route file names next, or any target the game
    /// itself reports as on the route, since the route file can lag a replot. The rail switches by itself,
    /// so the useful section is up without a click.
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
    public string RouteRest { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets fuel and the next scoop.</summary>
    public string FuelText { get; private set => SetProperty(ref field, value); } = "";

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

    /// <summary>Gets the payout estimates, or empty.</summary>
    public string ValueText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>
    /// Gets or sets whether the bodies are ordered biology first: every body with life at the top, the most
    /// valuable first by the scanner's genera or, before mapping, by the life its conditions allow, then by
    /// signal count, with a body you have landed on at the bottom since its biology counts as collected. Off,
    /// the bodies are nearest first by distance from arrival. Off by default. Remembered.
    /// </summary>
    public bool BiologyFirst
    {
        get => _workspace.Preferences.Get(BodySortKey) == BiologySort;
        set
        {
            if (BiologyFirst != value)
            {
                _workspace.Preferences.Set(BodySortKey, value ? BiologySort : DistanceSort);
                OnPropertyChanged();
                Refresh();
            }
        }
    }

    /// <summary>Gets the bodies with materials or life, after the bacteria filter, in the chosen order.</summary>
    public IReadOnlyList<RailBodyRow> Bodies { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a note on what the list leaves out, or empty.</summary>
    public string BodiesNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets or sets whether bodies whose life is bacteria alone are hidden, by <see cref="BodyRow.IsBacteriaOnly"/>. A hidden body carrying a material the plan is short of stays listed.</summary>
    public bool HideBacteriaOnly
    {
        get => _workspace.HideBacteriaOnly;
        set
        {
            if (_workspace.HideBacteriaOnly != value)
            {
                _workspace.HideBacteriaOnly = value;
                OnPropertyChanged();
                Refresh();
            }
        }
    }

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
    /// Picks the route or the bodies. The route while another system is the selected destination, while the
    /// drive is charging to jump or while the ship is in hyperspace, by the status file's flag or by the
    /// journal's <c>StartJump</c> until its <c>FSDJump</c>; the bodies otherwise, a body or station in this
    /// system targeted or nothing at all. <c>Status.json</c> says what is targeted and drops it the moment it
    /// is cleared; without that file the journal's last target stands in, since the journal never says a
    /// target was cleared.
    /// </summary>
    private void RefreshFocus()
    {
        var progress = _workspace.Route;
        var status = _workspace.Monitor.Status;
        bool targetsAnotherSystem;
        if (status is not null)
        {
            targetsAnotherSystem = status.DestinationSystem != 0 && status.DestinationSystem != progress.CurrentSystemAddress;
        }
        else
        {
            var target = progress.TargetSystem;
            var targetIsNextStop = target is not null && _nextStop is not null && string.Equals(target, _nextStop, StringComparison.OrdinalIgnoreCase);
            var targetIsRouteJump = target is not null && progress.RemainingJumps is not null && !string.Equals(target, progress.CurrentSystem, StringComparison.OrdinalIgnoreCase);
            targetsAnotherSystem = targetIsNextStop || targetIsRouteJump;
        }

        var charging = status is { IsFsdCharging: true, IsDocked: false, IsLanded: false };
        var jumping = status is { IsInHyperspace: true } || progress.InHyperspace;
        ShowsRoute = HasRoute && (targetsAnotherSystem || charging || jumping);
        ShowsBodies = !ShowsRoute;
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
        FuelText = RoutePage.FuelLine(_workspace, stops);
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
            _nextStop = null;
            RouteHeading = "Route";
            RouteStops = [];
            RouteRest = _workspace.Route.CurrentSystem is null ? "No route plotted." : "No route plotted. Plot one in the galaxy map and it appears here.";
            return;
        }

        var hereIndex = RoutePage.HereIndex(_workspace);
        var remaining = hereIndex < 0 ? stops.Count - 1 : stops.Count - 1 - hereIndex;
        _nextStop = hereIndex >= 0 && hereIndex + 1 < stops.Count ? stops[hereIndex + 1].System : null;
        RouteHeading = remaining == 0 ? "Route · arrived" : $"Route · {Display.Plural(remaining, "jump")} left";
        var start = Math.Max(hereIndex, 0);
        var window = stops.Skip(start).Take(RouteWindow).ToList();
        RouteStops = [.. window.Select(stop => new RailStopRow(stop.Index, stop.Link, stop.StarText, stop.Stage, stop.IsScoopable, stop.Engineer, stop.StarClass))];

        var rest = stops.Skip(start + window.Count).ToList();
        var notes = new List<string>();
        var detour = RoutePage.DetourLine(_workspace);
        if (detour.Length > 0)
        {
            notes.Add(detour);
        }

        if (hereIndex < 0)
        {
            notes.Add("You are not on the route yet.");
        }

        if (rest.Count > 0)
        {
            notes.Add($"{Display.Plural(rest.Count, "more jump")} to {stops[^1].System}.");
            var scoop = rest.FirstOrDefault(stop => stop.IsScoopable);
            if (scoop is not null && !window.Any(stop => stop.IsScoopable && stop.Stage != StopStage.Here))
            {
                notes.Add($"Next scoopable star is {scoop.System}, stop {scoop.Index}.");
            }

            foreach (var stop in rest.Where(stop => stop.HasEngineer))
            {
                notes.Add($"{stop.Engineer!.Label} is at stop {stop.Index}, {stop.System}.");
            }
        }

        RouteRest = string.Join(" ", notes);
    }

    private void RefreshSystem()
    {
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
        var scan = rows.Sum(row => row.ScanCredits);
        var bio = rows.Sum(row => row.ExobiologyCredits);
        ValueText = rows.Count == 0 ? "" : $"Cartographics about {Display.Credits(scan)}{(bio > 0 ? $", exobiology about {Display.Credits(bio)}" : "")}";

        var shortMaterials = _workspace.ShortMaterials();
        var interesting = rows.Where(row => row.HasBiology || row.HasMaterials).ToList();
        var shown = interesting.Where(row => !_workspace.HideBacteriaOnly || !row.IsBacteriaOnly || row.HasShortMaterial(shortMaterials)).ToList();
        var ordered = shown.Select(row => RailRow(row, shortMaterials));
        // Biology first: every body with life, by what the life is worth, known or predicted from the conditions, then by how much of it there is; a body you have landed on last.
        Bodies = BiologyFirst
            ? [
                .. ordered
                    .OrderBy(row => row.IsLanded ? 2 : row.HasBiology ? 0 : 1)
                    .ThenByDescending(row => row.ExobiologyCredits)
                    .ThenByDescending(row => row.BiologicalSignals)
                    .ThenByDescending(row => row.HasShortMaterial)
                    .ThenByDescending(row => row.MaterialCount),
            ]
            : [.. ordered.OrderBy(row => row.DistanceLs)];
        var hiddenRows = interesting.Except(shown).ToList();
        var byPrediction = hiddenRows.Count(row => row.IsBacteriaOnlyByPrediction);
        var earlier = ScanProgress.EarlierNote(snapshot);
        BodiesNote = rows.Count == 0
            ? earlier.Length > 0 ? earlier : "Nothing scanned yet."
            : interesting.Count == 0
                ? "No scanned body has materials or life."
                : hiddenRows.Count > 0
                    ? $"{Display.Plural(hiddenRows.Count, "body", "bodies")} with only bacteria hidden{(byPrediction > 0 ? $", {byPrediction} of them unmapped but with conditions only bacteria has been found under" : "")}."
                    : "";

    }

    private static RailBodyRow RailRow(BodyRow row, IReadOnlySet<string> shortMaterials)
    {
        // Before the scanner names the genera, one short line: what could live here and what it is likely worth.
        var life = row.Genera.Count > 0
            ? string.Join("; ", row.Genera.Select(genus => genus.Confirmed.Length > 0
                ? $"{genus.Name}: {genus.Confirmed}"
                : $"{genus.Name}: {FirstCandidate(genus.Candidates)}"))
            : row.PredictedExobiologyCredits > 0
                ? $"{Display.Plural(row.PossibleGenera, "genus", "genera")} could live here, likely about {Display.Credits(row.PredictedExobiologyCredits)}{(row.FirstLogLikely ? $", {Display.Credits(row.ExobiologyRank)} with the first-logged bonus" : "")}."
                : "";
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
            row.LandedByYou ? 0 : row.ExobiologyRank,
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
            row.FirstLogLikely,
            row.LandedByYou ? 0 : row.ExobiologyBaseCredits);
    }

    private static string FirstCandidate(string candidates)
    {
        var separator = candidates.IndexOf(" · ", StringComparison.Ordinal);
        return separator < 0 ? candidates : candidates[..separator];
    }
}
