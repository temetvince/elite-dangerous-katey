using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
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
    public string Verdict => AllOwned ? "ALL OWNED" : MissingCount < 0 ? "HULL NOT OWNED" : MissingCount > 0 ? $"{MissingCount} MISSING" : "PARTIAL";
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
        Title = "Fleet";
        Kicker = "Browse";
        _import = new EdsyImport(workspace.Catalog);
        Sections = new SectionSwitch([("ships", "Ships"), ("fits", "Saved fits"), ("unused", "Owned modules no fit uses")], "ships", workspace.Preferences, "section.fleet");
        SelectedPolicy = FleetChoices.Policies.First(choice => choice.Key == workspace.Fleet.Policy.ToString());
        SelectedFilter = FleetChoices.Filters.First(choice => choice.Key == workspace.Fleet.Filter.ToString());
        SelectedPlace = FleetChoices.Places.First(choice => choice.Key == workspace.Fleet.Place.ToString());
        AddFittingCommand = new RelayCommand(AddFitting, () => PasteText.Trim().Length > 0);
        CancelReplaceCommand = new RelayCommand(CancelReplace);
        ShipSort = new TableSort<ShipListRow>(
            [
                new("name", "Ship", row => row.Name),
                new("type", "Hull", row => row.TypeName),
                new("location", "Where", row => row.Where.Text),
            ],
            "name",
            workspace.Preferences,
            "sort.ships");
        FitSort = new TableSort<FitListRow>(
            [
                new("name", "Fit", row => row.Name),
                new("hull", "Hull", row => row.HullName),
                new("missing", "Missing", row => row.MissingCount, DescendingFirst: true),
            ],
            "name",
            workspace.Preferences,
            "sort.fits");
        UnusedSort = new TableSort<UnusedModuleRow>(
            [
                new("name", "Module", row => row.Name),
                new("engineering", "Engineering", row => row.Engineering),
                new("place", "Where", row => row.Where.Text),
                new("type", "Type", row => row.TypeName),
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
    public string ReplaceText => _pendingReplacement is { } pending ? $"Replace the saved fit {pending.Existing.Name} for the {ShipNames.Display(pending.Existing.Hull)}?" : "";

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
    public string UnusedHeading => $"Owned modules no fit uses ({Unused.Count})";

    /// <summary>Engineering as text, or "stock".</summary>
    /// <param name="workspace">The shared state, for names.</param>
    /// <param name="engineering">The engineering, or <see langword="null"/>.</param>
    /// <returns>Text.</returns>
    public static string EngineeringText(Workspace workspace, Engineering? engineering)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (engineering is null)
        {
            return "stock";
        }

        var catalog = workspace.Catalog;
        var blueprint = catalog.Blueprints.TryGetValue(engineering.Blueprint, out var bp) ? bp.Name : engineering.Blueprint;
        var text = $"{blueprint} {Display.Grade(engineering.Level)}{(engineering.IsComplete ? "" : $", {engineering.Percent}%")}";
        if (engineering.Experimental is { } effect)
        {
            text += " + " + (catalog.Experimentals.TryGetValue(effect, out var ex) ? ex.Name : effect);
        }
        else if (!engineering.ExperimentalKnown)
        {
            text += ", experimental unknown";
        }

        return text;
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
            return "missing";
        }

        var place = line.Found.Place == ModulePlace.Installed ? $"fitted to {line.Found.Holder}{(line.Found.IsPrebuilt ? ", prebuilt" : "")}" : $"stored at {line.Found.Holder}";
        return line.Quality switch
        {
            MatchQuality.ExperimentalUnknown => $"{place}, experimental unknown",
            MatchQuality.Partial when line.NeedsOnlyExperimental => $"partial: {place}, experimental missing",
            MatchQuality.Partial when line.Found.Engineering is { IsComplete: false } unfinished && unfinished.Level == (line.Required.Engineering?.Level ?? 0) => $"partial: {place}, {Display.Grade(unfinished.Level)} at {unfinished.Percent}%",
            MatchQuality.Partial => $"partial: {place}, {Display.Grade(line.Found.Engineering?.Level ?? 0)} of {Display.Grade(line.Required.Engineering?.Level ?? 0)}",
            MatchQuality.Missing => "missing",
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
            ? "You do not own this hull."
            : fit.MissingCount == 0 && fit.PartialCount == 0 && fit.UncertainCount == 0
                ? "Every module is owned."
                : string.Join(", ", new[]
                {
                    fit.MissingCount > 0 ? $"{fit.MissingCount} missing" : "",
                    fit.PartialCount > 0 ? $"{fit.PartialCount} partial" : "",
                    fit.UncertainCount > 0 ? $"{fit.UncertainCount} with an unknown experimental" : "",
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
        return ShipNames.Prefer(hull, ship?.TypeName);
    }

    /// <summary>A ship's title: name and ident when named, else its hull.</summary>
    /// <param name="ship">The ship.</param>
    /// <returns>Text.</returns>
    public static string ShipTitle(OwnedShip ship)
    {
        ArgumentNullException.ThrowIfNull(ship);
        return ship.Name.Length > 0 ? (ship.Ident.Length > 0 ? $"{ship.Name} ({ship.Ident})" : ship.Name) : ShipNames.Prefer(ship.Type, ship.TypeName);
    }

    /// <summary>Where a ship is: its system's link and the station, or a state when the system is unknown.</summary>
    /// <param name="ship">The ship.</param>
    /// <returns>The place.</returns>
    public Place WhereShip(OwnedShip ship)
    {
        ArgumentNullException.ThrowIfNull(ship);
        return ship.InTransit
            ? Place.Only("in transit")
            : ship.System is null
                ? Place.Only("location unknown")
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
            ? new Place(system, $"fitted to {module.Holder}")
            : new Place(system, system is null ? module.Holder : "in storage");
    }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var fleet = Workspace.FleetSnapshot;
        Ships = ShipSort.Apply(fleet.Ships.Select(ship => new ShipListRow(
            LinkTo(ShipTitle(ship), PageKind.Ship, ship.ShipId.ToString(CultureInfo.InvariantCulture)),
            ShipNames.Prefer(ship.Type, ship.TypeName),
            WhereShip(ship),
            ship.FitAsOf is { } asOf ? string.Create(CultureInfo.CurrentCulture, $"fit as of {asOf.ToLocalTime():g}") : "fit unknown: not flown since the journal began",
            ship.IsCurrent,
            ship.IsPrebuilt)));
        FleetStatus = fleet.Ships.Count == 0
            ? "No ships seen yet. The fleet appears once the journal has been read."
            : string.Create(
                CultureInfo.CurrentCulture,
                $"{Display.Plural(fleet.Ships.Count, "ship")}, {Display.Plural(fleet.StoredModules.Count, "stored module")}. Fleet list {(fleet.ShipsAsOf is { } s ? "as of " + s.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) : "not yet seen: dock at a shipyard")}; stored modules {(fleet.ModulesAsOf is { } m ? "as of " + m.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) : "not yet seen: dock at outfitting")}.");

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
                type is null ? null : LinkTo(type.Name, PageKind.ModuleType, type.Code),
                Workspace.Modules.NameOf(module.Item),
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
            AddError = result.Error ?? "The export could not be read.";
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
