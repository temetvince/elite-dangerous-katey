using System.Globalization;
using Katey.Core;
using Katey.Core.Exploration;
using Katey.Journal;

namespace Katey.App.ViewModels;

/// <summary>One genus found on a body and what it is likely worth.</summary>
/// <param name="Genus">Genus symbol.</param>
/// <param name="Name">Genus display name.</param>
/// <param name="Candidates">Likely species as text, most likely first, such as "Bacterium Aurasus 62%".</param>
/// <param name="ExpectedCredits">Probability-weighted payout, or 0 when unknown.</param>
/// <param name="Confirmed">The species already sampled here, or empty.</param>
/// <param name="TopSpecies">The sampled species, else the likeliest, without the genus word: "Tectonicas" for Stratum Tectonicas. Empty with no prediction.</param>
/// <param name="TopProbability">The chance of <paramref name="TopSpecies"/>, 0 to 1; 1 once sampled.</param>
/// <param name="Stage">Samples taken of the species so far, 1 to 3, or 0 before the first.</param>
public sealed record GenusRow(string Genus, string Name, string Candidates, long ExpectedCredits, string Confirmed, string TopSpecies = "", double TopProbability = 0, int Stage = 0)
{
    /// <summary>A species name without its genus word, since the genus is named beside it.</summary>
    /// <param name="species">The species name as the game spells it, such as "Bacterium Vesicula".</param>
    /// <param name="genus">The genus name, such as "Bacterium".</param>
    /// <returns>The name after the genus word, or the whole name when it does not start with it.</returns>
    public static string ShortSpecies(string species, string genus)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(genus);
        return genus.Length > 0 && species.StartsWith(genus + " ", StringComparison.OrdinalIgnoreCase) ? species[(genus.Length + 1)..] : species;
    }

    /// <summary>The bacteria genus, the one most bodies with life have and the least valuable.</summary>
    public const string BacteriaGenus = "$Codex_Ent_Bacterial_Genus_Name;";

    /// <summary>Gets the expected payout as text.</summary>
    public string ExpectedText => ExpectedCredits > 0 ? Display.Credits(ExpectedCredits) : "value unknown";

    /// <summary>Gets whether this is the bacteria genus.</summary>
    public bool IsBacteria => string.Equals(Genus, BacteriaGenus, StringComparison.OrdinalIgnoreCase);
}

/// <summary>One run of a sentence: plain text, or a figure to be coloured.</summary>
/// <param name="Text">The run's text, spaces included.</param>
/// <param name="IsFigure">Whether the run is a figure, drawn in the figure colour.</param>
public sealed record TextRun(string Text, bool IsFigure);

