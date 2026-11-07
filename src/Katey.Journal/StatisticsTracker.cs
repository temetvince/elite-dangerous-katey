using System.Text.Json;

namespace Katey.Journal;

/// <summary>What a credit of income came from.</summary>
public enum IncomeKind
{
    /// <summary>Bounty vouchers redeemed.</summary>
    Bounties,

    /// <summary>Combat bonds redeemed.</summary>
    CombatBonds,

    /// <summary>Profit on commodities bought and sold: the sale less what was paid.</summary>
    Trading,

    /// <summary>Sales of commodities that cost nothing: mined, collected or salvaged.</summary>
    MiningAndSalvage,

    /// <summary>Cartographic data sold.</summary>
    Exploration,

    /// <summary>Organic data sold.</summary>
    Exobiology,

    /// <summary>Mission rewards.</summary>
    Missions,

    /// <summary>Search and rescue rewards.</summary>
    SearchAndRescue,

    /// <summary>Powerplay salary.</summary>
    Powerplay,

    /// <summary>Everything else: settlement and scannable vouchers, data links, micro-resources, limpets.</summary>
    Other,
}

/// <summary>One credit of income.</summary>
/// <param name="At">When it was paid.</param>
/// <param name="Kind">What it came from.</param>
/// <param name="Amount">Credits paid, bonus included. Negative for a trade sold at a loss.</param>
/// <param name="Bonus">The part of <paramref name="Amount"/> that was a bonus: first discovery for cartographics, first logging for organic data. Zero otherwise.</param>
/// <param name="Detail">What it was for: the species, the commodity, the voucher type or the mission, as the journal names it; empty when it says nothing more.</param>
public sealed record IncomeEntry(DateTimeOffset At, IncomeKind Kind, long Amount, long Bonus, string Detail);

/// <summary>One credit of income the game marks as shared with other players.</summary>
/// <param name="Income">The payment. A bounty or kill bond is the voucher as it was earned, not its later redemption.</param>
/// <param name="Company">Whether it was shared across a wing or a crew.</param>
/// <param name="Partners">Who the player was flying with when it was earned; empty when the journal does not say, as for a dividend redeemed later.</param>
public sealed record SharedEntry(IncomeEntry Income, Company Company, IReadOnlyList<string> Partners);

/// <summary>How a wing mission stands.</summary>
public enum MissionOutcome
{
    /// <summary>Accepted and not yet over.</summary>
    Active,

    /// <summary>Handed in for its reward.</summary>
    Completed,

    /// <summary>Failed, the game's expiry included.</summary>
    Failed,

    /// <summary>Given up.</summary>
    Abandoned,

    /// <summary>Gone from the game's mission list without the journal saying why, as when it expired while the game was closed.</summary>
    Lapsed,
}

/// <summary>A mission accepted as a wing mission, whether taken from the board or shared by a wingmate: the journal does not say which.</summary>
/// <param name="MissionId">The journal's id.</param>
/// <param name="Name">The mission as the game shows it, or its symbol when the journal gives no text.</param>
/// <param name="Faction">Who gave it; empty when the journal does not say.</param>
/// <param name="Reward">Credits on offer when it was accepted.</param>
/// <param name="AcceptedAt">When it was accepted.</param>
/// <param name="Partners">Who was in the wing when it was accepted; empty when accepted alone.</param>
/// <param name="Outcome">How it stands.</param>
/// <param name="EndedAt">When it was completed, failed, abandoned or found gone, or <see langword="null"/> while active.</param>
/// <param name="Paid">Credits paid on completion; zero otherwise.</param>
public sealed record WingMission(
    long MissionId,
    string Name,
    string Faction,
    long Reward,
    DateTimeOffset AcceptedAt,
    IReadOnlyList<string> Partners,
    MissionOutcome Outcome,
    DateTimeOffset? EndedAt,
    long Paid);

