using System.Collections.Immutable;

namespace Katey.Core.Planning;

/// <summary>Where a plan entry came from.</summary>
/// <param name="Ship">The ship name from an EDSY export, or <see langword="null"/> for a manual entry.</param>
/// <param name="Slot">The slot such as <c>FrameShiftDrive</c>, or <see langword="null"/> for a manual entry.</param>
/// <param name="Item">The item symbol, or <see langword="null"/> for a manual entry.</param>
public sealed record PlanSource(string? Ship, string? Slot, string? Item)
{
    /// <summary>Gets the source of an entry the user added by hand.</summary>
    public static PlanSource Manual { get; } = new(null, null, null);

    /// <summary>Gets whether this entry was imported from a build.</summary>
    public bool IsImported => Ship is not null;
}

/// <summary>
/// One thing to buy with materials: a module to engineer, or a technology broker unlock. For a module,
/// the materials are the sum, over every grade from 1 to <see cref="TargetGrade"/> that the blueprint
/// has, of that grade's materials times the roll count for that grade, plus the experimental's materials
/// once. For an unlock, they are the unlock's fixed cost; the blueprint, grade, rolls and experimental
/// are then unused and empty.
/// </summary>
/// <param name="Id">Unique within a plan.</param>
/// <param name="ModuleType">Module type code, or <see langword="null"/> when an imported item matched no known type or the unlock is a fighter. The blueprint still prices a module entry; only the engineer lookups are empty.</param>
/// <param name="Blueprint">Blueprint key, which must exist in the catalog; empty for an unlock.</param>
/// <param name="TargetGrade">A grade the blueprint has, or 0 when only the experimental effect is wanted on a module already engineered. 0 for an unlock.</param>
/// <param name="Rolls">Grade → rolls to perform. Grades above the target are ignored. Every count is non-negative. Empty for an unlock.</param>
/// <param name="Experimental">Experimental key, or <see langword="null"/> for none.</param>
/// <param name="Enabled">A disabled entry stays in the plan but contributes no materials.</param>
/// <param name="Source">Where the entry came from.</param>
/// <param name="Unlock">The unlock's key, which must exist in the catalog, or <see langword="null"/> for a module to engineer.</param>
/// <param name="CraftsAfter">
/// Engineering the game reported at or before this instant does not count toward the entry; only later
/// rolls advance it. Set to the moment of creation, and moved forward to each roll applied, so a roll
/// is never applied twice and the history before the entry existed is never applied at all.
/// <see langword="null"/> on an entry saved before this was recorded; the application anchors it when it loads.
/// </param>
public sealed record PlanEntry(
    string Id,
    string? ModuleType,
    string Blueprint,
    int TargetGrade,
    ImmutableDictionary<int, int> Rolls,
    string? Experimental,
    bool Enabled,
    PlanSource Source,
    string? Unlock = null,
    DateTimeOffset? CraftsAfter = null)
{
    /// <summary>
    /// As many rolls at each grade as its number: one at grade 1 up to five at grade 5, the usual count to
    /// bring a grade to its full effect before moving up.
    /// </summary>
    public static ImmutableDictionary<int, int> DefaultRolls { get; } = Enumerable.Range(1, 5).ToImmutableDictionary(grade => grade, grade => grade);

    /// <summary>Gets whether the entry is a technology broker unlock rather than a module to engineer.</summary>
    public bool IsUnlock => Unlock is not null;

    /// <summary>Gets whether the entry wants only the experimental effect: a module entry whose target grade is 0, so no grade is rolled.</summary>
    public bool IsExperimentalOnly => !IsUnlock && TargetGrade == 0;

    /// <summary>
    /// Whether another entry asks for the same work: the same unlock, or the same module type, blueprint,
    /// target grade and experimental for the same ship and slot. Ids, rolls, the enabled flag and the
    /// craft anchor do not count, so an entry added twice from the same page reads as the same work.
    /// </summary>
    /// <param name="other">The entry to compare with.</param>
    /// <returns><see langword="true"/> when the two describe one job.</returns>
    public bool IsSameWorkAs(PlanEntry other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return IsUnlock || other.IsUnlock
            ? string.Equals(Unlock, other.Unlock, StringComparison.OrdinalIgnoreCase)
            : string.Equals(ModuleType, other.ModuleType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Blueprint, other.Blueprint, StringComparison.OrdinalIgnoreCase)
            && TargetGrade == other.TargetGrade
            && string.Equals(Experimental, other.Experimental, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Source.Ship, other.Source.Ship, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Source.Slot, other.Source.Slot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Creates an enabled entry for a technology broker unlock, with a fresh id.</summary>
    /// <param name="catalog">The catalog the unlock belongs to.</param>
    /// <param name="unlock">Unlock key. Must exist in <paramref name="catalog"/>.</param>
    /// <returns>The entry, carrying the unlock's module type.</returns>
    public static PlanEntry ForUnlock(Catalog catalog, string unlock)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(unlock);
        return new PlanEntry(
            Guid.NewGuid().ToString("N"),
            catalog.Unlocks[unlock].ModuleType,
            "",
            0,
            [],
            null,
            Enabled: true,
            PlanSource.Manual,
            unlock,
            DateTimeOffset.UtcNow);
    }

    /// <summary>Creates an enabled entry with <see cref="DefaultRolls"/> and a fresh id. The target grade is clamped to a grade the blueprint has, or kept at 0 for experimental only.</summary>
    /// <param name="catalog">The catalog the blueprint belongs to.</param>
    /// <param name="moduleType">Module type code, or <see langword="null"/>.</param>
    /// <param name="blueprint">Blueprint key. Must exist in <paramref name="catalog"/>.</param>
    /// <param name="targetGrade">Requested grade. Zero or less means experimental only.</param>
    /// <param name="experimental">Experimental key, or <see langword="null"/>.</param>
    /// <param name="source">Where the entry came from.</param>
    /// <returns>The entry.</returns>
    public static PlanEntry Create(
        Catalog catalog,
        string? moduleType,
        string blueprint,
        int targetGrade,
        string? experimental,
        PlanSource source)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(source);

        return new PlanEntry(
            Guid.NewGuid().ToString("N"),
            moduleType,
            blueprint,
            targetGrade <= 0 ? 0 : catalog.Blueprints[blueprint].ClampGrade(targetGrade),
            DefaultRolls,
            experimental,
            Enabled: true,
            source,
            null,
            DateTimeOffset.UtcNow);
    }

    /// <summary>The grades this entry rolls: every grade the blueprint has, up to and including the target. Empty for experimental only.</summary>
    /// <param name="catalog">The catalog the blueprint belongs to.</param>
    /// <returns>Ascending grades.</returns>
    public IReadOnlyList<int> RolledGrades(Catalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return IsUnlock ? [] : [.. catalog.Blueprints[Blueprint].GradeNumbers.Where(grade => grade <= TargetGrade)];
    }

    /// <summary>Rolls at one grade.</summary>
    /// <param name="grade">Grade 1–5.</param>
    /// <returns>The count, or 0 when the grade is not in <see cref="Rolls"/>.</returns>
    public int RollsAt(int grade) => Rolls.GetValueOrDefault(grade, 0);

    /// <summary>A copy with the roll count at one grade replaced.</summary>
    /// <param name="grade">Grade 1–5.</param>
    /// <param name="count">Non-negative.</param>
    /// <returns>The copy.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public PlanEntry WithRolls(int grade, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return this with { Rolls = Rolls.SetItem(grade, count) };
    }

    /// <summary>A copy with the target grade replaced, clamped to a grade the blueprint has, or set to 0 for experimental only.</summary>
    /// <param name="catalog">The catalog the blueprint belongs to.</param>
    /// <param name="grade">Requested grade. Zero or less means experimental only.</param>
    /// <returns>The copy.</returns>
    public PlanEntry WithTargetGrade(Catalog catalog, int grade)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return IsUnlock ? this : this with { TargetGrade = grade <= 0 ? 0 : catalog.Blueprints[Blueprint].ClampGrade(grade) };
    }
}
