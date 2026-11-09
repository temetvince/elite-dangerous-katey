using Katey.Core.Exploration;

namespace Katey.Core.Tests;

/// <summary>Checks star classification, the cartographics estimate, and the species predictor.</summary>
public sealed class ExplorationTests
{
    private static readonly ExobiologyPredictor Predictor = ExobiologyPredictor.LoadEmbedded();

    [Theory]
    [InlineData("K", StarKind.Scoopable)]
    [InlineData("g", StarKind.Scoopable)]
    [InlineData("M_RedGiant", StarKind.Scoopable)]
    [InlineData("A_BlueWhiteSuperGiant", StarKind.Scoopable)]
    [InlineData("M_Unknown", StarKind.Other)]
    [InlineData("N", StarKind.Neutron)]
    [InlineData("DA", StarKind.WhiteDwarf)]
    [InlineData("DCV", StarKind.WhiteDwarf)]
    [InlineData("H", StarKind.BlackHole)]
    [InlineData("SupermassiveBlackHole", StarKind.BlackHole)]
    [InlineData("L", StarKind.Other)]
    [InlineData("TTS", StarKind.Other)]
    [InlineData("", StarKind.Other)]
    [InlineData(null, StarKind.Other)]
    public void StarClasses_ClassifyTheJournalStrings(string? starClass, StarKind expected)
        => Assert.Equal(expected, StarClasses.Classify(starClass));

    [Theory]
    [InlineData("K", "Class K star")]
    [InlineData("M_RedGiant", "Class M red giant star")]
    [InlineData("K_OrangeGiant", "Class K orange giant star")]
    [InlineData("L", "Class L brown dwarf")]
    [InlineData("TTS", "T Tauri star")]
    [InlineData("WN", "Wolf-Rayet star, class WN")]
    [InlineData("N", "Neutron star")]
    [InlineData("DA", "White dwarf, class DA")]
    [InlineData("H", "Black hole")]
    [InlineData("Y", "Class Y brown dwarf")]
    [InlineData("X", "Class X star")]
    [InlineData(null, "Star")]
    public void StarClasses_DescribeAStarForAReader(string? starClass, string expected)
        => Assert.Equal(expected, StarClasses.Describe(starClass));

    [Fact]
    public void SamplingTime_CostsALandingByItsSpeciesCount()
    {
        Assert.Equal(SamplingTime.ApproachMinutes, SamplingTime.GroundMinutes(0));
        Assert.Equal(SamplingTime.ApproachMinutes, SamplingTime.GroundMinutes(-3));
        // One species: four minutes down and back up, three for the samples, whatever the genus. Each further species
        // adds its samples and a minute to find it.
        Assert.Equal(7, SamplingTime.GroundMinutes(1));
        Assert.Equal(11, SamplingTime.GroundMinutes(2));
        Assert.Equal(15, SamplingTime.GroundMinutes(3));
        Assert.Equal(35, SamplingTime.GroundMinutes(8));
        Assert.Equal(95_000_000 / 7.0, SamplingTime.CreditsPerMinute(95_000_000, 7), 1e-6);
        Assert.Equal(0, SamplingTime.CreditsPerMinute(0, 7));
        // Eight cheap species pay more in total but less a minute than one Stratum with the bonus.
        Assert.True(SamplingTime.CreditsPerMinute(217_000_000, SamplingTime.GroundMinutes(8)) < SamplingTime.CreditsPerMinute(95_000_000, SamplingTime.GroundMinutes(1)));
    }

    [Fact]
    public void SamplingTime_PacesTheBestLanding_AndTheOddsOfAJackpot()
    {
        // A Stratum beside seven cheap species: the best landing takes the Stratum and leaves, so its pace is the Stratum's own.
        var best = SamplingTime.BestCreditsPerMinute([95_000_000, .. Enumerable.Repeat(7_500_000L, 7)]);
        Assert.Equal(95_000_000 / 7.0, best, 1e-6);
        // Two good species beat one: the second raises the average, so it is taken.
        Assert.True(SamplingTime.BestCreditsPerMinute([95_000_000, 95_000_000]) > best);
        Assert.Equal(0, SamplingTime.BestCreditsPerMinute([]));
        Assert.Equal(0, SamplingTime.BestCreditsPerMinute([0, -5]));

        Assert.True(SamplingTime.KeepsPaceAlone(95_000_000, 8_000_000));
        Assert.False(SamplingTime.KeepsPaceAlone(19_000_000, 8_000_000));
        Assert.False(SamplingTime.KeepsPaceAlone(51_500_000, 8_000_000));
        Assert.True(SamplingTime.KeepsPaceAlone(59_500_000, 8_000_000));

        // Eight signals at one chance in ten each is better than even odds of at least one.
        Assert.Equal(0, SamplingTime.ChanceOfAny([]));
        Assert.Equal(0.57, SamplingTime.ChanceOfAny(Enumerable.Repeat(0.1, 8)), 0.01);
        Assert.Equal(1, SamplingTime.ChanceOfAny([0.2, 1.0]));
    }

    [Theory]
    [InlineData(null, GravityBand.Light)]
    [InlineData(0.3, GravityBand.Light)]
    [InlineData(0.99, GravityBand.Light)]
    [InlineData(1.0, GravityBand.High)]
    [InlineData(1.49, GravityBand.High)]
    [InlineData(1.6, GravityBand.Extreme)]
    public void SurfaceGravity_BandsALanding(double? gravityG, GravityBand expected)
        => Assert.Equal(expected, SurfaceGravity.Band(gravityG));

