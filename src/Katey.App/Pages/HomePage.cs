using System.Globalization;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.App.ViewModels;

namespace Katey.App.Pages;

/// <summary>One shortcut on the start page.</summary>
/// <param name="Link">Where it goes.</param>
/// <param name="Detail">What is there right now, such as a count; shown as text unless <paramref name="DetailLink"/> is given.</param>
/// <param name="DetailLink">The detail as a link, when what is there is itself a page, such as the system the ship is in; <see langword="null"/> for plain text.</param>
public sealed record HomeTile(Link Link, string Detail, Link? DetailLink = null)
{
    /// <summary>Gets whether the detail opens a page.</summary>
    public bool HasDetailLink => DetailLink is not null;
}

/// <summary>How a figure on the start page reads: the colour it takes.</summary>
public enum StatTone
{
    /// <summary>A plain figure in the identity colour.</summary>
    Accent,

    /// <summary>All is well.</summary>
    Good,

    /// <summary>Worth attention soon.</summary>
    Warn,

    /// <summary>Needs attention now.</summary>
    Bad,

    /// <summary>Nothing to report yet.</summary>
    Muted,
}

/// <summary>One figure that matters today, on the start page: a big number, what it counts, and a line under it.</summary>
/// <param name="Label">What the figure counts, in capitals.</param>
/// <param name="Value">The figure, as text.</param>
/// <param name="Detail">One line under it.</param>
/// <param name="Tone">The colour it takes.</param>
/// <param name="Link">The page that explains it.</param>
/// <param name="DetailParts">The detail with a link in it, when a system is named; <see langword="null"/> to show <paramref name="Detail"/> as text.</param>
public sealed record HomeStat(string Label, string Value, string Detail, StatTone Tone, Link Link, IReadOnlyList<PhrasePart>? DetailParts = null)
{
    /// <summary>Gets whether the detail carries a link.</summary>
    public bool HasDetailParts => DetailParts is not null;

    /// <summary>Gets whether the figure reads as all well.</summary>
    public bool IsGood => Tone == StatTone.Good;

    /// <summary>Gets whether the figure reads as a caution.</summary>
    public bool IsWarn => Tone == StatTone.Warn;

    /// <summary>Gets whether the figure reads as trouble.</summary>
    public bool IsBad => Tone == StatTone.Bad;

    /// <summary>Gets whether the figure has nothing to report.</summary>
    public bool IsMuted => Tone == StatTone.Muted;
}

/// <summary>The start page: the figures that matter today, where to browse, and what the plan and the journal look like right now.</summary>
public sealed class HomePage : Page
{
    private static readonly TimeSpan Week = TimeSpan.FromDays(7);

    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    public HomePage(Workspace workspace)
        : base(PageKey.Home, workspace)
    {
        Title = "Katey";
        Kicker = Strings.Get(Keys.Home_Kicker);
        Refresh();
    }

