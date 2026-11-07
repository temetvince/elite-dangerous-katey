using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Planning;

namespace Katey.App.Pages;

/// <summary>How a technology broker unlock stands against the player.</summary>
public enum UnlockState
{
    /// <summary>The journal records the purchase.</summary>
    Unlocked,

    /// <summary>The unlock is in the plan.</summary>
    Planned,

    /// <summary>Not planned, and what the plan leaves in the inventory covers it.</summary>
    Affordable,

    /// <summary>Not planned, and the inventory, after the plan, falls short of it.</summary>
    OutOfReach,
}

/// <summary>One technology broker unlock as the page shows it: its cost, where it stands, and the switch that puts it in the plan.</summary>
public sealed class UnlockRow : ObservableObject
{
    private readonly Action<UnlockRow, bool> _select;

    /// <summary>Creates the row.</summary>
    /// <param name="unlock">The unlock.</param>
    /// <param name="typeLink">The page of the module type it unlocks, or <see langword="null"/> for a fighter.</param>
    /// <param name="state">Where it stands.</param>
    /// <param name="cost">Its materials as chips.</param>
    /// <param name="shortLines">Its materials laid against what the plan leaves, for the shortfall text.</param>
    /// <param name="select">Receives this row and whether it was ticked.</param>
    /// <param name="unlockedBecause">Why it counts as bought, or empty when it does not.</param>
    public UnlockRow(Unlock unlock, Link? typeLink, UnlockState state, IReadOnlyList<MaterialChip> cost, IReadOnlyList<MaterialLine> shortLines, Action<UnlockRow, bool> select, string unlockedBecause = "")
    {
        ArgumentNullException.ThrowIfNull(unlock);
        ArgumentNullException.ThrowIfNull(cost);
        ArgumentNullException.ThrowIfNull(shortLines);
        ArgumentNullException.ThrowIfNull(select);
        Unlock = unlock;
        TypeLink = typeLink;
        State = state;
        Cost = cost;
        _select = select;
        UnlockedBecause = unlockedBecause ?? "";
        CommoditiesText = unlock.Commodities.Count == 0
            ? ""
            : "Also " + string.Join(", ", unlock.Commodities.Select(pair => string.Create(CultureInfo.InvariantCulture, $"{pair.Value}× {pair.Key}"))) + " from a market.";
        var missing = shortLines.Where(line => !line.IsCovered).Select(line => string.Create(CultureInfo.InvariantCulture, $"{line.Missing} {line.Material.Name}")).ToList();
        ShortText = state == UnlockState.OutOfReach && missing.Count > 0 ? "Short " + string.Join(", ", missing) + "." : "";
    }

    /// <summary>Gets the unlock.</summary>
    public Unlock Unlock { get; }

    /// <summary>Gets the unlock's key.</summary>
    public string Key => Unlock.Key;

    /// <summary>Gets the unlock's name.</summary>
    public string Name => Unlock.Name;

    /// <summary>Gets which broker sells it, as a label.</summary>
    public string BrokerText => UnlocksPage.BrokerLabel(Unlock.Broker);

    /// <summary>Gets the page of the module type it unlocks, or <see langword="null"/> for a fighter.</summary>
    public Link? TypeLink { get; }

    /// <summary>Gets whether there is a module type page to open.</summary>
    public bool HasType => TypeLink is not null;

    /// <summary>Gets where the unlock stands.</summary>
    public UnlockState State { get; }

    /// <summary>Gets the state in a word, for the badge.</summary>
    public string StateText => State switch
    {
        UnlockState.Unlocked => "UNLOCKED",
        UnlockState.Planned => "PLANNED",
        UnlockState.Affordable => "AFFORDABLE",
        UnlockState.OutOfReach => "SHORT",
        _ => State.ToString().ToUpperInvariant(),
    };

    /// <summary>Gets whether the journal records the purchase.</summary>
    public bool IsUnlocked => State == UnlockState.Unlocked;

    /// <summary>Gets whether the unlock is in the plan.</summary>
    public bool IsPlanned => State == UnlockState.Planned;

