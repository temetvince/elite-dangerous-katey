using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Katey.Core.Fleet;

/// <summary>
/// Display names for item symbols the module table lacks: the hull armour the game names by hull and grade,
/// and compound symbols such as <c>int_largecargorack_size6_class1</c> read as words.
/// </summary>
/// <remarks>
/// Stateless. Splitting a compound token into words needs a lexicon, which the caller builds from the names it
/// already knows plus <see cref="ExtraWords"/>; a token no lexicon words cover exactly is shown as one word.
/// </remarks>
public static partial class ModuleSymbolNames
{
    private const string Ratings = "EDCBA";

    private static readonly FrozenDictionary<string, string> Armour = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["grade1"] = "Lightweight Alloy",
        ["grade2"] = "Reinforced Alloy",
        ["grade3"] = "Military Grade Composite",
        ["mirrored"] = "Mirrored Surface Composite",
        ["reactive"] = "Reactive Surface Composite",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> Spellings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["mkii"] = "Mk II",
        ["mk2"] = "Mk II",
        ["mkiii"] = "Mk III",
        ["mk3"] = "Mk III",
        ["mkiv"] = "Mk IV",
        ["mkv"] = "Mk V",
        ["fdl"] = "FDL",
        ["sco"] = "SCO",
        ["srv"] = "SRV",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> Mounts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["fixed"] = "Fixed",
        ["gimbal"] = "Gimballed",
        ["turret"] = "Turreted",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, int> HardpointSizes = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["tiny"] = 0,
        ["small"] = 1,
        ["medium"] = 2,
        ["large"] = 3,
        ["huge"] = 4,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Gets the words the module table's names never contain but its missing symbols are built from.</summary>
    /// <value>Lower-case words, compared ordinally. Every lexicon handed to <see cref="Describe"/> should include them.</value>
    public static IReadOnlySet<string> ExtraWords { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "modular", "cargo", "bay", "door", "buggy", "large", "passenger", "cabin", "fighter", "planet", "approach",
        "suite", "docking", "computer", "supercruise", "assist", "advanced", "standard",
        "fdl", "mkii", "mk2", "mkiii", "mk3", "mkiv", "mkv",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The words of a display name, for a lexicon.</summary>
    /// <param name="name">A display name such as "Cargo Rack 1E".</param>
    /// <returns>Each run of three or more letters, lower case, in order. Empty for a name with none.</returns>
    public static IEnumerable<string> WordsOf(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return WordPattern().Matches(name).Select(match => match.Value.ToLowerInvariant());
    }

    /// <summary>The armour a symbol stands for.</summary>
    /// <param name="symbol">An item symbol, any letter case, such as <c>anacondamkii_armour_grade3</c>.</param>
    /// <returns>The armour's name as the outfitting screen shows it, or <see langword="null"/> when the symbol is not hull armour.</returns>
    public static string? ArmourName(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        var match = ArmourPattern().Match(symbol.Trim());
        return match.Success ? Armour[match.Groups[1].Value.ToLowerInvariant()] : null;
    }

    /// <summary>A symbol without the suffix the game adds to a module that came free with a ship or as a hull's default.</summary>
    /// <param name="symbol">An item symbol, any letter case.</param>
    /// <returns>The symbol with a trailing <c>_free</c> or <c>_default</c> removed, or the symbol unchanged.</returns>
    public static string WithoutSuffix(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return SuffixPattern().Replace(symbol.Trim(), "");
    }

    /// <summary>
    /// The one symbol every spelling of an item reduces to, so a fit's module and a ship's module compare as
    /// the same item. Letter case is dropped, as is a <c>_free</c> or <c>_default</c> suffix, and every
    /// ship-specific cargo hatch, such as <c>modularcargobaydoorfdl</c>, reads as <c>modularcargobaydoor</c>.
    /// </summary>
    /// <param name="symbol">An item symbol, any letter case.</param>
    /// <returns>The canonical symbol, lower case. Empty for an empty symbol.</returns>
    public static string Canonical(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        var bare = WithoutSuffix(symbol).ToLowerInvariant();
        return bare.StartsWith(CargoHatch, StringComparison.Ordinal) ? CargoHatch : bare;
    }

    /// <summary>The cargo hatch's symbol as EDSY writes it; the game appends a hull code on some ships.</summary>
    private const string CargoHatch = "modularcargobaydoor";

    /// <summary>
    /// Reads a symbol as words: a leading <c>int</c> or <c>hpt</c> is dropped, <c>sizeN</c> and <c>classN</c>
    /// become the size digit and rating letter, a mount becomes Fixed, Gimballed or Turreted, a hardpoint's
    /// size word becomes its digit, and every other token is split into lexicon words and capitalised.
    /// </summary>
    /// <param name="symbol">An item symbol, any letter case.</param>
    /// <param name="lexicon">Lower-case words, compared ordinally, that compound tokens may be split into.</param>
    /// <returns>Words, then the size and rating, then the mount, such as "Large Cargo Rack 6E". The trimmed symbol itself when nothing in it reads as a word.</returns>
    public static string Describe(string symbol, IReadOnlySet<string> lexicon)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(lexicon);
        var tokens = symbol.Trim().ToLowerInvariant().Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hardpoint = tokens.Length > 0 && tokens[0] == "hpt";
        var words = new List<string>();
        int? size = null;
        var rating = "";
        var mount = "";
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if ((index == 0 && token is "int" or "hpt") || token is "free" or "default")
            {
                continue;
            }

            if (SizePattern().Match(token) is { Success: true } sized)
            {
                size = sized.Groups[1].Value[0] - '0';
            }
            else if (ClassPattern().Match(token) is { Success: true } classed)
            {
                var grade = classed.Groups[1].Value[0] - '0';
                rating = grade is >= 1 and <= 5 ? Ratings[grade - 1].ToString() : "";
            }
            else if (Mounts.TryGetValue(token, out var mounted))
            {
                mount = mounted;
            }
            else if (hardpoint && HardpointSizes.TryGetValue(token, out var hardpointSize))
            {
                size = hardpointSize;
            }
            else
            {
                words.AddRange((Segment(token, lexicon) ?? [token]).Select(Capitalise));
            }
        }

