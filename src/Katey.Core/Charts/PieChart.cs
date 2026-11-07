using System.Globalization;

namespace Katey.Core.Charts;

/// <summary>One sector of a pie or ring chart.</summary>
/// <param name="Key">What the sector stands for.</param>
/// <param name="Value">Its value. Positive.</param>
/// <param name="Fraction">Its share of the whole, 0 to 1.</param>
/// <param name="StartAngle">Where it starts, in degrees clockwise from twelve o'clock.</param>
/// <param name="SweepAngle">How far it runs, in degrees. 360 for a sector that is the whole chart.</param>
/// <param name="PathData">The sector's outline in SVG path syntax, in a square of the chart's size with the origin at its top left.</param>
public sealed record PieSlice(string Key, double Value, double Fraction, double StartAngle, double SweepAngle, string PathData);

/// <summary>Lays values out as the sectors of a ring chart, each with the path that draws it.</summary>
public static class PieChart
{
    /// <summary>Builds the sectors.</summary>
    /// <param name="values">Keys and values, in the order the sectors run clockwise from twelve o'clock. Values that are not positive are left out.</param>
    /// <param name="size">The chart's width and height. Positive.</param>
    /// <param name="thickness">How much of the radius the ring takes, 0 exclusive to 1 inclusive; 1 is a full pie.</param>
    /// <returns>The sectors, in order; empty when no value is positive.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not positive or <paramref name="thickness"/> is outside its range.</exception>
    public static IReadOnlyList<PieSlice> Build(IEnumerable<(string Key, double Value)> values, double size = 200, double thickness = 0.42)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(thickness);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(thickness, 1);

        var positive = values.Where(pair => pair.Value > 0 && double.IsFinite(pair.Value)).ToList();
        var total = positive.Sum(pair => pair.Value);
        if (positive.Count == 0 || total <= 0)
        {
            return [];
        }

        var outer = size / 2;
        var inner = outer * (1 - thickness);
        var slices = new List<PieSlice>();
        var angle = 0.0;
        foreach (var (key, value) in positive)
        {
            var fraction = value / total;
            var sweep = fraction * 360;
            slices.Add(new PieSlice(key, value, fraction, angle, sweep, Sector(outer, inner, angle, sweep)));
            angle += sweep;
        }

        return slices;
    }

    private static string Sector(double outer, double inner, double start, double sweep)
    {
        var c = outer;
        if (sweep >= 359.999)
        {
            // A whole ring: two half circles out, two back, which the even-odd fill leaves hollow.
            return string.Create(CultureInfo.InvariantCulture,
                $"M{F(c)},{F(c - outer)} A{F(outer)},{F(outer)} 0 1 1 {F(c)},{F(c + outer)} A{F(outer)},{F(outer)} 0 1 1 {F(c)},{F(c - outer)} Z "
                + $"M{F(c)},{F(c - inner)} A{F(inner)},{F(inner)} 0 1 0 {F(c)},{F(c + inner)} A{F(inner)},{F(inner)} 0 1 0 {F(c)},{F(c - inner)} Z");
        }

        var large = sweep > 180 ? 1 : 0;
        var (x1, y1) = Point(c, outer, start);
        var (x2, y2) = Point(c, outer, start + sweep);
        var (x3, y3) = Point(c, inner, start + sweep);
        var (x4, y4) = Point(c, inner, start);
        return string.Create(CultureInfo.InvariantCulture,
            $"M{F(x1)},{F(y1)} A{F(outer)},{F(outer)} 0 {large} 1 {F(x2)},{F(y2)} L{F(x3)},{F(y3)} A{F(inner)},{F(inner)} 0 {large} 0 {F(x4)},{F(y4)} Z");
    }

    private static (double X, double Y) Point(double centre, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return (centre + (radius * Math.Sin(radians)), centre - (radius * Math.Cos(radians)));
    }

    private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
