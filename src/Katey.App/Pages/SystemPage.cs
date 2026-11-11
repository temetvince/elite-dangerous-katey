using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
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
        Kicker = Strings.Get(Keys.System_Kicker);
        Sort = new TableSort<BodyRow>(
            [
                new("name", Strings.Get(Keys.Column_Body), row => row.Name),
                new("class", Strings.Get(Keys.Column_Class), row => row.Class),
                new("distance", Strings.Get(Keys.Column_Distance), row => row.DistanceLs),
                new("scan", Strings.Get(Keys.Column_Cartographics), row => row.ScanCredits, DescendingFirst: true),
                // Landable bodies first, then the rail's biology metric: the payout, known from the scanner or predicted from the conditions, then the signal count.
                new("bio", Strings.Get(Keys.Column_Landable), row => (row.Landable, row.ExobiologyRank, row.BiologicalSignals), DescendingFirst: true),
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

    /// <summary>Gets the note on scans the journal holds no details for, or that it has not said where you are; empty otherwise. The scan count itself is on <see cref="WhereParts"/>.</summary>
    public string Progress { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the body the ship is at, or empty.</summary>
    public string CurrentBody { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the bodies, stars first then by distance.</summary>
    public IReadOnlyList<BodyRow> Bodies { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the line on what of this system's data is still aboard, what of it is for being first and what has sold, or empty; see <see cref="SystemValue.UnsoldLine"/>.</summary>
    public string UnsoldSummary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets a line summing the estimated scan and exobiology value, or empty; see <see cref="SystemValue.Line"/>.</summary>
    public string ValueSummary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the system's name, for the copy button, or empty before the journal has reported a position.</summary>
    public string Name { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether there is a name to copy.</summary>
    public bool HasName => Name.Length > 0;

    /// <summary>Gets or sets one line on the last copy, or empty. The window sets it after the clipboard answers.</summary>
    public string CopyStatus { get; set => SetProperty(ref field, value); } = "";

    /// <summary>
    /// Gets a line on discovery in this system: how many scanned bodies nobody had discovered before you, how
    /// many you have landed on, and how many landable bodies nobody has set foot on yet. Empty away from the
    /// current system.
    /// </summary>
    public string DiscoveryText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets a line on where the system is relative to the ship, the system you are in as a link, and when you were last there; for the system you are in, "You are here." and the scan count.</summary>
    public IReadOnlyList<PhrasePart> WhereParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets engineers based in this system.</summary>
    public IReadOnlyList<EngineerLink> Engineers { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether any engineer is based here.</summary>
    public bool HasEngineers => Engineers.Count > 0;

    /// <summary>Gets your ships and stored modules here, as text, or empty.</summary>
    public string FleetText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>
    /// One line on discovery across the scanned bodies: how many nobody had discovered before you, how many you
    /// have landed on, and how many landable bodies nobody has set foot on, you included. Your own firsts are
    /// each body's to tell, in its status detail.
    /// </summary>
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
        var landed = rows.Count(row => row.LandedByYou);
        var scanned = Display.Plural(Keys.Plural_ScannedBodies, rows.Count);
        var parts = new List<string>
        {
            undiscovered == rows.Count
                ? Strings.Format(Keys.System_NobodyDiscoveredAny, ("bodies", scanned))
                : undiscovered == 0
                    ? Strings.Format(Keys.System_EveryoneDiscoveredAll, ("bodies", scanned))
                    : Strings.Format(Keys.System_SomeUndiscovered, ("undiscovered", undiscovered), ("bodies", scanned)),
        };
        if (landed > 0)
        {
            parts.Add(Strings.Format(Keys.System_YouLandedOn, ("bodies", Display.Plural(Keys.Plural_Bodies, landed))));
        }

        var landable = rows.Count(row => row.Landable && !row.FromCatalogue);
        var untrodden = rows.Count(row => row.FootfallOpen);
        if (untrodden > 0)
        {
            var landableBodies = Display.Plural(Keys.Plural_LandableBodies, landable);
            parts.Add(untrodden == landable
                ? Strings.Format(Keys.System_NoFootfallAny, ("bodies", landableBodies))
                : Strings.Format(Keys.System_NoFootfallSome, ("untrodden", untrodden), ("bodies", landableBodies)));
        }

        return string.Join(Strings.Get(Keys.List_Clause), parts) + ".";
    }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var route = Workspace.Route;
        var snapshot = Workspace.System;
        var name = _requested.Length == 0 ? snapshot.SystemName ?? route.CurrentSystem : _requested;
        Title = name ?? Strings.Get(Keys.System_NoPositionYet);
        Name = name ?? "";
        OnPropertyChanged(nameof(HasName));
        IsCurrent = name is not null && string.Equals(name, snapshot.SystemName, StringComparison.OrdinalIgnoreCase);

        if (name is null)
        {
            Progress = Strings.Get(Keys.System_NotReported);
            Bodies = [];
            UnsoldSummary = "";
            ValueSummary = "";
            DiscoveryText = "";
            WhereParts = Phrase.Empty;
            Engineers = [];
            FleetText = "";
            OnPropertyChanged(nameof(HasEngineers));
            return;
        }

        if (IsCurrent)
        {
            CurrentBody = snapshot.CurrentBody is { } body ? BodyRowBuilder.ShortName(body, snapshot.SystemName) : "";
            Progress = ScanProgress.EarlierNote(snapshot);
            var rows = new BodyRowBuilder(Workspace.Catalog, Workspace.Predictor).Build(snapshot);
            Bodies = Sort.ActiveKey == "distance" && !Sort.Descending ? rows : Sort.Apply(rows);
            var value = rows.Count == 0 ? null : SystemValue.From(snapshot, rows, Workspace.Sold);
            UnsoldSummary = value?.UnsoldLine() ?? "";
            ValueSummary = value?.Line() ?? "";
            DiscoveryText = DiscoveryLine(rows);
            WhereParts = Phrase.Text(Strings.Format(Keys.System_YouAreHere, ("scan", ScanProgress.Sentence(snapshot).TrimEnd('.'))));
        }
        else
        {
            CurrentBody = "";
            Progress = "";
            Bodies = [];
            UnsoldSummary = "";
            ValueSummary = "";
            DiscoveryText = "";
            var known = route.FindByName(name);
            var engineerHere = Workspace.Catalog.Engineers.Values.FirstOrDefault(engineer => string.Equals(engineer.System, name, StringComparison.OrdinalIgnoreCase));
            var position = known?.Position is { } visited ? new GalacticPosition(visited.X, visited.Y, visited.Z) : engineerHere?.Position;
            var distance = position is { } point ? Workspace.DistanceFromHere(point) : null;
            var parts = new List<IReadOnlyList<PhrasePart>>();
            if (distance is { } ly && route.CurrentSystem is { } here)
            {
                parts.Add(Phrase.Format(Keys.System_LightYearsFrom, ("distance", Display.LightYears(ly)), ("system", LinkTo(here, PageKind.System, here))));
            }

            parts.Add(Phrase.Text(known is null
                ? Strings.Get(Keys.System_NotVisited)
                : Strings.Format(Keys.System_LastThere, ("when", known.LastVisit.ToLocalTime()), ("visits", Display.Plural(Keys.Plural_Visits, known.Visits)))));
            WhereParts = Phrase.Join(" ", parts);
        }

        Engineers = [.. Workspace.Catalog.Engineers.Values.Where(engineer => string.Equals(engineer.System, name, StringComparison.OrdinalIgnoreCase)).Select(engineer => Workspace.EngineerLinkFor(engineer.Name))];
        OnPropertyChanged(nameof(HasEngineers));

        var fleet = Workspace.FleetSnapshot;
        var ships = fleet.Ships.Count(ship => string.Equals(ship.System, name, StringComparison.OrdinalIgnoreCase));
        var stored = fleet.StoredModules.Count(module => string.Equals(module.System, name, StringComparison.OrdinalIgnoreCase));
        FleetText = ships == 0 && stored == 0 ? "" : Strings.Format(Keys.System_FleetHere, ("ships", Display.Plural(Keys.Plural_Ships, ships)), ("modules", Display.Plural(Keys.Plural_StoredModules, stored)));
    }
}
