namespace Katey.App.Mvvm;

/// <summary>One section of a page: a tab the view shows while it is current.</summary>
public sealed class SectionItem : ObservableObject
{
    /// <summary>Creates the item.</summary>
    /// <param name="key">The section key.</param>
    /// <param name="label">The text shown on the tab.</param>
    /// <param name="show">Makes this section current.</param>
    public SectionItem(string key, string label, Action<string> show)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(show);
        Key = key;
        Label = label;
        Command = new RelayCommand(() => show(key));
    }

    /// <summary>Gets the section key.</summary>
    public string Key { get; }

    /// <summary>Gets the text shown on the tab.</summary>
    public string Label { get; }

    /// <summary>Gets the command that makes this section current.</summary>
    public RelayCommand Command { get; }

    /// <summary>Gets whether this section is the one showing.</summary>
    public bool IsCurrent { get; internal set => SetProperty(ref field, value); }
}

/// <summary>
/// The sections of a page that shows one at a time instead of stacking them down the page: which is
/// current, and the tabs a view binds to. The choice is remembered through <see cref="UiPreferences"/>
/// when a preference key is given.
/// </summary>
public sealed class SectionSwitch : ObservableObject
{
    private readonly Dictionary<string, SectionItem> _items;
    private readonly UiPreferences? _preferences;
    private readonly string? _preferenceKey;

    /// <summary>Creates the switch on its default section.</summary>
    /// <param name="sections">Keys and labels in display order. At least one; keys unique.</param>
    /// <param name="defaultKey">The section to start on. Must be one of <paramref name="sections"/>.</param>
    /// <param name="preferences">Where the choice is remembered, or <see langword="null"/> to forget it.</param>
    /// <param name="preferenceKey">The preference name, such as <c>section.fleet</c>. Required with <paramref name="preferences"/>.</param>
    public SectionSwitch(IReadOnlyList<(string Key, string Label)> sections, string defaultKey, UiPreferences? preferences = null, string? preferenceKey = null)
    {
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(defaultKey);
        ArgumentOutOfRangeException.ThrowIfZero(sections.Count);

        _preferences = preferences;
        _preferenceKey = preferenceKey;
        _items = sections.ToDictionary(section => section.Key, section => new SectionItem(section.Key, section.Label, Show), StringComparer.Ordinal);
        Items = [.. sections.Select(section => _items[section.Key])];
        Current = defaultKey;
        if (preferences is not null && preferenceKey is not null && preferences.Get(preferenceKey) is { } saved && _items.ContainsKey(saved))
        {
            Current = saved;
        }

        Update();
    }

    /// <summary>Raised after the current section changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the tabs in display order.</summary>
    public IReadOnlyList<SectionItem> Items { get; }

    /// <summary>Gets the key of the section showing.</summary>
    public string Current { get; private set; }

    /// <summary>Gets a section by key, for binding its <see cref="SectionItem.IsCurrent"/>.</summary>
    /// <param name="key">A section key.</param>
    /// <returns>The section.</returns>
    /// <exception cref="KeyNotFoundException">No section has the key.</exception>
    public SectionItem this[string key] => _items[key];

    /// <summary>Makes a section current.</summary>
    /// <param name="key">A section key. Unknown keys are ignored.</param>
    public void Show(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!_items.ContainsKey(key) || key == Current)
        {
            return;
        }

        Current = key;
        Update();
        _preferences?.Set(_preferenceKey!, key);
        OnPropertyChanged(nameof(Current));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Update()
    {
        foreach (var item in Items)
        {
            item.IsCurrent = item.Key == Current;
        }
    }
}
