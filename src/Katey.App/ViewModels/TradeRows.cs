using System.Globalization;
using Katey.App.Navigation;
using Katey.Core;
using Katey.Core.Planning;
using Katey.Core.Trading;

namespace Katey.App.ViewModels;

/// <summary>One trade to make, ready to print.</summary>
/// <param name="Trade">The trade.</param>
/// <param name="From">The materials page, landing on the material handed over.</param>
/// <param name="To">The materials page, landing on the material received.</param>
public sealed record TradeRow(MaterialTrade Trade, Link From, Link To)
{
    /// <summary>Gets "Give 12 Iron".</summary>
    public string GiveText => string.Create(CultureInfo.InvariantCulture, $"Give {Trade.Give} {Trade.From.Name}");

    /// <summary>Gets "Give 12", for printing before the <see cref="From"/> link.</summary>
    public string GiveCount => string.Create(CultureInfo.InvariantCulture, $"Give {Trade.Give}");

    /// <summary>Gets "get 36 Carbon".</summary>
    public string GetText => string.Create(CultureInfo.InvariantCulture, $"get {Trade.Get} {Trade.To.Name}");

    /// <summary>Gets "get 36", for printing before the <see cref="To"/> link.</summary>
    public string GetCount => string.Create(CultureInfo.InvariantCulture, $"get {Trade.Get}");

    /// <summary>Gets the rate and the grades, such as "G2 → G1 at 1:3".</summary>
    public string RateText => string.Create(CultureInfo.InvariantCulture, $"{Display.Grade(Trade.From.Grade)} → {Display.Grade(Trade.To.Grade)} at {Trade.Rate.Text}");

    /// <summary>Gets the two materials' in-game families, which the traders group by: "Iron family → Carbon family", or "both in the Wake Scans family" when they share one.</summary>
    public string FamilyText => string.Equals(Trade.From.Line, Trade.To.Line, StringComparison.Ordinal)
        ? $"both in the {Trade.From.Line} family"
        : $"{Trade.From.Line} family → {Trade.To.Line} family";

    /// <summary>Gets a note on the surplus, or empty when the trade hands back exactly what is wanted.</summary>
    public string SurplusText => Trade.Surplus == 0 ? "" : string.Create(CultureInfo.InvariantCulture, $"{Trade.Surplus} more than needed");
}

/// <summary>A shortfall no trade covers, ready to print.</summary>
/// <param name="Shortfall">The shortfall.</param>
/// <param name="Material">The materials page, landing on the material that stays short.</param>
public sealed record UnmetRow(TradeShortfall Shortfall, Link Material)
{
    /// <summary>Gets "Still 4 Yttrium short".</summary>
    public string Text => string.Create(CultureInfo.InvariantCulture, $"Still {Shortfall.Remaining} {Shortfall.Target.Material.Name} short");

    /// <summary>Gets "Still 4", for printing before the <see cref="Material"/> link.</summary>
    public string Prefix => string.Create(CultureInfo.InvariantCulture, $"Still {Shortfall.Remaining}");

    /// <summary>Gets why.</summary>
    public string Why => Shortfall.Why;
}

/// <summary>The trades to make at one kind of trader.</summary>
/// <param name="Category">The inventory the trader deals in.</param>
/// <param name="Trades">The trades, in order.</param>
/// <param name="Unmet">What stays short in this inventory.</param>
public sealed record TraderGroupRow(MaterialCategory Category, IReadOnlyList<TradeRow> Trades, IReadOnlyList<UnmetRow> Unmet)
{
    /// <summary>Gets "At a raw materials trader".</summary>
    public string Heading => Category switch
    {
        MaterialCategory.Raw => "At a raw materials trader",
        MaterialCategory.Manufactured => "At a manufactured materials trader",
        MaterialCategory.Encoded => "At an encoded data trader",
        _ => "At a material trader",
    };

    /// <summary>Gets whether there is anything to do here.</summary>
    public bool HasTrades => Trades.Count > 0;
}

/// <summary>The trade plan laid out for a page: a headline and one group per trader.</summary>
/// <param name="Headline">One line on the whole: nothing short, all covered, or what stays short.</param>
/// <param name="Groups">One per inventory that has a trade or a shortfall, in inventory order.</param>
/// <param name="ReserveNote">One line on which targets are reserves rather than plan needs, or empty.</param>
public sealed record TradeView(string Headline, IReadOnlyList<TraderGroupRow> Groups, string ReserveNote)
{
    /// <summary>Gets whether there is at least one trade to make.</summary>
    public bool HasTrades => Groups.Any(group => group.HasTrades);

    /// <summary>Gets whether there is anything at all to show beyond the headline.</summary>
    public bool HasGroups => Groups.Count > 0;

    /// <summary>Lays out a plan.</summary>
    /// <param name="plan">The plan, or <see langword="null"/> when no inventory is known yet.</param>
    /// <param name="linkTo">Makes the link for a material: the materials page, landing on it. Required when <paramref name="plan"/> is given.</param>
    /// <returns>The view.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> is given without <paramref name="linkTo"/>.</exception>
    public static TradeView From(TradePlan? plan, Func<Material, Link>? linkTo = null)
    {
        if (plan is null)
        {
            return new TradeView("Trades appear once the journal watch has read your inventory.", [], "");
        }

        ArgumentNullException.ThrowIfNull(linkTo);

        if (plan.Targets.Count == 0)
        {
            return new TradeView("Nothing to trade for: you hold what the plan needs and what you keep.", [], "");
        }

        var groups = MaterialCalculator.CategoryOrder
            .Select(category => new TraderGroupRow(
                category,
                [.. plan.Trades.Where(trade => trade.To.Category == category).Select(trade => new TradeRow(trade, linkTo(trade.From), linkTo(trade.To)))],
                [.. plan.Unmet.Where(shortfall => shortfall.Target.Material.Category == category).Select(shortfall => new UnmetRow(shortfall, linkTo(shortfall.Target.Material)))]))
            .Where(group => group.Trades.Count > 0 || group.Unmet.Count > 0)
            .ToList();
        var covered = plan.Targets.Count - plan.Unmet.Count;
        var headline = plan.Trades.Count == 0
            ? $"No trade covers any of the {Display.Plural(plan.Targets.Count, "material")} you are short of."
            : plan.CoversEverything
                ? $"{Display.Plural(plan.Trades.Count, "trade")} cover{(plan.Trades.Count == 1 ? "s" : "")} everything you are short of."
                : $"{Display.Plural(plan.Trades.Count, "trade")} cover{(plan.Trades.Count == 1 ? "s" : "")} {covered} of the {Display.Plural(plan.Targets.Count, "material")} you are short of.";
        var reserves = plan.Targets.Where(target => target.ForReserve && !target.ForPlan).Select(target => target.Material.Name).ToList();
        var note = reserves.Count == 0 ? "" : $"Back to your reserve: {string.Join(", ", reserves)}.";
        return new TradeView(headline, groups, note);
    }
}
