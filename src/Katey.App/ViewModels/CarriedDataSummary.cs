using Katey.Core.Exploration;
using Katey.Journal;

namespace Katey.App.ViewModels;

/// <summary>What the data aboard is worth: the cartographics estimate per body and the species table per sample.</summary>
/// <param name="Systems">Systems with unsold scans.</param>
/// <param name="Bodies">Bodies scanned and unsold.</param>
/// <param name="Mapped">Of those, bodies the player mapped.</param>
/// <param name="CartographicsCredits">Estimated cartographics payout, first-discovery and first-mapping bonuses included where the scan flags show them.</param>
/// <param name="Samples">Completed organic samples unsold.</param>
/// <param name="FirstLoggedSamples">Of those, samples nobody had logged on their body when taken, which the first-logged bonus is likely for.</param>
/// <param name="ExobiologyBaseCredits">The samples' base payout, credits.</param>
/// <param name="ExobiologyCredits">The samples' payout with the first-logged bonus on every sample it is likely for, credits.</param>
public sealed record CarriedDataSummary(int Systems, int Bodies, int Mapped, long CartographicsCredits, int Samples, int FirstLoggedSamples, long ExobiologyBaseCredits, long ExobiologyCredits)
{
    /// <summary>The game pays this many times the base for a species nobody has logged on that body.</summary>
    public const int FirstLogMultiplier = 5;

    /// <summary>Gets nothing aboard.</summary>
    public static CarriedDataSummary Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>Gets whether nothing is aboard.</summary>
    public bool IsEmpty => Bodies == 0 && Samples == 0;

    /// <summary>Gets the cartographics estimate plus the exobiology figure with its likely bonuses, credits.</summary>
    public long TotalCredits => CartographicsCredits + ExobiologyCredits;

    /// <summary>Values the data aboard.</summary>
    /// <param name="carried">What the journal says is aboard.</param>
    /// <param name="predictor">The species table, for each sample's base payout. A species it lacks counts as 0.</param>
    /// <returns>The summary.</returns>
    public static CarriedDataSummary Of(CarriedData carried, ExobiologyPredictor predictor)
    {
        ArgumentNullException.ThrowIfNull(carried);
        ArgumentNullException.ThrowIfNull(predictor);
        long cartographics = 0;
        foreach (var body in carried.Systems.SelectMany(system => system.Bodies))
        {
            cartographics += body.IsStar
                ? BodyValue.Star(body.Class, body.Mass ?? 1, !body.WasDiscovered)
                : BodyValue.Planet(body.Class, body.Terraformable, body.Mass ?? 1, !body.WasDiscovered, body.Mapped, !body.WasMapped);
        }

        long baseCredits = 0;
        long withBonus = 0;
        var firstLogged = 0;
        foreach (var sample in carried.Samples)
        {
            var reward = predictor.Reward(sample.Species) ?? 0;
            baseCredits += reward;
            withBonus += sample.WasLogged ? reward : reward * FirstLogMultiplier;
            firstLogged += sample.WasLogged ? 0 : 1;
        }

        return new CarriedDataSummary(carried.Systems.Count, carried.BodyCount, carried.MappedCount, cartographics, carried.Samples.Count, firstLogged, baseCredits, withBonus);
    }
}
