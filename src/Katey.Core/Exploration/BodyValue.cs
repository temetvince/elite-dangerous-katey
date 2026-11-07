namespace Katey.Core.Exploration;

/// <summary>
/// Estimates what Universal Cartographics pays for a scanned body.
/// </summary>
/// <remarks>
/// This is the community's reverse-engineered formula, not game data: a base value per body type, scaled
/// by mass, multiplied for mapping and for being first. The result is an estimate; the game's own figure
/// can differ by a few percent. All inputs come from the journal's <c>Scan</c> event.
/// </remarks>
public static class BodyValue
{
    private const double MassExponent = 0.2;
    private const double Q = 0.56591828;

    /// <summary>Estimates a planet's value.</summary>
    /// <param name="planetClass">The <c>PlanetClass</c> string from the journal.</param>
    /// <param name="terraformable">Whether <c>TerraformState</c> says the body is terraformable.</param>
    /// <param name="earthMasses">The body's mass in Earth masses. Non-positive values are treated as 1.</param>
    /// <param name="firstDiscovered">Whether the player is the first to discover it.</param>
    /// <param name="mapped">Whether the player has mapped it with the surface scanner.</param>
    /// <param name="firstMapped">Whether the player is the first to map it. Ignored unless <paramref name="mapped"/>.</param>
    /// <returns>Credits, at least 500.</returns>
    public static long Planet(string? planetClass, bool terraformable, double earthMasses, bool firstDiscovered, bool mapped, bool firstMapped)
    {
        var k = PlanetBase(planetClass, terraformable);
        return Estimate(k, earthMasses, firstDiscovered, mapped, firstMapped);
    }

    /// <summary>Estimates a star's value.</summary>
    /// <param name="starClass">The <c>StarType</c> string from the journal.</param>
    /// <param name="solarMasses">The star's mass in solar masses. Non-positive values are treated as 1.</param>
    /// <param name="firstDiscovered">Whether the player is the first to discover it.</param>
    /// <returns>Credits.</returns>
    public static long Star(string? starClass, double solarMasses, bool firstDiscovered)
    {
        var mass = solarMasses > 0 ? solarMasses : 1;
        var k = StarClasses.Classify(starClass) switch
        {
            StarKind.WhiteDwarf => 14057.0,
            StarKind.Neutron => 22628.0,
            StarKind.BlackHole => string.Equals(starClass, "SupermassiveBlackHole", StringComparison.OrdinalIgnoreCase) ? 33.5678 : 22628.0,
            StarKind.Scoopable => 1200.0,
            StarKind.Other => 1200.0,
            _ => 1200.0,
        };
        var value = k + (mass * k / 66.25);
        return (long)Math.Round(value * (firstDiscovered ? 2.6 : 1.0));
    }

    /// <summary>Base values by planet class, most specific first; the second value applies when terraformable.</summary>
    private static readonly (string Needle, double Base, double? TerraformableBase)[] PlanetBases =
    [
        ("Metal rich", 21790, null),
        ("Metal-rich", 21790, null),
        ("Ammonia", 96932, null),
        ("class II gas giant", 9654, null),
        ("class I gas giant", 1656, null),
        ("High metal content", 9654, 9654 + 100677),
        ("Earthlike", 64831 + 116295, null),
        ("Earth-like", 64831 + 116295, null),
        ("Water world", 64831, 64831 + 116295),
    ];

    private static double PlanetBase(string? planetClass, bool terraformable)
    {
        var cls = planetClass ?? "";
        foreach (var (needle, baseValue, terraformableBase) in PlanetBases)
        {
            if (cls.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return terraformable && terraformableBase is { } raised ? raised : baseValue;
            }
        }

        return terraformable ? 300 + 93328 : 300;
    }

    private static long Estimate(double k, double earthMasses, bool firstDiscovered, bool mapped, bool firstMapped)
    {
        var mass = earthMasses > 0 ? earthMasses : 1;
        var value = k + (k * Q * Math.Pow(mass, MassExponent));
        if (mapped)
        {
            value *= firstDiscovered && firstMapped ? 3.699622554
                : firstMapped ? 8.0956
                : 3.3333333333;
        }

        value = Math.Max(500, value);
        value *= firstDiscovered ? 2.6 : 1.0;
        return (long)Math.Round(value);
    }
}
