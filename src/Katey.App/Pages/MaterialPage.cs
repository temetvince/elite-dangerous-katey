using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.ViewModels;
using Katey.Core;

namespace Katey.App.Pages;

/// <summary>One plan entry that spends a material.</summary>
/// <param name="Title">Module and blueprint.</param>
/// <param name="Detail">Which grades and how many rolls.</param>
/// <param name="Needed">How many the entry needs in all.</param>
public sealed record MaterialSpendRow(string Title, string Detail, int Needed);

/// <summary>One blueprint grade that uses a material, with the module types it applies to.</summary>
/// <param name="Blueprint">The blueprint's page.</param>
/// <param name="GradeNumber">1–5.</param>
/// <param name="Count">How many one roll costs.</param>
/// <param name="ModuleTypes">Module types the blueprint applies to.</param>
public sealed record MaterialUseRow(Link Blueprint, int GradeNumber, int Count, IReadOnlyList<Link> ModuleTypes)
{
    /// <summary>Gets "Gn".</summary>
    public string Grade => Display.Grade(GradeNumber);

    /// <summary>Gets "n per roll".</summary>
    public string CountText => $"{Count} per roll";
}

/// <summary>A body in the current system that carries a raw material.</summary>
/// <param name="Body">Short body name.</param>
/// <param name="Share">Surface share as text.</param>
/// <param name="Landable">Whether a ship can land there.</param>
public sealed record MaterialSourceRow(string Body, string Share, bool Landable);

/// <summary>One material: what you hold, what the plan spends it on, what else uses it, and where to find it nearby.</summary>
public sealed class MaterialPage : Page
{
    private readonly Material _material;

