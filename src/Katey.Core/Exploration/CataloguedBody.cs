namespace Katey.Core.Exploration;

/// <summary>
/// One body as a catalogue outside the game describes it, in the journal's spellings so the same rules
/// value and predict it. A catalogue knows the body, not the player: nothing here says whether the player
/// scanned, mapped or landed on it.
/// </summary>
/// <param name="BodyId">The body id the journal would give it, unique within the system.</param>
/// <param name="Name">Full body name.</param>
/// <param name="IsStar">Whether it is a star.</param>
/// <param name="ParentStarId">The body id of the star it orbits, or <see langword="null"/> for a star or when the catalogue does not say.</param>
/// <param name="Class">Star type or planet class as the journal spells it, or <see langword="null"/> when the catalogue's name is not known.</param>
/// <param name="DistanceLs">Distance from the arrival point in light seconds.</param>
/// <param name="Landable">Whether a ship can land on it.</param>
/// <param name="Atmosphere">Atmosphere description, or empty.</param>
/// <param name="Volcanism">Volcanism description, or empty.</param>
/// <param name="GravityG">Surface gravity in g, or <see langword="null"/>.</param>
/// <param name="TemperatureK">Surface temperature in kelvin, or <see langword="null"/>.</param>
/// <param name="PressurePa">Surface pressure in pascals, or <see langword="null"/>.</param>
/// <param name="Mass">Earth masses for a planet, solar masses for a star, or <see langword="null"/>.</param>
/// <param name="Terraformable">Whether the body can be terraformed.</param>
/// <param name="HasRings">Whether it has rings.</param>
/// <param name="Materials">Surface material display name → percentage.</param>
/// <param name="BiologicalSignals">Number of biological signals, or 0.</param>
/// <param name="GeologicalSignals">Number of geological signals, or 0.</param>
/// <param name="Genera">Genus symbols the catalogue lists for it, as the journal spells them.</param>
/// <param name="UpdatedAt">When the catalogue last updated the body, or <see langword="null"/>.</param>
public sealed record CataloguedBody(
    int BodyId,
    string Name,
    bool IsStar,
    int? ParentStarId,
    string? Class,
    double DistanceLs,
    bool Landable,
    string Atmosphere,
    string Volcanism,
    double? GravityG,
    double? TemperatureK,
    double? PressurePa,
    double? Mass,
    bool Terraformable,
    bool HasRings,
    IReadOnlyDictionary<string, double> Materials,
    int BiologicalSignals,
    int GeologicalSignals,
    IReadOnlyList<string> Genera,
    DateTimeOffset? UpdatedAt);

/// <summary>Turns the names Spansh and EDSM use for bodies into the journal's spellings.</summary>
public static class CatalogueNames
{
    private static readonly Dictionary<string, string> Planets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Metal-rich body"] = "Metal rich body",
        ["High metal content world"] = "High metal content body",
        ["Rocky body"] = "Rocky body",
        ["Icy body"] = "Icy body",
        ["Rocky Ice world"] = "Rocky ice body",
        ["Earth-like world"] = "Earthlike body",
        ["Water world"] = "Water world",
        ["Ammonia world"] = "Ammonia world",
        ["Water giant"] = "Water giant",
        ["Water giant with life"] = "Water giant with life",
        ["Gas giant with water-based life"] = "Gas giant with water based life",
        ["Gas giant with ammonia-based life"] = "Gas giant with ammonia based life",
        ["Class I gas giant"] = "Sudarsky class I gas giant",
        ["Class II gas giant"] = "Sudarsky class II gas giant",
        ["Class III gas giant"] = "Sudarsky class III gas giant",
        ["Class IV gas giant"] = "Sudarsky class IV gas giant",
        ["Class V gas giant"] = "Sudarsky class V gas giant",
        ["Helium-rich gas giant"] = "Helium rich gas giant",
        ["Helium gas giant"] = "Helium gas giant",
    };

    /// <summary>The journal's planet class for a catalogue's sub type.</summary>
    /// <param name="subType">The catalogue's name, such as "High metal content world".</param>
    /// <returns>The journal's spelling, or the name unchanged when it is not one the table knows.</returns>
    public static string PlanetClass(string subType)
    {
        ArgumentNullException.ThrowIfNull(subType);
        return Planets.GetValueOrDefault(subType.Trim(), subType.Trim());
    }

    /// <summary>The journal's star type for a catalogue's sub type.</summary>
    /// <param name="subType">The catalogue's name, such as "M (Red dwarf) Star", "M (Red giant) Star", "Neutron Star", "White Dwarf (DA) Star" or "Black Hole".</param>
    /// <returns>The journal's class string, such as "M", "M_RedGiant", "N", "DA" or "H"; the leading word of a name the table does not know.</returns>
    public static string StarClass(string subType)
    {
        ArgumentNullException.ThrowIfNull(subType);
        var name = subType.Trim();
        if (name.Contains("Supermassive", StringComparison.OrdinalIgnoreCase))
        {
            return "SupermassiveBlackHole";
        }

        if (name.Contains("Black Hole", StringComparison.OrdinalIgnoreCase))
        {
            return "H";
        }

        if (name.StartsWith("Neutron", StringComparison.OrdinalIgnoreCase))
        {
            return "N";
        }

        if (name.StartsWith("White Dwarf", StringComparison.OrdinalIgnoreCase))
        {
            var open = name.IndexOf('(', StringComparison.Ordinal);
            var close = name.IndexOf(')', StringComparison.Ordinal);
            return open >= 0 && close > open ? name[(open + 1)..close] : "D";
        }

        if (name.StartsWith("T Tauri", StringComparison.OrdinalIgnoreCase))
        {
            return "TTS";
        }

        if (name.StartsWith("Herbig", StringComparison.OrdinalIgnoreCase))
        {
            return "AeBe";
        }

        if (name.StartsWith("Wolf-Rayet", StringComparison.OrdinalIgnoreCase))
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 3 && parts[1].Length <= 2 ? "W" + parts[1] : "W";
        }

        var first = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var letter = first.EndsWith("-type", StringComparison.OrdinalIgnoreCase) ? first[..^5] : first;
        return GiantSuffix(name) is { } giant ? $"{letter}_{giant}" : letter;
    }

    /// <summary>
    /// The journal's giant suffix for a catalogue's parenthetical: "Red giant" is "RedGiant", "Blue-White super
    /// giant" is "BlueWhiteSuperGiant". The journal drops the yellow from its orange and white giants.
    /// </summary>
    private static string? GiantSuffix(string name)
    {
        var open = name.IndexOf('(', StringComparison.Ordinal);
        var close = name.IndexOf(')', StringComparison.Ordinal);
        if (open < 0 || close <= open)
        {
            return null;
        }

        var words = name[(open + 1)..close];
        if (!words.Contains("giant", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        words = words.Replace("Yellow-Orange", "Orange", StringComparison.OrdinalIgnoreCase).Replace("White-Yellow", "White", StringComparison.OrdinalIgnoreCase);
        return string.Concat(words.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries).Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }
}
