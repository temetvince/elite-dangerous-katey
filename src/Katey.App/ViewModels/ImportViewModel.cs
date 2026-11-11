using System.Collections.ObjectModel;
using Katey.App.Mvvm;
using Katey.App.Text;
using Katey.Core;
using Katey.Core.Edsy;
using Katey.Core.Planning;

namespace Katey.App.ViewModels;

/// <summary>One engineered module from a parsed export, with its include switch.</summary>
public sealed class ImportedModuleViewModel : ObservableObject
{
    private readonly Action _changed;

    /// <summary>Creates the row, included.</summary>
    /// <param name="module">The module.</param>
    /// <param name="catalog">The catalog, for display names.</param>
    /// <param name="changed">Called when the include switch flips.</param>
    public ImportedModuleViewModel(ImportedModule module, Catalog catalog, Action changed)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(changed);

        Module = module;
        _changed = changed;
        var moduleLabel = module.ModuleType is { } code && catalog.ModuleTypes.TryGetValue(code, out var type)
            ? Names.ModuleType(type)
            : Strings.Format(Keys.Plan_UnknownModuleItem, ("item", module.Item));
        var blueprint = catalog.Blueprints[module.Blueprint];
        Title = module.Experimental is { } key
            ? Strings.Format(Keys.Import_TitleWithExperimental, ("module", moduleLabel), ("blueprint", Names.Blueprint(blueprint)), ("grade", Display.Grade(module.Level)), ("experimental", Names.Experimental(catalog.Experimentals[key])))
            : Strings.Format(Keys.Import_Title, ("module", moduleLabel), ("blueprint", Names.Blueprint(blueprint)), ("grade", Display.Grade(module.Level)));

        var notes = new List<string>();
        if (module.ModuleType is null)
        {
            notes.Add(Strings.Get(Keys.Import_NoteUnknownType));
        }
        else if (!catalog.ModuleTypes[module.ModuleType].Blueprints.ContainsKey(module.Blueprint))
        {
            notes.Add(Strings.Get(Keys.Import_NoteNotListed));
        }

        if (module.Level > blueprint.MaxGrade)
        {
            notes.Add(Strings.Format(Keys.Import_NoteTopsOut, ("grade", blueprint.MaxGrade)));
        }

        Note = string.Join(" ", notes);
    }

    /// <summary>Gets the module.</summary>
    public ImportedModule Module { get; }

    /// <summary>Gets <c>module: blueprint Gn + experimental</c>.</summary>
    public string Title { get; }

    /// <summary>Gets the slot name with spaces, such as "Large Hardpoint 3".</summary>
    public string Slot => Core.Fleet.SlotNames.Display(Module.Slot);

    /// <summary>Gets a caveat, or empty.</summary>
    public string Note { get; }

    /// <summary>Gets whether <see cref="Note"/> has anything to show.</summary>
    public bool HasNote => Note.Length > 0;

    /// <summary>Gets or sets whether the module will be added.</summary>
    public bool Include
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _changed();
            }
        }
    } = true;
}

/// <summary>Paste-and-import for an EDSY journal Loadout export.</summary>
public sealed class ImportViewModel : ObservableObject
{
    private readonly Catalog _catalog;
    private readonly EdsyImport _import;
    private readonly Action<IReadOnlyList<PlanEntry>, string> _add;
    private ParsedLoadout? _loadout;

    /// <summary>Creates the form.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="add">Receives the entries and the ship name when the user adds them.</param>
    public ImportViewModel(Catalog catalog, Action<IReadOnlyList<PlanEntry>, string> add)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(add);

        _catalog = catalog;
        _import = new EdsyImport(catalog);
        _add = add;
        ReadCommand = new RelayCommand(Read, () => Text.Trim().Length > 0);
        AddCommand = new RelayCommand(Add, () => IncludedCount > 0);
    }

    /// <summary>Gets or sets the pasted export text.</summary>
    public string Text
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ReadCommand.RaiseCanExecuteChanged();
            }
        }
    } = "";

    /// <summary>Gets why the last read failed, or empty.</summary>
    public string Error { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether <see cref="Error"/> has anything to show.</summary>
    public bool HasError => Error.Length > 0;

    /// <summary>Gets whether a build has been read.</summary>
    public bool HasPreview => _loadout is not null;

    /// <summary>Gets the ship and module count, such as "Borrowed Time: 11 engineered modules".</summary>
    public string Heading { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the engineered modules read, in export order.</summary>
    public ObservableCollection<ImportedModuleViewModel> Modules { get; } = [];

    /// <summary>Gets whether the build had no engineered modules.</summary>
    public bool IsBuildEmpty => _loadout is not null && Modules.Count == 0;

    /// <summary>Gets notes about skipped or incomplete modules.</summary>
    public IReadOnlyList<string> Warnings { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether there are notes.</summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>Gets how many modules are ticked.</summary>
    public int IncludedCount => Modules.Count(module => module.Include);

    /// <summary>The sentence for a parse failure.</summary>
    /// <param name="error">The failure, or <see langword="null"/> when the cause is not known.</param>
    /// <returns>The sentence.</returns>
    public static string ImportErrorText(EdsyParseError? error) => error switch
    {
        EdsyParseError.Empty => Strings.Get(Keys.Import_PasteFirst),
        EdsyParseError.NotJson => Strings.Get(Keys.Import_NotJson),
        EdsyParseError.NoLoadout => Strings.Get(Keys.Import_NoLoadout),
        EdsyParseError.NoModules => Strings.Get(Keys.Import_NoModules),
        _ => Strings.Get(Keys.Import_CouldNotRead),
    };

    /// <summary>Gets the add button's text.</summary>
    public string AddLabel => Strings.Format(Keys.Import_AddToPlan, ("count", IncludedCount));

    /// <summary>Gets the command that parses <see cref="Text"/>.</summary>
    public RelayCommand ReadCommand { get; }

    /// <summary>Gets the command that adds the ticked modules.</summary>
    public RelayCommand AddCommand { get; }

    private void Read()
    {
        var result = _import.Parse(Text);
        Modules.Clear();
        if (!result.Ok)
        {
            _loadout = null;
            Error = ImportErrorText(result.Error);
            Heading = "";
            Warnings = [];
        }
        else
        {
            _loadout = result.Loadout;
            Error = "";
            var count = _loadout!.Modules.Count;
            Heading = Strings.Format(Keys.Import_Heading, ("ship", _loadout.ShipName), ("modules", Display.Plural(Keys.Plural_EngineeredModules, count)));
            foreach (var module in _loadout.Modules)
            {
                Modules.Add(new ImportedModuleViewModel(module, _catalog, OnIncludeChanged));
            }

            Warnings = _loadout.Warnings;
        }

        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(IsBuildEmpty));
        OnPropertyChanged(nameof(HasWarnings));
        OnIncludeChanged();
    }

    private void OnIncludeChanged()
    {
        OnPropertyChanged(nameof(IncludedCount));
        OnPropertyChanged(nameof(AddLabel));
        AddCommand.RaiseCanExecuteChanged();
    }

    private void Add()
    {
        if (_loadout is null)
        {
            return;
        }

        var entries = Modules
            .Where(row => row.Include)
            .Select(row => PlanEntry.Create(
                _catalog,
                row.Module.ModuleType,
                row.Module.Blueprint,
                row.Module.Level,
                row.Module.Experimental,
                new PlanSource(_loadout.ShipName, row.Module.Slot, row.Module.Item)))
            .ToList();
        _add(entries, _loadout.ShipName);
    }
}
