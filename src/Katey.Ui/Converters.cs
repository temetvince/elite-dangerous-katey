using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Katey.Core.Exploration;

namespace Katey.Ui;

/// <summary>Turns a hex colour string such as <c>#4E79A7</c> from a view model into a brush.</summary>
public sealed class BrushFromHex : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string text && Color.TryParse(text, out var color) ? new SolidColorBrush(color) : null;

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Turns SVG path syntax from a view model into a geometry a <c>Path</c> draws.</summary>
public sealed class GeometryFromPath : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string { Length: > 0 } text ? Geometry.Parse(text) : null;

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Looks a meaning up in the palette: the converter parameter is a resource key prefix and the value its
/// suffix, so a grade of 3 with the prefix <c>KG</c> finds <c>KG3</c>, and a <c>MaterialCategory.Raw</c> with
/// the prefix <c>K</c> finds <c>KRaw</c>. The brush comes from the theme in force when the binding
/// evaluates; a page rebuilt after a theme change picks the other theme's brush up.
/// </summary>
public sealed class SemanticBrush : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }

        var suffix = value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString();
        return Palette.Brush($"{parameter}{suffix}");
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Turns a star class string as the journal writes it, such as <c>K</c>, <c>DA</c> or <c>N</c>, into the colour the galaxy map gives that class.</summary>
public sealed class StarBrush : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var starClass = value as string ?? "";
        var key = StarClasses.Classify(starClass) switch
        {
            StarKind.Scoopable => "KStar" + StarClasses.Spectral(starClass),
            StarKind.Neutron => "KStarNeutron",
            StarKind.WhiteDwarf => "KStarWhiteDwarf",
            StarKind.BlackHole => "KStarBlackHole",
            StarKind.Other => "KStarOther",
            _ => "KStarOther",
        };
        return Palette.Brush(key);
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Multiplies a number by the converter parameter, so a fraction can drive an angle or a width.</summary>
public sealed class Scale : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var factor = parameter is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 1;
        return value switch
        {
            double d => d * factor,
            float f => f * factor,
            int i => i * factor,
            _ => 0d,
        };
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Finds brushes in the application's resources under the theme in force.</summary>
internal static class Palette
{
    /// <summary>The brush under a key, or a neutral fallback when the key names nothing.</summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The brush. Never <see langword="null"/>.</returns>
    internal static IBrush Brush(string key)
    {
        var app = Application.Current;
        return app is not null && app.TryGetResource(key, app.ActualThemeVariant, out var found) && found is IBrush brush
            ? brush
            : app is not null && app.TryGetResource("KMuted", app.ActualThemeVariant, out var muted) && muted is IBrush fallback ? fallback : Brushes.Gray;
    }
}
