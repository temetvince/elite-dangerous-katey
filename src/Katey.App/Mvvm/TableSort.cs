namespace Katey.App.Mvvm;

/// <summary>One sortable column's header: its label, the command that sorts by it, and the direction mark.</summary>
public sealed class SortHeader : ObservableObject
{
    /// <summary>Creates the header.</summary>
    /// <param name="key">The column key.</param>
    /// <param name="label">The text shown.</param>
    /// <param name="sort">Sorts by the key, or flips the direction when it is already the active key.</param>
    public SortHeader(string key, string label, Action<string> sort)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(sort);
        Key = key;
        Label = label;
        Command = new RelayCommand(() => sort(key));
    }

    /// <summary>Gets the column key.</summary>
    public string Key { get; }

    /// <summary>Gets the text shown.</summary>
    public string Label { get; }

    /// <summary>Gets the command that sorts by this column.</summary>
    public RelayCommand Command { get; }

    /// <summary>Gets whether the table is sorted by this column.</summary>
    public bool IsActive { get; private set => SetProperty(ref field, value); }

    /// <summary>Gets "▲" or "▼" while active, else empty.</summary>
    public string Indicator { get; private set => SetProperty(ref field, value); } = "";

    /// <summary>Updates the marks for the table's state.</summary>
    /// <param name="activeKey">The column the table is sorted by.</param>
    /// <param name="descending">Whether it is sorted descending.</param>
    internal void Update(string activeKey, bool descending)
    {
        IsActive = activeKey == Key;
        Indicator = IsActive ? (descending ? "▼" : "▲") : "";
    }
}

/// <summary>How one column orders rows.</summary>
/// <typeparam name="T">The row type.</typeparam>
/// <param name="Key">The column key, unique within the table.</param>
/// <param name="Label">The header text.</param>
/// <param name="By">Picks the value a row sorts by. Values must be mutually comparable; <see langword="null"/> sorts last.</param>
/// <param name="DescendingFirst">Whether the first click sorts descending, as a count or a value usually should.</param>
public sealed record SortColumn<T>(string Key, string Label, Func<T, IComparable?> By, bool DescendingFirst = false);

/// <summary>
/// The sort state of one table: which column, which direction, and the headers a view binds to. Clicking
/// the active header flips the direction; clicking another sorts by it. The state is remembered through
/// <see cref="UiPreferences"/> when a preference key is given.
/// </summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class TableSort<T> : ObservableObject
{
    private readonly IReadOnlyList<SortColumn<T>> _columns;
    private readonly Dictionary<string, SortHeader> _headers;
    private readonly UiPreferences? _preferences;
    private readonly string? _preferenceKey;

    /// <summary>Creates the sort on its default column.</summary>
    /// <param name="columns">The sortable columns. At least one; keys unique.</param>
    /// <param name="defaultKey">The column to start on. Must be one of <paramref name="columns"/>.</param>
    /// <param name="preferences">Where the choice is remembered, or <see langword="null"/> to forget it.</param>
    /// <param name="preferenceKey">The preference name, such as <c>sort.engineers</c>. Required with <paramref name="preferences"/>.</param>
    public TableSort(IReadOnlyList<SortColumn<T>> columns, string defaultKey, UiPreferences? preferences = null, string? preferenceKey = null)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(defaultKey);
        ArgumentOutOfRangeException.ThrowIfZero(columns.Count);

        _columns = columns;
        _preferences = preferences;
        _preferenceKey = preferenceKey;
        _headers = columns.ToDictionary(column => column.Key, column => new SortHeader(column.Key, column.Label, SortBy), StringComparer.Ordinal);
        Headers = [.. columns.Select(column => _headers[column.Key])];

        var start = columns.First(column => column.Key == defaultKey);
        ActiveKey = start.Key;
        Descending = start.DescendingFirst;
        if (preferences is not null && preferenceKey is not null && preferences.Get(preferenceKey) is { } saved)
        {
            var parts = saved.Split(':');
            if (parts.Length == 2 && _headers.ContainsKey(parts[0]))
            {
                ActiveKey = parts[0];
                Descending = parts[1] == "desc";
            }
        }

        UpdateHeaders();
    }

    /// <summary>Raised after the column or direction changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the headers in column order.</summary>
    public IReadOnlyList<SortHeader> Headers { get; }

    /// <summary>Gets the key of the column the table is sorted by.</summary>
    public string ActiveKey { get; private set; }

    /// <summary>Gets whether the sort is descending.</summary>
    public bool Descending { get; private set; }

    /// <summary>Gets a header by column key, for binding.</summary>
    /// <param name="key">A column key.</param>
    /// <returns>The header.</returns>
    /// <exception cref="KeyNotFoundException">No column has the key.</exception>
    public SortHeader this[string key] => _headers[key];

    /// <summary>Sorts by a column: flips the direction when it is already active, else starts on its default direction.</summary>
    /// <param name="key">A column key. Unknown keys are ignored.</param>
    public void SortBy(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var column = _columns.FirstOrDefault(column => column.Key == key);
        if (column is null)
        {
            return;
        }

        if (ActiveKey == key)
        {
            Descending = !Descending;
        }
        else
        {
            ActiveKey = key;
            Descending = column.DescendingFirst;
        }

        UpdateHeaders();
        _preferences?.Set(_preferenceKey!, $"{ActiveKey}:{(Descending ? "desc" : "asc")}");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Orders rows by the active column and direction. Ties keep their incoming order.</summary>
    /// <param name="rows">The rows.</param>
    /// <returns>A new list.</returns>
    public IReadOnlyList<T> Apply(IEnumerable<T> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var by = _columns.First(column => column.Key == ActiveKey).By;
        var indexed = rows.Select((row, index) => (Row: row, Index: index)).ToList();
        var ordered = Descending
            ? indexed.OrderByDescending(pair => by(pair.Row), NullsLastComparer.Instance).ThenBy(pair => pair.Index)
            : indexed.OrderBy(pair => by(pair.Row), NullsLastComparer.Instance).ThenBy(pair => pair.Index);
        return [.. ordered.Select(pair => pair.Row)];
    }

    private void UpdateHeaders()
    {
        foreach (var header in Headers)
        {
            header.Update(ActiveKey, Descending);
        }
    }

    /// <summary>Compares values, putting <see langword="null"/> after everything and strings case-insensitively.</summary>
    private sealed class NullsLastComparer : IComparer<IComparable?>
    {
        public static NullsLastComparer Instance { get; } = new();

        public int Compare(IComparable? x, IComparable? y)
        {
            return x is null
                ? y is null ? 0 : 1
                : y is null
                ? -1
                : x is string left && y is string right ? string.Compare(left, right, StringComparison.OrdinalIgnoreCase) : x.CompareTo(y);
        }
    }
}
