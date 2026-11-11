using System.Globalization;

namespace Katey.Localization;

/// <summary>The plural categories of Unicode CLDR. A language uses a subset; every language uses <see cref="Other"/>.</summary>
public enum PluralCategory
{
    /// <summary>The form for zero, in languages that have one.</summary>
    Zero,

    /// <summary>The singular form.</summary>
    One,

    /// <summary>The form for two, in languages that have one.</summary>
    Two,

    /// <summary>The form for a few, such as 2 to 4 in Russian.</summary>
    Few,

    /// <summary>The form for many, such as 5 to 20 in Russian.</summary>
    Many,

    /// <summary>The general plural, and the fallback for every other category.</summary>
    Other,
}

/// <summary>
/// Which plural form a whole number takes in a language, by CLDR's rules for integers. Katey counts whole
/// things, so the rules for fractions are left out.
/// </summary>
/// <remarks>
/// Known languages: English, German, Spanish and the neutral culture take <see cref="PluralCategory.One"/> for
/// exactly 1; French and Portuguese for 0 and 1; Russian and Ukrainian take one, few and many by the last
/// digits. Any other language is treated like English.
/// </remarks>
public static class PluralRules
{
    /// <summary>The category a count takes in a culture.</summary>
    /// <param name="culture">The culture; its language decides.</param>
    /// <param name="count">The whole number. Negative counts are judged by their magnitude.</param>
    /// <returns>The category.</returns>
    public static PluralCategory Of(CultureInfo culture, long count)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var n = Math.Abs(count);
        switch (culture.TwoLetterISOLanguageName)
        {
            case "fr":
            case "pt":
                return n is 0 or 1 ? PluralCategory.One : PluralCategory.Other;
            case "ru":
            case "uk":
                var tens = n % 100;
                var units = n % 10;
                if (units == 1 && tens != 11)
                {
                    return PluralCategory.One;
                }

                return units is >= 2 and <= 4 && tens is < 12 or > 14 ? PluralCategory.Few : PluralCategory.Many;
            default:
                return n == 1 ? PluralCategory.One : PluralCategory.Other;
        }
    }

    /// <summary>The categories a culture's language uses, so a table can be checked for completeness.</summary>
    /// <param name="culture">The culture.</param>
    /// <returns>The categories, <see cref="PluralCategory.Other"/> always among them.</returns>
    public static IReadOnlyList<PluralCategory> CategoriesOf(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return culture.TwoLetterISOLanguageName switch
        {
            "ru" or "uk" => [PluralCategory.One, PluralCategory.Few, PluralCategory.Many, PluralCategory.Other],
            _ => [PluralCategory.One, PluralCategory.Other],
        };
    }

    /// <summary>The key suffix a category takes in a table: <c>.one</c>, <c>.few</c>, <c>.many</c>, <c>.other</c> and so on.</summary>
    /// <param name="category">The category.</param>
    /// <returns>The suffix, with its leading dot.</returns>
    public static string Suffix(PluralCategory category) => category switch
    {
        PluralCategory.Zero => ".zero",
        PluralCategory.One => ".one",
        PluralCategory.Two => ".two",
        PluralCategory.Few => ".few",
        PluralCategory.Many => ".many",
        PluralCategory.Other => ".other",
        _ => ".other",
    };

    /// <summary>Whether a key ends in a plural suffix, and which base key and category it belongs to.</summary>
    /// <param name="key">The key.</param>
    /// <param name="baseKey">The key without the suffix.</param>
    /// <param name="category">The category the suffix names.</param>
    /// <returns><see langword="true"/> when the key is a plural form.</returns>
    public static bool IsPluralForm(string key, out string baseKey, out PluralCategory category)
    {
        ArgumentNullException.ThrowIfNull(key);
        var dot = key.LastIndexOf('.');
        if (dot > 0)
        {
            var suffix = key[dot..];
            foreach (var candidate in Enum.GetValues<PluralCategory>())
            {
                if (string.Equals(Suffix(candidate), suffix, StringComparison.Ordinal))
                {
                    baseKey = key[..dot];
                    category = candidate;
                    return true;
                }
            }
        }

        baseKey = key;
        category = PluralCategory.Other;
        return false;
    }
}