        var tail = size is { } digit ? $"{digit}{rating}" : rating;
        var parts = new[] { string.Join(' ', words), tail, mount }.Where(part => part.Length > 0);
        var name = string.Join(' ', parts);
        return name.Length > 0 ? name : symbol.Trim();
    }

    /// <summary>Splits a run of letters into the fewest lexicon words that cover it exactly.</summary>
    /// <param name="token">Lower-case letters and digits without separators.</param>
    /// <param name="lexicon">Lower-case words, compared ordinally.</param>
    /// <returns>The words in order, or <see langword="null"/> when no sequence of lexicon words spells the token.</returns>
    public static IReadOnlyList<string>? Segment(string token, IReadOnlySet<string> lexicon)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(lexicon);
        if (token.Length == 0)
        {
            return null;
        }

        if (lexicon.Contains(token))
        {
            return [token];
        }

        var best = new int[token.Length + 1];
        var cut = new int[token.Length + 1];
        Array.Fill(best, int.MaxValue);
        best[0] = 0;
        for (var end = 1; end <= token.Length; end++)
        {
            for (var start = 0; start < end; start++)
            {
                if (best[start] != int.MaxValue && best[start] + 1 < best[end] && lexicon.Contains(token[start..end]))
                {
                    best[end] = best[start] + 1;
                    cut[end] = start;
                }
            }
        }

        if (best[token.Length] == int.MaxValue)
        {
            return null;
        }

        var words = new List<string>();
        for (var end = token.Length; end > 0; end = cut[end])
        {
            words.Add(token[cut[end]..end]);
        }

        words.Reverse();
        return words;
    }

    private static string Capitalise(string word)
        => Spellings.TryGetValue(word, out var spelled) ? spelled : char.ToUpperInvariant(word[0]) + word[1..];

    [GeneratedRegex("[A-Za-z]{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();

    [GeneratedRegex("_armour_(grade1|grade2|grade3|mirrored|reactive)(?:_default)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ArmourPattern();

    [GeneratedRegex("_(free|default)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SuffixPattern();

    [GeneratedRegex("^size([0-9])$", RegexOptions.CultureInvariant)]
    private static partial Regex SizePattern();

    [GeneratedRegex("^class([0-9])$", RegexOptions.CultureInvariant)]
    private static partial Regex ClassPattern();
}
