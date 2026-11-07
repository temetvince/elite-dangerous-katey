namespace Katey.Core.Planning;

/// <summary>A material and how many of it a plan needs.</summary>
/// <param name="Material">The material.</param>
/// <param name="Needed">Total needed across every enabled entry. Positive.</param>
public sealed record MaterialTotal(Material Material, int Needed);

/// <summary>
/// A material line with the player's inventory laid against it.
/// </summary>
/// <param name="Material">The material.</param>
/// <param name="Needed">Total needed.</param>
/// <param name="Have">How many the player holds, or <see langword="null"/> when no inventory is known.</param>
public sealed record MaterialLine(Material Material, int Needed, int? Have)
{
    /// <summary>Gets how many are still missing: needed minus have, never negative; equals <see cref="Needed"/> when no inventory is known.</summary>
    public int Missing => Have is { } have ? Math.Max(0, Needed - have) : Needed;

    /// <summary>Gets whether the player already holds enough. False when no inventory is known.</summary>
    public bool IsCovered => Have is { } have && have >= Needed;
}

/// <summary>A commodity and how many of it the plan's unlocks need. Commodities are bought at a market, not collected, so they stand apart from the material totals.</summary>
/// <param name="Name">The commodity's name.</param>
/// <param name="Needed">Total needed across every enabled unlock. Positive.</param>
public sealed record CommodityTotal(string Name, int Needed);

/// <summary>Sums plan entries into material totals.</summary>
public static class MaterialCalculator
{
    /// <summary>Display order of the three inventories.</summary>
    public static IReadOnlyList<MaterialCategory> CategoryOrder { get; } =
        [MaterialCategory.Raw, MaterialCategory.Manufactured, MaterialCategory.Encoded];

    /// <summary>
    /// Materials one entry costs: for a module, each rolled grade's materials times its roll count, plus
    /// the experimental's materials once; for an unlock, its fixed cost. Ignores <see cref="PlanEntry.Enabled"/>.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="entry">The entry. Its blueprint and experimental, or its unlock, must exist in <paramref name="catalog"/>.</param>
    /// <returns>Material display name → count, in first-seen order. Only positive counts.</returns>
    public static IReadOnlyDictionary<string, int> ForEntry(Catalog catalog, PlanEntry entry)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(entry);

        var sums = new Dictionary<string, int>(StringComparer.Ordinal);
        if (entry.Unlock is { } unlock)
        {
            Add(sums, catalog.Unlocks[unlock].Materials, 1);
            return sums;
        }

        var blueprint = catalog.Blueprints[entry.Blueprint];
        foreach (var grade in entry.RolledGrades(catalog))
        {
            Add(sums, blueprint.Grades[grade], entry.RollsAt(grade));
        }

        if (entry.Experimental is { } experimental)
        {
            Add(sums, catalog.Experimentals[experimental].Components, 1);
        }

        return sums;
    }

    /// <summary>Sums the materials of every enabled entry.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="entries">The plan.</param>
    /// <returns>One total per material, sorted by inventory, then family, then grade, then name.</returns>
    public static IReadOnlyList<MaterialTotal> Totals(Catalog catalog, IEnumerable<PlanEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(entries);

        var sums = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries.Where(entry => entry.Enabled))
        {
            Add(sums, ForEntry(catalog, entry), 1);
        }

        return
        [
            .. sums
                .Select(pair => new MaterialTotal(catalog.Materials[pair.Key], pair.Value))
                .OrderBy(total => total.Material.Category)
                .ThenBy(total => total.Material.Line, StringComparer.Ordinal)
                .ThenBy(total => total.Material.Grade)
                .ThenBy(total => total.Material.Name, StringComparer.Ordinal),
        ];
    }

    /// <summary>Sums the commodities of every enabled unlock entry. Module entries need none.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="entries">The plan.</param>
    /// <returns>One total per commodity, by name.</returns>
    public static IReadOnlyList<CommodityTotal> Commodities(Catalog catalog, IEnumerable<PlanEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(entries);

        var sums = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries.Where(entry => entry.Enabled && entry.Unlock is not null))
        {
            Add(sums, catalog.Unlocks[entry.Unlock!].Commodities, 1);
        }

        return [.. sums.Select(pair => new CommodityTotal(pair.Key, pair.Value)).OrderBy(total => total.Name, StringComparer.Ordinal)];
    }

    /// <summary>Lays an inventory against totals.</summary>
    /// <param name="totals">The totals.</param>
    /// <param name="inventory">Journal symbol → count, compared case-insensitively, or <see langword="null"/> when no inventory is known.</param>
    /// <returns>One line per total, in the same order.</returns>
    public static IReadOnlyList<MaterialLine> WithInventory(
        IEnumerable<MaterialTotal> totals,
        IReadOnlyDictionary<string, int>? inventory)
    {
        ArgumentNullException.ThrowIfNull(totals);

        return
        [
            .. totals.Select(total => new MaterialLine(
                total.Material,
                total.Needed,
                inventory?.GetValueOrDefault(total.Material.Symbol, 0))),
        ];
    }

    private static void Add(Dictionary<string, int> sums, IReadOnlyDictionary<string, int> counts, int times)
    {
        if (times <= 0)
        {
            return;
        }

        foreach (var (name, count) in counts)
        {
            sums[name] = sums.GetValueOrDefault(name, 0) + (count * times);
        }
    }
}