/// <summary>One body in the current system, ready to print.</summary>
/// <param name="Name">Short body name, without the system name.</param>
/// <param name="IsStar">Whether it is a star. Stars cannot be mapped, so a star carries no mapping note.</param>
/// <param name="IsScoopable">Whether it is a star the drive can refuel from.</param>
/// <param name="Class">For a star, its class in words with whether it can be scooped; otherwise the planet class, or "Belt cluster".</param>
/// <param name="DistanceLs">Distance from arrival in light seconds.</param>
/// <param name="GravityG">Surface gravity in g, or <see langword="null"/> for a star or an unknown.</param>
/// <param name="Surface">Temperature and notes, or solar masses for a star.</param>
/// <param name="Atmosphere">Atmosphere, or "no atmosphere", or empty for a star.</param>
/// <param name="Volcanism">Volcanism, or empty.</param>
/// <param name="Landable">Whether a ship can land.</param>
/// <param name="FirstDiscovery">Whether nobody had discovered it before.</param>
/// <param name="FirstMapping">Whether nobody had mapped it before.</param>
/// <param name="FirstFootfall">Whether nobody had set foot on it before, as far as the journal can say: false in a populated system, where footfall cannot be claimed.</param>
/// <param name="MappedByYou">Whether the player mapped it this visit.</param>
/// <param name="LandedByYou">Whether the player has ever landed here.</param>
/// <param name="Signals">Signal counts as text, or empty.</param>
/// <param name="BiologicalSignals">Number of biological signals.</param>
/// <param name="Genera">Genera found, with predictions, the most valuable first.</param>
/// <param name="GroundMinutes">Minutes a landing here is estimated to take: the approach plus every species to sample, by the scanner's genera once mapped or the genera the conditions allow before that. 0 for a body without life.</param>
/// <param name="PaceKnown">Whether <paramref name="GroundMinutes"/> rests on anything: false when the conditions give no prediction, so the unknown is never hidden as too slow.</param>
/// <param name="PaceCreditsPerMinute">What the best landing here pays a minute on the ground, bonus included: on a probed body the species worth the time and no others, on an unprobed one the odds-weighted whole.</param>
/// <param name="PaceChance">The chance that at least one species here is worth the stop on its own at the min/max pace: by the scanner's genera and their likely species once probed, by the odds of each signal before that.</param>
/// <param name="BacteriaOnlyByPrediction">Whether, with signals but no genus list yet, bacteria is the only genus ever found under the body's conditions.</param>
/// <param name="Materials">Surface materials, highest share first, as text, or empty.</param>
/// <param name="MaterialNames">Display names of every surface material.</param>
/// <param name="ScanValue">Cartographics value as text.</param>
/// <param name="ScanCredits">Cartographics value as scanned, credits.</param>
/// <param name="ExobiologyCredits">Expected exobiology payout from the genera the scanner has named, credits, or 0 before it has.</param>
/// <param name="PredictedExobiologyCredits">
/// Before the scanner names the genera: the expected payout of one signal, each genus that can live under
/// the body's conditions weighted by how likely it is to be there, times the number of biological signals,
/// credits. 0 once the genera are named, when there are no signals, or when nothing is known to live under
/// such conditions.
/// </param>
/// <param name="PossibleLife">
/// Before the scanner names the genera: how many genera could live under these conditions and the three
/// likeliest, each with its odds, its likeliest species and its expected payout before any bonus, or empty.
/// </param>
/// <param name="PossibleGenera">Before the scanner names the genera: how many genera could live under these conditions, or 0.</param>
/// <param name="TopMaterialGrade">The highest grade among the surface materials, 1 to 5, or 0 with none.</param>
/// <param name="EarlierVisit">The day the body was scanned when that was an earlier visit than this one, such as "scanned 3 Nov", or empty.</param>
/// <param name="FromCatalogue">Whether the body comes from a catalogue rather than the player's own scan.</param>
public sealed record BodyRow(
    string Name,
    bool IsStar,
    bool IsScoopable,
    string Class,
    double DistanceLs,
    double? GravityG,
    string Surface,
    string Atmosphere,
    string Volcanism,
    bool Landable,
    bool FirstDiscovery,
    bool FirstMapping,
    bool FirstFootfall,
    bool MappedByYou,
    bool LandedByYou,
    string Signals,
    int BiologicalSignals,
    IReadOnlyList<GenusRow> Genera,
    bool BacteriaOnlyByPrediction,
    double GroundMinutes,
    bool PaceKnown,
    double PaceCreditsPerMinute,
    double PaceChance,
    string Materials,
    IReadOnlyList<string> MaterialNames,
    string ScanValue,
    long ScanCredits,
    long ExobiologyCredits,
    long PredictedExobiologyCredits,
    string PossibleLife,
    int PossibleGenera,
    int TopMaterialGrade,
    string EarlierVisit,
    bool FromCatalogue)
{
    /// <summary>Gets whether <see cref="EarlierVisit"/> has anything to say.</summary>
    public bool IsEarlierVisit => EarlierVisit.Length > 0;

    /// <summary>Gets the distance from arrival as text.</summary>
    public string Distance => string.Create(CultureInfo.InvariantCulture, $"{DistanceLs:0} ls");

    /// <summary>Gets whether it is a star the drive cannot refuel from, which deserves a warning of its own.</summary>
    public bool IsUnscoopableStar => IsStar && !IsScoopable;

    /// <summary>Gets the surface gravity as text, such as "1.60 g", or empty when unknown.</summary>
    public string GravityText => GravityG is { } g ? string.Create(CultureInfo.InvariantCulture, $"{g:0.00} g") : "";

    /// <summary>Gets the landing band of the gravity.</summary>
    public GravityBand Gravity => SurfaceGravity.Band(GravityG);

    /// <summary>Gets whether the gravity is high but not extreme: worth a careful approach.</summary>
    public bool IsHighGravity => Gravity == GravityBand.High;

    /// <summary>Gets whether the gravity is extreme: a heavy ship may not arrest its descent.</summary>
    public bool IsExtremeGravity => Gravity == GravityBand.Extreme;

    /// <summary>Gets the gravity warning, "high gravity" or "extreme gravity", or empty when the landing is ordinary.</summary>
    public string GravityWarning => Gravity switch
    {
        GravityBand.Extreme => "extreme gravity",
        GravityBand.High => "high gravity",
        GravityBand.Light => "",
        _ => "",
    };

    /// <summary>Gets whether nobody has mapped this body, which a star cannot be, and you have not either: a first mapping is open.</summary>
    public bool FirstMappingOpen => !IsStar && FirstMapping && !MappedByYou;

    /// <summary>Gets whether you were the first to map it.</summary>
    public bool MappedByYouFirst => !IsStar && FirstMapping && MappedByYou;

    /// <summary>Gets the discovery slot: "undiscovered" when nobody had discovered it before, else "discovered".</summary>
    public string DiscoveryStatus => FirstDiscovery ? "undiscovered" : "discovered";

    /// <summary>Gets whether the discovery is still open: nobody had discovered it before you.</summary>
    public bool DiscoveryOpen => FirstDiscovery;

    /// <summary>Gets the mapping slot: "unmapped" or "mapped"; empty for a star, which cannot be mapped, and "mapping unknown" for a body a catalogue described.</summary>
    public string MappingStatus => IsStar ? "" : FromCatalogue ? "mapping unknown" : FirstMappingOpen ? "unmapped" : "mapped";

    /// <summary>Gets whether the mapping is still open: nobody has mapped it, you included.</summary>
    public bool MappingOpen => FirstMappingOpen;

    /// <summary>Gets the footfall slot for a landable body: "no footfall" or "footfall"; empty where nobody can land, and "footfall unknown" for a body a catalogue described.</summary>
    public string FootfallStatus => !Landable ? "" : FromCatalogue ? "footfall unknown" : FootfallOpen ? "no footfall" : "footfall";

    /// <summary>Gets whether the footfall is still open: nobody has set foot on it, you included.</summary>
    public bool FootfallOpen => Landable && !FromCatalogue && FirstFootfall && !LandedByYou;

    /// <summary>Gets the quiet line on your own part in the statuses, such as "Mapped first by you, landed by you.", or empty.</summary>
    public string StatusDetail
    {
        get
        {
            var parts = new List<string>();
            if (FirstDiscovery)
            {
                parts.Add("first discovered by you");
            }

            if (MappedByYouFirst)
            {
                parts.Add("mapped first by you");
            }
            else if (!IsStar && MappedByYou)
            {
                parts.Add("mapped by you");
            }

            if (LandedByYou)
            {
                parts.Add(FirstFootfall ? "first footfall by you" : "landed by you");
            }

            if (parts.Count == 0)
            {
                return "";
            }

            var line = string.Join(", ", parts);
            return char.ToUpperInvariant(line[0]) + line[1..] + ".";
        }
    }

    /// <summary>Gets whether any genus is listed.</summary>
    public bool HasGenera => Genera.Count > 0;

    /// <summary>Gets whether the body has biological signals.</summary>
    public bool HasBiology => BiologicalSignals > 0;

    /// <summary>Gets whether the body has surface materials.</summary>
    public bool HasMaterials => MaterialNames.Count > 0;

    /// <summary>Whether the body carries any of the given materials, by display name.</summary>
    /// <param name="names">Material display names, such as the ones the plan is short of.</param>
    /// <returns><see langword="true"/> when at least one is on the surface.</returns>
    public bool HasShortMaterial(IReadOnlySet<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        return MaterialNames.Any(names.Contains);
    }

    /// <summary>The game pays this many times the base value for a species nobody has logged on that body before.</summary>
    public const int FirstLogMultiplier = 5;

    /// <summary>
    /// Gets whether the first-logged bonus is likely: nobody has set foot on the body, so nobody can have
    /// logged its life, which is done on foot. Someone can still land and sell before you, so it is a likelihood.
    /// </summary>
    public bool FirstLogLikely => !IsStar && FirstFootfall;

    /// <summary>Gets the base exobiology figure: the scanner's genera when named, else the prediction from the body's conditions.</summary>
    public long ExobiologyBaseCredits => ExobiologyCredits > 0 ? ExobiologyCredits : PredictedExobiologyCredits;

    /// <summary>Gets the exobiology figure to rank and show: the base, times <see cref="FirstLogMultiplier"/> when <see cref="FirstLogLikely"/>.</summary>
    public long ExobiologyRank => ExobiologyBaseCredits * (FirstLogLikely ? FirstLogMultiplier : 1);

    /// <summary>Gets whether the exobiology figure is a prediction from conditions rather than from the scanner's genera.</summary>
    public bool IsExobiologyPredicted => ExobiologyCredits == 0 && PredictedExobiologyCredits > 0;

    /// <summary>Gets what the best landing here pays per minute on the ground, bonus included where it applies; 0 without life.</summary>
    public double ExobiologyCreditsPerMinute => PaceCreditsPerMinute;

    /// <summary>
    /// Gets whether the body stays under min/max. Before the probes the question is whether it is worth flying over
    /// to probe: it stays while the chance of a species worth the stop on its own is <see cref="BodyRowBuilder.MinMaxChance"/>
    /// or better. Once probed the question is whether it is worth landing: the best landing must keep
    /// <see cref="BodyRowBuilder.MinMaxCreditsPerMinute"/>. A body whose pace cannot be judged passes.
    /// </summary>
    public bool KeepsMinMaxPace => !PaceKnown || (IsProbed ? PaceCreditsPerMinute >= BodyRowBuilder.MinMaxCreditsPerMinute : PaceChance >= BodyRowBuilder.MinMaxChance);

    /// <summary>Gets whether the scanner has named the body's genera: the probes have been fired.</summary>
    public bool IsProbed => Genera.Count > 0;

    /// <summary>Gets the biological signal count as a short badge, such as "3 bio", or empty with none.</summary>
    public string BiologyCountText => BiologicalSignals > 0 ? string.Create(CultureInfo.InvariantCulture, $"{BiologicalSignals} bio") : "";

    /// <summary>
    /// Gets the expected exobiology payout as a sentence in runs, the figures marked: the scanner's figure, or the
    /// prediction with how it was made, then the first-logged figure when the bonus is likely. Empty with no life.
    /// </summary>
    public IReadOnlyList<TextRun> ExobiologyRuns
    {
        get
        {
            if (ExobiologyBaseCredits == 0)
            {
                return [];
            }

            var runs = new List<TextRun>();
            if (ExobiologyCredits > 0)
            {
                runs.Add(new("Exobiology about ", false));
                runs.Add(new(Display.Credits(ExobiologyCredits), true));
            }
            else
            {
                runs.Add(new("Exobiology likely about ", false));
                runs.Add(new(Display.Credits(PredictedExobiologyCredits), true));
                runs.Add(new(" by the life these conditions allow", false));
            }

            if (FirstLogLikely)
            {
                runs.Add(new(", ", false));
                runs.Add(new(Display.Credits(ExobiologyRank), true));
                runs.Add(new(" with the first-logged bonus", false));
            }

            runs.Add(new(ExobiologyCredits > 0 ? "." : "; map it to know.", false));
            return runs;
        }
    }

    /// <summary>Gets <see cref="ExobiologyRuns"/> as one plain sentence, or empty.</summary>
    public string ExobiologyText => string.Concat(ExobiologyRuns.Select(run => run.Text));

    /// <summary>
    /// Gets whether the body's life is bacteria alone: the surface scanner has reported its genera and every
    /// one is the bacteria genus, or, before the scanner has, the body has one signal and bacteria is either
    /// the only genus ever found under its conditions or at least <see cref="BodyRowBuilder.BacteriaOnlyLikelihood"/>
    /// likely to be what is there. Two or more signals are two or more genera, never bacteria alone.
    /// </summary>
    public bool IsBacteriaOnly => Genera.Count > 0 ? Genera.All(genus => genus.IsBacteria) : IsBacteriaOnlyByPrediction;

    /// <summary>Gets whether <see cref="IsBacteriaOnly"/> rests on prediction rather than the scanner's genus list.</summary>
    public bool IsBacteriaOnlyByPrediction => Genera.Count == 0 && BiologicalSignals > 0 && BacteriaOnlyByPrediction;
}

