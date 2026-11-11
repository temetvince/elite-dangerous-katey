using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.Core.Planning;

namespace Katey.App.ViewModels;

/// <summary>
/// The plan as a drawer that opens over any page: the entries, the totals against the live inventory,
/// and the paste box for an EDSY export.
/// </summary>
public sealed class PlanDrawerViewModel : ObservableObject
{
    private readonly Workspace _workspace;

    /// <summary>Creates the drawer, closed.</summary>
    /// <param name="workspace">The shared state, whose plan this shows.</param>
    /// <param name="imported">Called after an import adds entries, with the entries and the ship name.</param>
    public PlanDrawerViewModel(Workspace workspace, Action<IReadOnlyList<PlanEntry>, string> imported)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(imported);

        _workspace = workspace;
        Totals = new TotalsViewModel(workspace.Catalog);
        Import = new ImportViewModel(workspace.Catalog, (entries, ship) =>
        {
            workspace.Plan.Add(entries);
            IsOpen = true;
            ShowImport = false;
            imported(entries, ship);
        });
        ToggleCommand = new RelayCommand(() => IsOpen = !IsOpen);
        CloseCommand = new RelayCommand(() => IsOpen = false);
        ToggleImportCommand = new RelayCommand(() => ShowImport = !ShowImport);
        Update();
    }

    /// <summary>Gets the plan's entries.</summary>
    public PlanViewModel Plan => _workspace.Plan;

    /// <summary>Gets the totals with the inventory laid against them.</summary>
    public TotalsViewModel Totals { get; }

    /// <summary>Gets the EDSY import form.</summary>
    public ImportViewModel Import { get; }

    /// <summary>Gets or sets whether the drawer is open.</summary>
    public bool IsOpen { get; set => SetProperty(ref field, value); }

    /// <summary>Gets or sets whether the import form is showing inside the drawer.</summary>
    public bool ShowImport { get; set => SetProperty(ref field, value); }

    /// <summary>Gets a short label for the button that opens the drawer, such as "3 · 2 short".</summary>
    public string Badge { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the drawer's subtitle.</summary>
    public IReadOnlyList<PhrasePart> SummaryParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets what to trade at the material traders to cover the shortfalls and restore the reserves.</summary>
    public TradeView Trades { get; private set => SetProperty(ref field, value); } = TradeView.From(null);

    /// <summary>Gets the command that opens or closes the drawer.</summary>
    public RelayCommand ToggleCommand { get; }

    /// <summary>Gets the command that closes the drawer.</summary>
    public RelayCommand CloseCommand { get; }

    /// <summary>Gets the command that shows or hides the import form.</summary>
    public RelayCommand ToggleImportCommand { get; }

    /// <summary>Recomputes the totals and labels from the plan and the inventory.</summary>
    public void Update()
    {
        var entries = _workspace.Plan.Snapshot();
        Totals.Update(entries, _workspace.InventoryCounts);
        Trades = TradeView.From(_workspace.Trades(), material => _workspace.Navigator.LinkTo(Names.Material(material), PageKind.Materials, material.Symbol));
        var enabled = entries.Count(entry => entry.Enabled);
        Badge = enabled == 0
            ? Strings.Get(Keys.Drawer_BadgeEmpty)
            : Totals.HasInventory
                ? (Totals.ShortCount == 0 ? Strings.Format(Keys.Drawer_BadgeCovered, ("count", enabled)) : Strings.Format(Keys.Drawer_BadgeShort, ("count", enabled), ("short", Totals.ShortCount)))
                : enabled.ToString(System.Globalization.CultureInfo.CurrentCulture);
        var engineers = _workspace.Engineers.Selected.Order(StringComparer.Ordinal).ToList();
        SummaryParts = Phrase.Format(
            Keys.Drawer_Summary,
            ("entries", Display.Plural(Keys.Plural_Entries, entries.Count)),
            ("counted", enabled),
            ("totals", Totals.Summary),
            ("engineers", EngineerLink.Names(engineers.Select(_workspace.EngineerLinkFor), Strings.Get(Keys.Drawer_NoneSelected))));
    }
}
