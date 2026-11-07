using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Katey.Core.Data;

namespace Katey.Core.Exploration;

/// <summary>Facts about frame shift drives and jump boosts that the fuel model needs.</summary>
public static partial class FrameShiftDrives
{
    /// <summary>The range multiplier a neutron star's jet cone gives the next jump.</summary>
    public const double NeutronBoost = 4.0;

    /// <summary>The range multiplier a white dwarf's jet cone gives the next jump.</summary>
    public const double WhiteDwarfBoost = 1.5;

    private static readonly double[] GuardianBoosterBySize = [0, 4.0, 6.0, 7.75, 9.25, 10.5];
    private static readonly JsonSerializerOptions TableOptions = new(JsonSerializerDefaults.Web);
    private static readonly Lazy<FrozenDictionary<string, DriveRow>> Table = new(LoadEmbedded);

    /// <summary>Gets every drive the table knows, SCO and Mk II drives included.</summary>
    public static IReadOnlyCollection<DriveRow> Known => Table.Value.Values;

    /// <summary>Reads a drive's size and class from its item symbol.</summary>
    /// <param name="item">An item symbol such as <c>int_hyperdrive_size5_class5</c>, <c>int_hyperdrive_overcharge_size6_class5</c> or the size 8 Mk II drive with its <c>_overchargebooster_mkii</c> suffix, any letter case.</param>
    /// <returns>Size 2–8 and class 1–5, where class 5 is rating A, or <see langword="null"/> when the symbol is not a drive.</returns>
    public static (int Size, int Class)? Parse(string item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var match = DrivePattern().Match(item);
        if (!match.Success)
        {
            return null;
        }

        var size = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var cls = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return size is >= 2 and <= 8 && cls is >= 1 and <= 5 ? (size, cls) : null;
    }

    /// <summary>The table's row for a drive.</summary>
    /// <param name="item">The item symbol, any letter case.</param>
    /// <returns>The row, or <see langword="null"/> when the item is not a drive the table knows.</returns>
    public static DriveRow? Figures(string item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Table.Value.GetValueOrDefault(item);
    }

    /// <summary>The range multiplier a neutron star gives a drive.</summary>
    /// <param name="item">The drive's item symbol, any letter case. Must be a drive the table knows.</param>
    /// <returns>6 for the size 8 Mk II drive; 4 for every other.</returns>
    /// <exception cref="ArgumentException"><paramref name="item"/> is not a drive the table knows.</exception>
    public static int SuperchargeMultiplier(string item) => Require(item).SuperchargeMultiplier;

    /// <summary>The fuel exponent of a drive.</summary>
    /// <param name="item">The drive's item symbol, any letter case. Must be a drive the table knows.</param>
    /// <returns>The exponent, 2.0 for a size 2 rising to 2.9 for a size 8, with the Mk II's own 2.5025.</returns>
    /// <exception cref="ArgumentException"><paramref name="item"/> is not a drive the table knows.</exception>
    public static double FuelPowerOf(string item) => Require(item).FuelPower;

    /// <summary>The fuel multiplier of a drive.</summary>
    /// <param name="item">The drive's item symbol, any letter case. Must be a drive the table knows.</param>
    /// <returns>The multiplier.</returns>
    /// <exception cref="ArgumentException"><paramref name="item"/> is not a drive the table knows.</exception>
    public static double FuelMultiplierOf(string item) => Require(item).FuelMultiplier;

    /// <summary>The optimal mass of an unmodified drive.</summary>
    /// <param name="item">The drive's item symbol, any letter case. Must be a drive the table knows.</param>
    /// <returns>Tonnes.</returns>
    /// <exception cref="ArgumentException"><paramref name="item"/> is not a drive the table knows.</exception>
    public static double OptimalMassOf(string item) => Require(item).OptimalMass;

    /// <summary>The most fuel one jump may burn on an unmodified drive.</summary>
    /// <param name="item">The drive's item symbol, any letter case. Must be a drive the table knows.</param>
    /// <returns>Tonnes.</returns>
    /// <exception cref="ArgumentException"><paramref name="item"/> is not a drive the table knows.</exception>
    public static double MaxFuelPerJumpOf(string item) => Require(item).MaxFuelPerJump;

