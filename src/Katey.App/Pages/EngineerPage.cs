using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.ViewModels;
using Katey.Core.Planning;

namespace Katey.App.Pages;

/// <summary>One blueprint an engineer offers on one module type, with a button to plan it at the top grade.</summary>
/// <param name="Blueprint">Display name.</param>
/// <param name="MaxGrade">The highest grade this engineer offers.</param>
/// <param name="TopGrade">The blueprint's highest grade.</param>
/// <param name="AddCommand">Adds an entry at <paramref name="MaxGrade"/>.</param>
public sealed record EngineerOfferRow(string Blueprint, int MaxGrade, int TopGrade, RelayCommand AddCommand)
{
    /// <summary>Gets "Gn", with "of Gm" when the engineer stops short of the top.</summary>
    public string GradeText => MaxGrade == TopGrade ? Display.Grade(MaxGrade) : $"{Display.Grade(MaxGrade)} of {Display.Grade(TopGrade)}";

    /// <summary>Gets whether the engineer reaches the blueprint's top grade.</summary>
    public bool ReachesTop => MaxGrade == TopGrade;
}

/// <summary>The blueprints an engineer offers on one module type.</summary>
/// <param name="ModuleType">The module type's page.</param>
/// <param name="Group">The module group label.</param>
/// <param name="Offers">One per blueprint.</param>
public sealed record EngineerOfferGroup(Link ModuleType, string Group, IReadOnlyList<EngineerOfferRow> Offers);

/// <summary>One plan entry this engineer can work on.</summary>
/// <param name="Title">Module and blueprint.</param>
/// <param name="Note">What they can do for it.</param>
/// <param name="IsFull">Whether they can roll the target grade and apply the experimental.</param>
public sealed record EngineerPlanRow(string Title, string Note, bool IsFull);

/// <summary>One engineer: where they are, what they offer, and what of your plan they can serve.</summary>
public sealed class EngineerPage : Page
{
    private readonly Core.EngineerProfile _profile;

    /// <summary>Creates the page.</summary>
    /// <param name="profile">The engineer, from the catalog.</param>
    /// <param name="workspace">The shared state.</param>
    public EngineerPage(Core.EngineerProfile profile, Workspace workspace)
        : base(new PageKey(PageKind.Engineer, KeyOf(profile)), workspace)
    {
        _profile = profile;
        Title = _profile.Name;
        Breadcrumbs = [LinkTo("Engineers", PageKind.Engineers)];
        Kicker = Display.RegionLabel(_profile.Region);
        Where = new Place(LinkTo(_profile.System, PageKind.System, _profile.System), _profile.Base);
        Refresh();
    }

    /// <summary>Gets where they are: the system's link and the base.</summary>
    public Place Where { get; }

    /// <summary>The page id of an engineer: the game's engineer id as decimal text, which <see cref="Find"/> reads back.</summary>
    /// <param name="profile">The engineer.</param>
    /// <returns>The id, such as <c>300100</c>.</returns>
    public static string KeyOf(Core.EngineerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile.Id.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>The engineer a page id names.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="id">A page id from <see cref="KeyOf"/>.</param>
    /// <returns>The engineer, or <see langword="null"/> when the id is not a number the catalog knows.</returns>
    public static Core.EngineerProfile? Find(Core.Catalog catalog, string id)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(id);
        return long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && catalog.EngineersById.TryGetValue(number, out var profile) ? profile : null;
    }

