using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Pages;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core.Search;

namespace Katey.App.Search;

/// <summary>One search hit.</summary>
/// <param name="Link">What it opens.</param>
/// <param name="Kind">"Engineer", "Module", "Blueprint", "Material", "System", "Ship" or "Fit".</param>
/// <param name="Subtitle">Where or what it is.</param>
public sealed record SearchResult(Link Link, string Kind, string Subtitle);

/// <summary>
/// The search box: one query over engineers, module types, blueprints, materials, the systems you have
/// visited, your ships and your saved fits.
/// </summary>
public sealed class SearchViewModel : ObservableObject
{
    private const int Limit = 12;

    private readonly Workspace _workspace;
    private TextIndex<Func<SearchResult>> _catalogIndex = new();
    private TextIndex<Func<SearchResult>> _journalIndex = new();
    private (int Systems, int Ships, int Fits) _journalShape = (-1, -1, -1);

    /// <summary>Creates the search over a workspace.</summary>
    /// <param name="workspace">The shared state.</param>
    public SearchViewModel(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        _workspace = workspace;
        OpenFirstCommand = new RelayCommand(OpenFirst, () => Results.Count > 0);
        ClearCommand = new RelayCommand(() => Query = "");
        BuildCatalogIndex();
        RefreshJournalIndex();
    }

