namespace Katey.App;

/// <summary>
/// Small choices the user makes in the window that are worth remembering: sort orders, which groups are
/// open, which filters are on. Keyed by a dotted name such as <c>sort.engineers</c>, with string values.
/// </summary>
/// <remarks>Not thread-safe. Raises <see cref="Changed"/> after any value changes, so the state can be saved.</remarks>
/// <remarks>Creates the preferences from saved values, keeping only the keys Katey writes today; see <see cref="PreferenceKeys"/>.</remarks>
/// <param name="values">Saved key → value pairs, or <see langword="null"/> for none.</param>
public sealed class UiPreferences(IReadOnlyDictionary<string, string>? values)
{
    private readonly Dictionary<string, string> _values = values is null
        ? new Dictionary<string, string>(StringComparer.Ordinal)
        : values.Where(pair => PreferenceKeys.IsCurrent(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    /// <summary>Raised after a value changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Reads a value.</summary>
    /// <param name="key">The preference name.</param>
    /// <returns>The value, or <see langword="null"/> when never set.</returns>
    public string? Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _values.GetValueOrDefault(key);
    }

    /// <summary>Reads a value as a flag.</summary>
    /// <param name="key">The preference name.</param>
    /// <param name="fallback">What an unset or unreadable value means.</param>
    /// <returns>The flag.</returns>
    public bool GetBool(string key, bool fallback) => bool.TryParse(Get(key), out var value) ? value : fallback;

    /// <summary>Reads a value as a set of names separated by commas.</summary>
    /// <param name="key">The preference name.</param>
    /// <returns>The names, compared ordinally. Empty when unset.</returns>
    public IReadOnlySet<string> GetSet(string key)
        => Get(key) is { } value
            ? value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Writes a value. Raises <see cref="Changed"/> only when it differs.</summary>
    /// <param name="key">The preference name.</param>
    /// <param name="value">The value.</param>
    public void Set(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        if (_values.TryGetValue(key, out var current) && current == value)
        {
            return;
        }

        _values[key] = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Forgets a value. Raises <see cref="Changed"/> only when there was one.</summary>
    /// <param name="key">The preference name.</param>
    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_values.Remove(key))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Writes a flag.</summary>
    /// <param name="key">The preference name.</param>
    /// <param name="value">The flag.</param>
    public void SetBool(string key, bool value) => Set(key, value ? "true" : "false");

    /// <summary>Writes a set of names.</summary>
    /// <param name="key">The preference name.</param>
    /// <param name="values">The names. Must not contain commas.</param>
    public void SetSet(string key, IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Set(key, string.Join(',', values.Order(StringComparer.Ordinal)));
    }

    /// <summary>Replaces every value, keeping only the keys Katey writes today.</summary>
    /// <param name="values">The new values, or <see langword="null"/> for none.</param>
    public void Replace(IReadOnlyDictionary<string, string>? values)
    {
        _values.Clear();
        if (values is not null)
        {
            foreach (var pair in values.Where(pair => PreferenceKeys.IsCurrent(pair.Key)))
            {
                _values[pair.Key] = pair.Value;
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Copies every value, for saving.</summary>
    /// <returns>A new dictionary.</returns>
    public IReadOnlyDictionary<string, string> Snapshot() => new Dictionary<string, string>(_values, StringComparer.Ordinal);
}
