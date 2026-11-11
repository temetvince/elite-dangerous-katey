using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Edsy;
using Katey.Core.Fleet;
using Katey.Core.Planning;
using Katey.Journal;

namespace Katey.App.Pages;

/// <summary>
/// One module fitted to a ship, with a picker to plan engineering for it: blueprint, grade and
/// experimental, starting from what the module already carries so an experimental alone is one click away.
/// </summary>
public sealed class FittedModuleRow : ObservableObject
{
    private readonly Catalog _catalog;
    private readonly EngineerScope _scope;
    private readonly Action<PlanEntry> _add;
    private readonly Func<PlanEntry, bool> _isPlanned;
    private readonly string _item;
    private readonly string _shipTitle;
    private readonly ModuleType? _type;
    private readonly Engineering? _current;

    /// <summary>Creates the row.</summary>
    /// <param name="catalog">The catalog, for blueprints and experimentals.</param>
    /// <param name="module">The fitted module.</param>
    /// <param name="name">Module display name.</param>
    /// <param name="type">Its module type, or <see langword="null"/> when unrecognised.</param>
    /// <param name="link">The module type's page, or <see langword="null"/>.</param>
    /// <param name="engineeringText">Its engineering as text, or "stock".</param>
    /// <param name="shipTitle">The ship, for the plan entry's source.</param>
    /// <param name="add">Receives the entry when the user adds one.</param>
    /// <param name="scope">What the picker may offer: everything, or only what the selected engineers can roll.</param>
    /// <param name="isPrebuilt">Whether the module came with a prebuilt ship and stays with it.</param>
    /// <param name="isPlanned">Says whether the plan already holds an entry for the same work, so the add button can stand down.</param>
    public FittedModuleRow(Catalog catalog, FitModule module, string name, ModuleType? type, Link? link, string engineeringText, string shipTitle, Action<PlanEntry> add, EngineerScope scope, bool isPrebuilt = false, Func<PlanEntry, bool>? isPlanned = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(engineeringText);
        ArgumentNullException.ThrowIfNull(shipTitle);
        ArgumentNullException.ThrowIfNull(add);

        _catalog = catalog;
        _add = add;
        _isPlanned = isPlanned ?? (_ => false);
        _scope = scope;
        Slot = module.Slot;
        _item = module.Item;
        _shipTitle = shipTitle;
        _type = type;
        _current = module.Engineering;
        Name = name;
        Link = link;
        Engineering = engineeringText;
        IsEngineered = module.Engineering is not null;
        IsPrebuilt = isPrebuilt;
        Blueprints = type is null ? [] : [.. scope.Blueprints(type).Select(blueprint => new Choice(blueprint.Key, Names.Blueprint(blueprint)))];
        Experimentals = type is null ? [Choice.None] : [Choice.None, .. scope.Experimentals(type).Select(effect => new Choice(effect.Key, Names.Experimental(effect)))];
        AddCommand = new RelayCommand(Add, () => CanPlan && SelectedBlueprint is not null && !IsPlanned && !IsAlreadyDone);

        var current = module.Engineering;
        var first = Blueprints.Count > 0 ? Blueprints[0] : null;
        SelectedBlueprint = current is not null ? Blueprints.FirstOrDefault(choice => choice.Key == current.Blueprint) ?? first : first;
        SelectedExperimental = current?.Experimental is { } effect ? Experimentals.FirstOrDefault(choice => choice.Key == effect) ?? Choice.None : Choice.None;
        SelectedGrade = current is not null && SelectedBlueprint?.Key == current.Blueprint
            ? (current.Experimental is null && Experimentals.Count > 1 ? 0 : current.Level)
            : SelectedBlueprint is null ? 0 : StartingGrade(SelectedBlueprint.Key);
        if (SelectedBlueprint is not null && !Grades.Contains(SelectedGrade))
        {
            SelectedGrade = StartingGrade(SelectedBlueprint.Key);
        }
    }

    /// <summary>Gets the slot name as the journal spells it.</summary>
    public string Slot { get; }

    /// <summary>Gets the slot name with spaces, such as "Large Hardpoint 3".</summary>
    public string SlotText => SlotNames.Display(Slot);

