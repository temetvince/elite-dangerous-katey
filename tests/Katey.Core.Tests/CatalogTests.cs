using Katey.Core.Data;

namespace Katey.Core.Tests;

/// <summary>Checks that the embedded tables load, validate, and answer the lookups the app relies on.</summary>
public sealed class CatalogTests
{
    private static readonly Catalog Catalog = Catalog.LoadEmbedded();

    [Fact]
    public void LoadEmbedded_HasTheExpectedTableSizes()
    {
        Assert.Equal(146, Catalog.Materials.Count);
        Assert.Equal(80, Catalog.Blueprints.Count);
        Assert.Equal(91, Catalog.Experimentals.Count);
        Assert.Equal(93, Catalog.ModuleTypes.Count);
        Assert.Equal(25, Catalog.Engineers.Count);
        Assert.Equal(50, Catalog.Unlocks.Count);
        Assert.Equal(29, Catalog.Unlocks.Values.Count(unlock => unlock.Broker == BrokerKind.Guardian));
    }

    [Fact]
    public void EngineerableModuleTypes_ExcludesTypesWithoutBlueprints_AndStartsWithCore()
    {
        var types = Catalog.EngineerableModuleTypes();

        Assert.Equal(45, types.Count);
        Assert.Equal(ModuleGroup.Core, types[0].Group);
        Assert.DoesNotContain(types, type => type.Code == "gfsb");
    }

    [Fact]
    public void Blueprint_GradesAndClamping()
    {
        var scb = Catalog.Blueprints["ShieldCellBank_Specialised"];
        var fsd = Catalog.Blueprints["FSD_LongRange"];

        Assert.Equal([1, 2, 3, 4], scb.GradeNumbers);
        Assert.Equal(4, scb.MaxGrade);
        Assert.Equal(4, scb.ClampGrade(5));
        Assert.Equal(5, fsd.MaxGrade);
        Assert.Equal(new Dictionary<string, int> { ["Arsenic"] = 1, ["ChemicalManipulators"] = 1, ["DataminedWake"] = 1 }, fsd.Grades[5]);
    }

    [Fact]
    public void EngineersFor_ReadsTheGradeTable()
    {
        Assert.Contains("Felicity Farseer", Catalog.EngineersFor("fsd", "FSD_LongRange", 5));
        Assert.Empty(Catalog.EngineersFor("scb", "FSD_LongRange", 1));
        Assert.Empty(Catalog.EngineersFor("fsd", "FSD_LongRange", 6));
    }

    [Fact]
    public void Materials_AreKeyedBySymbol_CaseInsensitively()
    {
        Assert.Equal("Datamined Wake Exceptions", Catalog.Materials["DATAMINEDWAKE"].Name);
        Assert.Equal("Iron", Catalog.Materials["iron"].Name);
        Assert.False(Catalog.Materials.ContainsKey("Datamined Wake Exceptions"));
    }

    [Fact]
    public void Engineers_AreFoundByNameAndById()
    {
        var farseer = Catalog.Engineers["Felicity Farseer"];
        Assert.Equal(300100, farseer.Id);
        Assert.Same(farseer, Catalog.EngineersById[300100]);
        Assert.Equal(Catalog.Engineers.Count, Catalog.EngineersById.Count);
    }

    [Fact]
    public void Unlocks_NameTheirModuleTypes_AndAreFoundByTypeAndMaterial()
    {
        var booster = Catalog.Unlocks["guardian_fsd_booster"];
        Assert.Equal(BrokerKind.Guardian, booster.Broker);
        Assert.Equal(UnlockCategory.Modules, booster.Category);
        Assert.Equal(UnlockCategory.Fighters, Catalog.Unlocks["trident_fighter"].Category);
        Assert.Equal(UnlockCategory.ModifiedWeapons, Catalog.Unlocks["modified_gauss_cannon_fixed_small"].Category);
        Assert.Equal(UnlockCategory.PreEngineered, Catalog.Unlocks["sirius_modified_heat_sink_launcher"].Category);
        Assert.Equal(UnlockCategory.Weapons, Catalog.Unlocks["enzyme_missile_rack"].Category);
        Assert.Equal("gfsb", booster.ModuleType);
        Assert.Equal(21, booster.Materials["Guardian_PowerCell"]);
        Assert.Equal(8, booster.Commodities["HN Shock Mount"]);
        Assert.True(booster.IsDetectable);
        Assert.True(booster.Covers("Int_GuardianFSDBooster_Size5"));
        Assert.False(booster.Covers("int_guardianpowerplant_size5_class1"));
        Assert.Equal([booster], Catalog.UnlocksFor("gfsb"));
        Assert.Contains(booster, Catalog.UnlocksUsing("FocusCrystals"));
        Assert.False(Catalog.Unlocks["engineered_fsd_v1"].IsDetectable);
        Assert.Equal(BrokerKind.Human, Catalog.Unlocks["shock_cannon_fixed_small"].Broker);
    }

