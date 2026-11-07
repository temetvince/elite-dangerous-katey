namespace Katey.Core.Trading;

/// <summary>How many of a material the inventory holds at most, by grade.</summary>
public static class MaterialCaps
{
    /// <summary>The storage cap for a grade.</summary>
    /// <param name="grade">1–5.</param>
    /// <returns>300 for grade 1, then 250, 200, 150 and 100.</returns>
    public static int For(int grade) => grade switch
    {
        <= 1 => 300,
        2 => 250,
        3 => 200,
        4 => 150,
        _ => 100,
    };
}

/// <summary>What the player wants kept of one material, whatever the plan needs.</summary>
/// <param name="Symbol">The material's journal symbol.</param>
/// <param name="Keep">How many to hold at least. Zero for no floor. Never negative.</param>
/// <param name="NeverTrade">Whether the material is never spent at a trader, however many are held.</param>
public sealed record MaterialReserve(string Symbol, int Keep, bool NeverTrade)
{
    /// <summary>Gets whether the reserve says anything at all.</summary>
    public bool IsEmpty => Keep <= 0 && !NeverTrade;
}

/// <summary>The exchange rate between two materials at a trader: give so many, get so many.</summary>
/// <param name="Give">Units handed over per batch. Positive.</param>
/// <param name="Get">Units received per batch. Positive.</param>
/// <remarks>
/// The game's rule: within one family, going up costs 6 per grade and going down returns 3 per grade,
/// both compounding; crossing to another family costs a flat factor of 6 on top of that; raw, manufactured
/// and encoded never trade for one another.
/// </remarks>
public sealed record TradeRate(int Give, int Get)
{
    /// <summary>Gets what one unit received costs in units given.</summary>
    public double CostPerUnit => (double)Give / Get;

    /// <summary>Gets "give:get", such as "6:1" or "1:3".</summary>
    public string Text => $"{Give}:{Get}";