/// <summary>What a Powerplay event did.</summary>
public enum PowerplayAction
{
    /// <summary>Merits were earned. The value is the merits.</summary>
    Merits,

    /// <summary>Powerplay goods were delivered. The value is the units.</summary>
    Delivered,

    /// <summary>Powerplay goods were collected. The value is the units.</summary>
    Collected,
}

/// <summary>One thing done for a power.</summary>
/// <param name="At">When.</param>
/// <param name="Action">What was done.</param>
/// <param name="Value">Merits or units, as the action says.</param>
/// <param name="Detail">The goods delivered or collected, as the journal names them, or empty.</param>
public sealed record PowerplayEntry(DateTimeOffset At, PowerplayAction Action, long Value, string Detail);

/// <summary>The commander's standing with a power, as the journal last reported it.</summary>
/// <param name="Power">The power pledged to.</param>
/// <param name="Rank">The rank held.</param>
/// <param name="Merits">Merits held.</param>
/// <param name="TimePledged">How long the pledge has stood.</param>
/// <param name="AsOf">When the journal last said.</param>
public sealed record PowerplayStanding(string Power, int Rank, long Merits, TimeSpan TimePledged, DateTimeOffset AsOf);

/// <summary>One section of the game's lifetime statistics, such as <c>Exploration</c> or <c>Bank_Account</c>.</summary>
/// <param name="Name">The section name as the journal spells it.</param>
/// <param name="Figures">Every numeric figure in it by its journal key, in the journal's order.</param>
public sealed record LifetimeSection(string Name, IReadOnlyDictionary<string, double> Figures);

/// <summary>The game's lifetime figures and the income the journal history records.</summary>
/// <param name="AsOf">When the game last wrote its statistics, or <see langword="null"/> before it has.</param>
/// <param name="Lifetime">The statistics sections in the journal's order; empty before the first <c>Statistics</c> event.</param>
/// <param name="Income">Every credit of income in the history, oldest first.</param>
/// <param name="Powerplay">The standing with a power, or <see langword="null"/> when not pledged or not yet reported.</param>
/// <param name="PowerplayActivity">Merits earned and goods moved for a power, oldest first.</param>
/// <param name="Shared">Income the game marks as shared with other players, oldest first. Separate from <paramref name="Income"/>: a shared bounty is here as the voucher and there as its redemption.</param>
/// <param name="Sessions">Every wing and crew session in the history, oldest first, open ones last.</param>
/// <param name="WingMissions">Every mission accepted as a wing mission in the history, oldest first, with how each stands.</param>
/// <param name="Latest">The timestamp of the last event read, or <see langword="null"/> before any; the time an open session is measured to.</param>
public sealed record StatisticsSnapshot(
    DateTimeOffset? AsOf,
    IReadOnlyList<LifetimeSection> Lifetime,
    IReadOnlyList<IncomeEntry> Income,
    PowerplayStanding? Powerplay,
    IReadOnlyList<PowerplayEntry> PowerplayActivity,
    IReadOnlyList<SharedEntry> Shared,
    IReadOnlyList<CompanySession> Sessions,
    IReadOnlyList<WingMission> WingMissions,
    DateTimeOffset? Latest)
{
    /// <summary>Gets nothing known yet.</summary>
    public static StatisticsSnapshot Empty { get; } = new(null, [], [], null, [], [], [], [], null);

    /// <summary>One lifetime figure.</summary>
    /// <param name="section">The section name, such as <c>Exploration</c>.</param>
    /// <param name="key">The figure's key, such as <c>Time_Played</c>.</param>
    /// <returns>The figure, or <see langword="null"/> when the game has not reported it.</returns>
    public double? Figure(string section, string key)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(key);
        foreach (var candidate in Lifetime)
        {
            if (string.Equals(candidate.Name, section, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pair in candidate.Figures)
                {
                    if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return pair.Value;
                    }
                }
            }
        }

        return null;
    }
}

