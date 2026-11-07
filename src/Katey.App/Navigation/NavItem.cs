using Katey.App.Mvvm;

namespace Katey.App.Navigation;

/// <summary>One section in the navbar: an index page, lit while the current page belongs to its section.</summary>
public sealed class NavItem : ObservableObject
{
    private readonly IReadOnlySet<PageKind> _section;

    /// <summary>Creates the item.</summary>
    /// <param name="link">The index page it opens.</param>
    /// <param name="section">Every page kind that counts as being inside this section, the index itself included.</param>
    public NavItem(Link link, IReadOnlySet<PageKind> section)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(section);
        Link = link;
        _section = section;
    }

    /// <summary>Gets the index page it opens.</summary>
    public Link Link { get; }

    /// <summary>Gets the text shown.</summary>
    public string Label => Link.Label;

    /// <summary>Gets whether the page showing belongs to this section.</summary>
    public bool IsCurrent { get; private set => SetProperty(ref field, value); }

    /// <summary>Recomputes <see cref="IsCurrent"/> for the page showing.</summary>
    /// <param name="current">The current page's kind.</param>
    public void Update(PageKind current) => IsCurrent = _section.Contains(current);
}
