using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
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
    public string CountText => Strings.Format(Keys.Material_PerRoll, ("count", Count));
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
    /// <param name="symbol">The material's journal symbol. Must exist in the catalog.</param>
    /// <param name="workspace">The shared state.</param>
    public MaterialPage(string symbol, Workspace workspace)
        : base(new PageKey(PageKind.Material, symbol), workspace)
    {
        _material = workspace.Catalog.Materials[symbol];
        Title = Names.Material(_material);
        Breadcrumbs = [LinkTo(Strings.Get(Keys.Nav_Materials), PageKind.Materials), LinkTo(Display.CategoryLabel(_material.Category), PageKind.Materials, _material.Category.ToString())];
        Kicker = Strings.Format(Keys.Material_Kicker, ("grade", Display.Grade(_material.Grade)), ("family", Names.Family(_material.Line)));
        Sections = new SectionSwitch([("spends", Strings.Get(Keys.Section_PlanSpends)), ("uses", Strings.Get(Keys.Section_UsedBy)), ("sources", Strings.Get(Keys.Section_WhereToFind))], "spends", workspace.Preferences, "section.material");
        SpendSort = new TableSort<MaterialSpendRow>(
            [
                new("entry", Strings.Get(Keys.Column_Entry), row => row.Title),
                new("needed", Strings.Get(Keys.Column_Needs), row => row.Needed, DescendingFirst: true),
            ],
            "entry",
            workspace.Preferences,
            "sort.materialSpends");
        UseSort = new TableSort<MaterialUseRow>(
            [
                new("blueprint", Strings.Get(Keys.Column_Blueprint), row => row.Blueprint.Label),
                new("grade", Strings.Get(Keys.Column_Grade), row => row.GradeNumber),
                new("count", Strings.Get(Keys.Column_PerRoll), row => row.Count, DescendingFirst: true),
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

    /// <summary>Gets the heading over the surface sources, the system you are in as a link; empty for a material that does not lie on surfaces.</summary>
    public IReadOnlyList<PhrasePart> SourcesParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets the journal symbol.</summary>
    public string SymbolText => Strings.Format(Keys.Material_JournalSymbol, ("symbol", _material.Symbol));

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
                if (blueprint.Grades[grade].TryGetValue(_material.Symbol, out var perRoll) && entry.RollsAt(grade) > 0)
                {
                    parts.Add(Strings.Format(Keys.Material_GradeTimesRolls, ("grade", Display.Grade(grade)), ("rolls", entry.RollsAt(grade))));
                    total += perRoll * entry.RollsAt(grade);
                }
            }

            if (entry.Experimental is { } effect && catalog.Experimentals[effect].Components.TryGetValue(_material.Symbol, out var forEffect))
            {
                parts.Add(Names.Experimental(catalog.Experimentals[effect]));
                total += forEffect;
            }

            if (total > 0)
            {
                var module = entry.ModuleType is { } code ? Names.ModuleType(catalog.ModuleTypes[code]) : Strings.Get(Keys.Page_UnknownModule);
                spends.Add(new MaterialSpendRow(Strings.Format(Keys.Material_SpendTitle, ("module", module), ("blueprint", Names.Blueprint(blueprint)), ("grade", Display.Grade(entry.TargetGrade))), Display.List(parts), total));
                needed += total;
            }
        }

        Spends = SpendSort.Apply(spends);
        OnPropertyChanged(nameof(HasSpends));
        HaveText = have is { } count ? count.ToString(CultureInfo.InvariantCulture) : "—";
        NeededText = needed.ToString(CultureInfo.InvariantCulture);
        IsShort = have is { } h && needed > h;
        ShortText = IsShort ? (needed - have!.Value).ToString(CultureInfo.InvariantCulture) : "";

        Uses = UseSort.Apply(catalog.MaterialUses(_material.Symbol).Select(use => new MaterialUseRow(
            LinkTo(Names.Blueprint(catalog.Blueprints[use.Blueprint]), PageKind.Blueprint, use.Blueprint),
            use.Grade,
            use.Count,
            [.. catalog.ModuleTypesWithBlueprint(use.Blueprint).Select(type => LinkTo(Names.ModuleType(type), PageKind.ModuleType, type.Code))])));
        var effects = catalog.ExperimentalsUsing(_material.Symbol);
        ExperimentalsText = effects.Count == 0 ? "" : Strings.Format(Keys.Material_AlsoInExperimentals, ("names", Display.List(effects.Select(Names.Experimental))));

        var system = Workspace.System;
        Sources =
        [
            .. system.Bodies
                .Where(body => body.Materials.ContainsKey(_material.Symbol))
                .OrderByDescending(body => body.Materials[_material.Symbol])
                .Select(body => new MaterialSourceRow(
                    BodyRowBuilder.ShortName(body.Name, system.SystemName),
                    Strings.Format(Keys.Unit_Percent, ("value", body.Materials[_material.Symbol])),
                    body.Landable)),
        ];
        SourcesParts = _material.Category != MaterialCategory.Raw
            ? Phrase.Empty
            : system.SystemName is not { } here
                ? Phrase.Text(Strings.Get(Keys.Material_SourcesWhenKnown))
                : Sources.Count == 0
                    ? Phrase.Format(Keys.Material_NoBodyCarriesIt, ("system", LinkTo(here, PageKind.System, here)))
                    : Phrase.Format(Keys.Material_OnTheSurfaceIn, ("system", LinkTo(here, PageKind.System, here)));
    }
}