/// <summary>
/// Keeps the game's lifetime statistics, which it writes at every <c>Statistics</c> event, a ledger of
/// income built from every sale, voucher, reward and salary in the journal history, and a second ledger of
/// the income the game marks as shared with other players.
/// </summary>
/// <remarks>
/// A commodity sale counts as trading profit, the sale less the price paid, unless nothing was paid for the
/// goods, when it counts as mining and salvage. The game's own lifetime figures are kept whole, section by
/// section, numbers only. The Powerplay standing comes from the <c>Powerplay</c> event at each load and is
/// kept current by rank and merit events; leaving or defecting clears it. Shared income is only what the
/// journal marks: a <c>Bounty</c> with a share count, any bounty or kill bond earned in a crew, a trade
/// dividend voucher, and a mission accepted as a wing mission. Income merely earned while in a wing is not
/// shared.
/// </remarks>
public sealed class StatisticsTracker : IJournalSink
{
    private static readonly HashSet<string> Wanted = new HashSet<string>(StringComparer.Ordinal)
    {
        "Statistics", "RedeemVoucher", "SellExplorationData", "MultiSellExplorationData", "SellOrganicData",
        "MarketSell", "MissionCompleted", "SearchAndRescue", "PowerplaySalary", "DatalinkVoucher",
        "SellMicroResources", "SellDrones", "Powerplay", "PowerplayRank", "PowerplayMerits", "PowerplayDeliver",
        "PowerplayCollect", "PowerplayLeave", "PowerplayDefect", "Bounty", "FactionKillBond", "MissionAccepted",
        "MissionAbandoned", "MissionFailed", "Missions",
    }.Union(CompanyLog.Events).ToHashSet(StringComparer.Ordinal);

    private readonly Lock _lock = new();
    private readonly List<IncomeEntry> _income = [];
    private readonly List<SharedEntry> _shared = [];
    private readonly List<PowerplayEntry> _powerplay = [];
    private readonly List<WingMission> _wingMissions = [];
    private readonly CompanyLog _company = new();
    private PowerplayStanding? _standing;
    private DateTimeOffset? _asOf;
    private DateTimeOffset? _latest;
    private List<LifetimeSection> _lifetime = [];
    private StatisticsSnapshot _snapshot = StatisticsSnapshot.Empty;

    /// <inheritdoc/>
    public IReadOnlySet<string>? InterestedEvents => Wanted;

    /// <summary>An immutable view of the statistics.</summary>
    /// <returns>The latest snapshot. Safe to call from any thread.</returns>
    public StatisticsSnapshot Snapshot()
    {
        lock (_lock)
        {
            return _snapshot;
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_lock)
        {
            _income.Clear();
            _shared.Clear();
            _powerplay.Clear();
            _wingMissions.Clear();
            _company.Reset();
            _standing = null;
            _lifetime = [];
            _asOf = null;
            _latest = null;
            _snapshot = StatisticsSnapshot.Empty;
        }
    }

