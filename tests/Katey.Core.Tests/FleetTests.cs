using Katey.Core.Edsy;
using Katey.Core.Fleet;

namespace Katey.Core.Tests;

/// <summary>Checks the module catalog and the fit matcher's three sharing policies.</summary>
public sealed class FleetTests
{
    private static readonly ModuleCatalog Modules = ModuleCatalog.LoadEmbedded();

    private static readonly Engineering LongRange5 = new("FSD_LongRange", 5, "special_fsd_heavy");

    private static Fitting Fit(string id, string hull, params FitModule[] modules) => new(id, hull, id, modules);

    private static OwnedModule Owned(string id, string item, Engineering? engineering = null, string system = "Deciat", ModulePlace place = ModulePlace.Stored, string? hull = null)
        => new(id, item, engineering, place, system, place == ModulePlace.Stored ? "Farseer Inc" : "Ship", hull);

    [Fact]
    public void ModuleCatalog_NamesModules()
    {
        Assert.True(Modules.Count > 900);
        Assert.Equal("Frame Shift Drive (SCO) 5A", Modules.Find("INT_HYPERDRIVE_OVERCHARGE_SIZE5_CLASS5")!.Name);
        Assert.Equal("Multi-cannon 4A Gimballed", Modules.NameOf("hpt_multicannon_gimbal_huge"));
        Assert.Equal("mc", Modules.Find("hpt_multicannon_gimbal_huge")!.Group);
    }

    [Fact]
    public void ModuleCatalog_NamesSymbolsTheTableLacks()
    {
        Assert.Equal("Military Grade Composite", Modules.NameOf("anacondamkii_armour_grade3"));
        Assert.Equal("Lightweight Alloy", Modules.NameOf("explorer_nx_armour_grade1_default"));
        Assert.Equal("Reactive Surface Composite", Modules.NameOf("cobramkv_armour_reactive"));
        Assert.Equal("Modular Cargo Bay Door", Modules.NameOf("modularcargobaydoor"));
        Assert.Equal("MkII Vessel Hangar 5D", Modules.NameOf("int_fighterbaymk2_size5_class1_free"));
        Assert.Equal("MkII Large Planetary Vehicle Hangar 4F", Modules.NameOf("int_mkiilargebuggybay_size4_class3_free"));
        Assert.Equal("MkII EC Passenger Cabin 6D", Modules.NameOf("int_mkii_passengercabin_size6_class1"));
        Assert.Equal("Pulse Wave Xeno Scanner 0C", Modules.NameOf("hpt_xenoscanner_advanced_tiny"));
        Assert.Equal("FSD 8E", Modules.NameOf("int_hyperdrive_size8_class1"));
        Assert.Equal("Modular Cargo Bay Door FDL", Modules.NameOf("modularcargobaydoorfdl"));
        Assert.Equal("Large Cargo Rack 5E", Modules.NameOf("int_largecargorack_size5_class1"));
        Assert.Equal("Mk II Passenger Cabin 5C", Modules.NameOf("int_mkii_passengercabin_size5_class3"));
        Assert.Equal("Mk II Large Buggy Bay 3C", Modules.NameOf("int_mkiilargebuggybay_size3_class3"));
        Assert.Equal(Modules.NameOf("int_fighterbay_size5_class1"), Modules.NameOf("int_fighterbay_size5_class1_free"));
        Assert.Equal("Zzqxwidget", Modules.NameOf("int_zzqxwidget"));
    }

    [Fact]
    public void ModuleSymbolNames_SegmentsIntoTheFewestWords()
    {
        var lexicon = new HashSet<string>(StringComparer.Ordinal) { "cargo", "rack", "cargorack", "car", "go" };
        Assert.Equal(["cargorack"], ModuleSymbolNames.Segment("cargorack", lexicon));
        Assert.Equal(["cargo", "rack"], ModuleSymbolNames.Segment("cargorack", new HashSet<string>(StringComparer.Ordinal) { "cargo", "rack", "car", "go" }));
        Assert.Null(ModuleSymbolNames.Segment("cargoracks", lexicon));
        Assert.Null(ModuleSymbolNames.ArmourName("int_cargorack_size1_class1"));
        Assert.Equal("int_fighterbay_size5_class1", ModuleSymbolNames.WithoutSuffix("int_fighterbay_size5_class1_free"));
    }

