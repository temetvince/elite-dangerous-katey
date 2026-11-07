namespace Katey.App.Mvvm;

/// <summary>
/// Which of a page's collapsible groups are open, remembered through <see cref="UiPreferences"/> as a set
/// of group keys under one preference name, with expand-all and collapse-all.
/// </summary>
public sealed class GroupExpansion
{
    private readonly UiPreferences _preferences;
    private readonly string _key;

    /// <summary>Creates the expansion state.</summary>
    /// <param name="preferences">Where the open groups are remembered.</param>
    /// <param name="key">The preference name, such as <c>modules.expanded</c>.</param>
    public GroupExpansion(UiPreferences preferences, string key)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _preferences = preferences;
        _key = key;
    }

    /// <summary>Whether a group is remembered as open.</summary>
    /// <param name="groupKey">The group's key.</param>
    /// <returns><see langword="true"/> when open.</returns>
    public bool IsExpanded(string groupKey)
    {
        ArgumentNullException.ThrowIfNull(groupKey);
        return _preferences.GetSet(_key).Contains(groupKey);
    }

    /// <summary>Remembers one group as open or closed.</summary>
    /// <param name="groupKey">The group's key. Must not contain a comma.</param>
    /// <param name="expanded">The new state.</param>
    public void Set(string groupKey, bool expanded)
    {
        ArgumentNullException.ThrowIfNull(groupKey);
        var set = _preferences.GetSet(_key).ToHashSet(StringComparer.Ordinal);
        if (expanded)
        {
            set.Add(groupKey);
        }
        else
        {
            set.Remove(groupKey);
        }

        _preferences.SetSet(_key, set);
    }

    /// <summary>Opens or closes every group and remembers that.</summary>
    /// <param name="groups">The groups.</param>
    /// <param name="expanded">The new state for all of them.</param>
    public void SetAll(IEnumerable<ExpandableGroup> groups, bool expanded)
    {
        ArgumentNullException.ThrowIfNull(groups);
        var list = groups.ToList();
        foreach (var group in list)
        {
            group.SetSilently(expanded);
        }

        _preferences.SetSet(_key, expanded ? list.Select(group => group.Key) : []);
    }
}

/// <summary>A collapsible group on a page whose open state a <see cref="GroupExpansion"/> remembers.</summary>
public abstract class ExpandableGroup : ObservableObject
{
    private readonly GroupExpansion _expansion;
    private bool _expanded;

    /// <summary>Creates the group, open or closed as remembered.</summary>
    /// <param name="key">The group's key within its expansion. Must not contain a comma.</param>
    /// <param name="label">The heading.</param>
    /// <param name="expansion">Where the state is remembered.</param>
    protected ExpandableGroup(string key, string label, GroupExpansion expansion)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(expansion);
        Key = key;
        Label = label;
        _expansion = expansion;
        _expanded = expansion.IsExpanded(key);
    }

    /// <summary>Gets the group's key.</summary>
    public string Key { get; }

    /// <summary>Gets the heading.</summary>
    public string Label { get; }

    /// <summary>Gets or sets whether the group is open. Setting it is remembered.</summary>
    public bool IsExpanded
    {
        get => _expanded;
        set
        {
            if (SetProperty(ref _expanded, value))
            {
                _expansion.Set(Key, value);
            }
        }
    }

    /// <summary>Opens or closes the group without remembering it, for expand-all and collapse-all.</summary>
    /// <param name="expanded">The new state.</param>
    internal void SetSilently(bool expanded) => SetProperty(ref _expanded, expanded, nameof(IsExpanded));
}