    /// <inheritdoc/>
    public bool Apply(JournalEvent journalEvent)
    {
        ArgumentNullException.ThrowIfNull(journalEvent);
        lock (_lock)
        {
            var payload = journalEvent.Payload;
            var at = journalEvent.Timestamp;
            _latest = at;
            var changed = journalEvent.Name switch
            {
                "Statistics" => ApplyStatistics(payload, at),
                "RedeemVoucher" => ApplyVoucher(payload, at),
                "SellExplorationData" or "MultiSellExplorationData" => Add(at, IncomeKind.Exploration, Credits(payload, "TotalEarnings"), Credits(payload, "Bonus"), ""),
                "SellOrganicData" => ApplyOrganic(payload, at),
                "MarketSell" => ApplyMarketSell(payload, at),
                "MissionCompleted" => ApplyMissionCompleted(payload, at),
                "MissionAccepted" => RememberWingMission(payload, at),
                "MissionAbandoned" => EndWingMission(payload, at, MissionOutcome.Abandoned),
                "MissionFailed" => EndWingMission(payload, at, MissionOutcome.Failed),
                "Missions" => ReconcileMissions(payload, at),
                "Bounty" => ApplyBounty(payload, at),
                "FactionKillBond" => ApplyKillBond(payload, at),
                "SearchAndRescue" => Add(at, IncomeKind.SearchAndRescue, Credits(payload, "Reward"), 0, JsonRead.Localised(payload, "Name") ?? JsonRead.Text(payload, "Name") ?? ""),
                "PowerplaySalary" => Add(at, IncomeKind.Powerplay, Credits(payload, "Amount"), 0, JsonRead.Text(payload, "Power") ?? ""),
                "DatalinkVoucher" => Add(at, IncomeKind.Other, Credits(payload, "Reward"), 0, "data link"),
                "SellMicroResources" => Add(at, IncomeKind.Other, Credits(payload, "Price"), 0, "micro-resources"),
                "SellDrones" => Add(at, IncomeKind.Other, Credits(payload, "TotalSale"), 0, "limpets"),
                "Powerplay" => ApplyPledge(payload, at),
                "PowerplayRank" => ApplyRank(payload, at),
                "PowerplayMerits" => ApplyMerits(payload, at),
                "PowerplayDeliver" => AddPowerplay(at, PowerplayAction.Delivered, JsonRead.Count(payload, "Count"), JsonRead.Localised(payload, "Type") ?? JsonRead.Text(payload, "Type") ?? ""),
                "PowerplayCollect" => AddPowerplay(at, PowerplayAction.Collected, JsonRead.Count(payload, "Count"), JsonRead.Localised(payload, "Type") ?? JsonRead.Text(payload, "Type") ?? ""),
                "PowerplayLeave" or "PowerplayDefect" => Unpledge(),
                _ => _company.Apply(journalEvent.Name, payload, at),
            };
            if (changed)
            {
                _snapshot = new StatisticsSnapshot(_asOf, _lifetime, [.. _income], _standing, [.. _powerplay], [.. _shared], _company.Sessions(), [.. _wingMissions], _latest);
            }

            return changed;
        }
    }

    private bool ApplyVoucher(JsonElement payload, DateTimeOffset at)
    {
        var type = JsonRead.Text(payload, "Type") ?? "";
        var amount = Credits(payload, "Amount");
        if (string.Equals(type, "trade", StringComparison.OrdinalIgnoreCase))
        {
            // Trade dividends are the wing's cut of a wingmate's sale, redeemed later: shared by definition.
            return Add(at, IncomeKind.Trading, amount, 0, "trade dividends", allowZero: true, shared: Company.Wing, partners: _company.WingPartners);
        }

        return Add(at, VoucherKind(type), amount, 0, type);
    }

    private bool ApplyBounty(JsonElement payload, DateTimeOffset at)
    {
        var shared = JsonRead.Count(payload, "SharedWithOthers") > 0;
        if (!shared && !_company.InCrew)
        {
            return false;
        }

        var amount = payload.TryGetProperty("TotalReward", out _) ? Credits(payload, "TotalReward") : Credits(payload, "Reward");
        var target = JsonRead.Localised(payload, "Target") ?? JsonRead.Text(payload, "Target") ?? "";
        return AddShared(at, IncomeKind.Bounties, amount, target);
    }

    private bool ApplyKillBond(JsonElement payload, DateTimeOffset at)
        => _company.InCrew && AddShared(at, IncomeKind.CombatBonds, Credits(payload, "Reward"), JsonRead.Localised(payload, "AwardingFaction") ?? JsonRead.Text(payload, "AwardingFaction") ?? "");

