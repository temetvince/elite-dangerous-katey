using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Katey.Core.Exploration;

/// <summary>A body's surface conditions, as the journal's <c>Scan</c> event reports them.</summary>
/// <param name="PlanetClass">The <c>PlanetClass</c> string, such as "Rocky body".</param>
/// <param name="Atmosphere">The <c>Atmosphere</c> string, such as "thin carbon dioxide atmosphere", or empty.</param>
/// <param name="Volcanism">The <c>Volcanism</c> string, such as "minor silicate vapour geysers volcanism", or empty.</param>
/// <param name="GravityG">Surface gravity in g.</param>
/// <param name="TemperatureK">Surface temperature in kelvin.</param>
/// <param name="PressurePa">Surface pressure in pascals, or <see langword="null"/> when unknown.</param>
public sealed record BodyConditions(string PlanetClass, string Atmosphere, string Volcanism, double GravityG, double TemperatureK, double? PressurePa);

/// <summary>One species that could be the life a genus signal stands for.</summary>
/// <param name="Species">Species symbol such as <c>$Codex_Ent_Bacterial_01_Name;</c>.</param>
/// <param name="Name">Display name such as "Bacterium Aurasus".</param>
/// <param name="Probability">Chance this is the species, 0–1, summing to 1 across the genus.</param>
/// <param name="Reward">Credits Vista Genomics pays for a complete sample, before any first-discovery bonus.</param>
public sealed record SpeciesPrediction(string Species, string Name, double Probability, long Reward);

/// <summary>What a genus signal on a body is likely to be worth.</summary>
/// <param name="Genus">Genus symbol.</param>
/// <param name="Candidates">Possible species, most likely first. Empty when the data has nothing for the genus under these conditions.</param>
public sealed record GenusPrediction(string Genus, IReadOnlyList<SpeciesPrediction> Candidates)
{
    /// <summary>Gets the probability-weighted reward, credits.</summary>
    public long ExpectedReward => (long)Math.Round(Candidates.Sum(candidate => candidate.Probability * candidate.Reward));

    /// <summary>Gets the lowest reward among the candidates, or 0.</summary>
    public long MinimumReward => Candidates.Count == 0 ? 0 : Candidates.Min(candidate => candidate.Reward);

    /// <summary>Gets the highest reward among the candidates, or 0.</summary>
    public long MaximumReward => Candidates.Count == 0 ? 0 : Candidates.Max(candidate => candidate.Reward);
}

/// <summary>How likely a genus is to be among a body's life, against the other genera that could live there.</summary>
/// <param name="Genus">Genus symbol.</param>
/// <param name="Probability">The genus's share of the body's likely life, 0–1, summing to 1 across the list it came from.</param>
/// <param name="Prediction">The genus's species prediction on this body. Never empty.</param>
public sealed record GenusLikelihood(string Genus, double Probability, GenusPrediction Prediction);

/// <summary>
/// Predicts which species a genus signal is, from where that genus has been found before.
/// </summary>
/// <remarks>
/// The data is Canonn Bioforge's: for every species variant, histograms of the atmosphere, body type,
/// volcanism, gravity, temperature and pressure of the bodies it was found on. A candidate's score is
/// the geometric mean of its frequency in each of those dimensions on the body in question, weighted by
/// how often the variant has been seen at all; scores are normalised within the genus and candidates
/// under one percent are dropped. A dimension the body lacks data for is skipped. Values are Vista
/// Genomics' base payouts; the first-discovery bonus is not included. The predictor is immutable and
/// thread-safe, and memoises every lookup by its <see cref="BodyConditions"/>, since a page rebuilds the
/// same bodies on every refresh; the memo is bounded and emptied when it fills.
/// </remarks>
public sealed partial class ExobiologyPredictor
{
    private const double StandardPressurePa = 101325.0;
    private const double MinimumShare = 0.01;

    private const int MemoLimit = 4096;

