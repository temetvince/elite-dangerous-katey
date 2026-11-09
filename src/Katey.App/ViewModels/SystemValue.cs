using Katey.Journal;

namespace Katey.App.ViewModels;

/// <summary>
/// What the current system is worth, the same figures on the system page and the rail: cartographics as
/// scanned and with the first-discovery bonus where a body was yours to discover; exobiology by the scanner's
/// genera, exact once every genus has a sampled species, and with the first-logged bonus where nobody had set
/// foot; what of the system's data is still aboard, and what has already sold. A bonus figure equal to its base
/// means the bonus applies nowhere.
/// </summary>
/// <param name="ScanBase">Cartographics as scanned, with no bonus, credits.</param>
/// <param name="Scan">Cartographics with the first-discovery bonus where it applies, credits.</param>
/// <param name="Exobiology">Exobiology by the scanner's genera, with no bonus, credits; zero without life.</param>
/// <param name="ExobiologyWithBonus">Exobiology with the first-logged bonus where it applies, credits.</param>
/// <param name="ExobiologyKnown">Whether every body with life has every genus sampled, so <paramref name="Exobiology"/> is exact.</param>
/// <param name="Unsold">What the system's data still aboard would pay, credits: scans not yet sold and species sampled in full and not yet sold, bonuses counted where they apply; see <see cref="BodyRow.UnsoldCredits"/>.</param>
/// <param name="UnsoldFirstBonus">What of <paramref name="Unsold"/> is for being first, credits.</param>
/// <param name="Sold">What the system's data has sold for across the history, credits; see <see cref="SystemSales"/>.</param>
public sealed record SystemValue(long ScanBase, long Scan, long Exobiology, long ExobiologyWithBonus, bool ExobiologyKnown, long Unsold, long UnsoldFirstBonus, long Sold)
{
    /// <summary>Gets whether the first-discovery bonus applies to any body.</summary>
    public bool HasDiscoveryBonus => Scan > ScanBase;

    /// <summary>Gets whether there is any exobiology to value.</summary>
    public bool HasExobiology => Exobiology > 0;

    /// <summary>Gets whether the first-logged bonus applies to any body.</summary>
    public bool HasFirstLogBonus => ExobiologyWithBonus > Exobiology;

    /// <summary>Sums the system from its body rows and the sales ledger.</summary>
    /// <param name="snapshot">The system.</param>
    /// <param name="rows">Its body rows, as built from the snapshot.</param>
    /// <param name="sold">The sales ledger; the system's own sales are picked out by its name.</param>
    /// <returns>The figures.</returns>
    public static SystemValue From(SystemSnapshot snapshot, IReadOnlyList<BodyRow> rows, SoldData sold)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(sold);
        var exobiology = rows.Sum(row => row.ExobiologyCredits);
        return new(
            snapshot.Bodies.Sum(BodyRowBuilder.ScanBaseCredits),
            rows.Sum(row => row.ScanCredits),
            exobiology,
            rows.Sum(row => row.ExobiologyCredits * (row.FirstLogLikely ? BodyRow.FirstLogMultiplier : 1)),
            exobiology > 0 && rows.Where(row => row.HasBiology).All(row => row.IsExobiologyKnown),
            rows.Sum(row => row.UnsoldCredits),
            rows.Sum(row => row.UnsoldFirstBonus),
            snapshot.SystemName is { } name ? SystemSales.Credits(sold, name) : 0);
    }

    /// <summary>The unsold figure in one sentence for the system page: what is aboard, what of it is for being first, and what has been sold.</summary>
    /// <returns>Such as "Exploration data unsold: 95.4M Cr, 76.0M Cr of it for being first; sold: 1.2M Cr." The first part is left out when no bonus applies, the sold part when nothing has sold.</returns>
    public string UnsoldLine()
    {
        var line = $"Exploration data unsold: {Display.Credits(Unsold)}";
        if (UnsoldFirstBonus > 0)
        {
            line += $", {Display.Credits(UnsoldFirstBonus)} of it for being first";
        }

        if (Sold > 0)
        {
            line += $"; sold: {Display.Credits(Sold)}";
        }

        return line + ".";
    }

    /// <summary>The unsold figure for the rail.</summary>
    /// <returns>Such as "Exploration data unsold: 95.4M Cr".</returns>
    public string UnsoldText() => $"Exploration data unsold: {Display.Credits(Unsold)}";

    /// <summary>The figures as a full sentence for the system page.</summary>
    /// <returns>
    /// Such as "Cartographics about 104k Cr as scanned, 312k Cr with the first-discovery bonus; exobiology about
    /// 67.4M Cr, 337.0M Cr with the first-logged bonus." The bonus figure is a sum over bodies, never written as a
    /// multiple: the bonus may apply to some bodies only, and rounded figures would not multiply out.
    /// </returns>
    public string Line()
    {
        var line = $"Cartographics about {Display.Credits(ScanBase)} as scanned";
        if (HasDiscoveryBonus)
        {
            line += $", {Display.Credits(Scan)} with the first-discovery bonus";
        }

        if (HasExobiology)
        {
            line += $"; exobiology {(ExobiologyKnown ? "" : "about ")}{Display.Credits(Exobiology)}";
            if (HasFirstLogBonus)
            {
                line += $", {Display.Credits(ExobiologyWithBonus)} with the first-logged bonus";
            }
        }

        return line + ".";
    }
}
