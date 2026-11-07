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

    /// <summary>A black hole, supermassive or not.</summary>
    BlackHole,

    /// <summary>Anything else: brown dwarfs, T Tauri, Herbig, Wolf-Rayet and carbon stars, and what the journal cannot name.</summary>
    Other,
}

/// <summary>
/// Reads the class strings the journal and <c>NavRoute.json</c> use for stars. A giant carries its spectral
/// class before an underscore, such as <c>M_RedGiant</c> or <c>A_BlueWhiteSuperGiant</c>, and scoops like
/// any star of that class.
/// </summary>
public static class StarClasses
{
    private static readonly HashSet<string> ScoopableClasses = new(StringComparer.OrdinalIgnoreCase) { "O", "B", "A", "F", "G", "K", "M" };
    private static readonly HashSet<string> BrownDwarfClasses = new(StringComparer.OrdinalIgnoreCase) { "L", "T", "Y" };

    private static readonly Dictionary<string, string> GiantWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RedGiant"] = "red giant",
        ["RedSuperGiant"] = "red supergiant",
        ["OrangeGiant"] = "orange giant",
        ["BlueWhiteSuperGiant"] = "blue-white supergiant",
        ["WhiteSuperGiant"] = "white supergiant",
    };

    /// <summary>Sorts a class string into what it means for a ship.</summary>
    /// <param name="starClass">The class as the journal spells it, such as "K", "M_RedGiant", "N", "DA" or "H". Case does not matter.</param>
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

    /// <summary>Whether the fuel scoop works at the star.</summary>
    /// <param name="starClass">The class as the journal spells it.</param>
    /// <returns><see langword="true"/> for class O, B, A, F, G, K or M, giants included.</returns>
    public static bool IsScoopable(string? starClass) => Classify(starClass) == StarKind.Scoopable;

    /// <summary>The spectral letter of a scoopable star.</summary>
    /// <param name="starClass">The class as the journal spells it.</param>
    /// <returns>"O" to "M" in upper case for a scoopable star, a giant's letter included; <see langword="null"/> for anything else.</returns>
    public static string? Spectral(string? starClass)
    {
        if (string.IsNullOrWhiteSpace(starClass))
        {
            return null;
        }

        var trimmed = starClass.Trim();
        var underscore = trimmed.IndexOf('_', StringComparison.Ordinal);
        var letter = underscore < 0 ? trimmed : trimmed[..underscore];
        return ScoopableClasses.Contains(letter) && (underscore < 0 || GiantSuffix(trimmed) is not null) ? letter.ToUpperInvariant() : null;
    }

    /// <summary>The class in a reader's words, briefly: "K", "M red giant", "N", "DA".</summary>
    /// <param name="starClass">The class as the journal spells it.</param>
    /// <returns>The spectral letter with the giant's words after it, or the class string trimmed; empty for none.</returns>
    public static string Brief(string? starClass)
    {
        var trimmed = starClass?.Trim() ?? "";
        return Spectral(trimmed) is { } letter && GiantSuffix(trimmed) is { } giant ? $"{letter} {giant}" : trimmed;
    }

    /// <summary>One line on a star for a reader: its class in words. Whether it can be scooped is <see cref="Label"/>'s to say, so the two sit side by side without repeating.</summary>
    /// <param name="starClass">The class as the journal spells it, or <see langword="null"/>.</param>
    /// <returns>Such as "Class K star", "Class M red giant star", "Neutron star", "White dwarf, class DA", "Black hole", "Class L brown dwarf"; "Star" for no class.</returns>
    public static string Describe(string? starClass)
    {
        var trimmed = starClass?.Trim() ?? "";
        return Classify(trimmed) switch
        {
            StarKind.Scoopable => $"Class {Brief(trimmed)} star",
            StarKind.Neutron => "Neutron star",
            StarKind.WhiteDwarf => $"White dwarf, class {trimmed}",
            StarKind.BlackHole => "Black hole",
            StarKind.Other => Other(trimmed),
            _ => Other(trimmed),
        };
    }

    /// <summary>The short label for a kind, for a route stop: "scoopable", "neutron star", "white dwarf", "black hole" or "not scoopable".</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The label in lower case.</returns>
    public static string Label(StarKind kind) => kind switch
    {
        StarKind.Scoopable => "scoopable",
        StarKind.Neutron => "neutron star",
        StarKind.WhiteDwarf => "white dwarf",
        StarKind.BlackHole => "black hole",
        StarKind.Other => "not scoopable",
        _ => "not scoopable",
    };

    private static string Other(string starClass) => starClass switch
    {
        "" => "Star",
        _ when BrownDwarfClasses.Contains(starClass) => $"Class {starClass} brown dwarf",
        _ when starClass.Equals("TTS", StringComparison.OrdinalIgnoreCase) => "T Tauri star",
        _ when starClass.Equals("AeBe", StringComparison.OrdinalIgnoreCase) => "Herbig Ae/Be star",
        _ when starClass.StartsWith('W') && starClass.Length <= 3 => $"Wolf-Rayet star, class {starClass}",
        _ when starClass.StartsWith('C') && starClass.Length <= 3 => $"Carbon star, class {starClass}",
        _ when starClass.Equals("RoguePlanet", StringComparison.OrdinalIgnoreCase) => "Rogue planet",
        _ => $"Class {starClass} star",
    };

    /// <summary>The words for a giant's suffix: "red giant" for "M_RedGiant"; an unknown suffix is split at its capitals.</summary>
    private static string? GiantSuffix(string starClass)
    {
        var underscore = starClass.IndexOf('_', StringComparison.Ordinal);
        if (underscore < 0 || underscore == starClass.Length - 1)
        {
            return null;
        }

        var suffix = starClass[(underscore + 1)..];
        if (GiantWords.TryGetValue(suffix, out var words))
        {
            return words;
        }

        if (!suffix.Contains("Giant", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var split = new System.Text.StringBuilder();
        foreach (var character in suffix)
        {
            if (char.IsUpper(character) && split.Length > 0)
            {
                split.Append(' ');
            }

            split.Append(char.ToLowerInvariant(character));
        }

        return split.ToString();
    }
}