    private readonly FrozenDictionary<string, IReadOnlyList<Variant>> _byGenus;
    private readonly FrozenDictionary<string, int> _genusCounts;
    private readonly FrozenDictionary<string, string> _speciesNames;
    private readonly FrozenDictionary<string, long> _speciesRewards;
    private readonly ConcurrentDictionary<BodyConditions, IReadOnlyList<string>> _possible = new();
    private readonly ConcurrentDictionary<BodyConditions, IReadOnlyList<GenusLikelihood>> _likely = new();
    private readonly ConcurrentDictionary<(string Genus, BodyConditions Body), GenusPrediction> _predictions = new();

    private ExobiologyPredictor(FrozenDictionary<string, IReadOnlyList<Variant>> byGenus, FrozenDictionary<string, string> speciesNames, FrozenDictionary<string, long> speciesRewards)
    {
        _byGenus = byGenus;
        _genusCounts = byGenus.ToFrozenDictionary(pair => pair.Key, pair => pair.Value.Sum(variant => variant.Count), byGenus.Comparer);
        _speciesNames = speciesNames;
        _speciesRewards = speciesRewards;
    }

    /// <summary>Gets the genus symbols the data covers.</summary>
    public IReadOnlyCollection<string> Genera => _byGenus.Keys;

    /// <summary>Loads the table embedded in this assembly.</summary>
    /// <returns>The predictor.</returns>
    /// <exception cref="CatalogException">The embedded table is missing or malformed. Never expected from a shipped build.</exception>
    public static ExobiologyPredictor LoadEmbedded()
    {
        using var stream = typeof(ExobiologyPredictor).Assembly.GetManifestResourceStream("Katey.Core.Data.exobiology.json")
            ?? throw new CatalogException("exobiology.json: embedded resource missing");
        using var document = JsonDocument.Parse(stream);
        var variants = new List<Variant>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            variants.Add(Variant.Read(property.Name, property.Value));
        }