    [Fact]
    public void Canonical_DropsCaseFreeSuffixAndHullCodes_AndFixturesAreKnown()
    {
        Assert.Equal("int_fighterbaymk2_size5_class1", ModuleSymbolNames.Canonical("Int_FighterBayMk2_Size5_Class1_free"));
        Assert.Equal("modularcargobaydoor", ModuleSymbolNames.Canonical("modularcargobaydoorfdl"));
        Assert.Equal("modularcargobaydoor", ModuleSymbolNames.Canonical("ModularCargoBayDoor"));
        Assert.Equal("int_cargorack_size5_class1", ModuleSymbolNames.Canonical("int_cargorack_size5_class1"));

        Assert.True(ModuleCatalog.IsFixture("modularcargobaydoorfdl"));
        Assert.True(ModuleCatalog.IsFixture("int_planetapproachsuite_advanced"));
        Assert.True(ModuleCatalog.IsFixture("Int_PlanetApproachSuite"));
        Assert.False(ModuleCatalog.IsFixture("int_cargorack_size5_class1"));
        Assert.False(ModuleCatalog.IsFixture("int_fighterbay_size5_class1_free"));
    }

    [Fact]
    public void OfType_ListsTheTypesModules_ByClassThenName_WithoutPlaceholders()
    {
        var beams = Modules.OfType("BL");

        Assert.Equal(12, beams.Count);
        Assert.Equal("Beam Laser 1E Fixed", beams[0].Name);
        Assert.Equal("Beam Laser 4A Gimballed", beams[^1].Name);
        Assert.Contains(beams, module => module.Name == "Retributor 1E Fixed");
        Assert.Equal(beams.OrderBy(module => module.Class).ThenBy(module => module.Name, StringComparer.Ordinal), beams);

        Assert.True(Modules.Find("int_missing_hyperdrive")!.IsPlaceholder);
        Assert.DoesNotContain(Modules.OfType("fsd"), module => module.IsPlaceholder);
        Assert.Empty(Modules.OfType("bh"));
    }

    [Fact]
    public void Match_PooledSharesAcrossFits_SameHullDoesNot_NoneNeedsCopies()
    {
        var fsd = new FitModule("FrameShiftDrive", "int_hyperdrive_size6_class5", LongRange5);
        var conda1 = Fit("conda-1", "anaconda", fsd);
        var conda2 = Fit("conda-2", "anaconda", fsd);
        var cutter = Fit("cutter", "cutter", new FitModule("FrameShiftDrive", "int_hyperdrive_size6_class5", LongRange5));
        var owned = new[] { Owned("a", "int_hyperdrive_size6_class5", LongRange5) };
        var hulls = new HashSet<string> { "anaconda", "cutter" };

        var pooled = FitMatcher.Match([conda1, conda2, cutter], owned, hulls, SharingPolicy.Pooled, null);
        Assert.Equal([0, 0, 0], pooled.Fits.Select(fit => fit.MissingCount).ToList());
        Assert.Empty(pooled.Unused);

        var sameHull = FitMatcher.Match([conda1, conda2, cutter], owned, hulls, SharingPolicy.SameHull, null);
        Assert.Equal([0, 0, 1], sameHull.Fits.Select(fit => fit.MissingCount).ToList());

        var sameHullCutterFirst = FitMatcher.Match([cutter, conda1, conda2], owned, hulls, SharingPolicy.SameHull, null);
        Assert.Equal([0, 1, 1], sameHullCutterFirst.Fits.Select(fit => fit.MissingCount).ToList());

        var none = FitMatcher.Match([conda1, conda2, cutter], owned, hulls, SharingPolicy.None, null);
        Assert.Equal([0, 1, 1], none.Fits.Select(fit => fit.MissingCount).ToList());

        var twoInOneFit = FitMatcher.Match([Fit("twin", "anaconda", fsd, fsd)], owned, hulls, SharingPolicy.Pooled, null);
        Assert.Equal(1, twoInOneFit.Fits[0].MissingCount);
    }