    /// <summary>Gets whether what the plan leaves covers it.</summary>
    public bool IsAffordable => State == UnlockState.Affordable;

    /// <summary>Gets whether what the plan leaves falls short of it.</summary>
    public bool IsShort => State == UnlockState.OutOfReach;

    /// <summary>Gets whether the row reads dimmed: out of reach until the plan changes. A dimmed row can still be ticked.</summary>
    public bool IsDim => State == UnlockState.OutOfReach;

    /// <summary>Gets whether the row can be ticked: anything the journal has not recorded as bought.</summary>
    public bool CanSelect => State != UnlockState.Unlocked;

    /// <summary>Gets the materials it costs.</summary>
    public IReadOnlyList<MaterialChip> Cost { get; }

    /// <summary>Gets the commodities it also costs, as one sentence, or empty.</summary>
    public string CommoditiesText { get; }

    /// <summary>Gets whether there are commodities to mention.</summary>
    public bool HasCommodities => CommoditiesText.Length > 0;

    /// <summary>Gets what the inventory falls short by, as one sentence, or empty when nothing is short.</summary>
    public string ShortText { get; }

    /// <summary>Gets whether there is a shortfall to show.</summary>
    public bool HasShort => ShortText.Length > 0;

    /// <summary>Gets why the unlock counts as bought: the journal records it, or you own one. Empty when it does not.</summary>
    public string UnlockedBecause { get; }

    /// <summary>Gets a note when the journal cannot tell whether this unlock was bought.</summary>
    public string DetectNote => Unlock.IsDetectable ? "" : "The journal does not name this item, so Katey cannot tell whether you have bought it.";

    /// <summary>Gets whether <see cref="DetectNote"/> has anything to show.</summary>
    public bool HasDetectNote => DetectNote.Length > 0;

    /// <summary>Gets or sets whether the unlock is in the plan. Setting it adds or removes the plan entry; the page then rebuilds.</summary>
    public bool IsSelected
    {
        get => IsPlanned;
        set
        {
            if (value != IsPlanned && CanSelect)
            {
                _select(this, value);
            }
        }
    }
}

/// <summary>One category of a broker's unlocks, collapsible, with its open state remembered.</summary>
public sealed class UnlockGroupRow : ExpandableGroup
{
    /// <summary>Creates the group.</summary>
    /// <param name="category">The category.</param>
    /// <param name="rows">The unlocks in it, in sort order.</param>
    /// <param name="expansion">Where the open state is remembered.</param>
    public UnlockGroupRow(UnlockCategory category, IReadOnlyList<UnlockRow> rows, GroupExpansion expansion)
        : base(category.ToString(), UnlocksPage.CategoryLabel(category), expansion)
    {
        ArgumentNullException.ThrowIfNull(rows);
        Category = category;
        Rows = rows;
    }

    /// <summary>Gets the category.</summary>
    public UnlockCategory Category { get; }

    /// <summary>Gets the unlocks in it.</summary>
    public IReadOnlyList<UnlockRow> Rows { get; }

    /// <summary>Gets a count line, such as "6 unlocks, 2 bought, 1 planned".</summary>
    public string Summary
    {
        get
        {
            var parts = new List<string> { Display.Plural(Rows.Count, "unlock") };
            var bought = Rows.Count(row => row.IsUnlocked);
            var planned = Rows.Count(row => row.IsPlanned);
            if (bought > 0)
            {
                parts.Add($"{bought} bought");
            }

            if (planned > 0)
            {
                parts.Add($"{planned} planned");
            }

            return string.Join(", ", parts);
        }
    }
}

