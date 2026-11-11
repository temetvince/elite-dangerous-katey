using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core;

namespace Katey.App.Pages;

/// <summary>One engineer in the index, with the tick that says you will visit them.</summary>
public sealed class EngineerListRow : ObservableObject
{
    private readonly EngineerSelection _selection;
    private bool _selected;

    /// <summary>Creates the row.</summary>
    /// <param name="profile">The engineer.</param>
    /// <param name="link">Their page.</param>
    /// <param name="systemLink">Their system's page.</param>
    /// <param name="distanceLy">Distance from the ship, or <see langword="null"/>.</param>
    /// <param name="offerCount">How many blueprint-on-module-type pairs they offer.</param>
    /// <param name="selection">The selection the tick edits.</param>
    /// <param name="narrowed">Whether the engineers switch narrows the lists to the ones you ticked, which colours the name.</param>
    public EngineerListRow(EngineerProfile profile, Link link, Link systemLink, double? distanceLy, int offerCount, EngineerSelection selection, bool narrowed)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(systemLink);
        ArgumentNullException.ThrowIfNull(selection);

        Name = profile.Name;
        Link = link;
        System = profile.System;
        Base = profile.Base;
        Where = new Place(systemLink, profile.Base);
        Region = Display.RegionLabel(profile.Region);
        DistanceLy = distanceLy;
        DistanceText = Display.DistanceOrUnknown(distanceLy);
        OfferCount = offerCount;
        OffersText = Display.Plural(Keys.Plural_Blueprints, offerCount);
        _selection = selection;
        _selected = selection.Contains(profile.Name);
        Narrowed = narrowed;
    }

    /// <summary>Gets whether the engineers switch narrows the lists to the ones you ticked.</summary>
    public bool Narrowed { get; }

    /// <summary>Gets whether the name takes the link colour: ticked while the switch is on.</summary>
    public bool Highlighted => Narrowed && Selected;

    /// <summary>Gets whether the name is set back: not ticked while the switch is on.</summary>
    public bool Dimmed => Narrowed && !Selected;

    /// <summary>Gets the engineer's name.</summary>
    public string Name { get; }

    /// <summary>Gets their page.</summary>
    public Link Link { get; }

    /// <summary>Gets the star system.</summary>
    public string System { get; }

    /// <summary>Gets the workshop.</summary>
    public string Base { get; }

    /// <summary>Gets where they are: the system's link and the base.</summary>
    public Place Where { get; }

    /// <summary>Gets the region label.</summary>
    public string Region { get; }

    /// <summary>Gets the distance from the ship, or <see langword="null"/>.</summary>
    public double? DistanceLy { get; }

    /// <summary>Gets the distance as text.</summary>
    public string DistanceText { get; }

    /// <summary>Gets how many blueprint-on-module-type pairs they offer.</summary>
    public int OfferCount { get; }

    /// <summary>Gets how much they offer, as text.</summary>
    public string OffersText { get; }

    /// <summary>Gets or sets whether the commander will visit this engineer.</summary>
    public bool Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                _selection.Set(Name, value);
                OnPropertyChanged(nameof(Highlighted));
                OnPropertyChanged(nameof(Dimmed));
            }
        }
    }
}