    /// <summary>Gets the distance from the ship, with the system you are in as a link; or why the distance is unknown.</summary>
    public IReadOnlyList<PhrasePart> DistanceParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets or sets whether the commander will visit this engineer.</summary>
    public bool Selected
    {
        get => Workspace.Engineers.Contains(_profile.Name);
        set
        {
            if (value != Selected)
            {
                Workspace.Engineers.Set(_profile.Name, value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets what the engineer offers, by module type.</summary>
    public IReadOnlyList<EngineerOfferGroup> Offers { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets the plan entries this engineer can work on.</summary>
    public IReadOnlyList<EngineerPlanRow> PlanRows { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether the plan has anything for this engineer.</summary>
    public bool HasPlanRows => PlanRows.Count > 0;

    /// <summary>Gets how many of your modules are stored in the engineer's system, the system as a link; empty with none.</summary>
    public IReadOnlyList<PhrasePart> StoredParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets a line on whether the engineer's system is on your route.</summary>
    public string RouteText { get; private set => SetProperty(ref field, value); } = "";

    /// <inheritdoc/>
    public override void Refresh()
    {
        var catalog = Workspace.Catalog;
        DistanceParts = Workspace.DistanceFromHere(Workspace.PositionOf(_profile)) is { } ly && Workspace.Route.CurrentSystem is { } here
            ? Phrase.Of($"{Display.LightYears(ly)} from ", LinkTo(here, PageKind.System, here))
            : Phrase.Text("Distance unknown until the journal reports your position.");
        OnPropertyChanged(nameof(Selected));

        Offers =
        [
            .. catalog.OffersOf(_profile.Name)
                .GroupBy(offer => offer.ModuleType)
                .Select(group =>
                {
                    var type = catalog.ModuleTypes[group.Key];
                    return new EngineerOfferGroup(
                        LinkTo(type.Name, PageKind.ModuleType, type.Code),
                        Display.GroupLabel(type.Group),
                        [
                            .. group.Select(offer => new EngineerOfferRow(
                                catalog.Blueprints[offer.Blueprint].Name,
                                offer.MaxGrade,
                                catalog.Blueprints[offer.Blueprint].MaxGrade,
                                new RelayCommand(() => Workspace.Plan.Add([PlanEntry.Create(catalog, type.Code, offer.Blueprint, offer.MaxGrade, null, PlanSource.Manual)])))),
                        ]);
                }),
        ];

        var rows = new List<EngineerPlanRow>();
        foreach (var entry in Workspace.Plan.Snapshot().Where(entry => entry.Enabled))
        {
            var service = EngineerCoverage.ServiceFor(catalog, entry, Workspace.Engineers.Selected);
            var canRoll = service.CanRoll.Contains(_profile.Name, StringComparer.Ordinal);
            var canApply = entry.Experimental is null || service.CanApplyExperimental.Contains(_profile.Name, StringComparer.Ordinal);
            var offer = entry.ModuleType is { } code ? catalog.OffersOf(_profile.Name).FirstOrDefault(offer => offer.ModuleType == code && offer.Blueprint == entry.Blueprint) : null;
            if (offer is null)
            {
                continue;
            }

            var title = $"{(entry.ModuleType is { } type ? catalog.ModuleTypes[type].Name : "Unknown module")}: {catalog.Blueprints[entry.Blueprint].Name} {Display.Grade(entry.TargetGrade)}";
            var note = canRoll
                ? (canApply ? "Rolls the target grade" + (entry.Experimental is null ? "." : " and applies the experimental.") : "Rolls the target grade; the experimental needs someone else.")
                : $"Stops at {Display.Grade(offer.MaxGrade)}; another engineer finishes it.";
            rows.Add(new EngineerPlanRow(title, note, canRoll && canApply));
        }

        PlanRows = rows;
        OnPropertyChanged(nameof(HasPlanRows));

        var stored = Workspace.FleetSnapshot.StoredModules.Count(module => string.Equals(module.System, _profile.System, StringComparison.OrdinalIgnoreCase));
        StoredParts = stored == 0 ? Phrase.Empty : Phrase.Of($"You have {Display.Plural(stored, "module")} stored in ", LinkTo(_profile.System, PageKind.System, _profile.System), ".");

        var stops = Workspace.Monitor.Route?.Stops ?? [];
        var index = stops.ToList().FindIndex(stop => string.Equals(stop.StarSystem, _profile.System, StringComparison.OrdinalIgnoreCase));
        RouteText = index < 0 ? "" : index == 0 ? "Your route starts here." : $"Stop {index + 1} of {stops.Count} on your route.";
    }
}
