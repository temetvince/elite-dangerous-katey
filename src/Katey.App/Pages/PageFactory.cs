using Katey.App.Navigation;

namespace Katey.App.Pages;

/// <summary>Builds the page for a key, or a not-found page when the key names nothing the data has.</summary>
public static class PageFactory
{
    /// <summary>Builds a page.</summary>
    /// <param name="key">What to show.</param>
    /// <param name="workspace">The shared state.</param>
    /// <returns>The page. Never <see langword="null"/>.</returns>
    public static Page Build(PageKey key, Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(workspace);
        var catalog = workspace.Catalog;
        return key.Kind switch
        {
            PageKind.Home => new HomePage(workspace),
            PageKind.Engineers => new EngineersPage(workspace),
            PageKind.Engineer when EngineerPage.Find(catalog, key.Id) is { } engineer => new EngineerPage(engineer, workspace),
            PageKind.Modules => new ModulesPage(workspace, key.Id),
            PageKind.ModuleType when catalog.ModuleTypes.ContainsKey(key.Id) => new ModuleTypePage(key.Id, workspace),
            PageKind.Blueprint when catalog.Blueprints.ContainsKey(key.Id) => new BlueprintPage(key.Id, workspace),
            PageKind.Materials => new MaterialsPage(workspace, key.Id),
            PageKind.Unlocks => new UnlocksPage(workspace, key.Id),
            PageKind.Material when catalog.Materials.ContainsKey(key.Id) => new MaterialPage(key.Id, workspace),
            PageKind.Fleet => new FleetPage(workspace),
            PageKind.Ship when ShipPage.Find(workspace.FleetSnapshot, key.Id) is not null => new ShipPage(key.Id, workspace),
            PageKind.Fit when workspace.Fleet.Find(key.Id) is not null => new FitPage(key.Id, workspace),
            PageKind.System => new SystemPage(key.Id, workspace),
            PageKind.Route => new RoutePage(workspace, key.Id),
            PageKind.Statistics => new StatisticsPage(workspace),
            PageKind.Settings => new SettingsPage(workspace),
            _ => new NotFoundPage(key, workspace),
        };
    }
}
