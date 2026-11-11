using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Text;
using Katey.Core;
using Katey.Core.Planning;

namespace Katey.App.ViewModels;

/// <summary>The roll count at one grade of an entry.</summary>
public sealed class RollViewModel : ObservableObject
{
    private readonly Action<int, int> _set;
    private int _count;

    /// <summary>Creates the row.</summary>
    /// <param name="grade">1–5.</param>
    /// <param name="count">The current count.</param>
    /// <param name="set">Receives the grade and the new count when the user edits it.</param>
    public RollViewModel(int grade, int count, Action<int, int> set)
    {
        ArgumentNullException.ThrowIfNull(set);

        Grade = grade;
        _count = count;
        _set = set;
    }

    /// <summary>Gets the grade.</summary>
    public int Grade { get; }

    /// <summary>Gets <c>Gn</c>.</summary>
    public string Label => Display.Grade(Grade);

    /// <summary>Gets or sets the count. Negative values are ignored.</summary>
    public int Count
    {
        get => _count;
        set
        {
            if (value >= 0 && SetProperty(ref _count, value))
            {
                _set(Grade, value);
            }
        }
    }
}

/// <summary>
/// One plan entry as the window edits it: its enable switch, target grade, experimental, rolls per
/// grade, the engineers who can serve it, and its materials. A technology broker unlock has only the
/// switch, its fixed materials and the commodities it also costs.
/// </summary>
public sealed class PlanEntryViewModel : ObservableObject
{
    private readonly Catalog _catalog;
    private readonly Action<PlanEntryViewModel> _changed;
    private readonly Action<PlanEntryViewModel> _remove;
    private readonly Func<string, EngineerLink> _engineerLink;
    private IReadOnlySet<string> _selected = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Creates the row.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="entry">The entry. Its blueprint, or its unlock, must exist in <paramref name="catalog"/>.</param>
    /// <param name="selected">The current engineer selection.</param>
    /// <param name="engineerLink">Makes an engineer's name as Katey shows it everywhere; see <see cref="Workspace.EngineerLinkFor"/>.</param>
    /// <param name="changed">Called after any edit, with this row.</param>
    /// <param name="remove">Called when the user removes the row.</param>
    public PlanEntryViewModel(
        Catalog catalog,
        PlanEntry entry,
        IReadOnlySet<string> selected,
        Func<string, EngineerLink> engineerLink,
        Action<PlanEntryViewModel> changed,
        Action<PlanEntryViewModel> remove)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(engineerLink);
        ArgumentNullException.ThrowIfNull(changed);
        ArgumentNullException.ThrowIfNull(remove);

        _catalog = catalog;
        _engineerLink = engineerLink;
        _changed = changed;
        _remove = remove;
        Entry = entry;
        RemoveCommand = new RelayCommand(() => _remove(this));

