using System.Collections.Frozen;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Katey.Core.Fleet;

/// <summary>Display names for the hull symbols the journal and EDSY use, such as <c>anacondamkii</c>.</summary>
public static partial class ShipNames
{
    private static readonly FrozenDictionary<string, string> Known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["adder"] = "Adder",
        ["anaconda"] = "Anaconda",
        ["anacondamkii"] = "Anaconda Mk II",
        ["asp"] = "Asp Explorer",
        ["asp_scout"] = "Asp Scout",
        ["belugaliner"] = "Beluga Liner",
        ["cobramkiii"] = "Cobra Mk III",
        ["cobramkiv"] = "Cobra Mk IV",
        ["cobramkv"] = "Cobra Mk V",
        ["corsair"] = "Corsair",
        ["cutter"] = "Imperial Cutter",
        ["diamondback"] = "Diamondback Scout",
        ["diamondbackxl"] = "Diamondback Explorer",
        ["dolphin"] = "Dolphin",
        ["eagle"] = "Eagle",
        ["empire_courier"] = "Imperial Courier",
        ["empire_eagle"] = "Imperial Eagle",
        ["empire_trader"] = "Imperial Clipper",
        ["explorer_nx"] = "Caspian Explorer",
        ["federation_corvette"] = "Federal Corvette",
        ["federation_dropship"] = "Federal Dropship",
        ["federation_dropship_mkii"] = "Federal Assault Ship",
        ["federation_gunship"] = "Federal Gunship",
        ["ferdelance"] = "Fer-de-Lance",
        ["hauler"] = "Hauler",
        ["independant_trader"] = "Keelback",
        ["krait_light"] = "Krait Phantom",
        ["krait_mkii"] = "Krait Mk II",
        ["mamba"] = "Mamba",
        ["mandalay"] = "Mandalay",
        ["mediumtransport01"] = "Lynx Highliner",
        ["orca"] = "Orca",
        ["panthermkii"] = "Panther Clipper Mk II",
        ["python"] = "Python",
        ["python_nx"] = "Python Mk II",
        ["sidewinder"] = "Sidewinder",
        ["type6"] = "Type-6 Transporter",
        ["type7"] = "Type-7 Transporter",
        ["type8"] = "Type-8 Transporter",
        ["type9"] = "Type-9 Heavy",
        ["type9_military"] = "Type-10 Defender",
        ["typex"] = "Alliance Chieftain",
        ["typex_2"] = "Alliance Crusader",
        ["typex_3"] = "Alliance Challenger",
        ["viper"] = "Viper Mk III",
        ["viper_mkiv"] = "Viper Mk IV",
        ["vulture"] = "Vulture",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>A display name for a hull symbol.</summary>
    /// <param name="symbol">The hull symbol, any letter case, such as <c>anacondamkii</c>.</param>
    /// <returns>
    /// The game's name when the hull is known. Otherwise a tidied form of the symbol: underscores become
    /// spaces, a trailing <c>mkii</c>-style suffix becomes <c>Mk II</c>, and each word is capitalised.
    /// Never empty for a non-empty symbol.
    /// </returns>
    public static string Display(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        if (Known.TryGetValue(symbol.Trim(), out var known))
        {
            return known;
        }

        var tidy = symbol.Trim().Replace('_', ' ');
        tidy = MarkPattern().Replace(tidy, match => " Mk " + match.Groups[1].Value.ToUpperInvariant());
        tidy = string.Join(' ', tidy.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        tidy = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(tidy.ToUpperInvariant().ToLowerInvariant());
        return tidy.Replace(" Mk Ii", " Mk II", StringComparison.Ordinal)
            .Replace(" Mk Iii", " Mk III", StringComparison.Ordinal)
            .Replace(" Mk Iv", " Mk IV", StringComparison.Ordinal)
            .Replace(" Mk V", " Mk V", StringComparison.Ordinal)
            .Trim();
    }

    /// <summary>The name to show for a ship: the journal's localised name when it offers one, else <see cref="Display"/> of the symbol.</summary>
    /// <param name="symbol">The hull symbol.</param>
    /// <param name="journalName">The journal's localised name, or <see langword="null"/> or the symbol itself when it offers none.</param>
    /// <returns>Never empty for a non-empty symbol.</returns>
    public static string Prefer(string symbol, string? journalName)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return journalName is { Length: > 0 } && !string.Equals(journalName, symbol, StringComparison.OrdinalIgnoreCase)
            ? journalName
            : Display(symbol);
    }

    [GeneratedRegex("mk(i{1,3}|iv|v)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MarkPattern();
}
