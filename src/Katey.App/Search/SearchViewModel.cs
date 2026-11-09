using System.Globalization;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Pages;
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
    private readonly TextIndex<Func<SearchResult>> _catalogIndex = new();
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

            index.Add(name, () => new SearchResult(Link(name, PageKind.System, name), "System", string.Create(CultureInfo.CurrentCulture, $"last visited {last.ToLocalTime():g}")));
        }

        foreach (var ship in fleet.Ships)
        {
            var hull = Core.Fleet.ShipNames.Prefer(ship.Type, ship.TypeName);
            var title = ship.Name.Length > 0 ? ship.Name : hull;
            var id = ship.ShipId.ToString(CultureInfo.InvariantCulture);
            var subtitle = ship.Name.Length > 0 ? hull + (ship.Ident.Length > 0 ? " · " + ship.Ident : "") : "unnamed";
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            index.Add(title, () => new SearchResult(Link(title, PageKind.Ship, id), "Ship", subtitle), hull + " " + ship.Ident);
        }

        foreach (var fit in _workspace.Fleet.Fittings)
        {
            var fitting = fit;
            var hullName = Core.Fleet.ShipNames.Display(fitting.Hull);
            if (string.IsNullOrWhiteSpace(fitting.Name))
            {
                continue;
            }

            index.Add(fitting.Name, () => new SearchResult(Link(fitting.Name, PageKind.Fit, fitting.Id), "Fit", hullName), hullName);
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
            _catalogIndex.Add(profile.Name, () => new SearchResult(Link(profile.Name, PageKind.Engineer, profile.Name), "Engineer", $"{profile.System} · {profile.Base}"), profile.System + " " + profile.Base);
        }

        foreach (var type in catalog.ModuleTypes.Values.Where(type => type.IsEngineerable))
        {
            var moduleType = type;
            _catalogIndex.Add(moduleType.Name, () => new SearchResult(Link(moduleType.Name, PageKind.ModuleType, moduleType.Code), "Module", Display.GroupLabel(moduleType.Group)), moduleType.Code + " " + Display.GroupLabel(moduleType.Group));
        }

        foreach (var blueprint in catalog.Blueprints.Values)
        {
            var key = blueprint.Key;
            var name = blueprint.Name;
            var types = string.Join(", ", catalog.ModuleTypesWithBlueprint(key).Select(type => type.Name));
            _catalogIndex.Add(name, () => new SearchResult(Link(name, PageKind.Blueprint, key), "Blueprint", types.Length > 0 ? types : key), key);
        }

        foreach (var unlock in catalog.Unlocks.Values)
        {
            var found = unlock;
            var section = found.Broker.ToString().ToLowerInvariant();
            _catalogIndex.Add(found.Name, () => new SearchResult(Link(found.Name, PageKind.Unlocks, section), "Unlock", UnlocksPage.BrokerLabel(found.Broker)), found.Key.Replace('_', ' ') + " tech broker");
        }

        foreach (var material in catalog.Materials.Values)
        {
            var item = material;
            _catalogIndex.Add(item.Name, () => new SearchResult(Link(item.Name, PageKind.Material, item.Name), "Material", $"{Display.Grade(item.Grade)} · {Core.Planning.ShoppingList.CategoryLabels[item.Category]} · {item.Line}"), item.Symbol + " " + item.Line);
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
