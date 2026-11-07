namespace Katey.Core.Planning;

/// <summary>What a set of engineers can do to one blueprint on one module type.</summary>
/// <param name="ModuleType">Module type code.</param>
/// <param name="Blueprint">Blueprint key.</param>
/// <param name="MaxGrade">Highest grade any selected engineer offers, or <see langword="null"/> when none does.</param>
/// <param name="EngineersAtMax">Selected engineers offering <paramref name="MaxGrade"/>. Empty when it is <see langword="null"/>.</param>
/// <param name="Experimentals">Whether any selected engineer can apply the module type's experimental effects: true when any of them offers any blueprint on the type.</param>
public sealed record BlueprintCoverage(
    string ModuleType,
    string Blueprint,
    int? MaxGrade,
    IReadOnlyList<string> EngineersAtMax,
    bool Experimentals);

/// <summary>Which engineers can serve one plan entry.</summary>
/// <param name="CanRoll">Every engineer who offers the entry's target grade. For an experimental-only entry, everyone who can apply its effect, or nobody when it has none.</param>
/// <param name="CanApplyExperimental">Every engineer who can apply the entry's experimental. Empty when it has none.</param>
/// <param name="SelectedCanRoll"><paramref name="CanRoll"/> filtered to the selection.</param>
/// <param name="SelectedCanApplyExperimental"><paramref name="CanApplyExperimental"/> filtered to the selection.</param>
public sealed record EntryService(
    IReadOnlyList<string> CanRoll,
    IReadOnlyList<string> CanApplyExperimental,
    IReadOnlyList<string> SelectedCanRoll,
    IReadOnlyList<string> SelectedCanApplyExperimental);

/// <summary>Works out what a selection of engineers unlocks.</summary>
public static class EngineerCoverage
{
    /// <summary>Coverage for every engineerable module type and blueprint, in picker order.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="selected">Engineers the commander is willing to visit. Compared ordinally.</param>
    /// <returns>With no engineers selected, every row has a <see langword="null"/> max grade.</returns>
    public static IReadOnlyList<BlueprintCoverage> For(Catalog catalog, IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(selected);

        var rows = new List<BlueprintCoverage>();
        foreach (var moduleType in catalog.EngineerableModuleTypes())
        {
            var experimentals = catalog.EngineersForModuleType(moduleType.Code).Any(selected.Contains);
            foreach (var blueprint in catalog.BlueprintsFor(moduleType.Code))
            {
                int? maxGrade = null;
                IReadOnlyList<string> atMax = [];
                foreach (var grade in blueprint.GradeNumbers)
                {
                    var able = catalog.EngineersFor(moduleType.Code, blueprint.Key, grade).Where(selected.Contains).ToList();
                    if (able.Count > 0)
                    {
                        maxGrade = grade;
                        atMax = able;
                    }
                }

                rows.Add(new BlueprintCoverage(moduleType.Code, blueprint.Key, maxGrade, atMax, experimentals));
            }
        }

        return rows;
    }

    /// <summary>The highest grade the selected engineers reach on one blueprint for one module type.</summary>
    /// <param name="type">The module type.</param>
    /// <param name="blueprint">Blueprint key.</param>
    /// <param name="selected">Engineers the commander is willing to visit. Compared ordinally.</param>
    /// <returns>The grade, or <see langword="null"/> when nobody selected offers the blueprint on the type, or the type does not take it.</returns>
    public static int? Reach(ModuleType type, string blueprint, IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(selected);
        return type.Blueprints.TryGetValue(blueprint, out var grades)
            ? grades.Where(grade => grade.Value.Any(selected.Contains)).Select(grade => (int?)grade.Key).DefaultIfEmpty(null).Max()
            : null;
    }

