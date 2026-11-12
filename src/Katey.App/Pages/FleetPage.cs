using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core.Edsy;
using Katey.Core.Fleet;
using Katey.Journal;

namespace Katey.App.Pages;

/// <summary>The colour a fit line's status takes.</summary>
public enum StatusTone
{
    /// <summary>Nothing suitable is owned: red.</summary>
    Missing,

    /// <summary>A module is owned but needs engineering, or its experimental is unknown: orange.</summary>
    Change,

    /// <summary>The module is in station storage: blue.</summary>
    Stored,

    /// <summary>The module is fitted to a ship of the fit's hull: green.</summary>
    Fitted,

    /// <summary>The module is fitted to a ship of another hull, which would have to give it up: yellow.</summary>
    Elsewhere,
}

/// <summary>One owned ship in the index.</summary>
/// <param name="Link">The ship's page.</param>
/// <param name="TypeName">Hull display name.</param>
/// <param name="Where">Where the ship is: its system and station, or where it is in transit to.</param>
/// <param name="FitText">One line on its fit, or that the fit has not been seen.</param>
/// <param name="IsCurrent">Whether you are flying it.</param>
/// <param name="IsPrebuilt">Whether the journal saw it redeemed from a bundle.</param>
public sealed record ShipListRow(Link Link, string TypeName, Place Where, string FitText, bool IsCurrent, bool IsPrebuilt)
{
    /// <summary>Gets the ship's title.</summary>
    public string Name => Link.Label;
}

/// <summary>One saved fit in the index with its report summary.</summary>
/// <param name="Link">Its page.</param>
/// <param name="HullName">Hull display name.</param>
/// <param name="Summary">Missing and uncertain counts, or a note that the hull is not owned.</param>
/// <param name="MissingCount">How many modules are missing, or -1 when the hull is not owned.</param>
/// <param name="AllOwned">Whether every module the fit wants is owned somewhere under the sharing policy, with no doubt. This says nothing about any one ship being fitted that way; the fit page's comparison does.</param>
/// <param name="RemoveCommand">Removes the fit.</param>
public sealed record FitListRow(Link Link, string HullName, string Summary, int MissingCount, bool AllOwned, RelayCommand RemoveCommand)
{
    /// <summary>Gets the fit's name.</summary>
    public string Name => Link.Label;

    /// <summary>Gets whether something the fit wants is not owned at all: a module, or the hull itself.</summary>
    public bool IsBad => MissingCount != 0;

    /// <summary>Gets whether everything is owned but some of it only partly engineered or in doubt.</summary>
    public bool IsWarn => MissingCount == 0 && !AllOwned;

    /// <summary>Gets the verdict in a word or two: "ALL OWNED", "HULL NOT OWNED", "3 MISSING" or "PARTIAL".</summary>
    public string Verdict => AllOwned ? Display.Upper(Keys.Fleet_VerdictAllOwned) : MissingCount < 0 ? Display.Upper(Keys.Fleet_VerdictHullNotOwned) : MissingCount > 0 ? Strings.Format(Keys.Fleet_VerdictMissing, ("count", MissingCount)).ToUpper(CultureInfo.CurrentCulture) : Display.Upper(Keys.Fleet_VerdictPartial);
}

/// <summary>One owned module no fit claimed.</summary>
/// <param name="Link">The module type's page, or <see langword="null"/> for an unrecognised item.</param>
/// <param name="Name">Module name.</param>
/// <param name="Engineering">Engineering as text, or "stock".</param>
/// <param name="Where"></param>
/// <param name="IsEngineered">Whether it carries engineering.</param>
public sealed record UnusedModuleRow(Link? Link, string Name, string Engineering, Place Where, bool IsEngineered)
{
    /// <summary>Gets whether the module type is known.</summary>
    public bool HasLink => Link is not null;

    /// <summary>Gets the module type name, or empty.</summary>
    public string TypeName => Link?.Label ?? "";
}

