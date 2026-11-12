using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Fleet;
using Katey.Core.Planning;
using Katey.Journal;

namespace Katey.App.Pages;

/// <summary>One required module in a fit report.</summary>
/// <param name="Slot">Slot name.</param>
/// <param name="Required">Module name and engineering.</param>
/// <param name="Link">The module type's page, or <see langword="null"/>.</param>
/// <param name="Status">Where it was found, or "missing".</param>
/// <param name="Quality">How well it matched.</param>
/// <param name="Tone">The colour the status takes.</param>
public sealed record FitLineRow(string Slot, string Required, Link? Link, string Status, MatchQuality Quality, StatusTone Tone)
{
    /// <summary>Gets whether an owned module satisfies the slot in full.</summary>
    public bool IsReady => Quality == MatchQuality.Exact;

    /// <summary>Gets whether the status is green: fitted to a ship of the fit's hull.</summary>
    public bool IsFitted => Tone == StatusTone.Fitted;

    /// <summary>Gets whether the status is blue: in storage.</summary>
    public bool IsStored => Tone == StatusTone.Stored;

    /// <summary>Gets whether the status is yellow: fitted to a ship of another hull.</summary>
    public bool IsElsewhere => Tone == StatusTone.Elsewhere;

    /// <summary>Gets whether the status is orange: owned but needing a change, or in doubt.</summary>
    public bool IsChange => Tone == StatusTone.Change;

    /// <summary>Gets the slot's group, for colouring by place on the ship, or <see langword="null"/> for a slot outside the four groups.</summary>
    public ModuleGroup? Group => Display.GroupOf(Slot);

    /// <summary>Gets whether the slot has a group colour.</summary>
    public bool HasGroup => Group is not null;

    /// <summary>Gets the slot name with spaces, such as "Large Hardpoint 3".</summary>
    public string SlotText => SlotNames.Display(Slot);

    /// <summary>Gets whether nothing suitable is owned.</summary>
    public bool IsMissing => Quality == MatchQuality.Missing;

    /// <summary>Gets whether the match rests on an unknown experimental.</summary>
    public bool IsUncertain => Quality == MatchQuality.ExperimentalUnknown;

    /// <summary>Gets whether a module is owned but not engineered all the way.</summary>
    public bool IsPartial => Quality == MatchQuality.Partial;

    /// <summary>Gets whether the row deserves a caution: partial, or resting on an unknown experimental.</summary>
    public bool IsWarn => IsPartial || IsUncertain;

    /// <summary>Gets whether the module type is known.</summary>
    public bool HasLink => Link is not null;
}

/// <summary>One slot where the chosen ship differs from the fit.</summary>
/// <param name="Slot">Slot name.</param>
/// <param name="Required">What the fit wants there.</param>
/// <param name="Fitted">What the ship has there, or "empty".</param>
/// <param name="KindText">How it falls short, in words.</param>
/// <param name="NeedsEngineering">Whether closing it is engineering work the plan can hold.</param>
/// <param name="NeedsPurchase">Whether closing it means buying the module.</param>
/// <param name="IsUncertain">Whether the ship's experimental is unknown, so the slot may already be fine.</param>
/// <param name="EngineersParts">Who can do the engineering, each engineer a link in the tone the tick and the engineers switch give it, or empty when none is needed.</param>
/// <param name="CoveredBySelection">Whether your selected engineers can do all of the engineering. False with no selection.</param>
/// <param name="OwnedText">A module you already own elsewhere that satisfies the slot, and where it is, or empty.</param>
/// <param name="OwnedCovers">Whether an owned module satisfies the slot for certain, so no engineering is planned.</param>
/// <param name="FittedIsPrebuilt">Whether the ship's module in the slot came with a prebuilt ship, so engineering it stays with the ship.</param>
public sealed record DifferenceRow(
    string Slot,
    string Required,
    string Fitted,
    string KindText,
    bool NeedsEngineering,
    bool NeedsPurchase,
    bool IsUncertain,
    IReadOnlyList<PhrasePart> EngineersParts,
    bool CoveredBySelection,
    string OwnedText,
    bool OwnedCovers,
    bool FittedIsPrebuilt = false)
{
    /// <summary>Gets whether to warn that the planned engineering lands on a prebuilt module, which cannot leave this ship.</summary>
    public bool WarnPrebuilt => FittedIsPrebuilt && NeedsEngineering;

    /// <summary>Gets the slot's group, for colouring by place on the ship, or <see langword="null"/> for a slot outside the four groups.</summary>
    public ModuleGroup? Group => Display.GroupOf(Slot);

    /// <summary>Gets whether the slot has a group colour.</summary>
    public bool HasGroup => Group is not null;

    /// <summary>Gets the slot name with spaces, such as "Large Hardpoint 3".</summary>
    public string SlotText => SlotNames.Display(Slot);

    /// <summary>Gets whether there is an engineers line to show.</summary>
    public bool HasEngineers => EngineersParts.Count > 0;

    /// <summary>Gets whether there is an owned-module line to show.</summary>
    public bool HasOwned => OwnedText.Length > 0;
}