        return new ExobiologyPredictor(
            variants.GroupBy(variant => variant.Genus, StringComparer.Ordinal)
                .ToFrozenDictionary(group => group.Key, IReadOnlyList<Variant> (group) => [.. group], StringComparer.Ordinal),
            variants.GroupBy(variant => variant.Species, StringComparer.Ordinal)
                .ToFrozenDictionary(group => group.Key, group => group.First().SpeciesName, StringComparer.Ordinal),
            variants.GroupBy(variant => variant.Species, StringComparer.Ordinal)
                .ToFrozenDictionary(group => group.Key, group => group.First().Reward, StringComparer.Ordinal));
    }

    /// <summary>The base payout of a species the data knows, before any first-logged bonus.</summary>
    /// <param name="species">Species symbol.</param>
    /// <returns>Credits, or <see langword="null"/> for an unknown species.</returns>
    public long? Reward(string species)
    {
        ArgumentNullException.ThrowIfNull(species);
        return _speciesRewards.TryGetValue(species, out var reward) ? reward : null;
    }

    /// <summary>The display name of a species the data knows.</summary>
    /// <param name="species">Species symbol.</param>
    /// <returns>The name, or <see langword="null"/>.</returns>
    public string? SpeciesName(string species)
    {
        ArgumentNullException.ThrowIfNull(species);
        return _speciesNames.GetValueOrDefault(species);
    }

    /// <summary>Predicts the species behind a genus signal on a body.</summary>
    /// <param name="genus">Genus symbol as the journal reports it.</param>
    /// <param name="body">The body's conditions.</param>
    /// <returns>The prediction; its candidates are empty for an unknown genus.</returns>
    public GenusPrediction Predict(string genus, BodyConditions body)
    {
        ArgumentNullException.ThrowIfNull(genus);
        ArgumentNullException.ThrowIfNull(body);
        return Remember(_predictions, (Genus: genus, Body: body), key => PredictNow(key.Genus, Conditions.Of(key.Body)));
    }

    private GenusPrediction PredictNow(string genus, Conditions body)
    {
        if (!_byGenus.TryGetValue(genus, out var variants))
        {
            return new GenusPrediction(genus, []);
        }

        var totalCount = _genusCounts[genus];
        var scored = new Dictionary<string, (double Score, long Reward, string Name)>(StringComparer.Ordinal);
        foreach (var variant in variants)
        {
            var score = Fit(variant, body) * (totalCount > 0 ? (double)variant.Count / totalCount : 1);
            var previous = scored.TryGetValue(variant.Species, out var existing) ? existing.Score : 0;
            scored[variant.Species] = (previous + score, variant.Reward, variant.SpeciesName);
        }

        var total = scored.Values.Sum(entry => entry.Score);
        if (total <= 0)
        {
            return new GenusPrediction(genus, []);
        }

        var kept = scored
            .Select(pair => (pair.Key, Share: pair.Value.Score / total, pair.Value.Reward, pair.Value.Name))
            .Where(entry => entry.Share >= MinimumShare)
            .ToList();
        var keptTotal = kept.Sum(entry => entry.Share);
        return new GenusPrediction(
            genus,
            [
                .. kept
                    .Select(entry => new SpeciesPrediction(entry.Key, entry.Name, entry.Share / keptTotal, entry.Reward))
                    .OrderByDescending(prediction => prediction.Probability),
            ]);
    }

    /// <summary>
    /// Whether a genus has ever been found under a body's conditions: some species of it has been recorded
    /// with the body's atmosphere, body type and volcanism, within its gravity and temperature ranges, and
    /// within its pressure range when the pressure is known. Unlike <see cref="Predict"/>, nothing is smoothed:
    /// a category never seen rules the genus out.
    /// </summary>
    /// <param name="genus">Genus symbol as the journal reports it.</param>
    /// <param name="body">The body's conditions.</param>
    /// <returns><see langword="false"/> for an unknown genus.</returns>
    public bool CanLive(string genus, BodyConditions body)
    {
        ArgumentNullException.ThrowIfNull(genus);
        ArgumentNullException.ThrowIfNull(body);
        return CanLiveNow(genus, Conditions.Of(body));
    }

    private bool CanLiveNow(string genus, Conditions body)
    {
        return _byGenus.TryGetValue(genus, out var variants) && variants.Any(variant =>
            Seen(variant.Atmospheres, body.Atmosphere)
            && Seen(variant.Bodies, body.BodyType)
            && Seen(variant.Volcanism, body.Volcanism)
            && Within(variant.Gravity, body.GravityG)
            && Within(variant.Temperature, body.TemperatureK)
            && (body.PressureAtm is not { } pressure || Within(variant.Pressure, pressure)));
    }

    /// <summary>The genera that have been found under a body's conditions, by <see cref="CanLive"/>.</summary>
    /// <param name="body">The body's conditions.</param>
    /// <returns>Genus symbols in the table's order. Empty when nothing has been found under such conditions.</returns>
    public IReadOnlyList<string> PossibleGenera(BodyConditions body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return Remember(_possible, body, key =>
        {
            var conditions = Conditions.Of(key);
            return [.. Genera.Where(genus => CanLiveNow(genus, conditions))];
        });
    }

    /// <summary>
    /// The genera that could live on a body, each weighted by how likely it is to be there: the sum over its
    /// variants of how often each has been seen times how well it fits the body's conditions, normalised
    /// across the genera. Bacteria, found almost everywhere, weigh more than a genus seen a handful of times
    /// under such conditions.
    /// </summary>
    /// <param name="body">The body's conditions.</param>
    /// <returns>The genera that pass <see cref="CanLive"/> and have a prediction, likeliest first. Empty when none do.</returns>
    public IReadOnlyList<GenusLikelihood> LikelyGenera(BodyConditions body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return Remember(_likely, body, LikelyGeneraNow);
    }

    private IReadOnlyList<GenusLikelihood> LikelyGeneraNow(BodyConditions body)
    {
        var conditions = Conditions.Of(body);
        var weighed = PossibleGenera(body)
            .Select(genus => (Genus: genus, Weight: _byGenus[genus].Sum(variant => Fit(variant, conditions) * variant.Count), Prediction: Predict(genus, body)))
            .Where(entry => entry.Weight > 0 && entry.Prediction.Candidates.Count > 0)
            .ToList();
        var total = weighed.Sum(entry => entry.Weight);
        return total <= 0
            ? []
            : [.. weighed.Select(entry => new GenusLikelihood(entry.Genus, entry.Weight / total, entry.Prediction)).OrderByDescending(likelihood => likelihood.Probability)];
    }

    /// <summary>A memoised lookup: the cached answer, or the computed one, with the memo emptied first when it is full.</summary>
    private static TValue Remember<TKey, TValue>(ConcurrentDictionary<TKey, TValue> memo, TKey key, Func<TKey, TValue> compute)
        where TKey : notnull
    {
        if (memo.TryGetValue(key, out var known))
        {
            return known;
        }

        if (memo.Count >= MemoLimit)
        {
            memo.Clear();
        }

        return memo.GetOrAdd(key, compute);
    }

    /// <summary>How well a variant fits a body: the geometric mean of its frequency in each dimension the body has data for.</summary>
    private static double Fit(Variant variant, Conditions body)
    {
        var logs = Math.Log(Share(variant.Atmospheres, variant.AtmosphereTotal, body.Atmosphere))
            + Math.Log(Share(variant.Bodies, variant.BodyTotal, body.BodyType))
            + Math.Log(Share(variant.Volcanism, variant.VolcanismTotal, body.Volcanism))
            + Math.Log(BinShare(variant.Gravity, variant.GravityTotal, body.GravityG))
            + Math.Log(BinShare(variant.Temperature, variant.TemperatureTotal, body.TemperatureK));
        var factors = 5;
        if (body.PressureAtm is { } pressure)
        {
            logs += Math.Log(BinShare(variant.Pressure, variant.PressureTotal, pressure));
            factors++;
        }

        return Math.Exp(logs / factors);
    }

    /// <summary>Whether a category has been recorded at all. A histogram with nothing in it constrains nothing.</summary>
    private static bool Seen(IReadOnlyDictionary<string, int> histogram, string key)
        => histogram.Count == 0 || (histogram.TryGetValue(key, out var count) && count > 0);

    /// <summary>Whether a value falls in a bin that has been recorded. No bins constrain nothing.</summary>
    private static bool Within(IReadOnlyList<Bin> bins, double value)
        => bins.Count == 0 || bins.Any(bin => bin.Count > 0 && value >= bin.Min && value <= bin.Max);

    /// <summary>Frequency of a category, smoothed so an unseen category is unlikely rather than impossible.</summary>
    /// <param name="histogram">The category counts.</param>
    /// <param name="total">The sum of the counts, kept on the variant.</param>
    /// <param name="key">The body's category.</param>
    private static double Share(IReadOnlyDictionary<string, int> histogram, int total, string key)
        => total == 0 ? 1 : histogram.TryGetValue(key, out var count) && count > 0 ? (double)count / total : 0.5 / (total + 1);

    private static double BinShare(IReadOnlyList<Bin> bins, int total, double value)
    {
        if (total == 0)
        {
            return 1;
        }

        var hit = bins.Where(bin => value >= bin.Min && value <= bin.Max).Sum(bin => bin.Count);
        return hit > 0 ? (double)hit / total : 0.5 / (total + 1);
    }

    private sealed record Bin(double Min, double Max, int Count);

    /// <summary>A body's conditions in the table's own keys, computed once per lookup rather than once per variant.</summary>
    private readonly record struct Conditions(string Atmosphere, string BodyType, string Volcanism, double GravityG, double TemperatureK, double? PressureAtm)
    {
        public static Conditions Of(BodyConditions body) => new(
            Canonical.Atmosphere(body.Atmosphere),
            Canonical.BodyType(body.PlanetClass),
            Canonical.Volcanism(body.PlanetClass, body.Volcanism),
            body.GravityG,
            body.TemperatureK,
            body.PressurePa is { } pa ? pa / StandardPressurePa : null);
    }

    private sealed record Variant(
        string Symbol,
        string Species,
        string SpeciesName,
        string Genus,
        long Reward,
        int Count,
        IReadOnlyDictionary<string, int> Atmospheres,
        IReadOnlyDictionary<string, int> Bodies,
        IReadOnlyDictionary<string, int> Volcanism,
        IReadOnlyList<Bin> Gravity,
        IReadOnlyList<Bin> Temperature,
        IReadOnlyList<Bin> Pressure)
    {
        public int AtmosphereTotal { get; } = Atmospheres.Values.Sum();

        public int BodyTotal { get; } = Bodies.Values.Sum();

        public int VolcanismTotal { get; } = Volcanism.Values.Sum();

        public int GravityTotal { get; } = Gravity.Sum(bin => bin.Count);

        public int TemperatureTotal { get; } = Temperature.Sum(bin => bin.Count);

        public int PressureTotal { get; } = Pressure.Sum(bin => bin.Count);

        public static Variant Read(string symbol, JsonElement element)
        {
            return new Variant(
                symbol,
                Text(element, "species"),
                Text(element, "speciesName"),
                Text(element, "genus"),
                element.GetProperty("reward").GetInt64(),
                element.GetProperty("count").GetInt32(),
                Histogram(element, "atmospheres", Canonical.Atmosphere),
                Histogram(element, "bodies", Canonical.BodyType),
                Histogram(element, "volcanism", Canonical.VolcanismKey),
                Bins(element, "gravity"),
                Bins(element, "temperature"),
                Bins(element, "pressure"));
        }

        private static string Text(JsonElement element, string property)
            => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()!
                : throw new CatalogException($"exobiology.json: \"{property}\" missing");

        private static Dictionary<string, int> Histogram(JsonElement element, string property, Func<string, string> canonical)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (element.TryGetProperty(property, out var histogram) && histogram.ValueKind == JsonValueKind.Object)
            {
                foreach (var entry in histogram.EnumerateObject())
                {
                    var key = canonical(entry.Name);
                    result[key] = result.GetValueOrDefault(key) + entry.Value.GetInt32();
                }
            }

            return result;
        }

        private static IReadOnlyList<Bin> Bins(JsonElement element, string property)
        {
            return !element.TryGetProperty(property, out var bins) || bins.ValueKind != JsonValueKind.Array
                ? []
                : [.. bins.EnumerateArray().Select(bin => new Bin(bin[0].GetDouble(), bin[1].GetDouble(), bin[2].GetInt32()))];
        }
    }

    /// <summary>
    /// Reduces the game's and Bioforge's spellings of the same thing to one key: lower case, letters and
    /// digits only, with "sulfur" spelt "sulphur", the word "atmosphere" dropped, and body types stripped
    /// of their trailing "body" or "world".
    /// </summary>
    private static partial class Canonical
    {
        public static string Atmosphere(string text)
        {
            var key = Squash(text.Replace("atmosphere", "", StringComparison.OrdinalIgnoreCase));
            return key.Length == 0 || key == "none" ? "noatmosphere" : key;
        }

        public static string BodyType(string text)
        {
            var key = Squash(text);
            foreach (var suffix in new[] { "body", "world" })
            {
                if (key.EndsWith(suffix, StringComparison.Ordinal) && key.Length > suffix.Length)
                {
                    key = key[..^suffix.Length];
                    break;
                }
            }

            return key;
        }

        /// <summary>The journal's volcanism string, in Bioforge's "body - volcanism" form, minor variants folded into the base type and every spelling of none folded into one key.</summary>
        public static string Volcanism(string planetClass, string volcanism)
        {
            var kind = volcanism.Replace("volcanism", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (kind.StartsWith("minor ", StringComparison.OrdinalIgnoreCase))
            {
                kind = kind[6..];
            }

            var key = Squash(kind);
            return BodyType(planetClass) + "|" + (key.Length == 0 || key == "no" || key == "none" ? "novolcanism" : key);
        }

        /// <summary>A Bioforge "Body type - Volcanism" histogram key, canonicalised the same way as <see cref="Volcanism"/>.</summary>
        public static string VolcanismKey(string text)
        {
            var separator = text.LastIndexOf(" - ", StringComparison.Ordinal);
            return separator < 0 ? BodyType(text) + "|novolcanism" : Volcanism(text[..separator], text[(separator + 3)..]);
        }

        private static string Squash(string text)
            => NonAlphanumeric().Replace(text.Replace("sulfur", "sulphur", StringComparison.OrdinalIgnoreCase), "").ToUpperInvariant().ToLowerInvariant();

        [GeneratedRegex("[^A-Za-z0-9]")]
        private static partial Regex NonAlphanumeric();
    }
}
