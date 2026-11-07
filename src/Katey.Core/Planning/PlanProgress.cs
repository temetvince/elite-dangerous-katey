namespace Katey.Core.Planning;

/// <summary>One engineering roll, as the game reported it, in the catalog's terms.</summary>
/// <param name="At">When it happened.</param>
/// <param name="ModuleType">The rolled module's type code, or <see langword="null"/> when the item matched no known type.</param>
/// <param name="Blueprint">Blueprint key.</param>
/// <param name="Level">The grade rolled, 1 to 5.</param>
/// <param name="Quality">The grade's progress after the roll, 0 to 1. 1 means the grade is complete.</param>
/// <param name="Experimental">The experimental effect the module carries after the roll, or <see langword="null"/>.</param>
public sealed record Craft(DateTimeOffset At, string? ModuleType, string Blueprint, int Level, double Quality, string? Experimental);

/// <summary>What applying rolls to a plan did.</summary>
/// <param name="Plan">The plan afterwards, in the same order less the completed entries.</param>
/// <param name="Completed">Entries whose target was reached and which left the plan, in plan order.</param>
/// <param name="Advanced">Entries still in the plan that a roll moved on, in plan order.</param>
public sealed record PlanAdvance(IReadOnlyList<PlanEntry> Plan, IReadOnlyList<PlanEntry> Completed, IReadOnlyList<PlanEntry> Advanced)
{
    /// <summary>Gets whether any entry changed.</summary>
    public bool Changed => Completed.Count > 0 || Advanced.Count > 0;
}

/// <summary>
/// Moves a plan on as the game reports engineering. A roll finds the first module entry for its
/// blueprint and module type that the roll is later than; it zeroes the rolls still planned at every
/// grade below the one rolled, since the module is past them, takes one roll off the grade rolled, or
/// all of them when the grade is complete, and drops the entry's experimental effect once the module
/// carries it, since there is nothing left to pay for it. The entry is done when every planned grade is
/// rolled out, the module stands at the target grade or above, and no effect is still wanted. A done
/// entry leaves the plan. Unlock entries are never touched, nor is an experimental-only entry with no
/// effect chosen, which has nothing the game could report.
/// </summary>
public static class PlanProgress
{
    /// <summary>Applies rolls to a plan, oldest first.</summary>
    /// <param name="catalog">The catalog the plan's blueprints belong to.</param>
    /// <param name="plan">The plan, in order.</param>
    /// <param name="crafts">The rolls, in any order. A roll at or before an entry's <see cref="PlanEntry.CraftsAfter"/> does not touch that entry.</param>
    /// <returns>The plan afterwards and what moved. Each entry that moved has its <see cref="PlanEntry.CraftsAfter"/> at the last roll applied to it.</returns>
    public static PlanAdvance Apply(Catalog catalog, IReadOnlyList<PlanEntry> plan, IEnumerable<Craft> crafts)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(crafts);

        var entries = plan.ToList();
        var completed = new List<PlanEntry>();
        var advanced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var craft in crafts.OrderBy(craft => craft.At))
        {
            var index = Target(entries, craft);
            if (index < 0)
            {
                continue;
            }

            var entry = Advance(catalog, entries[index], craft);
            if (IsDone(catalog, entry, craft))
            {
                completed.Add(entry);
                entries.RemoveAt(index);
                advanced.Remove(entry.Id);
            }
            else
            {
                entries[index] = entry;
                advanced.Add(entry.Id);
            }
        }

        return new PlanAdvance(entries, completed, [.. entries.Where(entry => advanced.Contains(entry.Id))]);
    }

    /// <summary>The earliest instant any module entry still accepts rolls after, so a caller can skip older rolls, or <see langword="null"/> when no entry accepts any.</summary>
    /// <param name="plan">The plan.</param>
    /// <returns>The instant, or <see langword="null"/>.</returns>
    public static DateTimeOffset? Floor(IReadOnlyList<PlanEntry> plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        DateTimeOffset? floor = null;
        foreach (var entry in plan.Where(entry => !entry.IsUnlock))
        {
            var after = entry.CraftsAfter ?? DateTimeOffset.MinValue;
            floor = floor is { } current && current <= after ? current : after;
        }

        return floor;
    }

    /// <summary>The entry a roll belongs to: the first for its blueprint and module type that still has rolls planned at that grade, else the first at all.</summary>
    private static int Target(List<PlanEntry> entries, Craft craft)
    {
        var first = -1;
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            if (!Matches(entry, craft))
            {
                continue;
            }

            if (entry.RollsAt(craft.Level) > 0)
            {
                return index;
            }

            if (first < 0)
            {
                first = index;
            }
        }

        return first;
    }

    private static bool Matches(PlanEntry entry, Craft craft)
        => !entry.IsUnlock
            && !(entry.IsExperimentalOnly && entry.Experimental is null)
            && craft.At > (entry.CraftsAfter ?? DateTimeOffset.MinValue)
            && string.Equals(entry.Blueprint, craft.Blueprint, StringComparison.OrdinalIgnoreCase)
            && (entry.ModuleType is null || craft.ModuleType is null || string.Equals(entry.ModuleType, craft.ModuleType, StringComparison.OrdinalIgnoreCase));

    private static PlanEntry Advance(Catalog catalog, PlanEntry entry, Craft craft)
    {
        var rolls = entry.Rolls.ToBuilder();
        foreach (var grade in entry.RolledGrades(catalog))
        {
            if (grade < craft.Level || (grade == craft.Level && craft.Quality >= 1))
            {
                rolls[grade] = 0;
            }
            else if (grade == craft.Level)
            {
                rolls[grade] = Math.Max(0, entry.RollsAt(grade) - 1);
            }
        }

        var effectApplied = entry.Experimental is not null && string.Equals(entry.Experimental, craft.Experimental, StringComparison.OrdinalIgnoreCase);
        return entry with { Rolls = rolls.ToImmutable(), Experimental = effectApplied ? null : entry.Experimental, CraftsAfter = craft.At };
    }

    private static bool IsDone(Catalog catalog, PlanEntry entry, Craft craft)
        => entry.Experimental is null
            && (entry.TargetGrade == 0
                || (craft.Level >= entry.TargetGrade && entry.RolledGrades(catalog).All(grade => entry.RollsAt(grade) == 0)));
}
