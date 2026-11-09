using System.Globalization;
using Katey.Core;
using Katey.Core.Fleet;
using Katey.Core.Planning;

namespace Katey.App.ViewModels;

/// <summary>A pick-list option: a stable key and the text shown for it.</summary>
/// <param name="Key">What the option stands for. Empty for a "none" option.</param>
/// <param name="Label">Shown to the user.</param>
public sealed record Choice(string Key, string Label)
{
    /// <summary>Gets the option that stands for nothing chosen.</summary>
    public static Choice None { get; } = new("", "None");

    /// <inheritdoc/>
    public override string ToString() => Label;
}

/// <summary>A material quantity ready to print.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Count">How many.</param>
/// <param name="Grade">1–5.</param>
/// <param name="Category">Which inventory.</param>
public sealed record MaterialChip(string Name, int Count, int Grade, MaterialCategory Category)
{
    /// <summary>Gets <c>count× name</c>.</summary>
    public string Text => string.Create(CultureInfo.InvariantCulture, $"{Count}× {Name}");

    /// <summary>Gets <c>Gn · Raw</c>, the grade and a short inventory name.</summary>
    public string Meta => string.Create(CultureInfo.InvariantCulture, $"G{Grade} · {Display.ShortCategory(Category)}");

    /// <summary>Builds chips from a count map, in its order.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="counts">Material display name → count. Every name must exist.</param>
    /// <returns>One chip per entry.</returns>
    public static IReadOnlyList<MaterialChip> From(Catalog catalog, IReadOnlyDictionary<string, int> counts)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(counts);

        return
        [
            .. counts.Select(pair =>
            {
                var material = catalog.Materials[pair.Key];
                return new MaterialChip(material.Name, pair.Value, material.Grade, material.Category);
            }),
        ];
    }
}

/// <summary>One row of the materials table: a total with the inventory laid against it.</summary>
/// <param name="Line">The line.</param>
public sealed record MaterialRow(MaterialLine Line)
{
    /// <summary>Gets the display name.</summary>
    public string Name => Line.Material.Name;

    /// <summary>Gets <c>Gn</c>.</summary>
    public string Grade => string.Create(CultureInfo.InvariantCulture, $"G{Line.Material.Grade}");

    /// <summary>Gets the in-game family.</summary>
    public string Family => Line.Material.Line;

    /// <summary>Gets how many the plan needs.</summary>
    public int Needed => Line.Needed;

    /// <summary>Gets how many the player holds, or a dash when no inventory is known.</summary>
    public string Have => Line.Have is { } have ? have.ToString(CultureInfo.InvariantCulture) : "—";

    /// <summary>Gets how many are still to collect, or a dash when no inventory is known.</summary>
    public string Missing => Line.Have is null ? "—" : Line.Missing.ToString(CultureInfo.InvariantCulture);

    /// <summary>Gets whether the player already holds enough.</summary>
    public bool IsCovered => Line.IsCovered;

    /// <summary>Gets whether an inventory is known and falls short.</summary>
    public bool IsShort => Line.Have is not null && !Line.IsCovered;

    /// <summary>Gets the grade, 1 to 5, for colouring.</summary>
    public int GradeNumber => Line.Material.Grade;

    /// <summary>Gets which inventory the material belongs to, for colouring.</summary>
    public MaterialCategory Category => Line.Material.Category;

    /// <summary>Gets how much of the need is held, 0 to 1, for a meter. 1 when nothing is needed.</summary>
    public double Fill => Line.Needed <= 0 ? 1 : Math.Clamp((Line.Have ?? 0) / (double)Line.Needed, 0, 1);

    /// <summary>Gets whether a meter means anything: an inventory is known and the plan needs some.</summary>
    public bool HasMeter => Line.Have is not null && Line.Needed > 0;
}

/// <summary>Shared labels.</summary>
public static class Display
{
    /// <summary>Human label for a module group.</summary>
    /// <param name="group">The group.</param>
    /// <returns>Plural, title case.</returns>
    public static string GroupLabel(ModuleGroup group) => group switch
    {
        ModuleGroup.Core => "Core internals",
        ModuleGroup.Optional => "Optional internals",
        ModuleGroup.Hardpoint => "Hardpoints",
        ModuleGroup.Utility => "Utility mounts",
        _ => group.ToString(),
    };

    /// <summary>The module group a slot belongs to, for colouring by place on the ship.</summary>
    /// <param name="slot">The slot name as the journal spells it.</param>
    /// <returns>The group, with military slots counted as optional internals, or <see langword="null"/> for a cosmetic or unknown slot.</returns>
    public static ModuleGroup? GroupOf(string slot) => SlotNames.Parse(slot ?? "").Kind switch
    {
        SlotKind.Core => ModuleGroup.Core,
        SlotKind.Hardpoint => ModuleGroup.Hardpoint,
        SlotKind.Utility => ModuleGroup.Utility,
        SlotKind.Optional => ModuleGroup.Optional,
        SlotKind.Military => ModuleGroup.Optional,
        SlotKind.Other => null,
        _ => null,
    };

    /// <summary>Three-letter inventory name.</summary>
    /// <param name="category">The inventory.</param>
    /// <returns><c>Raw</c>, <c>Mfd</c> or <c>Enc</c>.</returns>
    public static string ShortCategory(MaterialCategory category) => category switch
    {
        MaterialCategory.Raw => "Raw",
        MaterialCategory.Manufactured => "Mfd",
        MaterialCategory.Encoded => "Enc",
        _ => category.ToString(),
    };