    [Fact]
    public void Match_TakesTheFitsOwnHullFirst_ThenStorage_ThenOtherHulls()
    {
        var plant = new FitModule("PowerPlant", "int_powerplant_size5_class5", null);
        var onTaxi = Owned("taxi", "int_powerplant_size5_class5", place: ModulePlace.Installed, hull: "mediumtransport01");
        var stored = Owned("stored", "int_powerplant_size5_class5");
        var onConda = Owned("conda", "int_powerplant_size5_class5", place: ModulePlace.Installed, hull: "anaconda");
        var hulls = new HashSet<string> { "anaconda", "mediumtransport01", "mandalay" };

        // Each fit is satisfied by the module its own hull already carries, whatever order the modules are listed in.
        var taxi = FitMatcher.Match([Fit("taxi", "mediumtransport01", plant)], [onConda, stored, onTaxi], hulls, SharingPolicy.Pooled, null);
        Assert.Equal("taxi", taxi.Fits[0].Lines[0].Found!.Id);
        var conda = FitMatcher.Match([Fit("conda", "anaconda", plant)], [onTaxi, stored, onConda], hulls, SharingPolicy.Pooled, null);
        Assert.Equal("conda", conda.Fits[0].Lines[0].Found!.Id);

        // A hull with nothing of its own takes storage before robbing another ship.
        var mandalay = FitMatcher.Match([Fit("mandalay", "mandalay", plant)], [onTaxi, onConda, stored], hulls, SharingPolicy.Pooled, null);
        Assert.Equal("stored", mandalay.Fits[0].Lines[0].Found!.Id);

        // An earlier fit does not take a later fit's own module while it has its own.
        var both = FitMatcher.Match([Fit("conda", "anaconda", plant), Fit("taxi", "mediumtransport01", plant)], [onTaxi, onConda], hulls, SharingPolicy.Pooled, null);
        Assert.Equal(["conda", "taxi"], both.Fits.Select(fit => fit.Lines[0].Found!.Id).ToList());

        // A module already on the fit's own hull is kept even when it carries engineering the fit does not need: the ship is fitted as is.
        var engineeredOnTaxi = Owned("taxi-g3", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 3, null), place: ModulePlace.Installed, hull: "mediumtransport01");
        var ownWins = FitMatcher.Match([Fit("taxi", "mediumtransport01", plant)], [engineeredOnTaxi, onConda], hulls, SharingPolicy.Pooled, null);
        Assert.Equal("taxi-g3", ownWins.Fits[0].Lines[0].Found!.Id);