    [Fact]
    public void BodyValue_RisesWithTerraformingMappingAndFirstDiscovery()
    {
        var plain = BodyValue.Planet("High metal content body", terraformable: false, 0.5, firstDiscovered: false, mapped: false, firstMapped: false);
        var terraformable = BodyValue.Planet("High metal content body", terraformable: true, 0.5, firstDiscovered: false, mapped: false, firstMapped: false);
        var mapped = BodyValue.Planet("High metal content body", terraformable: true, 0.5, firstDiscovered: false, mapped: true, firstMapped: false);
        var first = BodyValue.Planet("High metal content body", terraformable: true, 0.5, firstDiscovered: true, mapped: true, firstMapped: true);

        Assert.True(plain < terraformable);
        Assert.True(terraformable < mapped);
        Assert.True(mapped < first);
        Assert.Equal(500, BodyValue.Planet("Icy body", terraformable: false, 0.001, firstDiscovered: false, mapped: false, firstMapped: false));
        Assert.True(BodyValue.Star("N", 1.5, firstDiscovered: false) > BodyValue.Star("K", 0.8, firstDiscovered: false));
        Assert.True(BodyValue.Star("K", 0.8, firstDiscovered: true) > BodyValue.Star("K", 0.8, firstDiscovered: false));
    }

    [Fact]
    public void Predictor_KnowsTheFifteenGenera_AndNamesSpecies()
    {
        Assert.Equal(15, Predictor.Genera.Count);
        Assert.Contains("$Codex_Ent_Bacterial_Genus_Name;", Predictor.Genera);
        Assert.Contains("$Codex_Ent_Tussocks_Genus_Name;", Predictor.Genera);
        Assert.Equal("Tussock Stigmasis", Predictor.SpeciesName("$Codex_Ent_Tussocks_13_Name;"));
        Assert.Equal("Bacterium Aurasus", Predictor.SpeciesName("$Codex_Ent_Bacterial_01_Name;"));
        Assert.Null(Predictor.SpeciesName("nope"));
    }

    [Fact]
    public void Predictor_FavoursSpeciesSeenUnderTheBodysConditions()
    {
        // A thin carbon dioxide rocky body is classic Bacterium Aurasus territory.
        var body = new BodyConditions("Rocky body", "thin carbon dioxide atmosphere", "", 0.4, 210, 900);
        var prediction = Predictor.Predict("$Codex_Ent_Bacterial_Genus_Name;", body);

        Assert.NotEmpty(prediction.Candidates);
        Assert.Equal(1.0, prediction.Candidates.Sum(candidate => candidate.Probability), 6);
        Assert.All(prediction.Candidates, candidate => Assert.True(candidate.Probability >= 0.01));
        Assert.Equal("$Codex_Ent_Bacterial_01_Name;", prediction.Candidates[0].Species);
        Assert.True(prediction.ExpectedReward >= prediction.MinimumReward);
        Assert.True(prediction.ExpectedReward <= prediction.MaximumReward);

        // Journal spellings differ from Bioforge's; both must land on the same canonical keys.
        var alt = Predictor.Predict("$Codex_Ent_Bacterial_Genus_Name;", body with { Atmosphere = "Thin CarbonDioxide", PlanetClass = "Rocky Body" });
        Assert.Equal(prediction.Candidates[0].Species, alt.Candidates[0].Species);
        Assert.Equal(prediction.Candidates[0].Probability, alt.Candidates[0].Probability, 9);
    }

    [Fact]
    public void Predictor_SaysWhichGeneraHaveBeenFoundUnderTheConditions()
    {
        var rocky = new BodyConditions("Rocky body", "thin carbon dioxide atmosphere", "", 0.4, 210, 900);
        var possible = Predictor.PossibleGenera(rocky);
        Assert.Contains("$Codex_Ent_Bacterial_Genus_Name;", possible);
        Assert.True(possible.Count > 1);
        Assert.True(Predictor.CanLive("$Codex_Ent_Bacterial_Genus_Name;", rocky));
        Assert.False(Predictor.CanLive("$Codex_Ent_Nope_Genus_Name;", rocky));

        var furnace = new BodyConditions("Rocky body", "thin carbon dioxide atmosphere", "", 0.4, 9000, 900);
        Assert.Empty(Predictor.PossibleGenera(furnace));
    }

    [Fact]
    public void Predictor_WeighsTheGeneraThatCouldLiveThere()
    {
        var rocky = new BodyConditions("Rocky body", "thin carbon dioxide atmosphere", "", 0.4, 210, 900);
        var likely = Predictor.LikelyGenera(rocky);
        Assert.True(likely.Count > 1);
        Assert.Equal(1.0, likely.Sum(genus => genus.Probability), 9);
        Assert.Equal(likely.OrderByDescending(genus => genus.Probability).Select(genus => genus.Genus), likely.Select(genus => genus.Genus));
        Assert.All(likely, genus => Assert.NotEmpty(genus.Prediction.Candidates));
        Assert.All(likely, genus => Assert.Contains(genus.Genus, Predictor.PossibleGenera(rocky)));
        Assert.Empty(Predictor.LikelyGenera(rocky with { TemperatureK = 9000 }));
    }

    [Fact]
    public void Predictor_ReturnsNothingForAnUnknownGenus()
        => Assert.Empty(Predictor.Predict("$Codex_Ent_Nope_Genus_Name;", new BodyConditions("Rocky body", "", "", 1, 300, null)).Candidates);
}
