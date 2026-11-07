using System.Globalization;
using System.Text;

namespace Katey.Core.Planning;

/// <summary>Formats material lines as plain text, for the clipboard.</summary>
public static class ShoppingList
{
    /// <summary>Human labels for the inventories.</summary>
    public static IReadOnlyDictionary<MaterialCategory, string> CategoryLabels { get; } =
        new Dictionary<MaterialCategory, string>
        {
            [MaterialCategory.Raw] = "Raw",
            [MaterialCategory.Manufactured] = "Manufactured",
            [MaterialCategory.Encoded] = "Encoded",
        };

    /// <summary>
    /// One inventory per section, one material per line as <c>needed × name (Gn, family)</c>, followed
    /// by <c>have h, short s</c> when an inventory is known. Inventories with nothing in them are omitted.
    /// Lines end with a line feed on every platform.
    /// </summary>
    /// <param name="lines">Lines in display order.</param>
    /// <returns>The text, or empty when there are no lines.</returns>
    public static string Format(IEnumerable<MaterialLine> lines) => Format(lines, []);

    /// <summary>
    /// The material sections, then, when any unlock needs commodities, a "Buy at a market" section with one
    /// commodity per line as <c>needed × name</c>.
    /// </summary>
    /// <param name="lines">Material lines in display order.</param>
    /// <param name="commodities">Commodity totals in display order. May be empty.</param>
    /// <returns>The text, or empty when there is nothing.</returns>
    public static string Format(IEnumerable<MaterialLine> lines, IEnumerable<CommodityTotal> commodities)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(commodities);

        var builder = new StringBuilder(FormatMaterials(lines));
        var goods = commodities.ToList();
        if (goods.Count > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append("Buy at a market");
            foreach (var good in goods)
            {
                builder.Append('\n').Append(CultureInfo.InvariantCulture, $"{good.Needed} × {good.Name}");
            }
        }

        return builder.ToString();
    }

    private static string FormatMaterials(IEnumerable<MaterialLine> lines)
    {
        var builder = new StringBuilder();
        foreach (var group in lines.GroupBy(line => line.Material.Category).OrderBy(group => group.Key))
        {
            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append(CategoryLabels[group.Key]);
            foreach (var line in group)
            {
                builder.Append('\n');
                builder.Append(CultureInfo.InvariantCulture, $"{line.Needed} × {line.Material.Name} (G{line.Material.Grade}, {line.Material.Line})");
                if (line.Have is { } have)
                {
                    builder.Append(CultureInfo.InvariantCulture, $" — have {have}, short {line.Missing}");
                }
            }
        }

        return builder.ToString();
    }
}
