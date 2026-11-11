namespace Katey.Core.Planning;

/// <summary>
/// What a technology broker unlock costs against what the plan leaves in the inventory. The plan comes
/// first: an unlock is affordable only out of the materials no enabled entry has already claimed, so
/// selecting one unlock can put another out of reach, and dropping it brings the other back.
/// </summary>
public static class UnlockBudget
{
    /// <summary>The inventory left after every enabled plan entry has taken its materials.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="entries">The plan. Disabled entries claim nothing.</param>
    /// <param name="inventory">Journal symbol → count, compared case-insensitively, or <see langword="null"/> when no inventory is known.</param>
    /// <returns>Material symbol → count left, never negative, for every material in the catalog; or <see langword="null"/> when no inventory is known.</returns>
    public static IReadOnlyDictionary<string, int>? Remaining(Catalog catalog, IEnumerable<PlanEntry> entries, IReadOnlyDictionary<string, int>? inventory)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(entries);
        if (inventory is null)
        {
            return null;
        }

        var needed = MaterialCalculator.Totals(catalog, entries).ToDictionary(total => total.Material.Symbol, total => total.Needed, StringComparer.OrdinalIgnoreCase);
        var remaining = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var material in catalog.Materials.Values)
        {
            var have = inventory.GetValueOrDefault(material.Symbol, 0);
            remaining[material.Symbol] = Math.Max(0, have - needed.GetValueOrDefault(material.Symbol, 0));
        }

        return remaining;
    }

    /// <summary>An unlock's materials laid against what is left.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="unlock">The unlock. Its materials must exist in <paramref name="catalog"/>.</param>
    /// <param name="remaining">Material symbol → count left, from <see cref="Remaining"/>, or <see langword="null"/> when no inventory is known.</param>
    /// <returns>One line per material the unlock costs, in the unlock's order. Commodities are not materials and are not here.</returns>
    public static IReadOnlyList<MaterialLine> Lines(Catalog catalog, Unlock unlock, IReadOnlyDictionary<string, int>? remaining)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(unlock);
        return
        [
            .. unlock.Materials.Select(pair => new MaterialLine(
                catalog.Materials[pair.Key],
                pair.Value,
                remaining?.GetValueOrDefault(pair.Key, 0))),
        ];
    }

    /// <summary>Whether what is left covers every material an unlock costs. False when no inventory is known.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="unlock">The unlock.</param>
    /// <param name="remaining">Material symbol → count left, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when every line is covered.</returns>
    public static bool CanAfford(Catalog catalog, Unlock unlock, IReadOnlyDictionary<string, int>? remaining)
        => remaining is not null && Lines(catalog, unlock, remaining).All(line => line.IsCovered);
}