        // Elsewhere, the least engineering is wasted: a stock need takes the stock stored module over an engineered one.
        var engineeredStored = Owned("stored-g3", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 3, null));
        var stockWins = FitMatcher.Match([Fit("mandalay", "mandalay", plant)], [engineeredStored, stored], hulls, SharingPolicy.Pooled, null);
        Assert.Equal("stored", stockWins.Fits[0].Lines[0].Found!.Id);

        // With no fit in play, storage beats a ship.
        Assert.Equal("stored", FitMatcher.Find(plant, [onConda, stored, onTaxi]).Found!.Id);

        // Within a place the nearer module wins, an unknown place comes last, and place still outranks distance.
        var nearStore = Owned("near", "int_powerplant_size5_class5", system: "Deciat");
        var farStore = Owned("far", "int_powerplant_size5_class5", system: "Sol");
        var lostStore = Owned("lost", "int_powerplant_size5_class5", system: "Nowhere");
        static double Farness(OwnedModule module) => module.System switch { "Deciat" => 0, "Sol" => 120.5, _ => double.PositiveInfinity };
        var nearest = FitMatcher.Match([Fit("mandalay", "mandalay", plant)], [lostStore, farStore, nearStore], hulls, SharingPolicy.Pooled, null, Farness);
        Assert.Equal("near", nearest.Fits[0].Lines[0].Found!.Id);
        Assert.Equal("far", FitMatcher.Find(plant, [lostStore, farStore], Farness).Found!.Id);
        var farOwnHull = Owned("taxi-far", "int_powerplant_size5_class5", system: "Sol", place: ModulePlace.Installed, hull: "mediumtransport01");
        Assert.Equal("taxi-far", FitMatcher.Match([Fit("taxi", "mediumtransport01", plant)], [nearStore, farOwnHull], hulls, SharingPolicy.Pooled, null, Farness).Fits[0].Lines[0].Found!.Id);
    }

    [Fact]
    public void Match_TreatsAnUnfinishedGradeAsPartial_AndALegacyFigureAsComplete()
    {
        var need = new FitModule("PowerPlant", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 3, null));
        var hulls = new HashSet<string> { "mandalay" };
        var quarter = Owned("quarter", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 3, null, Quality: 0.25));
        var done = Owned("done", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 3, null, Quality: 1));
        var legacy = Owned("legacy", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 3, null, Quality: 0));
        var higher = Owned("higher", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 4, null, Quality: 0.1));

        Assert.Equal(MatchQuality.Partial, FitMatcher.Match([Fit("f", "mandalay", need)], [quarter], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0].Quality);
        Assert.False(FitMatcher.Match([Fit("f", "mandalay", need)], [quarter], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0].NeedsOnlyExperimental);
        Assert.Equal(MatchQuality.Exact, FitMatcher.Match([Fit("f", "mandalay", need)], [done], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0].Quality);
        Assert.Equal(MatchQuality.Exact, FitMatcher.Match([Fit("f", "mandalay", need)], [legacy], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0].Quality);
        Assert.Equal(MatchQuality.Exact, FitMatcher.Match([Fit("f", "mandalay", need)], [higher], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0].Quality);
        Assert.Equal(25, quarter.Engineering!.Percent);
        Assert.True(legacy.Engineering!.IsComplete);
    }

    [Fact]
    public void Match_LeavesPrebuiltModulesToTheirOwnHull()
    {
        var plant = new FitModule("PowerPlant", "int_powerplant_size5_class5", null);
        var prebuiltOnTaxi = new OwnedModule("taxi-pre", "int_powerplant_size5_class5", null, ModulePlace.Installed, "Deciat", "Taxi", "mediumtransport01", IsPrebuilt: true);
        var hulls = new HashSet<string> { "anaconda", "mediumtransport01" };

        // Its own hull's fit may use it; another hull's fit may not, even with nothing else to hand; nor may a search with no fit in play.
        Assert.Equal("taxi-pre", FitMatcher.Match([Fit("taxi", "mediumtransport01", plant)], [prebuiltOnTaxi], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0].Found!.Id);
        Assert.Equal(MatchQuality.Missing, FitMatcher.Match([Fit("conda", "anaconda", plant)], [prebuiltOnTaxi], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0].Quality);
        Assert.Null(FitMatcher.Find(plant, [prebuiltOnTaxi]).Found);
    }

    [Fact]
    public void Match_RespectsEngineeringGradeExperimentalAndLocation()
    {
        var need = new FitModule("FrameShiftDrive", "int_hyperdrive_size5_class5", new Engineering("FSD_LongRange", 4, "special_fsd_heavy"));
        var fit = Fit("fit", "mandalay", need);
        var hulls = new HashSet<string> { "mandalay" };

        var tooLow = Owned("low", "int_hyperdrive_size5_class5", new Engineering("FSD_LongRange", 3, "special_fsd_heavy"));
        var wrongEffect = Owned("wrong", "int_hyperdrive_size5_class5", new Engineering("FSD_LongRange", 5, "special_fsd_fuelcapacity"));
        var unknownEffect = Owned("unknown", "int_hyperdrive_size5_class5", new Engineering("FSD_LongRange", 5, null, ExperimentalKnown: false));
        var exact = Owned("exact", "int_hyperdrive_size5_class5", new Engineering("FSD_LongRange", 5, "special_fsd_heavy"), system: "Sol");
        var stock = Owned("stock", "int_hyperdrive_size5_class5");

        var everywhere = FitMatcher.Match([fit], [tooLow, wrongEffect, unknownEffect, exact, stock], hulls, SharingPolicy.Pooled, null);
        var line = everywhere.Fits[0].Lines[0];
        Assert.Equal(MatchQuality.Exact, line.Quality);
        Assert.Equal("exact", line.Found!.Id);
        Assert.Equal(4, everywhere.Unused.Count);

        var deciatOnly = FitMatcher.Match([fit], [tooLow, wrongEffect, unknownEffect, exact, stock], hulls, SharingPolicy.Pooled, "deciat");
        Assert.Equal(MatchQuality.ExperimentalUnknown, deciatOnly.Fits[0].Lines[0].Quality);
        Assert.Equal("unknown", deciatOnly.Fits[0].Lines[0].Found!.Id);
        Assert.DoesNotContain(deciatOnly.Unused, module => module.Id == "exact");

        var lowOnly = FitMatcher.Match([fit], [tooLow, stock], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0];
        Assert.Equal(MatchQuality.Partial, lowOnly.Quality);
        Assert.Equal("low", lowOnly.Found!.Id);
        Assert.False(lowOnly.NeedsOnlyExperimental);
        var effectOnly = FitMatcher.Match([fit], [wrongEffect, stock], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0];
        Assert.Equal(MatchQuality.Partial, effectOnly.Quality);
        Assert.True(effectOnly.NeedsOnlyExperimental);
        Assert.Equal(MatchQuality.Missing, FitMatcher.Match([fit], [stock], hulls, SharingPolicy.Pooled, null).Fits[0].Lines[0].Quality);
        Assert.Equal(1, FitMatcher.Match([fit], [tooLow], hulls, SharingPolicy.Pooled, null).Fits[0].PartialCount);

        var stockNeed = Fit("stock-fit", "mandalay", new FitModule("FrameShiftDrive", "int_hyperdrive_size5_class5", null));
        var stockMatch = FitMatcher.Match([stockNeed], [exact, stock], hulls, SharingPolicy.Pooled, null);
        Assert.Equal("stock", stockMatch.Fits[0].Lines[0].Found!.Id);

        var notOwned = FitMatcher.Match([Fit("x", "corvette", need)], [exact], hulls, SharingPolicy.Pooled, null);
        Assert.False(notOwned.Fits[0].HullOwned);
        Assert.Single(notOwned.Unused);
    }

    [Fact]
    public void EdsyImport_ListsEveryModuleForFits()
    {
        var result = new EdsyImport(Catalog.LoadEmbedded()).Parse(TestData.Mandalay);

        Assert.True(result.Ok);
        var all = result.Loadout!.AllModules;
        Assert.Equal("mandalay", result.Loadout.Ship);
        Assert.True(all.Count >= 10);
        Assert.Contains(all, module => module.Slot == "CargoHatch" && module.Blueprint is null);
        var fsd = all.Single(module => module.Slot == "FrameShiftDrive");
        Assert.Equal("int_hyperdrive_overcharge_size5_class5", fsd.Item);
        Assert.Equal("FSD_LongRange", fsd.Blueprint);
        Assert.Equal(5, fsd.Level);
        Assert.Equal("special_fsd_heavy", fsd.Experimental);
    }
}
