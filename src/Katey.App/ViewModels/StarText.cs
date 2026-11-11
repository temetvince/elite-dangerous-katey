using System.Text;
using Katey.App.Text;
using Katey.Core.Exploration;

namespace Katey.App.ViewModels;

/// <summary>A star in words, from what <see cref="StarClasses"/> reads off its class string.</summary>
public static class StarText
{
    /// <summary>The class in brief: the spectral letter with its giant words, such as "M red giant", or the class as written.</summary>
    /// <param name="starClass">The class as the journal spells it, or <see langword="null"/>.</param>
    /// <returns>The text; empty for no class.</returns>
    public static string Brief(string? starClass)
    {
        var trimmed = starClass?.Trim() ?? "";
        return StarClasses.Spectral(trimmed) is { } letter && StarClasses.Giant(trimmed) is { } giant
            ? Strings.Format(Keys.Star_Brief, ("letter", letter), ("giant", GiantWords(giant)))
            : trimmed;
    }

    /// <summary>One line on a star for a reader: its class in words. Whether it can be scooped is <see cref="Label"/>'s to say, so the two sit side by side without repeating.</summary>
    /// <param name="starClass">The class as the journal spells it, or <see langword="null"/>.</param>
    /// <returns>Such as "Class K star", "Class M red giant star", "Neutron star", "White dwarf, class DA", "Black hole", "Class L brown dwarf"; "Star" for no class.</returns>
    public static string Describe(string? starClass)
    {
        var trimmed = starClass?.Trim() ?? "";
        var key = StarClasses.Family(trimmed) switch
        {
            StarFamily.Unknown => Keys.Star_Unknown,
            StarFamily.MainSequence => Keys.Star_Class,
            StarFamily.Neutron => Keys.Star_Neutron,
            StarFamily.WhiteDwarf => Keys.Star_WhiteDwarf,
            StarFamily.BlackHole => Keys.Star_BlackHole,
            StarFamily.BrownDwarf => Keys.Star_BrownDwarf,
            StarFamily.TTauri => Keys.Star_TTauri,
            StarFamily.HerbigAeBe => Keys.Star_HerbigAeBe,
            StarFamily.WolfRayet => Keys.Star_WolfRayet,
            StarFamily.CarbonStar => Keys.Star_CarbonStar,
            StarFamily.RoguePlanet => Keys.Star_RoguePlanet,
            StarFamily.Other => Keys.Star_Class,
            _ => Keys.Star_Class,
        };
        return Strings.Format(key, ("class", StarClasses.Family(trimmed) == StarFamily.MainSequence ? Brief(trimmed) : trimmed));
    }

    /// <summary>The short label for a kind, for a route stop: "scoopable", "neutron star", "white dwarf", "black hole" or "not scoopable".</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The label in lower case.</returns>
    public static string Label(StarKind kind) => kind switch
    {
        StarKind.Scoopable => Strings.Get(Keys.Star_LabelScoopable),
        StarKind.Neutron => Strings.Get(Keys.Star_LabelNeutron),
        StarKind.WhiteDwarf => Strings.Get(Keys.Star_LabelWhiteDwarf),
        StarKind.BlackHole => Strings.Get(Keys.Star_LabelBlackHole),
        StarKind.Other => Strings.Get(Keys.Star_LabelNotScoopable),
        _ => Strings.Get(Keys.Star_LabelNotScoopable),
    };

    /// <summary>The words for a giant suffix: the table's for the five the game uses, else the suffix split into lower-case words.</summary>
    /// <param name="giant">A suffix from <see cref="StarClasses.Giant"/>.</param>
    /// <returns>Such as "red giant" or "blue-white supergiant".</returns>
    public static string GiantWords(string giant)
    {
        ArgumentNullException.ThrowIfNull(giant);
        var key = giant.ToUpperInvariant() switch
        {
            "REDGIANT" => Keys.Star_RedGiant,
            "REDSUPERGIANT" => Keys.Star_RedSuperGiant,
            "ORANGEGIANT" => Keys.Star_OrangeGiant,
            "BLUEWHITESUPERGIANT" => Keys.Star_BlueWhiteSuperGiant,
            "WHITESUPERGIANT" => Keys.Star_WhiteSuperGiant,
            _ => null,
        };
        if (key is not null)
        {
            return Strings.Get(key);
        }

        var split = new StringBuilder();
        foreach (var character in giant)
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