    /// <summary>Gets the slot's group, for colouring by place on the ship, or <see langword="null"/> for a slot outside the four groups.</summary>
    public ModuleGroup? Group => Display.GroupOf(Slot);

    /// <summary>Gets whether the slot has a group colour.</summary>
    public bool HasGroup => Group is not null;

    /// <summary>Gets the module name.</summary>
    public string Name { get; }

    /// <summary>Gets the module type's page, or <see langword="null"/> for an unrecognised item.</summary>
    public Link? Link { get; }

    /// <summary>Gets whether the module type is known.</summary>
    public bool HasLink => Link is not null;

    /// <summary>Gets the engineering as text, or "stock".</summary>
    public string Engineering { get; }

    /// <summary>Gets whether it carries engineering.</summary>
    public bool IsEngineered { get; }

    /// <summary>Gets whether it came with a prebuilt ship. Such a module cannot be stored, moved to another ship or sold, so engineering it stays with the ship.</summary>
    public bool IsPrebuilt { get; }

    /// <summary>Gets whether to warn that engineering this module keeps it on this ship: it is prebuilt and the picker offers to plan it.</summary>
    public bool WarnPrebuilt => IsPrebuilt && CanPlan;

    /// <summary>Gets whether the type can be engineered, which is when the picker shows.</summary>
    public bool CanPlan => _type is { IsEngineerable: true };

    /// <summary>Gets whether the picker has a blueprint to offer, which is when it is shown. False when narrowed to engineers who offer nothing for this type.</summary>
    public bool ShowsPicker => CanPlan && Blueprints.Count > 0;

    /// <summary>Gets whether to say, in the picker's place, that the ticked engineers offer nothing for this type.</summary>
    public bool ShowsNoChoices => CanPlan && Blueprints.Count == 0;

    /// <summary>Gets whether the plan already holds what the picker currently describes for this slot, so adding it again would repeat the job.</summary>
    public bool IsPlanned => Candidate() is { } entry && _isPlanned(entry);

    /// <summary>
    /// Gets whether the module already has what the picker describes: the same blueprint at a complete grade no
    /// lower than the one chosen, and the chosen experimental or none chosen. Adding that would plan a job that is
    /// done, so the button stands down. A higher grade, another blueprint or another experimental is a job.
    /// </summary>
    public bool IsAlreadyDone
    {
        get
        {
            if (_current is not { } current || SelectedBlueprint is null || !string.Equals(SelectedBlueprint.Key, current.Blueprint, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var gradeDone = SelectedGrade == 0 || SelectedGrade < current.Level || (SelectedGrade == current.Level && current.IsComplete);
            var experimentalDone = SelectedExperimental.Key.Length == 0 || string.Equals(SelectedExperimental.Key, current.Experimental, StringComparison.OrdinalIgnoreCase);
            return gradeDone && experimentalDone;
        }
    }

    /// <summary>Gets the add button's label: "Add to plan", "In the plan" when the picker's choice is already planned, or "Already engineered" when the module already has it.</summary>
    public string AddText => IsPlanned ? Strings.Get(Keys.Ship_InThePlan) : IsAlreadyDone ? Strings.Get(Keys.Ship_AlreadyEngineered) : Strings.Get(Keys.Ship_AddToPlan);

    /// <summary>Gets the blueprints the type allows.</summary>
    public IReadOnlyList<Choice> Blueprints { get; }

    /// <summary>Gets or sets the blueprint to plan. Starts on the module's own.</summary>
    public Choice? SelectedBlueprint
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                Grades = [0, .. _type is null ? _catalog.Blueprints[value.Key].GradeNumbers : _scope.Grades(_type, value.Key)];
                if (!Grades.Contains(SelectedGrade))
                {
                    SelectedGrade = StartingGrade(value.Key);
                }

                PlannedChanged();
            }
        }
    }

    /// <summary>Gets the grades to choose from: 0 for experimental only, then every grade the blueprint has, or only up to the reach when narrowed to the selected engineers.</summary>
    public IReadOnlyList<int> Grades { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets or sets the grade to plan. Starts at 0 when the module has the blueprint but no experimental, else at its grade, else at the top, or at the grade the selected engineers reach when narrowed to them.</summary>
    public int SelectedGrade
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                PlannedChanged();
            }
        }
    }

    /// <summary>Gets the experimental effects the type allows, "None" first.</summary>
    public IReadOnlyList<Choice> Experimentals { get; }

    /// <summary>Gets whether there is an experimental to choose: the picker hides when "None" is the only option.</summary>
    public bool HasExperimentals => Experimentals.Count > 1;

    /// <summary>Gets or sets the experimental to plan. Starts on the module's own.</summary>
    public Choice SelectedExperimental
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                PlannedChanged();
            }
        }
    } = Choice.None;

    /// <summary>Gets the command that adds the chosen engineering to the plan, sourced to this ship and slot.</summary>
    public RelayCommand AddCommand { get; }

    private int StartingGrade(string blueprint)
        => _type is null ? _catalog.Blueprints[blueprint].MaxGrade : _scope.StartingGrade(_type, blueprint);

    private void PlannedChanged()
    {
        OnPropertyChanged(nameof(IsPlanned));
        OnPropertyChanged(nameof(IsAlreadyDone));
        OnPropertyChanged(nameof(AddText));
        AddCommand?.RaiseCanExecuteChanged();
    }

    /// <summary>The entry the picker currently describes, or <see langword="null"/> when the type cannot be engineered or no blueprint is chosen.</summary>
    private PlanEntry? Candidate()
        => _type is null || SelectedBlueprint is null
            ? null
            : PlanEntry.Create(
                _catalog,
                _type.Code,
                SelectedBlueprint.Key,
                SelectedGrade,
                SelectedExperimental.Key.Length == 0 ? null : SelectedExperimental.Key,
                new PlanSource(_shipTitle, Slot, _item));

    private void Add()
    {
        if (Candidate() is { } entry)
        {
            _add(entry);
        }
    }
}

