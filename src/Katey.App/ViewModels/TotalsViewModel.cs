using Katey.App.Mvvm;
using Katey.App.Text;
using Katey.Core;
using Katey.Core.Planning;

namespace Katey.App.ViewModels;

/// <summary>One inventory's rows of the materials table.</summary>
/// <param name="Label">"Raw", "Manufactured" or "Encoded".</param>
/// <param name="Rows">Rows in display order.</param>
public sealed record TotalsSection(string Label, IReadOnlyList<MaterialRow> Rows)
{
    /// <summary>Gets whether there is nothing to collect in this inventory.</summary>
    public bool IsEmpty => Rows.Count == 0;

    /// <summary>Gets the sum of every row's need.</summary>
    public int Needed => Rows.Sum(row => row.Needed);

    /// <summary>Gets <c>Label (needed)</c>.</summary>
    public string Heading => Strings.Format(Keys.Totals_SectionHeading, ("label", Label), ("count", Needed));
}

/// <summary>The shopping list: every material the plan needs, with the live inventory laid against it.</summary>
public sealed class TotalsViewModel : ObservableObject
{
    private readonly Catalog _catalog;

    /// <summary>Creates empty totals.</summary>
    /// <param name="catalog">The catalog.</param>
    public TotalsViewModel(Catalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
        Update([], null);
    }

    /// <summary>Gets the three inventories, in display order, including empty ones.</summary>
    public IReadOnlyList<TotalsSection> Sections { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the commodities the plan's unlocks need, bought at a market rather than collected.</summary>
    public IReadOnlyList<CommodityTotal> Commodities { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether any commodity is needed.</summary>
    public bool HasCommodities => Commodities.Count > 0;

    /// <summary>Gets the plain-text list for the clipboard.</summary>
    public string Text { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether the plan needs anything.</summary>
    public bool IsEmpty { get; private set => SetProperty(ref field, value); } = true;

    /// <summary>Gets whether an inventory was laid against the totals.</summary>
    public bool HasInventory { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets how many materials are still short of what the plan needs. Zero when no inventory is known.</summary>
    public int ShortCount { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets one line summarising the state of the list.</summary>
    public string Summary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Recomputes from a plan and an inventory.</summary>
    /// <param name="entries">The plan.</param>
    /// <param name="inventory">Journal symbol → count, compared case-insensitively, or <see langword="null"/> when none is known.</param>
    public void Update(IReadOnlyList<PlanEntry> entries, IReadOnlyDictionary<string, int>? inventory)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var lines = MaterialCalculator.WithInventory(MaterialCalculator.Totals(_catalog, entries), inventory);
        Sections =
        [
            .. MaterialCalculator.CategoryOrder.Select(category => new TotalsSection(
                Display.CategoryLabel(category),
                [.. lines.Where(line => line.Material.Category == category).Select(line => new MaterialRow(line))])),
        ];
        Commodities = MaterialCalculator.Commodities(_catalog, entries);
        OnPropertyChanged(nameof(HasCommodities));
        Text = ShoppingListText.Format(lines, Commodities);
        IsEmpty = lines.Count == 0;
        HasInventory = inventory is not null;
        ShortCount = inventory is null ? 0 : lines.Count(line => !line.IsCovered);
        var enabled = entries.Count(entry => entry.Enabled);
        Summary = enabled switch
        {
            0 => Strings.Get(Keys.Totals_NoneCounted),
            _ when inventory is null => Strings.Plural(Keys.Totals_NoInventory, enabled),
            _ when ShortCount == 0 => Strings.Plural(Keys.Totals_AllHeld, enabled),
            _ => Strings.Plural(Keys.Totals_ForEntries, enabled) + " " + Strings.Plural(Keys.Totals_ShortMaterials, ShortCount),
        };
    }
}
