namespace Katey.App.Navigation;

/// <summary>The kinds of page the workbench can show.</summary>
public enum PageKind
{
    /// <summary>The start page. Id is empty.</summary>
    Home,

    /// <summary>Every engineer. Id is empty.</summary>
    Engineers,

    /// <summary>One engineer. Id is the game's engineer id as decimal text, <see cref="Pages.EngineerPage.KeyOf"/>.</summary>
    Engineer,

    /// <summary>Every module type, by group. Id is empty.</summary>
    Modules,

    /// <summary>One module type. Id is the module type code.</summary>
    ModuleType,

    /// <summary>One blueprint across the module types it applies to. Id is the blueprint key.</summary>
    Blueprint,

    /// <summary>Every material with the live inventory. Id is empty.</summary>
    Materials,

    /// <summary>The technology broker unlocks, Guardian and human. Id is empty, or the section key.</summary>
    Unlocks,

    /// <summary>One material. Id is the material's journal symbol.</summary>
    Material,

    /// <summary>Ships, stored modules and saved fits. Id is empty.</summary>
    Fleet,

    /// <summary>One owned ship. Id is the ship id as text.</summary>
    Ship,

    /// <summary>One saved fit. Id is the fit id.</summary>
    Fit,

    /// <summary>A star system: the current one in full, any visited one in brief. Id is the system name, or empty for the current system.</summary>
    System,

    /// <summary>The plotted route. Id is empty, or the route page's section key.</summary>
    Route,

    /// <summary>Income, activity and lifetime statistics. Id is empty.</summary>
    Statistics,

    /// <summary>The journal folder and the watch. Id is empty.</summary>
    Settings,
}

/// <summary>Names one page: what kind, and which one.</summary>
/// <param name="Kind">The page kind.</param>
/// <param name="Id">The entity id the kind documents, or empty for an index page.</param>
public sealed record PageKey(PageKind Kind, string Id = "")
{
    /// <summary>Gets the start page's key.</summary>
    public static PageKey Home { get; } = new(PageKind.Home);
}