/// <summary>How a prebuilt ship stands against the fit it was redeemed with.</summary>
public enum PrebuiltTone
{
    /// <summary>The fit as redeemed has not been seen yet.</summary>
    Unknown,

    /// <summary>Every slot is as it came.</summary>
    Unchanged,

    /// <summary>At least one slot differs.</summary>
    Changed,
}

/// <summary>The prebuilt ship's line in parts, so the view can colour each: the lead, the state, and the rest of the sentence.</summary>
/// <param name="Lead">"Prebuilt ship".</param>
/// <param name="State">How it stands: Redeemed from a bundle, Still as redeemed, or Changed since redemption.</param>
/// <param name="Detail">The rest of the sentence, starting with its punctuation.</param>
/// <param name="Tone">Whether the state is good news, bad news, or not yet known.</param>
public sealed record PrebuiltSummary(string Lead, string State, string Detail, PrebuiltTone Tone)
{
    /// <summary>Gets the whole line as one sentence.</summary>
    public string Text => Strings.Format(Keys.Ship_PrebuiltLine, ("lead", Lead), ("state", State), ("detail", Detail));

    /// <summary>Gets whether a slot differs from the fit as redeemed.</summary>
    public bool IsChanged => Tone == PrebuiltTone.Changed;

    /// <summary>Gets whether every slot is as it came.</summary>
    public bool IsUnchanged => Tone == PrebuiltTone.Unchanged;
}

/// <summary>One slot of a prebuilt ship that no longer holds what it came with.</summary>
/// <param name="Slot">The slot name with spaces, such as "Main Engines".</param>
/// <param name="Before">What it came with, as module and engineering, or "empty".</param>
/// <param name="After">What it holds now, likewise.</param>
public sealed record PrebuiltChangeRow(string Slot, string Before, string After);

/// <summary>One owned ship: where it is, what is fitted to it, a way to plan engineering for any module on it, and a copy for EDSY.</summary>
public sealed class ShipPage : Page, IEdsyExportable
{
    private const string PrebuiltChangesKey = "ship.prebuiltChanges";

    private readonly int _shipId;