    /// <summary>Creates the page.</summary>
    /// <param name="name">Material display name. Must exist in the catalog.</param>
    /// <param name="workspace">The shared state.</param>
    public MaterialPage(string name, Workspace workspace)
        : base(new PageKey(PageKind.Material, name), workspace)
    {
        _material = workspace.Catalog.Materials[name];
        Title = _material.Name;
        Breadcrumbs = [LinkTo("Materials", PageKind.Materials), LinkTo(Core.Planning.ShoppingList.CategoryLabels[_material.Category], PageKind.Materials, _material.Category.ToString())];
        Kicker = $"{Display.Grade(_material.Grade)} · {_material.Line}";
        Sections = new SectionSwitch([("spends", "Where your plan spends it"), ("uses", "Used by"), ("sources", "Where to find it")], "spends", workspace.Preferences, "section.material");
        SpendSort = new TableSort<MaterialSpendRow>(
            [
                new("entry", "Entry", row => row.Title),
                new("needed", "Needs", row => row.Needed, DescendingFirst: true),
            ],
            "entry",
            workspace.Preferences,
            "sort.materialSpends");
        UseSort = new TableSort<MaterialUseRow>(
            [
                new("blueprint", "Blueprint", row => row.Blueprint.Label),
                new("grade", "Grade", row => row.GradeNumber),
                new("count", "Per roll", row => row.Count, DescendingFirst: true),
            ],
            "blueprint",
            workspace.Preferences,
            "sort.materialUses");
        SpendSort.Changed += (_, _) => Refresh();
        UseSort.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Gets the page's sections: plan spends, uses, sources. One shows at a time.</summary>
    public SectionSwitch Sections { get; }

    /// <summary>Gets the spend table's sort.</summary>
    public TableSort<MaterialSpendRow> SpendSort { get; }

    /// <summary>Gets the uses table's sort.</summary>
    public TableSort<MaterialUseRow> UseSort { get; }

    /// <summary>Gets how many you hold as text, or a dash.</summary>
    public string HaveText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets how many the plan needs as text.</summary>
    public string NeededText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets how many are still to collect as text, or empty.</summary>
    public string ShortText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether the plan needs more than you hold.</summary>
    public bool IsShort { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the plan entries that spend it.</summary>
    public IReadOnlyList<MaterialSpendRow> Spends { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether the plan spends it.</summary>
    public bool HasSpends => Spends.Count > 0;

    /// <summary>Gets every blueprint grade that uses it.</summary>
    public IReadOnlyList<MaterialUseRow> Uses { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the experimental effects that use it, as text, or empty.</summary>
    public string ExperimentalsText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets bodies in the current system with it on the surface. Only raw materials have any.</summary>
    public IReadOnlyList<MaterialSourceRow> Sources { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a heading for the sources, or a note that none are known.</summary>
    public string SourcesText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the journal symbol.</summary>
    public string SymbolText => $"Journal symbol {_material.Symbol}";

    /// <inheritdoc/>
    public override void Refresh()
    {
        var catalog = Workspace.Catalog;
        var counts = Workspace.InventoryCounts;
        var have = counts?.GetValueOrDefault(_material.Symbol);
        var entries = Workspace.Plan.Snapshot().Where(entry => entry.Enabled).ToList();

        var spends = new List<MaterialSpendRow>();
        var needed = 0;
        foreach (var entry in entries)
        {
            var blueprint = catalog.Blueprints[entry.Blueprint];
            var parts = new List<string>();
            var total = 0;
            foreach (var grade in entry.RolledGrades(catalog))
            {
                if (blueprint.Grades[grade].TryGetValue(_material.Name, out var perRoll) && entry.RollsAt(grade) > 0)
                {
                    parts.Add($"{Display.Grade(grade)} ×{entry.RollsAt(grade)}");
                    total += perRoll * entry.RollsAt(grade);
                }
            }

            if (entry.Experimental is { } effect && catalog.Experimentals[effect].Components.TryGetValue(_material.Name, out var forEffect))
            {
                parts.Add(catalog.Experimentals[effect].Name);
                total += forEffect;
            }

            if (total > 0)
            {
                var module = entry.ModuleType is { } code ? catalog.ModuleTypes[code].Name : "Unknown module";
                spends.Add(new MaterialSpendRow($"{module}: {blueprint.Name} {Display.Grade(entry.TargetGrade)}", string.Join(", ", parts), total));
                needed += total;
            }
        }

        Spends = SpendSort.Apply(spends);
        OnPropertyChanged(nameof(HasSpends));
        HaveText = have is { } count ? count.ToString(CultureInfo.InvariantCulture) : "—";
        NeededText = needed.ToString(CultureInfo.InvariantCulture);
        IsShort = have is { } h && needed > h;
        ShortText = IsShort ? (needed - have!.Value).ToString(CultureInfo.InvariantCulture) : "";

        Uses = UseSort.Apply(catalog.MaterialUses(_material.Name).Select(use => new MaterialUseRow(
            LinkTo(catalog.Blueprints[use.Blueprint].Name, PageKind.Blueprint, use.Blueprint),
            use.Grade,
            use.Count,
            [.. catalog.ModuleTypesWithBlueprint(use.Blueprint).Select(type => LinkTo(type.Name, PageKind.ModuleType, type.Code))])));
        var effects = catalog.ExperimentalsUsing(_material.Name);
        ExperimentalsText = effects.Count == 0 ? "" : "Also in the experimental effects " + string.Join(", ", effects.Select(effect => effect.Name)) + ".";

        var system = Workspace.System;
        Sources =
        [
            .. system.Bodies
                .Where(body => body.Materials.ContainsKey(_material.Symbol))
                .OrderByDescending(body => body.Materials[_material.Symbol])
                .Select(body => new MaterialSourceRow(
                    BodyRowBuilder.ShortName(body.Name, system.SystemName),
                    string.Create(CultureInfo.InvariantCulture, $"{body.Materials[_material.Symbol]:0.0}%"),
                    body.Landable)),
        ];
        SourcesText = _material.Category != MaterialCategory.Raw
            ? ""
            : system.SystemName is null
                ? "Surface sources appear once the journal reports a system."
                : Sources.Count == 0
                    ? $"No scanned body in {system.SystemName} carries it."
                    : $"On the surface in {system.SystemName}";
    }
}
