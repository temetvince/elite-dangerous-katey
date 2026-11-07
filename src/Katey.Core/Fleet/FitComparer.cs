namespace Katey.Core.Fleet;

/// <summary>How a ship's slot falls short of what a fit asks for there.</summary>
public enum DifferenceKind
{
    /// <summary>The ship has nothing in the slot, or what it has there is spoken for by another of the fit's modules.</summary>
    SlotEmpty,

    /// <summary>The ship has a different item in the slot, and the item the fit wants is nowhere in a slot of that kind.</summary>
    DifferentModule,

    /// <summary>The right item, but stock where the fit wants engineering.</summary>
    NotEngineered,

    /// <summary>The right item, engineered with a different blueprint.</summary>
    DifferentBlueprint,

    /// <summary>The right item and blueprint, but below the required grade.</summary>
    LowerGrade,

    /// <summary>The right item, blueprint and grade, but the grade is not yet rolled to its full effect.</summary>
    UnfinishedGrade,

    /// <summary>The right item, blueprint and grade, but the experimental effect is missing or different.</summary>
    DifferentExperimental,

    /// <summary>The right item, blueprint and grade, but the ship's experimental effect is not known, so the fit may or may not be satisfied.</summary>
    ExperimentalUnknown,
}

/// <summary>One slot where a ship differs from a fit.</summary>
/// <param name="Required">What the fit wants in the slot.</param>
/// <param name="Fitted">
/// The ship's module the requirement was judged against: the same item found in the named slot or in
/// another slot of the same kind, or whatever sits in the named slot when the item is not on the ship, or
/// <see langword="null"/> when that slot is empty.
/// </param>
/// <param name="Kind">How it falls short.</param>
public sealed record FitDifference(FitModule Required, FitModule? Fitted, DifferenceKind Kind)
{
    /// <summary>Gets whether closing the difference means buying the module: the slot is empty or holds another item.</summary>
    public bool NeedsPurchase => Kind is DifferenceKind.SlotEmpty or DifferenceKind.DifferentModule;

    /// <summary>
    /// Gets whether closing the difference means engineering work: the fit wants engineering and the ship's
    /// module does not already carry it for certain.
    /// </summary>
    public bool NeedsEngineering => Required.Engineering is not null && Kind != DifferenceKind.ExperimentalUnknown;

    /// <summary>
    /// Gets whether the only engineering left is the experimental effect: the ship's module already has the
    /// blueprint at the required grade or higher.
    /// </summary>
    public bool NeedsOnlyExperimental => Kind == DifferenceKind.DifferentExperimental;

    /// <summary>Gets whether the module judged sits in a different slot from the one the fit names.</summary>
    public bool FoundElsewhere => Fitted is not null && !string.Equals(Fitted.Slot, Required.Slot, StringComparison.OrdinalIgnoreCase);
}

/// <summary>A fit laid against one ship's actual modules, slot by slot.</summary>
/// <param name="Fitting">The fit.</param>
/// <param name="Matching">Required modules the ship already satisfies, in fit order.</param>
/// <param name="Differences">Required modules it does not, in fit order.</param>
public sealed record FitComparison(Fitting Fitting, IReadOnlyList<FitModule> Matching, IReadOnlyList<FitDifference> Differences)
{
    /// <summary>Gets how many of the fit's modules the ship satisfies.</summary>
    public int MatchCount => Matching.Count;

    /// <summary>Gets whether the ship satisfies every module.</summary>
    public bool IsComplete => Differences.Count == 0;
}