    /// <summary>The rate between two materials.</summary>
    /// <param name="from">The material given.</param>
    /// <param name="to">The material received.</param>
    /// <returns>The rate, or <see langword="null"/> when the two are the same material or in different inventories.</returns>
    public static TradeRate? Between(Material from, Material to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (from.Category != to.Category || string.Equals(from.Symbol, to.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var sameLine = string.Equals(from.Line, to.Line, StringComparison.Ordinal);
        var steps = to.Grade - from.Grade;
        var give = steps > 0 ? Power(6, steps) : 1;
        var get = steps < 0 ? Power(3, -steps) : 1;
        if (!sameLine)
        {
            give *= 6;
        }

        var divisor = Gcd(give, get);
        return new TradeRate(give / divisor, get / divisor);
    }

    private static int Power(int value, int exponent)
    {
        var result = 1;
        for (var i = 0; i < exponent; i++)
        {
            result *= value;
        }

        return result;
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }
}

/// <summary>A material the player holds fewer of than they want.</summary>
/// <param name="Material">The material.</param>
/// <param name="Have">How many are held.</param>
/// <param name="Needed">How many the plan needs.</param>
/// <param name="Keep">How many the player wants kept at least.</param>
public sealed record TradeTarget(Material Material, int Have, int Needed, int Keep)
{
    /// <summary>Gets what the player wants to hold: the larger of the need and the reserve, no more than the cap.</summary>
    public int Wanted => Math.Min(Math.Max(Needed, Keep), MaterialCaps.For(Material.Grade));

    /// <summary>Gets how many are missing against <see cref="Wanted"/>. Zero when nothing is.</summary>
    public int Missing => Math.Max(0, Wanted - Have);

    /// <summary>Gets whether the plan is what wants more.</summary>
    public bool ForPlan => Needed > Have;

    /// <summary>Gets whether the reserve is what wants more.</summary>
    public bool ForReserve => Keep > Have;
}

/// <summary>One trade to make at a trader.</summary>
/// <param name="From">The material handed over.</param>
/// <param name="Give">How many to hand over. A whole number of batches.</param>
/// <param name="To">The material received.</param>
/// <param name="Get">How many come back.</param>
/// <param name="Rate">The rate used.</param>
/// <param name="Surplus">How many of <paramref name="Get"/> are beyond what was wanted, because batches are whole.</param>
public sealed record MaterialTrade(Material From, int Give, Material To, int Get, TradeRate Rate, int Surplus);

/// <summary>A shortfall no trade can cover, and why.</summary>
/// <param name="Target">What is short.</param>
/// <param name="Remaining">How many are still missing after every trade planned.</param>
/// <param name="Why">One line for the player.</param>
public sealed record TradeShortfall(TradeTarget Target, int Remaining, string Why);

/// <summary>What to trade at the material traders to cover every shortfall that can be covered.</summary>
/// <param name="Targets">Everything short, plan needs and reserves alike, in the order worked.</param>
/// <param name="Trades">The trades, in the order to make them.</param>
/// <param name="Unmet">What stays short after them.</param>
public sealed record TradePlan(IReadOnlyList<TradeTarget> Targets, IReadOnlyList<MaterialTrade> Trades, IReadOnlyList<TradeShortfall> Unmet)
{
    /// <summary>Gets a plan with nothing short.</summary>
    public static TradePlan Empty { get; } = new([], [], []);

    /// <summary>Gets whether every shortfall is covered by the trades.</summary>
    public bool CoversEverything => Unmet.Count == 0;
}

/// <summary>
/// Works out what to hand over at a material trader so that the inventory holds what the plan needs and
/// what the player keeps in reserve.
/// </summary>
/// <remarks>
/// <para>
/// Each material has a floor: the larger of what the plan needs and the reserve the player set. Anything
/// held above its floor is spare, and only spare is ever handed over; a material marked never-trade is not
/// spent at all. Materials held below their floor are the targets, worked highest grade first.
/// </para>
/// <para>
/// For each target the planner looks at every spare material in the same inventory and scores a trade from
/// it by worth lost: a unit is worth three of the grade below, so trading down a family loses nothing,
/// trading up loses half per grade and crossing families loses five sixths. The score is made smaller the
/// fuller the source is above its floor, since a full stack can take no more and anything found later is
/// lost, and a source that cannot cover the whole shortfall on its own carries a surcharge of one fifth
/// for the extra trade it leaves the player to make. The lowest score wins; ties go to the trade that
/// hands back the least beyond what is wanted. When the winner leaves a remainder, the planner goes again
/// for the rest. Batches are whole and never overfill the target's stack, so a trade down a grade can hand
/// back a few more than wanted; the surplus is reported.
/// </para>
/// </remarks>
public static class MaterialTrader
{
    private const double PartialSurcharge = 1.2;

    /// <summary>Plans the trades.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="needs">Material display name → how many the plan needs. Names not in the catalog are ignored.</param>
    /// <param name="inventory">Journal symbol → how many are held. Symbols not in the catalog are ignored.</param>
    /// <param name="reserves">What the player keeps. One per symbol; a later entry for the same symbol replaces an earlier one.</param>
    /// <returns>The plan. <see cref="TradePlan.Empty"/> when nothing is short.</returns>
    public static TradePlan Plan(
        Catalog catalog,
        IReadOnlyDictionary<string, int> needs,
        IReadOnlyDictionary<string, int> inventory,
        IEnumerable<MaterialReserve> reserves)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(needs);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(reserves);

        var reserveBySymbol = new Dictionary<string, MaterialReserve>(StringComparer.OrdinalIgnoreCase);
        foreach (var reserve in reserves)
        {
            reserveBySymbol[reserve.Symbol] = reserve;
        }

        var stocks = new List<Stock>();
        foreach (var material in catalog.Materials.Values)
        {
            var reserve = reserveBySymbol.GetValueOrDefault(material.Symbol);
            stocks.Add(new Stock(
                material,
                inventory.GetValueOrDefault(material.Symbol, 0),
                needs.GetValueOrDefault(material.Name, 0),
                reserve?.Keep ?? 0,
                reserve?.NeverTrade ?? false));
        }

        var targets = stocks
            .Where(stock => stock.Target.Missing > 0)
            .OrderByDescending(stock => stock.Material.Grade)
            .ThenByDescending(stock => stock.Target.Missing)
            .ThenBy(stock => stock.Material.Name, StringComparer.Ordinal)
            .ToList();
        if (targets.Count == 0)
        {
            return TradePlan.Empty;
        }

        var trades = new List<MaterialTrade>();
        var unmet = new List<TradeShortfall>();
        foreach (var target in targets)
        {
            var remaining = target.Target.Missing;
            while (remaining > 0)
            {
                var room = MaterialCaps.For(target.Material.Grade) - target.Have - target.Received;
                var best = stocks
                    .Where(stock => stock.Spare > 0 && !stock.NeverTrade)
                    .Select(stock => Candidate.For(stock, target.Material, remaining, room))
                    .Where(candidate => candidate is not null)
                    .Select(candidate => candidate!)
                    .OrderBy(candidate => candidate.Score)
                    .ThenBy(candidate => candidate.Get - Math.Min(candidate.Get, remaining))
                    .ThenBy(candidate => candidate.Give)
                    .ThenBy(candidate => candidate.Source.Material.Name, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (best is null)
                {
                    unmet.Add(new TradeShortfall(target.Target, remaining, Why(stocks, target.Material)));
                    break;
                }

                best.Source.Spent += best.Give;
                target.Received += best.Get;
                var surplus = Math.Max(0, best.Get - remaining);
                trades.Add(new MaterialTrade(best.Source.Material, best.Give, target.Material, best.Get, best.Rate, surplus));
                remaining -= best.Get;
            }
        }

        return new TradePlan([.. targets.Select(stock => stock.Target)], trades, unmet);
    }

    private static string Why(IEnumerable<Stock> stocks, Material target)
    {
        var sameInventory = stocks.Where(stock => stock.Material.Category == target.Category && !string.Equals(stock.Material.Symbol, target.Symbol, StringComparison.OrdinalIgnoreCase)).ToList();
        return sameInventory.Any(stock => stock.Spare > 0 && !stock.NeverTrade)
            ? "Every trade left would hand back more than the stack can hold: batches are whole."
            : sameInventory.Any(stock => stock.Spare > 0 && stock.NeverTrade)
            ? "The only spare materials in this inventory are ones you never trade."
            : sameInventory.Any(stock => stock.Have > 0)
            ? "Nothing in this inventory is held above what the plan needs and you keep."
            : "Nothing else in this inventory is held at all.";
    }

    /// <summary>A unit's worth on the game's own scale, where one unit is worth three of the grade below.</summary>
    private static int Worth(Material material) => material.Grade switch
    {
        <= 1 => 1,
        2 => 3,
        3 => 9,
        4 => 27,
        _ => 81,
    };

    /// <summary>One material's standing while the plan is worked: what is held, wanted, spent and received so far.</summary>
    private sealed class Stock(Material material, int have, int needed, int keep, bool neverTrade)
    {
        public Material Material { get; } = material;

        public int Have { get; } = have;

        public bool NeverTrade { get; } = neverTrade;

        public TradeTarget Target { get; } = new(material, have, needed, keep);

        public int Spent { get; set; }

        public int Received { get; set; }

        /// <summary>Gets how many may still be handed over: what is held above the floor, less what is already spent.</summary>
        public int Spare => Math.Max(0, Have - Target.Wanted - Spent);

        /// <summary>Gets how full the stack is above its floor, 0 at the floor and 1 at the cap.</summary>
        public double Fill
        {
            get
            {
                var room = MaterialCaps.For(Material.Grade) - Target.Wanted;
                return room <= 0 ? 1 : Math.Clamp((double)(Have - Target.Wanted) / room, 0, 1);
            }
        }
    }

    private sealed record Candidate(Stock Source, TradeRate Rate, int Give, int Get, double Score)
    {
        /// <summary>Scores a trade from one source, or none when no whole batch is affordable and fits.</summary>
        /// <param name="source">The material to hand over.</param>
        /// <param name="target">The material wanted.</param>
        /// <param name="wanted">How many are still wanted.</param>
        /// <param name="room">How many more the target's stack can hold.</param>
        public static Candidate? For(Stock source, Material target, int wanted, int room)
        {
            if (TradeRate.Between(source.Material, target) is not { } rate)
            {
                return null;
            }

            var batchesWanted = (wanted + rate.Get - 1) / rate.Get;
            var batchesAffordable = source.Spare / rate.Give;
            var batchesThatFit = room / rate.Get;
            var batches = Math.Min(batchesWanted, Math.Min(batchesAffordable, batchesThatFit));
            if (batches <= 0)
            {
                return null;
            }

            var give = batches * rate.Give;
            var get = batches * rate.Get;
            var score = (double)(give * Worth(source.Material)) / (get * Worth(target)) * (1 - (0.5 * source.Fill));
            if (get < wanted)
            {
                score *= PartialSurcharge;
            }

            return new Candidate(source, rate, give, get, score);
        }
    }
}