    [Fact]
    public void FromRows_RejectsAnUnlockWithABadMaterialBrokerOrType()
    {
        var tables = Snapshot();
        var unlocks = new Dictionary<string, UnlockRow>(StringComparer.Ordinal)
        {
            ["bad"] = new("Bad", "thargoid", "toys", "nope", ["Hpt_Upper"], new Dictionary<string, int> { ["Guardian_PowerCel"] = 1 }, new Dictionary<string, int>()),
        };

        var exception = Assert.Throws<CatalogException>(() => Catalog.FromRows(tables.Materials, tables.Blueprints, tables.Experimentals, tables.ModuleTypes, tables.Engineers, unlocks));

        Assert.Contains(exception.Problems, problem => problem.Contains("Guardian_PowerCel", StringComparison.Ordinal));
        Assert.Contains(exception.Problems, problem => problem.Contains("guardian or human", StringComparison.Ordinal));
        Assert.Contains(exception.Problems, problem => problem.Contains("category must be", StringComparison.Ordinal));
        Assert.Contains(exception.Problems, problem => problem.Contains("unknown module type", StringComparison.Ordinal));
        Assert.Contains(exception.Problems, problem => problem.Contains("lower case", StringComparison.Ordinal));
    }

    [Fact]
    public void FromRows_RejectsAMisspeltMaterial()
    {
        var tables = Snapshot();
        var effect = tables.Experimentals["special_lock_breaker"];
        tables.Experimentals["special_lock_breaker"] = effect with
        {
            Components = effect.Components.ToDictionary(pair => pair.Key.Replace("Encryptors", "Encyptors", StringComparison.Ordinal), pair => pair.Value),
        };

        var exception = Assert.Throws<CatalogException>(() => Catalog.FromRows(tables.Materials, tables.Blueprints, tables.Experimentals, tables.ModuleTypes, tables.Engineers));

        Assert.Contains(exception.Problems, problem => problem.Contains("AdaptiveEncyptors", StringComparison.Ordinal));
    }

    [Fact]
    public void FromRows_RejectsAnUnknownEngineerAndAnUnusedOne()
    {
        var tables = Snapshot();
        var fsd = tables.ModuleTypes["fsd"];
        var grades = fsd.Blueprints["FSD_LongRange"].ToDictionary(pair => pair.Key, pair => pair.Value);
        grades["5"] = [.. grades["5"], "Felicty Farseer"];
        tables.ModuleTypes["fsd"] = fsd with
        {
            Blueprints = fsd.Blueprints.ToDictionary(pair => pair.Key, pair => pair.Key == "FSD_LongRange" ? grades : pair.Value),
        };
        tables.Engineers["Nobody"] = new EngineerRow("Nobody", 999999, "Sol", "Nowhere", "bubble", 0, 0, 0);

        var exception = Assert.Throws<CatalogException>(() => Catalog.FromRows(tables.Materials, tables.Blueprints, tables.Experimentals, tables.ModuleTypes, tables.Engineers));

        Assert.Contains(exception.Problems, problem => problem.Contains("unknown engineer \"Felicty Farseer\"", StringComparison.Ordinal));
        Assert.Contains(exception.Problems, problem => problem.Contains("\"Nobody\": offers no blueprint", StringComparison.Ordinal));
    }