    /// <summary>Records a mission accepted as a wing mission, with the wing at the time, so it can be listed and its completion counts as shared.</summary>
    private bool RememberWingMission(JsonElement payload, DateTimeOffset at)
    {
        if (!JsonRead.Flag(payload, "Wing"))
        {
            return false;
        }

        var id = JsonRead.Identifier(payload, "MissionID");
        _wingMissions.RemoveAll(mission => mission.MissionId == id);
        _wingMissions.Add(new WingMission(
            id,
            MissionName(payload),
            JsonRead.Text(payload, "Faction") ?? "",
            Credits(payload, "Reward"),
            at,
            _company.WingPartners,
            MissionOutcome.Active,
            null,
            0));
        return true;
    }

    /// <summary>Settles an active wing mission.</summary>
    /// <returns><see langword="true"/> when the mission was an active wing mission.</returns>
    private bool EndWingMission(JsonElement payload, DateTimeOffset at, MissionOutcome outcome, long paid = 0)
    {
        var id = JsonRead.Identifier(payload, "MissionID");
        var index = _wingMissions.FindIndex(mission => mission.MissionId == id && mission.Outcome == MissionOutcome.Active);
        if (index < 0)
        {
            return false;
        }

        _wingMissions[index] = _wingMissions[index] with { Outcome = outcome, EndedAt = at, Paid = paid };
        return true;
    }

    /// <summary>
    /// Settles the wing missions the game no longer lists when the player loads in: a listed failure is failed,
    /// one missing from every list has lapsed. One listed as complete is still awaiting its reward and stays active.
    /// </summary>
    private bool ReconcileMissions(JsonElement payload, DateTimeOffset at)
    {
        var active = MissionIds(payload, "Active");
        var complete = MissionIds(payload, "Complete");
        var failed = MissionIds(payload, "Failed");
        var changed = false;
        for (var index = 0; index < _wingMissions.Count; index++)
        {
            var mission = _wingMissions[index];
            if (mission.Outcome != MissionOutcome.Active || active.Contains(mission.MissionId) || complete.Contains(mission.MissionId))
            {
                continue;
            }

            _wingMissions[index] = mission with { Outcome = failed.Contains(mission.MissionId) ? MissionOutcome.Failed : MissionOutcome.Lapsed, EndedAt = at };
            changed = true;
        }

        return changed;
    }

    private static HashSet<long> MissionIds(JsonElement payload, string list)
        => [.. JsonRead.Items(payload, list).Select(item => JsonRead.Identifier(item, "MissionID"))];

    private static string MissionName(JsonElement payload)
        => JsonRead.Text(payload, "LocalisedName") ?? JsonRead.Localised(payload, "Name") ?? JsonRead.Text(payload, "Name") ?? "";

    private bool ApplyMissionCompleted(JsonElement payload, DateTimeOffset at)
    {
        var name = MissionName(payload);
        var reward = Credits(payload, "Reward");
        var wing = EndWingMission(payload, at, MissionOutcome.Completed, reward);
        var added = wing
            ? Add(at, IncomeKind.Missions, reward, 0, name, shared: Company.Wing, partners: _company.WingPartners)
            : Add(at, IncomeKind.Missions, reward, 0, name);
        return wing || added;
    }

    /// <summary>Records a voucher earned together: in a crew when in one, else across the wing.</summary>
    private bool AddShared(DateTimeOffset at, IncomeKind kind, long amount, string detail)
    {
        if (amount == 0)
        {
            return false;
        }

        var company = _company.InCrew ? Company.Crew : Company.Wing;
        _shared.Add(new SharedEntry(new IncomeEntry(at, kind, amount, 0, detail), company, company == Company.Crew ? _company.CrewPartners : _company.WingPartners));
        return true;
    }

    private static long Credits(JsonElement element, string property) => (long)Math.Round(JsonRead.Measure(element, property) ?? 0);

    private static IncomeKind VoucherKind(string? type) => type?.ToUpperInvariant() switch
    {
        "BOUNTY" => IncomeKind.Bounties,
        "COMBATBOND" => IncomeKind.CombatBonds,
        _ => IncomeKind.Other,
    };

