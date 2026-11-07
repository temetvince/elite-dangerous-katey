namespace Katey.Core.Search;

/// <summary>One hit from a <see cref="TextIndex{T}"/> query.</summary>
/// <typeparam name="T">The indexed item type.</typeparam>
/// <param name="Item">The item.</param>
/// <param name="Score">How well it matched. Higher is better; see <see cref="TextIndex{T}.Query"/> for the scale.</param>
public sealed record SearchHit<T>(T Item, int Score);

/// <summary>
/// A small in-memory name index: items are added with the text they are known by, and a query returns
/// the items whose text matches every word of it, best match first.
/// </summary>
/// <typeparam name="T">The indexed item type.</typeparam>
/// <remarks>
/// Matching is case-insensitive and ignores leading, trailing and repeated spaces. Not thread-safe while
/// items are being added; queries from one thread after loading are fine. Meant for a few thousand
/// entries, which it scans in full on every query.
/// </remarks>
public sealed class TextIndex<T>
{
    private const int ExactScore = 100;
    private const int PrefixScore = 80;
    private const int WordPrefixScore = 60;
    private const int ContainsScore = 40;
    private const int SecondaryPenalty = 20;

    private readonly List<Entry> _entries = [];

    /// <summary>Gets how many items are indexed.</summary>
    public int Count => _entries.Count;

    /// <summary>Adds an item under the text it is known by.</summary>
    /// <param name="text">The primary name. Must not be blank.</param>
    /// <param name="item">The item.</param>
    /// <param name="secondary">Other text the item may be found by, such as a code or a location, or <see langword="null"/>. Matches here score lower than matches on <paramref name="text"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="text"/> is blank.</exception>
    public void Add(string text, T item, string? secondary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _entries.Add(new Entry(Normalize(text), secondary is null ? null : Normalize(secondary), item));
    }

    /// <summary>Finds the items matching a query.</summary>
    /// <param name="query">Words to look for. Blank returns nothing.</param>
    /// <param name="limit">The most hits to return. Must be positive.</param>
    /// <returns>
    /// Items whose primary or secondary text matches every word of the query, highest score first and
    /// then by primary text. The score is 100 for a primary text equal to the whole query, 80 when it
    /// starts with the query, 60 when some word in it starts with every query word, 40 when it merely
    /// contains them, and 20 less for a match only on the secondary text.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is not positive.</exception>
    public IReadOnlyList<SearchHit<T>> Query(string query, int limit)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        var normalized = Normalize(query);
        if (normalized.Length == 0)
        {
            return [];
        }

        var words = normalized.Split(' ');
        var hits = new List<(SearchHit<T> Hit, string Text)>();
        foreach (var entry in _entries)
        {
            var score = Score(entry.Text, normalized, words);
            if (score == 0 && entry.Secondary is { } secondary)
            {
                var secondaryScore = Score(secondary, normalized, words);
                score = secondaryScore == 0 ? 0 : secondaryScore - SecondaryPenalty;
            }

            if (score > 0)
            {
                hits.Add((new SearchHit<T>(entry.Item, score), entry.Text));
            }
        }

        return
        [
            .. hits
                .OrderByDescending(hit => hit.Hit.Score)
                .ThenBy(hit => hit.Text, StringComparer.Ordinal)
                .Take(limit)
                .Select(hit => hit.Hit),
        ];
    }

    private static int Score(string text, string query, string[] words)
    {
        if (text == query)
        {
            return ExactScore;
        }

        if (text.StartsWith(query, StringComparison.Ordinal))
        {
            return PrefixScore;
        }

        var textWords = text.Split(' ');
        var allWordPrefixes = words.All(word => textWords.Any(textWord => textWord.StartsWith(word, StringComparison.Ordinal)));
        return allWordPrefixes ? WordPrefixScore : words.All(word => text.Contains(word, StringComparison.Ordinal)) ? ContainsScore : 0;
    }

    private static string Normalize(string text)
        => string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToUpperInvariant().ToLowerInvariant();

    private sealed record Entry(string Text, string? Secondary, T Item);
}
