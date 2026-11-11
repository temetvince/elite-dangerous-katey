using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Fleet;
using Katey.Core.Planning;

namespace Katey.App.Pages;

/// <summary>One blueprint of a module type, with the grade to plan it at and the engineers who offer it.</summary>
public sealed class BlueprintRow : ObservableObject
{
    private readonly ModuleTypePage _page;
    private readonly Blueprint _blueprint;

    /// <summary>Creates the row.</summary>
    /// <param name="page">The page, which knows the selected experimental.</param>
    /// <param name="blueprint">The blueprint.</param>
    /// <param name="link">The blueprint's own page.</param>
    /// <param name="reachable">Highest grade the selected engineers reach, or <see langword="null"/>.</param>
    /// <param name="grades">The grades the picker offers, without the experimental-only 0, lowest first.</param>
    /// <param name="startingGrade">The grade the picker starts at.</param>
    /// <param name="engineers">Every engineer who offers it, with the grade they stop at and whether they are selected.</param>
    public BlueprintRow(ModuleTypePage page, Blueprint blueprint, Link link, int? reachable, IReadOnlyList<int> grades, int startingGrade, IReadOnlyList<BlueprintEngineer> engineers)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(engineers);

        _page = page;
        _blueprint = blueprint;
        Link = link;
        Grades = [0, .. grades];
        SelectedGrade = startingGrade;
        ReachableGrade = reachable;
        Engineers = engineers;
        AddCommand = new RelayCommand(Add);
    }

    /// <summary>Gets the blueprint's page.</summary>
    public Link Link { get; }

    /// <summary>Gets the blueprint key.</summary>
    public string Key => _blueprint.Key;

    /// <summary>Gets the display name.</summary>
    public string Name => Names.Blueprint(_blueprint);

    /// <summary>Gets the grades to choose from: 0 for experimental only, then every grade the blueprint has.</summary>
    public IReadOnlyList<int> Grades { get; }

    /// <summary>Gets or sets the grade an added entry targets. Starts at the top, or at the grade the selected engineers reach when the pickers are narrowed to them. Must be one of <see cref="Grades"/>; 0 adds an experimental-only entry.</summary>
    public int SelectedGrade { get; set => SetProperty(ref field, value); }

    /// <summary>Gets the highest grade the selected engineers reach, or <see langword="null"/>.</summary>
    public int? ReachableGrade { get; }

    /// <summary>Gets "Gn" for the reachable grade, "none" when nobody selected offers it, or empty with no selection.</summary>
    public string ReachableText => _page.HasSelection ? (ReachableGrade is { } grade ? Display.Grade(grade) : Strings.Get(Keys.ModuleType_None)) : "";

    /// <summary>Gets whether the selected engineers reach the top grade.</summary>
    public bool ReachesTop => ReachableGrade == _blueprint.MaxGrade;

    /// <summary>Gets who offers the blueprint.</summary>
    public IReadOnlyList<BlueprintEngineer> Engineers { get; }

    /// <summary>Gets the command that adds the blueprint to the plan at <see cref="SelectedGrade"/> with the page's experimental.</summary>
    public RelayCommand AddCommand { get; }

    private void Add() => _page.Add(_blueprint.Key, SelectedGrade);
}

/// <summary>One engineer who offers a blueprint on a module type.</summary>
/// <param name="Engineer">Their name as Katey shows it everywhere.</param>
/// <param name="MaxGrade">The grade they stop at.</param>
/// <param name="DistanceLy">Distance from the ship, or <see langword="null"/>.</param>
public sealed record BlueprintEngineer(EngineerLink Engineer, int MaxGrade, double? DistanceLy)
{
    /// <summary>Gets whether you will visit them.</summary>
    public bool Selected => Engineer.Selected;

    /// <summary>Gets "to Gn".</summary>
    public string GradeText => Strings.Format(Keys.ModuleType_ToGrade, ("grade", Display.Grade(MaxGrade)));

    /// <summary>Gets the distance as text.</summary>
    public string DistanceText => Display.DistanceOrUnknown(DistanceLy);
}

/// <summary>One module you own of this type.</summary>
/// <param name="Name">Module name with size and rating.</param>
/// <param name="Engineering">Engineering as text, or "stock".</param>
/// <param name="Where"></param>
/// <param name="IsEngineered">Whether it carries engineering.</param>
public sealed record OwnedModuleRow(string Name, string Engineering, Place Where, bool IsEngineered);