/// <summary>Builds <see cref="BodyRow"/>s from the exploration tracker's snapshot.</summary>
public sealed class BodyRowBuilder
{
    private readonly Catalog _catalog;
    private readonly ExobiologyPredictor _predictor;

    /// <summary>Creates the builder.</summary>
    /// <param name="catalog">The catalog, for material names.</param>
    /// <param name="predictor">The species predictor.</param>
    public BodyRowBuilder(Catalog catalog, ExobiologyPredictor predictor)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(predictor);
        _catalog = catalog;
        _predictor = predictor;
    }

    /// <summary>Cartographics value of a body as scanned and mapped so far.</summary>
    /// <param name="body">The body.</param>
    /// <returns>Credits.</returns>
    public static long ScanCredits(ScannedBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.FromNavBeacon || body.FromCatalogue)
        {
            // Data the game gave away at a beacon, or that a catalogue holds, is not the player's to sell.
            return 0;
        }

        return body.IsStar
            ? BodyValue.Star(body.Class, body.MassEarths ?? 1, !body.WasDiscovered)
            : BodyValue.Planet(body.Class, body.Terraformable, body.MassEarths ?? 1, !body.WasDiscovered, body.MappedByYou, !body.WasMapped);
    }

    /// <summary>A body's short name: the full name without the system's.</summary>
    /// <param name="bodyName">Full body name.</param>
    /// <param name="systemName">System name, or <see langword="null"/>.</param>
    /// <returns>The short name.</returns>
    public static string ShortName(string bodyName, string? systemName)
    {
        ArgumentNullException.ThrowIfNull(bodyName);
        return systemName is not null && bodyName.StartsWith(systemName + " ", StringComparison.OrdinalIgnoreCase)
            ? bodyName[(systemName.Length + 1)..]
            : bodyName;
    }

    /// <summary>
    /// Every scanned body, star by star: each star in order of distance from arrival, followed by the bodies
    /// that orbit it, nearest first. Bodies orbiting no scanned star, such as those around the barycentre of
    /// a pair, come last by distance.
    /// </summary>
    /// <param name="snapshot">The system.</param>
    /// <returns>Rows in display order.</returns>
    public IReadOnlyList<BodyRow> Build(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var stars = snapshot.Bodies.Where(body => body.IsStar).OrderBy(body => body.DistanceLs).ToList();
        var starIds = stars.Select(star => star.BodyId).ToHashSet();
        var ordered = new List<ScannedBody>();
        foreach (var star in stars)
        {
            ordered.Add(star);
            ordered.AddRange(snapshot.Bodies.Where(body => !body.IsStar && body.ParentStarId == star.BodyId).OrderBy(body => body.DistanceLs));
        }

        ordered.AddRange(snapshot.Bodies.Where(body => !body.IsStar && (body.ParentStarId is not { } parent || !starIds.Contains(parent))).OrderBy(body => body.DistanceLs));
        return [.. ordered.Select(body => Row(body, snapshot))];
    }

    /// <summary>How likely bacteria must be, 0 to 1, for a single unmapped signal to count as bacteria alone.</summary>
    public const double BacteriaOnlyLikelihood = 0.8;

    /// <summary>
    /// The pace, in credits per minute on the ground with the bonus where it applies, from which a body is worth
    /// landing on under the min/max choice: a lone Stratum Tectonicas with the first-logged bonus runs about
    /// fourteen million a minute, a Clypeus about twelve, an Aleoida Gravis, an Osseus Discus or a Tubus Cavas
    /// alone about nine, a body of eight cheap species about six, a lone bacterium about one.
    /// </summary>
    public const double MinMaxCreditsPerMinute = 8_000_000;

    /// <summary>The chance of a species worth the stop on its own from which an unprobed body stays under min/max: one in four, so many signals are not thrown away on an average.</summary>
    public const double MinMaxChance = 0.25;

    /// <summary>"Stratum (likely Tectonicas) 20%, 9.4M Cr expected": a genus named by its likeliest species, split at the species name's first space, with its odds.</summary>
    private static string GenusLine(string species, double probability, long expected)
    {
        var space = species.IndexOf(' ', StringComparison.Ordinal);
        var genus = space > 0 ? species[..space] : species;
        var rest = space > 0 ? species[(space + 1)..] : "";
        var odds = string.Create(CultureInfo.InvariantCulture, $"{probability * 100:0}%");
        return rest.Length > 0
            ? $"{genus} (likely {rest}) {odds}, {Display.Credits(expected)} expected"
            : $"{genus} {odds}, {Display.Credits(expected)} expected";
    }

    private BodyRow Row(ScannedBody body, SystemSnapshot snapshot)
    {
        var genera = new List<GenusRow>();
        long bio = 0;
        var groundMinutes = 0.0;
        var paceKnown = true;
        var bonus = !body.IsStar && !body.WasFootfalled && !snapshot.Populated ? BodyRow.FirstLogMultiplier : 1;
        var landing = new List<long>();
        var paceChances = new List<double>();
        var conditions = new BodyConditions(
            body.Class ?? "",
            body.Atmosphere,
            body.Volcanism,
            body.SurfaceGravityG ?? 0,
            body.SurfaceTemperatureK ?? 0,
            body.SurfacePressurePa);
        foreach (var genus in body.Genuses)
        {
            var confirmed = body.Organics.FirstOrDefault(organic => organic.Genus == genus.Genus);
            var prediction = _predictor.Predict(genus.Genus, conditions);
            long expected;
            string candidates;
            if (confirmed is not null)
            {
                expected = snapshot.LearnedSpecies.GetValueOrDefault(confirmed.Species)?.Value
                    ?? prediction.Candidates.FirstOrDefault(candidate => candidate.Species == confirmed.Species)?.Reward
                    ?? 0;
                candidates = string.Create(CultureInfo.InvariantCulture, $"{confirmed.SpeciesName}, stage {confirmed.Stage} of 3");
            }
            else
            {
                expected = prediction.ExpectedReward;
                candidates = prediction.Candidates.Count == 0
                    ? "No prediction for these conditions"
                    : string.Join(" · ", prediction.Candidates.Take(3).Select(candidate =>
                        string.Create(CultureInfo.InvariantCulture, $"{candidate.Name} {candidate.Probability * 100:0}% ({Display.Credits(candidate.Reward)})")));
            }

            paceKnown &= confirmed is not null || prediction.Candidates.Count > 0;
            landing.Add(expected * bonus);
            paceChances.Add(confirmed is not null
                ? (SamplingTime.KeepsPaceAlone(expected * bonus, MinMaxCreditsPerMinute) ? 1 : 0)
                : prediction.Candidates.Where(candidate => SamplingTime.KeepsPaceAlone(candidate.Reward * bonus, MinMaxCreditsPerMinute)).Sum(candidate => candidate.Probability));
            bio += expected;
            var top = prediction.Candidates.Count > 0 ? prediction.Candidates[0] : null;
            genera.Add(new GenusRow(
                genus.Genus,
                genus.Name,
                candidates,
                expected,
                confirmed?.SpeciesName ?? "",
                GenusRow.ShortSpecies(confirmed?.SpeciesName ?? top?.Name ?? "", genus.Name),
                confirmed is not null ? 1 : top?.Probability ?? 0,
                confirmed?.Stage ?? 0));
        }

        var bacteriaOnlyByPrediction = false;
        long predicted = 0;
        var possibleLife = "";
        var possibleGenera = 0;
        if (body.BiologicalSignals > 0 && body.Genuses.Count == 0)
        {
            var possible = _predictor.PossibleGenera(conditions);
            var likely = _predictor.LikelyGenera(conditions);
            paceKnown = likely.Count > 0;
            var onlyBacteria = possible.Count > 0 && possible.All(genus => string.Equals(genus, GenusRow.BacteriaGenus, StringComparison.OrdinalIgnoreCase));
            var bacteriaLikely = likely.Count > 0 && string.Equals(likely[0].Genus, GenusRow.BacteriaGenus, StringComparison.OrdinalIgnoreCase) && likely[0].Probability >= BacteriaOnlyLikelihood;
            bacteriaOnlyByPrediction = body.BiologicalSignals == 1 && (onlyBacteria || bacteriaLikely);
            if (likely.Count > 0)
            {
                possibleGenera = likely.Count;
                predicted = (long)Math.Round(likely.Sum(genus => genus.Probability * genus.Prediction.ExpectedReward) * body.BiologicalSignals);
                var top = likely.Take(3).Select(genus => GenusLine(genus.Prediction.Candidates[0].Name, genus.Probability, genus.Prediction.ExpectedReward));
                possibleLife = $"{Display.Plural(likely.Count, "genus", "genera")} could live here; the likeliest: {string.Join(" · ", top)}";
            }
        }

        var pace = 0.0;
        if (body.Genuses.Count > 0)
        {
            groundMinutes = SamplingTime.GroundMinutes(body.Genuses.Count);
            pace = SamplingTime.BestCreditsPerMinute(landing);
        }
        else if (body.BiologicalSignals > 0)
        {
            // Unprobed: each signal is one draw from the genera the conditions allow, and the chance of a species
            // worth the stop compounds over the signals.
            var likely = _predictor.LikelyGenera(conditions);
            var weight = likely.Sum(genus => genus.Probability);
            groundMinutes = SamplingTime.GroundMinutes(body.BiologicalSignals);
            pace = SamplingTime.CreditsPerMinute(predicted * bonus, groundMinutes);
            var perSignal = weight > 0
                ? likely.Sum(genus => genus.Probability * genus.Prediction.Candidates.Where(candidate => SamplingTime.KeepsPaceAlone(candidate.Reward * bonus, MinMaxCreditsPerMinute)).Sum(candidate => candidate.Probability)) / weight
                : 0;
            paceChances.AddRange(Enumerable.Repeat(perSignal, body.BiologicalSignals));
        }

        var paceChance = SamplingTime.ChanceOfAny(paceChances);

        var signals = new List<string>();
        if (body.BiologicalSignals > 0)
        {
            signals.Add(Display.Plural(body.BiologicalSignals, "biological"));
        }

        if (body.GeologicalSignals > 0)
        {
            signals.Add(Display.Plural(body.GeologicalSignals, "geological"));
        }

        signals.AddRange(body.OtherSignals);

        var materials = body.Materials
            .OrderByDescending(pair => pair.Value)
            .Select(pair => _catalog.MaterialsBySymbol.TryGetValue(pair.Key, out var material)
                ? (material.Name, Share: pair.Value, material.Grade)
                : (Name: pair.Key, Share: pair.Value, Grade: 0))
            .ToList();

        var surface = body.IsStar
            ? (body.MassEarths is { } solar ? string.Create(CultureInfo.InvariantCulture, $"{solar:0.00} solar masses") : "")
            : string.Join(" · ", new[]
            {
                body.SurfaceTemperatureK is { } k ? string.Create(CultureInfo.InvariantCulture, $"{k:0} K") : "",
                body.Terraformable ? "terraformable" : "",
                body.HasRings ? "rings" : "",
            }.Where(part => part.Length > 0));

        var scanCredits = ScanCredits(body);
        var scanValue = body.FromNavBeacon
            ? "nav beacon data, nothing to sell"
            : body.FromCatalogue
            ? ""
            : body.IsStar
            ? Display.Credits(scanCredits)
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{Display.Credits(BodyValue.Planet(body.Class, body.Terraformable, body.MassEarths ?? 1, !body.WasDiscovered, mapped: false, !body.WasMapped))} scanned, {Display.Credits(BodyValue.Planet(body.Class, body.Terraformable, body.MassEarths ?? 1, !body.WasDiscovered, mapped: true, !body.WasMapped))} mapped");

        return new BodyRow(
            ShortName(body.Name, snapshot.SystemName),
            body.IsStar,
            body.IsStar && StarClasses.IsScoopable(body.Class),
            body.IsStar ? StarClasses.Describe(body.Class) : body.Class ?? "Belt cluster",
            body.DistanceLs,
            body.IsStar ? null : body.SurfaceGravityG,
            surface,
            body.Atmosphere.Length > 0 ? body.Atmosphere : (body.IsStar ? "" : "no atmosphere"),
            body.Volcanism,
            body.Landable,
            !body.WasDiscovered,
            !body.WasMapped,
            !body.WasFootfalled && !snapshot.Populated,
            body.MappedByYou,
            body.LandedByYou,
            signals.Count > 0 ? string.Join(", ", signals) : "",
            body.BiologicalSignals,
            [.. genera.OrderByDescending(genus => genus.ExpectedCredits).ThenBy(genus => genus.Name, StringComparer.Ordinal)],
            bacteriaOnlyByPrediction,
            groundMinutes,
            paceKnown,
            pace,
            paceChance,
            string.Join(", ", materials.Take(6).Select(material => string.Create(CultureInfo.InvariantCulture, $"{material.Name} {material.Share:0.0}%"))),
            [.. materials.Select(material => material.Name)],
            scanValue,
            scanCredits,
            bio,
            predicted,
            possibleLife,
            possibleGenera,
            materials.Count == 0 ? 0 : materials.Max(material => material.Grade),
            !body.FromCatalogue && snapshot.ArrivedAt is { } arrived && body.ScannedAt != default && body.ScannedAt < arrived
                ? string.Create(CultureInfo.InvariantCulture, $"scanned {body.ScannedAt.ToLocalTime():d MMM}")
                : "",
            body.FromCatalogue);
    }
}