/// <summary>
/// One saved fit: what it needs against everything you own, and, slot by slot, how one of your ships
/// differs from it, with a button that puts the engineering gap into the plan.
/// </summary>
public sealed class FitPage : Page, IEdsyExportable
{
    private const string UseOwnedKey = "fit.useOwned";

    private readonly string _id;
    private Choice? _selectedShip;
    private IReadOnlyList<FitDifference> _differences = [];
    private HashSet<string> _ownedSlots = new(StringComparer.OrdinalIgnoreCase);
    private string _comparedShipTitle = "";

    /// <summary>Creates the page.</summary>
    /// <param name="id">The fit's id. Must be a saved fit.</param>
    /// <param name="workspace">The shared state.</param>
    public FitPage(string id, Workspace workspace)
        : base(new PageKey(PageKind.Fit, id), workspace)
    {
        _id = id;
        Kicker = Strings.Get(Keys.Fit_Kicker);
        Breadcrumbs = [LinkTo(Strings.Get(Keys.Nav_Fleet), PageKind.Fleet)];
        Sections = new SectionSwitch([("compare", Strings.Get(Keys.Section_CompareWithShip)), ("owned", Strings.Get(Keys.Section_AgainstEverythingOwned))], "compare", workspace.Preferences, "section.fit");
        LineSort = new TableSort<FitLineRow>(
            [
                new("slot", Strings.Get(Keys.Column_Slot), row => row.Slot),
                new("required", Strings.Get(Keys.Column_Required), row => row.Required),
                new("status", Strings.Get(Keys.Column_Status), row => row.Status),
            ],
            "slot",
            workspace.Preferences,
            "sort.fitLines");
        DifferenceSort = new TableSort<DifferenceRow>(
            [
                new("slot", Strings.Get(Keys.Column_Slot), row => row.Slot),
                new("required", Strings.Get(Keys.Column_FitWants), row => row.Required),
                new("fitted", Strings.Get(Keys.Column_ShipHas), row => row.Fitted),
                new("kind", Strings.Get(Keys.Column_Difference), row => row.KindText),
            ],
            "slot",
            workspace.Preferences,
            "sort.fitDifferences");
        LineSort.Changed += (_, _) => Refresh();
        DifferenceSort.Changed += (_, _) => RefreshComparison();
        SelectedPlace = FleetChoices.Places.First(choice => choice.Key == workspace.Fleet.Place.ToString());
        RemoveCommand = new RelayCommand(() => workspace.Fleet.Remove(_id));
        PlanMissingCommand = new RelayCommand(PlanMissing, () => MissingEngineered > 0);
        PlanDifferencesCommand = new RelayCommand(PlanDifferences, () => UnplannedDifferences > 0);
        Refresh();
    }

