using System.Globalization;

namespace Katey.App.Text;

/// <summary>One text size the picker offers. Two options with the same factor are equal, whichever list they came from.</summary>
/// <param name="Scale">The factor the windows' content is drawn at: 1 is the design size.</param>
/// <param name="Label">What the picker shows, in the language in force.</param>
public sealed record TextSizeOption(double Scale, string Label)
{
    /// <inheritdoc/>
    public override string ToString() => Label;
}

/// <summary>
/// The text size: a factor every window's content is drawn at, so a reader who needs larger type gets it
/// in every language, and the preference that remembers it.
/// </summary>
/// <remarks>The factor scales the whole content, text and spacing alike, so nothing laid out for the design size is cut short by larger type.</remarks>
public static class TextSize
{
    /// <summary>The preference key the chosen factor is saved under, as an invariant number; absent means the design size.</summary>
    public const string PreferenceKey = "app.textScale";

    /// <summary>Gets the factors the picker offers, smallest first. The first is the design size.</summary>
    public static IReadOnlyList<double> Scales { get; } = [1, 1.15, 1.3, 1.5, 1.75, 2];

    /// <summary>The factor in force.</summary>
    /// <param name="preferences">Where the choice is kept.</param>
    /// <returns>The saved factor when it is one of <see cref="Scales"/>; the design size otherwise.</returns>
    public static double Read(UiPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return double.TryParse(preferences.Get(PreferenceKey), NumberStyles.Float, CultureInfo.InvariantCulture, out var scale) && Scales.Contains(scale) ? scale : Scales[0];
    }

    /// <summary>Remembers a factor.</summary>
    /// <param name="preferences">Where the choice is kept.</param>
    /// <param name="scale">One of <see cref="Scales"/>. The design size removes the key.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale"/> is not one of <see cref="Scales"/>.</exception>
    public static void Save(UiPreferences preferences, double scale)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (!Scales.Contains(scale))
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Not one of the offered text sizes.");
        }

        if (scale == Scales[0])
        {
            preferences.Remove(PreferenceKey);
        }
        else
        {
            preferences.Set(PreferenceKey, scale.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>The picker's options, each a percentage of the design size, in the language in force.</summary>
    /// <returns>The options, smallest first, one per factor in <see cref="Scales"/>.</returns>
    public static IReadOnlyList<TextSizeOption> Options()
        => [.. Scales.Select(scale => new TextSizeOption(scale, Strings.Format(Keys.TextSize_Percent, ("percent", (int)Math.Round(scale * 100)))))];
}
