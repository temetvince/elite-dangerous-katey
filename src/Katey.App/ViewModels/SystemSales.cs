using Katey.Journal;

namespace Katey.App.ViewModels;

/// <summary>
/// What the player has been paid for one system's data. A cartographics sale reports one total for every
/// system in it, so a sale of several systems is shared among them by Katey's estimate of each system's
/// bodies; a sale of one system is taken whole. A species sale is attributed to the system its sample came
/// from, exactly as the game paid it.
/// </summary>
public static class SystemSales
{
    /// <summary>Sums what the system's data has sold for across the history.</summary>
    /// <param name="sold">The sales ledger.</param>
    /// <param name="systemName">The system, matched to the journal's spelling without regard to case.</param>
    /// <returns>Credits; 0 when nothing from the system has been sold.</returns>
    public static long Credits(SoldData sold, string systemName)
    {
        ArgumentNullException.ThrowIfNull(sold);
        ArgumentNullException.ThrowIfNull(systemName);
        return sold.Exploration.Sum(sale => Share(sale, systemName))
            + sold.Organic.Where(sale => string.Equals(sale.Sample.SystemName, systemName, StringComparison.OrdinalIgnoreCase)).Sum(sale => sale.Credits);
    }

    /// <summary>The system's share of one cartographics sale.</summary>
    /// <param name="sale">The sale.</param>
    /// <param name="systemName">The system.</param>
    /// <returns>
    /// The whole sale when the system is the only one in it; otherwise the sale's earnings in proportion to the
    /// estimated value of the system's bodies against every system's, or by body count when nothing can be
    /// valued; 0 when the system is not in the sale.
    /// </returns>
    public static long Share(ExplorationSale sale, string systemName)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(systemName);
        var mine = sale.Systems.FirstOrDefault(system => string.Equals(system.Name, systemName, StringComparison.OrdinalIgnoreCase));
        if (mine is null)
        {
            return 0;
        }

        if (sale.Systems.Count == 1)
        {
            return sale.TotalEarnings;
        }

        var all = sale.Systems.Sum(system => system.Bodies.Sum(CarriedDataSummary.Cartographics));
        if (all > 0)
        {
            return (long)Math.Round(sale.TotalEarnings * (double)mine.Bodies.Sum(CarriedDataSummary.Cartographics) / all);
        }

        var bodies = sale.Systems.Sum(system => system.Bodies.Count);
        return bodies > 0 ? (long)Math.Round(sale.TotalEarnings * (double)mine.Bodies.Count / bodies) : 0;
    }
}
