using Katey.App.Mvvm;

namespace Katey.App.Navigation;

/// <summary>
/// One navbar entry: an index page, lit while the current page belongs to its section. Its command opens the page,
/// and closes it again when pressed while that very page is open, by going back to the page before it.
/// </summary>
public sealed class NavItem : ObservableObject
{
    private readonly IReadOnlySet<PageKind> _section;

    /// <summary>Creates the entry.</summary>
    /// <param name="link">The index page the entry opens.</param>
    /// <param name="section">The page kinds that light the entry.</param>
    /// <param name="navigator">The navigator the command drives; the same one <paramref name="link"/> opens through.</param>
    public NavItem(Link link, IReadOnlySet<PageKind> section, Navigator navigator)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(navigator);
        Link = link;
        _section = section;
        Command = new RelayCommand(() =>
        {
            if (navigator.IsAt(link.Target) && navigator.CanGoBack)
            {
                navigator.BackCommand.Execute(null);
            }
            else
            {
                link.OpenCommand.Execute(null);
            }
        });
    }

    /// <summary>Gets the index page the entry opens.</summary>
    public Link Link { get; }

    /// <summary>Gets the text shown.</summary>
    public string Label => Link.Label;

    /// <summary>Gets the command the entry's button runs: open the page, or go back when its page is already the current one and there is a page to go back to.</summary>
    public RelayCommand Command { get; }

    /// <summary>Gets whether the current page belongs to this entry's section.</summary>
    public bool IsCurrent { get; private set => SetProperty(ref field, value); }

    /// <summary>Lights or dims the entry for a page kind.</summary>
    /// <param name="current">The current page's kind.</param>
    public void Update(PageKind current) => IsCurrent = _section.Contains(current);
}
