using System.Text;
using Katey.App.Text;
using Katey.Core.Planning;

namespace Katey.App.ViewModels;

/// <summary>Formats material lines as plain text for the clipboard, in the language in force.</summary>
public static class ShoppingListText
{
    /// <summary>
    /// One inventory per section, one material per line as <c>needed × name (Gn, family)</c>, followed by
    /// <c>have h, short s</c> when an inventory is known. Inventories with nothing in them are omitted. Lines end
    /// with a line feed on every platform.
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

            builder.Append(Strings.Get(Keys.Shopping_BuyAtAMarket));
            foreach (var good in goods)
            {
                builder.Append('\n').Append(Strings.Format(Keys.Shopping_Commodity, ("needed", good.Needed), ("name", good.Name)));
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

            builder.Append(Display.CategoryLabel(group.Key));
            foreach (var line in group)
            {
                builder.Append('\n');
                var key = line.Have is null ? Keys.Shopping_Line : Keys.Shopping_LineWithInventory;
                builder.Append(Strings.Format(
                    key,
                    ("needed", line.Needed),
                    ("material", Names.Material(line.Material)),
                    ("grade", Display.Grade(line.Material.Grade)),
                    ("family", Names.Family(line.Material.Line)),
                    ("have", line.Have),
                    ("short", line.Missing)));
            }
        }

        return builder.ToString();
    }
}
