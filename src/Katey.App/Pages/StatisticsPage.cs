using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core.Charts;
using Katey.Journal;

namespace Katey.App.Pages;

/// <summary>One figure, ready to print.</summary>
/// <param name="Label">What it is.</param>
/// <param name="Value">The figure as text.</param>
public sealed record StatRow(string Label, string Value);

/// <summary>A titled set of figures.</summary>
/// <param name="Title">The heading.</param>
/// <param name="Rows">The figures, in display order.</param>
public sealed record StatGroup(string Title, IReadOnlyList<StatRow> Rows);

/// <summary>One sector of the income chart.</summary>
/// <param name="Label">The activity.</param>
/// <param name="Color">Its colour as a hex string such as <c>#4E79A7</c>.</param>
/// <param name="PathData">Its outline in SVG path syntax, in a 220 by 220 square.</param>
/// <param name="Tip">Activity, amount and share, for a tooltip.</param>
public sealed record ChartSlice(string Label, string Color, string PathData, string Tip);

/// <summary>One species of organic data sold.</summary>
/// <param name="Species">The species as the journal names it.</param>
/// <param name="Samples">How many were sold.</param>
/// <param name="Total">Credits paid, bonus included.</param>
/// <param name="Bonus">The first-logged bonus within that.</param>
public sealed record SpeciesRow(string Species, int Samples, long Total, long Bonus)
{
    /// <summary>Gets the total as text.</summary>
    public string TotalText => Display.CreditsExact(Total);

    /// <summary>Gets the total without the bonus as text.</summary>
    public string BaseText => Display.CreditsExact(Total - Bonus);

    /// <summary>Gets the bonus as text.</summary>
    public string BonusText => Display.CreditsExact(Bonus);
}

/// <summary>One mission accepted as a wing mission.</summary>
/// <param name="Name">The mission as the game shows it.</param>
/// <param name="Faction">Who gave it; empty when unknown.</param>
/// <param name="Reward">Credits on offer when accepted.</param>
/// <param name="AcceptedAt">When it was accepted.</param>
/// <param name="Partners">Who was in the wing then; empty when accepted alone.</param>
/// <param name="Outcome">How it stands.</param>
/// <param name="Paid">Credits paid on completion; zero otherwise.</param>
public sealed record WingMissionRow(string Name, string Faction, long Reward, DateTimeOffset AcceptedAt, IReadOnlyList<string> Partners, MissionOutcome Outcome, long Paid)
{
    /// <summary>Gets the reward as text.</summary>
    public string RewardText => Display.CreditsExact(Reward);

    /// <summary>Gets when it was accepted, in local time.</summary>
    public string AcceptedText => AcceptedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

    /// <summary>Gets the wing at acceptance as text.</summary>
    public string WithText => Display.Names(Partners, Strings.Get(Keys.Stats_NobodyInWing));

    /// <summary>Gets how it stands as text, with what it paid when completed.</summary>
    public string OutcomeText => Outcome switch
    {
        MissionOutcome.Active => Strings.Get(Keys.Stats_MissionOpen),
        MissionOutcome.Completed => Strings.Format(Keys.Stats_MissionCompleted, ("paid", Display.CreditsExact(Paid))),
        MissionOutcome.Failed => Strings.Get(Keys.Stats_MissionFailed),
        MissionOutcome.Abandoned => Strings.Get(Keys.Stats_MissionAbandoned),
        MissionOutcome.Lapsed => Strings.Get(Keys.Stats_MissionLapsed),
        _ => Outcome.ToString(),
    };

    /// <summary>Gets whether it is still open.</summary>
    public bool IsActive => Outcome == MissionOutcome.Active;
}

/// <summary>One player flown with, and what the game marked as shared with them.</summary>
/// <param name="Name">The commander's name as the journal gives it.</param>
/// <param name="Sessions">Wing and crew sessions they were part of, in the period.</param>
/// <param name="Together">Time in those sessions, clipped to the period.</param>
/// <param name="Shared">Credits the game marked as shared while they were present, in the period.</param>
/// <param name="LastSeen">When they were last in a session.</param>
public sealed record PartnerRow(string Name, int Sessions, TimeSpan Together, long Shared, DateTimeOffset LastSeen)
{
    /// <summary>Gets the time together as text.</summary>
    public string TogetherText => Display.Duration(Together);

    /// <summary>Gets the shared credits as text.</summary>
    public string SharedText => Display.CreditsExact(Shared);

    /// <summary>Gets the last session's date as text.</summary>
    public string LastSeenText => LastSeen.ToLocalTime().ToString("d", CultureInfo.CurrentCulture);
}

/// <summary>One activity in the income chart's legend: its switch, colour and figures.</summary>
public sealed class ActivityToggle : ObservableObject
{
    private readonly Action<ActivityToggle> _changed;
    private bool _shown;

