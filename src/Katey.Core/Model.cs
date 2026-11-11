using Katey.Core.Exploration;

namespace Katey.Core;

/// <summary>The three in-game material inventories.</summary>
public enum MaterialCategory
{
    /// <summary>Elements, grades 1–4.</summary>
    Raw,

    /// <summary>Manufactured components, grades 1–5.</summary>
    Manufactured,

    /// <summary>Encoded data, grades 1–5.</summary>
    Encoded,
}

/// <summary>Where a module fits on a ship. Drives how the picker groups module types.</summary>
public enum ModuleGroup
{
    /// <summary>Power plant, thrusters, drive, and the other core internals.</summary>
    Core,

    /// <summary>Optional internals.</summary>
    Optional,

    /// <summary>Weapons.</summary>
    Hardpoint,

    /// <summary>Utility mounts such as shield boosters and scanners.</summary>
    Utility,
}

/// <summary>Where an engineer's workshop is.</summary>
public enum Region
{
    /// <summary>The inhabited bubble around Sol.</summary>
    Bubble,

    /// <summary>The Colonia region.</summary>
    Colonia,
}

/// <summary>One engineering material.</summary>
/// <param name="Symbol">Frontier's internal id, as the journal reports it. Unique within the catalog, case-insensitively; the catalog's key, and what every recipe refers to a material by.</param>
/// <param name="Name">Display name. Unique within the catalog. Shown, never used as a key.</param>
/// <param name="Category">Which inventory holds it.</param>
/// <param name="Grade">Rarity, 1–5. Raw materials stop at 4.</param>
/// <param name="Line">The in-game family, such as "Wake Scans".</param>
public sealed record Material(string Symbol, string Name, MaterialCategory Category, int Grade, string Line);

/// <summary>
/// One blueprint. A single roll at a grade costs exactly that grade's materials; only the grades the
/// blueprint has are present.
/// </summary>
/// <param name="Key">Frontier journal name, such as <c>FSD_LongRange</c>. This is what EDSY exports.</param>
/// <param name="Name">Display name, such as "Increased range".</param>
/// <param name="Grades">Grade → material symbol → count, for every grade the blueprint has. Never empty.</param>
public sealed record Blueprint(
    string Key,
    string Name,
    IReadOnlyDictionary<int, IReadOnlyDictionary<string, int>> Grades)
{
    /// <summary>Gets the grades the blueprint has, ascending.</summary>
    public IReadOnlyList<int> GradeNumbers => [.. Grades.Keys.Order()];

    /// <summary>Gets the highest grade the blueprint has.</summary>
    public int MaxGrade => Grades.Keys.Max();

    /// <summary>The nearest grade this blueprint has that is not above <paramref name="grade"/>, or its lowest grade when every grade is above.</summary>
    /// <param name="grade">The requested grade.</param>
    /// <returns>A grade in <see cref="Grades"/>.</returns>
    public int ClampGrade(int grade)
    {
        var below = Grades.Keys.Where(g => g <= grade).ToList();
        return below.Count > 0 ? below.Max() : Grades.Keys.Min();
    }
}

/// <summary>One experimental effect.</summary>
/// <param name="Key">Frontier journal name, such as <c>special_fsd_heavy</c>.</param>
/// <param name="Name">Display name, such as "Mass Manager".</param>
/// <param name="Components">Material symbol → count. Empty for the three Legacy effects the game no longer applies.</param>
public sealed record Experimental(string Key, string Name, IReadOnlyDictionary<string, int> Components);

/// <summary>
/// One kind of module. A module type with no blueprints cannot be engineered; it is kept so an imported
/// build can still be described.
/// </summary>
/// <param name="Code">Short code such as <c>fsd</c>.</param>
/// <param name="Name">Display name such as "Frame Shift Drive".</param>
/// <param name="Group">Where it fits on a ship.</param>
/// <param name="ItemPatterns">
/// Regular-expression sources matched case-insensitively against an EDSY or journal item symbol. When
/// several module types match one item, the longest matching source wins.
/// </param>
/// <param name="Blueprints">Blueprint key → grade → engineers offering that grade. Only grades the blueprint has.</param>
/// <param name="Experimentals">Keys of the experimental effects that can be applied to this module type.</param>
public sealed record ModuleType(
    string Code,
    string Name,
    ModuleGroup Group,
    IReadOnlyList<string> ItemPatterns,
    IReadOnlyDictionary<string, IReadOnlyDictionary<int, IReadOnlyList<string>>> Blueprints,
    IReadOnlyList<string> Experimentals)
{
    /// <summary>Gets whether the game lets you engineer this module type.</summary>
    public bool IsEngineerable => Blueprints.Count > 0;
}

