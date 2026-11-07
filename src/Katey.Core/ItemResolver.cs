using System.Text.RegularExpressions;

namespace Katey.Core;

/// <summary>
/// Resolves an EDSY or journal item symbol such as <c>int_hyperdrive_overcharge_size5_class5</c> to a
/// module type code.
/// </summary>
/// <remarks>
/// Each module type's <see cref="ModuleType.ItemPatterns"/> are compiled once, case-insensitively. When
/// several module types match one item, the one whose matching pattern source is longest wins, so a
/// variant pattern such as a bi-weave shield generator beats its base type. Thread-safe after construction.
/// </remarks>
public sealed class ItemResolver
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private readonly List<(string Code, Regex Pattern)> _patterns;

    /// <summary>Compiles every module type's patterns.</summary>
    /// <param name="catalog">The catalog whose module types to resolve against.</param>
    /// <exception cref="ArgumentNullException"><paramref name="catalog"/> is <see langword="null"/>.</exception>
    public ItemResolver(Catalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        _patterns =
        [
            .. catalog.ModuleTypes.Values.SelectMany(type => type.ItemPatterns.Select(source =>
                (type.Code, new Regex(source, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout)))),
        ];
    }

    /// <summary>Finds the module type for an item symbol.</summary>
    /// <param name="item">The item symbol, in any letter case.</param>
    /// <returns>The module type code, or <see langword="null"/> when no pattern matches.</returns>
    public string? Resolve(string item)
    {
        ArgumentNullException.ThrowIfNull(item);

        string? best = null;
        var bestLength = -1;
        foreach (var (code, pattern) in _patterns)
        {
            if (pattern.ToString().Length > bestLength && pattern.IsMatch(item))
            {
                best = code;
                bestLength = pattern.ToString().Length;
            }
        }

        return best;
    }
}
