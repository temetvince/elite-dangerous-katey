namespace Katey.Journal.Tests;

/// <summary>Checks the lifetime figures, the income ledger by activity, and the Powerplay standing.</summary>
public sealed class StatisticsTrackerTests
{
    private static void Feed(StatisticsTracker tracker, params string[] lines)
    {
        foreach (var line in lines)
        {
            var journalEvent = JournalEvent.Parse(line) ?? throw new InvalidOperationException("fixture did not parse: " + line);
            if (tracker.InterestedEvents!.Contains(journalEvent.Name))
            {
                tracker.Apply(journalEvent);
            }
        }
    }

    [Fact]
    public void Apply_KeepsLifetimeFigures_AndLedgersEveryKindOfIncome()
    {
        var tracker = new StatisticsTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Statistics","Bank_Account":{"Current_Wealth":123456789,"Spent_On_Ships":50000000},"Exploration":{"Systems_Visited":2345,"Time_Played":360000,"Total_Hyperspace_Distance":98765.4},"Exobiology":{"Organic_Data_Profits":987654321,"First_Logged":12},"TG_ENCOUNTERS":{"TG_ENCOUNTER_TOTAL_LAST_SYSTEM":"Sol"}}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"RedeemVoucher","Type":"bounty","Amount":150000,"Factions":[{"Faction":"A","Amount":150000}]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"RedeemVoucher","Type":"CombatBond","Amount":90000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:03:00Z","event":"SellOrganicData","MarketID":1,"BioData":[{"Genus":"$Codex_Ent_Stratum_Genus_Name;","Genus_Localised":"Stratum","Species":"$Codex_Ent_Stratum_07_Name;","Species_Localised":"Stratum Tectonicas","Value":19010800,"Bonus":76043200},{"Genus":"$Codex_Ent_Bacterial_Genus_Name;","Species":"$Codex_Ent_Bacterial_01_Name;","Species_Localised":"Bacterium Aurasus","Value":1000000,"Bonus":0}]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:04:00Z","event":"MarketSell","MarketID":1,"Type":"gold","Type_Localised":"Gold","Count":10,"SellPrice":50000,"TotalSale":500000,"AvgPricePaid":45000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:05:00Z","event":"MarketSell","MarketID":1,"Type":"painite","Type_Localised":"Painite","Count":5,"SellPrice":200000,"TotalSale":1000000,"AvgPricePaid":0}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:06:00Z","event":"MissionCompleted","Name":"Mission_Courier_name","LocalisedName":"Courier job","Reward":250000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:07:00Z","event":"SellExplorationData","Systems":["A"],"Discovered":["A"],"BaseValue":100000,"Bonus":20000,"TotalEarnings":120000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:08:00Z","event":"MultiSellExplorationData","Discovered":[],"BaseValue":5000,"Bonus":0,"TotalEarnings":5000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:09:00Z","event":"SearchAndRescue","MarketID":1,"Name":"occupiedcryopod","Count":1,"Reward":30000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:10:00Z","event":"PowerplaySalary","Power":"Zemina Torval","Amount":700000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:11:00Z","event":"DatalinkVoucher","Reward":10000,"VictimFaction":"X","PayeeFaction":"Y"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:12:00Z","event":"MissionCompleted","Name":"Mission_Donate_name","Reward":0}""");

        var snapshot = tracker.Snapshot();
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero), snapshot.AsOf);
        Assert.Equal(["Bank_Account", "Exploration", "Exobiology"], snapshot.Lifetime.Select(section => section.Name));
        Assert.Equal(360000, snapshot.Figure("Exploration", "Time_Played"));
        Assert.Equal(12, snapshot.Figure("exobiology", "first_logged"));
        Assert.Null(snapshot.Figure("Exploration", "Nope"));

        var byKind = snapshot.Income.ToLookup(entry => entry.Kind);
        Assert.Equal(150000, byKind[IncomeKind.Bounties].Sum(entry => entry.Amount));
        Assert.Equal(90000, byKind[IncomeKind.CombatBonds].Sum(entry => entry.Amount));
        var organic = byKind[IncomeKind.Exobiology].ToList();
        Assert.Equal(2, organic.Count);
        Assert.Equal("Stratum Tectonicas", organic[0].Detail);
        Assert.Equal(95054000, organic[0].Amount);
        Assert.Equal(76043200, organic[0].Bonus);
        Assert.Equal(1000000, organic[1].Amount);
        var trade = Assert.Single(byKind[IncomeKind.Trading]);
        Assert.Equal(50000, trade.Amount);
        Assert.Equal("Gold", trade.Detail);
        var mined = Assert.Single(byKind[IncomeKind.MiningAndSalvage]);
        Assert.Equal(1000000, mined.Amount);
        var mission = Assert.Single(byKind[IncomeKind.Missions]);
        Assert.Equal("Courier job", mission.Detail);
        Assert.Equal(250000, mission.Amount);
        Assert.Equal(125000, byKind[IncomeKind.Exploration].Sum(entry => entry.Amount));
        Assert.Equal(20000, byKind[IncomeKind.Exploration].Sum(entry => entry.Bonus));
        Assert.Equal(30000, Assert.Single(byKind[IncomeKind.SearchAndRescue]).Amount);
        Assert.Equal(700000, Assert.Single(byKind[IncomeKind.Powerplay]).Amount);
        Assert.Equal(10000, Assert.Single(byKind[IncomeKind.Other]).Amount);

        tracker.Reset();
        Assert.Same(StatisticsSnapshot.Empty, tracker.Snapshot());
    }

    [Fact]
    public void Apply_KeepsWingMissions_WithTheWingAtAcceptance_AndHowEachEnded()
    {
        var tracker = new StatisticsTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"WingJoin","Others":["Alice"]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"MissionAccepted","Faction":"F","Name":"Mission_MassacreWing","LocalisedName":"Massacre 20 pirates","MissionID":1,"Reward":500000,"Wing":true}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"MissionAccepted","Faction":"G","Name":"Mission_Delivery","LocalisedName":"Deliver 10 tonnes","MissionID":2,"Reward":100000,"Wing":false}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:03:00Z","event":"MissionAccepted","Faction":"H","Name":"Mission_MassacreWing","LocalisedName":"Massacre 30 pirates","MissionID":3,"Reward":700000,"Wing":true}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:04:00Z","event":"MissionAccepted","Faction":"H","Name":"Mission_MassacreWing","LocalisedName":"Massacre 40 pirates","MissionID":4,"Reward":900000,"Wing":true}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:00:00Z","event":"MissionCompleted","Faction":"F","Name":"Mission_MassacreWing_name","MissionID":1,"Reward":480000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:30:00Z","event":"MissionFailed","Name":"Mission_MassacreWing","MissionID":3}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T12:00:00Z","event":"WingLeave"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-06T09:00:00Z","event":"MissionAccepted","Faction":"J","Name":"Mission_MassacreWing","LocalisedName":"Massacre 50 pirates","MissionID":5,"Reward":950000,"Wing":true}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-07T09:00:00Z","event":"Missions","Active":[{"MissionID":5,"Name":"Mission_MassacreWing","PassengerMission":false,"Expires":3600}],"Failed":[],"Complete":[]}""");

        var missions = tracker.Snapshot().WingMissions;
        Assert.Equal([1L, 3L, 4L, 5L], missions.Select(mission => mission.MissionId));
        var first = missions[0];
        Assert.Equal("Massacre 20 pirates", first.Name);
        Assert.Equal("F", first.Faction);
        Assert.Equal(500000, first.Reward);
        Assert.Equal(["Alice"], first.Partners);
        Assert.Equal(MissionOutcome.Completed, first.Outcome);
        Assert.Equal(480000, first.Paid);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 11, 0, 0, TimeSpan.Zero), first.EndedAt);
        Assert.Equal(MissionOutcome.Failed, missions[1].Outcome);
        Assert.Equal(MissionOutcome.Lapsed, missions[2].Outcome);
        Assert.Equal(MissionOutcome.Active, missions[3].Outcome);
        Assert.Empty(missions[3].Partners);
        Assert.Single(tracker.Snapshot().Shared, entry => entry.Income.Kind == IncomeKind.Missions && entry.Income.Amount == 480000);
    }

    [Fact]
    public void Apply_SharesOnlyWhatTheGameMarks_AndKeepsTheSessions()
    {
        var tracker = new StatisticsTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Bounty","Rewards":[{"Faction":"A","Reward":1000}],"Target":"adder","TotalReward":1000,"VictimFaction":"V"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"WingJoin","Others":["Alice"]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"WingAdd","Name":"Bob"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:03:00Z","event":"Bounty","Rewards":[{"Faction":"A","Reward":50000}],"Target":"anaconda","Target_Localised":"Anaconda","TotalReward":50000,"VictimFaction":"V","SharedWithOthers":2}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:04:00Z","event":"Bounty","Rewards":[{"Faction":"A","Reward":700}],"Target":"sidewinder","TotalReward":700,"VictimFaction":"V"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:05:00Z","event":"SellExplorationData","Systems":["A"],"Discovered":["A"],"BaseValue":100000,"Bonus":20000,"TotalEarnings":120000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:06:00Z","event":"MissionAccepted","Name":"Mission_Massacre_name","MissionID":7,"Wing":true,"Reward":900000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:07:00Z","event":"MissionAccepted","Name":"Mission_Courier_name","MissionID":8,"Wing":false,"Reward":1000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:30:00Z","event":"WingLeave"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:31:00Z","event":"MissionCompleted","Name":"Mission_Massacre_name","LocalisedName":"Massacre","MissionID":7,"Reward":900000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:32:00Z","event":"MissionCompleted","Name":"Mission_Courier_name","LocalisedName":"Courier","MissionID":8,"Reward":1000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:33:00Z","event":"RedeemVoucher","Type":"trade","Amount":25000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:34:00Z","event":"RedeemVoucher","Type":"bounty","Amount":51700}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:00:00Z","event":"CrewMemberJoins","Crew":"Carol","Telepresence":true}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:10:00Z","event":"FactionKillBond","Reward":30000,"AwardingFaction":"F","VictimFaction":"V"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:11:00Z","event":"Bounty","Rewards":[{"Faction":"A","Reward":4000}],"Target":"eagle","TotalReward":4000,"VictimFaction":"V"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:12:00Z","event":"CrewMemberQuits","Crew":"Carol"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:13:00Z","event":"FactionKillBond","Reward":999,"AwardingFaction":"F","VictimFaction":"V"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T12:00:00Z","event":"WingAdd","Name":"Dave"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T12:45:00Z","event":"Shutdown"}""");

        var snapshot = tracker.Snapshot();
        var shared = snapshot.Shared;
        Assert.Equal([50000L, 900000L, 25000L, 30000L, 4000L], shared.Select(entry => entry.Income.Amount));
        Assert.Equal([IncomeKind.Bounties, IncomeKind.Missions, IncomeKind.Trading, IncomeKind.CombatBonds, IncomeKind.Bounties], shared.Select(entry => entry.Income.Kind));
        Assert.Equal([Company.Wing, Company.Wing, Company.Wing, Company.Crew, Company.Crew], shared.Select(entry => entry.Company));
        Assert.Equal(["Alice", "Bob"], shared[0].Partners);
        Assert.Equal("Anaconda", shared[0].Income.Detail);
        Assert.Empty(shared[1].Partners);
        Assert.Equal(["Carol"], shared[3].Partners);

        // The full ledger still counts redemption, with dividends as trading income.
        Assert.Equal(51700, snapshot.Income.Single(entry => entry.Kind == IncomeKind.Bounties).Amount);
        Assert.Equal("trade dividends", snapshot.Income.Single(entry => entry.Kind == IncomeKind.Trading).Detail);
        Assert.Equal(2, snapshot.Income.Count(entry => entry.Kind == IncomeKind.Missions));

        var sessions = snapshot.Sessions;
        Assert.Equal(3, sessions.Count);
        Assert.Equal(Company.Wing, sessions[0].Company);
        Assert.Equal(["Alice", "Bob"], sessions[0].Partners);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 10, 1, 0, TimeSpan.Zero), sessions[0].Start);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 10, 30, 0, TimeSpan.Zero), sessions[0].End);
        Assert.Equal(Company.Crew, sessions[1].Company);
        Assert.Equal(TimeSpan.FromMinutes(12), sessions[1].Length(DateTimeOffset.MaxValue));
        Assert.Equal(["Dave"], sessions[2].Partners);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 12, 45, 0, TimeSpan.Zero), sessions[2].End);
        Assert.Equal(snapshot.Latest, sessions[2].End);

        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-06T09:00:00Z","event":"JoinACrew","Captain":"Erin"}""");
        var open = tracker.Snapshot().Sessions[^1];
        Assert.Null(open.End);
        Assert.Equal(TimeSpan.FromHours(1), open.Length(new DateTimeOffset(2026, 11, 6, 10, 0, 0, TimeSpan.Zero)));

        tracker.Reset();
        Assert.Empty(tracker.Snapshot().Sessions);
        Assert.Empty(tracker.Snapshot().Shared);
    }

    [Fact]
    public void Apply_FollowsThePowerplayStanding_AndRecordsMeritsAndDeliveries()
    {
        var tracker = new StatisticsTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Powerplay","Power":"Zemina Torval","Rank":3,"Merits":1200,"Votes":0,"TimePledged":864000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"PowerplayMerits","Power":"Zemina Torval","MeritsGained":150,"TotalMerits":1350}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"PowerplayDeliver","Power":"Zemina Torval","Type":"torvalcommoditybundle","Type_Localised":"Torval commodity bundle","Count":20}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:03:00Z","event":"PowerplayCollect","Power":"Zemina Torval","Type":"torvalcommoditybundle","Count":20}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:04:00Z","event":"PowerplayRank","Power":"Zemina Torval","Rank":4}""");

        var snapshot = tracker.Snapshot();
        var standing = snapshot.Powerplay!;
        Assert.Equal("Zemina Torval", standing.Power);
        Assert.Equal(4, standing.Rank);
        Assert.Equal(1350, standing.Merits);
        Assert.Equal(TimeSpan.FromDays(10), standing.TimePledged);
        Assert.Equal(3, snapshot.PowerplayActivity.Count);
        Assert.Equal(150, snapshot.PowerplayActivity[0].Value);
        Assert.Equal(PowerplayAction.Delivered, snapshot.PowerplayActivity[1].Action);
        Assert.Equal("Torval commodity bundle", snapshot.PowerplayActivity[1].Detail);
        Assert.Equal(PowerplayAction.Collected, snapshot.PowerplayActivity[2].Action);

        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:05:00Z","event":"PowerplayLeave","Power":"Zemina Torval"}""");
        Assert.Null(tracker.Snapshot().Powerplay);
        Assert.Equal(3, tracker.Snapshot().PowerplayActivity.Count);
    }
}