/// <summary>One engineer and where to find them.</summary>
/// <param name="Name">Display name, exactly as the module tables spell it. The tables cross-reference engineers by it.</param>
/// <param name="Id">The game's engineer id, as the journal reports it. Unique within the catalog; what a page key and the saved selection name an engineer by, since the journal's spelling of a name can differ from the tables'.</param>
/// <param name="System">Star system.</param>
/// <param name="Base">Workshop name.</param>
/// <param name="Region">Bubble or Colonia.</param>
/// <param name="Position">Where <paramref name="System"/> is, for distances.</param>
public sealed record EngineerProfile(string Name, long Id, string System, string Base, Region Region, GalacticPosition Position);

/// <summary>Which technology broker sells an unlock.</summary>
public enum BrokerKind
{
    /// <summary>A Guardian technology broker.</summary>
    Guardian,

    /// <summary>A human technology broker.</summary>
    Human,
}

/// <summary>What kind of thing a technology broker unlock is, for grouping.</summary>
public enum UnlockCategory
{
    /// <summary>Internal modules and reinforcements.</summary>
    Modules,

    /// <summary>Hardpoint weapons as the broker normally sells them.</summary>
    Weapons,

    /// <summary>The Guardian weapons in their modified, pre-engineered form.</summary>
    ModifiedWeapons,

    /// <summary>Ship-launched fighters.</summary>
    Fighters,

    /// <summary>Human modules sold already engineered.</summary>
    PreEngineered,
}

/// <summary>
/// One technology broker unlock: a one-off purchase in materials and commodities that makes a module, or
/// a fighter, available to buy at outfitting.
/// </summary>
/// <param name="Key">Stable key, such as <c>guardian_fsd_booster</c>.</param>
/// <param name="Name">Display name.</param>
/// <param name="Broker">Which broker sells it.</param>
/// <param name="Category">What kind of thing it is, for grouping.</param>
/// <param name="ModuleType">The module type code of what it unlocks, or <see langword="null"/> for a fighter.</param>
/// <param name="Items">Lower-case item symbol prefixes the purchase makes available. Empty when the game's symbol is not known, in which case the journal cannot say whether it has been bought.</param>
/// <param name="Materials">Material symbol → count. Never empty.</param>
/// <param name="Commodities">Commodity name → count, bought at a market rather than collected. May be empty.</param>
public sealed record Unlock(
    string Key,
    string Name,
    BrokerKind Broker,
    UnlockCategory Category,
    string? ModuleType,
    IReadOnlyList<string> Items,
    IReadOnlyDictionary<string, int> Materials,
    IReadOnlyDictionary<string, int> Commodities)
{
    /// <summary>Gets whether the journal can tell that this unlock has been bought.</summary>
    public bool IsDetectable => Items.Count > 0;

    /// <summary>Whether an item is one this unlock makes available.</summary>
    /// <param name="symbol">An item symbol in any spelling; it is read through <see cref="Fleet.ModuleSymbolNames.Canonical"/>.</param>
    /// <returns><see langword="true"/> when the canonical symbol starts with one of <see cref="Items"/>.</returns>
    public bool Covers(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        var canonical = Fleet.ModuleSymbolNames.Canonical(symbol);
        return Items.Any(prefix => canonical.StartsWith(prefix, StringComparison.Ordinal));
    }
}

/// <summary>One blueprint grade that consumes a material.</summary>
/// <param name="Blueprint">Blueprint key.</param>
/// <param name="Grade">The grade whose recipe lists the material.</param>
/// <param name="Count">How many one roll at that grade costs.</param>
public sealed record MaterialUse(string Blueprint, int Grade, int Count);

/// <summary>One blueprint an engineer offers on one module type.</summary>
/// <param name="ModuleType">Module type code.</param>
/// <param name="Blueprint">Blueprint key.</param>
/// <param name="MaxGrade">The highest grade the engineer offers there.</param>
public sealed record EngineerOffer(string ModuleType, string Blueprint, int MaxGrade);