    /// <summary>Gets the hull display name.</summary>
    public string HullName { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the report summary.</summary>
    public string Summary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether you own the hull.</summary>
    public bool HullOwned { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the page's sections: the ship comparison and the report against everything owned.</summary>
    public SectionSwitch Sections { get; }

    /// <summary>Gets the report table's sort. The default, by slot, is the fit's order.</summary>
    public TableSort<FitLineRow> LineSort { get; }

    /// <summary>Gets the comparison table's sort.</summary>
    public TableSort<DifferenceRow> DifferenceSort { get; }

    /// <summary>Gets one row per required module, against everything you own, in sort order.</summary>
    public IReadOnlyList<FitLineRow> Lines { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets how many missing or partial modules need engineering that the plan does not yet hold, which is what <see cref="PlanMissingCommand"/> adds.</summary>
    public int MissingEngineered { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets how many missing or partial modules need engineering at all, planned or not.</summary>
    public int MissingEngineeredInAll { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the ownership plan button's label: how many gaps in what you own it would plan, or that they are all planned already, or that there are none.</summary>
    public string PlanMissingText => MissingEngineered > 0
        ? Strings.Plural(Keys.Fit_PlanGaps, MissingEngineered)
        : MissingEngineeredInAll > 0 ? Strings.Get(Keys.Fit_GapsPlanned) : Strings.Get(Keys.Fit_NoGaps);

    /// <summary>Gets the ships to compare with, best match first. Each label carries the match count.</summary>
    public IReadOnlyList<Choice> Ships { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether there is a ship to compare with.</summary>
    public bool HasShips => Ships.Count > 0;

    /// <summary>Gets or sets the ship compared. Must be one of <see cref="Ships"/>. Starts on the ship with the most matching modules.</summary>
    public Choice? SelectedShip
    {
        get => _selectedShip;
        set
        {
            if (value is not null && SetProperty(ref _selectedShip, value))
            {
                RefreshComparison();
            }
        }
    }

    /// <summary>Gets or sets whether modules you already own elsewhere count against the differences. Remembered; starts on.</summary>
    public bool UseOwned
    {
        get => Workspace.Preferences.GetBool(UseOwnedKey, true);
        set
        {
            if (value != UseOwned)
            {
                Workspace.Preferences.SetBool(UseOwnedKey, value);
                OnPropertyChanged();
                RefreshComparison();
            }
        }
    }

    /// <summary>Gets where an owned module may be to count: the fleet's place filter.</summary>
    public IReadOnlyList<Choice> Places { get; } = FleetChoices.Places;

    /// <summary>Gets or sets the place filter. Must be one of <see cref="Places"/>. Shared with the fleet page and remembered.</summary>
    public Choice SelectedPlace
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                Workspace.Fleet.Place = Enum.Parse<PlaceFilter>(value.Key);
            }
        }
    } = FleetChoices.Places[0];

    /// <summary>Gets "Everywhere" plus every system holding a ship or a stored module.</summary>
    public IReadOnlyList<Choice> Locations { get; private set => SetProperty(ref field, value); } = [FleetChoices.Everywhere];

    /// <summary>Gets or sets the location filter. Must be one of <see cref="Locations"/>. Shared with the fleet page and remembered.</summary>
    public Choice SelectedLocation
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                Workspace.Fleet.Location = value.Key.Length == 0 ? null : value.Key;
            }
        }
    } = FleetChoices.Everywhere;