/// <summary>
/// The technology broker unlocks, Guardian and human, each with its cost and whether what the plan leaves
/// in the inventory covers it. Ticking an unlock adds it to the plan; the plan comes first, so ticking one
/// can put another out of reach, and unticking brings it back. A bought unlock reads as such and cannot be
/// ticked; bought means the journal records the purchase or a module it makes available is owned.
/// </summary>
public sealed class UnlocksPage : Page
{
    private readonly GroupExpansion _expansion;

    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <param name="section">The section to open on: <c>guardian</c>, <c>human</c>, or empty for the remembered one.</param>
    public UnlocksPage(Workspace workspace, string section = "")
        : base(new PageKey(PageKind.Unlocks, section ?? ""), workspace)
    {
        Title = "Technology broker unlocks";
        Kicker = "Browse";
        Sections = new SectionSwitch([("guardian", "Guardian"), ("human", "Human")], "guardian", workspace.Preferences, "section.unlocks");
        if (section is "guardian" or "human")
        {
            Sections.Show(section);
        }

        Sections.PropertyChanged += (_, _) => Refresh();
        Sort = new TableSort<UnlockRow>(
            [
                new("name", "Unlock", row => row.Name),
                new("state", "State", row => (int)row.State),
                new("type", "Module type", row => row.TypeLink?.Label ?? ""),
            ],
            "name",
            workspace.Preferences,
            "sort.unlocks");
        Sort.Changed += (_, _) => Refresh();
        _expansion = new GroupExpansion(workspace.Preferences, "unlocks.expanded");
        ExpandAllCommand = new RelayCommand(() => _expansion.SetAll(Groups, true));
        CollapseAllCommand = new RelayCommand(() => _expansion.SetAll(Groups, false));
        if (workspace.Preferences.Get("unlocks.expanded") is null)
        {
            foreach (var category in Enum.GetValues<UnlockCategory>())
            {
                _expansion.Set(category.ToString(), true);
            }
        }

        Refresh();
    }

    /// <summary>Gets the two brokers, one showing at a time.</summary>
    public SectionSwitch Sections { get; }

    /// <summary>Gets the current broker's unlocks by category, each group collapsible and its open state remembered. A group never seen before opens.</summary>
    public IReadOnlyList<UnlockGroupRow> Groups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the command that opens every group.</summary>
    public RelayCommand ExpandAllCommand { get; }

    /// <summary>Gets the command that closes every group.</summary>
    public RelayCommand CollapseAllCommand { get; }

    /// <summary>Gets the table's sort.</summary>
    public TableSort<UnlockRow> Sort { get; }

