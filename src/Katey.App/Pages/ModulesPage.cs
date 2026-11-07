using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.ViewModels;
using Katey.Core;

namespace Katey.App.Pages;

/// <summary>One module type in the index.</summary>
/// <param name="Link">Its page.</param>
/// <param name="BlueprintCount">How many blueprints apply.</param>
/// <param name="Reachable">How many of them the selected engineers can roll at some grade.</param>
/// <param name="OwnedCount">How many modules of this type you own.</param>
public sealed record ModuleTypeRow(Link Link, int BlueprintCount, int Reachable, int OwnedCount)
{
    /// <summary>Gets the blueprint count as text, with the reachable count when engineers are selected.</summary>
    public string BlueprintsText => Reachable == BlueprintCount
        ? Display.Plural(BlueprintCount, "blueprint")
        : $"{Reachable} of {Display.Plural(BlueprintCount, "blueprint")} with your engineers";

    /// <summary>Gets "you own n", or empty.</summary>
    public string OwnedText => OwnedCount == 0 ? "" : $"you own {OwnedCount}";
}

/// <summary>Module types in one group, rolled up until opened. Whether it is open is remembered.</summary>
public sealed class ModuleGroupRow : ExpandableGroup
{
    /// <summary>Creates the group.</summary>
    /// <param name="group">The module group.</param>
    /// <param name="types">The types in it, alphabetical.</param>
    /// <param name="expansion">Where the open state is remembered.</param>
    public ModuleGroupRow(ModuleGroup group, IReadOnlyList<ModuleTypeRow> types, GroupExpansion expansion)
        : base(group.ToString(), Display.GroupLabel(group), expansion)
    {
        ArgumentNullException.ThrowIfNull(types);
        Group = group;
        Types = types;
    }

    /// <summary>Gets the module group.</summary>
    public ModuleGroup Group { get; }

    /// <summary>Gets a count line, such as "8 types, 3 owned".</summary>
    public string Summary => $"{Display.Plural(Types.Count, "type")}, {Types.Sum(type => type.OwnedCount)} owned";

    /// <summary>Gets the types in it.</summary>
    public IReadOnlyList<ModuleTypeRow> Types { get; }
}

/// <summary>
/// Every engineerable module type, by where it fits on a ship, one collapsible group per place, with a
/// switch that narrows the page to the types the selected engineers can roll a blueprint on.
/// </summary>
public sealed class ModulesPage : Page
{
    private readonly GroupExpansion _expansion;

    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <param name="openGroup">A <see cref="ModuleGroup"/> name to open on arrival, or empty. The choice is remembered like a click.</param>
    public ModulesPage(Workspace workspace, string openGroup = "")
        : base(new PageKey(PageKind.Modules, openGroup), workspace)
    {
        Title = "Modules";
        Kicker = "Browse";
        _expansion = new GroupExpansion(workspace.Preferences, "modules.expanded");
        ExpandAllCommand = new RelayCommand(() => _expansion.SetAll(Groups, true));
        CollapseAllCommand = new RelayCommand(() => _expansion.SetAll(Groups, false));
        if (Enum.TryParse<ModuleGroup>(openGroup, ignoreCase: true, out var group))
        {
            _expansion.Set(group.ToString(), true);
        }

        Refresh();
    }

    /// <summary>Gets the groups in catalog order. While the engineers page's "Show only items from selected engineers" switch narrows the app, only groups with a type the selected engineers can roll a blueprint on.</summary>
    public IReadOnlyList<ModuleGroupRow> Groups { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether an engineer is selected, so the switch has something to narrow to.</summary>
    public bool HasSelection => Workspace.Engineers.Count > 0;

    /// <summary>Gets one line on what the engineers page's switch is doing to this page, or empty while it is off.</summary>
    public string FilterText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the command that opens every group.</summary>
    public RelayCommand ExpandAllCommand { get; }

    /// <summary>Gets the command that closes every group.</summary>
    public RelayCommand CollapseAllCommand { get; }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var catalog = Workspace.Catalog;
        var selected = Workspace.Engineers.Selected;
        var owned = FleetState.OwnedModules(Workspace.FleetSnapshot)
            .Select(module => Workspace.Modules.Find(module.Item)?.Group)
            .Where(group => group is not null)
            .GroupBy(group => group!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var types = catalog.EngineerableModuleTypes();
        var narrow = Workspace.Scope.IsNarrowed;
        Groups =
        [
            .. types
                .GroupBy(type => type.Group)
                .Select(group => new ModuleGroupRow(
                    group.Key,
                    [
                        .. group.Select(type => new ModuleTypeRow(
                                LinkTo(type.Name, PageKind.ModuleType, type.Code),
                                type.Blueprints.Count,
                                selected.Count == 0
                                    ? type.Blueprints.Count
                                    : type.Blueprints.Count(blueprint => blueprint.Value.Values.Any(names => names.Any(selected.Contains))),
                                owned.GetValueOrDefault(type.Code)))
                            .Where(row => !narrow || row.Reachable > 0),
                    ],
                    _expansion))
                .Where(group => group.Types.Count > 0),
        ];
        OnPropertyChanged(nameof(HasSelection));
        FilterText = !Workspace.Scope.OnlyMyEngineers
            ? ""
            : selected.Count == 0
                ? "Show only items from selected engineers is on, but nobody is ticked on the engineers page, so every type is shown."
                : $"Show only items from selected engineers is on: {Display.Plural(Groups.Sum(group => group.Types.Count), "type")} of {types.Count} have a blueprint your {Display.Plural(selected.Count, "ticked engineer")} can roll. Change that on the engineers page.";
    }
}