    /// <summary>Creates the page.</summary>
    /// <param name="shipId">The game's ship id, as text. Must name a ship in the fleet.</param>
    /// <param name="workspace">The shared state.</param>
    public ShipPage(string shipId, Workspace workspace)
        : base(new PageKey(PageKind.Ship, shipId), workspace)
    {
        _shipId = int.Parse(shipId, CultureInfo.InvariantCulture);
        Kicker = Strings.Get(Keys.Ship_Kicker);
        Breadcrumbs = [LinkTo(Strings.Get(Keys.Nav_Fleet), PageKind.Fleet)];
        Sort = new TableSort<FittedModuleRow>(
            [
                new("slot", Strings.Get(Keys.Column_Slot), row => row.Slot),
                new("name", Strings.Get(Keys.Column_Module), row => row.Name),
                new("engineering", Strings.Get(Keys.Column_Engineering), row => row.Engineering),
            ],
            "slot",
            workspace.Preferences,
            "sort.shipModules");
        Sort.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Gets the module table's sort. The default, by slot, is the journal's order.</summary>
    public TableSort<FittedModuleRow> Sort { get; }

    /// <summary>Gets the hull display name.</summary>
    public string TypeName { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets where the ship is: the system's link and the station, or a state such as "in transit".</summary>
    public Place Where { get; private set => SetProperty(ref field, value); } = Place.Only("");

    /// <summary>Gets whether you are flying it.</summary>
    public bool IsCurrent { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets whether the journal saw it redeemed from a bundle.</summary>
    public bool IsPrebuilt { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets one line saying the pickers are narrowed to the selected engineers, or empty.</summary>
    public string ScopeNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets how fresh the fit is.</summary>
    public string FitText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>
    /// Gets, for a ship redeemed from a bundle, whether it is still as it came, so selling it and redeeming it
    /// again loses nothing, or how many slots have changed since. <see langword="null"/> for a ship that was bought.
    /// </summary>
    public PrebuiltSummary? Prebuilt { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets <see cref="Prebuilt"/> as one sentence, or empty.</summary>
    public string PrebuiltText => Prebuilt?.Text ?? "";

    /// <summary>Gets whether there is a prebuilt line to show.</summary>
    public bool HasPrebuiltText => Prebuilt is not null;

    /// <summary>Gets whether the prebuilt line stands alone, with no changed slots to open.</summary>
    public bool HasPrebuiltLineAlone => Prebuilt is not null && PrebuiltChanges.Count == 0;

    /// <summary>Gets, for a changed prebuilt ship, one row per changed slot: what it came with and what is there now.</summary>
    public IReadOnlyList<PrebuiltChangeRow> PrebuiltChanges { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether there are changed slots to list, which is when the prebuilt line opens into them.</summary>
    public bool HasPrebuiltChanges => PrebuiltChanges.Count > 0;

    /// <summary>Gets or sets whether the changed-slot list is open. Remembered; closed until opened once.</summary>
    public bool ShowPrebuiltChanges
    {
        get => Workspace.Preferences.GetBool(PrebuiltChangesKey, false);
        set
        {
            if (value != ShowPrebuiltChanges)
            {
                Workspace.Preferences.SetBool(PrebuiltChangesKey, value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets the fitted modules in sort order. Cosmetic items are left out.</summary>
    public IReadOnlyList<FittedModuleRow> Modules { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a count line such as "28 modules, 9 engineered".</summary>
    public string ModulesText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets saved fits for this hull.</summary>
    public IReadOnlyList<Link> Fits { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether any saved fit is for this hull.</summary>
    public bool HasFits => Fits.Count > 0;

    /// <inheritdoc/>
    public string EdsyJson { get; private set => SetProperty(ref field, value); } = "";

    /// <inheritdoc/>
    public string ExportStatus { get; set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether there is a loadout to copy.</summary>
    public bool CanExport => EdsyJson.Length > 0;

    /// <summary>One line on a prebuilt ship, in parts: as redeemed, or how far it has drifted from that.</summary>
    /// <param name="ship">The ship.</param>
    /// <param name="changed">How many slots differ from the fit as redeemed, from <see cref="PrebuiltChangeRows"/>.</param>
    /// <returns>The line, or <see langword="null"/> for a ship that was bought.</returns>
    public static PrebuiltSummary? PrebuiltLine(OwnedShip ship, int changed)
    {
        ArgumentNullException.ThrowIfNull(ship);
        return !ship.IsPrebuilt
            ? null
            : ship.RedeemedModules is null
            ? new PrebuiltSummary(Strings.Get(Keys.Ship_PrebuiltLead), Strings.Get(Keys.Ship_PrebuiltUnknown), Strings.Get(Keys.Ship_PrebuiltUnknownDetail), PrebuiltTone.Unknown)
            : changed == 0
            ? new PrebuiltSummary(Strings.Get(Keys.Ship_PrebuiltLead), Strings.Get(Keys.Ship_PrebuiltUnchanged), Strings.Get(Keys.Ship_PrebuiltUnchangedDetail), PrebuiltTone.Unchanged)
            : new PrebuiltSummary(Strings.Get(Keys.Ship_PrebuiltLead), Strings.Get(Keys.Ship_PrebuiltChanged), Strings.Plural(Keys.Ship_PrebuiltChangedDetail, changed), PrebuiltTone.Changed);
    }

    /// <summary>
    /// What has changed on a prebuilt ship since it was redeemed, slot by slot: the module and engineering
    /// it came with, then what is there now. A prebuilt module replaced by a bought one of the same kind
    /// counts, since the new one can leave the ship. Cosmetic items do not count.
    /// </summary>
    /// <param name="workspace">The shared state, for module names.</param>
    /// <param name="ship">The ship.</param>
    /// <returns>One row per changed slot in journal order, or empty when nothing changed or the ship is not prebuilt.</returns>
    public static IReadOnlyList<PrebuiltChangeRow> PrebuiltChangeRows(Workspace workspace, OwnedShip ship)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(ship);
        if (!ship.IsPrebuilt || ship.RedeemedModules is null)
        {
            return [];
        }

        var now = FleetState.FittedModules(ship);
        var then = FleetState.FittedModules(ship with { Modules = ship.RedeemedModules });
        var before = then.ToDictionary(module => module.Slot, StringComparer.OrdinalIgnoreCase);
        var prebuiltThen = ship.RedeemedModules.Where(module => module.IsPrebuilt).Select(module => module.Slot).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var prebuiltNow = ship.Modules.Where(module => module.IsPrebuilt).Select(module => module.Slot).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = new List<PrebuiltChangeRow>();
        foreach (var module in now)
        {
            if (!before.TryGetValue(module.Slot, out var was))
            {
                rows.Add(new PrebuiltChangeRow(SlotNames.Display(module.Slot), Strings.Get(Keys.Ship_Empty), Describe(workspace, module)));
            }
            else if (!string.Equals(was.Item, module.Item, StringComparison.OrdinalIgnoreCase) || was.Engineering != module.Engineering)
            {
                rows.Add(new PrebuiltChangeRow(SlotNames.Display(module.Slot), Describe(workspace, was), Describe(workspace, module)));
            }
            else if (prebuiltThen.Contains(module.Slot) && !prebuiltNow.Contains(module.Slot))
            {
                rows.Add(new PrebuiltChangeRow(SlotNames.Display(module.Slot), Strings.Format(Keys.Ship_WasPrebuilt, ("module", Describe(workspace, was))), Strings.Format(Keys.Ship_NowBought, ("module", Describe(workspace, module)))));
            }
        }

        foreach (var module in then.Where(module => !now.Any(current => string.Equals(current.Slot, module.Slot, StringComparison.OrdinalIgnoreCase))))
        {
            rows.Add(new PrebuiltChangeRow(SlotNames.Display(module.Slot), Describe(workspace, module), Strings.Get(Keys.Ship_Empty)));
        }

        return rows;
    }

    private static string Describe(Workspace workspace, FitModule module)
        => Strings.Format(Keys.Ship_ModuleAndEngineering, ("module", workspace.ModuleName(module.Item)), ("engineering", FleetPage.EngineeringText(workspace, module.Engineering)));

    /// <summary>Finds the ship, or <see langword="null"/> when the fleet does not have it.</summary>
    /// <param name="fleet">The fleet.</param>
    /// <param name="shipId">The id as text.</param>
    /// <returns>The ship or <see langword="null"/>.</returns>
    public static OwnedShip? Find(FleetSnapshot fleet, string shipId)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        return int.TryParse(shipId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? fleet.Ships.FirstOrDefault(ship => ship.ShipId == id)
            : null;
    }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var fleet = Workspace.FleetSnapshot;
        var ship = fleet.Ships.FirstOrDefault(ship => ship.ShipId == _shipId);
        if (ship is null)
        {
            Title = Strings.Get(Keys.Ship_SoldOrUnknown);
            Prebuilt = null;
            PrebuiltChanges = [];
            OnPropertyChanged(nameof(PrebuiltText));
            OnPropertyChanged(nameof(HasPrebuiltText));
            OnPropertyChanged(nameof(HasPrebuiltLineAlone));
            OnPropertyChanged(nameof(HasPrebuiltChanges));
            Modules = [];
            EdsyJson = "";
            OnPropertyChanged(nameof(CanExport));
            return;
        }

        var title = FleetPage.ShipTitle(ship);
        // A fit for this hull may have planned the same slot's engineering under its own name; that is this ship's job too.
        var fitNames = Workspace.Fleet.Fittings.Where(fit => string.Equals(fit.Hull, ship.Type, StringComparison.OrdinalIgnoreCase)).Select(fit => fit.Name).ToList();
        bool IsPlanned(PlanEntry entry) => Workspace.Plan.Contains(entry) || fitNames.Any(name => Workspace.Plan.Contains(entry with { Source = entry.Source with { Ship = name } }));
        Title = title;
        TypeName = ShipNames.Prefer(ship.Type, ship.TypeName);
        IsCurrent = ship.IsCurrent;
        IsPrebuilt = ship.IsPrebuilt;
        Where = ship.InTransit
            ? Place.Only(Strings.Get(Keys.Fleet_InTransit))
            : ship.System is null
                ? Place.Only(Strings.Get(Keys.Fleet_LocationUnknown))
                : new Place(LinkTo(ship.System, PageKind.System, ship.System), ship.Station ?? "");
        FitText = ship.FitAsOf is { } asOf ? Strings.Format(Keys.Ship_FitAsOf, ("when", asOf.ToLocalTime())) : Strings.Get(Keys.Ship_FitUnknown);
        ScopeNote = Workspace.Scope.Note;
        var prebuiltSlots = ship.Modules.Where(module => module.IsPrebuilt).Select(module => module.Slot).ToHashSet(StringComparer.OrdinalIgnoreCase);
        PrebuiltChanges = PrebuiltChangeRows(Workspace, ship);
        Prebuilt = PrebuiltLine(ship, PrebuiltChanges.Count);
        OnPropertyChanged(nameof(PrebuiltText));
        OnPropertyChanged(nameof(HasPrebuiltText));
        OnPropertyChanged(nameof(HasPrebuiltLineAlone));
        OnPropertyChanged(nameof(HasPrebuiltChanges));
        var modules = FleetState.FittedModules(ship);
        Modules = Sort.Apply(modules.Select(module =>
        {
            var code = Workspace.Modules.Find(module.Item)?.Group;
            var type = code is not null && Workspace.Catalog.ModuleTypes.TryGetValue(code, out var found) ? found : null;
            return new FittedModuleRow(
                Workspace.Catalog,
                module,
                Workspace.ModuleName(module.Item),
                type,
                type is null ? null : LinkTo(Names.ModuleType(type), PageKind.ModuleType, type.Code),
                FleetPage.EngineeringText(Workspace, module.Engineering),
                title,
                entry => Workspace.Plan.Add([entry]),
                Workspace.Scope,
                prebuiltSlots.Contains(module.Slot),
                IsPlanned);
        }));
        ModulesText = modules.Count == 0 ? "" : Strings.Format(Keys.Ship_ModulesText, ("modules", Display.Plural(Keys.Plural_Modules, modules.Count)), ("engineered", modules.Count(module => module.Engineering is not null)));
        Fits = [.. Workspace.Fleet.Fittings.Where(fit => string.Equals(fit.Hull, ship.Type, StringComparison.OrdinalIgnoreCase)).Select(fit => LinkTo(fit.Name, PageKind.Fit, fit.Id))];
        OnPropertyChanged(nameof(HasFits));
        EdsyJson = ship.LoadoutJson ?? (modules.Count == 0 ? "" : EdsyExport.LoadoutJson(ship.Type, ship.Name, ship.Ident, modules));
        OnPropertyChanged(nameof(CanExport));
    }
}