/// <summary>Ships, saved fits and the owned modules no fit uses, with the sharing policy and filters.</summary>
public sealed class FleetPage : Page
{
    private readonly EdsyImport _import;
    private (Fitting Parsed, Fitting Existing)? _pendingReplacement;

    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    public FleetPage(Workspace workspace)
        : base(new PageKey(PageKind.Fleet), workspace)
    {
        Title = Strings.Get(Keys.Nav_Fleet);
        Kicker = Strings.Get(Keys.Page_KickerBrowse);
        _import = new EdsyImport(workspace.Catalog);
        Sections = new SectionSwitch([("ships", Strings.Get(Keys.Section_Ships)), ("fits", Strings.Get(Keys.Section_SavedFits)), ("unused", Strings.Get(Keys.Section_UnusedModules))], "ships", workspace.Preferences, "section.fleet");
        SelectedPolicy = FleetChoices.Policies.First(choice => choice.Key == workspace.Fleet.Policy.ToString());
        SelectedFilter = FleetChoices.Filters.First(choice => choice.Key == workspace.Fleet.Filter.ToString());
        SelectedPlace = FleetChoices.Places.First(choice => choice.Key == workspace.Fleet.Place.ToString());
        AddFittingCommand = new RelayCommand(AddFitting, () => PasteText.Trim().Length > 0);
        CancelReplaceCommand = new RelayCommand(CancelReplace);
        ShipSort = new TableSort<ShipListRow>(
            [
                new("name", Strings.Get(Keys.Column_Ship), row => row.Name),
                new("type", Strings.Get(Keys.Column_Hull), row => row.TypeName),
                new("location", Strings.Get(Keys.Column_Where), row => row.Where.Text),
            ],
            "name",
            workspace.Preferences,
            "sort.ships");
        FitSort = new TableSort<FitListRow>(
            [
                new("name", Strings.Get(Keys.Column_Fit), row => row.Name),
                new("hull", Strings.Get(Keys.Column_Hull), row => row.HullName),
                new("missing", Strings.Get(Keys.Column_Missing), row => row.MissingCount, DescendingFirst: true),
            ],
            "name",
            workspace.Preferences,
            "sort.fits");
        UnusedSort = new TableSort<UnusedModuleRow>(
            [
                new("name", Strings.Get(Keys.Column_Module), row => row.Name),
                new("engineering", Strings.Get(Keys.Column_Engineering), row => row.Engineering),
                new("place", Strings.Get(Keys.Column_Where), row => row.Where.Text),
                new("type", Strings.Get(Keys.Column_Type), row => row.TypeName),
            ],
            "name",
            workspace.Preferences,
            "sort.unused");
        ShipSort.Changed += (_, _) => Refresh();
        FitSort.Changed += (_, _) => Refresh();
        UnusedSort.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Gets the page's sections: ships, saved fits, unused modules. One shows at a time.</summary>
    public SectionSwitch Sections { get; }

    /// <summary>Gets a line on how fresh the ownership data is.</summary>
    public string FleetStatus { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the ship table's sort.</summary>
    public TableSort<ShipListRow> ShipSort { get; }

    /// <summary>Gets the fit table's sort.</summary>
    public TableSort<FitListRow> FitSort { get; }

    /// <summary>Gets the unused-module table's sort.</summary>
    public TableSort<UnusedModuleRow> UnusedSort { get; }

    /// <summary>Gets the owned ships, in sort order.</summary>
    public IReadOnlyList<ShipListRow> Ships { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the saved fits, in sort order.</summary>
    public IReadOnlyList<FitListRow> Fits { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether any fit is saved.</summary>
    public bool HasFits => Fits.Count > 0;

    /// <summary>Gets or sets the EDSY export text to save as a fit. Changing it withdraws any replacement awaiting confirmation.</summary>
    public string PasteText
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? ""))
            {
                AddFittingCommand?.RaiseCanExecuteChanged();
                CancelReplace();
            }
        }
    } = "";

    /// <summary>
    /// Gets whether the pasted build names a fit that is already saved, the same name on the same hull, and
    /// awaits a second press to replace it. A build with the same name on another hull is a new fit.
    /// </summary>
    public bool IsConfirmingReplace => _pendingReplacement is not null;

    /// <summary>Gets the question put while a replacement awaits confirmation, such as "Replace the saved fit Combat for the Anaconda Mk II?", or empty.</summary>
    public string ReplaceText => _pendingReplacement is { } pending ? Strings.Format(Keys.Fleet_ReplaceQuestion, ("name", pending.Existing.Name), ("hull", Names.Hull(pending.Existing.Hull, null))) : "";

    /// <summary>Gets the command that withdraws the replacement and keeps the saved fit.</summary>
    public RelayCommand CancelReplaceCommand { get; }