/// <summary>Compares a saved fit with the modules actually fitted to one ship.</summary>
/// <remarks>
/// A required item is looked for in the slot the fit names first, then in any other slot of the same kind
/// (see <see cref="SlotNames.Parse"/>), nearest in size first, so a module the ship carries in a different
/// optional bay or hardpoint still counts. Each fitted module satisfies at most one requirement. A stock
/// requirement is satisfied by any module of the item, engineered or not. An engineered requirement is
/// satisfied by the same item with the same blueprint at the required grade or higher and, when the fit
/// names an experimental effect, the same effect. Slot names are compared case-insensitively, and items
/// through <see cref="ModuleSymbolNames.Canonical"/>, so a bundled <c>_free</c> module or a ship-specific
/// cargo hatch is the item the fit names.
/// </remarks>
public static class FitComparer
{
    /// <summary>Lays a fit against a ship's modules.</summary>
    /// <param name="fitting">The fit.</param>
    /// <param name="fitted">The ship's modules, one per slot. Must not contain two modules in one slot.</param>
    /// <returns>The comparison. Slots the ship fills that the fit does not mention are ignored.</returns>
    public static FitComparison Compare(Fitting fitting, IReadOnlyList<FitModule> fitted)
    {
        ArgumentNullException.ThrowIfNull(fitting);
        ArgumentNullException.ThrowIfNull(fitted);

        var bySlot = fitted.ToDictionary(module => module.Slot, StringComparer.OrdinalIgnoreCase);
        var claimed = new HashSet<FitModule>(ReferenceEqualityComparer.Instance);
        var found = new FitModule?[fitting.Modules.Count];

        // First the slot the fit names, so a module stays where the fit put it when it can.
        for (var index = 0; index < fitting.Modules.Count; index++)
        {
            var required = fitting.Modules[index];
            if (bySlot.GetValueOrDefault(required.Slot) is { } present && SameItem(required, present) && claimed.Add(present))
            {
                found[index] = present;
            }
        }

        // Then any other slot of the same kind, nearest in size first.
        for (var index = 0; index < fitting.Modules.Count; index++)
        {
            if (found[index] is not null)
            {
                continue;
            }

            var required = fitting.Modules[index];
            var place = SlotNames.Parse(required.Slot);
            var candidate = fitted
                .Where(module => !claimed.Contains(module) && SameItem(required, module))
                .Select(module => (Module: module, Place: SlotNames.Parse(module.Slot)))
                .Where(pair => pair.Place.Kind == place.Kind)
                .OrderBy(pair => Math.Abs(pair.Place.Size - place.Size))
                .Select(pair => pair.Module)
                .FirstOrDefault();
            if (candidate is not null && claimed.Add(candidate))
            {
                found[index] = candidate;
            }
        }

        var matching = new List<FitModule>();
        var differences = new List<FitDifference>();
        for (var index = 0; index < fitting.Modules.Count; index++)
        {
            var required = fitting.Modules[index];
            if (found[index] is { } module)
            {
                var kind = Classify(required.Engineering, module.Engineering);
                if (kind is null)
                {
                    matching.Add(required);
                }
                else
                {
                    differences.Add(new FitDifference(required, module, kind.Value));
                }

                continue;
            }

            var inSlot = bySlot.GetValueOrDefault(required.Slot);
            differences.Add(inSlot is null || claimed.Contains(inSlot)
                ? new FitDifference(required, null, DifferenceKind.SlotEmpty)
                : new FitDifference(required, inSlot, DifferenceKind.DifferentModule));
        }

        return new FitComparison(fitting, matching, differences);
    }

    private static bool SameItem(FitModule required, FitModule fitted) => string.Equals(ModuleSymbolNames.Canonical(required.Item), ModuleSymbolNames.Canonical(fitted.Item), StringComparison.Ordinal);

    private static DifferenceKind? Classify(Engineering? wanted, Engineering? has) => (wanted, has) switch
    {
        (null, _) => null,
        ({ }, null) => DifferenceKind.NotEngineered,
        ({ } required, { } present) => ClassifyEngineering(required, present),
    };

    private static DifferenceKind? ClassifyEngineering(Engineering wanted, Engineering has)
        => !string.Equals(has.Blueprint, wanted.Blueprint, StringComparison.OrdinalIgnoreCase)
            ? DifferenceKind.DifferentBlueprint
            : has.Level < wanted.Level
            ? DifferenceKind.LowerGrade
            : has.Level == wanted.Level && !has.IsComplete
            ? DifferenceKind.UnfinishedGrade
            : wanted.Experimental is null
            ? null
            : !has.ExperimentalKnown
            ? DifferenceKind.ExperimentalUnknown
            : string.Equals(has.Experimental, wanted.Experimental, StringComparison.OrdinalIgnoreCase) ? null : DifferenceKind.DifferentExperimental;
}
