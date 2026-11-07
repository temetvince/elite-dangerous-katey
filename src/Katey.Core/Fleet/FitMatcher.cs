namespace Katey.Core.Fleet;

/// <summary>Engineering a fit requires, or a module carries.</summary>
/// <param name="Blueprint">Blueprint journal name such as <c>FSD_LongRange</c>.</param>
/// <param name="Level">Grade 1–5.</param>
/// <param name="Experimental">Experimental effect journal name, or <see langword="null"/> for none.</param>
/// <param name="ExperimentalKnown">Whether <paramref name="Experimental"/> is known. Station storage reports blueprint and grade but not the effect.</param>
/// <param name="Quality">Progress within <paramref name="Level"/>, 0 to 1, as the game reports it; 1 when the grade is complete or the figure is unknown. A module engineered before the game kept the figure reads as complete.</param>
public sealed record Engineering(string Blueprint, int Level, string? Experimental, bool ExperimentalKnown = true, double Quality = 1)
{
    /// <summary>Gets whether the grade is rolled to its full effect: a quality of 1, or no figure at all.</summary>
    public bool IsComplete => Quality is >= 1 or <= 0;

    /// <summary>Gets the progress within the grade as a whole percentage, 100 when complete.</summary>
    public int Percent => IsComplete ? 100 : (int)Math.Round(Quality * 100);
}

/// <summary>One module a fit calls for.</summary>
/// <param name="Slot">Slot name such as <c>FrameShiftDrive</c>.</param>
/// <param name="Item">Item symbol, compared case-insensitively.</param>
/// <param name="Engineering">Required engineering, or <see langword="null"/> for stock.</param>
public sealed record FitModule(string Slot, string Item, Engineering? Engineering);

/// <summary>A saved fit for one hull.</summary>
/// <param name="Id">Unique among saved fits.</param>
/// <param name="Hull">Ship type symbol, compared case-insensitively, such as <c>anaconda</c>.</param>
/// <param name="Name">The fit's name.</param>
/// <param name="Modules">Every module slot the fit fills, cargo hatch included.</param>
/// <param name="LoadoutJson">The export text the fit was pasted from, exactly as given, so it can be handed back to EDSY unchanged and its masses read; <see langword="null"/> on a fit saved without it.</param>
public sealed record Fitting(string Id, string Hull, string Name, IReadOnlyList<FitModule> Modules, string? LoadoutJson = null);

/// <summary>Where an owned module is.</summary>
public enum ModulePlace
{
    /// <summary>In station storage.</summary>
    Stored,

    /// <summary>Fitted to a ship.</summary>
    Installed,
}

/// <summary>One module the player owns.</summary>
/// <param name="Id">Unique among owned modules.</param>
/// <param name="Item">Item symbol.</param>
/// <param name="Engineering">Its engineering, or <see langword="null"/> for stock.</param>
/// <param name="Place">Stored or installed.</param>
/// <param name="System">Star system, or <see langword="null"/> when unknown or in transit.</param>
/// <param name="Holder">Station name or ship name, for display.</param>
/// <param name="Hull">The hull it is installed in, or <see langword="null"/> when stored.</param>
/// <param name="IsPrebuilt">Whether it came with a ship redeemed from a bundle and is still in the slot it came in. Such a module cannot leave its ship, so it counts only for fits of that ship's hull.</param>
public sealed record OwnedModule(string Id, string Item, Engineering? Engineering, ModulePlace Place, string? System, string Holder, string? Hull, bool IsPrebuilt = false)
{
    /// <summary>Gets whether the module carries any engineering.</summary>
    public bool IsEngineered => Engineering is not null;
}

/// <summary>
/// How owned modules may be reused across fits. Within one fit a module is always used once; the policy
/// decides whether a module one fit uses is still available to another.
/// </summary>
public enum SharingPolicy
{
    /// <summary>Modules move between ships as needed: every fit may use every owned module.</summary>
    Pooled,

    /// <summary>Modules stay with a hull type: fits for the same hull may reuse a module, fits for a different hull may not.</summary>
    SameHull,

    /// <summary>Every fit needs its own copy of every module.</summary>
    None,
}

/// <summary>How well an owned module satisfies a required one.</summary>
public enum MatchQuality
{
    /// <summary>Nothing suitable is owned.</summary>
    Missing,

    /// <summary>The item matches and carries the right blueprint, but below the required grade, or at the grade without the required experimental effect.</summary>
    Partial,

    /// <summary>The item matches and the engineering is at least what is required, but the module's experimental effect is unknown.</summary>
    ExperimentalUnknown,

    /// <summary>The item and engineering match.</summary>
    Exact,
}

/// <summary>One required module and what was found for it.</summary>
/// <param name="Required">The requirement.</param>
/// <param name="Found">The owned module assigned, or <see langword="null"/>.</param>
/// <param name="Quality">How well it fits.</param>
public sealed record FitLine(FitModule Required, OwnedModule? Found, MatchQuality Quality)
{
    /// <summary>Gets whether the module found lacks only the experimental effect: it carries the blueprint at the required grade or higher.</summary>
    public bool NeedsOnlyExperimental => Quality == MatchQuality.Partial && Found?.Engineering is { } have && Required.Engineering is { } want && have.Level >= want.Level && (have.Level > want.Level || have.IsComplete);
}

