using Katey.Core.Trading;

namespace Katey.Core.Tests;

/// <summary>Checks the trader's exchange rates and the trade planner's choices.</summary>
public sealed class TradingTests
{
    private static readonly Catalog Catalog = Catalog.LoadEmbedded();

    private static Material M(string name) => Catalog.Materials.Values.Single(material => material.Name == name);

    private static Dictionary<string, int> Inventory(params (string Name, int Count)[] held)
        => held.ToDictionary(pair => M(pair.Name).Symbol, pair => pair.Count, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, int> Needs(params (string Name, int Count)[] needs)
        => needs.ToDictionary(pair => M(pair.Name).Symbol, pair => pair.Count, StringComparer.OrdinalIgnoreCase);

    [Theory]
    [InlineData("Carbon", "Vanadium", 6, 1)]
    [InlineData("Carbon", "Niobium", 36, 1)]
    [InlineData("Carbon", "Yttrium", 216, 1)]
    [InlineData("Yttrium", "Niobium", 1, 3)]
    [InlineData("Yttrium", "Vanadium", 1, 9)]
    [InlineData("Yttrium", "Carbon", 1, 27)]
    [InlineData("Iron", "Carbon", 6, 1)]
    [InlineData("Iron", "Vanadium", 36, 1)]
    [InlineData("Selenium", "Niobium", 2, 1)]
    [InlineData("Selenium", "Vanadium", 2, 3)]
    [InlineData("Selenium", "Carbon", 2, 9)]
    public void Between_FollowsTheGamesRule(string from, string to, int give, int get)
    {
        var rate = TradeRate.Between(M(from), M(to));
        Assert.NotNull(rate);
        Assert.Equal(new TradeRate(give, get), rate);
    }

    [Fact]
    public void Between_IsNullAcrossInventories_AndForTheSameMaterial()
    {
        Assert.Null(TradeRate.Between(M("Carbon"), M("Carbon")));
        var manufactured = Catalog.Materials.Values.First(material => material.Category == MaterialCategory.Manufactured);
        Assert.Null(TradeRate.Between(M("Carbon"), manufactured));
        Assert.Equal([300, 250, 200, 150, 100], Enumerable.Range(1, 5).Select(MaterialCaps.For));
    }

    [Fact]
    public void Plan_TradesDownFromAFullerStack_AndReportsTheSurplus()
    {
        // Need 10 Vanadium, hold 4. Yttrium is full and trades down at 1:9; Niobium trades down at 1:3 but is barely above its floor.
        var plan = MaterialTrader.Plan(
            Catalog,
            Needs(("Vanadium", 10)),
            Inventory(("Vanadium", 4), ("Yttrium", 150), ("Niobium", 30)),
            []);

        var trade = Assert.Single(plan.Trades);
        Assert.Equal("Yttrium", trade.From.Name);
        Assert.Equal(1, trade.Give);
        Assert.Equal(9, trade.Get);
        Assert.Equal(3, trade.Surplus);
        Assert.True(plan.CoversEverything);
        var target = Assert.Single(plan.Targets);
        Assert.Equal(6, target.Missing);
        Assert.True(target.ForPlan);
        Assert.False(target.ForReserve);
    }

    [Fact]
    public void Plan_NeverSpendsBelowAFloor_NorAMaterialMarkedNeverTrade()
    {
        var needs = Needs(("Vanadium", 10), ("Yttrium", 2));
        var inventory = Inventory(("Yttrium", 3), ("Niobium", 50));

        // Yttrium has one spare above the plan's own need of 2; Niobium covers the rest.
        var plan = MaterialTrader.Plan(Catalog, needs, inventory, []);
        Assert.Equal(["Niobium"], plan.Trades.Select(trade => trade.From.Name).Distinct());
        Assert.True(plan.CoversEverything);

        // With Niobium kept at 50 nothing is spare but the one Yttrium, which gives 9 and leaves 1 unmet.
        var kept = MaterialTrader.Plan(Catalog, needs, inventory, [new MaterialReserve(M("Niobium").Symbol, 50, false)]);
        var trade = Assert.Single(kept.Trades);
        Assert.Equal("Yttrium", trade.From.Name);
        var unmet = Assert.Single(kept.Unmet);
        Assert.Equal(1, unmet.Remaining);
        Assert.Contains("above what the plan needs and you keep", unmet.Why, StringComparison.Ordinal);

        // Marked never-trade, Niobium is not touched even though it is spare, and the reason says so.
        var never = MaterialTrader.Plan(Catalog, needs, inventory, [new MaterialReserve(M("Niobium").Symbol, 0, true)]);
        Assert.DoesNotContain(never.Trades, trade => trade.From.Name == "Niobium");
        Assert.Contains("never trade", Assert.Single(never.Unmet).Why, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_RestoresAReserve_AndNeverCrossesInventories()
    {
        var plan = MaterialTrader.Plan(
            Catalog,
            Needs(),
            Inventory(("Carbon", 5), ("Iron", 300), ("Zinc", 80)),
            [new MaterialReserve(M("Carbon").Symbol, 20, false)]);

        var target = Assert.Single(plan.Targets);
        Assert.True(target.ForReserve);
        Assert.False(target.ForPlan);
        Assert.Equal(15, target.Missing);
        // Iron and Zinc lose the same worth across families; the full Iron stack goes first.
        var trade = Assert.Single(plan.Trades);
        Assert.Equal("Iron", trade.From.Name);
        Assert.Equal(new TradeRate(6, 1), trade.Rate);
        Assert.Equal(90, trade.Give);
        Assert.Equal(15, trade.Get);

        var zincFull = MaterialTrader.Plan(Catalog, Needs(), Inventory(("Carbon", 5), ("Iron", 100), ("Zinc", 250)), [new MaterialReserve(M("Carbon").Symbol, 20, false)]);
        var fromZinc = Assert.Single(zincFull.Trades);
        Assert.Equal("Zinc", fromZinc.From.Name);
        Assert.Equal(new TradeRate(2, 1), fromZinc.Rate);
        Assert.Equal(30, fromZinc.Give);

        var encodedNeed = Catalog.Materials.Values.First(material => material.Category == MaterialCategory.Encoded && material.Grade == 1);
        var noCross = MaterialTrader.Plan(Catalog, Needs((encodedNeed.Name, 3)), Inventory(("Iron", 300)), []);
        Assert.Empty(noCross.Trades);
        Assert.Contains("Nothing else in this inventory", Assert.Single(noCross.Unmet).Why, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_SplitsAcrossSources_WhenNoneCoversAlone_AndIsEmptyWhenNothingIsShort()
    {
        var plan = MaterialTrader.Plan(
            Catalog,
            Needs(("Yttrium", 4)),
            Inventory(("Niobium", 12), ("Tin", 72)),
            []);

        Assert.Equal(2, plan.Trades.Count);
        Assert.Equal(4, plan.Trades.Sum(trade => trade.Get));
        Assert.True(plan.CoversEverything);
        Assert.Equal(["Niobium", "Tin"], plan.Trades.Select(trade => trade.From.Name));

        // Batches never overfill the stack: 5 Carbon short of the cap can take one batch of 3, not a batch of 27.
        var nearCap = MaterialTrader.Plan(Catalog, Needs(("Carbon", 300)), Inventory(("Carbon", 295), ("Yttrium", 10), ("Vanadium", 10)), []);
        var fits = Assert.Single(nearCap.Trades);
        Assert.Equal("Vanadium", fits.From.Name);
        Assert.Equal(3, fits.Get);
        Assert.Contains("more than the stack can hold", Assert.Single(nearCap.Unmet).Why, StringComparison.Ordinal);

        Assert.Same(TradePlan.Empty, MaterialTrader.Plan(Catalog, Needs(("Carbon", 3)), Inventory(("Carbon", 3)), []));
    }
}
