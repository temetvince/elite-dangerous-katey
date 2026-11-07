using Katey.Core.Exploration;

namespace Katey.Core.Routing;

/// <summary>
/// The numbers a route plotter needs to know how far a ship jumps and what each jump burns: the drive's
/// fuel equation, the ship's masses and tanks, and the boosts it carries.
/// </summary>
/// <param name="FuelPower">The drive's exponent, from <see cref="FrameShiftDrives.FuelPowerOf"/>.</param>
/// <param name="FuelMultiplier">The drive's fuel multiplier, from <see cref="FrameShiftDrives.FuelMultiplierOf"/>.</param>
/// <param name="OptimalMass">The drive's optimal mass in tonnes, engineering included.</param>
/// <param name="MaxFuelPerJump">The most one jump may burn in tonnes, engineering included.</param>
/// <param name="BaseMass">The hull and modules with the reserve tank full and the main tank empty, tonnes.</param>
/// <param name="TankSize">The main tank's capacity in tonnes, every fuel tank module included.</param>
/// <param name="InternalTankSize">The reserve tank's capacity in tonnes.</param>
/// <param name="RangeBoost">Light years a Guardian booster adds, or 0.</param>
/// <param name="SuperchargeMultiplier">The range multiplier a neutron star gives this drive: 4, or 6 for the size 8 Mk II drive.</param>
/// <remarks>Built from a loadout's figures by <see cref="Build"/>: the game's own <c>Loadout</c> event and EDSY's export of one carry the same fields.</remarks>
public sealed record JumpProfile(
    double FuelPower,
    double FuelMultiplier,
    double OptimalMass,
    double MaxFuelPerJump,
    double BaseMass,
    double TankSize,
    double InternalTankSize,
    double RangeBoost,
    int SuperchargeMultiplier)
{
    /// <summary>The longest jump the ship makes with a full tank.</summary>
    /// <param name="cargo">Tonnes of cargo aboard. Not negative.</param>
    /// <returns>Light years, booster included.</returns>
    public double Range(double cargo = 0) => RangeAt(TankSize, cargo);

    /// <summary>The longest jump the ship can make at all: with only one jump's fuel aboard, which is the figure the game calls the maximum jump range.</summary>
    /// <param name="cargo">Tonnes of cargo aboard. Not negative.</param>
    /// <returns>Light years, booster included.</returns>
    public double MaxRange(double cargo = 0) => RangeAt(MaxFuelPerJump, cargo);

    /// <summary>The longest jump at a given load.</summary>
    /// <param name="fuelAboard">Tonnes in the main tank.</param>
    /// <param name="cargo">Tonnes of cargo aboard.</param>
    /// <returns>Light years, booster included.</returns>
    public double RangeAt(double fuelAboard, double cargo)
        => (OptimalMass / (BaseMass + fuelAboard + cargo) * Math.Pow(MaxFuelPerJump / FuelMultiplier, 1 / FuelPower)) + RangeBoost;

    /// <summary>
    /// The fuel model the game applies to this ship at a given load: the fuel equation's scale is the
    /// multiplier times the ratio of the mass aboard to the drive's optimal mass, raised to the drive's
    /// power, so the model needs no jump to be observed first. The mass is the one aboard at the moment
    /// asked; <see cref="FuelPlanner"/> asks again before each jump with the fuel then left,
    /// so a ship lightening as it burns is followed.
    /// </summary>
    /// <param name="fuelAboard">Tonnes in the main tank. Negative counts as 0.</param>
    /// <param name="cargo">Tonnes of cargo. Negative counts as 0.</param>
    /// <returns>The model, with the drive's maximum per jump and the booster's range.</returns>
    public JumpFuelModel FuelModel(double fuelAboard, double cargo)
    {
        var mass = BaseMass + Math.Max(0, fuelAboard) + Math.Max(0, cargo);
        return new JumpFuelModel(FuelMultiplier * Math.Pow(mass / OptimalMass, FuelPower), FuelPower, MaxFuelPerJump, RangeBoost);
    }

    /// <summary>
    /// Builds the profile from a loadout's figures. The drive is the first module whose item is a frame
    /// shift drive; its engineering is read from its modifiers, where <c>MaxFuelPerJump</c> and
    /// <c>FSDOptimalMass</c> replace the stock figures when present and positive. A Guardian booster
    /// anywhere in the loadout adds its range. When the game's own maximum jump range is given, the fuel
    /// cap is set so that <see cref="MaxRange(double)"/> with no cargo equals it: the game quotes that range
    /// with one jump's fuel aboard, and its figure is the authority over the table.
    /// </summary>
    /// <param name="modules">Every module, with its item symbol in any letter case and its modified statistics by label, empty for a stock module.</param>
    /// <param name="unladenMass">The hull and modules without fuel or cargo, tonnes.</param>
    /// <param name="mainTank">The main tank's capacity, tonnes.</param>
    /// <param name="reserveTank">The reserve tank's capacity, tonnes, or 0 when unknown.</param>
    /// <param name="maxJumpRange">The game's maximum jump range for the loadout, full tank and no cargo, light years, or <see langword="null"/> when unknown. Ignored unless it exceeds the booster's range.</param>
    /// <returns>The profile, or <see langword="null"/> when no module is a frame shift drive the table knows.</returns>
    public static JumpProfile? Build(IEnumerable<ProfileModule> modules, double unladenMass, double mainTank, double reserveTank, double? maxJumpRange = null)
    {
        ArgumentNullException.ThrowIfNull(modules);
        var list = modules.ToList();
        var drive = list.FirstOrDefault(module => FrameShiftDrives.Figures(module.Item) is not null);
        if (drive is null)
        {
            return null;
        }

        var maxFuel = drive.Modifiers.TryGetValue("MaxFuelPerJump", out var modifiedFuel) && modifiedFuel > 0
            ? modifiedFuel
            : FrameShiftDrives.MaxFuelPerJumpOf(drive.Item);
        var optimalMass = drive.Modifiers.TryGetValue("FSDOptimalMass", out var modifiedMass) && modifiedMass > 0
            ? modifiedMass
            : FrameShiftDrives.OptimalMassOf(drive.Item);
        var booster = list.Select(module => FrameShiftDrives.GuardianBoosterLy(module.Item)).DefaultIfEmpty(0).Max();
        var power = FrameShiftDrives.FuelPowerOf(drive.Item);
        var multiplier = FrameShiftDrives.FuelMultiplierOf(drive.Item);
        if (maxJumpRange is { } range && range > booster)
        {
            // The range equation solved for the fuel cap, at the load the game quotes the range for: the ship with one
            // jump's fuel aboard. The cap is on both sides, so a few rounds from the table's value settle it.
            for (var round = 0; round < 8; round++)
            {
                maxFuel = multiplier * Math.Pow((range - booster) * (unladenMass + reserveTank + maxFuel) / optimalMass, power);
            }
        }

        return new JumpProfile(
            power,
            multiplier,
            optimalMass,
            maxFuel,
            unladenMass + reserveTank,
            mainTank,
            reserveTank,
            booster,
            FrameShiftDrives.SuperchargeMultiplier(drive.Item));
    }
}

/// <summary>One module as <see cref="JumpProfile.Build"/> reads it.</summary>
/// <param name="Item">The item symbol, in any letter case.</param>
/// <param name="Modifiers">The engineering's modified statistics by label, as the game and EDSY write them. Empty for a stock module.</param>
public sealed record ProfileModule(string Item, IReadOnlyDictionary<string, double> Modifiers)
{
    /// <summary>Gets no modifiers, for a stock module.</summary>
    public static IReadOnlyDictionary<string, double> None { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
}
