using System.Globalization;

namespace Katey.Localization;

/// <summary>
/// Words by key, in the language in force: plain texts, templates filled by name, and counted nouns in the
/// plural form the language takes. Every call reads <see cref="Language.Current"/>, so a lookup made after a
/// switch is already in the new language.
/// </summary>
/// <remarks>Thread-safe when its source is. A key the source lacks comes back as the key itself, so a gap reads as a gap rather than as nothing.</remarks>
public sealed class Localizer
{
    /// <summary>Creates a localizer over a source.</summary>
    /// <param name="source">Where the words come from.</param>
    public Localizer(ITextSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;
    }

    /// <summary>Gets where the words come from.</summary>
    public ITextSource Source { get; private set; }

    /// <summary>Wraps the source, for a pseudo-locale or a recording source. Call before the first lookup.</summary>
    /// <param name="wrap">Given the current source, returns the one to use.</param>
    public void Decorate(Func<ITextSource, ITextSource> wrap)
    {
        ArgumentNullException.ThrowIfNull(wrap);
        Source = wrap(Source) ?? throw new InvalidOperationException("The decorator returned no source.");
    }

    /// <summary>The text for a key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The text in the language in force, or the key when the table lacks it.</returns>
    public string Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Source.Lookup(key, Language.Current) ?? key;
    }

    /// <summary>The text for a key, when the table has it.</summary>
    /// <param name="key">The key.</param>
    /// <param name="text">The text in the language in force, or a fallback's; empty when not found.</param>
    /// <returns><see langword="true"/> when a lookup found text.</returns>
    public bool TryGet(string key, out string text)
    {
        ArgumentNullException.ThrowIfNull(key);
        text = Source.Lookup(key, Language.Current) ?? "";
        return text.Length > 0 || Source.Lookup(key, Language.Current) is not null;
    }

    /// <summary>Whether a key has text in the language in force or a fallback.</summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when a lookup would find text.</returns>
    public bool Has(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Source.Lookup(key, Language.Current) is not null;
    }

    /// <summary>A template filled by name.</summary>
    /// <param name="key">The template's key.</param>
    /// <param name="args">Name → value. A value is written with its placeholder's format and the formatting culture; a <see langword="null"/> writes nothing.</param>
    /// <returns>The text. A placeholder no argument names stays as written.</returns>
    public string Format(string key, params (string Name, object? Value)[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return Fill(Get(key), args);
    }

    /// <summary>A counted noun or sentence in the plural form the language takes for the count.</summary>
    /// <param name="key">The base key; the table holds <c>key.one</c>, <c>key.other</c> and whichever other forms the language needs.</param>
    /// <param name="count">The count, written into <c>{count}</c>.</param>
    /// <param name="args">Further name → value pairs for the form's other placeholders.</param>
    /// <returns>The text. A form the table lacks falls back to <c>.other</c>, then to the base key's own text, then to the key.</returns>
    public string Plural(string key, long count, params (string Name, object? Value)[] args)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(args);
        var culture = Language.Current;
        var template = Source.Lookup(key + PluralRules.Suffix(PluralRules.Of(culture, count)), culture)
            ?? Source.Lookup(key + PluralRules.Suffix(PluralCategory.Other), culture)
            ?? Source.Lookup(key, culture)
            ?? key;
        var all = new (string Name, object? Value)[args.Length + 1];
        all[0] = ("count", count);
        args.CopyTo(all, 1);
        return Fill(template, all);
    }

    /// <summary>Writes one value as a placeholder asks: with its format, in the formatting culture.</summary>
    /// <param name="value">The value; <see langword="null"/> writes nothing.</param>
    /// <param name="format">The placeholder's format, or <see langword="null"/>.</param>
    /// <returns>The text.</returns>
    public static string Write(object? value, string? format) => value switch
    {
        null => "",
        string text => text,
        IFormattable formattable => formattable.ToString(format, CultureInfo.CurrentCulture),
        _ => value.ToString() ?? "",
    };

    private static string Fill(string template, (string Name, object? Value)[] args)
        => Template.Fill(template, (name, format) =>
        {
            foreach (var (argName, value) in args)
            {
                if (string.Equals(argName, name, StringComparison.Ordinal))
                {
                    return Write(value, format);
                }
            }

            return null;
        });
}
