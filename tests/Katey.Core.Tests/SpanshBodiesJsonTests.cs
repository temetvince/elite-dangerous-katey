using Katey.Core.Exploration;
using Katey.Core.Routing;

namespace Katey.Core.Tests;

public sealed class SpanshBodiesJsonTests
{
    private const string Dump = /*lang=json,strict*/ """
        {"system":{"id64":5078665078537,"name":"Praea Euq BF-V b30-2","bodyCount":16,"bodies":[
          {"bodyId":0,"id64":5078665078537,"name":"Praea Euq BF-V b30-2","type":"Star","subType":"M (Red dwarf) Star","mainStar":true,"distanceToArrival":0.0,"solarMasses":0.273438,"surfaceTemperature":2731.0,"updateTime":"2022-11-18T11:44:36Z"},
          {"bodyId":5,"id64":1,"name":"Praea Euq BF-V b30-2 barycentre 5","type":"Barycentre","updateTime":"2022-11-18T11:44:36Z"},
          {"bodyId":6,"id64":216177860778862345,"name":"Praea Euq BF-V b30-2 1","type":"Planet","subType":"High metal content world","distanceToArrival":20.424933,"gravity":0.114447129601305,"isLandable":true,"atmosphereType":"Thin Carbon dioxide","volcanismType":"Major Rocky Magma","surfaceTemperature":377.139679,"surfacePressure":0.02,"earthMasses":0.001826,"terraformingState":"Candidate for terraforming","materials":{"Carbon":13.620528,"Iron":22.922901},"parents":[{"Null":5},{"Star":0}],"signals":{"signals":{"$SAA_SignalType_Biological;":2,"$SAA_SignalType_Geological;":4},"genuses":["$Codex_Ent_Bacterial_Genus_Name;","$Codex_Ent_Stratum_Genus_Name;"],"updateTime":"2024-01-01T00:00:00Z"},"rings":[{"name":"A Ring"}],"updateTime":"2022-11-18T11:44:36Z"}
        ]}}
        """;

    [Fact]
    public void Read_TurnsADumpIntoBodies_InTheJournalsSpellings()
    {
        var bodies = SpanshBodiesJson.Read(Dump);

        Assert.Equal(2, bodies.Count);
        var star = bodies[0];
        Assert.True(star.IsStar);
        Assert.Equal("M", star.Class);
        Assert.Equal(0.273438, star.Mass);
        var planet = bodies[1];
        Assert.Equal(6, planet.BodyId);
        Assert.Equal("High metal content body", planet.Class);
        Assert.Equal(0, planet.ParentStarId);
        Assert.Null(star.ParentStarId);
        Assert.True(planet.Landable);
        Assert.Equal(0.114447129601305, planet.GravityG);
        Assert.Equal(0.02 * 101325.0, planet.PressurePa!.Value, 1e-6);
        Assert.True(planet.Terraformable);
        Assert.True(planet.HasRings);
        Assert.Equal(22.922901, planet.Materials["Iron"]);
        Assert.Equal(2, planet.BiologicalSignals);
        Assert.Equal(4, planet.GeologicalSignals);
        Assert.Equal(["$Codex_Ent_Bacterial_Genus_Name;", "$Codex_Ent_Stratum_Genus_Name;"], planet.Genera);
        Assert.Equal(new DateTimeOffset(2022, 11, 18, 11, 44, 36, TimeSpan.Zero), planet.UpdatedAt);
    }

    [Theory]
    [InlineData("K (Yellow-Orange) Star", "K")]
    [InlineData("Neutron Star", "N")]
    [InlineData("White Dwarf (DA) Star", "DA")]
    [InlineData("Black Hole", "H")]
    [InlineData("Supermassive Black Hole", "SupermassiveBlackHole")]
    [InlineData("T Tauri Star", "TTS")]
    [InlineData("Wolf-Rayet N Star", "WN")]
    [InlineData("M (Red giant) Star", "M_RedGiant")]
    [InlineData("M (Red super giant) Star", "M_RedSuperGiant")]
    [InlineData("K (Yellow-Orange giant) Star", "K_OrangeGiant")]
    [InlineData("A (Blue-White super giant) Star", "A_BlueWhiteSuperGiant")]
    [InlineData("G (White-Yellow super giant) Star", "G_WhiteSuperGiant")]
    [InlineData("L (Brown dwarf) Star", "L")]
    [InlineData("MS-type Star", "MS")]
    public void CatalogueNames_SpellStarsAsTheJournalDoes(string catalogue, string journal)
        => Assert.Equal(journal, CatalogueNames.StarClass(catalogue));

    [Theory]
    [InlineData("Earth-like world", "Earthlike body")]
    [InlineData("Class II gas giant", "Sudarsky class II gas giant")]
    [InlineData("Rocky Ice world", "Rocky ice body")]
    [InlineData("Something new", "Something new")]
    public void CatalogueNames_SpellPlanetsAsTheJournalDoes(string catalogue, string journal)
        => Assert.Equal(journal, CatalogueNames.PlanetClass(catalogue));

    [Fact]
    public void Read_WithNoBodies_IsEmpty()
    {
        Assert.Empty(SpanshBodiesJson.Read(/*lang=json,strict*/ """{"system":{"name":"Nowhere"}}"""));
        Assert.Empty(SpanshBodiesJson.Read(/*lang=json,strict*/ """{}"""));
    }
}