    /// <summary>Gets why the last add failed, or empty.</summary>
    public string AddError { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets whether <see cref="AddError"/> has anything to show.</summary>
    public bool HasAddError => AddError.Length > 0;

    /// <summary>Gets the command that parses <see cref="PasteText"/> into a saved fit.</summary>
    public RelayCommand AddFittingCommand { get; }

    /// <summary>Gets the sharing policies.</summary>
    public IReadOnlyList<Choice> Policies { get; } = FleetChoices.Policies;

    /// <summary>Gets or sets the sharing policy.</summary>
    public Choice SelectedPolicy
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                Workspace.Fleet.Policy = Enum.Parse<SharingPolicy>(value.Key);
            }
        }
    } = FleetChoices.Policies[0];

    /// <summary>Gets "Everywhere" plus every system holding a ship or a stored module.</summary>
    public IReadOnlyList<Choice> Locations { get; private set => SetProperty(ref field, value); } = [FleetChoices.Everywhere];

    /// <summary>Gets or sets the location filter. Must be one of <see cref="Locations"/>.</summary>
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

    /// <summary>Gets the ways the unused list can be narrowed by engineering.</summary>
    public IReadOnlyList<Choice> Filters { get; } = FleetChoices.Filters;

    /// <summary>Gets or sets which unused modules are listed. Must be one of <see cref="Filters"/>.</summary>
    public Choice SelectedFilter
    {
        get;
        set
        {
            if (value is not null && SetProperty(ref field, value))
            {
                Workspace.Fleet.Filter = Enum.Parse<EngineeringFilter>(value.Key);
            }
        }
    } = FleetChoices.Filters[0];

    /// <summary>Gets the ways the unused list can be narrowed by place.</summary>
    public IReadOnlyList<Choice> Places { get; } = FleetChoices.Places;

    /// <summary>Gets or sets where listed modules may be. Must be one of <see cref="Places"/>.</summary>
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

    /// <summary>Gets owned modules no fit claimed, within the filters, in sort order.</summary>
    public IReadOnlyList<UnusedModuleRow> Unused { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a heading for the unused list with its count.</summary>
    public string UnusedHeading => Strings.Format(Keys.Fleet_UnusedHeading, ("count", Unused.Count));

    /// <summary>Engineering as text, or "stock".</summary>
    /// <param name="workspace">The shared state, for names.</param>
    /// <param name="engineering">The engineering, or <see langword="null"/>.</param>
    /// <returns>Text.</returns>
    public static string EngineeringText(Workspace workspace, Engineering? engineering)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (engineering is null)
        {
            return Strings.Get(Keys.Engineering_Stock);
        }

        var catalog = workspace.Catalog;
        var blueprint = catalog.Blueprints.TryGetValue(engineering.Blueprint, out var bp) ? Names.Blueprint(bp) : engineering.Blueprint;
        var grade = Display.Grade(engineering.Level);
        var experimental = engineering.Experimental is { } effect ? (catalog.Experimentals.TryGetValue(effect, out var ex) ? Names.Experimental(ex) : effect) : null;
        var key = (engineering.IsComplete, experimental is not null, engineering.ExperimentalKnown) switch
        {
            (true, true, _) => Keys.Engineering_TextWithExperimental,
            (true, false, true) => Keys.Engineering_Text,
            (true, false, false) => Keys.Engineering_TextExperimentalUnknown,
            (false, true, _) => Keys.Engineering_TextUnfinishedWithExperimental,
            (false, false, true) => Keys.Engineering_TextUnfinished,
            (false, false, false) => Keys.Engineering_TextUnfinishedExperimentalUnknown,
        };
        return Strings.Format(key, ("blueprint", blueprint), ("grade", grade), ("percent", engineering.Percent), ("experimental", experimental));
    }

    /// <summary>
    /// The colour a fit line's status takes: missing, a module that needs changing, one in storage, one fitted
    /// to a ship of the fit's hull, or one fitted to a ship of another hull.
    /// </summary>
    /// <param name="line">The fit line.</param>
    /// <param name="hull">The fit's hull symbol, compared case-insensitively.</param>
    /// <returns>The tone.</returns>
    public static StatusTone ToneOf(FitLine line, string hull)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(hull);
        return line.Found is null || line.Quality == MatchQuality.Missing ? StatusTone.Missing
            : line.Quality is MatchQuality.Partial or MatchQuality.ExperimentalUnknown ? StatusTone.Change
            : line.Found.Place == ModulePlace.Stored ? StatusTone.Stored
            : string.Equals(line.Found.Hull, hull, StringComparison.OrdinalIgnoreCase) ? StatusTone.Fitted
            : StatusTone.Elsewhere;
    }

    /// <summary>Where a found module is, with the match caveat, and whether it is prebuilt and so bound to its ship.</summary>
    /// <param name="line">The fit line.</param>
    /// <returns>Text such as "fitted to Ironic", "fitted to Ironic, prebuilt" or "missing".</returns>
    public static string StatusText(FitLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (line.Found is null)
        {
            return Strings.Get(Keys.Match_Missing);
        }

        var place = line.Found.Place == ModulePlace.Installed
            ? Strings.Format(line.Found.IsPrebuilt ? Keys.Match_FittedToPrebuilt : Keys.Match_FittedTo, ("ship", line.Found.Holder))
            : Strings.Format(Keys.Match_StoredAt, ("place", line.Found.Holder));
        return line.Quality switch
        {
            MatchQuality.ExperimentalUnknown => Strings.Format(Keys.Match_ExperimentalUnknown, ("place", place)),
            MatchQuality.Partial when line.NeedsOnlyExperimental => Strings.Format(Keys.Match_PartialExperimentalMissing, ("place", place)),
            MatchQuality.Partial when line.Found.Engineering is { IsComplete: false } unfinished && unfinished.Level == (line.Required.Engineering?.Level ?? 0) => Strings.Format(Keys.Match_PartialUnfinished, ("place", place), ("grade", Display.Grade(unfinished.Level)), ("percent", unfinished.Percent)),
            MatchQuality.Partial => Strings.Format(Keys.Match_PartialGrade, ("place", place), ("grade", Display.Grade(line.Found.Engineering?.Level ?? 0)), ("wanted", Display.Grade(line.Required.Engineering?.Level ?? 0))),
            MatchQuality.Missing => Strings.Get(Keys.Match_Missing),
            MatchQuality.Exact => place,
            _ => place,
        };
    }

    /// <summary>A fit report's one-line summary.</summary>
    /// <param name="fit">The report.</param>
    /// <returns>Text.</returns>
    public static string Summary(FitReport fit)
    {
        ArgumentNullException.ThrowIfNull(fit);
        return !fit.HullOwned
            ? Strings.Get(Keys.Fleet_HullNotOwned)
            : fit.MissingCount == 0 && fit.PartialCount == 0 && fit.UncertainCount == 0
                ? Strings.Get(Keys.Fleet_EveryModuleOwned)
                : Display.List(new[]
                {
                    fit.MissingCount > 0 ? Strings.Format(Keys.Fleet_SummaryMissing, ("count", fit.MissingCount)) : "",
                    fit.PartialCount > 0 ? Strings.Format(Keys.Fleet_SummaryPartial, ("count", fit.PartialCount)) : "",
                    fit.UncertainCount > 0 ? Strings.Format(Keys.Fleet_SummaryUncertain, ("count", fit.UncertainCount)) : "",
                }.Where(part => part.Length > 0)) + ".";
    }

    /// <summary>A hull's display name: the journal's when you own one, else the game's name for the symbol.</summary>
    /// <param name="fleet">The fleet.</param>
    /// <param name="hull">Ship type symbol.</param>
    /// <returns>Text such as "Anaconda Mk II".</returns>
    public static string HullName(FleetSnapshot fleet, string hull)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        ArgumentNullException.ThrowIfNull(hull);
        var ship = fleet.Ships.FirstOrDefault(ship => string.Equals(ship.Type, hull, StringComparison.OrdinalIgnoreCase));
        return Names.Hull(hull, ship?.TypeName);
    }

    /// <summary>A ship's title: name and ident when named, else its hull.</summary>
    /// <param name="ship">The ship.</param>
    /// <returns>Text.</returns>
    public static string ShipTitle(OwnedShip ship)
    {
        ArgumentNullException.ThrowIfNull(ship);
        return ship.Name.Length > 0 ? (ship.Ident.Length > 0 ? Strings.Format(Keys.Fleet_ShipTitle, ("name", ship.Name), ("ident", ship.Ident)) : ship.Name) : Names.Hull(ship.Type, ship.TypeName);
    }

    /// <summary>Where a ship is: its system's link and the station, or a state when the system is unknown.</summary>
    /// <param name="ship">The ship.</param>
    /// <returns>The place.</returns>
    public Place WhereShip(OwnedShip ship)
    {
        ArgumentNullException.ThrowIfNull(ship);
        return ship.InTransit
            ? Place.Only(Strings.Get(Keys.Fleet_InTransit))
            : ship.System is null
                ? Place.Only(Strings.Get(Keys.Fleet_LocationUnknown))
                : new Place(LinkTo(ship.System, PageKind.System, ship.System), ship.Station ?? "");
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
        var fleet = Workspace.FleetSnapshot;
        Ships = ShipSort.Apply(fleet.Ships.Select(ship => new ShipListRow(
            LinkTo(ShipTitle(ship), PageKind.Ship, ship.ShipId.ToString(CultureInfo.InvariantCulture)),
            Names.Hull(ship.Type, ship.TypeName),
            WhereShip(ship),
            ship.FitAsOf is { } asOf ? Strings.Format(Keys.Fleet_FitAsOf, ("when", asOf.ToLocalTime())) : Strings.Get(Keys.Fleet_FitUnknown),
            ship.IsCurrent,
            ship.IsPrebuilt)));
        FleetStatus = fleet.Ships.Count == 0
            ? Strings.Get(Keys.Fleet_NoShips)
            : Strings.Format(
                Keys.Fleet_Status,
                ("ships", Display.Plural(Keys.Plural_Ships, fleet.Ships.Count)),
                ("modules", Display.Plural(Keys.Plural_StoredModules, fleet.StoredModules.Count)),
                ("shipsAsOf", fleet.ShipsAsOf is { } s ? Strings.Format(Keys.Fleet_AsOf, ("when", s.ToLocalTime())) : Strings.Get(Keys.Fleet_ShipsNotSeen)),
                ("modulesAsOf", fleet.ModulesAsOf is { } m ? Strings.Format(Keys.Fleet_AsOf, ("when", m.ToLocalTime())) : Strings.Get(Keys.Fleet_ModulesNotSeen)));

        Locations = FleetChoices.Locations(fleet);
        SelectedLocation = FleetChoices.LocationChoice(Locations, Workspace.Fleet.Location);

        var report = Workspace.Fleet.Match(fleet, Workspace.Farness);
        Fits = FitSort.Apply(report.Fits.Select(fit => new FitListRow(
            LinkTo(fit.Fitting.Name, PageKind.Fit, fit.Fitting.Id),
            HullName(fleet, fit.Fitting.Hull),
            Summary(fit),
            fit.HullOwned ? fit.MissingCount : -1,
            fit.HullOwned && fit.Lines.All(line => line.Quality == MatchQuality.Exact),
            new RelayCommand(() => Workspace.Fleet.Remove(fit.Fitting.Id)))));
        OnPropertyChanged(nameof(HasFits));

        Unused = UnusedSort.Apply(Workspace.Fleet.Unused(report).Select(module =>
        {
            var code = Workspace.Modules.Find(module.Item)?.Group;
            var type = code is not null && Workspace.Catalog.ModuleTypes.TryGetValue(code, out var found) ? found : null;
            return new UnusedModuleRow(
                type is null ? null : LinkTo(Names.ModuleType(type), PageKind.ModuleType, type.Code),
                Workspace.ModuleName(module.Item),
                EngineeringText(Workspace, module.Engineering),
                WhereOwned(module),
                module.IsEngineered);
        }));
        OnPropertyChanged(nameof(UnusedHeading));
    }

    /// <summary>
    /// Saves the pasted build as a fit. A build whose name and hull match a saved fit is not saved at once:
    /// the first press asks, the second replaces that fit in place, keeping its id and position. A build
    /// with the same name on another hull, or a new name, is added beside the others.
    /// </summary>
    private void AddFitting()
    {
        if (_pendingReplacement is { } pending)
        {
            Workspace.Fleet.Update(pending.Parsed with { Id = pending.Existing.Id });
            CancelReplace();
            PasteText = "";
            AddError = "";
            OnPropertyChanged(nameof(HasAddError));
            return;
        }

        var result = _import.Parse(PasteText);
        if (!result.Ok)
        {
            AddError = ImportViewModel.ImportErrorText(result.Error);
            OnPropertyChanged(nameof(HasAddError));
            return;
        }

        var loadout = result.Loadout!;
        var parsed = new Fitting(
            Guid.NewGuid().ToString("N"),
            loadout.Ship,
            loadout.ShipName,
            [
                .. loadout.AllModules.Select(module => new FitModule(
                    module.Slot,
                    module.Item,
                    module.Blueprint is null ? null : new Engineering(module.Blueprint, module.Level, module.Experimental))),
            ],
            PasteText.Trim());
        AddError = "";
        OnPropertyChanged(nameof(HasAddError));
        if (Workspace.Fleet.Find(parsed.Name, parsed.Hull) is { } existing)
        {
            _pendingReplacement = (parsed, existing);
            OnPropertyChanged(nameof(IsConfirmingReplace));
            OnPropertyChanged(nameof(ReplaceText));
            return;
        }

        Workspace.Fleet.Add(parsed);
        PasteText = "";
    }

    private void CancelReplace()
    {
        if (_pendingReplacement is null)
        {
            return;
        }

        _pendingReplacement = null;
        OnPropertyChanged(nameof(IsConfirmingReplace));
        OnPropertyChanged(nameof(ReplaceText));
    }
}
