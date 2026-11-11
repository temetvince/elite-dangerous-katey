using Katey.Core.Edsy;

namespace Katey.Core.Tests;

/// <summary>Checks the EDSY export parser against two real exports and against bad input.</summary>
public sealed class EdsyImportTests
{
    private static readonly EdsyImport Import = new(Catalog.LoadEmbedded());

    [Fact]
    public void Parse_Anaconda_FindsElevenEngineeredModules()
    {
        var result = Import.Parse(TestData.Anaconda);

        Assert.True(result.Ok, result.Error?.ToString());
        var loadout = result.Loadout!;
        Assert.Equal("Borrowed Time", loadout.ShipName);
        Assert.Equal(11, loadout.Modules.Count);
        Assert.Empty(loadout.Warnings);
        Assert.Equal(6, loadout.Modules.Count(module => module.ModuleType == "sb" && module.Blueprint == "ShieldBooster_HeavyDuty" && module.Experimental == "special_shieldbooster_chunky"));
        Assert.Contains(loadout.Modules, module => module.ModuleType == "bsg" && module.Blueprint == "ShieldGenerator_Reinforced" && module.Level == 3);
        Assert.Contains(loadout.Modules, module => module.ModuleType == "fsd" && module.Level == 5 && module.Experimental == "special_fsd_heavy");
        Assert.Contains(loadout.Modules, module => module.ModuleType == "scb" && module.Blueprint == "ShieldCellBank_Specialised");
    }

    [Fact]
    public void Parse_Mandalay_FindsTwoModules_AndTheBareLoadoutToo()
    {
        var result = Import.Parse(TestData.Mandalay);

        Assert.True(result.Ok);
        Assert.Equal(2, result.Loadout!.Modules.Count);
        Assert.Equal("Ironic PentamEter", result.Loadout.ShipName);

        using var document = System.Text.Json.JsonDocument.Parse(TestData.Mandalay);
        var bare = document.RootElement[0].GetProperty("data").GetRawText();
        Assert.True(Import.Parse(bare).Ok);
    }

    [Fact]
    public void Parse_ReadsTheJumpProfile_WhenTheExportCarriesTheMasses()
    {
        var withMasses = Import.Parse(/*lang=json,strict*/ """{"event":"Loadout","Ship":"Mandalay","ShipName":"Dream fit","UnladenMass":400.5,"FuelCapacity":{"Main":32,"Reserve":0.5},"Modules":[{"Slot":"FrameShiftDrive","Item":"Int_Hyperdrive_Size5_Class5","Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"ExperimentalEffect":"special_fsd_heavy","Modifiers":[{"Label":"FSDOptimalMass","Value":1692.6,"OriginalValue":1050},{"Label":"MaxFuelPerJump","Value":5.2,"OriginalValue":5}]}},{"Slot":"Slot05_Size3","Item":"int_guardianfsdbooster_size3"}]}""");
        Assert.True(withMasses.Ok, withMasses.Error?.ToString());
        var profile = withMasses.Loadout!.Profile;
        Assert.NotNull(profile);
        Assert.Equal(1692.6, profile.OptimalMass);
        Assert.Equal(5.2, profile.MaxFuelPerJump);
        Assert.Equal(401.0, profile.BaseMass, 1e-9);
        Assert.Equal(32, profile.TankSize);
        Assert.Equal(0.5, profile.InternalTankSize);
        Assert.Equal(7.75, profile.RangeBoost);
        Assert.Equal(4, profile.SuperchargeMultiplier);

        // The same numbers come straight from the text, catalog or not, in any shape the import accepts.
        var fromText = EdsyImport.ProfileOf(/*lang=json,strict*/ """[{"header":{"appName":"EDSY"},"data":{"event":"Loadout","Ship":"Mandalay","UnladenMass":400.5,"FuelCapacity":{"Main":32,"Reserve":0.5},"Modules":[{"Slot":"FrameShiftDrive","Item":"Int_Hyperdrive_Size5_Class5","Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"Modifiers":[{"Label":"FSDOptimalMass","Value":1692.6}]}}]}}]""");
        Assert.Equal(1692.6, fromText!.OptimalMass);
        Assert.Equal(401.0, fromText.BaseMass, 1e-9);
        Assert.Null(EdsyImport.ProfileOf("not json"));
        Assert.Null(EdsyImport.ProfileOf(""));

        // Stock figures stand in when the drive carries no modifiers; no masses or no drive means no profile.
        var stock = Import.Parse(/*lang=json,strict*/ """{"event":"Loadout","Ship":"Mandalay","UnladenMass":400,"FuelCapacity":{"Main":32},"Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size5_class5"}]}""").Loadout!.Profile;
        Assert.NotNull(stock);
        Assert.Equal(1050, stock.OptimalMass);
        Assert.Equal(400, stock.BaseMass);
        Assert.Null(Import.Parse(/*lang=json,strict*/ """{"event":"Loadout","Ship":"Mandalay","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size5_class5"}]}""").Loadout!.Profile);
        Assert.Null(Import.Parse(/*lang=json,strict*/ """{"event":"Loadout","Ship":"Mandalay","UnladenMass":400,"FuelCapacity":{"Main":32},"Modules":[{"Slot":"PowerPlant","Item":"int_powerplant_size5_class5"}]}""").Loadout!.Profile);
    }

    [Fact]
    public void Parse_RejectsBadInputWithoutThrowing()
    {
        Assert.Equal(EdsyParseError.Empty, Import.Parse("   ").Error);
        Assert.Equal(EdsyParseError.NotJson, Import.Parse("not json").Error);
        Assert.Equal(EdsyParseError.NoLoadout, Import.Parse("[]").Error);
        Assert.Equal(EdsyParseError.NoModules, Import.Parse(/*lang=json,strict*/ """{"event":"Loadout"}""").Error);
    }

    [Fact]
    public void Parse_SkipsUnknownBlueprints_DropsUnknownEffects_AndFlagsUnknownItems()
    {
        const string Text = /*lang=json,strict*/ """
            {"event":"Loadout","Ship":"x","Modules":[
              {"Slot":"A","Item":"int_engine_size5_class5"},
              {"Slot":"B","Item":"int_engine_size5_class5","Engineering":{"BlueprintName":"Nope","Level":1}},
              {"Slot":"C","Item":"int_engine_size5_class5","Engineering":{"BlueprintName":"Engine_Dirty","Level":9}},
              {"Slot":"D","Item":"int_engine_size5_class5","Engineering":{"BlueprintName":"Engine_Dirty","Level":2,"ExperimentalEffect":"special_bogus"}},
              {"Slot":"E","Item":"int_mystery_size5_class5","Engineering":{"BlueprintName":"Engine_Dirty","Level":2}}
            ]}
            """;

        var result = Import.Parse(Text);

        Assert.True(result.Ok);
        var loadout = result.Loadout!;
        Assert.Equal("x", loadout.ShipName);
        Assert.Equal(2, loadout.Modules.Count);
        Assert.Null(loadout.Modules[0].Experimental);
        Assert.Null(loadout.Modules[1].ModuleType);
        Assert.Equal(4, loadout.Warnings.Count);
    }
}