        Grades = entry.Unlock is null ? [0, .. catalog.Blueprints[entry.Blueprint].GradeNumbers] : [];
        Word();
        Refresh(selected);
    }

    /// <summary>Gets whether the entry is a technology broker unlock, which has no grade, experimental or rolls to edit.</summary>
    public bool IsUnlock => Entry.IsUnlock;

    /// <summary>Gets the commodities an unlock also costs, as one sentence, or empty.</summary>
    public string CommoditiesText { get; private set; } = "";

    /// <summary>Gets whether there are commodities to mention.</summary>
    public bool HasCommodities => CommoditiesText.Length > 0;

    /// <summary>Gets the entry as it currently stands.</summary>
    public PlanEntry Entry { get; private set; }

    /// <summary>Gets the module type name, or a description of the item when the type is unknown.</summary>
    public string ModuleLabel { get; private set; } = "";

    /// <summary>Gets the blueprint's display name, or the unlock's name.</summary>
    public string BlueprintLabel { get; private set; } = "";

    /// <summary>Gets <c>module: blueprint</c>, or the unlock's name alone.</summary>
    public string Title => IsUnlock ? BlueprintLabel : Strings.Format(Keys.Plan_Title, ("module", ModuleLabel), ("blueprint", BlueprintLabel));

    /// <summary>Gets where the entry came from: the ship and slot of an import, the broker of an unlock, or empty for a manual module entry.</summary>
    public string SourceLabel { get; private set; } = "";

    /// <summary>Gets whether <see cref="SourceLabel"/> has anything to show.</summary>
    public bool HasSource => SourceLabel.Length > 0;

    /// <summary>Gets or sets whether the entry counts toward the totals.</summary>
    public bool Enabled
    {
        get => Entry.Enabled;
        set
        {
            if (Entry.Enabled != value)
            {
                Entry = Entry with { Enabled = value };
                OnPropertyChanged();
                OnPropertyChanged(nameof(StateText));
                _changed(this);
            }
        }
    }

    /// <summary>Gets "" while enabled, otherwise a note that the entry is excluded.</summary>
    public string StateText => Entry.Enabled ? "" : Strings.Get(Keys.Plan_ExcludedFromTotals);

    /// <summary>Gets the grades to choose from: 0 for experimental only, then every grade the blueprint has.</summary>
    public IReadOnlyList<int> Grades { get; }

    /// <summary>Gets or sets the target grade. Clamped to a grade the blueprint has, or 0 for experimental only.</summary>
    public int TargetGrade
    {
        get => Entry.TargetGrade;
        set
        {
            var next = Entry.WithTargetGrade(_catalog, value);
            if (next.TargetGrade != Entry.TargetGrade)
            {
                Entry = next;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExperimentalOnly));
                Refresh(_selected);
                _changed(this);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the entry wants only the experimental effect. Setting it true puts the grade at
    /// 0; setting it false puts the grade back at the blueprint's top grade. It reads true whenever the grade
    /// is 0.
    /// </summary>
    public bool ExperimentalOnly
    {
        get => Entry.IsExperimentalOnly;
        set
        {
            if (value != Entry.IsExperimentalOnly)
            {
                TargetGrade = value ? 0 : _catalog.Blueprints[Entry.Blueprint].MaxGrade;
            }
        }
    }

    /// <summary>Gets the experimentals the module type allows, "None" first.</summary>
    public IReadOnlyList<Choice> Experimentals { get; private set; } = [Choice.None];

    /// <summary>Gets whether there is an experimental to choose: the picker hides when "None" is the only option.</summary>
    public bool HasExperimentals => Experimentals.Count > 1;

    /// <summary>Gets or sets the chosen experimental. Must be one of <see cref="Experimentals"/>.</summary>
    public Choice SelectedExperimental
    {
        get => Experimentals.FirstOrDefault(choice => choice.Key == (Entry.Experimental ?? "")) ?? Choice.None;
        set
        {
            if (value is null)
            {
                return;
            }

            var key = value.Key.Length == 0 ? null : value.Key;
            if (!string.Equals(key, Entry.Experimental, StringComparison.Ordinal))
            {
                Entry = Entry with { Experimental = key };
                OnPropertyChanged();
                Refresh(_selected);
                _changed(this);
            }
        }
    }

    /// <summary>Gets the roll counts for every grade up to the target.</summary>
    public IReadOnlyList<RollViewModel> Rolls { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the materials this entry costs at its current settings.</summary>
    public IReadOnlyList<MaterialChip> Materials { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets who offers the target grade, each engineer a link in the tone the tick and the engineers switch give it; for an unlock, how it is bought.</summary>
    public IReadOnlyList<PhrasePart> CanRollParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets who can apply the experimental, each engineer a link in their tone; empty for an unlock.</summary>
    public IReadOnlyList<PhrasePart> CanApplyParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets whether an experimental is chosen.</summary>
    public bool HasExperimental => Entry.Experimental is not null;

    /// <summary>Gets a warning sentence, or empty.</summary>
    public string Warning { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether <see cref="Warning"/> has anything to show.</summary>
    public bool HasWarning => Warning.Length > 0;

    /// <summary>Gets the command that removes this entry.</summary>
    public RelayCommand RemoveCommand { get; }

    /// <summary>Takes a new version of the entry, such as one the game's engineering moved on, and re-reads every figure from it. Does not raise the plan's change; the plan does.</summary>
    /// <param name="entry">The entry, with the same id.</param>
    public void Replace(PlanEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
        Refresh(_selected);
        OnPropertyChanged(string.Empty);
    }

    /// <summary>Words the row again in the language in force: its labels, its experimental choices, the engineers and the warnings. Called after a language switch; raises a change for every property.</summary>
    public void Reword()
    {
        Word();
        Refresh(_selected);
        OnPropertyChanged(string.Empty);
    }

    private void Word()
    {
        var moduleType = Entry.ModuleType is { } code && _catalog.ModuleTypes.TryGetValue(code, out var type) ? type : null;
        if (Entry.Unlock is { } key)
        {
            var unlock = _catalog.Unlocks[key];
            ModuleLabel = moduleType is null ? Strings.Get(Keys.Plan_Fighter) : Names.ModuleType(moduleType);
            BlueprintLabel = Names.Unlock(unlock);
            SourceLabel = Pages.UnlocksPage.BrokerLabel(unlock.Broker);
            Experimentals = [Choice.None];
            CommoditiesText = unlock.Commodities.Count == 0
                ? ""
                : Strings.Format(Keys.Plan_AlsoFromMarket, ("items", Display.List(unlock.Commodities.Select(pair => Strings.Format(Keys.Material_CountTimesName, ("count", pair.Value), ("name", pair.Key))))));
            return;
        }

        ModuleLabel = moduleType is null
            ? Entry.Source.Item is { } item ? Strings.Format(Keys.Plan_UnknownModuleItem, ("item", item)) : Strings.Get(Keys.Plan_UnknownModule)
            : Names.ModuleType(moduleType);
        BlueprintLabel = Names.Blueprint(_catalog.Blueprints[Entry.Blueprint]);
        SourceLabel = Entry.Source.IsImported ? Strings.Format(Keys.Plan_FromShipSlot, ("ship", Entry.Source.Ship), ("slot", Core.Fleet.SlotNames.Display(Entry.Source.Slot ?? ""))) : "";
        CommoditiesText = "";
        Experimentals = moduleType is null
            ? [Choice.None]
            : [Choice.None, .. _catalog.ExperimentalsFor(moduleType.Code).Select(effect => new Choice(effect.Key, Names.Experimental(effect)))];
        if (Entry.Experimental is { } experimental && Experimentals.All(choice => choice.Key != experimental))
        {
            Experimentals = [.. Experimentals, new Choice(experimental, Names.Experimental(_catalog.Experimentals[experimental]))];
        }
    }

    /// <summary>Recomputes the engineers, warning and materials.</summary>
    /// <param name="selected">The current engineer selection.</param>
    public void Refresh(IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(selected);

        _selected = selected;
        var service = EngineerCoverage.ServiceFor(_catalog, Entry, selected);
        if (IsUnlock)
        {
            CanRollParts = Phrase.Text(Strings.Format(HasCommodities ? Keys.Plan_BoughtOnceWithCommodities : Keys.Plan_BoughtOnce, ("source", SourceLabel.ToLower(CultureInfo.CurrentCulture))));
            CanApplyParts = Phrase.Empty;
            Warning = "";
            Rolls = [];
            Materials = MaterialChip.From(_catalog, MaterialCalculator.ForEntry(_catalog, Entry));
            OnPropertyChanged(nameof(HasWarning));
            OnPropertyChanged(nameof(HasExperimental));
            return;
        }

        CanRollParts = Entry.IsExperimentalOnly
            ? Entry.Experimental is null
                ? Phrase.Text(Strings.Get(Keys.Plan_ExperimentalOnlyNoEffect))
                : Phrase.Format(Keys.Plan_ExperimentalOnlyBy, ("engineers", EngineerLink.Names(service.CanRoll.Select(_engineerLink), Strings.Get(Keys.Plan_NobodyCanApplyIt))))
            : EngineerLink.Names(service.CanRoll.Select(_engineerLink), Strings.Get(Keys.Plan_NobodyOffersGrade));
        CanApplyParts = EngineerLink.Names(service.CanApplyExperimental.Select(_engineerLink), Strings.Get(Keys.Plan_NobodyCanApply));
        Warning = WarningFor(service, selected);
        Rolls = [.. Entry.RolledGrades(_catalog).Select(grade => new RollViewModel(grade, Entry.RollsAt(grade), SetRolls))];
        Materials = MaterialChip.From(_catalog, MaterialCalculator.ForEntry(_catalog, Entry));
        OnPropertyChanged(nameof(HasWarning));
        OnPropertyChanged(nameof(HasExperimental));
    }

    private void SetRolls(int grade, int count)
    {
        Entry = Entry.WithRolls(grade, count);
        Materials = MaterialChip.From(_catalog, MaterialCalculator.ForEntry(_catalog, Entry));
        _changed(this);
    }

    private string WarningFor(EntryService service, IReadOnlySet<string> selected)
    {
        var warnings = new List<string>();
        if (Entry.IsExperimentalOnly && Entry.Experimental is null)
        {
            warnings.Add(Strings.Get(Keys.Plan_WarnNoEffect));
        }
        else if (Entry.ModuleType is null)
        {
            warnings.Add(Strings.Get(Keys.Plan_WarnUnknownType));
        }
        else if (Entry.IsExperimentalOnly)
        {
            // The experimental check below covers it.
        }
        else if (service.CanRoll.Count == 0)
        {
            warnings.Add(Strings.Get(Keys.Plan_WarnNobodyOffers));
        }
        else if (selected.Count > 0 && service.SelectedCanRoll.Count == 0)
        {
            warnings.Add(Strings.Format(Keys.Plan_WarnSelectedGrade, ("grade", Entry.TargetGrade)));
        }

        if (Entry.Experimental is not null && service.CanApplyExperimental.Count > 0 && selected.Count > 0 && service.SelectedCanApplyExperimental.Count == 0)
        {
            warnings.Add(Strings.Get(Keys.Plan_WarnSelectedEffect));
        }

        return string.Join(" ", warnings);
    }
}