    /// <summary>Creates the toggle.</summary>
    /// <param name="kind">The activity.</param>
    /// <param name="label">Its name.</param>
    /// <param name="color">Its colour as a hex string.</param>
    /// <param name="shown">Whether it starts in the chart.</param>
    /// <param name="changed">Called after the switch changes.</param>
    public ActivityToggle(IncomeKind kind, string label, string color, bool shown, Action<ActivityToggle> changed)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(color);
        ArgumentNullException.ThrowIfNull(changed);
        Kind = kind;
        Label = label;
        Color = color;
        _changed = changed;
        _shown = shown;
    }

    /// <summary>Gets the activity.</summary>
    public IncomeKind Kind { get; }

    /// <summary>Gets the activity's name.</summary>
    public string Label { get; }

    /// <summary>Gets the colour as a hex string.</summary>
    public string Color { get; }

    /// <summary>Gets or sets whether the activity is in the chart.</summary>
    public bool IsShown
    {
        get => _shown;
        set
        {
            if (SetProperty(ref _shown, value))
            {
                _changed(this);
            }
        }
    }

    /// <summary>Gets the credits earned in the period, as text.</summary>
    public string AmountText { get; internal set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the share of the chart, as text, or empty when not in it.</summary>
    public string ShareText { get; internal set => SetProperty(ref field, value); } = "";

    /// <summary>Gets how many payments, as text.</summary>
    public string CountText { get; internal set => SetProperty(ref field, value); } = "";
}

/// <summary>
/// Income by activity as a ring chart with switches and a period, the figures behind each activity,
/// exobiology with and without the first-logged bonus, Powerplay, the wings and crews flown with, what the
/// game marked as shared with each commander, the wing missions accepted, and every lifetime figure the
/// game reports.
/// </summary>
/// <remarks>The lifetime figures are the game's own and do not follow the period.</remarks>
public sealed class StatisticsPage : Page
{
    private const string HiddenKey = "stats.hidden";
    private const string ZerosKey = "stats.zeros";
    private const string PeriodKey = "stats.period";

    private static readonly string[] Palette = ["#4E79A7", "#F28E2B", "#E15759", "#76B7B2", "#59A14F", "#EDC948", "#B07AA1", "#FF9DA7", "#9C755F", "#BAB0AC"];

    private static readonly (IncomeKind Kind, string Key)[] Kinds =
    [
        (IncomeKind.Bounties, Keys.Stats_KindBounties),
        (IncomeKind.CombatBonds, Keys.Stats_KindCombatBonds),
        (IncomeKind.Trading, Keys.Stats_KindTrading),
        (IncomeKind.MiningAndSalvage, Keys.Stats_KindMiningAndSalvage),
        (IncomeKind.Exploration, Keys.Stats_KindExploration),
        (IncomeKind.Exobiology, Keys.Stats_KindExobiology),
        (IncomeKind.Missions, Keys.Stats_KindMissions),
        (IncomeKind.SearchAndRescue, Keys.Stats_KindSearchAndRescue),
        (IncomeKind.Powerplay, Keys.Stats_KindPowerplay),
        (IncomeKind.Other, Keys.Stats_KindOther),
    ];

    private bool _loading;

    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    public StatisticsPage(Workspace workspace)
        : base(new PageKey(PageKind.Statistics), workspace)
    {
        Title = Strings.Get(Keys.Stats_Title);
        Kicker = Strings.Get(Keys.Stats_Kicker);
        Sections = new SectionSwitch(
            [
                ("income", Strings.Get(Keys.Section_Income)),
                ("exobiology", Strings.Get(Keys.Section_Exobiology)),
                ("exploration", Strings.Get(Keys.Section_Exploration)),
                ("combat", Strings.Get(Keys.Section_Combat)),
                ("trading", Strings.Get(Keys.Section_TradingAndMining)),
                ("missions", Strings.Get(Keys.Section_MissionsAndOther)),
                ("powerplay", Strings.Get(Keys.Section_Powerplay)),
                ("together", Strings.Get(Keys.Section_WingsAndCrews)),
                ("lifetime", Strings.Get(Keys.Section_Lifetime)),
            ],
            "income",
            workspace.Preferences,
            "section.stats");
        PartnerSort = new TableSort<PartnerRow>(
            [
                new("name", Strings.Get(Keys.Column_Commander), row => row.Name),
                new("sessions", Strings.Get(Keys.Column_Sessions), row => row.Sessions, DescendingFirst: true),
                new("together", Strings.Get(Keys.Column_TimeTogether), row => row.Together, DescendingFirst: true),
                new("shared", Strings.Get(Keys.Column_Shared), row => row.Shared, DescendingFirst: true),
                new("seen", Strings.Get(Keys.Column_LastFlown), row => row.LastSeen, DescendingFirst: true),
            ],
            "shared",
            workspace.Preferences,
            "sort.partners");
        PartnerSort.Changed += (_, _) => Refresh();
        WingMissionSort = new TableSort<WingMissionRow>(
            [
                new("mission", Strings.Get(Keys.Column_Mission), row => row.Name),
                new("faction", Strings.Get(Keys.Column_Faction), row => row.Faction),
                new("reward", Strings.Get(Keys.Column_Reward), row => row.Reward, DescendingFirst: true),
                new("accepted", Strings.Get(Keys.Column_Accepted), row => row.AcceptedAt, DescendingFirst: true),
                new("status", Strings.Get(Keys.Column_Status), row => row.Outcome),
            ],
            "accepted",
            workspace.Preferences,
            "sort.wingmissions");
        WingMissionSort.Changed += (_, _) => Refresh();
        SpeciesSort = new TableSort<SpeciesRow>(
            [
                new("species", Strings.Get(Keys.Column_Species), row => row.Species),
                new("samples", Strings.Get(Keys.Column_Samples), row => row.Samples, DescendingFirst: true),
                new("total", Strings.Get(Keys.Column_WithBonus), row => row.Total, DescendingFirst: true),
                new("base", Strings.Get(Keys.Column_WithoutBonus), row => row.Total - row.Bonus, DescendingFirst: true),
                new("bonus", Strings.Get(Keys.Column_Bonus), row => row.Bonus, DescendingFirst: true),
            ],
            "total",
            workspace.Preferences,
            "sort.species");
        SpeciesSort.Changed += (_, _) => Refresh();
        var hidden = workspace.Preferences.GetSet(HiddenKey);
        Activities = [.. Kinds.Select((pair, index) => new ActivityToggle(pair.Kind, Strings.Get(pair.Key), Palette[index % Palette.Length], !hidden.Contains(pair.Kind.ToString()), OnToggle))];
        SelectedPeriod = Periods.FirstOrDefault(choice => choice.Key == workspace.Preferences.Get(PeriodKey)) ?? Periods[0];
        Refresh();
    }

