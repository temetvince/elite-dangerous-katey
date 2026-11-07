using System.Globalization;
using System.Text.RegularExpressions;
using Katey.Core.Data;

namespace Katey.Core;

/// <summary>
/// The rules the six tables must satisfy before they become a <see cref="Catalog"/>. Every rule is a
/// present-tense fact about the schema; a broken rule is reported as one sentence.
/// </summary>
public static class CatalogValidator
{
    private static readonly string[] Categories = ["raw", "manufactured", "encoded"];
    private static readonly string[] Groups = ["core", "optional", "hardpoint", "utility"];
    private static readonly string[] Regions = ["bubble", "colonia"];
    private static readonly string[] Brokers = ["guardian", "human"];
    private static readonly string[] UnlockCategories = ["modules", "weapons", "modifiedWeapons", "fighters", "preEngineered"];

    /// <summary>Checks the tables and lists every problem.</summary>
    /// <param name="materials">Material display name → row.</param>
    /// <param name="blueprints">Blueprint journal name → row.</param>
    /// <param name="experimentals">Experimental journal name → row.</param>
    /// <param name="moduleTypes">Module type code → row.</param>
    /// <param name="engineers">Engineer name → row.</param>
    /// <param name="unlocks">Unlock key → row.</param>
    /// <returns>Empty when the tables are valid.</returns>
    public static IReadOnlyList<string> Validate(
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

        var problems = new List<string>();
        CheckMaterials(materials, problems);
        CheckBlueprints(blueprints, materials, problems);
        CheckExperimentals(experimentals, materials, problems);
        CheckEngineers(engineers, problems);
        CheckModuleTypes(moduleTypes, blueprints, experimentals, engineers, problems);
        CheckUnlocks(unlocks, materials, moduleTypes, problems);
        return problems;
    }

    private static void CheckUnlocks(
        IReadOnlyDictionary<string, UnlockRow> unlocks,
        IReadOnlyDictionary<string, MaterialRow> materials,
        IReadOnlyDictionary<string, ModuleTypeRow> moduleTypes,
        List<string> problems)
    {
        foreach (var (key, row) in unlocks)
        {
            var where = $"unlocks.json \"{key}\"";
            Expect(!string.IsNullOrWhiteSpace(row.Name), $"{where}: name missing", problems);
            Expect(Brokers.Contains(row.Broker, StringComparer.Ordinal), $"{where}: broker must be guardian or human", problems);
            Expect(UnlockCategories.Contains(row.Category, StringComparer.Ordinal), $"{where}: category must be modules, weapons, modifiedWeapons, fighters or preEngineered", problems);
            Expect(row.ModuleType is null || moduleTypes.ContainsKey(row.ModuleType), $"{where}: unknown module type \"{row.ModuleType}\"", problems);
            Expect(row.Materials.Count > 0, $"{where}: at least one material", problems);
            CheckMaterialCounts(row.Materials, materials, where, problems);
            foreach (var (name, count) in row.Commodities)
            {
                Expect(!string.IsNullOrWhiteSpace(name) && count > 0, $"{where}: commodity \"{name}\" must have a name and a positive count", problems);
            }

            foreach (var item in row.Items)
            {
                Expect(item.Length > 0 && !item.Any(char.IsUpper), $"{where}: item prefix \"{item}\" must be lower case", problems);
            }
        }
    }

    private static void CheckMaterials(IReadOnlyDictionary<string, MaterialRow> materials, List<string> problems)
    {
        foreach (var (key, row) in materials)
        {
            var where = $"materials.json \"{key}\"";
            Expect(row.Name == key, $"{where}: name must equal its key", problems);
            Expect(!string.IsNullOrWhiteSpace(row.Symbol), $"{where}: symbol missing", problems);
            Expect(Categories.Contains(row.Category, StringComparer.Ordinal), $"{where}: category must be raw, manufactured or encoded", problems);
            Expect(row.Grade is >= 1 and <= 5, $"{where}: grade must be 1–5", problems);
            Expect(!string.IsNullOrWhiteSpace(row.Line), $"{where}: line missing", problems);
        }

        var symbols = materials.Values.Select(row => row.Symbol).ToList();
        Expect(symbols.Distinct(StringComparer.OrdinalIgnoreCase).Count() == symbols.Count, "materials.json: symbols must be unique", problems);
    }

    private static void CheckMaterialCounts(
        IReadOnlyDictionary<string, int> counts,
        IReadOnlyDictionary<string, MaterialRow> materials,
        string where,
        List<string> problems)
    {
        foreach (var (name, count) in counts)
        {
            Expect(materials.ContainsKey(name), $"{where}: unknown material \"{name}\"", problems);
            Expect(count > 0, $"{where}: count for \"{name}\" must be a positive integer", problems);
        }
    }

    private static void CheckBlueprints(
        IReadOnlyDictionary<string, BlueprintRow> blueprints,
        IReadOnlyDictionary<string, MaterialRow> materials,
        List<string> problems)
    {
        foreach (var (key, row) in blueprints)
        {
            var where = $"blueprints.json \"{key}\"";
            Expect(!string.IsNullOrWhiteSpace(row.Name), $"{where}: name missing", problems);
            Expect(row.Grades.Count > 0, $"{where}: at least one grade", problems);
            foreach (var (gradeKey, counts) in row.Grades)
            {
                Expect(IsGradeKey(gradeKey), $"{where}: grade key \"{gradeKey}\" must be \"1\"–\"5\"", problems);
                CheckMaterialCounts(counts, materials, $"{where} grade {gradeKey}", problems);
            }
        }
    }