    /// <summary>Gets every unlock of the current broker, across the groups, in sort order.</summary>
    public IReadOnlyList<UnlockRow> Rows { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets one line on the current broker: how many unlocks are bought, planned and affordable.</summary>
    public string Summary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets every material the planned unlocks need, with the inventory laid against it.</summary>
    public IReadOnlyList<MaterialRow> Materials { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets every commodity the planned unlocks need.</summary>
    public IReadOnlyList<CommodityTotal> Commodities { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether any unlock is planned, so the materials section has something to say.</summary>
    public bool HasPlanned { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets whether the planned unlocks need commodities.</summary>
    public bool HasCommodities => Commodities.Count > 0;

    /// <summary>Gets one line on the planned unlocks' materials.</summary>
    public string MaterialsSummary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>A category as a label.</summary>
    /// <param name="category">The category.</param>
    /// <returns>Plural, title case, such as "Modified weapons".</returns>
    public static string CategoryLabel(UnlockCategory category) => category switch
    {
        UnlockCategory.Modules => "Modules",
        UnlockCategory.Weapons => "Weapons",
        UnlockCategory.ModifiedWeapons => "Modified weapons",
        UnlockCategory.Fighters => "Fighters",
        UnlockCategory.PreEngineered => "Pre-engineered modules",
        _ => category.ToString(),
    };

    /// <summary>A broker as a label.</summary>
    /// <param name="broker">The broker.</param>
    /// <returns>"Guardian technology broker" or "Human technology broker".</returns>
    public static string BrokerLabel(BrokerKind broker) => broker switch
    {
        BrokerKind.Guardian => "Guardian technology broker",
        BrokerKind.Human => "Human technology broker",
        _ => broker.ToString(),
    };

    /// <inheritdoc/>
    public override void Refresh()
    {
        var catalog = Workspace.Catalog;
        var entries = Workspace.Plan.Snapshot();
        var inventory = Workspace.InventoryCounts;
        var broker = Sections.Current == "human" ? BrokerKind.Human : BrokerKind.Guardian;
        var rows = new List<UnlockRow>();
        foreach (var unlock in catalog.Unlocks.Values.Where(unlock => unlock.Broker == broker))
        {
            var planned = entries.FirstOrDefault(entry => string.Equals(entry.Unlock, unlock.Key, StringComparison.Ordinal));
            var others = planned is null ? entries : entries.Where(entry => entry.Id != planned.Id);
            var remaining = UnlockBudget.Remaining(catalog, others, inventory);
            var lines = UnlockBudget.Lines(catalog, unlock, remaining);
            var because = Workspace.UnlockedBecause(unlock);
            var state = because is not null ? UnlockState.Unlocked
                : planned is not null ? UnlockState.Planned
                : UnlockBudget.CanAfford(catalog, unlock, remaining) ? UnlockState.Affordable
                : UnlockState.OutOfReach;
            var typeLink = unlock.ModuleType is { } code && catalog.ModuleTypes.TryGetValue(code, out var type) ? LinkTo(type.Name, PageKind.ModuleType, type.Code) : null;
            rows.Add(new UnlockRow(unlock, typeLink, state, MaterialChip.From(catalog, unlock.Materials), lines, Select, because ?? ""));
        }

        Rows = Sort.Apply(rows);
        Groups =
        [
            .. Rows.GroupBy(row => row.Unlock.Category)
                .OrderBy(group => group.Key)
                .Select(group => new UnlockGroupRow(group.Key, [.. group], _expansion)),
        ];

        var counts = rows.CountBy(row => row.State).ToDictionary();
        Summary = inventory is null
            ? $"{Display.Plural(rows.Count, "unlock")}. Start the journal watch to see which you can afford."
            : string.Join(" ", new[]
            {
                $"{Display.Plural(rows.Count, "unlock")}:",
                $"{counts.GetValueOrDefault(UnlockState.Unlocked)} bought,",
                $"{counts.GetValueOrDefault(UnlockState.Planned)} planned,",
                $"{counts.GetValueOrDefault(UnlockState.Affordable)} affordable with what the plan leaves,",
                $"{counts.GetValueOrDefault(UnlockState.OutOfReach)} short.",
            });

        var plannedUnlocks = entries.Where(entry => entry.IsUnlock && entry.Enabled).ToList();
        HasPlanned = plannedUnlocks.Count > 0;
        Materials = [.. MaterialCalculator.WithInventory(MaterialCalculator.Totals(catalog, plannedUnlocks), inventory).Select(line => new MaterialRow(line))];
        Commodities = MaterialCalculator.Commodities(catalog, plannedUnlocks);
        OnPropertyChanged(nameof(HasCommodities));
        var shortCount = inventory is null ? 0 : Materials.Count(row => row.IsShort);
        MaterialsSummary = !HasPlanned
            ? "Tick an unlock to plan it. Its materials join the plan's totals and the trades at the material traders."
            : inventory is null
                ? $"{Display.Plural(plannedUnlocks.Count, "unlock")} planned. Start the journal watch to see what you still need."
                : shortCount == 0
                    ? $"{Display.Plural(plannedUnlocks.Count, "unlock")} planned and every material covered."
                    : $"{Display.Plural(plannedUnlocks.Count, "unlock")} planned, {Display.Plural(shortCount, "material")} short.";
    }

    private void Select(UnlockRow row, bool selected)
    {
        if (selected)
        {
            Workspace.Plan.Add([PlanEntry.ForUnlock(Workspace.Catalog, row.Key)]);
            return;
        }

        var entry = Workspace.Plan.Entries.FirstOrDefault(entry => string.Equals(entry.Entry.Unlock, row.Key, StringComparison.Ordinal));
        entry?.RemoveCommand.Execute(null);
    }
}