/// <summary>One fit's result.</summary>
/// <param name="Fitting">The fit.</param>
/// <param name="HullOwned">Whether the player owns a ship of this hull. When false, no modules are assigned.</param>
/// <param name="Lines">One per required module, in fit order.</param>
public sealed record FitReport(Fitting Fitting, bool HullOwned, IReadOnlyList<FitLine> Lines)
{
    /// <summary>Gets how many required modules are missing.</summary>
    public int MissingCount => Lines.Count(line => line.Quality == MatchQuality.Missing);

    /// <summary>Gets how many required modules are owned but not engineered all the way.</summary>
    public int PartialCount => Lines.Count(line => line.Quality == MatchQuality.Partial);

    /// <summary>Gets how many assignments rest on an unknown experimental effect.</summary>
    public int UncertainCount => Lines.Count(line => line.Quality == MatchQuality.ExperimentalUnknown);
}

/// <summary>The whole comparison.</summary>
/// <param name="Fits">One report per fit, in the order given.</param>
/// <param name="Unused">Owned modules no fit claimed, within the location filter.</param>
public sealed record MatchReport(IReadOnlyList<FitReport> Fits, IReadOnlyList<OwnedModule> Unused);

/// <summary>Compares saved fits against the modules the player owns.</summary>
public static class FitMatcher
{
    /// <summary>Works out what each fit still needs and what is left over.</summary>
    /// <param name="fittings">The fits to check, in priority order. Earlier fits claim modules first.</param>
    /// <param name="owned">Every owned module.</param>
    /// <param name="ownedHulls">Ship type symbols the player owns, compared case-insensitively.</param>
    /// <param name="policy">How modules may be shared between fits.</param>
    /// <param name="system">Only consider modules in this system, or <see langword="null"/> for everywhere. Compared case-insensitively.</param>
    /// <param name="farness">
    /// How far a module is from the player, lower nearer: light years when known, 0 for one in the player's own
    /// system, <see cref="double.PositiveInfinity"/> for one whose place is unknown. Among modules in the same
    /// place the nearer is taken. <see langword="null"/> when distance should not count.
    /// </param>
    /// <returns>The report.</returns>
    public static MatchReport Match(
        IReadOnlyList<Fitting> fittings,
        IReadOnlyList<OwnedModule> owned,
        IReadOnlySet<string> ownedHulls,
        SharingPolicy policy,
        string? system,
        Func<OwnedModule, double>? farness = null)
    {
        ArgumentNullException.ThrowIfNull(fittings);
        ArgumentNullException.ThrowIfNull(owned);
        ArgumentNullException.ThrowIfNull(ownedHulls);

        var hulls = new HashSet<string>(ownedHulls, StringComparer.OrdinalIgnoreCase);
        var available = owned
            .Where(module => system is null || string.Equals(module.System, system, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var claimedBy = new Dictionary<string, string>(StringComparer.Ordinal);
        var reports = new List<FitReport>();

        foreach (var fitting in fittings)
        {
            if (!hulls.Contains(fitting.Hull))
            {
                reports.Add(new FitReport(fitting, HullOwned: false, [.. fitting.Modules.Select(module => new FitLine(module, null, MatchQuality.Missing))]));
                continue;
            }

            var usedInFit = new HashSet<string>(StringComparer.Ordinal);
            var lines = new List<FitLine>();
            foreach (var required in fitting.Modules)
            {
                var (found, quality) = Pick(required, fitting.Hull, available, module => usedInFit.Contains(module.Id) || IsReserved(policy, claimedBy, module.Id, fitting.Hull), farness);
                if (found is not null)
                {
                    usedInFit.Add(found.Id);
                    claimedBy.TryAdd(found.Id, fitting.Hull);
                }

                lines.Add(new FitLine(required, found, quality));
            }

            reports.Add(new FitReport(fitting, HullOwned: true, lines));
        }

        return new MatchReport(reports, [.. available.Where(module => !claimedBy.ContainsKey(module.Id))]);
    }

    /// <summary>The best owned module for one requirement, with no fit or policy in play.</summary>
    /// <param name="required">The requirement.</param>
    /// <param name="candidates">Owned modules to consider.</param>
    /// <param name="farness">How far a module is from the player, as for <see cref="Match"/>, or <see langword="null"/> when distance should not count.</param>
    /// <returns>The line: the best candidate and how well it fits, or <see cref="MatchQuality.Missing"/> with nothing found. Among equal matches a stored module beats one fitted to a ship, then the nearer wins, then the one that wastes least engineering.</returns>
    public static FitLine Find(FitModule required, IEnumerable<OwnedModule> candidates, Func<OwnedModule, double>? farness = null)
    {
        ArgumentNullException.ThrowIfNull(required);
        ArgumentNullException.ThrowIfNull(candidates);
        var (found, quality) = Pick(required, null, [.. candidates], _ => false, farness);
        return new FitLine(required, found, quality);
    }

    /// <summary>Whether an earlier fit's claim on a module keeps this fit from using it.</summary>
    private static bool IsReserved(SharingPolicy policy, Dictionary<string, string> claimedBy, string moduleId, string hull)
    {
        var claimed = claimedBy.TryGetValue(moduleId, out var claimingHull);
        return claimed && Reserves(policy, claimingHull, hull);
    }

    private static bool Reserves(SharingPolicy policy, string? claimingHull, string hull) => policy switch
    {
        SharingPolicy.Pooled => false,
        SharingPolicy.SameHull => !string.Equals(claimingHull, hull, StringComparison.OrdinalIgnoreCase),
        SharingPolicy.None => true,
        _ => true,
    };

    /// <summary>
    /// The best available module for one requirement: the highest match quality first, and among equal
    /// quality the module that is easiest to use for this fit, then the nearest, then the one that wastes
    /// least engineering.
    /// </summary>
    /// <param name="required">The requirement.</param>
    /// <param name="hull">The fit's hull, so a module already fitted to a ship of that hull comes first, or <see langword="null"/> with no fit in play.</param>
    /// <param name="available">The owned modules to consider.</param>
    /// <param name="unavailable">Whether a module is already spoken for.</param>
    /// <param name="farness">How far a module is, lower nearer, or <see langword="null"/> when distance should not count.</param>
    private static (OwnedModule? Found, MatchQuality Quality) Pick(FitModule required, string? hull, List<OwnedModule> available, Func<OwnedModule, bool> unavailable, Func<OwnedModule, double>? farness)
    {
        OwnedModule? best = null;
        var bestQuality = MatchQuality.Missing;
        var bestScore = (Place: int.MaxValue, Farness: double.PositiveInfinity, Engineering: int.MaxValue);
        foreach (var candidate in available)
        {
            if (unavailable(candidate) || !string.Equals(ModuleSymbolNames.Canonical(candidate.Item), ModuleSymbolNames.Canonical(required.Item), StringComparison.Ordinal))
            {
                continue;
            }

            // A prebuilt module cannot leave its ship: it serves a fit for that ship's hull and no other, and no requirement with no hull in play.
            if (candidate.IsPrebuilt && (hull is null || !string.Equals(candidate.Hull, hull, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var quality = Quality(required.Engineering, candidate.Engineering);
            if (quality == MatchQuality.Missing)
            {
                continue;
            }

            var score = (PlaceScore(candidate, hull), farness?.Invoke(candidate) ?? 0, Score(required.Engineering, candidate.Engineering));
            if (quality > bestQuality || (quality == bestQuality && score.CompareTo(bestScore) < 0))
            {
                best = candidate;
                bestQuality = quality;
                bestScore = score;
            }
        }

        return (best, bestQuality);
    }

    /// <summary>Lower is nearer to hand: already fitted to a ship of the fit's hull, then in storage, then fitted to a ship of another hull, which would have to give it up.</summary>
    private static int PlaceScore(OwnedModule candidate, string? hull) => candidate.Place switch
    {
        ModulePlace.Installed when hull is not null && string.Equals(candidate.Hull, hull, StringComparison.OrdinalIgnoreCase) => 0,
        ModulePlace.Stored => 1,
        _ => 2,
    };

    /// <summary>
    /// A stock requirement accepts any module of the item. An engineered requirement needs the same
    /// blueprint at the required grade or higher, the grade rolled to its full effect when it is the
    /// required one, and the same experimental effect when one is required.
    /// A module with the right blueprint below the grade, or at the grade with the wrong or no experimental,
    /// is a partial match: it is owned, and finishing its engineering closes the gap.
    /// </summary>
    private static MatchQuality Quality(Engineering? required, Engineering? candidate)
    {
        return required is null
            ? MatchQuality.Exact
            : candidate is null || !string.Equals(candidate.Blueprint, required.Blueprint, StringComparison.OrdinalIgnoreCase)
            ? MatchQuality.Missing
            : candidate.Level < required.Level || (candidate.Level == required.Level && !candidate.IsComplete)
            ? MatchQuality.Partial
            : required.Experimental is null
            ? MatchQuality.Exact
            : !candidate.ExperimentalKnown
            ? MatchQuality.ExperimentalUnknown
            : string.Equals(candidate.Experimental, required.Experimental, StringComparison.OrdinalIgnoreCase)
            ? MatchQuality.Exact
            : MatchQuality.Partial;
    }

    /// <summary>Lower is a better use of the module: do not spend an engineered module on a stock slot, nor a G5 on a G3 need, and finish the nearest grade.</summary>
    private static int Score(Engineering? required, Engineering? candidate) => candidate is null ? 0 : required is null ? 100 + candidate.Level : Math.Abs(candidate.Level - required.Level);
}
