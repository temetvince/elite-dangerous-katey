using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Planning;

namespace Katey.App.Tests;

/// <summary>The clipboard text of the plan's totals.</summary>
public sealed class ShoppingListTextTests
{
    private static readonly Catalog Catalog = Catalog.LoadEmbedded();

    [Fact]
    public void Format_PrintsSectionsAndHaveShort()
    {
        var fsd = PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, null, PlanSource.Manual);
        var totals = MaterialCalculator.Totals(Catalog, [fsd]);

        var text = ShoppingListText.Format(MaterialCalculator.WithInventory(totals, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Arsenic"] = 4 }));

        Assert.StartsWith("Raw", text, StringComparison.Ordinal);
        Assert.Contains("5 × Arsenic (G2, Rhenium) — have 4, short 1", text, StringComparison.Ordinal);
        Assert.Contains("\n\nEncoded\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_EndsWithTheCommoditiesToBuy()
    {
        var booster = PlanEntry.ForUnlock(Catalog, "guardian_fsd_booster");

        var text = ShoppingListText.Format(MaterialCalculator.WithInventory(MaterialCalculator.Totals(Catalog, [booster]), null), MaterialCalculator.Commodities(Catalog, [booster]));

        Assert.EndsWith("Buy at a market\n8 × HN Shock Mount", text, StringComparison.Ordinal);
        Assert.Contains("21 × Guardian Power Cell (G", text, StringComparison.Ordinal);
    }
}