    /// <summary>Gets the figures that matter today: materials short, credits earned this week, jumps left, bodies scanned.</summary>
    public IReadOnlyList<HomeStat> Stats { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the browse shortcuts, one per index page.</summary>
    public IReadOnlyList<HomeTile> Browse { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a line on the plan: entries and shortfall.</summary>
    public string PlanText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets a line on the journal watch.</summary>
    public string WatchText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets a line on the catalog's size.</summary>
    public string CatalogText { get; private set => SetProperty(ref field, value); } = "";

    /// <inheritdoc/>
    public override void Refresh()
    {
        var catalog = Workspace.Catalog;
        var fleet = Workspace.FleetSnapshot;
        var route = Workspace.Route;
        var selected = Workspace.Engineers.Count;
        Browse =
        [
            new(LinkTo(Strings.Get(Keys.Nav_Engineers), PageKind.Engineers), selected == 0 ? Strings.Format(Keys.Home_EngineersNoneSelected, ("engineers", Display.Plural(Keys.Plural_Engineers, catalog.Engineers.Count))) : Strings.Format(Keys.Home_EngineersYouWillVisit, ("engineers", Display.Plural(Keys.Plural_Engineers, selected)))),
            new(LinkTo(Strings.Get(Keys.Nav_Modules), PageKind.Modules), Display.Plural(Keys.Plural_EngineerableTypes, catalog.EngineerableModuleTypes().Count)),
            new(LinkTo(Strings.Get(Keys.Nav_Materials), PageKind.Materials), Workspace.InventoryCounts is { } counts ? Strings.Format(Keys.Home_MaterialsInInventory, ("materials", Display.Plural(Keys.Plural_Materials, counts.Count))) : Strings.Get(Keys.Home_InventoryNotRead)),
            new(LinkTo(Strings.Get(Keys.Nav_Unlocks), PageKind.Unlocks), UnlocksTile()),
            new(LinkTo(Strings.Get(Keys.Nav_Fleet), PageKind.Fleet), fleet.Ships.Count == 0 ? Strings.Get(Keys.Home_NoShipsSeen) : Strings.Format(Keys.Home_ShipsAndFits, ("ships", Display.Plural(Keys.Plural_Ships, fleet.Ships.Count)), ("fits", Display.Plural(Keys.Plural_SavedFits, Workspace.Fleet.Fittings.Count)))),
            new(LinkTo(Strings.Get(Keys.Nav_Stats), PageKind.Statistics), Workspace.Statistics.Figure("Exploration", "Time_Played") is { } played ? Strings.Format(Keys.Home_Played, ("duration", Display.Duration(TimeSpan.FromSeconds(played)))) : Strings.Get(Keys.Home_WaitingForFigures)),
            new(LinkTo(Strings.Get(Keys.Home_ThisSystem), PageKind.System), route.CurrentSystem ?? Strings.Get(Keys.Home_PositionUnknown), route.CurrentSystem is { } here ? LinkTo(here, PageKind.System) : null),
            new(LinkTo(Strings.Get(Keys.Nav_Route), PageKind.Route), Workspace.Monitor.Route is { Stops.Count: > 1 } navRoute ? Strings.Format(Keys.Home_JumpsPlotted, ("jumps", Display.Plural(Keys.Plural_Jumps, navRoute.Stops.Count - 1))) : Strings.Get(Keys.Home_NoRoutePlotted)),
            new(LinkTo(Strings.Get(Keys.Nav_Settings), PageKind.Settings), Workspace.JournalFolder.Length == 0 ? Strings.Get(Keys.Home_JournalFolderNotSet) : Strings.Get(Keys.Home_JournalFolderSet)),
        ];

        var lines = Workspace.PlanLines();
        var enabled = Workspace.Plan.EnabledCount;
        var shortCount = Workspace.InventoryCounts is null ? 0 : lines.Count(line => !line.IsCovered);
        var entries = Display.Plural(Keys.Plural_Entries, enabled);
        PlanText = enabled == 0
            ? Strings.Get(Keys.Home_PlanEmpty)
            : Workspace.InventoryCounts is null
                ? Strings.Format(Keys.Home_PlanNoInventory, ("entries", entries))
                : shortCount == 0
                    ? Strings.Format(Keys.Home_PlanCovered, ("entries", entries))
                    : Strings.Format(Keys.Home_PlanShort, ("entries", entries), ("materials", Display.Plural(Keys.Plural_Materials, shortCount)));
        WatchText = Workspace.Monitor.State switch
        {
            Journal.MonitorState.Watching => Strings.Get(Keys.Home_Watching),
            Journal.MonitorState.CatchingUp => Strings.Get(Keys.Home_CatchingUp),
            Journal.MonitorState.FolderMissing => Strings.Get(Keys.Home_FolderMissing),
            Journal.MonitorState.Stopped => Strings.Get(Keys.Home_NotWatching),
            _ => Strings.Get(Keys.Home_NotWatching),
        };
        CatalogText = Strings.Format(
            Keys.Home_Catalog,
            ("blueprints", Display.Plural(Keys.Plural_Blueprints, catalog.Blueprints.Count)),
            ("experimentals", Display.Plural(Keys.Plural_Experimentals, catalog.Experimentals.Count)),
            ("engineers", Display.Plural(Keys.Plural_Engineers, catalog.Engineers.Count)),
            ("materials", Display.Plural(Keys.Plural_Materials, catalog.Materials.Count)),
            ("modules", Display.Plural(Keys.Plural_Modules, Workspace.Modules.Count)));
        Stats = [PlanStat(lines.Count, enabled, shortCount), EarningsStat(), RouteStat(), SystemStat(), DataStat()];
    }

    private string UnlocksTile()
    {
        var catalog = Workspace.Catalog;
        var boughtCount = catalog.Unlocks.Values.Count(unlock => Workspace.UnlockedBecause(unlock) is not null);
        var planned = Workspace.Plan.Snapshot().Count(entry => entry.IsUnlock);
        return Strings.Format(Keys.Home_UnlocksTile, ("unlocks", Display.Plural(Keys.Plural_BrokerUnlocks, catalog.Unlocks.Count)), ("bought", boughtCount), ("planned", planned));
    }

    private HomeStat PlanStat(int lineCount, int enabled, int shortCount)
    {
        var link = LinkTo(Strings.Get(Keys.Nav_Materials), PageKind.Materials);
        var label = Display.Upper(Keys.Home_StatMaterialsShort);
        var materials = Display.Plural(Keys.Plural_Materials, lineCount);
        return enabled == 0
            ? new HomeStat(label, "—", Strings.Get(Keys.Home_StatPlanEmpty), StatTone.Muted, link)
            : Workspace.InventoryCounts is null
            ? new HomeStat(label, "—", Strings.Format(Keys.Home_StatMaterialsNeeded, ("materials", materials)), StatTone.Muted, link)
            : shortCount == 0
            ? new HomeStat(label, "0", Strings.Format(Keys.Home_StatMaterialsCovered, ("materials", materials)), StatTone.Good, link)
            : new HomeStat(label, shortCount.ToString(CultureInfo.CurrentCulture), Strings.Format(Keys.Home_StatMaterialsOf, ("materials", materials)), StatTone.Bad, link);
    }

    private HomeStat EarningsStat()
    {
        var statistics = Workspace.Statistics;
        var link = LinkTo(Strings.Get(Keys.Nav_Stats), PageKind.Statistics);
        var label = Display.Upper(Keys.Home_StatEarned);
        if (statistics.Latest is not { } latest)
        {
            return new HomeStat(label, "—", Strings.Get(Keys.Home_StatNoIncome), StatTone.Muted, link);
        }

        var since = latest - Week;
        var recent = statistics.Income.Where(entry => entry.At >= since).ToList();
        var total = recent.Sum(entry => entry.Amount);
        return recent.Count == 0
            ? new HomeStat(label, Strings.Get(Keys.Home_StatZeroCredits), Strings.Get(Keys.Home_StatNothingEarned), StatTone.Muted, link)
            : new HomeStat(label, Display.Credits(total), Strings.Format(Keys.Home_StatFromPayments, ("payments", Display.Plural(Keys.Plural_Payments, recent.Count))), StatTone.Accent, link);
    }

    private HomeStat RouteStat()
    {
        var link = LinkTo(Strings.Get(Keys.Nav_Route), PageKind.Route);
        var label = Display.Upper(Keys.Home_StatJumpsLeft);
        var stops = RoutePage.BuildStops(Workspace);
        if (stops.Count == 0)
        {
            return new HomeStat(label, "—", Strings.Get(Keys.Home_StatNoRoute), StatTone.Muted, link);
        }

        var hereIndex = RoutePage.HereIndex(Workspace);
        var remaining = hereIndex < 0 ? stops.Count - 1 : stops.Count - 1 - hereIndex;
        if (remaining == 0)
        {
            var arrived = Phrase.Format(Keys.Home_StatArrivedAt, ("system", LinkTo(stops[^1].System, PageKind.System, stops[^1].System)));
            return new HomeStat(label, "0", Phrase.Plain(arrived), StatTone.Good, link, arrived);
        }

        var fuel = FuelEstimate.For(Workspace, stops);
        var detail = fuel.HasEstimate && fuel.JumpsWithoutScooping is { } jumps
            ? Phrase.Text(jumps >= remaining ? Strings.Get(Keys.Fuel_WholeRoute) : jumps == 0 ? Strings.Get(Keys.Fuel_NotEnoughNext) : Strings.Format(Keys.Home_StatFuelBeforeScoop, ("jumps", Display.Plural(Keys.Plural_Jumps, jumps))))
            : Phrase.Format(Keys.Home_StatTo, ("system", LinkTo(stops[^1].System, PageKind.System, stops[^1].System)));
        var tone = fuel.HasEstimate && fuel.JumpsWithoutScooping is { } left
            ? left == 0 ? StatTone.Bad : left < remaining ? StatTone.Warn : StatTone.Good
            : StatTone.Accent;
        return new HomeStat(label, remaining.ToString(CultureInfo.CurrentCulture), Phrase.Plain(detail), tone, link, detail);
    }

    /// <summary>What the unsold exploration and organic data aboard is worth, the first-logged bonus taken as likely where the journal says nobody had logged the species.</summary>
    private HomeStat DataStat()
    {
        var link = LinkTo(Strings.Get(Keys.Nav_Stats), PageKind.Statistics);
        var label = Display.Upper(Keys.Home_StatDataAboard);
        var aboard = CarriedDataSummary.Of(Workspace.Carried, Workspace.Predictor);
        if (aboard.IsEmpty)
        {
            return new HomeStat(label, "—", Strings.Get(Keys.Home_StatNothingAboard), StatTone.Muted, link);
        }

        var parts = new List<string>();
        if (aboard.Bodies > 0)
        {
            parts.Add(Strings.Format(Keys.Home_StatBodiesInSystems, ("bodies", Display.Plural(Keys.Plural_Bodies, aboard.Bodies)), ("systems", Display.Plural(Keys.Plural_Systems, aboard.Systems))));
        }

        if (aboard.Samples > 0)
        {
            parts.Add(Display.Plural(Keys.Plural_Samples, aboard.Samples));
        }

        return new HomeStat(label, Display.Credits(aboard.TotalCredits), Strings.Format(Keys.Home_StatUnsold, ("what", Display.List(parts))), StatTone.Accent, link);
    }

    private HomeStat SystemStat()
    {
        var system = Workspace.System;
        var label = Display.Upper(Keys.Home_StatBodiesScanned);
        if (system.SystemName is null)
        {
            return new HomeStat(label, "—", Strings.Get(Keys.Home_StatNoPosition), StatTone.Muted, LinkTo(Strings.Get(Keys.Home_ThisSystem), PageKind.System));
        }

        // The link under the figure is the system's own name, so the name opens the system wherever it is shown.
        var link = LinkTo(system.SystemName, PageKind.System);
        var scanned = system.BodiesScanned;
        if (system.BodyCount is { } total && total > 0)
        {
            var known = system.BodiesKnown ?? scanned;
            var tone = known >= total ? StatTone.Good : StatTone.Accent;
            return new HomeStat(label, ScanProgress.Figure(system), Strings.Get(Keys.Home_StatInThisSystem), tone, link);
        }

        return scanned == 0
            ? new HomeStat(label, "0", Strings.Get(Keys.Home_StatHonkToCount), StatTone.Muted, link)
            : new HomeStat(label, scanned.ToString(CultureInfo.CurrentCulture), Strings.Get(Keys.Home_StatBeforeHonk), StatTone.Accent, link);
    }
}

/// <summary>Shown when a link names something the catalogs and the journal do not have.</summary>
public sealed class NotFoundPage : Page
{
    /// <summary>Creates the page.</summary>
    /// <param name="key">The key that could not be resolved.</param>
    /// <param name="workspace">The shared state.</param>
    public NotFoundPage(PageKey key, Workspace workspace)
        : base(key, workspace)
    {
        Title = Strings.Get(Keys.NotFound_Title);
        Message = Strings.Format(Keys.NotFound_Message, ("id", key.Id));
        HomeLink = LinkTo(Strings.Get(Keys.NotFound_BackHome), PageKind.Home);
    }

    /// <summary>Gets what went wrong.</summary>
    public string Message { get; }

    /// <summary>Gets a way out.</summary>
    public Link HomeLink { get; }

    /// <inheritdoc/>
    public override void Refresh()
    {
    }
}
