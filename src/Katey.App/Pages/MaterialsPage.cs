using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Planning;
using Katey.Core.Trading;

namespace Katey.App.Pages;

/// <summary>One material in the index: what you hold against what the plan needs, and the reserve you keep on it.</summary>
public sealed class InventoryMaterialRow : ObservableObject
{
    private readonly Action<string, int, bool> _setReserve;
    private int _keep;
    private bool _neverTrade;

    /// <summary>Creates the row.</summary>
    /// <param name="link">Its page.</param>
    /// <param name="symbol">The journal symbol the reserve is keyed by.</param>
    /// <param name="gradeNumber">1–5.</param>
    /// <param name="family">In-game family.</param>
    /// <param name="have">How many you hold, or <see langword="null"/> before the inventory is read.</param>
    /// <param name="needed">How many the plan needs.</param>
    /// <param name="reserve">The reserve kept on it.</param>
    /// <param name="setReserve">Receives the symbol, the count to keep and the never-trade flag when either is edited.</param>
    /// <param name="isHighlighted">Whether the page landed on this material, so the row stands out and is scrolled to.</param>
    public InventoryMaterialRow(Link link, string symbol, int gradeNumber, string family, int? have, int needed, MaterialReserve reserve, Action<string, int, bool> setReserve, bool isHighlighted = false)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(reserve);
        ArgumentNullException.ThrowIfNull(setReserve);
        Link = link;
        Symbol = symbol;
        GradeNumber = gradeNumber;
        Family = family;
        Have = have;
        Needed = needed;
        _keep = reserve.Keep;
        _neverTrade = reserve.NeverTrade;
        _setReserve = setReserve;
        IsHighlighted = isHighlighted;
    }

    /// <summary>Gets whether the page landed on this material: the row stands out and the window scrolls to it.</summary>
    public bool IsHighlighted { get; }

    /// <summary>Gets its page.</summary>
    public Link Link { get; }

    /// <summary>Gets the journal symbol.</summary>
    public string Symbol { get; }

    /// <summary>Gets the grade, 1–5.</summary>
    public int GradeNumber { get; }

    /// <summary>Gets the in-game family.</summary>
    public string Family { get; }

    /// <summary>Gets how many you hold, or <see langword="null"/> before the inventory is read.</summary>
    public int? Have { get; }

    /// <summary>Gets how many the plan needs.</summary>
    public int Needed { get; }

    /// <summary>Gets or sets how many to keep at least, as typed. Empty or unreadable text reads as none.</summary>
    public string KeepText
    {
        get => _keep == 0 ? "" : _keep.ToString(CultureInfo.InvariantCulture);
        set
        {
            var keep = int.TryParse((value ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 ? parsed : 0;
            if (keep != _keep)
            {
                _keep = keep;
                _setReserve(Symbol, _keep, _neverTrade);
            }

            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets whether the trades may spend this material. Off means it is never handed over at a trader, however many are held.</summary>
    public bool MayTrade
    {
        get => !_neverTrade;
        set
        {
            if (_neverTrade == value)
            {
                _neverTrade = !value;
                OnPropertyChanged();
                _setReserve(Symbol, _keep, _neverTrade);
            }
        }
    }

    /// <summary>Gets the material name.</summary>
    public string Name => Link.Label;

    /// <summary>Gets "Gn".</summary>
    public string Grade => Display.Grade(GradeNumber);

    /// <summary>Gets the count held as text, or a dash.</summary>
    public string HaveText => Have is { } have ? have.ToString(CultureInfo.InvariantCulture) : "—";

    /// <summary>Gets the need as text, or empty when the plan does not use it.</summary>
    public string NeededText => Needed == 0 ? "" : Needed.ToString(CultureInfo.InvariantCulture);

    /// <summary>Gets how many are still to collect, or 0.</summary>
    public int ShortCount => Have is { } have && Needed > have ? Needed - have : 0;

    /// <summary>Gets how many are still to collect as text, or empty.</summary>
    public string ShortText => ShortCount == 0 ? "" : ShortCount.ToString(CultureInfo.InvariantCulture);

    /// <summary>Gets whether the plan needs more than you hold.</summary>
    public bool IsShort => ShortCount > 0;

    /// <summary>Gets whether the plan needs it at all.</summary>
    public bool IsNeeded => Needed > 0;

    /// <summary>Gets how much of the need is held, 0 to 1, for a meter. 1 when nothing is needed.</summary>
    public double Fill => Needed <= 0 ? 1 : Math.Clamp((Have ?? 0) / (double)Needed, 0, 1);

    /// <summary>Gets whether a meter means anything: an inventory is known and the plan needs some.</summary>
    public bool HasMeter => Have is not null && Needed > 0;
}

/// <summary>One inventory's materials.</summary>
/// <param name="Label">"Raw", "Manufactured" or "Encoded".</param>
/// <param name="Rows">In the table's sort order.</param>
/// <param name="Held">Total count held, or 0 before the inventory is read.</param>
/// <param name="Category">Which inventory, for colouring.</param>
public sealed record InventorySectionRow(string Label, IReadOnlyList<InventoryMaterialRow> Rows, int Held, MaterialCategory Category)
{
    /// <summary>Gets "Label · n held".</summary>
    public string Heading => $"{Label} · {Held} held";
}

/// <summary>One of the three inventories as a toggle. Whether it is shown is remembered.</summary>
public sealed class CategoryToggle : ObservableObject
{
    private readonly Action<MaterialCategory, bool> _toggle;
    private bool _shown;

    /// <summary>Creates the toggle.</summary>
    /// <param name="category">The inventory.</param>
    /// <param name="shown">Whether it starts shown.</param>
    /// <param name="toggle">Receives the inventory and the new state when the user flips it.</param>
    public CategoryToggle(MaterialCategory category, bool shown, Action<MaterialCategory, bool> toggle)
    {
        ArgumentNullException.ThrowIfNull(toggle);
        Category = category;
        Label = ShoppingList.CategoryLabels[category];
        _shown = shown;
        _toggle = toggle;
    }

    /// <summary>Gets the inventory.</summary>
    public MaterialCategory Category { get; }

    /// <summary>Gets the label.</summary>
    public string Label { get; }

    /// <summary>Gets or sets whether the inventory is shown.</summary>
    public bool IsShown
    {
        get => _shown;
        set
        {
            if (SetProperty(ref _shown, value))
            {
                _toggle(Category, value);
            }
        }
    }
}

/// <summary>Every material, with your live inventory and the plan's needs laid against it, by inventory with each one switchable.</summary>
public sealed class MaterialsPage : Page
{
    private const string HiddenKey = "materials.hidden";
    private const string ShortOnlyKey = "materials.shortOnly";

    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <param name="show">
    /// A <see cref="MaterialCategory"/> name to make sure is shown on arrival, or a material symbol to land on,
    /// whose category is shown and whose row is highlighted and kept whatever the filters say, or empty. A
    /// category shown this way is remembered like a click.
    /// </param>
    public MaterialsPage(Workspace workspace, string show = "")
        : base(new PageKey(PageKind.Materials, show), workspace)
    {
        Title = "Materials";
        Kicker = "Browse";
        var landing = workspace.Catalog.MaterialsBySymbol.GetValueOrDefault(show);
        HighlightedSymbol = landing?.Symbol ?? "";
        var reveal = landing?.Category ?? (Enum.TryParse<MaterialCategory>(show, ignoreCase: true, out var wanted) ? wanted : null);
        if (reveal is { } category)
        {
            var set = workspace.Preferences.GetSet(HiddenKey).ToHashSet(StringComparer.Ordinal);
            set.Remove(category.ToString());
            workspace.Preferences.SetSet(HiddenKey, set);
        }

        var hidden = workspace.Preferences.GetSet(HiddenKey);
        Categories = [.. MaterialCalculator.CategoryOrder.Select(category => new CategoryToggle(category, !hidden.Contains(category.ToString()), ToggleCategory))];
        Sort = new TableSort<InventoryMaterialRow>(
            [
                new("name", "Material", row => row.Name),
                new("grade", "Grade", row => row.GradeNumber),
                new("family", "Family", row => row.Family),
                new("have", "Have", row => row.Have, DescendingFirst: true),
                new("need", "Need", row => row.Needed, DescendingFirst: true),
                new("short", "Short", row => row.ShortCount, DescendingFirst: true),
            ],
            "family",
            workspace.Preferences,
            "sort.materials");
        Sort.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Gets the symbol of the material the page landed on and highlights, or empty.</summary>
    public string HighlightedSymbol { get; }

    /// <summary>Gets or sets a filter on name and family. Empty shows everything.</summary>
    public string Filter
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? ""))
            {
                Refresh();
            }
        }
    } = "";

    /// <summary>Gets or sets whether only materials the plan is short of are shown. Remembered.</summary>
    public bool ShortOnly
    {
        get => Workspace.Preferences.GetBool(ShortOnlyKey, false);
        set
        {
            if (value != ShortOnly)
            {
                Workspace.Preferences.SetBool(ShortOnlyKey, value);
                OnPropertyChanged();
                Refresh();
            }
        }
    }

    /// <summary>Gets the three inventories as toggles, all shown until the user says otherwise.</summary>
    public IReadOnlyList<CategoryToggle> Categories { get; }

    /// <summary>Gets the table's sort, shared by every inventory.</summary>
    public TableSort<InventoryMaterialRow> Sort { get; }

    /// <summary>Gets the inventories that are shown, in catalog order.</summary>
    public IReadOnlyList<InventorySectionRow> Sections { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a line on the inventory's freshness.</summary>
    public string StatusText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets materials the journal reports that the catalog does not know, as text, or empty.</summary>
    public string UnknownText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets what to trade at the material traders to cover the shortfalls and restore the reserves.</summary>
    public TradeView Trades { get; private set => SetProperty(ref field, value); } = TradeView.From(null);

    /// <inheritdoc/>
    public override void Refresh()
    {
        var catalog = Workspace.Catalog;
        var inventory = Workspace.Inventory;
        var counts = Workspace.InventoryCounts;
        var needs = MaterialCalculator.Totals(catalog, Workspace.Plan.Snapshot()).ToDictionary(total => total.Material.Name, total => total.Needed, StringComparer.Ordinal);
        var filter = Filter.Trim();
        var shortOnly = ShortOnly;

        Sections =
        [
            .. Categories.Where(toggle => toggle.IsShown).Select(toggle =>
            {
                var category = toggle.Category;
                var rows = catalog.Materials.Values
                    .Where(material => material.Category == category)
                    .Where(material => filter.Length == 0
                        || string.Equals(material.Symbol, HighlightedSymbol, StringComparison.OrdinalIgnoreCase)
                        || material.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        || material.Line.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(material => material.Line, StringComparer.Ordinal)
                    .ThenBy(material => material.Grade)
                    .ThenBy(material => material.Name, StringComparer.Ordinal)
                    .Select(material => new InventoryMaterialRow(
                        LinkTo(material.Name, PageKind.Material, material.Name),
                        material.Symbol,
                        material.Grade,
                        material.Line,
                        counts?.GetValueOrDefault(material.Symbol),
                        needs.GetValueOrDefault(material.Name),
                        Workspace.Reserves.For(material.Symbol),
                        SetReserve,
                        string.Equals(material.Symbol, HighlightedSymbol, StringComparison.OrdinalIgnoreCase)))
                    .Where(row => !shortOnly || row.IsShort || row.IsHighlighted);
                var held = counts is null ? 0 : catalog.Materials.Values.Where(material => material.Category == category).Sum(material => counts.GetValueOrDefault(material.Symbol));
                return new InventorySectionRow(toggle.Label, Sort.Apply(rows), held, category);
            }),
        ];

        StatusText = !inventory.HasSnapshot
            ? "No inventory yet. Counts appear once the journal watch has read a Materials snapshot, which the game writes on every load."
            : $"Inventory of {inventory.Commander ?? "the commander"} as of {inventory.AsOf?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)}.";
        var unknown = counts is null ? [] : counts.Keys.Where(symbol => !catalog.MaterialsBySymbol.ContainsKey(symbol)).Order(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var symbol in unknown)
        {
            Workspace.NoteUnknown("material", symbol);
        }

        UnknownText = unknown.Count == 0 ? "" : "Not in the tables: " + string.Join(", ", unknown) + ".";
        RefreshTrades();
    }

    /// <summary>Recomputes the trades alone, so an edited reserve does not rebuild the rows under the cursor.</summary>
    private void RefreshTrades() => Trades = TradeView.From(Workspace.Trades(), material => LinkTo(material.Name, PageKind.Materials, material.Symbol));

    private void SetReserve(string symbol, int keep, bool neverTrade)
    {
        Workspace.Reserves.Set(symbol, keep, neverTrade);
        RefreshTrades();
    }

    private void ToggleCategory(MaterialCategory category, bool shown)
    {
        var hidden = Workspace.Preferences.GetSet(HiddenKey).ToHashSet(StringComparer.Ordinal);
        if (shown)
        {
            hidden.Remove(category.ToString());
        }
        else
        {
            hidden.Add(category.ToString());
        }

        Workspace.Preferences.SetSet(HiddenKey, hidden);
        Refresh();
    }
}