    private static void CheckExperimentals(
        IReadOnlyDictionary<string, ExperimentalRow> experimentals,
        IReadOnlyDictionary<string, MaterialRow> materials,
        List<string> problems)
    {
        foreach (var (key, row) in experimentals)
        {
            var where = $"experimentals.json \"{key}\"";
            Expect(!string.IsNullOrWhiteSpace(row.Name), $"{where}: name missing", problems);
            CheckMaterialCounts(row.Components, materials, where, problems);
        }
    }

    private static void CheckEngineers(IReadOnlyDictionary<string, EngineerRow> engineers, List<string> problems)
    {
        foreach (var (key, row) in engineers)
        {
            var where = $"engineers.json \"{key}\"";
            Expect(row.Name == key, $"{where}: name must equal its key", problems);
            Expect(!string.IsNullOrWhiteSpace(row.System), $"{where}: system missing", problems);
            Expect(!string.IsNullOrWhiteSpace(row.Base), $"{where}: base missing", problems);
            Expect(Regions.Contains(row.Region, StringComparer.Ordinal), $"{where}: region must be bubble or colonia", problems);
            Expect(double.IsFinite(row.X) && double.IsFinite(row.Y) && double.IsFinite(row.Z), $"{where}: x, y and z must be finite numbers", problems);
        }
    }

    private static void CheckModuleTypes(
        IReadOnlyDictionary<string, ModuleTypeRow> moduleTypes,
        IReadOnlyDictionary<string, BlueprintRow> blueprints,
        IReadOnlyDictionary<string, ExperimentalRow> experimentals,
        IReadOnlyDictionary<string, EngineerRow> engineers,
        List<string> problems)
    {
        var engineersSeen = new HashSet<string>(StringComparer.Ordinal);
        var patternsSeen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (code, row) in moduleTypes)
        {
            var where = $"moduleTypes.json \"{code}\"";
            Expect(!string.IsNullOrWhiteSpace(row.Name), $"{where}: name missing", problems);
            Expect(Groups.Contains(row.Group, StringComparer.Ordinal), $"{where}: group must be core, optional, hardpoint or utility", problems);
            CheckItemPatterns(row.ItemPatterns, patternsSeen, where, problems);
            CheckModuleBlueprints(row.Blueprints, blueprints, engineers, engineersSeen, where, problems);
            Expect(row.Experimentals.Distinct(StringComparer.Ordinal).Count() == row.Experimentals.Count, $"{where}: duplicate experimental", problems);
            foreach (var experimental in row.Experimentals)
            {
                Expect(experimentals.ContainsKey(experimental), $"{where}: unknown experimental \"{experimental}\"", problems);
            }

            Expect(row.Experimentals.Count == 0 || row.Blueprints.Count > 0, $"{where}: experimentals listed on a module type with no blueprints", problems);
        }

        foreach (var name in engineers.Keys.Where(name => !engineersSeen.Contains(name)))
        {
            problems.Add($"engineers.json \"{name}\": offers no blueprint in moduleTypes.json");
        }
    }

    private static void CheckItemPatterns(IReadOnlyList<string> patterns, HashSet<string> seen, string where, List<string> problems)
    {
        Expect(patterns.Count > 0, $"{where}: itemPatterns must be a non-empty array", problems);
        foreach (var pattern in patterns)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                problems.Add($"{where}: every itemPattern must be a non-empty string");
                continue;
            }

            try
            {
                _ = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                Expect(seen.Add(pattern), $"{where}: itemPattern \"{pattern}\" is also used by another module type", problems);
            }
            catch (ArgumentException)
            {
                problems.Add($"{where}: itemPattern \"{pattern}\" is not a valid regular expression");
            }
        }
    }

    private static void CheckModuleBlueprints(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> moduleBlueprints,
        IReadOnlyDictionary<string, BlueprintRow> blueprints,
        IReadOnlyDictionary<string, EngineerRow> engineers,
        HashSet<string> engineersSeen,
        string where,
        List<string> problems)
    {
        foreach (var (blueprintKey, grades) in moduleBlueprints)
        {
            var known = blueprints.TryGetValue(blueprintKey, out var blueprint);
            Expect(known, $"{where}: unknown blueprint \"{blueprintKey}\"", problems);
            foreach (var (gradeKey, names) in grades)
            {
                var gradeWhere = $"{where} {blueprintKey} grade {gradeKey}";
                Expect(IsGradeKey(gradeKey), $"{where} {blueprintKey}: grade key \"{gradeKey}\" must be \"1\"–\"5\"", problems);
                Expect(!known || blueprint!.Grades.ContainsKey(gradeKey), $"{where} {blueprintKey}: grade {gradeKey} does not exist on that blueprint", problems);
                Expect(names.Distinct(StringComparer.Ordinal).Count() == names.Count, $"{gradeWhere}: duplicate engineer", problems);
                foreach (var name in names)
                {
                    if (engineers.ContainsKey(name))
                    {
                        engineersSeen.Add(name);
                    }
                    else
                    {
                        problems.Add($"{gradeWhere}: unknown engineer \"{name}\"");
                    }
                }
            }
        }
    }

    private static bool IsGradeKey(string key)
        => int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out var grade) && grade is >= 1 and <= 5;

    private static void Expect(bool condition, string message, List<string> problems)
    {
        if (!condition)
        {
            problems.Add(message);
        }
    }
}