    /// <summary>Gets a line on the comparison: matches, engineering, purchases, and how much your engineers cover.</summary>
    public string CompareSummary { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets a note when the compared ship is a different hull from the fit's, or empty.</summary>
    public string HullNote { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the slots where the ship falls short, in fit order.</summary>
    public IReadOnlyList<DifferenceRow> Differences { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets how many differences are engineering work.</summary>
    public int EngineeringCount { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets how many of those the plan does not yet hold, which is what <see cref="PlanDifferencesCommand"/> adds.</summary>
    public int UnplannedDifferences { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets the ship plan button's label: how many engineering changes for this ship it would plan, or that they are all planned already, or that there are none.</summary>
    public string PlanDifferencesText => UnplannedDifferences > 0
        ? Strings.Plural(Keys.Fit_PlanChanges, UnplannedDifferences)
        : EngineeringCount > 0 ? Strings.Get(Keys.Fit_ChangesPlanned) : Strings.Get(Keys.Fit_NoChanges);

    /// <inheritdoc/>
    public string EdsyJson { get; private set => SetProperty(ref field, value); } = "";

    /// <inheritdoc/>
    public string ExportStatus { get; set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the command that removes the fit.</summary>
    public RelayCommand RemoveCommand { get; }

    /// <summary>Gets the command that adds a plan entry for every missing or partial module that carries engineering: the full grade, or the experimental alone when that is all a partial module lacks.</summary>
    public RelayCommand PlanMissingCommand { get; }

    /// <summary>Gets the command that appends a plan entry for every engineering difference against the chosen ship. A slot that only lacks the experimental gets an experimental-only entry.</summary>
    public RelayCommand PlanDifferencesCommand { get; }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var fitting = Workspace.Fleet.Find(_id);
        if (fitting is null)
        {
            Title = Strings.Get(Keys.Fit_Removed);
            Lines = [];
            Ships = [];
            Differences = [];
            OnPropertyChanged(nameof(HasShips));
            return;
        }

        var fleet = Workspace.FleetSnapshot;
        var report = Workspace.Fleet.Match(fleet, Workspace.Farness).Fits.First(fit => fit.Fitting.Id == _id);
        Title = fitting.Name;
        EdsyJson = fitting.LoadoutJson ?? "";
        HullName = FleetPage.HullName(fleet, fitting.Hull);
        HullOwned = report.HullOwned;
        Summary = FleetPage.Summary(report);
        Lines = LineSort.Apply(report.Lines.Select(line =>
        {
            var code = Workspace.Modules.Find(line.Required.Item)?.Group;
            var type = code is not null && Workspace.Catalog.ModuleTypes.TryGetValue(code, out var found) ? found : null;
            return new FitLineRow(
                line.Required.Slot,
                Strings.Format(Keys.Ship_ModuleAndEngineering, ("module", Workspace.ModuleName(line.Required.Item)), ("engineering", FleetPage.EngineeringText(Workspace, line.Required.Engineering))),
                type is null ? null : LinkTo(Names.ModuleType(type), PageKind.ModuleType, type.Code),
                FleetPage.StatusText(line),
                line.Quality,
                FleetPage.ToneOf(line, fitting.Hull));
        }));
        var gaps = GapEntries(fitting, report);
        MissingEngineeredInAll = gaps.Count;
        MissingEngineered = gaps.Count(entry => !IsPlanned(entry, fitting.Name));
        OnPropertyChanged(nameof(PlanMissingText));
        PlanMissingCommand.RaiseCanExecuteChanged();

        Locations = FleetChoices.Locations(fleet);
        SelectedLocation = FleetChoices.LocationChoice(Locations, Workspace.Fleet.Location);
        SelectedPlace = FleetChoices.Places.First(choice => choice.Key == Workspace.Fleet.Place.ToString());
        RefreshShips(fitting, fleet);
    }

    private string FittedText(FitModule fitted, bool foundElsewhere)
    {
        var text = Strings.Format(Keys.Ship_ModuleAndEngineering, ("module", Workspace.ModuleName(fitted.Item)), ("engineering", FleetPage.EngineeringText(Workspace, fitted.Engineering)));
        return foundElsewhere ? Strings.Format(Keys.Fit_FittedElsewhere, ("module", text), ("slot", SlotNames.Display(fitted.Slot))) : text;
    }

    private static string KindText(DifferenceKind kind) => kind switch
    {
        DifferenceKind.SlotEmpty => Strings.Get(Keys.Fit_KindEmptySlot),
        DifferenceKind.DifferentModule => Strings.Get(Keys.Fit_KindDifferentModule),
        DifferenceKind.NotEngineered => Strings.Get(Keys.Fit_KindNotEngineered),
        DifferenceKind.DifferentBlueprint => Strings.Get(Keys.Fit_KindDifferentBlueprint),
        DifferenceKind.LowerGrade => Strings.Get(Keys.Fit_KindLowerGrade),
        DifferenceKind.UnfinishedGrade => Strings.Get(Keys.Fit_KindUnfinishedGrade),
        DifferenceKind.DifferentExperimental => Strings.Get(Keys.Fit_KindExperimentalMissing),
        DifferenceKind.ExperimentalUnknown => Strings.Get(Keys.Fit_KindExperimentalUnknown),
        _ => kind.ToString(),
    };

    private void RefreshShips(Fitting fitting, FleetSnapshot fleet)
    {
        var ranked = fleet.Ships
            .Select(ship => (Ship: ship, Comparison: FitComparer.Compare(fitting, FleetState.FittedModules(ship))))
            .OrderByDescending(pair => pair.Comparison.MatchCount)
            .ThenByDescending(pair => string.Equals(pair.Ship.Type, fitting.Hull, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(pair => pair.Ship.IsCurrent)
            .ThenBy(pair => pair.Ship.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Ships =
        [
            .. ranked.Select(pair => new Choice(
                pair.Ship.ShipId.ToString(CultureInfo.InvariantCulture),
                Strings.Format(Keys.Fit_ShipChoice, ("ship", FleetPage.ShipTitle(pair.Ship)), ("hull", Names.Hull(pair.Ship.Type, pair.Ship.TypeName)), ("matching", pair.Comparison.MatchCount), ("total", fitting.Modules.Count)))),
        ];
        OnPropertyChanged(nameof(HasShips));
        // Choices compare by value, so a rebuilt list leaves the selection equal to the old one; the
        // comparison must be rerun regardless, because the fleet behind it may have changed.
        var keep = _selectedShip is { } previous ? Ships.FirstOrDefault(choice => choice.Key == previous.Key) : null;
        _selectedShip = keep ?? (Ships.Count > 0 ? Ships[0] : null);
        OnPropertyChanged(nameof(SelectedShip));
        RefreshComparison();
    }

    private void RefreshComparison()
    {
        var fitting = Workspace.Fleet.Find(_id);
        var fleet = Workspace.FleetSnapshot;
        var ship = SelectedShip is { } chosen ? ShipPage.Find(fleet, chosen.Key) : null;
        if (fitting is null || ship is null)
        {
            _differences = [];
            Differences = [];
            EngineeringCount = 0;
            UnplannedDifferences = 0;
            CompareSummary = fleet.Ships.Count == 0 ? Strings.Get(Keys.Fit_NoShipsToCompare) : "";
            HullNote = "";
            OnPropertyChanged(nameof(PlanDifferencesText));
            PlanDifferencesCommand.RaiseCanExecuteChanged();
            return;
        }

        var comparison = FitComparer.Compare(fitting, FleetState.FittedModules(ship));
        _differences = comparison.Differences;
        _comparedShipTitle = FleetPage.ShipTitle(ship);
        var selected = Workspace.Engineers.Selected;
        var resolver = new ItemResolver(Workspace.Catalog);
        var rows = new List<DifferenceRow>();
        var covered = 0;
        var ownedCovers = 0;
        _ownedSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var elsewhere = UseOwned ? OwnedElsewhere(fleet, ship) : [];
        var prebuiltSlots = ship.Modules.Where(module => module.IsPrebuilt).Select(module => module.Slot).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var difference in comparison.Differences)
        {
            var ownedText = "";
            var ownedCover = false;
            if (UseOwned && difference.Required.Engineering is not null)
            {
                var found = FitMatcher.Find(difference.Required, elsewhere, Workspace.Farness);
                if (found.Found is { } owned)
                {
                    ownedCover = found.Quality == MatchQuality.Exact;
                    var where = owned.Place == ModulePlace.Installed ? Strings.Format(Keys.Match_FittedTo, ("ship", owned.Holder)) : Strings.Format(Keys.Match_StoredAt, ("place", owned.Holder));
                    ownedText = Strings.Format(ownedCover ? Keys.Fit_AlreadyOwned : Keys.Fit_MayAlreadyOwn, ("where", where));
                    if (ownedCover)
                    {
                        _ownedSlots.Add(difference.Required.Slot);
                        ownedCovers++;
                    }
                }
            }

            var engineersParts = Phrase.Empty;
            var coveredBySelection = false;
            if (difference.NeedsEngineering && !ownedCover && TentativeEntry(resolver, _comparedShipTitle, difference) is { } entry)
            {
                var service = EngineerCoverage.ServiceFor(Workspace.Catalog, entry, selected);
                var able = entry.IsExperimentalOnly ? service.CanApplyExperimental : service.CanRoll;
                coveredBySelection = selected.Count > 0
                    && (entry.IsExperimentalOnly || service.SelectedCanRoll.Count > 0)
                    && (entry.Experimental is null || service.SelectedCanApplyExperimental.Count > 0);
                engineersParts = selected.Count == 0
                    ? EngineerLink.Names(able.Select(Workspace.EngineerLinkFor), Strings.Get(Keys.Fit_NobodyOffersThis))
                    : coveredBySelection
                        ? Phrase.Format(Keys.Fit_YourEngineers, ("engineers", EngineerLink.Names(able.Where(selected.Contains).Select(Workspace.EngineerLinkFor), "")))
                        : Phrase.Format(Keys.Fit_NoneOfYours, ("engineers", EngineerLink.Names(able.Select(Workspace.EngineerLinkFor), Strings.Get(Keys.Fit_NobodyInData))));
                if (coveredBySelection)
                {
                    covered++;
                }
            }

            rows.Add(new DifferenceRow(
                difference.Required.Slot,
                Strings.Format(Keys.Ship_ModuleAndEngineering, ("module", Workspace.ModuleName(difference.Required.Item)), ("engineering", FleetPage.EngineeringText(Workspace, difference.Required.Engineering))),
                difference.Fitted is { } fitted
                    ? FittedText(fitted, difference.FoundElsewhere)
                    : Strings.Get(Keys.Ship_Empty),
                KindText(difference.Kind),
                difference.NeedsEngineering && !ownedCover,
                difference.NeedsPurchase && !ownedCover,
                difference.Kind == DifferenceKind.ExperimentalUnknown,
                engineersParts,
                coveredBySelection,
                ownedText,
                ownedCover,
                difference.Fitted is { } fittedModule && prebuiltSlots.Contains(fittedModule.Slot)));
        }

        Differences = DifferenceSort.Apply(rows);
        EngineeringCount = rows.Count(row => row.NeedsEngineering);
        UnplannedDifferences = DifferenceEntries().Count;
        var purchases = rows.Count(row => row.NeedsPurchase);
        var uncertain = rows.Count(row => row.IsUncertain);
        var parts = new List<string> { Strings.Format(Keys.Fit_AlreadyMatch, ("matching", comparison.MatchCount), ("total", fitting.Modules.Count)) };
        if (ownedCovers > 0)
        {
            parts.Add(Strings.Format(Keys.Fit_OwnedCanFill, ("slots", Display.Plural(Keys.Plural_Slots, ownedCovers))));
        }

        if (EngineeringCount > 0)
        {
            var slots = Display.Plural(Keys.Plural_Slots, EngineeringCount);
            parts.Add(selected.Count == 0
                ? Strings.Format(Keys.Fit_NeedEngineering, ("slots", slots))
                : Strings.Format(Keys.Fit_NeedEngineeringCovered, ("slots", slots), ("covered", covered)));
        }

        if (purchases > 0)
        {
            parts.Add(Strings.Format(Keys.Fit_MustBuy, ("modules", Display.Plural(Keys.Plural_Modules, purchases))));
        }

        if (uncertain > 0)
        {
            parts.Add(Strings.Format(Keys.Fit_MayBeRight, ("slots", Display.Plural(Keys.Plural_Slots, uncertain))));
        }

        var prebuilt = rows.Count(row => row.WarnPrebuilt);
        if (prebuilt > 0)
        {
            parts.Add(Strings.Plural(Keys.Fit_PrebuiltSlots, prebuilt));
        }

        CompareSummary = string.Join(" ", parts);
        HullNote = string.Equals(ship.Type, fitting.Hull, StringComparison.OrdinalIgnoreCase)
            ? ""
            : Strings.Format(Keys.Fit_HullNote, ("ship", Names.Hull(ship.Type, ship.TypeName)), ("fit", HullName));
        OnPropertyChanged(nameof(PlanDifferencesText));
        PlanDifferencesCommand.RaiseCanExecuteChanged();
    }

    private PlanEntry? TentativeEntry(ItemResolver resolver, string sourceName, FitDifference difference)
        => GapEntry(resolver, sourceName, difference.Required, difference.NeedsOnlyExperimental, difference.Fitted?.Engineering);

    /// <summary>
    /// The plan entry that closes one engineering gap: the full grade, or the experimental alone when that
    /// is all that is missing. The entry's source is the ship and slot it is for when the gap is one ship's,
    /// the same stamp the ship page gives its entries, so the two pages agree on what is already planned; a
    /// gap in what you own, which is no one ship's, carries the fit's name instead. The rolls start from
    /// where the module stands: grades it already has are not planned, and the grade it is part way
    /// through gets the rolls still owed, so a module at grade 3 and a quarter wanting grade 3 plans
    /// three quarters of the grade's rolls, rounded up.
    /// </summary>
    /// <returns>The entry, or <see langword="null"/> when the requirement is stock, names a blueprint the catalog lacks, or wants only an experimental the catalog lacks.</returns>
    private PlanEntry? GapEntry(ItemResolver resolver, string sourceName, FitModule required, bool onlyExperimental, Engineering? has)
    {
        var engineering = required.Engineering;
        if (engineering is null || !Workspace.Catalog.Blueprints.ContainsKey(engineering.Blueprint))
        {
            return null;
        }

        var experimental = engineering.Experimental is { } effect && Workspace.Catalog.Experimentals.ContainsKey(effect) ? effect : null;
        if (onlyExperimental && experimental is null)
        {
            return null;
        }

        var entry = PlanEntry.Create(
            Workspace.Catalog,
            resolver.Resolve(required.Item),
            engineering.Blueprint,
            onlyExperimental ? 0 : engineering.Level,
            experimental,
            new PlanSource(sourceName, required.Slot, required.Item));
        if (has is null || !string.Equals(has.Blueprint, engineering.Blueprint, StringComparison.OrdinalIgnoreCase))
        {
            return entry;
        }

        foreach (var grade in entry.RolledGrades(Workspace.Catalog))
        {
            if (grade < has.Level || (grade == has.Level && has.IsComplete))
            {
                entry = entry.WithRolls(grade, 0);
            }
            else if (grade == has.Level)
            {
                entry = entry.WithRolls(grade, Math.Max(1, (int)Math.Ceiling(entry.RollsAt(grade) * (1 - has.Quality))));
            }
        }

        return entry;
    }

    /// <summary>The entries that close every missing or partial line of a report.</summary>
    private List<PlanEntry> GapEntries(Fitting fitting, FitReport report)
    {
        var resolver = new ItemResolver(Workspace.Catalog);
        return
        [
            .. report.Lines
                .Where(line => line.Quality is MatchQuality.Missing or MatchQuality.Partial)
                .Select(line => GapEntry(resolver, fitting.Name, line.Required, line.NeedsOnlyExperimental, line.Found?.Engineering))
                .Where(entry => entry is not null)
                .Select(entry => entry!),
        ];
    }

    /// <summary>The entries that close the chosen ship's engineering differences and are not in the plan yet, stamped with that ship and slot.</summary>
    private List<PlanEntry> DifferenceEntries()
    {
        var resolver = new ItemResolver(Workspace.Catalog);
        var fitName = Workspace.Fleet.Find(_id)?.Name ?? "";
        return
        [
            .. _differences
                .Where(difference => difference.NeedsEngineering && !_ownedSlots.Contains(difference.Required.Slot))
                .Select(difference => TentativeEntry(resolver, _comparedShipTitle, difference))
                .Where(entry => entry is not null && !IsPlanned(entry, fitName))
                .Select(entry => entry!),
        ];
    }

    /// <summary>
    /// Whether the plan already holds this work under either stamp this page gives an entry: the ship compared
    /// with, or the fit itself. The ownership button stamps the fit, the ship button stamps the ship, and a
    /// slot's engineering planned from either is the same job, so each button stands down for the other's work.
    /// </summary>
    private bool IsPlanned(PlanEntry entry, string fitName)
        => Workspace.Plan.Contains(entry)
            || Workspace.Plan.Contains(entry with { Source = entry.Source with { Ship = fitName } })
            || (_comparedShipTitle.Length > 0 && Workspace.Plan.Contains(entry with { Source = entry.Source with { Ship = _comparedShipTitle } }));

    private void PlanDifferences()
    {
        var entries = DifferenceEntries();
        if (entries.Count > 0)
        {
            Workspace.Plan.Add(entries);
        }
    }

    private IReadOnlyList<OwnedModule> OwnedElsewhere(FleetSnapshot fleet, OwnedShip ship)
    {
        var shipPrefix = string.Create(CultureInfo.InvariantCulture, $"ship:{ship.ShipId}:");
        var location = Workspace.Fleet.Location;
        return
        [
            .. FleetState.OwnedModules(fleet)
                .Where(module => !module.Id.StartsWith(shipPrefix, StringComparison.Ordinal))
                .Where(module => Workspace.Fleet.Place switch
                {
                    PlaceFilter.Anywhere => true,
                    PlaceFilter.StoredOnly => module.Place == ModulePlace.Stored,
                    PlaceFilter.FittedOnly => module.Place == ModulePlace.Installed,
                    _ => true,
                })
                .Where(module => location is null || string.Equals(module.System, location, StringComparison.OrdinalIgnoreCase)),
        ];
    }

    private void PlanMissing()
    {
        var fitting = Workspace.Fleet.Find(_id);
        if (fitting is null)
        {
            return;
        }

        var report = Workspace.Fleet.Match(Workspace.FleetSnapshot, Workspace.Farness).Fits.First(fit => fit.Fitting.Id == _id);
        var entries = GapEntries(fitting, report).Where(entry => !IsPlanned(entry, fitting.Name)).ToList();
        if (entries.Count > 0)
        {
            Workspace.Plan.Add(entries);
        }
    }
}
