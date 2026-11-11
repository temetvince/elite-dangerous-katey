namespace Katey.App;

/// <summary>
/// The preference keys Katey writes today. A saved file is pruned to these on load, so a key an update retired
/// is dropped rather than carried along, and a value whose meaning changed is never read under its old name.
/// A new key is added here when it is added to the code that uses it; a key left out is forgotten at the next
/// start, which the tests over the registry are there to catch.
/// </summary>
public static class PreferenceKeys
{
    /// <summary>Prefixes under which any key is current: a table's sort and a page's section, named by the table or page.</summary>
    public static IReadOnlyList<string> OpenPrefixes { get; } = ["sort.", "section."];

    /// <summary>The exact keys that are current.</summary>
    public static IReadOnlySet<string> Exact { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "app.language",
        "engineers.onlyMine",
        "fit.useOwned",
        "fleet.engineering",
        "fleet.location",
        "fleet.place",
        "materials.hidden",
        "materials.shortOnly",
        "modules.expanded",
        "plot.algorithm",
        "plot.cargo",
        "plot.injections",
        "plot.refuelEveryScoopable",
        "plot.secondary",
        "plot.supercharge",
        "plot.target",
        "plot.to",
        "rail.bodyFilter",
        "rail.bodyView",
        "rail.bounds",
        "rail.clickThrough",
        "rail.overlay",
        "ship.prebuiltChanges",
        "stats.hidden",
        "stats.period",
        "stats.zeros",
        "unlocks.expanded",
    };

    /// <summary>Whether a key is one Katey writes today.</summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> for an exact key or one under an open prefix.</returns>
    public static bool IsCurrent(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Exact.Contains(key) || OpenPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal) && key.Length > prefix.Length);
    }
}