    /// <summary>Human label for a region.</summary>
    /// <param name="region">The region.</param>
    /// <returns>"The Bubble" or "Colonia".</returns>
    public static string RegionLabel(Region region) => region switch
    {
        Region.Bubble => "The Bubble",
        Region.Colonia => "Colonia",
        _ => region.ToString(),
    };

    /// <summary>Credits, compactly: "1.2M Cr", "850k Cr" or "120 Cr".</summary>
    /// <param name="credits">Credits.</param>
    /// <returns>Text.</returns>
    public static string Credits(long credits) => credits >= 1_000_000
        ? string.Create(CultureInfo.InvariantCulture, $"{credits / 1_000_000.0:0.0}M Cr")
        : credits >= 1_000
            ? string.Create(CultureInfo.InvariantCulture, $"{credits / 1_000.0:0}k Cr")
            : string.Create(CultureInfo.InvariantCulture, $"{credits} Cr");

    /// <summary>
    /// Credits in millions with one decimal whatever the amount, "0.8M Cr" or "12.3M Cr", for a column of figures
    /// whose decimals must line up in a monospaced face.
    /// </summary>
    /// <param name="credits">Credits.</param>
    /// <returns>Text.</returns>
    public static string CreditsMillions(long credits) => $"{Millions(credits)} Cr";

    /// <summary>Credits in millions with one decimal and no unit, "0.8M" or "12.3M", for a figure that shares its unit with another.</summary>
    /// <param name="credits">Credits.</param>
    /// <returns>Text.</returns>
    public static string Millions(long credits) => string.Create(CultureInfo.InvariantCulture, $"{credits / 1_000_000.0:0.0}M");

    /// <summary>Credits in full, with a thousands separator.</summary>
    /// <param name="credits">Credits.</param>
    /// <returns>Text such as "1,234,567 Cr".</returns>
    public static string CreditsExact(long credits) => string.Create(CultureInfo.InvariantCulture, $"{credits:#,0} Cr");

    /// <summary>A span of time in days, hours and minutes.</summary>
    /// <param name="span">The span. Negative reads as zero.</param>
    /// <returns>Text such as "3d 4h", "4h 12m" or "12m".</returns>
    public static string Duration(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        return span.TotalDays >= 1
            ? string.Create(CultureInfo.InvariantCulture, $"{(int)span.TotalDays:#,0}d {span.Hours}h")
            : span.TotalHours >= 1
                ? string.Create(CultureInfo.InvariantCulture, $"{span.Hours}h {span.Minutes}m")
                : string.Create(CultureInfo.InvariantCulture, $"{span.Minutes}m");
    }

    /// <summary>A whole number with a thousands separator.</summary>
    /// <param name="value">The number.</param>
    /// <returns>Text such as "12,345".</returns>
    public static string Whole(double value) => string.Create(CultureInfo.InvariantCulture, $"{value:#,0}");

    /// <summary>A distance in light years: whole numbers above 100, one decimal below, with a thousands separator.</summary>
    /// <param name="lightYears">Light years.</param>
    /// <returns>Text such as "41.2 ly" or "22,003 ly".</returns>
    public static string LightYears(double lightYears)
        => lightYears >= 100
            ? string.Create(CultureInfo.InvariantCulture, $"{lightYears:#,0} ly")
            : string.Create(CultureInfo.InvariantCulture, $"{lightYears:0.0} ly");

    /// <summary>A distance from the ship, or a note that the ship's position is unknown.</summary>
    /// <param name="lightYears">Light years, or <see langword="null"/>.</param>
    /// <returns>Text.</returns>
    public static string DistanceOrUnknown(double? lightYears) => lightYears is { } ly ? LightYears(ly) : "distance unknown";

    /// <summary>"Gn".</summary>
    /// <param name="grade">1–5.</param>
    /// <returns>Text.</returns>
    public static string Grade(int grade) => string.Create(CultureInfo.InvariantCulture, $"G{grade}");

    /// <summary>A local clock time, "HH:mm".</summary>
    /// <param name="at">The instant.</param>
    /// <returns>Text in the local time zone.</returns>
    public static string Clock(DateTimeOffset at) => at.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>A count with its noun, singular or plural: "1 jump", "3 jumps".</summary>
    /// <param name="count">The count.</param>
    /// <param name="singular">The noun.</param>
    /// <param name="plural">Its plural, or <see langword="null"/> to add an "s".</param>
    /// <returns>Text.</returns>
    public static string Plural(int count, string singular, string? plural = null)
        => string.Create(CultureInfo.InvariantCulture, $"{count} {(count == 1 ? singular : plural ?? (singular + "s"))}");

    /// <summary>Joins names with commas, or a fallback when there are none.</summary>
    /// <param name="names">Names in order.</param>
    /// <param name="fallback">Shown when <paramref name="names"/> is empty.</param>
    /// <returns>The text.</returns>
    public static string Names(IEnumerable<string> names, string fallback)
    {
        ArgumentNullException.ThrowIfNull(names);
        var list = names.ToList();
        return list.Count == 0 ? fallback : string.Join(", ", list);
    }
}
