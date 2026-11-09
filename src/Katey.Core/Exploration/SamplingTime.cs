namespace Katey.Core.Exploration;

/// <summary>
/// How long exobiology takes on the ground. Each species is three samples, and the distance between them does
/// not matter: a commander drives or reparks the ship, so a species costs about the same whatever its genus,
/// and the next species is usually in sight from the last. The figures are medians from a practised
/// commander's journal: the glide from orbit to the first touchdown and the climb back to supercruise, the
/// time from a species' first sample to its analysis, and the pause between one species and the next.
/// </summary>
public static class SamplingTime
{
    /// <summary>Minutes a landing costs before the first sample and after the last: about two and a half from orbit to the first touchdown, and under two from the last liftoff back to supercruise.</summary>
    public const double ApproachMinutes = 4;

    /// <summary>Minutes one species costs: from its first sample to its analysis, three touchdowns or a drive between.</summary>
    public const double SpeciesMinutes = 3;

    /// <summary>Minutes between one species' analysis and the next one's first sample: the look around, or the short hop to its patches.</summary>
    public const double BetweenSpeciesMinutes = 1;

    /// <summary>Minutes a landing takes for a number of species.</summary>
    /// <param name="species">How many species to sample; a negative count reads as none.</param>
    /// <returns><see cref="ApproachMinutes"/>, plus <see cref="SpeciesMinutes"/> per species, plus <see cref="BetweenSpeciesMinutes"/> for each after the first; the approach alone for none.</returns>
    public static double GroundMinutes(int species)
    {
        var count = Math.Max(species, 0);
        return ApproachMinutes + (count * SpeciesMinutes) + (Math.Max(count - 1, 0) * BetweenSpeciesMinutes);
    }

    /// <summary>Credits a landing pays per minute on the ground.</summary>
    /// <param name="credits">What the landing pays, bonus included where it applies.</param>
    /// <param name="groundMinutes">The minutes from <see cref="GroundMinutes"/>; anything under a minute counts as one.</param>
    /// <returns>Credits per minute, 0 for no credits.</returns>
    public static double CreditsPerMinute(long credits, double groundMinutes) => credits <= 0 ? 0 : credits / Math.Max(groundMinutes, 1);

    /// <summary>
    /// The best pace a landing can run: a commander on the ground samples the species worth the time and leaves
    /// the rest, so the species are taken richest first and the landing stops where adding the next would slow
    /// it down.
    /// </summary>
    /// <param name="species">What each species to choose from pays, bonus included.</param>
    /// <returns>Credits per minute of the best prefix, the approach counted; 0 with nothing worth sampling.</returns>
    public static double BestCreditsPerMinute(IEnumerable<long> species)
    {
        ArgumentNullException.ThrowIfNull(species);
        var best = 0.0;
        long credits = 0;
        var taken = 0;
        foreach (var pay in species.Where(credits => credits > 0).OrderByDescending(credits => credits))
        {
            credits += pay;
            taken++;
            best = Math.Max(best, CreditsPerMinute(credits, GroundMinutes(taken)));
        }

        return best;
    }

    /// <summary>Whether one species, sampled alone on a landing, keeps a pace.</summary>
    /// <param name="credits">What the species pays, bonus included.</param>
    /// <param name="creditsPerMinute">The pace to keep.</param>
    /// <returns><see langword="true"/> when the approach plus one species' minutes still pays the pace.</returns>
    public static bool KeepsPaceAlone(long credits, double creditsPerMinute) => CreditsPerMinute(credits, GroundMinutes(1)) >= creditsPerMinute;

    /// <summary>The chance that at least one of several independent chances comes off.</summary>
    /// <param name="chances">Each between 0 and 1.</param>
    /// <returns>1 minus the product of the misses; 0 for none.</returns>
    public static double ChanceOfAny(IEnumerable<double> chances)
    {
        ArgumentNullException.ThrowIfNull(chances);
        var miss = 1.0;
        foreach (var chance in chances)
        {
            miss *= 1 - Math.Clamp(chance, 0, 1);
        }

        return 1 - miss;
    }
}
