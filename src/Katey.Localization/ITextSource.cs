using System.Globalization;
using System.Resources;

namespace Katey.Localization;

/// <summary>Where the words come from: one text per key per culture.</summary>
/// <remarks>Implementations are read on every lookup and must be safe to call from any thread.</remarks>
public interface ITextSource
{
    /// <summary>The text for a key in a culture.</summary>
    /// <param name="key">The key, as the table spells it.</param>
    /// <param name="culture">The culture wanted. A source may fall back to a parent culture or to its neutral text.</param>
    /// <returns>The text, or <see langword="null"/> when the key is not in the table for that culture or any fallback.</returns>
    string? Lookup(string key, CultureInfo culture);

    /// <summary>Every key the source has in a culture, without falling back.</summary>
    /// <param name="culture">The culture.</param>
    /// <returns>The keys, in no particular order; empty when the culture has no table of its own.</returns>
    IReadOnlySet<string> Keys(CultureInfo culture);
}

/// <summary>A text source over a <see cref="ResourceManager"/>: a <c>.resx</c> table compiled into an assembly and its satellites.</summary>
/// <remarks>Culture fallback is the resource manager's: the culture asked for, its parents, then the neutral table.</remarks>
public sealed class ResourceTextSource : ITextSource
{
    private readonly ResourceManager _manager;

    /// <summary>Creates the source.</summary>
    /// <param name="manager">The resource manager over the table.</param>
    public ResourceTextSource(ResourceManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        _manager = manager;
    }

    /// <inheritdoc/>
    public string? Lookup(string key, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(culture);
        return _manager.GetString(key, culture);
    }

    /// <inheritdoc/>
    public IReadOnlySet<string> Keys(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var set = _manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        if (set is null)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (System.Collections.DictionaryEntry entry in set)
        {
            if (entry.Key is string key)
            {
                keys.Add(key);
            }
        }

        return keys;
    }
}

/// <summary>A text source over dictionaries, one per culture name, for tests and tools.</summary>
/// <remarks>Falls back from a culture to its parent and then to the neutral table, keyed by the empty culture name.</remarks>
public sealed class DictionaryTextSource : ITextSource
{
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _tables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates the source.</summary>
    /// <param name="tables">Culture name, such as <c>de</c> or <c>pt-BR</c>, or empty for the neutral table → key → text.</param>
    public DictionaryTextSource(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> tables)
    {
        ArgumentNullException.ThrowIfNull(tables);
        foreach (var (culture, table) in tables)
        {
            _tables[culture] = table;
        }
    }

    /// <inheritdoc/>
    public string? Lookup(string key, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(culture);
        for (var current = culture; ; current = current.Parent)
        {
            if (_tables.TryGetValue(current.Name, out var table) && table.TryGetValue(key, out var text))
            {
                return text;
            }

            if (current.Name.Length == 0)
            {
                return null;
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlySet<string> Keys(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return _tables.TryGetValue(culture.Name, out var table) ? table.Keys.ToHashSet(StringComparer.Ordinal) : new HashSet<string>(StringComparer.Ordinal);
    }
}