    [Fact]
    public void FromRows_RejectsABadGradeAndABadRegion()
    {
        var tables = Snapshot();
        tables.Materials["Iron"] = tables.Materials["Iron"] with { Grade = 9 };
        tables.Engineers["Felicity Farseer"] = tables.Engineers["Felicity Farseer"] with { Region = "mars" };

        var exception = Assert.Throws<CatalogException>(() => Catalog.FromRows(tables.Materials, tables.Blueprints, tables.Experimentals, tables.ModuleTypes, tables.Engineers));

        Assert.Contains(exception.Problems, problem => problem.Contains("grade must be 1–5", StringComparison.Ordinal));
        Assert.Contains(exception.Problems, problem => problem.Contains("region must be bubble or colonia", StringComparison.Ordinal));
    }

    [Fact]
    public void FromRows_RejectsBrokenIdentity()
    {
        var tables = Snapshot();
        tables.Materials["Iron"] = tables.Materials["Iron"] with { Symbol = "Fe" };
        tables.Materials["Zinc"] = tables.Materials["Zinc"] with { Name = "Carbon" };
        tables.Engineers["Felicity Farseer"] = tables.Engineers["Felicity Farseer"] with { Id = tables.Engineers["Elvira Martuuk"].Id };
        tables.Engineers["The Dweller"] = tables.Engineers["The Dweller"] with { Id = 0 };

        var exception = Assert.Throws<CatalogException>(() => Catalog.FromRows(tables.Materials, tables.Blueprints, tables.Experimentals, tables.ModuleTypes, tables.Engineers));

        Assert.Contains(exception.Problems, problem => problem.Contains("\"Iron\": symbol must equal its key", StringComparison.Ordinal));
        Assert.Contains(exception.Problems, problem => problem.Contains("names must be unique", StringComparison.Ordinal));
        Assert.Contains(exception.Problems, problem => problem.Contains("ids must be unique", StringComparison.Ordinal));
        Assert.Contains(exception.Problems, problem => problem.Contains("\"The Dweller\": id must be a positive integer", StringComparison.Ordinal));
    }

    private sealed class Tables
    {
        public required Dictionary<string, MaterialRow> Materials { get; init; }

        public required Dictionary<string, BlueprintRow> Blueprints { get; init; }

        public required Dictionary<string, ExperimentalRow> Experimentals { get; init; }

        public required Dictionary<string, ModuleTypeRow> ModuleTypes { get; init; }

        public required Dictionary<string, EngineerRow> Engineers { get; init; }
    }

    private static Tables Snapshot()
    {
        var materials = Catalog.Materials.ToDictionary(
            pair => pair.Key,
            pair => new MaterialRow(pair.Value.Symbol, pair.Value.Name, pair.Value.Category.ToString().ToUpperInvariant() switch
            {
                "RAW" => "raw",
                "MANUFACTURED" => "manufactured",
                _ => "encoded",
            }, pair.Value.Grade, pair.Value.Line));
        var blueprints = Catalog.Blueprints.ToDictionary(
            pair => pair.Key,
            pair => new BlueprintRow(pair.Value.Name, pair.Value.Grades.ToDictionary(grade => grade.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), grade => grade.Value)));
        var experimentals = Catalog.Experimentals.ToDictionary(pair => pair.Key, pair => new ExperimentalRow(pair.Value.Name, pair.Value.Components));
        var moduleTypes = Catalog.ModuleTypes.ToDictionary(
            pair => pair.Key,
            pair => new ModuleTypeRow(
                pair.Value.Name,
                pair.Value.Group.ToString().ToUpperInvariant() switch
                {
                    "CORE" => "core",
                    "OPTIONAL" => "optional",
                    "HARDPOINT" => "hardpoint",
                    _ => "utility",
                },
                pair.Value.ItemPatterns,
                pair.Value.Blueprints.ToDictionary(
                    bp => bp.Key,
                    IReadOnlyDictionary<string, IReadOnlyList<string>> (bp) => bp.Value.ToDictionary(grade => grade.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), grade => grade.Value)),
                pair.Value.Experimentals));
        var engineers = Catalog.Engineers.ToDictionary(
            pair => pair.Key,
            pair => new EngineerRow(pair.Value.Name, pair.Value.Id, pair.Value.System, pair.Value.Base, pair.Value.Region == Region.Bubble ? "bubble" : "colonia", pair.Value.Position.X, pair.Value.Position.Y, pair.Value.Position.Z));
        return new Tables
        {
            Materials = materials,
            Blueprints = blueprints,
            Experimentals = experimentals,
            ModuleTypes = moduleTypes,
            Engineers = engineers,
        };
    }
}