    /// <summary>The grade a picker starts at: the highest the selected engineers reach, else the blueprint's top grade.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="type">The module type.</param>
    /// <param name="blueprint">Blueprint key. Must exist in the catalog.</param>
    /// <param name="selected">Engineers the commander is willing to visit. Compared ordinally.</param>
    /// <returns>A grade the blueprint has.</returns>
    public static int StartingGrade(Catalog catalog, ModuleType type, string blueprint, IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return Reach(type, blueprint, selected) ?? catalog.Blueprints[blueprint].MaxGrade;
    }

    /// <summary>Whether any selected engineer offers any blueprint on a module type, which is when they can apply its experimental effects.</summary>
    /// <param name="type">The module type.</param>
    /// <param name="selected">Engineers the commander is willing to visit. Compared ordinally.</param>
    /// <returns><see langword="true"/> when at least one selected engineer offers at least one grade of some blueprint on the type.</returns>
    public static bool OffersAny(ModuleType type, IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(selected);
        return type.Blueprints.Values.Any(grades => grades.Values.Any(names => names.Any(selected.Contains)));
    }

    /// <summary>The blueprints of a module type the selected engineers can roll at some grade.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="type">The module type.</param>
    /// <param name="selected">Engineers the commander is willing to visit. Compared ordinally.</param>
    /// <returns>A subsequence of <see cref="Catalog.BlueprintsFor"/>, in its order. Empty with nobody selected.</returns>
    public static IReadOnlyList<Blueprint> Blueprints(Catalog catalog, ModuleType type, IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(type);
        return [.. catalog.BlueprintsFor(type.Code).Where(blueprint => Reach(type, blueprint.Key, selected) is not null)];
    }

    /// <summary>The grades of a blueprint the selected engineers reach on a module type.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="type">The module type.</param>
    /// <param name="blueprint">Blueprint key. Must exist in the catalog.</param>
    /// <param name="selected">Engineers the commander is willing to visit. Compared ordinally.</param>
    /// <returns>The blueprint's grades up to and including <see cref="Reach"/>, lowest first. Empty when nobody selected offers it.</returns>
    public static IReadOnlyList<int> Grades(Catalog catalog, ModuleType type, string blueprint, IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return Reach(type, blueprint, selected) is { } reach
            ? [.. catalog.Blueprints[blueprint].GradeNumbers.Where(grade => grade <= reach)]
            : [];
    }

    /// <summary>The module types the selected engineers can roll a blueprint on.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="blueprint">Blueprint key. Must exist in the catalog.</param>
    /// <param name="selected">Engineers the commander is willing to visit. Compared ordinally.</param>
    /// <returns>A subsequence of <see cref="Catalog.ModuleTypesWithBlueprint"/>, in its order. Empty with nobody selected.</returns>
    public static IReadOnlyList<ModuleType> ModuleTypes(Catalog catalog, string blueprint, IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(blueprint);
        return [.. catalog.ModuleTypesWithBlueprint(blueprint).Where(type => Reach(type, blueprint, selected) is not null)];
    }

    /// <summary>Engineers who can roll an entry's target grade and apply its experimental.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="entry">The entry. An entry with no module type has nobody, and so does an unlock: a technology broker sells it, not an engineer.</param>
    /// <param name="selected">Engineers the commander is willing to visit.</param>
    /// <returns>The service.</returns>
    public static EntryService ServiceFor(Catalog catalog, PlanEntry entry, IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(selected);

        if (entry.IsUnlock || entry.ModuleType is not { } moduleType || !catalog.ModuleTypes.ContainsKey(moduleType))
        {
            return new EntryService([], [], [], []);
        }

        var canApply = entry.Experimental is null ? [] : catalog.EngineersForModuleType(moduleType);
        var canRoll = entry.IsExperimentalOnly ? canApply : catalog.EngineersFor(moduleType, entry.Blueprint, entry.TargetGrade);
        return new EntryService(
            canRoll,
            canApply,
            [.. canRoll.Where(selected.Contains)],
            [.. canApply.Where(selected.Contains)]);
    }
}
