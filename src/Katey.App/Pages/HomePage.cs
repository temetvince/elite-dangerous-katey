using System.Globalization;
using Katey.App.Navigation;
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
        Kicker = "Start";
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
            new(LinkTo("Engineers", PageKind.Engineers), selected == 0 ? Display.Plural(catalog.Engineers.Count, "engineer") + ", none selected" : $"{Display.Plural(selected, "engineer")} you will visit"),
            new(LinkTo("Modules", PageKind.Modules), Display.Plural(catalog.EngineerableModuleTypes().Count, "engineerable type")),
            new(LinkTo("Materials", PageKind.Materials), Workspace.InventoryCounts is { } counts ? Display.Plural(counts.Count, "material") + " in your inventory" : "inventory not read yet"),
            new(LinkTo("Unlocks", PageKind.Unlocks), UnlocksTile()),
            new(LinkTo("Fleet", PageKind.Fleet), fleet.Ships.Count == 0 ? "no ships seen yet" : $"{Display.Plural(fleet.Ships.Count, "ship")}, {Display.Plural(Workspace.Fleet.Fittings.Count, "saved fit")}"),
            new(LinkTo("Stats", PageKind.Statistics), Workspace.Statistics.Figure("Exploration", "Time_Played") is { } played ? Display.Duration(TimeSpan.FromSeconds(played)) + " played" : "waiting for the game's figures"),
            new(LinkTo("This system", PageKind.System), route.CurrentSystem ?? "position unknown", route.CurrentSystem is { } here ? LinkTo(here, PageKind.System) : null),
            new(LinkTo("Route", PageKind.Route), Workspace.Monitor.Route is { Stops.Count: > 1 } navRoute ? Display.Plural(navRoute.Stops.Count - 1, "jump") + " plotted" : "no route plotted"),
            new(LinkTo("Settings", PageKind.Settings), Workspace.JournalFolder.Length == 0 ? "journal folder not set" : "journal folder set"),
        ];

        var lines = Workspace.PlanLines();
        var enabled = Workspace.Plan.EnabledCount;
        var shortCount = Workspace.InventoryCounts is null ? 0 : lines.Count(line => !line.IsCovered);
        PlanText = enabled == 0
            ? "The plan is empty. Add a blueprint from any module page."
            : Workspace.InventoryCounts is null
                ? $"{Display.Plural(enabled, "entry", "entries")} planned. Start the journal watch to see what you still need."
                : shortCount == 0
                    ? $"{Display.Plural(enabled, "entry", "entries")} planned and every material covered."
                    : $"{Display.Plural(enabled, "entry", "entries")} planned, {Display.Plural(shortCount, "material")} short.";
        WatchText = Workspace.Monitor.State switch
        {
            Journal.MonitorState.Watching => "Watching the journal.",
            Journal.MonitorState.CatchingUp => "Reading the journal history.",
            Journal.MonitorState.FolderMissing => "The journal folder does not exist. Fix it in Settings.",
            Journal.MonitorState.Stopped => "Not watching the journal. Start it in Settings.",
            _ => "Not watching the journal. Start it in Settings.",
        };
        CatalogText = $"{Display.Plural(catalog.Blueprints.Count, "blueprint")}, {Display.Plural(catalog.Experimentals.Count, "experimental effect")}, {Display.Plural(catalog.Engineers.Count, "engineer")}, {Display.Plural(catalog.Materials.Count, "material")}, {Display.Plural(Workspace.Modules.Count, "module")}.";
        Stats = [PlanStat(lines.Count, enabled, shortCount), EarningsStat(), RouteStat(), SystemStat(), DataStat()];
    }

    private string UnlocksTile()
    {
        var catalog = Workspace.Catalog;
        var boughtCount = catalog.Unlocks.Values.Count(unlock => Workspace.UnlockedBecause(unlock) is not null);
        var planned = Workspace.Plan.Snapshot().Count(entry => entry.IsUnlock);
        return $"{Display.Plural(catalog.Unlocks.Count, "technology broker unlock")}, {boughtCount} bought, {planned} planned";
    }

    private HomeStat PlanStat(int lineCount, int enabled, int shortCount)
    {
        var link = LinkTo("Materials", PageKind.Materials);
        return enabled == 0
            ? new HomeStat("MATERIALS SHORT", "—", "The plan is empty.", StatTone.Muted, link)
            : Workspace.InventoryCounts is null
            ? new HomeStat("MATERIALS SHORT", "—", $"{Display.Plural(lineCount, "material")} needed; inventory not read yet.", StatTone.Muted, link)
            : shortCount == 0
            ? new HomeStat("MATERIALS SHORT", "0", $"Every one of the {Display.Plural(lineCount, "material")} the plan needs is covered.", StatTone.Good, link)
            : new HomeStat("MATERIALS SHORT", shortCount.ToString(CultureInfo.InvariantCulture), $"of the {Display.Plural(lineCount, "material")} the plan needs.", StatTone.Bad, link);
    }

    private HomeStat EarningsStat()
    {
        var statistics = Workspace.Statistics;
        var link = LinkTo("Stats", PageKind.Statistics);
        if (statistics.Latest is not { } latest)
        {
            return new HomeStat("EARNED, 7 DAYS", "—", "No income in the journal yet.", StatTone.Muted, link);
        }

        var since = latest - Week;
        var recent = statistics.Income.Where(entry => entry.At >= since).ToList();
        var total = recent.Sum(entry => entry.Amount);
        return recent.Count == 0
            ? new HomeStat("EARNED, 7 DAYS", "0 Cr", "Nothing earned in the last week of play.", StatTone.Muted, link)
            : new HomeStat("EARNED, 7 DAYS", Display.Credits(total), $"from {Display.Plural(recent.Count, "payment")} in the last week of play.", StatTone.Accent, link);
    }

    private HomeStat RouteStat()
    {
        var link = LinkTo("Route", PageKind.Route);
        var stops = RoutePage.BuildStops(Workspace);
        if (stops.Count == 0)
        {
            return new HomeStat("JUMPS LEFT", "—", "No route plotted.", StatTone.Muted, link);
        }

        var hereIndex = RoutePage.HereIndex(Workspace);
        var remaining = hereIndex < 0 ? stops.Count - 1 : stops.Count - 1 - hereIndex;
        if (remaining == 0)
        {
            var arrived = Phrase.Of("Arrived at ", LinkTo(stops[^1].System, PageKind.System, stops[^1].System), ".");
            return new HomeStat("JUMPS LEFT", "0", Phrase.Plain(arrived), StatTone.Good, link, arrived);
        }

        var fuel = FuelEstimate.For(Workspace, stops);
        var detail = fuel.HasEstimate && fuel.JumpsWithoutScooping is { } jumps
            ? Phrase.Text(jumps >= remaining ? "Fuel for the whole route without scooping." : jumps == 0 ? "Not enough fuel for the next jump." : $"Fuel for {Display.Plural(jumps, "jump")} before you must scoop.")
            : Phrase.Of("to ", LinkTo(stops[^1].System, PageKind.System, stops[^1].System), ".");
        var tone = fuel.HasEstimate && fuel.JumpsWithoutScooping is { } left
            ? left == 0 ? StatTone.Bad : left < remaining ? StatTone.Warn : StatTone.Good
            : StatTone.Accent;
        return new HomeStat("JUMPS LEFT", remaining.ToString(CultureInfo.InvariantCulture), Phrase.Plain(detail), tone, link, detail);
    }

    /// <summary>What the unsold exploration and organic data aboard is worth, the first-logged bonus taken as likely where the journal says nobody had logged the species.</summary>
    private HomeStat DataStat()
    {
        var link = LinkTo("Stats", PageKind.Statistics);
        var aboard = CarriedDataSummary.Of(Workspace.Carried, Workspace.Predictor);
        if (aboard.IsEmpty)
        {
            return new HomeStat("DATA ABOARD", "—", "Nothing scanned or sampled since you last sold.", StatTone.Muted, link);
        }

        var parts = new List<string>();
        if (aboard.Bodies > 0)
        {
            parts.Add($"{Display.Plural(aboard.Bodies, "body", "bodies")} in {Display.Plural(aboard.Systems, "system")}");
        }

        if (aboard.Samples > 0)
        {
            parts.Add(Display.Plural(aboard.Samples, "sample"));
        }

        return new HomeStat("DATA ABOARD", Display.Credits(aboard.TotalCredits), $"{string.Join(", ", parts)} unsold; lost if you die.", StatTone.Accent, link);
    }

    private HomeStat SystemStat()
    {
        var system = Workspace.System;
        if (system.SystemName is null)
        {
            return new HomeStat("BODIES SCANNED", "—", "No position yet.", StatTone.Muted, LinkTo("This system", PageKind.System));
        }

        // The link under the figure is the system's own name, so the name opens the system wherever it is shown.
        var link = LinkTo(system.SystemName, PageKind.System);
        var scanned = system.BodiesScanned;
        if (system.BodyCount is { } total && total > 0)
        {
            var known = system.BodiesKnown ?? scanned;
            var tone = known >= total ? StatTone.Good : StatTone.Accent;
            return new HomeStat("BODIES SCANNED", ScanProgress.Figure(system), "in this system.", tone, link);
        }

        return scanned == 0
            ? new HomeStat("BODIES SCANNED", "0", "in this system. Honk to count the bodies.", StatTone.Muted, link)
            : new HomeStat("BODIES SCANNED", scanned.ToString(CultureInfo.InvariantCulture), "in this system, before a honk.", StatTone.Accent, link);
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
        Title = "Nothing here";
        Kicker = key.Kind.ToString();
        Message = $"There is no {key.Kind.ToString().ToLowerInvariant()} called \"{key.Id}\".";
        HomeLink = LinkTo("Back to the start page", PageKind.Home);
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
