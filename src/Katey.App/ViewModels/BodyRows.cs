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
/// <param name="HasFirstLogBonus">
/// Whether the first-logged bonus, <see cref="BodyRow.FirstLogMultiplier"/> times the payout, applies: once
/// sampled, the game's own word that nobody had logged the species here; before that, the body's
/// <see cref="BodyRow.FirstLogLikely"/>.
/// </param>
/// <param name="IsSold">Whether the species has been sampled in full and sold.</param>
public sealed record GenusRow(string Genus, string Name, string Candidates, long ExpectedCredits, string Confirmed, string TopSpecies = "", double TopProbability = 0, int Stage = 0, bool HasFirstLogBonus = false, bool IsSold = false)
{
    /// <summary>Gets the payout with the first-logged bonus where it applies, credits.</summary>
    public long CreditsWithBonus => ExpectedCredits * (HasFirstLogBonus ? BodyRow.FirstLogMultiplier : 1);

    /// <summary>The width the figure in <see cref="ExpectedText"/> is padded to, "999.9M", so the figures of one body's genera line up in a monospaced face.</summary>
    private const int FigureWidth = 6;

    /// <summary>The width the bracketed bonus figure in <see cref="ExpectedText"/> is padded to, "(999.9M)".</summary>
    private const int BonusWidth = 8;

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

    /// <summary>
    /// Gets the expected payout as text: the figure in millions, with the figure with the first-logged bonus in
    /// brackets after it when that is likely, such as "  2.1M  (10.5M) Cr". Each figure is padded to a fixed
    /// width, so the figures of one body's genera line up when right-aligned in a monospaced face.
    /// </summary>
    public string ExpectedText
    {
        get
        {
            if (ExpectedCredits == 0)
            {
                return "value unknown";
            }

            if (!HasFirstLogBonus)
            {
                return Display.CreditsMillions(ExpectedCredits);
            }

            var bonus = $"({Display.Millions(ExpectedCredits * BodyRow.FirstLogMultiplier)})";
            return $"{Display.Millions(ExpectedCredits),FigureWidth} {bonus,BonusWidth} Cr";
        }
    }

