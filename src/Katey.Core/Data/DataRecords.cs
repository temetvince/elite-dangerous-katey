namespace Katey.Core.Data;

/// <summary>A material row as `materials.json` spells it.</summary>
/// <param name="Symbol">Journal symbol.</param>
/// <param name="Name">Display name; equals the row's key.</param>
/// <param name="Category">`raw`, `manufactured` or `encoded`.</param>
/// <param name="Grade">1–5.</param>
/// <param name="Line">In-game family.</param>
public sealed record MaterialRow(string Symbol, string Name, string Category, int Grade, string Line);

/// <summary>A blueprint row as `blueprints.json` spells it.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Grades">Grade key (`"1"`–`"5"`) → material name → count.</param>
public sealed record BlueprintRow(string Name, IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> Grades);

/// <summary>An experimental row as `experimentals.json` spells it.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Components">Material name → count.</param>
public sealed record ExperimentalRow(string Name, IReadOnlyDictionary<string, int> Components);

/// <summary>A module type row as `moduleTypes.json` spells it.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Group">`core`, `optional`, `hardpoint` or `utility`.</param>
/// <param name="ItemPatterns">Regular-expression sources.</param>
/// <param name="Blueprints">Blueprint key → grade key → engineer names.</param>
/// <param name="Experimentals">Experimental keys.</param>
public sealed record ModuleTypeRow(
    string Name,
    string Group,
    IReadOnlyList<string> ItemPatterns,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> Blueprints,
    IReadOnlyList<string> Experimentals);

/// <summary>A technology broker unlock row as `unlocks.json` spells it.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Broker">`guardian` or `human`.</param>
/// <param name="Category">`modules`, `weapons`, `modifiedWeapons`, `fighters` or `preEngineered`: the group the page shows it under.</param>
/// <param name="ModuleType">The module type code the unlocked module belongs to, or <see langword="null"/> for a fighter.</param>
/// <param name="Items">Lower-case item symbol prefixes the purchase makes available, for matching the journal's `TechnologyBroker` event. Empty when the game's symbol is not known.</param>
/// <param name="Materials">Material name → count.</param>
/// <param name="Commodities">Commodity name → count.</param>
public sealed record UnlockRow(
    string Name,
    string Broker,
    string Category,
    string? ModuleType,
    IReadOnlyList<string> Items,
    IReadOnlyDictionary<string, int> Materials,
    IReadOnlyDictionary<string, int> Commodities);

/// <summary>An engineer row as `engineers.json` spells it.</summary>
/// <param name="Name">Display name; equals the row's key.</param>
/// <param name="System">Star system.</param>
/// <param name="Base">Workshop.</param>
/// <param name="Region">`bubble` or `colonia`.</param>
/// <param name="X">The system's galactic x coordinate, light years.</param>
/// <param name="Y">The system's galactic y coordinate, light years.</param>
/// <param name="Z">The system's galactic z coordinate, light years.</param>
public sealed record EngineerRow(string Name, string System, string Base, string Region, double X, double Y, double Z);

/// <summary>One frame shift drive as <c>drives.json</c> spells it.</summary>
/// <param name="Symbol">Item symbol, lower case.</param>
/// <param name="Size">Drive size 2–8.</param>
/// <param name="Rating">Rating letter, A to E.</param>
/// <param name="OptimalMass">Optimal mass of the unmodified drive, tonnes.</param>
/// <param name="MaxFuelPerJump">The most fuel one jump may burn on the unmodified drive, tonnes.</param>
/// <param name="FuelMultiplier">The fuel equation's multiplier.</param>
/// <param name="FuelPower">The fuel equation's exponent.</param>
/// <param name="SuperchargeMultiplier">The range multiplier a neutron star gives it: 4, or 6 for the size 8 Mk II.</param>
public sealed record DriveRow(string Symbol, int Size, string Rating, double OptimalMass, double MaxFuelPerJump, double FuelMultiplier, double FuelPower, int SuperchargeMultiplier);
