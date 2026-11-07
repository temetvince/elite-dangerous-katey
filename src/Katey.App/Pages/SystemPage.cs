using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.ViewModels;
using Katey.Core.Exploration;

namespace Katey.App.Pages;

/// <summary>
/// A star system. For the system the ship is in: every body scanned, with signals, materials and what each
/// is worth. For any other system the player has visited: where it is and when they were there.
/// </summary>
public sealed class SystemPage : Page
{
    private readonly string _requested;

    /// <summary>Creates the page.</summary>
    /// <param name="name">System name, or empty for the current system.</param>
    /// <param name="workspace">The shared state.</param>
    public SystemPage(string name, Workspace workspace)
        : base(new PageKey(PageKind.System, name), workspace)
    {
        _requested = name;
        Kicker = "System";
        Sort = new TableSort<BodyRow>(
            [
                new("name", "Body", row => row.Name),
                new("class", "Class", row => row.Class),
                new("distance", "Distance", row => row.DistanceLs),
                new("scan", "Cartographics", row => row.ScanCredits, DescendingFirst: true),
                // Landable bodies first, then the rail's biology metric: the payout, known from the scanner or predicted from the conditions, then the signal count.
                new("bio", "Landable", row => (row.Landable, row.ExobiologyRank, row.BiologicalSignals), DescendingFirst: true),
            ],
            "distance",
            workspace.Preferences,
            "sort.bodies");
        Sort.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Gets the body table's sort. The default, by distance, keeps stars first.</summary>
    public TableSort<BodyRow> Sort { get; }

    /// <summary>Gets whether this is the system the ship is in, which is the only one with body data.</summary>
    public bool IsCurrent { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets one line on scan progress.</summary>
    public string Progress { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the body the ship is at, or empty.</summary>
    public string CurrentBody { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the bodies, stars first then by distance.</summary>
    public IReadOnlyList<BodyRow> Bodies { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a line summing the estimated scan and exobiology value, or empty.</summary>
    public string ValueSummary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the system's name, for the copy button, or empty before the journal has reported a position.</summary>
    public string Name { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether there is a name to copy.</summary>
    public bool HasName => Name.Length > 0;

    /// <summary>Gets or sets one line on the last copy, or empty. The window sets it after the clipboard answers.</summary>
    public string CopyStatus { get; set => SetProperty(ref field, value); } = "";

    /// <summary>
    /// Gets a line on discovery in this system: how many scanned bodies nobody had discovered before you, how
    /// many you were first to map, and how many you have landed on. Empty away from the current system.
    /// </summary>
    public string DiscoveryText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets a line on where the system is relative to the ship and when you were last there.</summary>
    public string WhereText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets engineers based in this system.</summary>
    public IReadOnlyList<Link> Engineers { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether any engineer is based here.</summary>
    public bool HasEngineers => Engineers.Count > 0;

    /// <summary>Gets your ships and stored modules here, as text, or empty.</summary>
    public string FleetText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>One line on discovery across the scanned bodies.</summary>
    /// <param name="rows">The scanned bodies.</param>
    /// <returns>The line, or empty when nothing is scanned.</returns>
    public static string DiscoveryLine(IReadOnlyList<BodyRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            return "";
        }

        var undiscovered = rows.Count(row => row.FirstDiscovery);
        var firstMapped = rows.Count(row => row.MappedByYou && row.FirstMapping);
        var landed = rows.Count(row => row.LandedByYou);
        var parts = new List<string>
        {
            undiscovered == rows.Count
                ? $"Nobody had discovered any of the {Display.Plural(rows.Count, "scanned body", "scanned bodies")} before you"
                : undiscovered == 0
                    ? $"Every one of the {Display.Plural(rows.Count, "scanned body", "scanned bodies")} was discovered before you"
                    : $"{undiscovered} of the {Display.Plural(rows.Count, "scanned body", "scanned bodies")} had never been discovered before you",
        };
        if (firstMapped > 0)
        {
            parts.Add($"you were first to map {firstMapped}");
        }

        if (landed > 0)
        {
            parts.Add($"you have landed on {landed}");
        }

        var landable = rows.Count(row => row.Landable && !row.FromCatalogue);
        var untrodden = rows.Count(row => row.Landable && !row.FromCatalogue && row.FirstFootfall);
        if (untrodden > 0)
        {
            parts.Add(untrodden == landable
                ? $"nobody has set foot on any of its {Display.Plural(landable, "landable body", "landable bodies")}"
                : $"nobody has set foot on {untrodden} of its {Display.Plural(landable, "landable body", "landable bodies")}");
        }

        return string.Join("; ", parts) + ".";
    }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var route = Workspace.Route;
        var snapshot = Workspace.System;
        var name = _requested.Length == 0 ? snapshot.SystemName ?? route.CurrentSystem : _requested;
        Title = name ?? "No position yet";
        Name = name ?? "";
        OnPropertyChanged(nameof(HasName));
        IsCurrent = name is not null && string.Equals(name, snapshot.SystemName, StringComparison.OrdinalIgnoreCase);

        if (name is null)
        {
            Progress = "The journal has not reported where you are.";
            Bodies = [];
            ValueSummary = "";
            DiscoveryText = "";
            WhereText = "";
            Engineers = [];
            FleetText = "";
            OnPropertyChanged(nameof(HasEngineers));
            return;
        }

        if (IsCurrent)
        {
            CurrentBody = snapshot.CurrentBody is { } body ? BodyRowBuilder.ShortName(body, snapshot.SystemName) : "";
            var earlier = ScanProgress.EarlierNote(snapshot);
            Progress = earlier.Length > 0 ? $"{ScanProgress.Sentence(snapshot)}. {earlier}" : $"{ScanProgress.Sentence(snapshot)}.";
            var rows = new BodyRowBuilder(Workspace.Catalog, Workspace.Predictor).Build(snapshot);
            Bodies = Sort.ActiveKey == "distance" && !Sort.Descending ? rows : Sort.Apply(rows);
            var scan = rows.Sum(row => row.ScanCredits);
            var bio = rows.Sum(row => row.ExobiologyCredits);
            ValueSummary = rows.Count == 0 ? "" : $"Cartographics about {Display.Credits(scan)} as scanned{(bio > 0 ? $", exobiology about {Display.Credits(bio)}" : "")}.";
            DiscoveryText = DiscoveryLine(rows);
            WhereText = "You are here.";
        }
        else
        {
            CurrentBody = "";
            Progress = "";
            Bodies = [];
            ValueSummary = "";
            DiscoveryText = "";
            var known = route.FindByName(name);
            var engineerHere = Workspace.Catalog.Engineers.Values.FirstOrDefault(engineer => string.Equals(engineer.System, name, StringComparison.OrdinalIgnoreCase));
            var position = known?.Position is { } visited ? new GalacticPosition(visited.X, visited.Y, visited.Z) : engineerHere?.Position;
            var distance = position is { } point ? Workspace.DistanceFromHere(point) : null;
            var parts = new List<string>();
            if (distance is { } ly)
            {
                parts.Add($"{Display.LightYears(ly)} from {route.CurrentSystem}.");
            }

            parts.Add(known is null
                ? "You have not been here since the journal began."
                : string.Create(CultureInfo.CurrentCulture, $"Last there {known.LastVisit.ToLocalTime():g}, {Display.Plural(known.Visits, "visit")} in all."));
            WhereText = string.Join(" ", parts);
        }

        Engineers = [.. Workspace.Catalog.Engineers.Values.Where(engineer => string.Equals(engineer.System, name, StringComparison.OrdinalIgnoreCase)).Select(engineer => LinkTo(engineer.Name, PageKind.Engineer, engineer.Name))];
        OnPropertyChanged(nameof(HasEngineers));

        var fleet = Workspace.FleetSnapshot;
        var ships = fleet.Ships.Count(ship => string.Equals(ship.System, name, StringComparison.OrdinalIgnoreCase));
        var stored = fleet.StoredModules.Count(module => string.Equals(module.System, name, StringComparison.OrdinalIgnoreCase));
        FleetText = ships == 0 && stored == 0 ? "" : $"You have {Display.Plural(ships, "ship")} and {Display.Plural(stored, "stored module")} here.";
    }
}