    /// <summary>Gets whether this is the bacteria genus.</summary>
    public bool IsBacteria => string.Equals(Genus, BacteriaGenus, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets whether exactly one of the three samples is taken: the species is named, the work has begun.</summary>
    public bool IsFirstSample => Stage == 1;

    /// <summary>Gets whether two of the three samples are taken: one more and it is logged.</summary>
    public bool IsSecondSample => Stage == 2;

    /// <summary>Gets whether all three samples are taken, so the species is logged and ready to sell.</summary>
    public bool IsComplete => Stage >= 3;
}

/// <summary>One run of a sentence: plain text, or a figure to be coloured.</summary>
/// <param name="Text">The run's text, spaces included.</param>
/// <param name="IsFigure">Whether the run is a figure, drawn in the figure colour.</param>
/// <param name="IsEarned">Whether the figure is money already in hand, drawn in the good colour instead.</param>
public sealed record TextRun(string Text, bool IsFigure, bool IsEarned = false);

/// <summary>One body in the current system, ready to print.</summary>
/// <param name="Name">Short body name, without the system name.</param>
/// <param name="IsStar">Whether it is a star. Stars cannot be mapped, so a star carries no mapping note.</param>
/// <param name="IsScoopable">Whether it is a star the drive can refuel from.</param>
/// <param name="Class">For a star, its class in words with whether it can be scooped; otherwise the planet class, or "Belt cluster".</param>
/// <param name="DistanceLs">Distance from arrival in light seconds.</param>
/// <param name="GravityG">Surface gravity in g, or <see langword="null"/> for a star or an unknown.</param>
/// <param name="Surface">Notes on the surface, terraformable and rings, or solar masses for a star; empty for none.</param>
/// <param name="TemperatureText">The surface temperature, such as "300K", or empty when unknown or for a star.</param>
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
/// <param name="Sold">Whether the scan has been sold, so its cartographics are no longer aboard.</param>
/// <param name="ExobiologySold">Whether you have sold exobiology data from this body.</param>
public sealed record BodyRow(
    string Name,
    bool IsStar,
    bool IsScoopable,
    string Class,
    double DistanceLs,
    double? GravityG,
    string Surface,
    string TemperatureText,
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
    bool FromCatalogue,
    bool Sold = false,
    bool ExobiologySold = false)
{
    /// <summary>Gets whether <see cref="EarlierVisit"/> has anything to say.</summary>
    public bool IsEarlierVisit => EarlierVisit.Length > 0;

    /// <summary>Gets the cartographics value with no first-discovery bonus, credits; set by the builder.</summary>
    public long ScanBaseCredits { get; init; }

    /// <summary>Gets the distance from arrival as text.</summary>
    public string Distance => string.Create(CultureInfo.InvariantCulture, $"{DistanceLs:0}ls");

    /// <summary>Gets whether it is a star the drive cannot refuel from, which deserves a warning of its own.</summary>
    public bool IsUnscoopableStar => IsStar && !IsScoopable;

    /// <summary>Gets the surface gravity as text, such as "1.60g", or empty when unknown.</summary>
    public string GravityText => GravityG is { } g ? string.Create(CultureInfo.InvariantCulture, $"{g:0.00}g") : "";

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

    /// <summary>Gets the discovery slot: "discovered by you" when nobody had discovered it before your scan, else "discovered"; "discovery unknown" for a body a catalogue described, which says nothing of who found it.</summary>
    public string DiscoveryStatus => FromCatalogue ? "discovery unknown" : DiscoveryYours ? "discovered by you" : "discovered";

    /// <summary>Gets whether the first discovery is yours: nobody had discovered it before you scanned it. The game credits it when the data is sold. False for a body a catalogue described.</summary>
    public bool DiscoveryYours => !FromCatalogue && FirstDiscovery;

    /// <summary>
    /// Gets the mapping slot: "unmapped" while nobody has mapped it, "mapped by you" when you were the first, else
    /// "mapped"; empty for a star, which cannot be mapped, and "mapping unknown" for a body a catalogue described.
    /// </summary>
    public string MappingStatus => IsStar ? "" : FromCatalogue ? "mapping unknown" : MappedByYouFirst ? "mapped by you" : FirstMappingOpen ? "unmapped" : "mapped";

    /// <summary>Gets whether the first mapping is yours: still open, or taken by you. False for a star and for a body a catalogue described.</summary>
    public bool MappingYours => !IsStar && !FromCatalogue && FirstMapping;

    /// <summary>
    /// Gets the footfall slot for a landable body: "no footfall" while nobody has set foot on it, "footfall by you"
    /// when you were the first, else "footfall"; empty where nobody can land, and "footfall unknown" for a body a
    /// catalogue described.
    /// </summary>
    public string FootfallStatus => !Landable ? "" : FromCatalogue ? "footfall unknown" : FootfallOpen ? "no footfall" : FootfallYours ? "footfall by you" : "footfall";

    /// <summary>Gets whether the first footfall is yours: still open, or taken by you. False where nobody can land and for a body a catalogue described.</summary>
    public bool FootfallYours => Landable && !FromCatalogue && FirstFootfall;

    /// <summary>Gets whether the footfall is still open: nobody has set foot on it, you included.</summary>
    public bool FootfallOpen => FootfallYours && !LandedByYou;

    /// <summary>
    /// Gets the quiet line on your part the slots do not tell: "Mapped by you." or "Landed by you." where someone
    /// else was first, or empty. Your firsts are in the slots themselves.
    /// </summary>
    public string StatusDetail
    {
        get
        {
            var parts = new List<string>();
            if (!IsStar && MappedByYou && !FirstMapping)
            {
                parts.Add("mapped by you");
            }

            if (LandedByYou && !FirstFootfall)
            {
                parts.Add("landed by you");
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
    /// Gets whether the first-logged bonus is likely for a species not yet sampled here: nobody but you has set
    /// foot on the body, so nobody else can have logged its life, which is done on foot. Once you have sold
    /// exobiology data from the body, nothing more is assumed for what you left unsampled, since another
    /// commander may have landed since. Someone can still sell before you, so it is a likelihood. A sampled
    /// species has the game's own word instead, on its <see cref="GenusRow.HasFirstLogBonus"/>.
    /// </summary>
    public bool FirstLogLikely => !IsStar && FirstFootfall && !ExobiologySold;

    /// <summary>Gets the base exobiology figure: the scanner's genera when named, else the prediction from the body's conditions.</summary>
    public long ExobiologyBaseCredits => ExobiologyCredits > 0 ? ExobiologyCredits : PredictedExobiologyCredits;

    /// <summary>
    /// Gets the exobiology figure to rank and show, the first-logged bonus counted where it applies: genus by
    /// genus once the scanner has named them, else the prediction times <see cref="FirstLogMultiplier"/> when
    /// <see cref="FirstLogLikely"/>.
    /// </summary>
    public long ExobiologyRank => Genera.Count > 0 ? Genera.Sum(genus => genus.CreditsWithBonus) : ExobiologyBaseCredits * (FirstLogLikely ? FirstLogMultiplier : 1);

    /// <summary>Gets whether the first-logged bonus applies to every genus, so <see cref="ExobiologyRank"/> is exactly <see cref="FirstLogMultiplier"/> times the base.</summary>
    public bool HasFirstLogBonusThroughout => ExobiologyBaseCredits > 0 && ExobiologyRank == ExobiologyBaseCredits * FirstLogMultiplier;

    /// <summary>
    /// Gets what this body's data still aboard would pay, credits: its cartographics unless sold, plus every
    /// species sampled in full and not yet sold, with the first-logged bonus where the game says it applies.
    /// </summary>
    public long UnsoldCredits => (Sold ? 0 : ScanCredits) + Genera.Where(genus => genus.IsComplete && !genus.IsSold).Sum(genus => genus.CreditsWithBonus);

    /// <summary>Gets what of <see cref="UnsoldCredits"/> is for being first: the discovery bonus on an unsold scan and the first-logged bonus on unsold species.</summary>
    public long UnsoldFirstBonus => (Sold ? 0 : ScanCredits - ScanBaseCredits) + Genera.Where(genus => genus.IsComplete && !genus.IsSold).Sum(genus => genus.CreditsWithBonus - genus.ExpectedCredits);

    /// <summary>Gets whether the exobiology figure is a prediction from conditions rather than from the scanner's genera.</summary>
    public bool IsExobiologyPredicted => ExobiologyCredits == 0 && PredictedExobiologyCredits > 0;

    /// <summary>Gets whether the scanner has named the body's genera: the probes have been fired.</summary>
    public bool IsProbed => Genera.Count > 0;

    /// <summary>Gets the biological signal count as a short badge, such as "3 bio", or empty with none.</summary>
    public string BiologyCountText => BiologicalSignals > 0 ? string.Create(CultureInfo.InvariantCulture, $"{BiologicalSignals} bio") : "";

    /// <summary>
    /// Gets the expected exobiology payout as a sentence in runs, the figures marked: the scanner's figure, exact
    /// once every genus is sampled, or the prediction with how it was made; when the first-logged bonus is likely
    /// the figure with the bonus follows after a comma. Empty with no life.
    /// </summary>
    public IReadOnlyList<TextRun> ExobiologyRuns
    {
        get
        {
            if (ExobiologyBaseCredits == 0)
            {
                return [];
            }

            var known = ExobiologyCredits > 0;
            var runs = new List<TextRun>
            {
                new(known ? IsExobiologyKnown ? "Exobiology " : "Exobiology about " : "Exobiology likely about ", false),
                new(Display.Credits(ExobiologyBaseCredits), true),
            };
            if (ExobiologyRank > ExobiologyBaseCredits)
            {
                runs.Add(new(", ", false));
                runs.Add(new(Display.Credits(ExobiologyRank), true));
                runs.Add(new(" with the first-logged bonus", false));
            }

            runs.Add(new(known ? "." : " by the life these conditions allow; map it to know.", false));
            return runs;
        }
    }

    /// <summary>Gets <see cref="ExobiologyRuns"/> as one plain sentence, or empty.</summary>
    public string ExobiologyText => string.Concat(ExobiologyRuns.Select(run => run.Text));

    /// <summary>
    /// Gets whether the exobiology figure is exact: the scanner has listed the genera and every one has a species
    /// sampled, so nothing is left to the prediction.
    /// </summary>
    public bool IsExobiologyKnown => Genera.Count > 0 && Genera.All(genus => genus.Stage >= 1);

    /// <summary>
    /// Gets the line on the exobiology already in hand on this body: the species with all three samples taken,
    /// their payout in the good colour, and the payout with the first-logged bonus when it applies here. Empty
    /// until one species is complete.
    /// </summary>
    public IReadOnlyList<TextRun> GatheredRuns
    {
        get
        {
            var complete = Genera.Where(genus => genus.IsComplete).ToList();
            var gathered = complete.Sum(genus => genus.ExpectedCredits);
            if (gathered == 0)
            {
                return [];
            }

            var withBonus = complete.Sum(genus => genus.CreditsWithBonus);
            var runs = new List<TextRun> { new("You've gathered ", false), new(Display.Credits(gathered), true, true) };
            if (withBonus > gathered)
            {
                runs.Add(new(", ", false));
                runs.Add(new(Display.Credits(withBonus), true, true));
                runs.Add(new(" with the first-logged bonus", false));
            }

            runs.Add(new(".", false));
            return runs;
        }
    }

    /// <summary>Gets whether <see cref="GatheredRuns"/> has anything to say.</summary>
    public bool HasGathered => GatheredRuns.Count > 0;

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

    /// <summary>The cartographics a scan of the body pays with no bonus at all: not first discovered, not mapped.</summary>
    /// <param name="body">The body.</param>
    /// <returns>Credits, 0 for a body from a nav beacon or a catalogue.</returns>
    public static long ScanBaseCredits(ScannedBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return body.FromNavBeacon || body.FromCatalogue
            ? 0
            : body.IsStar
            ? BodyValue.Star(body.Class, body.MassEarths ?? 1, firstDiscovered: false)
            : BodyValue.Planet(body.Class, body.Terraformable, body.MassEarths ?? 1, firstDiscovered: false, mapped: false, firstMapped: false);
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
        // The bonus for a species not yet sampled: nobody but you has set foot here, and you have not sold this body's exobiology yet.
        var firstLogLikely = !body.IsStar && !body.WasFootfalled && !snapshot.Populated && !body.ExobiologySold;
        var bonus = firstLogLikely ? BodyRow.FirstLogMultiplier : 1;
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

            // A sampled species has the game's word on the bonus; one not yet sampled takes the body's likelihood.
            var genusBonus = confirmed is not null ? (confirmed.WasLogged ? 1 : BodyRow.FirstLogMultiplier) : bonus;
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
                confirmed?.Stage ?? 0,
                genusBonus > 1,
                confirmed?.Sold ?? false));
        }

        var bacteriaOnlyByPrediction = false;
        long predicted = 0;
        var possibleLife = "";
        var possibleGenera = 0;
        if (body.BiologicalSignals > 0 && body.Genuses.Count == 0)
        {
            var possible = _predictor.PossibleGenera(conditions);
            var likely = _predictor.LikelyGenera(conditions);
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
            .Select(pair => _catalog.Materials.TryGetValue(pair.Key, out var material)
                ? (material.Name, Share: pair.Value, material.Grade)
                : (Name: pair.Key, Share: pair.Value, Grade: 0))
            .ToList();

        var surface = body.IsStar
            ? (body.MassEarths is { } solar ? string.Create(CultureInfo.InvariantCulture, $"{solar:0.00} solar masses") : "")
            : string.Join(" · ", new[]
            {
                body.Terraformable ? "terraformable" : "",
                body.HasRings ? "rings" : "",
            }.Where(part => part.Length > 0));
        var temperature = !body.IsStar && body.SurfaceTemperatureK is { } kelvin ? string.Create(CultureInfo.InvariantCulture, $"{kelvin:0}K") : "";

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
            temperature,
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
            body.FromCatalogue,
            body.Sold,
            body.ExobiologySold)
        {
            ScanBaseCredits = ScanBaseCredits(body),
        };
    }
}