    private bool ApplyStatistics(JsonElement payload, DateTimeOffset at)
    {
        var sections = new List<LifetimeSection>();
        foreach (var section in payload.EnumerateObject())
        {
            if (section.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var figures = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var figure in section.Value.EnumerateObject())
            {
                if (figure.Value.ValueKind == JsonValueKind.Number && figure.Value.TryGetDouble(out var number))
                {
                    figures[figure.Name] = number;
                }
            }

            if (figures.Count > 0)
            {
                sections.Add(new LifetimeSection(section.Name, figures));
            }
        }

        _lifetime = sections;
        _asOf = at;
        return true;
    }

    private bool ApplyOrganic(JsonElement payload, DateTimeOffset at)
    {
        var changed = false;
        foreach (var item in JsonRead.Items(payload, "BioData"))
        {
            var value = Credits(item, "Value");
            var bonus = Credits(item, "Bonus");
            var species = JsonRead.Localised(item, "Species") ?? JsonRead.Text(item, "Species") ?? "unknown species";
            changed |= Add(at, IncomeKind.Exobiology, value + bonus, bonus, species);
        }

        return changed;
    }

    private bool ApplyMarketSell(JsonElement payload, DateTimeOffset at)
    {
        var count = JsonRead.Count(payload, "Count");
        var total = Credits(payload, "TotalSale");
        var paid = Credits(payload, "AvgPricePaid");
        var commodity = JsonRead.Localised(payload, "Type") ?? JsonRead.Text(payload, "Type") ?? "";
        return paid == 0
            ? Add(at, IncomeKind.MiningAndSalvage, total, 0, commodity)
            : Add(at, IncomeKind.Trading, total - (count * paid), 0, commodity, allowZero: true);
    }

    private bool ApplyPledge(JsonElement payload, DateTimeOffset at)
    {
        if (JsonRead.Text(payload, "Power") is not { Length: > 0 } power)
        {
            return false;
        }

        _standing = new PowerplayStanding(
            power,
            JsonRead.Count(payload, "Rank"),
            Credits(payload, "Merits"),
            TimeSpan.FromSeconds(JsonRead.Measure(payload, "TimePledged") ?? 0),
            at);
        return true;
    }

    private bool ApplyRank(JsonElement payload, DateTimeOffset at)
    {
        var power = JsonRead.Text(payload, "Power") ?? _standing?.Power;
        if (power is null)
        {
            return false;
        }

        _standing = new PowerplayStanding(power, JsonRead.Count(payload, "Rank"), _standing?.Merits ?? 0, _standing?.TimePledged ?? TimeSpan.Zero, at);
        return true;
    }

    private bool ApplyMerits(JsonElement payload, DateTimeOffset at)
    {
        var gained = Credits(payload, "MeritsGained");
        var power = JsonRead.Text(payload, "Power") ?? _standing?.Power;
        if (power is not null)
        {
            _standing = new PowerplayStanding(power, _standing?.Rank ?? 0, Credits(payload, "TotalMerits"), _standing?.TimePledged ?? TimeSpan.Zero, at);
        }

        return AddPowerplay(at, PowerplayAction.Merits, gained, "") | power is not null;
    }

    private bool Unpledge()
    {
        if (_standing is null)
        {
            return false;
        }

        _standing = null;
        return true;
    }

    private bool AddPowerplay(DateTimeOffset at, PowerplayAction action, long value, string detail)
    {
        if (value <= 0)
        {
            return false;
        }

        _powerplay.Add(new PowerplayEntry(at, action, value, detail));
        return true;
    }

    private bool Add(DateTimeOffset at, IncomeKind kind, long amount, long bonus, string detail, bool allowZero = false, Company? shared = null, IReadOnlyList<string>? partners = null)
    {
        if (amount == 0 && !allowZero)
        {
            return false;
        }

        var entry = new IncomeEntry(at, kind, amount, bonus, detail);
        _income.Add(entry);
        if (shared is { } company)
        {
            _shared.Add(new SharedEntry(entry, company, partners ?? []));
        }

        return true;
    }
}