/// <summary>One saved fit that wants a module of this type.</summary>
/// <param name="Fit">The fit's page.</param>
/// <param name="Wants">Module and engineering wanted.</param>
/// <param name="Status">Where it was found, or "missing".</param>
/// <param name="Quality">How well it matched.</param>
/// <param name="Tone">The colour the status takes.</param>
public sealed record FitWantRow(Link Fit, string Wants, string Status, MatchQuality Quality, StatusTone Tone)
{
    /// <summary>Gets whether an owned module satisfies the want in full.</summary>
    public bool IsReady => Quality == MatchQuality.Exact;

    /// <summary>Gets whether the status is green: fitted to a ship of the fit's hull.</summary>
    public bool IsFitted => Tone == StatusTone.Fitted;

    /// <summary>Gets whether the status is blue: in storage.</summary>
    public bool IsStored => Tone == StatusTone.Stored;

    /// <summary>Gets whether the status is yellow: fitted to a ship of another hull.</summary>
    public bool IsElsewhere => Tone == StatusTone.Elsewhere;

    /// <summary>Gets whether the status is orange: owned but needing a change, or in doubt.</summary>
    public bool IsChange => Tone == StatusTone.Change;

    /// <summary>Gets whether nothing suitable is owned.</summary>
    public bool IsMissing => Quality == MatchQuality.Missing;

    /// <summary>Gets whether the match rests on an unknown experimental.</summary>
    public bool IsUncertain => Quality == MatchQuality.ExperimentalUnknown;

    /// <summary>Gets whether a module is owned but not engineered all the way.</summary>
    public bool IsPartial => Quality == MatchQuality.Partial;

    /// <summary>Gets whether the row deserves a caution: partial, or resting on an unknown experimental.</summary>
    public bool IsWarn => IsPartial || IsUncertain;
}

/// <summary>One variant of the type the game sells.</summary>
/// <param name="Name">Name with size, rating and mount.</param>
/// <param name="Owned">Whether you own at least one.</param>
public sealed record VariantRow(string Name, bool Owned);

/// <summary>
/// One module type: its blueprints with who offers them and the grade your engineers reach, its
/// experimental effects, the variants the game sells, what you own, and what your fits want.
/// </summary>
public sealed class ModuleTypePage : Page
{
    private readonly ModuleType _type;