/// <summary>Every engineer, with a search box, sortable columns, the visit tick, and the app-wide switch that narrows every index and picker to what the selection can roll.</summary>
public sealed class EngineersPage : Page
{
    /// <summary>Creates the page.</summary>
    /// <param name="workspace">The shared state.</param>
    public EngineersPage(Workspace workspace)
        : base(new PageKey(PageKind.Engineers), workspace)
    {
        Title = Strings.Get(Keys.Nav_Engineers);
        Kicker = Strings.Get(Keys.Page_KickerBrowse);
        SelectAllCommand = new RelayCommand(() => workspace.Engineers.SetAll(true));
        SelectNoneCommand = new RelayCommand(() => workspace.Engineers.SetAll(false));
        Sort = new TableSort<EngineerListRow>(
            [
                new("selected", Strings.Get(Keys.Column_Visit), row => row.Selected ? 0 : 1),
                new("name", Strings.Get(Keys.Column_Engineer), row => row.Name),
                new("place", Strings.Get(Keys.Column_Where), row => row.System),
                new("region", Strings.Get(Keys.Column_Region), row => row.Region),
                new("offers", Strings.Get(Keys.Column_Offers), row => row.OfferCount, DescendingFirst: true),
                new("distance", Strings.Get(Keys.Column_Distance), row => row.DistanceLy),
            ],
            "distance",
            workspace.Preferences,
            "sort.engineers");
        Sort.Changed += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>Gets or sets a filter on name, system and base. Empty shows everyone.</summary>
    public string Filter
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? ""))
            {
                Refresh();
            }
        }
    } = "";

    /// <summary>Gets the engineer table's sort.</summary>
    public TableSort<EngineerListRow> Sort { get; }

    /// <summary>
    /// Gets or sets the app-wide switch: on, the modules index and every blueprint, grade and experimental
    /// picker offer only what the selected engineers can roll, and grades start at the grade they reach.
    /// Off, everything is offered and grades start at the top. Remembered. Does nothing while nobody is selected.
    /// </summary>
    public bool OnlyMyEngineers
    {
        get => Workspace.Scope.OnlyMyEngineers;
        set
        {
            if (value != OnlyMyEngineers)
            {
                Workspace.Scope.OnlyMyEngineers = value;
                OnPropertyChanged();
                Refresh();
            }
        }
    }

    /// <summary>Gets what the switch does, so it is an informed choice.</summary>
    public static string OnlyMyEngineersText => Strings.Get(Keys.Engineers_OnlyMineText);

    /// <summary>Gets one line on what the switch is doing right now.</summary>
    public string ScopeStatus { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the engineers that pass the filter, in sort order.</summary>
    public IReadOnlyList<EngineerListRow> Engineers { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets a line on the selection.</summary>
    public string SelectionText { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Gets where the distances are measured from, the system as a link; or that they wait for a position.</summary>
    public IReadOnlyList<PhrasePart> FromParts { get; private set => SetProperty(ref field, value); } = Phrase.Empty;

    /// <summary>Gets the command that selects every engineer.</summary>
    public RelayCommand SelectAllCommand { get; }

    /// <summary>Gets the command that clears the selection.</summary>
    public RelayCommand SelectNoneCommand { get; }

    /// <inheritdoc/>
    public override void Refresh()
    {
        var catalog = Workspace.Catalog;
        var here = Workspace.Route.CurrentSystem;
        var filter = Filter.Trim();
        Engineers = Sort.Apply(catalog.Engineers.Values
            .Where(profile => filter.Length == 0
                || profile.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || profile.System.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || profile.Base.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(profile => profile.Name, StringComparer.Ordinal)
            .Select(profile => new EngineerListRow(
                profile,
                LinkTo(profile.Name, PageKind.Engineer, EngineerPage.KeyOf(profile)),
                LinkTo(profile.System, PageKind.System, profile.System),
                Workspace.DistanceFromHere(Workspace.PositionOf(profile)),
                catalog.OffersOf(profile.Name).Count,
                Workspace.Engineers,
                Workspace.Scope.IsNarrowed)));
        SelectionText = Workspace.Engineers.Count == 0
            ? Strings.Get(Keys.Engineers_NobodySelected)
            : Strings.Format(Keys.Engineers_Selected, ("engineers", Display.Plural(Keys.Plural_Engineers, Workspace.Engineers.Count)));
        FromParts = here is null ? Phrase.Text(Strings.Get(Keys.Engineers_DistancesWhenKnown)) : Phrase.Format(Keys.Engineers_DistancesFrom, ("system", LinkTo(here, PageKind.System, here)));
        OnPropertyChanged(nameof(OnlyMyEngineers));
        ScopeStatus = !OnlyMyEngineers
            ? Strings.Get(Keys.Engineers_ScopeOff)
            : Workspace.Engineers.Count == 0
                ? Strings.Get(Keys.Engineers_ScopeOnNobody)
                : Strings.Format(Keys.Engineers_ScopeOn, ("engineers", Display.Plural(Keys.Plural_TickedEngineers, Workspace.Engineers.Count)));
    }
}