    /// <summary>Gets or sets the text typed. Results follow it.</summary>
    public string Query
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? ""))
            {
                Run();
            }
        }
    } = "";

    /// <summary>Gets the hits, best first.</summary>
    public IReadOnlyList<SearchResult> Results { get; private set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether there is a query with hits.</summary>
    public bool HasResults => Results.Count > 0;

    /// <summary>Gets whether there is a query but nothing matched.</summary>
    public bool IsEmpty => Query.Trim().Length > 0 && Results.Count == 0;

    /// <summary>Gets the command that opens the best hit and clears the box.</summary>
    public RelayCommand OpenFirstCommand { get; }

    /// <summary>Gets the command that clears the box.</summary>
    public RelayCommand ClearCommand { get; }

    /// <summary>Opens a hit and clears the box.</summary>
    /// <param name="result">One of <see cref="Results"/>.</param>
    public void Open(SearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        result.Link.OpenCommand.Execute(null);
        Query = "";
    }

    /// <summary>Rebuilds the whole index, as after a language change, when every label and kind in it is stale.</summary>
    public void Rebuild()
    {
        _catalogIndex = new TextIndex<Func<SearchResult>>();
        BuildCatalogIndex();
        _journalShape = (-1, -1, -1);
        RefreshJournalIndex();
    }

    /// <summary>Rebuilds the journal half of the index when the ships, fits or visited systems changed.</summary>
    public void RefreshJournalIndex()
    {
        var route = _workspace.Route;
        var fleet = _workspace.FleetSnapshot;
        var shape = (route.KnownSystems.Count, fleet.Ships.Count, _workspace.Fleet.Fittings.Count);
        if (shape == _journalShape)
        {
            return;
        }

        _journalShape = shape;
        var index = new TextIndex<Func<SearchResult>>();
        foreach (var system in route.KnownSystems.Values)
        {
            var name = system.Name;
            var last = system.LastVisit;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            index.Add(name, () => new SearchResult(Link(name, PageKind.System, name), Strings.Get(Keys.Search_KindSystem), Strings.Format(Keys.Search_LastVisited, ("when", last.ToLocalTime()))));
        }

        foreach (var ship in fleet.Ships)
        {
            var hull = Names.Hull(ship.Type, ship.TypeName);
            var title = ship.Name.Length > 0 ? ship.Name : hull;
            var id = ship.ShipId.ToString(CultureInfo.InvariantCulture);
            var subtitle = ship.Name.Length > 0 ? (ship.Ident.Length > 0 ? Strings.Format(Keys.Search_ShipIdent, ("hull", hull), ("ident", ship.Ident)) : hull) : Strings.Get(Keys.Search_Unnamed);
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            index.Add(title, () => new SearchResult(Link(title, PageKind.Ship, id), Strings.Get(Keys.Search_KindShip), subtitle), hull + " " + ship.Ident);
        }

        foreach (var fit in _workspace.Fleet.Fittings)
        {
            var fitting = fit;
            var hullName = Names.Hull(fitting.Hull, null);
            if (string.IsNullOrWhiteSpace(fitting.Name))
            {
                continue;
            }

            index.Add(fitting.Name, () => new SearchResult(Link(fitting.Name, PageKind.Fit, fitting.Id), Strings.Get(Keys.Search_KindFit), hullName), hullName);
        }

        _journalIndex = index;
        if (Query.Length > 0)
        {
            Run();
        }
    }

    private Link Link(string label, PageKind kind, string id) => _workspace.Navigator.LinkTo(label, kind, id);

    private void BuildCatalogIndex()
    {
        var catalog = _workspace.Catalog;
        foreach (var engineer in catalog.Engineers.Values)
        {
            var profile = engineer;
            _catalogIndex.Add(profile.Name, () => new SearchResult(Link(profile.Name, PageKind.Engineer, EngineerPage.KeyOf(profile)), Strings.Get(Keys.Search_KindEngineer), Strings.Format(Keys.Search_Place, ("system", profile.System), ("base", profile.Base))), profile.System + " " + profile.Base);
        }

        foreach (var type in catalog.ModuleTypes.Values.Where(type => type.IsEngineerable))
        {
            var moduleType = type;
            _catalogIndex.Add(Names.ModuleType(moduleType), () => new SearchResult(Link(Names.ModuleType(moduleType), PageKind.ModuleType, moduleType.Code), Strings.Get(Keys.Search_KindModule), Display.GroupLabel(moduleType.Group)), moduleType.Code + " " + moduleType.Name + " " + Display.GroupLabel(moduleType.Group));
        }

        foreach (var blueprint in catalog.Blueprints.Values)
        {
            var key = blueprint.Key;
            var name = Names.Blueprint(blueprint);
            var types = Display.List(catalog.ModuleTypesWithBlueprint(key).Select(Names.ModuleType));
            _catalogIndex.Add(name, () => new SearchResult(Link(name, PageKind.Blueprint, key), Strings.Get(Keys.Search_KindBlueprint), types.Length > 0 ? types : key), key + " " + blueprint.Name);
        }

        foreach (var unlock in catalog.Unlocks.Values)
        {
            var found = unlock;
            var section = found.Broker.ToString().ToLowerInvariant();
            _catalogIndex.Add(Names.Unlock(found), () => new SearchResult(Link(Names.Unlock(found), PageKind.Unlocks, section), Strings.Get(Keys.Search_KindUnlock), UnlocksPage.BrokerLabel(found.Broker)), found.Key.Replace('_', ' ') + " " + found.Name + " tech broker");
        }

        foreach (var material in catalog.Materials.Values)
        {
            var item = material;
            _catalogIndex.Add(Names.Material(item), () => new SearchResult(Link(Names.Material(item), PageKind.Material, item.Symbol), Strings.Get(Keys.Search_KindMaterial), Strings.Format(Keys.Search_MaterialDetail, ("grade", Display.Grade(item.Grade)), ("category", Display.CategoryLabel(item.Category)), ("line", Names.Family(item.Line)))), item.Symbol + " " + item.Name + " " + item.Line + " " + Names.Family(item.Line));
        }
    }

    private void Run()
    {
        var query = Query.Trim();
        Results = query.Length == 0
            ? []
            : [
                .. _catalogIndex.Query(query, Limit)
                    .Concat(_journalIndex.Query(query, Limit))
                    .OrderByDescending(hit => hit.Score)
                    .Take(Limit)
                    .Select(hit => hit.Item()),
            ];

        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(IsEmpty));
        OpenFirstCommand.RaiseCanExecuteChanged();
    }

    private void OpenFirst()
    {
        if (Results.Count > 0)
        {
            Open(Results[0]);
        }
    }
}