    private static DriveRow Require(string item)
        => Figures(item) ?? throw new ArgumentException($"{item} is not a frame shift drive the table knows.", nameof(item));

    /// <summary>Loads <c>drives.json</c> from this assembly.</summary>
    /// <exception cref="CatalogException">The embedded table is missing or malformed. Never expected from a shipped build.</exception>
    private static FrozenDictionary<string, DriveRow> LoadEmbedded()
    {
        using var stream = typeof(FrameShiftDrives).Assembly.GetManifestResourceStream("Katey.Core.Data.drives.json")
            ?? throw new CatalogException("drives.json: embedded resource missing");
        var rows = JsonSerializer.Deserialize<Dictionary<string, DriveRow>>(stream, TableOptions)
            ?? throw new CatalogException("drives.json: empty");
        foreach (var (key, row) in rows)
        {
            if (row.OptimalMass <= 0 || row.MaxFuelPerJump <= 0 || row.FuelMultiplier <= 0 || row.FuelPower <= 0 || row.SuperchargeMultiplier <= 0)
            {
                throw new CatalogException($"drives.json \"{key}\": every figure must be positive");
            }
        }

        return rows.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The range a Guardian frame shift drive booster adds.</summary>
    /// <param name="item">An item symbol such as <c>int_guardianfsdbooster_size5</c>, any letter case.</param>
    /// <returns>Light years, or 0 when the symbol is not a booster.</returns>
    public static double GuardianBoosterLy(string item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var match = BoosterPattern().Match(item);
        return match.Success && int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) is var size && size is >= 1 and <= 5
            ? GuardianBoosterBySize[size]
            : 0;
    }

    /// <summary>The range multiplier a jump gets from the star it departs.</summary>
    /// <param name="departing">The departure star's kind.</param>
    /// <returns>4 from a neutron star, 1.5 from a white dwarf, else 1.</returns>
    public static double BoostFrom(StarKind departing) => departing switch
    {
        StarKind.Neutron => NeutronBoost,
        StarKind.WhiteDwarf => WhiteDwarfBoost,
        StarKind.Scoopable => 1,
        StarKind.BlackHole => 1,
        StarKind.Other => 1,
        _ => 1,
    };

