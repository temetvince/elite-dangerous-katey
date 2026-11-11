using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Planning;

namespace Katey.App.Pages;

/// <summary>One grade of a blueprint: what one roll costs, who offers it anywhere, and a button that plans it.</summary>
/// <param name="Grade">1–5.</param>
/// <param name="Materials">What one roll costs, with your inventory.</param>
/// <param name="Engineers">Everyone who offers this grade on some module type, selected ones first.</param>
/// <param name="AddCommand">Adds the blueprint at this grade, on the page's chosen module type and experimental, to the plan.</param>
public sealed record BlueprintGradeRow(int Grade, IReadOnlyList<MaterialChip> Materials, IReadOnlyList<EngineerLink> Engineers, RelayCommand AddCommand)
{
    /// <summary>Gets "Gn".</summary>
    public string Label => Display.Grade(Grade);

    /// <summary>Gets whether the grade has no materials.</summary>
    public bool IsFree => Materials.Count == 0;
}

/// <summary>One blueprint across every module type it applies to, with a bar that adds it to the plan.</summary>
public sealed class BlueprintPage : Page
{
    private readonly Blueprint _blueprint;

    /// <summary>Creates the page.</summary>
    /// <param name="key">Blueprint key. Must exist in the catalog.</param>
    /// <param name="workspace">The shared state.</param>
    public BlueprintPage(string key, Workspace workspace)
        : base(new PageKey(PageKind.Blueprint, key), workspace)
    {
        _blueprint = workspace.Catalog.Blueprints[key];
        Title = Names.Blueprint(_blueprint);
        Breadcrumbs = [LinkTo(Strings.Get(Keys.Nav_Modules), PageKind.Modules)];
        Kicker = Strings.Get(Keys.Blueprint_Kicker);
        GradeChoices = [0, .. _blueprint.GradeNumbers];
        AddCommand = new RelayCommand(() => Add(SelectedGrade), () => CanAdd(SelectedGrade));
        ModuleTypeChoices = [.. workspace.Scope.ModuleTypes(key).Select(type => new Choice(type.Code, Names.ModuleType(type)))];
        SelectedModuleType = ModuleTypeChoices.Count > 0 ? ModuleTypeChoices[0] : null;
        SelectedGrade = SelectedModuleType is { } type
            ? workspace.Scope.StartingGrade(workspace.Catalog.ModuleTypes[type.Key], key)
            : _blueprint.MaxGrade;
        Refresh();
    }

