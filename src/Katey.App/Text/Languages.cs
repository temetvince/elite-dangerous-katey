using System.Globalization;
using Katey.Localization;

namespace Katey.App.Text;

/// <summary>One language the picker offers: a culture Katey has tables for, or the machine's own.</summary>
/// <param name="Culture">The culture, or <see langword="null"/> for "follow the machine".</param>
/// <param name="Label">What the picker shows: the language's own name for itself, never translated, so a reader in the wrong language can find the way back.</param>
public sealed record LanguageOption(CultureInfo? Culture, string Label)
{
    /// <summary>Gets the value saved under <see cref="Languages.PreferenceKey"/>: the culture name, or empty to follow the machine.</summary>
    public string Saved => Culture?.Name ?? "";

    /// <summary>Gets whether this is the choice in force.</summary>
    public bool IsCurrent { get; init; }

    /// <inheritdoc/>
    public override string ToString() => Label;
}

/// <summary>The languages Katey ships in, and the preference that remembers the choice.</summary>
public static class Languages
{
    /// <summary>The preference key the chosen language is saved under; absent means follow the machine.</summary>
    public const string PreferenceKey = "app.language";

    /// <summary>Gets the language Windows gave the process at start, which the system-language choice follows. Read before any switch, which is why it is fixed here rather than read live: a switch overwrites the thread's culture.</summary>
    public static CultureInfo Machine { get; } = CultureInfo.CurrentUICulture;

    /// <summary>Gets the cultures tables exist for. English first: it is the neutral table every other falls back to.</summary>
    public static IReadOnlyList<CultureInfo> Supported { get; } =
    [
        CultureInfo.GetCultureInfo("en"),
        CultureInfo.GetCultureInfo("de"),
        CultureInfo.GetCultureInfo("es"),
        CultureInfo.GetCultureInfo("fr"),
        CultureInfo.GetCultureInfo("pt-BR"),
        CultureInfo.GetCultureInfo("ru"),
    ];

    /// <summary>The picker's options: the machine's language first, then every supported culture under its own name.</summary>
    /// <param name="saved">The saved choice, from <see cref="PreferenceKey"/>; empty or <see langword="null"/> when following the machine. Marks the matching option current.</param>
    /// <returns>The options, in display order.</returns>
    public static IReadOnlyList<LanguageOption> Options(string? saved)
    {
        var chosen = saved ?? "";
        return
        [
            new LanguageOption(null, Strings.Get(Keys.Language_System)) { IsCurrent = chosen.Length == 0 },
            .. Supported.Select(culture => new LanguageOption(culture, NativeName(culture)) { IsCurrent = string.Equals(culture.Name, chosen, StringComparison.OrdinalIgnoreCase) }),
        ];
    }

    /// <summary>Applies the saved choice, or the machine's language when nothing is saved, and makes it the language in force.</summary>
    /// <param name="preferences">Where the choice is kept.</param>
    /// <returns>The culture now in force.</returns>
    public static CultureInfo Apply(UiPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var culture = Language.Resolve(preferences.Get(PreferenceKey), Machine, Supported);
        Language.Set(culture);
        return culture;
    }

    /// <summary>A language's name for itself, with its first letter in capitals: "Deutsch", "Português (Brasil)".</summary>
    /// <param name="culture">The culture.</param>
    /// <returns>The name.</returns>
    public static string NativeName(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var name = culture.NativeName;
        return name.Length == 0 ? culture.Name : string.Concat(name[..1].ToUpper(culture), name.AsSpan(1));
    }
}
