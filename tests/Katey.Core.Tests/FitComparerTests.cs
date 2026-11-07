using Katey.Core.Fleet;

namespace Katey.Core.Tests;

/// <summary>Checks the fit-to-ship comparison, the single-requirement lookup, hull names and cosmetic items.</summary>
public sealed class FitComparerTests
{
    private static readonly Engineering LongRange5 = new("FSD_LongRange", 5, "special_fsd_heavy");

    [Fact]
    public void Compare_ClassifiesEverySlot()
    {
        var fitting = new Fitting("f", "mandalay", "Dream",
        [
            new FitModule("FrameShiftDrive", "int_hyperdrive_size5_class5", LongRange5),
            new FitModule("PowerPlant", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 3, null)),
            new FitModule("MainEngines", "int_engine_size5_class5", new Engineering("Engine_Dirty", 5, "special_engine_overloaded")),
            new FitModule("Slot01_Size5", "int_cargorack_size5_class1", null),
            new FitModule("Slot02_Size4", "int_fuelscoop_size4_class5", null),
            new FitModule("TinyHardpoint1", "hpt_heatsinklauncher_turret_tiny", new Engineering("Misc_LightWeight", 2, null)),
            new FitModule("LargeHardpoint1", "hpt_beamlaser_gimbal_large", new Engineering("Weapon_Efficient", 5, "special_thermal_vent")),
        ]);
        var ship = new List<FitModule>
        {
            new("frameshiftdrive", "INT_HYPERDRIVE_SIZE5_CLASS5", new Engineering("FSD_LongRange", 5, null)),
            new("PowerPlant", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 2, null)),
            new("MainEngines", "int_engine_size5_class5", new Engineering("Engine_Dirty", 5, null, ExperimentalKnown: false)),
            new("Slot01_Size5", "int_cargorack_size5_class1", new Engineering("CargoRack_IncreasedCapacity", 5, null)),
            new("Slot02_Size4", "int_shieldgenerator_size4_class5", null),
            new("TinyHardpoint1", "hpt_heatsinklauncher_turret_tiny", null),
        };

        var comparison = FitComparer.Compare(fitting, ship);

        Assert.Equal(["Slot01_Size5"], comparison.Matching.Select(module => module.Slot));
        Assert.Equal(1, comparison.MatchCount);
        Assert.False(comparison.IsComplete);
        var kinds = comparison.Differences.ToDictionary(difference => difference.Required.Slot, difference => difference.Kind);
        Assert.Equal(DifferenceKind.DifferentExperimental, kinds["FrameShiftDrive"]);
        Assert.Equal(DifferenceKind.LowerGrade, kinds["PowerPlant"]);

        // The right grade, not rolled out, is its own difference; a legacy figure of 0 reads as complete.
        var wantG3 = new Fitting("g3", "anaconda", "g3", [new FitModule("PowerPlant", "int_powerplant_size8_class5", new Engineering("PowerPlant_Boosted", 3, null))]);
        Assert.Equal(DifferenceKind.UnfinishedGrade, Assert.Single(FitComparer.Compare(wantG3, [new FitModule("PowerPlant", "int_powerplant_size8_class5", new Engineering("PowerPlant_Boosted", 3, null, Quality: 0.5))]).Differences).Kind);
        Assert.Empty(FitComparer.Compare(wantG3, [new FitModule("PowerPlant", "int_powerplant_size8_class5", new Engineering("PowerPlant_Boosted", 3, null, Quality: 0))]).Differences);
        Assert.Equal(DifferenceKind.ExperimentalUnknown, kinds["MainEngines"]);
        Assert.Equal(DifferenceKind.DifferentModule, kinds["Slot02_Size4"]);
        Assert.Equal(DifferenceKind.NotEngineered, kinds["TinyHardpoint1"]);
        Assert.Equal(DifferenceKind.SlotEmpty, kinds["LargeHardpoint1"]);

        var fsd = comparison.Differences.Single(difference => difference.Required.Slot == "FrameShiftDrive");
        Assert.True(fsd.NeedsOnlyExperimental);
        Assert.True(fsd.NeedsEngineering);
        Assert.False(fsd.NeedsPurchase);
        var engines = comparison.Differences.Single(difference => difference.Required.Slot == "MainEngines");
        Assert.False(engines.NeedsEngineering);
        var beam = comparison.Differences.Single(difference => difference.Required.Slot == "LargeHardpoint1");
        Assert.True(beam.NeedsPurchase);
        Assert.True(beam.NeedsEngineering);
    }

    [Fact]
    public void Compare_TreatsFreeAndHullSpecificSpellings_AsTheItemTheFitNames()
    {
        var fitting = new Fitting("f", "ferdelance", "Taxi",
        [
            new FitModule("CargoHatch", "modularcargobaydoor", null),
            new FitModule("Slot05_Size5", "int_fighterbaymk2_size5_class1", null),
        ]);
        var ship = new List<FitModule>
        {
            new("CargoHatch", "modularcargobaydoorfdl", null),
            new("Slot03_Size5", "int_fighterbaymk2_size5_class1_free", null),
        };

        var comparison = FitComparer.Compare(fitting, ship);

        Assert.True(comparison.IsComplete);
        Assert.Equal(["CargoHatch", "Slot05_Size5"], comparison.Matching.Select(module => module.Slot));
    }

    [Fact]
    public void Compare_FindsModulesInOtherSlotsOfTheSameKind_AndClaimsEachOnce()
    {
        var overcharged = new Engineering("Weapon_Overcharged", 5, "special_incendiary_rounds");
        var fitting = new Fitting("f", "anaconda", "Swap",
        [
            new FitModule("Slot01_Size7", "int_cargorack_size6_class1", null),
            new FitModule("Slot02_Size6", "int_fuelscoop_size6_class5", null),
            new FitModule("MediumHardpoint1", "hpt_multicannon_gimbal_medium", overcharged),
            new FitModule("MediumHardpoint2", "hpt_multicannon_gimbal_medium", overcharged),
            new FitModule("Slot03_Size5", "int_shieldgenerator_size5_class5", null),
            new FitModule("Slot04_Size5", "int_repairer_size5_class5", null),
        ]);
        var ship = new List<FitModule>
        {
            new("Slot01_Size7", "int_fuelscoop_size6_class5", null),
            new("Slot02_Size6", "int_cargorack_size6_class1", null),
            new("MediumHardpoint1", "hpt_multicannon_gimbal_medium", overcharged),
            new("LargeHardpoint1", "hpt_multicannon_gimbal_medium", new Engineering("Weapon_Overcharged", 3, null)),
            new("Slot04_Size5", "int_shieldgenerator_size5_class5", null),
        };

        var comparison = FitComparer.Compare(fitting, ship);

        Assert.Equal(["Slot01_Size7", "Slot02_Size6", "MediumHardpoint1", "Slot03_Size5"], comparison.Matching.Select(module => module.Slot));
        var second = comparison.Differences.Single(difference => difference.Required.Slot == "MediumHardpoint2");
        Assert.Equal(DifferenceKind.LowerGrade, second.Kind);
        Assert.Equal("LargeHardpoint1", second.Fitted!.Slot);
        Assert.True(second.FoundElsewhere);
        // The repairer's slot holds the shield generator, which the shield requirement claimed, so it reads as empty.
        var repairer = comparison.Differences.Single(difference => difference.Required.Slot == "Slot04_Size5");
        Assert.Equal(DifferenceKind.SlotEmpty, repairer.Kind);
        Assert.Null(repairer.Fitted);
        Assert.False(repairer.FoundElsewhere);

        var stranger = FitComparer.Compare(fitting, [new("Slot01_Size7", "int_dockingcomputer_advanced", null)]);
        var cargo = stranger.Differences.Single(difference => difference.Required.Slot == "Slot01_Size7");
        Assert.Equal(DifferenceKind.DifferentModule, cargo.Kind);
        Assert.Equal("int_dockingcomputer_advanced", cargo.Fitted!.Item);
    }

    [Fact]
    public void SlotNames_ParseKindAndSize()
    {
        Assert.Equal(new SlotPlace(SlotKind.Core, 0), SlotNames.Parse("FrameShiftDrive"));
        Assert.Equal(new SlotPlace(SlotKind.Core, 0), SlotNames.Parse("CargoHatch"));
        Assert.Equal(new SlotPlace(SlotKind.Hardpoint, 1), SlotNames.Parse("SmallHardpoint2"));
        Assert.Equal(new SlotPlace(SlotKind.Hardpoint, 3), SlotNames.Parse("LargeHardpoint1"));
        Assert.Equal(new SlotPlace(SlotKind.Hardpoint, 4), SlotNames.Parse("HugeHardpoint1"));
        Assert.Equal(new SlotPlace(SlotKind.Utility, 0), SlotNames.Parse("TinyHardpoint4"));
        Assert.Equal(new SlotPlace(SlotKind.Optional, 7), SlotNames.Parse("slot01_size7"));
        Assert.Equal(new SlotPlace(SlotKind.Military, 0), SlotNames.Parse("Military02"));
        Assert.Equal(new SlotPlace(SlotKind.Other, 0), SlotNames.Parse("PaintJob"));
        Assert.Equal(new SlotPlace(SlotKind.Other, 0), SlotNames.Parse("ShipCockpit"));
    }

    [Fact]
    public void Find_PicksTheBestOwnedModule_OrNothing()
    {
        var required = new FitModule("FrameShiftDrive", "int_hyperdrive_size5_class5", LongRange5);
        var owned = new List<OwnedModule>
        {
            new("a", "int_hyperdrive_size5_class5", new Engineering("FSD_LongRange", 5, null, ExperimentalKnown: false), ModulePlace.Stored, "Deciat", "Farseer Inc", null),
            new("b", "int_hyperdrive_size5_class5", LongRange5, ModulePlace.Installed, "Sol", "Hauler", "type9"),
            new("c", "int_hyperdrive_size5_class5", null, ModulePlace.Stored, "Sol", "Abraham Lincoln", null),
        };

        var found = FitMatcher.Find(required, owned);
        Assert.Equal("b", found.Found?.Id);
        Assert.Equal(MatchQuality.Exact, found.Quality);

        var uncertain = FitMatcher.Find(required, owned.Take(1));
        Assert.Equal(MatchQuality.ExperimentalUnknown, uncertain.Quality);

        var none = FitMatcher.Find(required, owned.Skip(2));
        Assert.Null(none.Found);
        Assert.Equal(MatchQuality.Missing, none.Quality);
    }

    [Fact]
    public void ShipNames_KnowTheHulls_AndTidyTheRest()
    {
        Assert.Equal("Anaconda Mk II", ShipNames.Display("anacondamkii"));
        Assert.Equal("Cobra Mk III", ShipNames.Display("CobraMkIII"));
        Assert.Equal("Federal Corvette", ShipNames.Display("federation_corvette"));
        Assert.Equal("Type-9 Heavy", ShipNames.Display("type9"));
        Assert.Equal("Lynx Highliner", ShipNames.Display("mediumtransport01"));
        Assert.Equal("Caspian Explorer", ShipNames.Display("Explorer_NX"));
        Assert.Equal("Newship Mk IV", ShipNames.Display("newship_mkiv"));
        Assert.Equal("Anaconda Mk II", ShipNames.Prefer("anacondamkii", "anacondamkii"));
        Assert.Equal("Anaconda Mk II", ShipNames.Prefer("anacondamkii", null));
        Assert.Equal("Borrowed Name", ShipNames.Prefer("anacondamkii", "Borrowed Name"));
    }

    [Fact]
    public void SlotNames_SpaceOutTheJournalsSlots()
    {
        Assert.Equal("Cargo Hatch", SlotNames.Display("CargoHatch"));
        Assert.Equal("Large Hardpoint 3", SlotNames.Display("LargeHardpoint3"));
        Assert.Equal("Slot 01 Size 5", SlotNames.Display("Slot01_Size5"));
        Assert.Equal("Frame Shift Drive", SlotNames.Display("FrameShiftDrive"));
        Assert.Equal("Military 02", SlotNames.Display("Military02"));
        Assert.Equal("Planetary Approach Suite", SlotNames.Display("PlanetaryApproachSuite"));
        Assert.Equal("", SlotNames.Display(""));
    }

    [Fact]
    public void EdsyExport_WritesALoadoutEdsyCanRead()
    {
        var catalog = Catalog.LoadEmbedded();
        var modules = new List<FitModule>
        {
            new("FrameShiftDrive", "int_hyperdrive_size5_class5", LongRange5),
            new("CargoHatch", "modularcargobaydoor", null),
        };

        var json = Edsy.EdsyExport.LoadoutJson("mandalay", "Dream", "DR-01", modules);
        var parsed = new Edsy.EdsyImport(catalog).Parse(json);

        Assert.True(parsed.Ok);
        Assert.Equal("mandalay", parsed.Loadout!.Ship);
        Assert.Equal("Dream", parsed.Loadout.ShipName);
        Assert.Equal(2, parsed.Loadout.AllModules.Count);
        var fsd = Assert.Single(parsed.Loadout.Modules);
        Assert.Equal("FSD_LongRange", fsd.Blueprint);
        Assert.Equal(5, fsd.Level);
        Assert.Equal("special_fsd_heavy", fsd.Experimental);
        Assert.Contains("\"event\":\"Loadout\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ModuleCatalog_FlagsCosmeticItems()
    {
        Assert.True(ModuleCatalog.IsCosmetic("shipkita_bumper2"));
        Assert.True(ModuleCatalog.IsCosmetic("anacondamkii_shipkita_bumper2"));
        Assert.True(ModuleCatalog.IsCosmetic("mandalay_shipkita_wings1"));
        Assert.True(ModuleCatalog.IsCosmetic("anacondamkii_cockpit"));
        Assert.True(ModuleCatalog.IsCosmetic("EngineCustomisation_Cyan"));
        Assert.True(ModuleCatalog.IsCosmetic("voicepack_verity"));
        Assert.True(ModuleCatalog.IsCosmetic("paintjob_anaconda_tactical_grey"));
        Assert.True(ModuleCatalog.IsCosmetic("nameplate_explorer01_white"));
        Assert.False(ModuleCatalog.IsCosmetic("int_hyperdrive_size5_class5"));
        Assert.False(ModuleCatalog.IsCosmetic("anacondamkii_armour_grade3"));
        Assert.False(ModuleCatalog.IsCosmetic("hpt_beamlaser_gimbal_large"));
    }
}