    /// <summary>Creates the page.</summary>
    /// <param name="code">Module type code. Must exist in the catalog.</param>
    /// <param name="workspace">The shared state.</param>
    public ModuleTypePage(string code, Workspace workspace)
        : base(new PageKey(PageKind.ModuleType, code), workspace)
    {
        _type = workspace.Catalog.ModuleTypes[code];
        Title = Names.ModuleType(_type);
        Breadcrumbs = [LinkTo(Strings.Get(Keys.Nav_Modules), PageKind.Modules), LinkTo(Display.GroupLabel(_type.Group), PageKind.Modules, _type.Group.ToString())];
        SelectedExperimental = Choice.None;
        Sections = new SectionSwitch([("blueprints", Strings.Get(Keys.Section_Blueprints)), ("owned", Strings.Get(Keys.Section_YouOwn)), ("fits", Strings.Get(Keys.Section_InYourFits)), ("variants", Strings.Get(Keys.Section_Variants))], "blueprints", workspace.Preferences, "section.moduleType");
        BlueprintSort = new TableSort<BlueprintRow>(
            [
                new("name", Strings.Get(Keys.Column_Blueprint), row => row.Name),
                new("reach", Strings.Get(Keys.Column_YouReach), row => row.ReachableGrade, DescendingFirst: true),
            ],
            "name",
            workspace.Preferences,
            "sort.blueprints");
        OwnedSort = new TableSort<OwnedModuleRow>(
            [
                new("name", Strings.Get(Keys.Column_Module), row => row.Name),
                new("engineering", Strings.Get(Keys.Column_Engineering), row => row.Engineering),
                new("place", Strings.Get(Keys.Column_Where), row => row.Where.Text),
            ],
            "name",
            workspace.Preferences,
            "sort.owned");
        FitWantSort = new TableSort<FitWantRow>(
            [
                new("fit", Strings.Get(Keys.Column_Fit), row => row.Fit.Label),
                new("wants", Strings.Get(Keys.Column_Wants), row => row.Wants),
                new("status", Strings.Get(Keys.Column_Status), row => row.Status),
            ],
            "fit",
            workspace.Preferences,
            "sort.fitWants");
        BlueprintSort.Changed += (_, _) => Refresh();
        OwnedSort.Changed += (_, _) => Refresh();
        FitWantSort.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Gets the page's sections: blueprints, owned, fits, variants. One shows at a time.</summary>
    public SectionSwitch Sections { get; }

    /// <summary>Gets the blueprint table's sort.</summary>
    public TableSort<BlueprintRow> BlueprintSort { get; }

    /// <summary>Gets the owned-module table's sort.</summary>
    public TableSort<OwnedModuleRow> OwnedSort { get; }

    /// <summary>Gets the fit-wants table's sort.</summary>
    public TableSort<FitWantRow> FitWantSort { get; }

    /// <summary>Gets whether any engineer is selected, which is when reachable grades are shown.</summary>
    public bool HasSelection => Workspace.Engineers.Count > 0;

    /// <summary>Gets whether the type can be engineered at all.</summary>
    public bool IsEngineerable => _type.IsEngineerable;

    /// <summary>Gets one row per blueprint.</summary>
    public IReadOnlyList<BlueprintRow> Blueprints { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the experimental effects the picker offers, "None" first: every one the type allows, or only "None" when narrowed to engineers who cannot apply them.</summary>
    public IReadOnlyList<Choice> Experimentals
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasExperimentals));
            }
        }
    } = [Choice.None];

    /// <summary>Gets whether there is an experimental to choose: the picker hides when "None" is the only option.</summary>
    public bool HasExperimentals => Experimentals.Count > 1;

    /// <summary>Gets one line saying the page is narrowed to the selected engineers, or empty.</summary>
    public string ScopeNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets or sets the experimental an added entry gets. Must be one of <see cref="Experimentals"/>.</summary>
    public Choice SelectedExperimental
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                ExperimentalMaterials = value.Key.Length == 0 ? [] : MaterialChip.From(Workspace.Catalog, Workspace.Catalog.Experimentals[value.Key].Components);
            }
        }
    } = Choice.None;

    /// <summary>Gets what the chosen experimental costs. Empty when none is chosen.</summary>
    public IReadOnlyList<MaterialChip> ExperimentalMaterials { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a line on experimentals and who applies them.</summary>
    public IReadOnlyList<PhrasePart> ExperimentalsParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets the variants the game sells, by class then name.</summary>
    public IReadOnlyList<VariantRow> Variants { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether the table lists variants for this type.</summary>
    public bool HasVariants => Variants.Count > 0;

    /// <summary>Gets the modules you own of this type.</summary>
    public IReadOnlyList<OwnedModuleRow> Owned { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether you own any.</summary>
    public bool HasOwned => Owned.Count > 0;

    /// <summary>Gets what your saved fits want of this type.</summary>
    public IReadOnlyList<FitWantRow> FitWants { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether any fit wants one.</summary>
    public bool HasFitWants => FitWants.Count > 0;

    /// <summary>Gets a line on the plan: how many entries use this type.</summary>
    public string PlanText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Adds a plan entry for this type.</summary>
    /// <param name="blueprint">Blueprint key the type lists.</param>
    /// <param name="grade">Target grade.</param>
    public void Add(string blueprint, int grade)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        var experimental = SelectedExperimental.Key.Length == 0 ? null : SelectedExperimental.Key;
        Workspace.Plan.Add([PlanEntry.Create(Workspace.Catalog, _type.Code, blueprint, grade, experimental, PlanSource.Manual)]);
    }

    /// <summary>Where an owned module is: its system's link, then the ship it is fitted to or that it is stored.</summary>
    /// <param name="module">The module.</param>
    /// <returns>The place. A module in transit has no system.</returns>
    public Place WhereOwned(OwnedModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        var system = module.System is { Length: > 0 } name ? LinkTo(name, PageKind.System, name) : null;
        return module.Place == ModulePlace.Installed
            ? new Place(system, Strings.Format(Keys.ModuleType_FittedTo, ("ship", module.Holder)))
            : new Place(system, system is null ? module.Holder : Strings.Get(Keys.ModuleType_InStorage));
    }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var catalog = Workspace.Catalog;
        var selected = Workspace.Engineers.Selected;
        var scope = Workspace.Scope;
        OnPropertyChanged(nameof(HasSelection));
        ScopeNote = scope.Note;
        Experimentals = [Choice.None, .. scope.Experimentals(_type).Select(effect => new Choice(effect.Key, Names.Experimental(effect)))];
        if (!Experimentals.Contains(SelectedExperimental))
        {
            SelectedExperimental = Choice.None;
        }

        var rows = new List<BlueprintRow>();
        foreach (var blueprint in scope.Blueprints(_type))
        {
            var grades = _type.Blueprints[blueprint.Key];
            var engineers = grades.Values.SelectMany(names => names).Distinct(StringComparer.Ordinal)
                .Select(name => new BlueprintEngineer(
                    Workspace.EngineerLinkFor(name),
                    grades.Where(grade => grade.Value.Contains(name, StringComparer.Ordinal)).Max(grade => grade.Key),
                    Workspace.DistanceFromHere(Workspace.PositionOf(catalog.Engineers[name]))))
                .OrderByDescending(engineer => engineer.Selected)
                .ThenByDescending(engineer => engineer.MaxGrade)
                .ThenBy(engineer => engineer.DistanceLy is null)
                .ThenBy(engineer => engineer.DistanceLy)
                .ToList();
            var reachable = EngineerCoverage.Reach(_type, blueprint.Key, selected);
            rows.Add(new BlueprintRow(
                this,
                blueprint,
                LinkTo(Names.Blueprint(blueprint), PageKind.Blueprint, blueprint.Key),
                reachable,
                scope.Grades(_type, blueprint.Key),
                scope.StartingGrade(_type, blueprint.Key),
                engineers));
        }

        Blueprints = BlueprintSort.Apply(rows);

        var appliers = catalog.EngineersForModuleType(_type.Code);
        var selectedAppliers = appliers.Where(selected.Contains).ToList();
        var experimentals = Display.Plural(Keys.Plural_Experimentals, _type.Experimentals.Count);
        ExperimentalsParts = _type.Experimentals.Count == 0
            ? Phrase.Text(Strings.Get(Keys.ModuleType_NoExperimentals))
            : selected.Count == 0
                ? Phrase.Format(Keys.ModuleType_ExperimentalsAnyEngineer, ("experimentals", experimentals))
                : selectedAppliers.Count == 0
                    ? Phrase.Format(Keys.ModuleType_ExperimentalsNobodyTicked, ("experimentals", experimentals))
                    : Phrase.Format(Keys.ModuleType_ExperimentalsBy, ("experimentals", experimentals), ("engineers", EngineerLink.Names(selectedAppliers.Select(Workspace.EngineerLinkFor), "")));

        var fleet = Workspace.FleetSnapshot;
        var owned = FleetState.OwnedModules(fleet).Where(module => string.Equals(Workspace.Modules.Find(module.Item)?.Group, _type.Code, StringComparison.OrdinalIgnoreCase)).ToList();
        var ownedNames = owned.Select(module => Workspace.ModuleName(module.Item)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Variants = [.. Workspace.Modules.OfType(_type.Code).Select(module => new VariantRow(module.Name, ownedNames.Contains(module.Name)))];
        OnPropertyChanged(nameof(HasVariants));
        Owned = OwnedSort.Apply(owned.Select(module => new OwnedModuleRow(
            Workspace.ModuleName(module.Item),
            EngineeringText(module.Engineering),
            WhereOwned(module),
            module.IsEngineered)));
        OnPropertyChanged(nameof(HasOwned));

        var report = Workspace.Fleet.Match(fleet, Workspace.Farness);
        FitWants = FitWantSort.Apply(report.Fits.SelectMany(fit => fit.Lines
                .Where(line => string.Equals(Workspace.Modules.Find(line.Required.Item)?.Group, _type.Code, StringComparison.OrdinalIgnoreCase))
                .Select(line => new FitWantRow(
                    LinkTo(fit.Fitting.Name, PageKind.Fit, fit.Fitting.Id),
                    Strings.Format(Keys.ModuleType_FitWants, ("module", Workspace.ModuleName(line.Required.Item)), ("engineering", EngineeringText(line.Required.Engineering))),
                    FleetPage.StatusText(line),
                    line.Quality,
                    FleetPage.ToneOf(line, fit.Fitting.Hull)))));
        OnPropertyChanged(nameof(HasFitWants));

        var planned = Workspace.Plan.Snapshot().Count(entry => entry.ModuleType == _type.Code);
        PlanText = planned == 0 ? "" : Strings.Format(Keys.ModuleType_PlanEntriesUse, ("entries", Display.Plural(Keys.Plural_PlanEntries, planned)));
    }

    private string EngineeringText(Engineering? engineering)
    {
        if (engineering is null)
        {
            return Strings.Get(Keys.Engineering_Stock);
        }

        var catalog = Workspace.Catalog;
        var blueprint = catalog.Blueprints.TryGetValue(engineering.Blueprint, out var bp) ? Names.Blueprint(bp) : engineering.Blueprint;
        var grade = Display.Grade(engineering.Level);
        if (engineering.Experimental is { } effect)
        {
            return Strings.Format(Keys.Engineering_TextWithExperimental, ("blueprint", blueprint), ("grade", grade), ("experimental", catalog.Experimentals.TryGetValue(effect, out var ex) ? Names.Experimental(ex) : effect));
        }

        return Strings.Format(engineering.ExperimentalKnown ? Keys.Engineering_Text : Keys.Engineering_TextExperimentalUnknown, ("blueprint", blueprint), ("grade", grade));
    }
}