    [GeneratedRegex("^int_hyperdrive(?:_overcharge)?_size(\\d)_class(\\d)(?:_[a-z0-9_]+)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DrivePattern();

    [GeneratedRegex("^int_guardianfsdbooster_size(\\d)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BoosterPattern();
}

/// <summary>One jump the ship made: how far, what it burned, and how much the departure star boosted it.</summary>
/// <param name="DistanceLy">Jump distance in light years. Positive.</param>
/// <param name="FuelUsed">Tonnes burned. Positive.</param>
/// <param name="Boost">The range multiplier in effect, 1 for none.</param>
public sealed record JumpObservation(double DistanceLy, double FuelUsed, double Boost);

/// <summary>
/// How much fuel this ship burns per jump: <c>fuel = k × effective^power</c>, where the effective distance
/// is the jump's distance divided by the star's boost and scaled by <c>range ÷ (range + booster)</c>, the
/// drive's own range against its range with the Guardian booster. The booster thus stretches every jump by
/// the same factor rather than taking a flat slice off it, which is how the game charges for it. <c>k</c> is
/// the fuel equation's scale: the multiplier times the mass ratio raised to the power, or a value fitted to
/// the jumps the ship has actually made, so the ship's mass and the drive's engineering are in it without
/// being known.
/// </summary>
/// <param name="K">The scale.</param>
/// <param name="Power">The drive's exponent, from <see cref="FrameShiftDrives.FuelPowerOf"/>.</param>
/// <param name="MaxFuelPerJump">The most one jump may burn, tonnes.</param>
/// <param name="BoosterLy">Range a Guardian booster adds, light years, or 0.</param>
public sealed record JumpFuelModel(double K, double Power, double MaxFuelPerJump, double BoosterLy)
{
    /// <summary>Fits a model to observed jumps.</summary>
    /// <param name="observations">Jumps this ship made. Jumps with non-positive distance or fuel are ignored.</param>
    /// <param name="power">The drive's exponent.</param>
    /// <param name="maxFuelPerJump">The most one jump may burn, tonnes.</param>
    /// <param name="boosterLy">Range a Guardian booster adds, or 0.</param>
    /// <returns>The model, with <see cref="K"/> the median of the per-jump fits, or <see langword="null"/> when no jump is usable.</returns>
    public static JumpFuelModel? Calibrate(IEnumerable<JumpObservation> observations, double power, double maxFuelPerJump, double boosterLy)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var usable = observations.Where(jump => jump.DistanceLy > 0 && jump.FuelUsed > 0 && jump.Boost > 0).ToList();
        if (usable.Count == 0)
        {
            return null;
        }

        // The booster's share of each jump depends on the drive's range, which depends on k: a few rounds settle it.
        var model = new JumpFuelModel(usable[0].FuelUsed / Math.Pow(usable[0].DistanceLy / usable[0].Boost, power), power, maxFuelPerJump, boosterLy);
        for (var round = 0; round < 8; round++)
        {
            var fits = usable
                .Select(jump => jump.FuelUsed / Math.Pow(model.Effective(jump.DistanceLy, jump.Boost), power))
                .Where(k => double.IsFinite(k) && k > 0)
                .Order()
                .ToList();
            if (fits.Count == 0)
            {
                return null;
            }

            var median = fits.Count % 2 == 1 ? fits[fits.Count / 2] : (fits[(fits.Count / 2) - 1] + fits[fits.Count / 2]) / 2;
            model = model with { K = median };
        }

        return model;
    }

    /// <summary>Gets the drive's own range at this scale, light years: the distance one jump at the cap covers with no boost and no booster.</summary>
    public double DriveRangeLy => Math.Pow(MaxFuelPerJump / K, 1 / Power);

    /// <summary>Fuel one jump burns.</summary>
    /// <param name="distanceLy">The jump distance.</param>
    /// <param name="boost">The range multiplier from the departure star, 1 for none.</param>
    /// <returns>Tonnes, never above <see cref="MaxFuelPerJump"/>.</returns>
    public double FuelFor(double distanceLy, double boost)
        => Math.Min(MaxFuelPerJump, K * Math.Pow(Effective(distanceLy, boost), Power));

    /// <summary>The distance the drive is charged for: the jump less the star's boost, scaled down by the booster's share of the range.</summary>
    private double Effective(double distanceLy, double boost)
    {
        var unboosted = Math.Max(0, distanceLy / Math.Max(boost, double.Epsilon));
        if (BoosterLy <= 0)
        {
            return unboosted;
        }

        var range = DriveRangeLy;
        return range > 0 && double.IsFinite(range) ? unboosted * range / (range + BoosterLy) : unboosted;
    }
}

/// <summary>One leg of a route for the fuel projection.</summary>
/// <param name="DistanceLy">Distance from the previous stop. 0 for the first.</param>
/// <param name="Boost">Range multiplier from the previous stop's star, 1 for none.</param>
/// <param name="Scoopable">Whether the ship can refuel at this stop.</param>
public sealed record FuelLeg(double DistanceLy, double Boost, bool Scoopable);

/// <summary>Fuel at one stop of the projection.</summary>
/// <param name="FuelOnArrival">Tonnes left on arrival, before any scoop here. <see langword="null"/> when the stop cannot be reached.</param>
/// <param name="Reachable">Whether the ship gets here at all, scooping at every scoopable star on the way.</param>
/// <param name="ScoopHere">Whether the projection refills here.</param>
/// <param name="Cost">Tonnes the jump into this stop burns, whether or not the ship can make it.</param>
public sealed record FuelStop(double? FuelOnArrival, bool Reachable, bool ScoopHere, double Cost);

/// <summary>The projection along the rest of a route.</summary>
/// <param name="Stops">One per stop ahead of the ship, in order.</param>
/// <param name="JumpsWithoutScooping">How many upcoming jumps the fuel in the tank covers with no scoop.</param>
/// <param name="MustScoopBy">Index into <paramref name="Stops"/> of the last scoopable stop reachable on the fuel in the tank, or <see langword="null"/> when none is.</param>
/// <param name="RunsDry">Whether the ship runs out before the end even when scooping wherever it can.</param>
public sealed record FuelProjection(IReadOnlyList<FuelStop> Stops, int JumpsWithoutScooping, int? MustScoopBy, bool RunsDry);

/// <summary>Runs a fuel model along the legs ahead.</summary>
public static class FuelPlanner
{
    /// <summary>Projects fuel along the route ahead with one model for every jump, refilling at every scoopable stop.</summary>
    /// <param name="model">The ship's fuel model, the same whatever is aboard.</param>
    /// <param name="fuelNow">Tonnes in the main tank now.</param>
    /// <param name="capacity">The main tank's size, tonnes. A scoop fills to this.</param>
    /// <param name="legs">The legs ahead, next jump first.</param>
    /// <returns>The projection. With no legs, no stops and zero jumps.</returns>
    public static FuelProjection Project(JumpFuelModel model, double fuelNow, double capacity, IReadOnlyList<FuelLeg> legs)
    {
        ArgumentNullException.ThrowIfNull(model);
        return Project(_ => model, fuelNow, capacity, legs);
    }

    /// <summary>
    /// Projects fuel along the route ahead, refilling at every scoopable stop, asking for the model before
    /// each jump at the fuel then aboard. A ship burns fuel and gets lighter, so a model that depends on
    /// the mass aboard, such as <see cref="Routing.JumpProfile.FuelModel"/>, is followed jump by jump
    /// rather than fixed at the start.
    /// </summary>
    /// <param name="modelAt">The ship's fuel model for a jump that starts with the given tonnes in the main tank.</param>
    /// <param name="fuelNow">Tonnes in the main tank now.</param>
    /// <param name="capacity">The main tank's size, tonnes. A scoop fills to this.</param>
    /// <param name="legs">The legs ahead, next jump first.</param>
    /// <returns>The projection. With no legs, no stops and zero jumps. Each stop's cost is the jump's cost on the path that scoops wherever it can.</returns>
    public static FuelProjection Project(Func<double, JumpFuelModel> modelAt, double fuelNow, double capacity, IReadOnlyList<FuelLeg> legs)
    {
        ArgumentNullException.ThrowIfNull(modelAt);
        ArgumentNullException.ThrowIfNull(legs);

        var stops = new List<FuelStop>();
        var fuel = Math.Max(0, fuelNow);
        var unscooped = Math.Max(0, fuelNow);
        var jumpsWithoutScooping = 0;
        var countingDry = true;
        int? mustScoopBy = null;
        var reachable = true;
        var runsDry = false;
        for (var index = 0; index < legs.Count; index++)
        {
            var leg = legs[index];
            var cost = modelAt(fuel).FuelFor(leg.DistanceLy, leg.Boost);
            if (countingDry)
            {
                var dryCost = modelAt(unscooped).FuelFor(leg.DistanceLy, leg.Boost);
                if (unscooped >= dryCost)
                {
                    unscooped -= dryCost;
                    jumpsWithoutScooping++;
                    if (leg.Scoopable)
                    {
                        mustScoopBy = index;
                    }
                }
                else
                {
                    countingDry = false;
                }
            }

            if (!reachable || fuel < cost)
            {
                reachable = false;
                runsDry = true;
                stops.Add(new FuelStop(null, false, false, cost));
                continue;
            }

            fuel -= cost;
            var scoop = leg.Scoopable && capacity > 0;
            stops.Add(new FuelStop(fuel, true, scoop, cost));
            if (scoop)
            {
                fuel = capacity;
            }
        }

        return new FuelProjection(stops, jumpsWithoutScooping, mustScoopBy, runsDry);
    }
}
