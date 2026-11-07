using Katey.Core.Exploration;
using Katey.Core.Search;

namespace Katey.Core.Tests;

/// <summary>Checks the text index's ranking and the galactic distance helper.</summary>
public sealed class SearchTests
{
    [Fact]
    public void Query_RanksExactThenPrefixThenWordPrefixThenContains_AndRequiresEveryWord()
    {
        var index = new TextIndex<string>();
        index.Add("Beam Laser", "bl");
        index.Add("Beam Laser Turret", "blt");
        index.Add("Pulse Laser", "pl");
        index.Add("Mining Laser", "ml");
        index.Add("Frame Shift Drive", "fsd", "fsd core");
        index.Add("Datamined Wake Exceptions", "dwe");

        var hits = index.Query("beam laser", 10);
        Assert.Equal(["bl", "blt"], hits.Select(hit => hit.Item));
        Assert.Equal(100, hits[0].Score);
        Assert.Equal(80, hits[1].Score);

        var laser = index.Query("  LASER ", 10);
        Assert.Equal(["bl", "blt", "ml", "pl"], laser.Select(hit => hit.Item));
        Assert.All(laser, hit => Assert.Equal(60, hit.Score));

        Assert.Equal(["dwe"], index.Query("wake exc", 10).Select(hit => hit.Item));
        Assert.Equal(["dwe"], index.Query("amined", 10).Select(hit => hit.Item));
        Assert.Equal(40, index.Query("amined", 10)[0].Score);
        Assert.Empty(index.Query("beam pulse", 10));
        Assert.Empty(index.Query("   ", 10));
    }

    [Fact]
    public void Query_FallsBackToSecondaryText_AtALowerScore_AndHonoursTheLimit()
    {
        var index = new TextIndex<string>();
        index.Add("Frame Shift Drive", "fsd", "fsd core");
        index.Add("Fuel Scoop", "fs", "fs optional");

        var hit = Assert.Single(index.Query("fsd", 10));
        Assert.Equal("fsd", hit.Item);
        Assert.Equal(60, hit.Score);

        Assert.Single(index.Query("f", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => index.Query("f", 0));
        Assert.Throws<ArgumentException>(() => index.Add(" ", "blank"));
    }

    [Fact]
    public void GalacticPosition_MeasuresStraightLineDistance()
    {
        var deciat = new GalacticPosition(122.625, -0.8125, -47.28125);
        var koine = new GalacticPosition(107.40625, -40.59375, -71.5625);

        Assert.Equal(0, deciat.DistanceTo(deciat));
        Assert.Equal(131.5, GalacticPosition.Sol.DistanceTo(deciat), 0.1);
        Assert.Equal(deciat.DistanceTo(koine), koine.DistanceTo(deciat));
        Assert.Equal(49.1, deciat.DistanceTo(koine), 0.1);
    }

    [Fact]
    public void Catalog_AnswersTheCrossLinkQueries()
    {
        var catalog = Catalog.LoadEmbedded();

        Assert.Equal(new GalacticPosition(122.625, -0.8125, -47.28125), catalog.Engineers["Felicity Farseer"].Position);

        var types = catalog.ModuleTypesWithBlueprint("Weapon_Efficient");
        Assert.Contains(types, type => type.Code == "bl");
        Assert.Contains(types, type => type.Code == "mc");
        Assert.Empty(catalog.ModuleTypesWithBlueprint("Nope"));

        var uses = catalog.MaterialUses("Datamined Wake Exceptions");
        Assert.Contains(uses, use => use.Blueprint == "FSD_LongRange" && use.Grade == 5 && use.Count == 1);
        Assert.Equal(uses.Select(use => catalog.Blueprints[use.Blueprint].Name).Order(StringComparer.Ordinal), uses.Select(use => catalog.Blueprints[use.Blueprint].Name));
        Assert.Empty(catalog.MaterialUses("Nothing"));

        Assert.Contains(catalog.ExperimentalsUsing("Atypical Disrupted Wake Echoes"), effect => effect.Key == "special_fsd_heavy");

        var offers = catalog.OffersOf("Felicity Farseer");
        Assert.Contains(offers, offer => offer.ModuleType == "fsd" && offer.Blueprint == "FSD_LongRange" && offer.MaxGrade == 5);
        Assert.Contains(offers, offer => offer.ModuleType == "t" && offer.Blueprint == "Engine_Dirty" && offer.MaxGrade == 3);
        Assert.DoesNotContain(offers, offer => offer.ModuleType == "bl");
        Assert.Empty(catalog.OffersOf("Nobody"));
    }
}
