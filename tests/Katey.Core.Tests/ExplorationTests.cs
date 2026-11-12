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
    [InlineData("K", StarFamily.MainSequence)]
    [InlineData("M_RedGiant", StarFamily.MainSequence)]
    [InlineData("L", StarFamily.BrownDwarf)]
    [InlineData("TTS", StarFamily.TTauri)]
    [InlineData("AeBe", StarFamily.HerbigAeBe)]
    [InlineData("WN", StarFamily.WolfRayet)]
    [InlineData("CN", StarFamily.CarbonStar)]
    [InlineData("N", StarFamily.Neutron)]
    [InlineData("DA", StarFamily.WhiteDwarf)]
    [InlineData("H", StarFamily.BlackHole)]
    [InlineData("RoguePlanet", StarFamily.RoguePlanet)]
    [InlineData("X", StarFamily.Other)]
    [InlineData(null, StarFamily.Unknown)]
    public void StarClasses_SortAClassIntoItsFamily(string? starClass, StarFamily expected)
        => Assert.Equal(expected, StarClasses.Family(starClass));

    [Theory]
    [InlineData("M_RedGiant", "RedGiant")]
    [InlineData("A_BlueWhiteSuperGiant", "BlueWhiteSuperGiant")]
    [InlineData("K", null)]
    [InlineData("M_", null)]
    [InlineData(null, null)]
    public void StarClasses_ReadTheGiantSuffix(string? starClass, string? expected)
        => Assert.Equal(expected, StarClasses.Giant(starClass));

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
    public void Predictor_AnswersTheSameBodyFromItsMemo()
    {
        var body = new BodyConditions("Rocky body", "thin carbon dioxide atmosphere", "", 0.4, 210, 900);
        var again = new BodyConditions("Rocky body", "thin carbon dioxide atmosphere", "", 0.4, 210, 900);

        Assert.Same(Predictor.LikelyGenera(body), Predictor.LikelyGenera(again));
        Assert.Same(Predictor.PossibleGenera(body), Predictor.PossibleGenera(again));
        Assert.Same(Predictor.Predict("$Codex_Ent_Bacterial_Genus_Name;", body), Predictor.Predict("$Codex_Ent_Bacterial_Genus_Name;", again));
        Assert.NotSame(Predictor.LikelyGenera(body), Predictor.LikelyGenera(body with { GravityG = 0.5 }));
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
