using System.Globalization;

namespace Katey.Localization;

/// <summary>
/// The checks a string table and its translations must pass, as sentences a test or a tool can print: the
/// generated key constants match the neutral table, and every translation has every key, the same
/// placeholders, and every plural form its language uses.
/// </summary>
public static class TableChecks
{
    /// <summary>Where the key constants and the neutral table disagree.</summary>
    /// <param name="source">The table.</param>
    /// <param name="constants">The keys the generated constants name.</param>
    /// <returns>One sentence per key missing on either side; empty when they match. Plural forms are folded to their base key.</returns>
    public static IReadOnlyList<string> KeyProblems(ITextSource source, IEnumerable<string> constants)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(constants);
        var named = constants.ToHashSet(StringComparer.Ordinal);
        var table = source.Keys(CultureInfo.InvariantCulture)
            .Select(key => PluralRules.IsPluralForm(key, out var baseKey, out _) ? baseKey : key)
            .ToHashSet(StringComparer.Ordinal);
        var problems = new List<string>();
        if (table.Count == 0)
        {
            problems.Add("The neutral table is empty or not embedded.");
        }

        problems.AddRange(table.Except(named).Order(StringComparer.Ordinal).Select(key => $"{key} is in the table but has no constant."));
        problems.AddRange(named.Except(table).Order(StringComparer.Ordinal).Select(key => $"{key} has a constant but is not in the table."));
        return problems;
    }

    /// <summary>Where a translation falls short of the neutral table, or goes beyond it.</summary>
    /// <param name="source">The table.</param>
    /// <param name="cultures">The translated cultures to check; the neutral table is the reference.</param>
    /// <returns>One sentence per problem, each starting with the culture's name; empty when every translation is complete.</returns>
    public static IReadOnlyList<string> TranslationProblems(ITextSource source, IEnumerable<CultureInfo> cultures)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(cultures);
        var neutral = source.Keys(CultureInfo.InvariantCulture);
        var plainKeys = neutral.Where(key => !PluralRules.IsPluralForm(key, out _, out _)).Order(StringComparer.Ordinal).ToList();
        var pluralBases = neutral
            .Where(key => PluralRules.IsPluralForm(key, out _, out _))
            .Select(key => key[..key.LastIndexOf('.')])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        var problems = new List<string>();
        foreach (var culture in cultures)
        {
            var keys = source.Keys(culture);
            if (keys.Count == 0)
            {
                problems.Add($"{culture.Name}: no table.");
                continue;
            }

            foreach (var key in plainKeys)
            {
                if (!keys.Contains(key))
                {
                    problems.Add($"{culture.Name}: {key} is missing.");
                    continue;
                }

                ComparePlaceholders(source, culture, key, key, problems);
            }

            foreach (var baseKey in pluralBases)
            {
                var reference = baseKey + PluralRules.Suffix(PluralCategory.Other);
                foreach (var category in PluralRules.CategoriesOf(culture))
                {
                    var key = baseKey + PluralRules.Suffix(category);
                    if (!keys.Contains(key))
                    {
                        problems.Add($"{culture.Name}: plural form {key} is missing.");
                        continue;
                    }

                    ComparePlaceholders(source, culture, key, reference, problems);
                }
            }

            foreach (var key in keys.Order(StringComparer.Ordinal))
            {
                var known = neutral.Contains(key) || (PluralRules.IsPluralForm(key, out var baseKey, out _) && pluralBases.Contains(baseKey));
                if (!known)
                {
                    problems.Add($"{culture.Name}: {key} is not in the neutral table.");
                }
            }
        }

        return problems;
    }

    /// <summary>Where a translation of a table whose neutral file is empty falls short of the keys it must hold, or goes beyond them.</summary>
    /// <param name="source">The table.</param>
    /// <param name="culture">The translated culture to check.</param>
    /// <param name="expected">Every key a complete translation holds.</param>
    /// <returns>One sentence per missing or unexpected key, each starting with the culture's name; empty when the translation is complete.</returns>
    public static IReadOnlyList<string> KeyProblems(ITextSource source, CultureInfo culture, IEnumerable<string> expected)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(expected);
        var wanted = expected.ToHashSet(StringComparer.Ordinal);
        var keys = source.Keys(culture);
        if (keys.Count == 0)
        {
            return [$"{culture.Name}: no table."];
        }

        var problems = new List<string>();
        problems.AddRange(wanted.Except(keys).Order(StringComparer.Ordinal).Select(key => $"{culture.Name}: {key} is missing."));
        problems.AddRange(keys.Except(wanted).Order(StringComparer.Ordinal).Select(key => $"{culture.Name}: {key} is not a key the table takes."));
        foreach (var key in wanted.Intersect(keys).Order(StringComparer.Ordinal))
        {
            try
            {
                Template.Placeholders(source.Lookup(key, culture) ?? "");
            }
            catch (FormatException exception)
            {
                problems.Add($"{culture.Name}: {key} does not parse: {exception.Message}");
            }
        }

        return problems;
    }

    private static void ComparePlaceholders(ITextSource source, CultureInfo culture, string key, string referenceKey, List<string> problems)
    {
        IReadOnlyList<string> expected;
        IReadOnlyList<string> actual;
        try
        {
            expected = Template.Placeholders(source.Lookup(referenceKey, CultureInfo.InvariantCulture) ?? "");
            actual = Template.Placeholders(source.Lookup(key, culture) ?? "");
        }
        catch (FormatException exception)
        {
            problems.Add($"{culture.Name}: {key} does not parse: {exception.Message}");
            return;
        }

        var expectedSet = expected.Order(StringComparer.Ordinal).ToList();
        var actualSet = actual.Order(StringComparer.Ordinal).ToList();
        if (!expectedSet.SequenceEqual(actualSet))
        {
            problems.Add($"{culture.Name}: {key} has placeholders [{string.Join(", ", actualSet)}] but the neutral text has [{string.Join(", ", expectedSet)}].");
        }
    }
}
