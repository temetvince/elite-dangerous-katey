namespace Katey.Core.Exploration;

/// <summary>What a star means to a ship passing through: fuel, a neutron boost, a hazard, or nothing.</summary>
public enum StarKind
{
    /// <summary>A main-sequence or giant star of class O, B, A, F, G, K or M: the fuel scoop works here.</summary>
    Scoopable,

    /// <summary>A neutron star: a jet cone boosts the drive and the star cannot be scooped.</summary>
    Neutron,

    /// <summary>A white dwarf of any class: a jet cone, a lesser boost, and a hazard.</summary>
    WhiteDwarf,

    /// <summary>A black hole, stellar or supermassive: nothing to scoop and a hazard.</summary>
    BlackHole,

    /// <summary>Anything else: a brown dwarf, a Wolf-Rayet or carbon star, a rogue planet, or an unknown class.</summary>
    Other,
}

/// <summary>What a star is, as far as a reader wants to know: the family its class belongs to.</summary>
public enum StarFamily
{
    /// <summary>No class given.</summary>
    Unknown,

    /// <summary>A main-sequence or giant star of class O, B, A, F, G, K or M.</summary>
    MainSequence,

    /// <summary>A neutron star.</summary>
    Neutron,

    /// <summary>A white dwarf of any class.</summary>
    WhiteDwarf,

    /// <summary>A black hole, stellar or supermassive.</summary>
    BlackHole,

    /// <summary>A brown dwarf of class L, T or Y.</summary>
    BrownDwarf,

    /// <summary>A T Tauri star.</summary>
    TTauri,

    /// <summary>A Herbig Ae/Be star.</summary>
    HerbigAeBe,

    /// <summary>A Wolf-Rayet star of class W, WN, WNC, WC or WO.</summary>
    WolfRayet,

    /// <summary>A carbon star of class C, CN, CJ, CH, CHd, CS or MS.</summary>
    CarbonStar,

    /// <summary>A rogue planet.</summary>
    RoguePlanet,

    /// <summary>A class the catalog does not know.</summary>
    Other,
}

/// <summary>
/// Reads the star class strings the journal and <c>NavRoute.json</c> use: <c>K</c>, <c>M_RedGiant</c>, <c>N</c>,
/// <c>DA</c>, <c>H</c>, <c>TTS</c> and so on. Says what the class is; the words for a reader are the application's.
/// </summary>
public static class StarClasses
{
    private static readonly HashSet<string> ScoopableClasses = new(StringComparer.OrdinalIgnoreCase) { "O", "B", "A", "F", "G", "K", "M" };
    private static readonly HashSet<string> BrownDwarfClasses = new(StringComparer.OrdinalIgnoreCase) { "L", "T", "Y" };

    /// <summary>What a star means to a ship passing through.</summary>
    /// <param name="starClass">The class as the journal spells it, or <see langword="null"/>.</param>
    /// <returns>The kind; <see cref="StarKind.Other"/> for an empty or unknown class.</returns>
    public static StarKind Classify(string? starClass)
    {
        if (string.IsNullOrWhiteSpace(starClass))
        {
            return StarKind.Other;
        }

        var trimmed = starClass.Trim();
        return Spectral(trimmed) is not null
            ? StarKind.Scoopable
            : trimmed.Equals("N", StringComparison.OrdinalIgnoreCase)
            ? StarKind.Neutron
            : trimmed.Equals("H", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("SupermassiveBlackHole", StringComparison.OrdinalIgnoreCase)
            ? StarKind.BlackHole
            : trimmed.StartsWith('D') && trimmed.Length <= 3 ? StarKind.WhiteDwarf : StarKind.Other;
    }

    /// <summary>Whether the fuel scoop works at a star.</summary>
    /// <param name="starClass">The class as the journal spells it, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> for a main-sequence or giant star of class O to M.</returns>
    public static bool IsScoopable(string? starClass) => Classify(starClass) == StarKind.Scoopable;

    /// <summary>The spectral letter of a scoopable star.</summary>
    /// <param name="starClass">The class as the journal spells it, or <see langword="null"/>.</param>
    /// <returns>The letter in capitals, O to M, or <see langword="null"/> for any other class.</returns>
    public static string? Spectral(string? starClass)
    {
        if (string.IsNullOrWhiteSpace(starClass))
        {
            return null;
        }

        var trimmed = starClass.Trim();
        var underscore = trimmed.IndexOf('_', StringComparison.Ordinal);
        var letter = underscore < 0 ? trimmed : trimmed[..underscore];
        return ScoopableClasses.Contains(letter) && (underscore < 0 || Giant(trimmed) is not null) ? letter.ToUpperInvariant() : null;
    }

    /// <summary>The giant suffix of a class such as <c>M_RedGiant</c>.</summary>
    /// <param name="starClass">The class as the journal spells it, or <see langword="null"/>.</param>
    /// <returns>The suffix after the underscore, such as <c>RedGiant</c> or <c>BlueWhiteSuperGiant</c>, when it names a giant; <see langword="null"/> otherwise.</returns>
    public static string? Giant(string? starClass)
    {
        if (string.IsNullOrWhiteSpace(starClass))
        {
            return null;
        }

        var trimmed = starClass.Trim();
        var underscore = trimmed.IndexOf('_', StringComparison.Ordinal);
        if (underscore < 0 || underscore == trimmed.Length - 1)
        {
            return null;
        }

        var suffix = trimmed[(underscore + 1)..];
        return suffix.Contains("Giant", StringComparison.OrdinalIgnoreCase) ? suffix : null;
    }

    /// <summary>The family a class belongs to, for a reader's description.</summary>
    /// <param name="starClass">The class as the journal spells it, or <see langword="null"/>.</param>
    /// <returns>The family; <see cref="StarFamily.Unknown"/> for no class, <see cref="StarFamily.Other"/> for one the catalog does not know.</returns>
    public static StarFamily Family(string? starClass)
    {
        var trimmed = starClass?.Trim() ?? "";
        return trimmed.Length == 0
            ? StarFamily.Unknown
            : Classify(trimmed) switch
            {
                StarKind.Scoopable => StarFamily.MainSequence,
                StarKind.Neutron => StarFamily.Neutron,
                StarKind.WhiteDwarf => StarFamily.WhiteDwarf,
                StarKind.BlackHole => StarFamily.BlackHole,
                StarKind.Other when BrownDwarfClasses.Contains(trimmed) => StarFamily.BrownDwarf,
                StarKind.Other when trimmed.Equals("TTS", StringComparison.OrdinalIgnoreCase) => StarFamily.TTauri,
                StarKind.Other when trimmed.Equals("AeBe", StringComparison.OrdinalIgnoreCase) => StarFamily.HerbigAeBe,
                StarKind.Other when trimmed.StartsWith('W') && trimmed.Length <= 3 => StarFamily.WolfRayet,
                StarKind.Other when trimmed.StartsWith('C') && trimmed.Length <= 3 => StarFamily.CarbonStar,
                StarKind.Other when trimmed.Equals("RoguePlanet", StringComparison.OrdinalIgnoreCase) => StarFamily.RoguePlanet,
                StarKind.Other => StarFamily.Other,
                _ => StarFamily.Other,
            };
    }
}