    /// <summary>Gets the module types the blueprint applies to.</summary>
    public IReadOnlyList<Link> ModuleTypes { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets one row per grade.</summary>
    public IReadOnlyList<BlueprintGradeRow> Grades { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a line on the journal key, for matching against exports.</summary>
    public string KeyText => Strings.Format(Keys.Blueprint_JournalName, ("key", _blueprint.Key));

    /// <summary>Gets the module types an added entry can be for: every type that takes the blueprint, or only those the selected engineers offer it on when narrowed to them.</summary>
    public IReadOnlyList<Choice> ModuleTypeChoices { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets or sets the module type an added entry is for. One of <see cref="ModuleTypeChoices"/>, or <see langword="null"/> when there is none. Choosing one sets the grades and experimentals on offer.</summary>
    public Choice? SelectedModuleType
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                var type = value is null ? null : Workspace.Catalog.ModuleTypes[value.Key];
                Experimentals = type is null ? [Choice.None] : [Choice.None, .. Workspace.Scope.Experimentals(type).Select(effect => new Choice(effect.Key, Names.Experimental(effect)))];
                SelectedExperimental = Choice.None;
                GradeChoices = type is null ? [0, .. _blueprint.GradeNumbers] : [0, .. Workspace.Scope.Grades(type, _blueprint.Key)];
                if (type is not null && !GradeChoices.Contains(SelectedGrade))
                {
                    SelectedGrade = Workspace.Scope.StartingGrade(type, _blueprint.Key);
                }

                AddCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets the grades an added entry can target on the chosen module type: every grade, or only up to the reach when narrowed to the selected engineers. 0 plans the experimental effect only.</summary>
    public IReadOnlyList<int> GradeChoices { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets one line saying the page is narrowed to the selected engineers, or empty.</summary>
    public string ScopeNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets or sets the grade the bar's Add button targets. Starts at the top, or at the grade the selected engineers reach on the chosen module type when narrowed to them. Must be one of <see cref="GradeChoices"/>.</summary>
    public int SelectedGrade
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AddCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets the experimental effects the chosen module type takes, "None" first.</summary>
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

    /// <summary>Gets or sets the experimental an added entry gets. Must be one of <see cref="Experimentals"/>.</summary>
    public Choice SelectedExperimental
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                AddCommand?.RaiseCanExecuteChanged();
            }
        }
    } = Choice.None;

    /// <summary>Gets the command that adds the blueprint at <see cref="SelectedGrade"/> to the plan.</summary>
    public RelayCommand AddCommand { get; }

    /// <summary>Gets a line on the last entry added from this page, or empty.</summary>
    public string AddedText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Adds the blueprint to the plan on the chosen module type, with the chosen experimental.</summary>
    /// <param name="grade">The target grade, or 0 for the experimental effect alone, which then needs an experimental chosen.</param>
    public void Add(int grade)
    {
        if (!CanAdd(grade) || SelectedModuleType is not { } type)
        {
            return;
        }

        var experimental = SelectedExperimental.Key.Length == 0 ? null : SelectedExperimental.Key;
        Workspace.Plan.Add([PlanEntry.Create(Workspace.Catalog, type.Key, _blueprint.Key, grade, experimental, PlanSource.Manual)]);
        AddedText = grade == 0
            ? Strings.Format(Keys.Blueprint_AddedExperimentalAlone, ("experimental", SelectedExperimental.Label), ("type", type.Label))
            : experimental is null
                ? Strings.Format(Keys.Blueprint_Added, ("blueprint", Names.Blueprint(_blueprint)), ("grade", Display.Grade(grade)), ("type", type.Label))
                : Strings.Format(Keys.Blueprint_AddedWithExperimental, ("blueprint", Names.Blueprint(_blueprint)), ("grade", Display.Grade(grade)), ("type", type.Label), ("experimental", SelectedExperimental.Label));
    }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var catalog = Workspace.Catalog;
        var selected = Workspace.Engineers.Selected;
        var types = catalog.ModuleTypesWithBlueprint(_blueprint.Key);
        ModuleTypes = [.. types.Select(type => LinkTo(Names.ModuleType(type), PageKind.ModuleType, type.Code))];
        var choices = Workspace.Scope.ModuleTypes(_blueprint.Key).Select(type => new Choice(type.Code, Names.ModuleType(type))).ToList();
        ScopeNote = !Workspace.Scope.IsNarrowed
            ? ""
            : choices.Count == 0
                ? Strings.Get(Keys.Blueprint_ScopeNobodyOffers)
                : Workspace.Scope.Note;
        if (!choices.SequenceEqual(ModuleTypeChoices))
        {
            ModuleTypeChoices = choices;
            SelectedModuleType = SelectedModuleType is { } current && choices.Contains(current) ? current : choices.Count > 0 ? choices[0] : null;
        }

        Grades =
        [
            .. _blueprint.GradeNumbers.Select(grade => new BlueprintGradeRow(
                grade,
                MaterialChip.From(catalog, _blueprint.Grades[grade]),
                [
                    .. types
                        .SelectMany(type => type.Blueprints[_blueprint.Key].GetValueOrDefault(grade, []))
                        .Distinct(StringComparer.Ordinal)
                        .OrderByDescending(selected.Contains)
                        .ThenBy(name => name, StringComparer.Ordinal)
                        .Select(Workspace.EngineerLinkFor),
                ],
                new RelayCommand(() => Add(grade), () => CanAdd(grade)))),
        ];
        AddCommand.RaiseCanExecuteChanged();
    }

    private bool CanAdd(int grade) => SelectedModuleType is not null && (grade > 0 || SelectedExperimental.Key.Length > 0);
}
