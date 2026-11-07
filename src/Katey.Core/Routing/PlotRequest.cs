using System.Globalization;

namespace Katey.Core.Routing;

/// <summary>How the plotter weighs its choices.</summary>
public enum PlotAlgorithm
{
    /// <summary>Favours neutron jumps and penalises stretches with few neutron stars: usually the fewest jumps.</summary>
    Optimistic,

    /// <summary>Favours the sure route over the fast one.</summary>
    Pessimistic,
}

/// <summary>A galaxy plot to ask for: where from, where to, in what ship, and how to route.</summary>
/// <param name="Source">The departure system's name.</param>
/// <param name="Destination">The destination system's name.</param>
/// <param name="Ship">The ship's jump numbers.</param>
/// <param name="Cargo">Tonnes of cargo aboard. Not negative.</param>
/// <param name="Algorithm">How the plotter weighs its choices.</param>
/// <param name="UseSupercharge">Whether the route may take neutron stars for the boost.</param>
/// <param name="UseInjections">Whether the route may count on FSD injections.</param>
/// <param name="ExcludeSecondary">Whether the route avoids systems whose scoopable star is not the arrival star.</param>
/// <param name="RefuelEveryScoopable">Whether the plot fills the tank at every scoopable star it passes.</param>
/// <param name="ReserveSize">Tonnes to keep in the tank at every stop. Not negative.</param>
/// <param name="IsSupercharged">Whether the drive is already supercharged at departure.</param>
public sealed record PlotRequest(
    string Source,
    string Destination,
    JumpProfile Ship,
    double Cargo = 0,
    PlotAlgorithm Algorithm = PlotAlgorithm.Optimistic,
    bool UseSupercharge = true,
    bool UseInjections = false,
    bool ExcludeSecondary = false,
    bool RefuelEveryScoopable = false,
    double ReserveSize = 0,
    bool IsSupercharged = false)
{
    /// <summary>The request as the form fields Spansh's galaxy plotter takes, in its names.</summary>
    /// <returns>Field names and values. Numbers are written invariantly; flags as 1 or 0.</returns>
    public IReadOnlyList<KeyValuePair<string, string>> FormFields()
    {
        return
        [
            Field("source", Source),
            Field("destination", Destination),
            Field("fuel_power", Ship.FuelPower),
            Field("fuel_multiplier", Ship.FuelMultiplier),
            Field("optimal_mass", Ship.OptimalMass),
            Field("max_fuel_per_jump", Ship.MaxFuelPerJump),
            Field("base_mass", Ship.BaseMass),
            Field("tank_size", Ship.TankSize),
            Field("internal_tank_size", Ship.InternalTankSize),
            Field("range_boost", Ship.RangeBoost),
            Field("supercharge_multiplier", Ship.SuperchargeMultiplier),
            Field("cargo", Cargo),
            Field("reserve_size", ReserveSize),
            Field("algorithm", Algorithm == PlotAlgorithm.Pessimistic ? "pessimistic" : "optimistic"),
            Field("use_supercharge", UseSupercharge),
            Field("use_injections", UseInjections),
            Field("exclude_secondary", ExcludeSecondary),
            Field("refuel_every_scoopable", RefuelEveryScoopable),
            Field("is_supercharged", IsSupercharged),
        ];
    }

    private static KeyValuePair<string, string> Field(string name, string value) => new(name, value);

    private static KeyValuePair<string, string> Field(string name, double value) => new(name, value.ToString("0.####", CultureInfo.InvariantCulture));

    private static KeyValuePair<string, string> Field(string name, bool value) => new(name, value ? "1" : "0");
}
