using Katey.App.Mvvm;
using Katey.App.Pages;

namespace Katey.App.Navigation;

/// <summary>
/// The workbench's history: which page is showing, the pages behind and ahead of it, and how far down
/// each was scrolled. Pages are built fresh each time they are opened and refreshed in place while
/// they show.
/// </summary>
/// <remarks>Not thread-safe; call on the UI thread. Shows nothing until the first <see cref="Open"/>.</remarks>
public sealed class Navigator : ObservableObject
{
    private const int MaxHistory = 50;

    private readonly Func<PageKey, Page> _build;
    private readonly List<HistoryEntry> _history = [];
    private int _index = -1;
    private Page? _current;

    /// <summary>Creates the navigator with no page showing.</summary>
    /// <param name="build">Makes the page for a key. Must return a page for every key.</param>
    public Navigator(Func<PageKey, Page> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        _build = build;
        BackCommand = new RelayCommand(Back, () => CanGoBack);
        ForwardCommand = new RelayCommand(Forward, () => CanGoForward);
    }

    /// <summary>Raised just before <see cref="Current"/> changes, while the old page still shows. The view records its scroll position here.</summary>
    public event EventHandler? Navigating;

    /// <summary>Raised after <see cref="Current"/> changes.</summary>
    public event EventHandler? Navigated;

    /// <summary>
    /// Gets how far down the current page should be scrolled, as a fraction of its scrollable height
    /// from 0 to 1. A fraction survives a change of window size better than a pixel offset. Zero for a
    /// page just opened; the recorded value for a page reached by back or forward.
    /// </summary>
    public double ScrollFraction => _index >= 0 && _index < _history.Count ? _history[_index].ScrollFraction : 0;

    /// <summary>Gets the page showing.</summary>
    /// <exception cref="InvalidOperationException">Nothing has been opened yet.</exception>
    public Page Current => _current ?? throw new InvalidOperationException("No page has been opened yet.");

    /// <summary>Gets whether there is a page behind the current one.</summary>
    public bool CanGoBack => _index > 0;

    /// <summary>Whether a page is the current one.</summary>
    /// <param name="key">The page.</param>
    /// <returns><see langword="true"/> when the current page has this key; <see langword="false"/> before the first <see cref="Open"/>.</returns>
    public bool IsAt(PageKey key) => _current is not null && key == _current.Key;

    /// <summary>Gets whether there is a page ahead of the current one.</summary>
    public bool CanGoForward => _index < _history.Count - 1;

    /// <summary>Gets the command that returns to the previous page.</summary>
    public RelayCommand BackCommand { get; }

    /// <summary>Gets the command that goes to the page left by <see cref="Back"/>.</summary>
    public RelayCommand ForwardCommand { get; }

    /// <summary>Opens a page, dropping any pages ahead in the history. Opening the current page again refreshes it.</summary>
    /// <param name="key">The page.</param>
    public void Open(PageKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_current is not null && key == _current.Key)
        {
            _current.Refresh();
            return;
        }

        Navigating?.Invoke(this, EventArgs.Empty);
        _history.RemoveRange(_index + 1, _history.Count - _index - 1);
        _history.Add(new HistoryEntry(key));
        if (_history.Count > MaxHistory)
        {
            _history.RemoveAt(0);
        }

        _index = _history.Count - 1;
        Show(key);
    }

    /// <summary>Records how far down the current page is scrolled, for when it is returned to.</summary>
    /// <param name="fraction">Scroll offset over scrollable height, 0 to 1. Values outside are clamped.</param>
    public void RecordScroll(double fraction)
    {
        if (_index >= 0 && _index < _history.Count)
        {
            _history[_index].ScrollFraction = double.IsFinite(fraction) ? Math.Clamp(fraction, 0, 1) : 0;
        }
    }

    /// <summary>A link to a page.</summary>
    /// <param name="label">The text shown.</param>
    /// <param name="kind">The page kind.</param>
    /// <param name="id">The entity id, or empty.</param>
    /// <returns>The link.</returns>
    public Link LinkTo(string label, PageKind kind, string id = "") => new(label, new PageKey(kind, id), this);

    /// <summary>Rebuilds the current page's content from the live data. Does nothing before the first <see cref="Open"/>.</summary>
    public void Refresh() => _current?.Refresh();

    /// <summary>
    /// Builds the current page again from its key, as after a language change, when every word on it is stale.
    /// The history and its scroll positions are kept. Does nothing before the first <see cref="Open"/>.
    /// </summary>
    public void Rebuild()
    {
        if (_current is not null)
        {
            Show(_current.Key);
        }
    }

    private void Back()
    {
        if (CanGoBack)
        {
            Navigating?.Invoke(this, EventArgs.Empty);
            _index--;
            Show(_history[_index].Key);
        }
    }

    private void Forward()
    {
        if (CanGoForward)
        {
            Navigating?.Invoke(this, EventArgs.Empty);
            _index++;
            Show(_history[_index].Key);
        }
    }

    private void Show(PageKey key)
    {
        _current = _build(key);
        OnPropertyChanged(nameof(Current));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
        BackCommand.RaiseCanExecuteChanged();
        ForwardCommand.RaiseCanExecuteChanged();
        Navigated?.Invoke(this, EventArgs.Empty);
    }

    private sealed class HistoryEntry(PageKey key)
    {
        public PageKey Key { get; } = key;

        public double ScrollFraction { get; set; }
    }
}
