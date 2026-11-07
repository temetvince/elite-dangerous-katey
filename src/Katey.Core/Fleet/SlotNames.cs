using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Katey.Core.Fleet;

/// <summary>What a slot holds: the kinds the journal's slot names fall into.</summary>
public enum SlotKind
{
    /// <summary>A core internal with a fixed name, such as <c>FrameShiftDrive</c> or <c>PowerPlant</c>.</summary>
    Core,

    /// <summary>A weapon hardpoint: small, medium, large or huge.</summary>
    Hardpoint,

    /// <summary>A utility mount, which the journal calls a tiny hardpoint.</summary>
    Utility,

    /// <summary>An optional internal, <c>SlotNN_SizeS</c>.</summary>
    Optional,

    /// <summary>A military slot, <c>MilitaryNN</c>.</summary>
    Military,

    /// <summary>Anything else: cosmetic slots, the cockpit, and names the game has not used before.</summary>
    Other,
}

/// <summary>Where a slot is on a ship: its kind and, when the name carries one, its size.</summary>
/// <param name="Kind">The slot's kind.</param>
/// <param name="Size">
/// The size the name carries: 1 to 8 for an optional internal, 1 to 4 for a hardpoint from small to huge, and
/// 0 when the name carries none.
/// </param>
public readonly record struct SlotPlace(SlotKind Kind, int Size);

/// <summary>Readable forms and meanings of the journal's slot names, such as <c>LargeHardpoint3</c> or <c>Slot01_Size5</c>.</summary>
public static partial class SlotNames
{
    private static readonly HashSet<string> CoreSlots = new(StringComparer.OrdinalIgnoreCase)
    {
        "PowerPlant", "MainEngines", "FrameShiftDrive", "LifeSupport", "PowerDistributor", "Radar", "FuelTank",
        "Armour", "PlanetaryApproachSuite", "CargoHatch", "DataLinkScanner", "CodexScanner", "DiscoveryScanner",
    };

    /// <summary>A slot name with spaces where the journal runs words and numbers together.</summary>
    /// <param name="slot">The slot name as the journal spells it.</param>
    /// <returns>
    /// Words split at capital letters and underscores, digits set off as their own word: <c>CargoHatch</c>
    /// becomes "Cargo Hatch", <c>LargeHardpoint3</c> "Large Hardpoint 3", <c>Slot01_Size5</c> "Slot 01 Size 5".
    /// Empty for an empty name.
    /// </returns>
    public static string Display(string slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        var text = new StringBuilder(slot.Length + 8);
        var previous = '\0';
        foreach (var current in slot)
        {
            if (current == '_')
            {
                if (text.Length > 0 && text[^1] != ' ')
                {
                    text.Append(' ');
                }

                previous = ' ';
                continue;
            }

            var startsWord = text.Length > 0 && text[^1] != ' ' && (
                (char.IsUpper(current) && !char.IsUpper(previous))
                || (char.IsDigit(current) && !char.IsDigit(previous))
                || (char.IsLetter(current) && char.IsDigit(previous)));
            if (startsWord)
            {
                text.Append(' ');
            }

            text.Append(current);
            previous = current;
        }

        return text.ToString().Trim();
    }

    /// <summary>What kind of slot a name denotes, and its size when the name carries one.</summary>
    /// <param name="slot">The slot name as the journal spells it, any letter case.</param>
    /// <returns>
    /// The place. A hardpoint's size runs 1 to 4 from small to huge; a tiny hardpoint is a
    /// <see cref="SlotKind.Utility"/> of size 0; an optional internal carries the size from its name; every
    /// other kind has size 0.
    /// </returns>
    public static SlotPlace Parse(string slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (CoreSlots.Contains(slot))
        {
            return new SlotPlace(SlotKind.Core, 0);
        }

        var hardpoint = HardpointPattern().Match(slot);
        if (hardpoint.Success)
        {
            return hardpoint.Groups[1].Value.ToUpperInvariant() switch
            {
                "TINY" => new SlotPlace(SlotKind.Utility, 0),
                "SMALL" => new SlotPlace(SlotKind.Hardpoint, 1),
                "MEDIUM" => new SlotPlace(SlotKind.Hardpoint, 2),
                "LARGE" => new SlotPlace(SlotKind.Hardpoint, 3),
                _ => new SlotPlace(SlotKind.Hardpoint, 4),
            };
        }

        var optional = OptionalPattern().Match(slot);
        return optional.Success
            ? new SlotPlace(SlotKind.Optional, int.Parse(optional.Groups[1].Value, CultureInfo.InvariantCulture))
            : MilitaryPattern().IsMatch(slot) ? new SlotPlace(SlotKind.Military, 0) : new SlotPlace(SlotKind.Other, 0);
    }

    [GeneratedRegex("^(Tiny|Small|Medium|Large|Huge)Hardpoint\\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HardpointPattern();

    [GeneratedRegex("^Slot\\d+_Size(\\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OptionalPattern();

    [GeneratedRegex("^Military\\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MilitaryPattern();
}
