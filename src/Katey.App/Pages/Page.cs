using Katey.App.Mvvm;
using Katey.App.Navigation;

namespace Katey.App.Pages;

/// <summary>
/// One page of the workbench. A page is built for a key, fills itself from the catalogs and the live
/// data in <see cref="Refresh"/>, and is refreshed again whenever that data changes while it shows.
/// </summary>
public abstract class Page : ObservableObject
{
    /// <summary>Creates the page.</summary>
    /// <param name="key">What it shows.</param>
    /// <param name="workspace">The shared state it reads.</param>
    protected Page(PageKey key, Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(workspace);

        Key = key;
        Workspace = workspace;
    }

    /// <summary>Gets what the page shows.</summary>
    public PageKey Key { get; }

    /// <summary>Gets the heading.</summary>
    public string Title { get; protected set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the small label above the heading, such as the category the entity belongs to. Shown after <see cref="Breadcrumbs"/>.</summary>
    public string Kicker { get; protected set => SetProperty(ref field, value); } = "";

    /// <summary>Gets the pages this one sits under, outermost first, shown as links above the heading. Empty for an index page.</summary>
    public IReadOnlyList<Link> Breadcrumbs { get; protected set => SetProperty(ref field, value); } = [];

    /// <summary>Gets whether there is a kicker to show.</summary>
    public bool HasKicker => Kicker.Length > 0;

    /// <summary>Gets the shared state.</summary>
    protected Workspace Workspace { get; }

    /// <summary>Gets the navigator, for links.</summary>
    protected Navigator Navigator => Workspace.Navigator;

    /// <summary>Rebuilds every bound property from the current data.</summary>
    public abstract void Refresh();

    /// <summary>A link to a page.</summary>
    /// <param name="label">The text shown.</param>
    /// <param name="kind">The page kind.</param>
    /// <param name="id">The entity id, or empty.</param>
    /// <returns>The link.</returns>
    protected Link LinkTo(string label, PageKind kind, string id = "") => Navigator.LinkTo(label, kind, id);
}
