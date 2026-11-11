using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Katey.Core.Data;
using Katey.Core.Exploration;

namespace Katey.Core;

/// <summary>
/// The six data tables, loaded and cross-checked: every reference from one table to another resolves,
/// every grade is 1–5, and every enumerated field holds one of its allowed values.
/// </summary>
/// <remarks>
/// Lookups by blueprint key, experimental key, module type code and engineer name are ordinal and
/// case-sensitive, matching the keys the tables use. Lookups by material symbol are case-insensitive,
/// matching how the game journal spells symbols. Instances are immutable and safe to share between
/// threads.
/// </remarks>
public sealed class Catalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private Catalog(
        FrozenDictionary<string, Material> materials,
        FrozenDictionary<string, Blueprint> blueprints,
        FrozenDictionary<string, Experimental> experimentals,
        FrozenDictionary<string, ModuleType> moduleTypes,
        FrozenDictionary<string, EngineerProfile> engineers,
        FrozenDictionary<string, Unlock> unlocks)
    {
        Materials = materials;
        Blueprints = blueprints;
        Experimentals = experimentals;
        ModuleTypes = moduleTypes;
        Engineers = engineers;
        EngineersById = engineers.Values.ToFrozenDictionary(engineer => engineer.Id, engineer => engineer);
        Unlocks = unlocks;
    }

    /// <summary>Gets every material by journal symbol, case-insensitively.</summary>
    public IReadOnlyDictionary<string, Material> Materials { get; }

    /// <summary>Gets every blueprint by journal name.</summary>
    public IReadOnlyDictionary<string, Blueprint> Blueprints { get; }

    /// <summary>Gets every experimental effect by journal name.</summary>
    public IReadOnlyDictionary<string, Experimental> Experimentals { get; }

    /// <summary>Gets every module type by code, in table order.</summary>
    public IReadOnlyDictionary<string, ModuleType> ModuleTypes { get; }

    /// <summary>Gets every engineer by name, as the module tables spell it.</summary>
    public IReadOnlyDictionary<string, EngineerProfile> Engineers { get; }

    /// <summary>Gets every engineer by the game's engineer id.</summary>
    public IReadOnlyDictionary<long, EngineerProfile> EngineersById { get; }

    /// <summary>Gets every technology broker unlock by key, in table order: Guardian first, then human.</summary>
    public IReadOnlyDictionary<string, Unlock> Unlocks { get; }

    /// <summary>Loads the tables embedded in this assembly.</summary>
    /// <returns>A validated catalog.</returns>
    /// <exception cref="CatalogException">The embedded tables fail validation. Never expected from a shipped build.</exception>
    public static Catalog LoadEmbedded()
    {
        var assembly = typeof(Catalog).Assembly;
        return FromRows(
            ReadTable<MaterialRow>(assembly, "materials.json"),
            ReadTable<BlueprintRow>(assembly, "blueprints.json"),
            ReadTable<ExperimentalRow>(assembly, "experimentals.json"),
            ReadTable<ModuleTypeRow>(assembly, "moduleTypes.json"),
            ReadTable<EngineerRow>(assembly, "engineers.json"),
            ReadTable<UnlockRow>(assembly, "unlocks.json"));
    }

    /// <summary>Builds and validates a catalog from table rows, each keyed as the JSON files key them.</summary>
    /// <param name="materials">Material symbol → row.</param>
    /// <param name="blueprints">Blueprint journal name → row.</param>
    /// <param name="experimentals">Experimental journal name → row.</param>
    /// <param name="moduleTypes">Module type code → row, in display order.</param>
    /// <param name="engineers">Engineer name → row.</param>
    /// <returns>A validated catalog with no unlocks.</returns>
    /// <exception cref="CatalogException">Any rule in <see cref="CatalogValidator"/> is broken. The message lists every problem.</exception>
    public static Catalog FromRows(
        IReadOnlyDictionary<string, MaterialRow> materials,
        IReadOnlyDictionary<string, BlueprintRow> blueprints,
        IReadOnlyDictionary<string, ExperimentalRow> experimentals,
        IReadOnlyDictionary<string, ModuleTypeRow> moduleTypes,
        IReadOnlyDictionary<string, EngineerRow> engineers)
        => FromRows(materials, blueprints, experimentals, moduleTypes, engineers, new Dictionary<string, UnlockRow>(StringComparer.Ordinal));

    /// <summary>Builds and validates a catalog from table rows, each keyed as the JSON files key them.</summary>
    /// <param name="materials">Material symbol → row.</param>
    /// <param name="blueprints">Blueprint journal name → row.</param>
    /// <param name="experimentals">Experimental journal name → row.</param>
    /// <param name="moduleTypes">Module type code → row, in display order.</param>
    /// <param name="engineers">Engineer name → row.</param>
    /// <param name="unlocks">Unlock key → row, in display order.</param>
    /// <returns>A validated catalog.</returns>
    /// <exception cref="CatalogException">Any rule in <see cref="CatalogValidator"/> is broken. The message lists every problem.</exception>
    public static Catalog FromRows(
        IReadOnlyDictionary<string, MaterialRow> materials,
        IReadOnlyDictionary<string, BlueprintRow> blueprints,
        IReadOnlyDictionary<string, ExperimentalRow> experimentals,
        IReadOnlyDictionary<string, ModuleTypeRow> moduleTypes,
        IReadOnlyDictionary<string, EngineerRow> engineers,
        IReadOnlyDictionary<string, UnlockRow> unlocks)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(blueprints);
        ArgumentNullException.ThrowIfNull(experimentals);
        ArgumentNullException.ThrowIfNull(moduleTypes);
        ArgumentNullException.ThrowIfNull(engineers);
        ArgumentNullException.ThrowIfNull(unlocks);

        var problems = CatalogValidator.Validate(materials, blueprints, experimentals, moduleTypes, engineers, unlocks);
        if (problems.Count > 0)
        {
            throw new CatalogException(problems);
        }

        var materialTable = materials.ToFrozenDictionary(
            pair => pair.Key,
            pair => new Material(
                pair.Value.Symbol,
                pair.Value.Name,
                Enum.Parse<MaterialCategory>(pair.Value.Category, ignoreCase: true),
                pair.Value.Grade,
                pair.Value.Line),
            StringComparer.OrdinalIgnoreCase);

        return new Catalog(
            materialTable,
            blueprints.ToFrozenDictionary(
                pair => pair.Key,
                pair => new Blueprint(pair.Key, pair.Value.Name, ToGradeMap(pair.Value.Grades, counts => counts)),
                StringComparer.Ordinal),
            experimentals.ToFrozenDictionary(
                pair => pair.Key,
                pair => new Experimental(pair.Key, pair.Value.Name, pair.Value.Components),
                StringComparer.Ordinal),
            moduleTypes.ToFrozenDictionary(
                pair => pair.Key,
                pair => new ModuleType(
                    pair.Key,
                    pair.Value.Name,
                    Enum.Parse<ModuleGroup>(pair.Value.Group, ignoreCase: true),
                    pair.Value.ItemPatterns,
                    pair.Value.Blueprints.ToDictionary(
                        bp => bp.Key,
                        IReadOnlyDictionary<int, IReadOnlyList<string>> (bp) => ToGradeMap(bp.Value, names => names),
                        StringComparer.Ordinal),
                    pair.Value.Experimentals),
                StringComparer.Ordinal),
            engineers.ToFrozenDictionary(
                pair => pair.Key,
                pair => new EngineerProfile(
                    pair.Value.Name,
                    pair.Value.Id,
                    pair.Value.System,
                    pair.Value.Base,
                    Enum.Parse<Region>(pair.Value.Region, ignoreCase: true),
                    new GalacticPosition(pair.Value.X, pair.Value.Y, pair.Value.Z)),
                StringComparer.Ordinal),
            unlocks.ToFrozenDictionary(
                pair => pair.Key,
                pair => new Unlock(
                    pair.Key,
                    pair.Value.Name,
                    Enum.Parse<BrokerKind>(pair.Value.Broker, ignoreCase: true),
                    Enum.Parse<UnlockCategory>(pair.Value.Category, ignoreCase: true),
                    pair.Value.ModuleType,
                    pair.Value.Items,
                    pair.Value.Materials,
                    pair.Value.Commodities),
                StringComparer.Ordinal));
    }

    /// <summary>Technology broker unlocks that make a module of one type available, in table order.</summary>
    /// <param name="moduleType">Module type code. Need not exist.</param>
    /// <returns>Empty when no unlock names the type.</returns>
    public IReadOnlyList<Unlock> UnlocksFor(string moduleType)
    {
        ArgumentNullException.ThrowIfNull(moduleType);
        return [.. Unlocks.Values.Where(unlock => string.Equals(unlock.ModuleType, moduleType, StringComparison.Ordinal))];
    }

    /// <summary>Technology broker unlocks whose recipe lists a material, in table order.</summary>
    /// <param name="material">Material symbol, spelt as the tables spell it. Need not exist.</param>
    /// <returns>Empty for an unused or unknown material.</returns>
    public IReadOnlyList<Unlock> UnlocksUsing(string material)
    {
        ArgumentNullException.ThrowIfNull(material);
        return [.. Unlocks.Values.Where(unlock => unlock.Materials.ContainsKey(material))];
    }

    /// <summary>Module types that can be engineered, sorted by group (core first) and then by name.</summary>
    /// <returns>Never empty for the embedded catalog.</returns>
    public IReadOnlyList<ModuleType> EngineerableModuleTypes()

        => [
            .. ModuleTypes.Values
                .Where(type => type.IsEngineerable)
                .OrderBy(type => type.Group)
                .ThenBy(type => type.Name, StringComparer.Ordinal),
        ];

    /// <summary>Blueprints that can be applied to a module type, sorted by display name.</summary>
    /// <param name="moduleType">Module type code. Must exist.</param>
    /// <returns>Empty for a type that cannot be engineered.</returns>
    public IReadOnlyList<Blueprint> BlueprintsFor(string moduleType)

        => [
            .. ModuleTypes[moduleType].Blueprints.Keys
                .Select(key => Blueprints[key])
                .OrderBy(blueprint => blueprint.Name, StringComparer.Ordinal),
        ];

    /// <summary>Experimental effects that can be applied to a module type, sorted by display name.</summary>
    /// <param name="moduleType">Module type code. Must exist.</param>
    /// <returns>Empty for a type that cannot be engineered.</returns>
    public IReadOnlyList<Experimental> ExperimentalsFor(string moduleType)

        => [
            .. ModuleTypes[moduleType].Experimentals
                .Select(key => Experimentals[key])
                .OrderBy(experimental => experimental.Name, StringComparer.Ordinal),
        ];

    /// <summary>Engineers who offer a grade of a blueprint on a module type.</summary>
    /// <param name="moduleType">Module type code. Must exist.</param>
    /// <param name="blueprint">Blueprint key.</param>
    /// <param name="grade">Grade.</param>
    /// <returns>Empty when the module type does not offer the blueprint or the grade does not exist.</returns>
    public IReadOnlyList<string> EngineersFor(string moduleType, string blueprint, int grade)
        => ModuleTypes[moduleType].Blueprints.TryGetValue(blueprint, out var grades)
            && grades.TryGetValue(grade, out var engineers)
            ? engineers
            : [];

    /// <summary>
    /// Every engineer who offers any grade of any blueprint on a module type. In the game, any of them
    /// can also apply any of the type's experimental effects.
    /// </summary>
    /// <param name="moduleType">Module type code. Must exist.</param>
    /// <returns>Distinct names, in first-seen order.</returns>
    public IReadOnlyList<string> EngineersForModuleType(string moduleType)

        => [
            .. ModuleTypes[moduleType].Blueprints.Values
                .SelectMany(grades => grades.Values)
                .SelectMany(names => names)
                .Distinct(StringComparer.Ordinal),
        ];

    /// <summary>Module types a blueprint can be applied to, in catalog order.</summary>
    /// <param name="blueprint">Blueprint key. Need not exist.</param>
    /// <returns>Empty when no module type lists it.</returns>
    public IReadOnlyList<ModuleType> ModuleTypesWithBlueprint(string blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        return [.. ModuleTypes.Values.Where(type => type.Blueprints.ContainsKey(blueprint))];
    }

    /// <summary>Every blueprint grade whose recipe lists a material.</summary>
    /// <param name="material">Material symbol, spelt as the tables spell it. Need not exist.</param>
    /// <returns>Ordered by blueprint display name, then grade. Empty for an unused or unknown material.</returns>
    public IReadOnlyList<MaterialUse> MaterialUses(string material)
    {
        ArgumentNullException.ThrowIfNull(material);
        return
        [
            .. Blueprints.Values
                .OrderBy(blueprint => blueprint.Name, StringComparer.Ordinal)
                .SelectMany(blueprint => blueprint.Grades
                    .Where(grade => grade.Value.ContainsKey(material))
                    .OrderBy(grade => grade.Key)
                    .Select(grade => new MaterialUse(blueprint.Key, grade.Key, grade.Value[material]))),
        ];
    }

    /// <summary>Experimental effects whose recipe lists a material.</summary>
    /// <param name="material">Material symbol, spelt as the tables spell it. Need not exist.</param>
    /// <returns>Ordered by display name. Empty for an unused or unknown material.</returns>
    public IReadOnlyList<Experimental> ExperimentalsUsing(string material)
    {
        ArgumentNullException.ThrowIfNull(material);
        return
        [
            .. Experimentals.Values
                .Where(effect => effect.Components.ContainsKey(material))
                .OrderBy(effect => effect.Name, StringComparer.Ordinal),
        ];
    }

    /// <summary>Everything one engineer offers: each blueprint on each module type, with the top grade.</summary>
    /// <param name="engineer">Engineer name. Need not exist.</param>
    /// <returns>In picker order: by module group, then module type name, then blueprint display name. Empty for an unknown engineer.</returns>
    public IReadOnlyList<EngineerOffer> OffersOf(string engineer)
    {
        ArgumentNullException.ThrowIfNull(engineer);
        var offers = new List<EngineerOffer>();
        foreach (var type in EngineerableModuleTypes())
        {
            foreach (var blueprint in BlueprintsFor(type.Code))
            {
                var grades = type.Blueprints[blueprint.Key]
                    .Where(grade => grade.Value.Contains(engineer, StringComparer.Ordinal))
                    .Select(grade => grade.Key)
                    .ToList();
                if (grades.Count > 0)
                {
                    offers.Add(new EngineerOffer(type.Code, blueprint.Key, grades.Max()));
                }
            }
        }

        return offers;
    }

    private static Dictionary<int, TValue> ToGradeMap<TSource, TValue>(
        IReadOnlyDictionary<string, TSource> grades,
        Func<TSource, TValue> convert)
        => grades.ToDictionary(pair => int.Parse(pair.Key, CultureInfo.InvariantCulture), pair => convert(pair.Value));

    private static Dictionary<string, TRow> ReadTable<TRow>(Assembly assembly, string file)
    {
        using var stream = assembly.GetManifestResourceStream($"Katey.Core.Data.{file}")
            ?? throw new CatalogException([$"{file}: embedded resource missing"]);
        return JsonSerializer.Deserialize<Dictionary<string, TRow>>(stream, JsonOptions)
            ?? throw new CatalogException([$"{file}: not a JSON object"]);
    }
}

/// <summary>The data tables broke one or more of the catalog's rules.</summary>
public sealed class CatalogException : Exception
{
    /// <summary>Creates the exception from the problems found.</summary>
    /// <param name="problems">One sentence per problem. Must not be empty.</param>
    public CatalogException(IReadOnlyList<string> problems)
        : base($"The data tables have {problems?.Count ?? 0} problem(s):\n{string.Join('\n', problems ?? [])}")
        => Problems = problems ?? [];

    /// <summary>Creates an exception with no detail.</summary>
    public CatalogException()
        : base("The data tables are invalid.")
        => Problems = [];

    /// <summary>Creates an exception from a message.</summary>
    /// <param name="message">What went wrong.</param>
    public CatalogException(string message)
        : base(message)
        => Problems = [message];

    /// <summary>Creates an exception from a message and a cause.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The cause.</param>
    public CatalogException(string message, Exception innerException)
        : base(message, innerException)
        => Problems = [message];

    /// <summary>Gets every problem found, one sentence each.</summary>
    public IReadOnlyList<string> Problems { get; }
}