    /// <summary>Gets the page's sections.</summary>
    public SectionSwitch Sections { get; }

    /// <summary>Gets the periods the ledger figures can be narrowed to.</summary>
    public IReadOnlyList<Choice> Periods { get; } =
    [
        new("all", Strings.Get(Keys.Stats_PeriodAll)),
        new("7", Strings.Get(Keys.Stats_Period7)),
        new("30", Strings.Get(Keys.Stats_Period30)),
        new("90", Strings.Get(Keys.Stats_Period90)),
        new("365", Strings.Get(Keys.Stats_Period365)),
    ];

    /// <summary>Gets or sets the period. Must be one of <see cref="Periods"/>. Remembered.</summary>
    public Choice SelectedPeriod
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                Workspace.Preferences.Set(PeriodKey, value.Key);
                if (!_loading)
                {
                    Refresh();
                }
            }
        }
    }

    /// <summary>Gets one line on what the ledger covers.</summary>
    public string LedgerNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the activities with their switches, in chart order.</summary>
    public IReadOnlyList<ActivityToggle> Activities { get; }

    /// <summary>Gets the chart's sectors for the activities switched on.</summary>
    public IReadOnlyList<ChartSlice> Slices { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether the chart has anything to show.</summary>
    public bool HasSlices => Slices.Count > 0;

    /// <summary>Gets the total of the activities switched on, as text.</summary>
    public string IncomeTotalText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the income section's figures.</summary>
    public IReadOnlyList<StatGroup> IncomeGroups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the exobiology figures.</summary>
    public IReadOnlyList<StatGroup> ExobiologyGroups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the species table's sort.</summary>
    public TableSort<SpeciesRow> SpeciesSort { get; }

    /// <summary>Gets every species sold in the period, in sort order.</summary>
    public IReadOnlyList<SpeciesRow> Species { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether any species was sold in the period.</summary>
    public bool HasSpecies => Species.Count > 0;

    /// <summary>Gets the exploration figures.</summary>
    public IReadOnlyList<StatGroup> ExplorationGroups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the combat figures.</summary>
    public IReadOnlyList<StatGroup> CombatGroups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the trading and mining figures.</summary>
    public IReadOnlyList<StatGroup> TradingGroups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the missions and other income figures.</summary>
    public IReadOnlyList<StatGroup> MissionGroups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the Powerplay figures.</summary>
    public IReadOnlyList<StatGroup> PowerplayGroups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the wings and crews figures: time together and what was shared, by kind.</summary>
    public IReadOnlyList<StatGroup> TogetherGroups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets one line on what the wings and crews section counts.</summary>
    public string TogetherNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the partner table's sort.</summary>
    public TableSort<PartnerRow> PartnerSort { get; }

    /// <summary>Gets the wing missions table's sort.</summary>
    public TableSort<WingMissionRow> WingMissionSort { get; }

    /// <summary>Gets the wing missions accepted in the period, and every one still active, in sort order.</summary>
    public IReadOnlyList<WingMissionRow> WingMissions { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether there is a wing mission to show.</summary>
    public bool HasWingMissions => WingMissions.Count > 0;

    /// <summary>Gets what the wing missions table lists, so a mission taken from the board while winged is not a surprise.</summary>
    public static string WingMissionsNote => Strings.Get(Keys.Stats_WingMissionsNote);

    /// <summary>Gets everyone flown with in the period, in sort order.</summary>
    public IReadOnlyList<PartnerRow> Partners { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether anyone was flown with in the period.</summary>
    public bool HasPartners => Partners.Count > 0;

    /// <summary>Gets or sets whether the lifetime figures include those the game reports as zero. Remembered. Off hides them and drops a section left empty.</summary>
    public bool ShowZeroFigures
    {
        get => Workspace.Preferences.GetBool(ZerosKey, false);
        set
        {
            if (value != ShowZeroFigures)
            {
                Workspace.Preferences.SetBool(ZerosKey, value);
                OnPropertyChanged();
                Refresh();
            }
        }
    }

    /// <summary>Gets every section of the game's lifetime figures.</summary>
    public IReadOnlyList<StatGroup> LifetimeGroups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets when the game last wrote its figures, or why there are none.</summary>
    public string LifetimeNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>A lifetime figure's key as a label: <c>Bounty_Hunting_Profit</c> reads "Bounty hunting profit".</summary>
    /// <param name="key">The journal key.</param>
    /// <returns>The label.</returns>
    public static string LabelOf(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var words = key.Replace("TG_", "Thargoid_", StringComparison.Ordinal).Split('_', StringSplitOptions.RemoveEmptyEntries);
        var text = string.Join(' ', words.Select(word => word.Length <= 3 && word.All(char.IsUpper) ? word : word.ToLowerInvariant()));
        return text.Length == 0 ? key : char.ToUpperInvariant(text[0]) + text[1..];
    }

    /// <summary>A lifetime figure as text, by what its key says it measures.</summary>
    /// <param name="key">The journal key.</param>
    /// <param name="value">The figure.</param>
    /// <returns>Credits, a duration, a distance or a plain number.</returns>
    public static string FormatFigure(string key, double value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Contains("Time_Played", StringComparison.OrdinalIgnoreCase) || key.Contains("Time_Pledged", StringComparison.OrdinalIgnoreCase))
        {
            return Display.Duration(TimeSpan.FromSeconds(value));
        }

        if (key.Contains("OnFoot_Distance", StringComparison.OrdinalIgnoreCase))
        {
            return Strings.Format(Keys.Unit_Kilometres, ("value", value / 1000));
        }

        if (key.Contains("Distance", StringComparison.OrdinalIgnoreCase))
        {
            return Display.LightYears(value);
        }

        var credits = key.Contains("Profit", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Spent", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Wealth", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Highest", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Payout", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Earnings", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Reward", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Fines", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Bounties_Received", StringComparison.OrdinalIgnoreCase)
            || key.Contains("Value", StringComparison.OrdinalIgnoreCase);
        return credits ? Display.CreditsExact((long)Math.Round(value)) : Display.Whole(value);
    }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var statistics = Workspace.Statistics;
        var since = SelectedPeriod.Key == "all" ? DateTimeOffset.MinValue : DateTimeOffset.UtcNow.AddDays(-int.Parse(SelectedPeriod.Key, CultureInfo.InvariantCulture));
        var ledger = statistics.Income;
        var income = ledger.Where(entry => entry.At >= since).ToList();
        LedgerNote = ledger.Count == 0
            ? Strings.Get(Keys.Stats_LedgerEmpty)
            : SelectedPeriod.Key == "all"
                ? Strings.Format(Keys.Stats_PaymentsSince, ("payments", Display.Plural(Keys.Plural_Payments, income.Count)), ("when", ledger[0].At.ToLocalTime()))
                : Strings.Format(Keys.Stats_PaymentsInPeriod, ("payments", Display.Plural(Keys.Plural_Payments, income.Count)));

        RefreshChart(income);
        var aboard = CarriedDataSummary.Of(Workspace.Carried, Workspace.Predictor);
        ExobiologyGroups = [AboardExobiology(aboard), .. BuildExobiology(statistics, income)];
        ExplorationGroups = [AboardCartographics(aboard), .. BuildExploration(statistics, income)];
        CombatGroups = BuildCombat(statistics, income);
        TradingGroups = BuildTrading(statistics, income);
        MissionGroups = BuildMissions(statistics, income);
        PowerplayGroups = BuildPowerplay(statistics, income, since);
        RefreshTogether(statistics, since);
        var showZeros = ShowZeroFigures;
        OnPropertyChanged(nameof(ShowZeroFigures));
        LifetimeGroups =
        [
            .. statistics.Lifetime
                .Select(section => new StatGroup(
                    Names.FigureSection(section.Name, LabelOf(section.Name)),
                    [.. section.Figures.Where(pair => showZeros || pair.Value != 0).Select(pair => new StatRow(Names.Figure(pair.Key, LabelOf(pair.Key)), FormatFigure(pair.Key, pair.Value)))]))
                .Where(group => group.Rows.Count > 0),
        ];
        LifetimeNote = statistics.AsOf is { } asOf
            ? Strings.Format(Keys.Stats_LifetimeAsOf, ("when", asOf.ToLocalTime()))
            : Strings.Get(Keys.Stats_LifetimeNone);
    }

    private static Summary Summarise(IEnumerable<IncomeEntry> entries)
    {
        var list = entries.ToList();
        return new Summary(list.Count, list.Sum(entry => entry.Amount), list.Sum(entry => entry.Bonus), list.Count == 0 ? 0 : list.Max(entry => entry.Amount));
    }

    private static IReadOnlyList<StatRow> LifetimeRows(StatisticsSnapshot statistics, string section, params string[] keys)
        => [.. keys.Select(key => (Key: key, Value: statistics.Figure(section, key))).Where(pair => pair.Value is not null).Select(pair => new StatRow(Names.Figure(pair.Key, LabelOf(pair.Key)), FormatFigure(pair.Key, pair.Value!.Value)))];

    private static StatGroup? Lifetime(StatisticsSnapshot statistics, string title, string section, params string[] keys)
    {
        var rows = LifetimeRows(statistics, section, keys);
        return rows.Count == 0 ? null : new StatGroup(title, rows);
    }

    private static IReadOnlyList<StatGroup> Groups(params StatGroup?[] groups) => [.. groups.Where(group => group is not null).Select(group => group!)];

    private static StatGroup Ledger(string title, Summary summary, string paymentWord)
        => new(title,
        [
            new StatRow(Strings.Get(Keys.Stats_Earned), Display.CreditsExact(summary.Total)),
            new StatRow(paymentWord, Display.Whole(summary.Count)),
            new StatRow(Strings.Get(Keys.Stats_Largest), Display.CreditsExact(summary.Largest)),
            new StatRow(Strings.Get(Keys.Stats_Average), Display.CreditsExact(summary.Count == 0 ? 0 : summary.Total / summary.Count)),
        ]);

    private void OnToggle(ActivityToggle toggle)
    {
        Workspace.Preferences.SetSet(HiddenKey, Activities.Where(activity => !activity.IsShown).Select(activity => activity.Kind.ToString()));
        if (!_loading)
        {
            Refresh();
        }
    }

    private void RefreshChart(List<IncomeEntry> income)
    {
        _loading = true;
        var byKind = income.GroupBy(entry => entry.Kind).ToDictionary(group => group.Key, Summarise);
        var shown = Activities.Where(activity => activity.IsShown).ToList();
        var shownTotal = shown.Sum(activity => Math.Max(0, byKind.GetValueOrDefault(activity.Kind)?.Total ?? 0));
        foreach (var activity in Activities)
        {
            var summary = byKind.GetValueOrDefault(activity.Kind);
            activity.AmountText = Display.CreditsExact(summary?.Total ?? 0);
            activity.CountText = summary is null ? Strings.Get(Keys.Stats_NoPayments) : Display.Plural(Keys.Plural_Payments, summary.Count);
            var share = activity.IsShown && shownTotal > 0 && summary is { Total: > 0 } ? (double)summary.Total / shownTotal : 0;
            activity.ShareText = share > 0 ? Strings.Format(Keys.Unit_Percent, ("value", share * 100)) : "";
        }

        var slices = PieChart.Build(shown.Select(activity => (activity.Kind.ToString(), (double)Math.Max(0, byKind.GetValueOrDefault(activity.Kind)?.Total ?? 0))), 220);
        Slices =
        [
            .. slices.Select(slice =>
            {
                var activity = Activities.First(candidate => candidate.Kind.ToString() == slice.Key);
                return new ChartSlice(activity.Label, activity.Color, slice.PathData, Strings.Format(Keys.Stats_ChartSlice, ("activity", activity.Label), ("amount", activity.AmountText), ("percent", slice.Fraction * 100)));
            }),
        ];
        OnPropertyChanged(nameof(HasSlices));
        IncomeTotalText = shownTotal == 0 ? Strings.Get(Keys.Stats_NothingEarnedShown) : Strings.Format(Keys.Stats_EarnedFromShown, ("credits", Display.CreditsExact(shownTotal)));
        var all = Summarise(income);
        IncomeGroups =
        [
            new StatGroup(Strings.Get(Keys.Stats_EverythingInPeriod),
            [
                new StatRow(Strings.Get(Keys.Stats_Earned), Display.CreditsExact(all.Total)),
                new StatRow(Strings.Get(Keys.Stats_Payments), Display.Whole(all.Count)),
                new StatRow(Strings.Get(Keys.Stats_LargestSinglePayment), Display.CreditsExact(all.Largest)),
                new StatRow(Strings.Get(Keys.Stats_BestActivity), byKind.Count == 0 ? Strings.Get(Keys.Stats_NoneYet) : Activities.First(activity => activity.Kind == byKind.MaxBy(pair => pair.Value.Total).Key).Label),
            ]),
        ];
        _loading = false;
    }

    /// <summary>The organic samples aboard, unsold, with the first-logged bonus taken as likely wherever nobody had logged the species on its body.</summary>
    private static StatGroup AboardExobiology(CarriedDataSummary aboard) => new(Strings.Get(Keys.Stats_AboardUnsold),
    [
        new StatRow(Strings.Get(Keys.Stats_Samples), Display.Whole(aboard.Samples)),
        new StatRow(Strings.Get(Keys.Stats_LikelyFirstLogged), Display.Whole(aboard.FirstLoggedSamples)),
        new StatRow(Strings.Get(Keys.Stats_WorthWithBonus), Display.CreditsExact(aboard.ExobiologyCredits)),
        new StatRow(Strings.Get(Keys.Stats_WorthWithoutBonus), Display.CreditsExact(aboard.ExobiologyBaseCredits)),
    ]);

    /// <summary>The cartographic data aboard, unsold, valued by the estimate the system page uses.</summary>
    private static StatGroup AboardCartographics(CarriedDataSummary aboard) => new(Strings.Get(Keys.Stats_AboardUnsold),
    [
        new StatRow(Strings.Get(Keys.Stats_Systems), Display.Whole(aboard.Systems)),
        new StatRow(Strings.Get(Keys.Stats_BodiesScanned), Display.Whole(aboard.Bodies)),
        new StatRow(Strings.Get(Keys.Stats_BodiesMapped), Display.Whole(aboard.Mapped)),
        new StatRow(Strings.Get(Keys.Stats_EstimatedValue), Display.CreditsExact(aboard.CartographicsCredits)),
    ]);

    private IReadOnlyList<StatGroup> BuildExobiology(StatisticsSnapshot statistics, List<IncomeEntry> income)
    {
        var sold = income.Where(entry => entry.Kind == IncomeKind.Exobiology).ToList();
        var summary = Summarise(sold);
        var rows = sold
            .GroupBy(entry => entry.Detail, StringComparer.OrdinalIgnoreCase)
            .Select(group => new SpeciesRow(group.Key, group.Count(), group.Sum(entry => entry.Amount), group.Sum(entry => entry.Bonus)))
            .ToList();
        Species = SpeciesSort.Apply(rows);
        OnPropertyChanged(nameof(HasSpecies));
        var period = new StatGroup(Strings.Get(Keys.Stats_SoldInPeriod),
        [
            new StatRow(Strings.Get(Keys.Stats_WithFirstLoggedBonus), Display.CreditsExact(summary.Total)),
            new StatRow(Strings.Get(Keys.Stats_WithoutTheBonus), Display.CreditsExact(summary.Total - summary.Bonus)),
            new StatRow(Strings.Get(Keys.Stats_TheBonusAlone), Display.CreditsExact(summary.Bonus)),
            new StatRow(Strings.Get(Keys.Stats_SamplesSold), Display.Whole(summary.Count)),
            new StatRow(Strings.Get(Keys.Stats_SpeciesSold), Display.Whole(rows.Count)),
            new StatRow(Strings.Get(Keys.Stats_AveragePerSample), Display.CreditsExact(summary.Count == 0 ? 0 : summary.Total / summary.Count)),
            new StatRow(Strings.Get(Keys.Stats_BestSample), Display.CreditsExact(summary.Largest)),
        ]);
        return Groups(period, Lifetime(statistics, Strings.Get(Keys.Stats_Lifetime), "Exobiology", "Organic_Data_Profits", "First_Logged_Profits", "Organic_Data", "First_Logged", "Organic_Systems", "Organic_Planets", "Organic_Genus", "Organic_Species", "Organic_Variant_Encountered"));
    }

    private static IReadOnlyList<StatGroup> BuildExploration(StatisticsSnapshot statistics, List<IncomeEntry> income)
        => Groups(
            Ledger(Strings.Get(Keys.Stats_CartographicsSold), Summarise(income.Where(entry => entry.Kind == IncomeKind.Exploration)), Strings.Get(Keys.Stats_Sales)) with
            {
                Rows =
                [
                    .. Ledger("", Summarise(income.Where(entry => entry.Kind == IncomeKind.Exploration)), Strings.Get(Keys.Stats_Sales)).Rows,
                    new StatRow(Strings.Get(Keys.Stats_FirstDiscoveryWithin), Display.CreditsExact(income.Where(entry => entry.Kind == IncomeKind.Exploration).Sum(entry => entry.Bonus))),
                ],
            },
            Lifetime(statistics, Strings.Get(Keys.Stats_Lifetime), "Exploration", "Systems_Visited", "Exploration_Profits", "Planets_Scanned_To_Level_2", "Planets_Scanned_To_Level_3", "Efficient_Scans", "Highest_Payout", "Total_Hyperspace_Distance", "Total_Hyperspace_Jumps", "Greatest_Distance_From_Start", "Time_Played", "First_Footfalls", "Planet_Footfalls", "Settlements_Visited"));

    private static IReadOnlyList<StatGroup> BuildCombat(StatisticsSnapshot statistics, List<IncomeEntry> income)
        => Groups(
            Ledger(Strings.Get(Keys.Stats_BountiesInPeriod), Summarise(income.Where(entry => entry.Kind == IncomeKind.Bounties)), Strings.Get(Keys.Stats_VouchersRedeemed)),
            Ledger(Strings.Get(Keys.Stats_CombatBondsInPeriod), Summarise(income.Where(entry => entry.Kind == IncomeKind.CombatBonds)), Strings.Get(Keys.Stats_VouchersRedeemed)),
            Lifetime(statistics, Strings.Get(Keys.Stats_Lifetime), "Combat", "Bounties_Claimed", "Bounty_Hunting_Profit", "Combat_Bonds", "Combat_Bond_Profits", "Assassinations", "Assassination_Profits", "Highest_Single_Reward", "Skimmers_Killed", "OnFoot_Combat_Bonds", "OnFoot_Combat_Bonds_Profits", "OnFoot_Ships_Destroyed", "Dropships_Taken"));

    private static IReadOnlyList<StatGroup> BuildTrading(StatisticsSnapshot statistics, List<IncomeEntry> income)
    {
        var trades = income.Where(entry => entry.Kind == IncomeKind.Trading).ToList();
        var losses = trades.Count(entry => entry.Amount < 0);
        var trading = Ledger(Strings.Get(Keys.Stats_TradingInPeriod), Summarise(trades), Strings.Get(Keys.Stats_Sales));
        trading = trading with { Rows = [new StatRow(Strings.Get(Keys.Stats_Profit), trading.Rows[0].Value), .. trading.Rows.Skip(1), new StatRow(Strings.Get(Keys.Stats_SalesAtALoss), Display.Whole(losses))] };
        return Groups(
            trading,
            Ledger(Strings.Get(Keys.Stats_MiningInPeriod), Summarise(income.Where(entry => entry.Kind == IncomeKind.MiningAndSalvage)), Strings.Get(Keys.Stats_Sales)),
            Lifetime(statistics, Strings.Get(Keys.Stats_LifetimeTrading), "Trading", "Markets_Traded_With", "Market_Profits", "Resources_Traded", "Average_Profit", "Highest_Single_Transaction"),
            Lifetime(statistics, Strings.Get(Keys.Stats_LifetimeMining), "Mining", "Mining_Profits", "Quantity_Mined", "Materials_Collected"));
    }

    private static IReadOnlyList<StatGroup> BuildMissions(StatisticsSnapshot statistics, List<IncomeEntry> income)
        => Groups(
            Ledger(Strings.Get(Keys.Stats_MissionsInPeriod), Summarise(income.Where(entry => entry.Kind == IncomeKind.Missions)), Strings.Get(Keys.Stats_CompletedWithReward)),
            Ledger(Strings.Get(Keys.Stats_SearchAndRescueInPeriod), Summarise(income.Where(entry => entry.Kind == IncomeKind.SearchAndRescue)), Strings.Get(Keys.Stats_Rewards)),
            Ledger(Strings.Get(Keys.Stats_OtherIncomeInPeriod), Summarise(income.Where(entry => entry.Kind == IncomeKind.Other)), Strings.Get(Keys.Stats_Payments)),
            Lifetime(statistics, Strings.Get(Keys.Stats_LifetimePassengers), "Passengers", "Passengers_Missions_Accepted", "Passengers_Missions_Bulk", "Passengers_Missions_VIP", "Passengers_Missions_Delivered", "Passengers_Missions_Ejected"),
            Lifetime(statistics, Strings.Get(Keys.Stats_LifetimeSearchAndRescue), "Search_And_Rescue", "SearchRescue_Traded", "SearchRescue_Profit", "SearchRescue_Count"),
            Lifetime(statistics, Strings.Get(Keys.Stats_LifetimeCrime), "Crime", "Notoriety", "Fines", "Total_Fines", "Bounties_Received", "Total_Bounties", "Highest_Bounty"));

    private static IReadOnlyList<StatGroup> BuildPowerplay(StatisticsSnapshot statistics, List<IncomeEntry> income, DateTimeOffset since)
    {
        var activity = statistics.PowerplayActivity.Where(entry => entry.At >= since).ToList();
        var salary = Summarise(income.Where(entry => entry.Kind == IncomeKind.Powerplay));
        var merits = activity.Where(entry => entry.Action == PowerplayAction.Merits).ToList();
        var delivered = activity.Where(entry => entry.Action == PowerplayAction.Delivered).ToList();
        var collected = activity.Where(entry => entry.Action == PowerplayAction.Collected).ToList();
        var standing = statistics.Powerplay is { } pledge
            ? new StatGroup(Strings.Get(Keys.Stats_Standing),
            [
                new StatRow(Strings.Get(Keys.Stats_PledgedTo), pledge.Power),
                new StatRow(Strings.Get(Keys.Stats_Rank), Display.Whole(pledge.Rank)),
                new StatRow(Strings.Get(Keys.Stats_Merits), Display.Whole(pledge.Merits)),
                new StatRow(Strings.Get(Keys.Stats_TimePledged), Display.Duration(pledge.TimePledged)),
                new StatRow(Strings.Get(Keys.Stats_AsOf), pledge.AsOf.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)),
            ])
            : new StatGroup(Strings.Get(Keys.Stats_Standing), [new StatRow(Strings.Get(Keys.Stats_PledgedTo), Strings.Get(Keys.Stats_PledgedToNobody))]);
        var period = new StatGroup(Strings.Get(Keys.Stats_InPeriod),
        [
            new StatRow(Strings.Get(Keys.Stats_MeritsEarned), Display.Whole(merits.Sum(entry => entry.Value))),
            new StatRow(Strings.Get(Keys.Stats_MeritAwards), Display.Whole(merits.Count)),
            new StatRow(Strings.Get(Keys.Stats_LargestAward), Display.Whole(merits.Count == 0 ? 0 : merits.Max(entry => entry.Value))),
            new StatRow(Strings.Get(Keys.Stats_Salary), Display.CreditsExact(salary.Total)),
            new StatRow(Strings.Get(Keys.Stats_SalaryPayments), Display.Whole(salary.Count)),
            new StatRow(Strings.Get(Keys.Stats_GoodsDelivered), Strings.Format(Keys.Stats_UnitsIn, ("units", Display.Whole(delivered.Sum(entry => entry.Value))), ("runs", Display.Plural(Keys.Plural_Deliveries, delivered.Count)))),
            new StatRow(Strings.Get(Keys.Stats_GoodsCollected), Strings.Format(Keys.Stats_UnitsIn, ("units", Display.Whole(collected.Sum(entry => entry.Value))), ("runs", Display.Plural(Keys.Plural_Pickups, collected.Count)))),
        ]);
        return [standing, period];
    }

    private void RefreshTogether(StatisticsSnapshot statistics, DateTimeOffset since)
    {
        var now = statistics.Latest ?? DateTimeOffset.UtcNow;
        var sessions = statistics.Sessions
            .Select(session => (Session: session, Length: Overlap(session, since, now)))
            .Where(pair => pair.Length > TimeSpan.Zero)
            .ToList();
        var shared = statistics.Shared.Where(entry => entry.Income.At >= since).ToList();
        var wings = sessions.Where(pair => pair.Session.Company == Company.Wing).ToList();
        var crews = sessions.Where(pair => pair.Session.Company == Company.Crew).ToList();
        var inWing = shared.Where(entry => entry.Company == Company.Wing).ToList();
        var inCrew = shared.Where(entry => entry.Company == Company.Crew).ToList();
        TogetherGroups =
        [
            new StatGroup(Strings.Get(Keys.Stats_TimeTogetherInPeriod),
            [
                new StatRow(Strings.Get(Keys.Stats_InWings), Display.Duration(wings.Aggregate(TimeSpan.Zero, (sum, pair) => sum + pair.Length))),
                new StatRow(Strings.Get(Keys.Stats_WingSessions), Display.Whole(wings.Count)),
                new StatRow(Strings.Get(Keys.Stats_InCrews), Display.Duration(crews.Aggregate(TimeSpan.Zero, (sum, pair) => sum + pair.Length))),
                new StatRow(Strings.Get(Keys.Stats_CrewSessions), Display.Whole(crews.Count)),
                new StatRow(Strings.Get(Keys.Stats_LongestSession), Display.Duration(sessions.Count == 0 ? TimeSpan.Zero : sessions.Max(pair => pair.Length))),
                new StatRow(Strings.Get(Keys.Stats_PeopleFlownWith), Display.Whole(sessions.SelectMany(pair => pair.Session.Partners).Distinct(StringComparer.Ordinal).Count())),
            ]),
            new StatGroup(Strings.Get(Keys.Stats_SharedInPeriod),
            [
                new StatRow(Strings.Get(Keys.Stats_EverythingShared), Display.CreditsExact(shared.Sum(entry => entry.Income.Amount))),
                new StatRow(Strings.Get(Keys.Stats_BountiesSharedWing), Display.CreditsExact(inWing.Where(entry => entry.Income.Kind == IncomeKind.Bounties).Sum(entry => entry.Income.Amount))),
                new StatRow(Strings.Get(Keys.Stats_TradeDividends), Display.CreditsExact(inWing.Where(entry => entry.Income.Kind == IncomeKind.Trading).Sum(entry => entry.Income.Amount))),
                new StatRow(Strings.Get(Keys.Stats_WingMissions), Display.CreditsExact(inWing.Where(entry => entry.Income.Kind == IncomeKind.Missions).Sum(entry => entry.Income.Amount))),
                new StatRow(Strings.Get(Keys.Stats_BountiesInCrew), Display.CreditsExact(inCrew.Where(entry => entry.Income.Kind == IncomeKind.Bounties).Sum(entry => entry.Income.Amount))),
                new StatRow(Strings.Get(Keys.Stats_CombatBondsInCrew), Display.CreditsExact(inCrew.Where(entry => entry.Income.Kind == IncomeKind.CombatBonds).Sum(entry => entry.Income.Amount))),
                new StatRow(Strings.Get(Keys.Stats_SharedPayments), Display.Whole(shared.Count)),
            ]),
        ];
        var rows = sessions
            .SelectMany(pair => pair.Session.Partners.Select(name => (Name: name, pair.Session, pair.Length)))
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .Select(group => new PartnerRow(
                group.Key,
                group.Count(),
                group.Aggregate(TimeSpan.Zero, (sum, item) => sum + item.Length),
                shared.Where(entry => entry.Partners.Contains(group.Key, StringComparer.Ordinal)).Sum(entry => entry.Income.Amount),
                group.Max(item => item.Session.End ?? now)))
            .ToList();
        Partners = PartnerSort.Apply(rows);
        OnPropertyChanged(nameof(HasPartners));
        WingMissions = WingMissionSort.Apply(statistics.WingMissions
            .Where(mission => mission.AcceptedAt >= since || mission.Outcome == MissionOutcome.Active)
            .Select(mission => new WingMissionRow(mission.Name, mission.Faction, mission.Reward, mission.AcceptedAt, mission.Partners, mission.Outcome, mission.Paid)));
        OnPropertyChanged(nameof(HasWingMissions));
        TogetherNote = statistics.Sessions.Count == 0
            ? Strings.Get(Keys.Stats_NoWingYet)
            : Strings.Get(Keys.Stats_SharedNote);
    }

    private static TimeSpan Overlap(CompanySession session, DateTimeOffset since, DateTimeOffset now)
    {
        var start = session.Start > since ? session.Start : since;
        var end = session.End ?? now;
        return end > start ? end - start : TimeSpan.Zero;
    }

    private sealed record Summary(int Count, long Total, long Bonus, long Largest);
}
