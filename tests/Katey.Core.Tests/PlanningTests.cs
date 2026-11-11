using Katey.Core.Planning;

namespace Katey.Core.Tests;

/// <summary>Checks the material sums, the shortfall against an inventory, and the engineer coverage.</summary>
public sealed class PlanningTests
{
    private static readonly Catalog Catalog = Catalog.LoadEmbedded();

    private static PlanEntry Fsd(int grade = 5, string? experimental = "special_fsd_heavy")
        => PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", grade, experimental, PlanSource.Manual);

    [Fact]
    public void Create_ClampsTheGradeAndStartsWithAsManyRollsAsTheGrade()
    {
        var entry = PlanEntry.Create(Catalog, "scb", "ShieldCellBank_Specialised", 5, null, PlanSource.Manual);

        Assert.Equal(4, entry.TargetGrade);
        Assert.True(entry.Enabled);
        Assert.All(Enumerable.Range(1, 5), grade => Assert.Equal(grade, entry.RollsAt(grade)));
        Assert.Equal([1, 2, 3, 4], entry.RolledGrades(Catalog));
    }

    [Fact]
    public void IsSameWorkAs_MatchesTheJob_NotTheEntry()
    {
        var source = new PlanSource("Dream fit", "FrameShiftDrive", "int_hyperdrive_size5_class5");
        var entry = PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, "special_fsd_heavy", source);
        Assert.True(entry.IsSameWorkAs(PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, "special_fsd_heavy", source) with { Enabled = false }));
        Assert.True(entry.IsSameWorkAs(entry.WithRolls(5, 0)));
        Assert.False(entry.IsSameWorkAs(PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 4, "special_fsd_heavy", source)));
        Assert.False(entry.IsSameWorkAs(PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, null, source)));
        Assert.False(entry.IsSameWorkAs(PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, "special_fsd_heavy", new PlanSource("Dream fit", "Slot01_Size5", "int_hyperdrive_size5_class5"))));
        Assert.False(entry.IsSameWorkAs(PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, "special_fsd_heavy", PlanSource.Manual)));
        var unlock = PlanEntry.ForUnlock(Catalog, Catalog.Unlocks.Keys.First());
        Assert.True(unlock.IsSameWorkAs(PlanEntry.ForUnlock(Catalog, Catalog.Unlocks.Keys.First())));
        Assert.False(unlock.IsSameWorkAs(entry));
    }

    [Fact]
    public void PlanProgress_CountsRollsDown_AndRetiresTheEntryAtTheTargetWithItsEffect()
    {
        var start = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var entry = Fsd() with { CraftsAfter = start };
        var unlock = PlanEntry.ForUnlock(Catalog, Catalog.Unlocks.Keys.First());
        Craft Roll(int minute, int level, double quality, string? effect = null, string blueprint = "FSD_LongRange", string? type = "fsd")
            => new(start.AddMinutes(minute), type, blueprint, level, quality, effect);

        // A roll before the entry existed is ignored; a grade 1 roll that completes the grade zeroes it.
        var first = PlanProgress.Apply(Catalog, [entry, unlock], [Roll(-5, 5, 1, "special_fsd_heavy"), Roll(1, 1, 1)]);
        Assert.False(first.Plan[0] == entry);
        var after1 = Assert.Single(first.Advanced);
        Assert.Equal(0, after1.RollsAt(1));
        Assert.Equal(2, after1.RollsAt(2));
        Assert.Equal(start.AddMinutes(1), after1.CraftsAfter);
        Assert.Same(unlock, first.Plan[1]);
        Assert.Empty(first.Completed);

        // Jumping to grade 4 on a module already past the lower grades zeroes them; a partial roll takes one off.
        var fourth = PlanProgress.Apply(Catalog, first.Plan, [Roll(2, 4, 0.25)]);
        var after4 = fourth.Plan[0];
        Assert.Equal([0, 0, 0, 3, 5], Enumerable.Range(1, 5).Select(after4.RollsAt));

        // The grade 5 completing roll without the effect leaves the entry wanting only the effect; the effect retires it.
        var fifth = PlanProgress.Apply(Catalog, fourth.Plan, [Roll(3, 5, 1)]);
        Assert.Empty(fifth.Completed);
        Assert.All(Enumerable.Range(1, 5), grade => Assert.Equal(0, fifth.Plan[0].RollsAt(grade)));
        var done = PlanProgress.Apply(Catalog, fifth.Plan, [Roll(4, 5, 1, "special_fsd_heavy")]);
        Assert.Single(done.Completed);
        Assert.Equal([unlock.Id], done.Plan.Select(entry => entry.Id));
        Assert.True(done.Changed);

        // The effect applied early comes off the entry at once, so its materials stop counting, and the grades carry on.
        var early = PlanProgress.Apply(Catalog, [entry], [Roll(1, 2, 0.5, "special_fsd_heavy")]);
        var wantingGrades = Assert.Single(early.Advanced);
        Assert.Null(wantingGrades.Experimental);
        Assert.Equal(1, wantingGrades.RollsAt(2));
        Assert.Empty(early.Completed);
        Assert.Single(PlanProgress.Apply(Catalog, early.Plan, [Roll(2, 5, 1)]).Completed);

        // An experimental-only entry with no effect chosen has nothing the game could report.
        Assert.False(PlanProgress.Apply(Catalog, [Fsd(0, null) with { CraftsAfter = start }], [Roll(1, 5, 1, "special_fsd_heavy")]).Changed);

        // The same roll a second time changes nothing, since the entry's anchor moved past it.
        Assert.False(PlanProgress.Apply(Catalog, fourth.Plan, [Roll(2, 4, 0.25)]).Changed);

        // Another blueprint or module type is not this entry's roll; an unknown module type is given the benefit of the doubt.
        Assert.False(PlanProgress.Apply(Catalog, [entry], [Roll(1, 1, 1, blueprint: "FSD_FastBoot")]).Changed);
        Assert.False(PlanProgress.Apply(Catalog, [entry], [Roll(1, 1, 1, type: "pp")]).Changed);
        Assert.True(PlanProgress.Apply(Catalog, [entry], [Roll(1, 1, 1, type: null)]).Changed);

        // Two entries for one blueprint: a roll goes to the first that still plans that grade, so each roll advances one of them.
        var twin = Fsd() with { CraftsAfter = start };
        var pair = PlanProgress.Apply(Catalog, [entry, twin], [Roll(1, 1, 1), Roll(2, 1, 1)]);
        Assert.Equal([0, 0], pair.Plan.Select(entry => entry.RollsAt(1)));

        // Experimental only: the effect alone retires it.
        var effectOnly = Fsd(0) with { CraftsAfter = start };
        var applied = PlanProgress.Apply(Catalog, [effectOnly], [Roll(1, 5, 1, "special_fsd_heavy")]);
        Assert.Single(applied.Completed);
        Assert.Empty(applied.Plan);

        // The floor is the earliest anchor among module entries; unlocks do not count.
        Assert.Equal(start, PlanProgress.Floor([unlock, entry, twin with { CraftsAfter = start.AddDays(1) }]));
        Assert.Null(PlanProgress.Floor([unlock]));
        Assert.Equal(DateTimeOffset.MinValue, PlanProgress.Floor([entry with { CraftsAfter = null }]));
    }

    [Fact]
    public void ForEntry_SumsEveryGradeUpToTheTargetPlusTheExperimental()
    {
        var materials = MaterialCalculator.ForEntry(Catalog, Fsd());

        var expected = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var grade in Enumerable.Range(1, 5))
        {
            foreach (var (name, count) in Catalog.Blueprints["FSD_LongRange"].Grades[grade])
            {
                expected[name] = expected.GetValueOrDefault(name) + (count * grade);
            }
        }

        foreach (var (name, count) in Catalog.Experimentals["special_fsd_heavy"].Components)
        {
            expected[name] = expected.GetValueOrDefault(name) + count;
        }

        Assert.Equal(expected.OrderBy(pair => pair.Key, StringComparer.Ordinal), materials.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        Assert.Equal(5, materials["DataminedWake"]);
        Assert.Equal(5 + 1 + 2, materials["DisruptedWakeEchoes"]);
    }

    [Fact]
    public void GradeZero_IsExperimentalOnly_RollsNothing_AndIsServedByAnyApplier()
    {
        var entry = PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 0, "special_fsd_heavy", PlanSource.Manual);
        Assert.True(entry.IsExperimentalOnly);
        Assert.Empty(entry.RolledGrades(Catalog));
        Assert.Equal(Catalog.Experimentals["special_fsd_heavy"].Components, MaterialCalculator.ForEntry(Catalog, entry));

        var service = EngineerCoverage.ServiceFor(Catalog, entry, new HashSet<string> { "Felicity Farseer" });
        Assert.Equal(service.CanApplyExperimental, service.CanRoll);
        Assert.Contains("Felicity Farseer", service.SelectedCanRoll);

        Assert.Equal(0, Fsd().WithTargetGrade(Catalog, -2).TargetGrade);
        Assert.Equal(5, Fsd().WithTargetGrade(Catalog, 0).WithTargetGrade(Catalog, 9).TargetGrade);
        Assert.Empty(EngineerCoverage.ServiceFor(Catalog, entry with { Experimental = null }, new HashSet<string>()).CanRoll);
    }

    [Fact]
    public void ForEntry_MultipliesByRolls_AndLowerTargetDropsGrades()
    {
        var sevenRolls = Fsd().WithRolls(5, 7);
        Assert.Equal(7, MaterialCalculator.ForEntry(Catalog, sevenRolls)["DataminedWake"]);

        var zeroRolls = Fsd(experimental: null).WithRolls(5, 0);
        Assert.False(MaterialCalculator.ForEntry(Catalog, zeroRolls).ContainsKey("DataminedWake"));

        var lower = Fsd(experimental: null).WithTargetGrade(Catalog, 2);
        Assert.Equal(2, lower.TargetGrade);
        Assert.False(MaterialCalculator.ForEntry(Catalog, lower).ContainsKey("DataminedWake"));
    }

    [Fact]
    public void WithRolls_RejectsNegativeCounts()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Fsd().WithRolls(1, -1));

    [Fact]
    public void UnlockEntries_CostTheirFixedMaterials_AndTheBudgetFollowsThePlan()
    {
        var booster = PlanEntry.ForUnlock(Catalog, "guardian_fsd_booster");
        Assert.True(booster.IsUnlock);
        Assert.False(booster.IsExperimentalOnly);
        Assert.Equal("gfsb", booster.ModuleType);
        Assert.Empty(booster.RolledGrades(Catalog));
        Assert.Same(booster, booster.WithTargetGrade(Catalog, 5));
        Assert.Equal(21, MaterialCalculator.ForEntry(Catalog, booster)["Guardian_PowerCell"]);
        Assert.Empty(EngineerCoverage.ServiceFor(Catalog, booster, new HashSet<string>(StringComparer.Ordinal) { "Felicity Farseer" }).CanRoll);

        var distributor = PlanEntry.ForUnlock(Catalog, "guardian_power_distributor");
        var totals = MaterialCalculator.Totals(Catalog, [booster, distributor, Fsd()]);
        Assert.Equal(45, totals.Single(total => total.Material.Name == "Guardian Power Cell").Needed);
        Assert.Equal(["HN Shock Mount", "Heatsink Interlink"], MaterialCalculator.Commodities(Catalog, [booster, distributor, distributor with { Enabled = false }]).Select(total => total.Name));
        Assert.Equal(8, MaterialCalculator.Commodities(Catalog, [booster]).Single().Needed);

        // 30 power cells pay for either unlock alone, but the plan takes its 21 first, leaving 9: too few for the distributor's 24.
        var inventory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Guardian_PowerCell"] = 30,
            ["Guardian_TechComponent"] = 25,
            ["Guardian_ModuleBlueprint"] = 2,
            ["FocusCrystals"] = 24,
            ["AncientBiologicalData"] = 20,
            ["PhaseAlloys"] = 18,
        };
        var alone = UnlockBudget.Remaining(Catalog, [], inventory);
        Assert.True(UnlockBudget.CanAfford(Catalog, Catalog.Unlocks["guardian_fsd_booster"], alone));
        Assert.True(UnlockBudget.CanAfford(Catalog, Catalog.Unlocks["guardian_power_distributor"], alone));
        var afterBooster = UnlockBudget.Remaining(Catalog, [booster], inventory);
        Assert.Equal(9, afterBooster!["Guardian_PowerCell"]);
        Assert.False(UnlockBudget.CanAfford(Catalog, Catalog.Unlocks["guardian_power_distributor"], afterBooster));
        var line = UnlockBudget.Lines(Catalog, Catalog.Unlocks["guardian_power_distributor"], afterBooster).Single(line => line.Material.Name == "Guardian Power Cell");
        Assert.Equal(15, line.Missing);
        Assert.Null(UnlockBudget.Remaining(Catalog, [booster], null));
        Assert.False(UnlockBudget.CanAfford(Catalog, Catalog.Unlocks["guardian_fsd_booster"], null));

        var text = ShoppingList.Format(MaterialCalculator.WithInventory(MaterialCalculator.Totals(Catalog, [booster]), inventory), MaterialCalculator.Commodities(Catalog, [booster]));
        Assert.EndsWith("Buy at a market\n8 × HN Shock Mount", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Totals_SkipDisabledEntries_AndSortByInventory()
    {
        var booster = PlanEntry.Create(Catalog, "sb", "ShieldBooster_HeavyDuty", 1, "special_shieldbooster_chunky", PlanSource.Manual);
        var totals = MaterialCalculator.Totals(Catalog, [Fsd(), booster, booster, booster with { Enabled = false }]);

        Assert.Equal(6, totals.Single(total => total.Material.Name == "Untypical Shield Scans").Needed);
        Assert.Equal(5, totals.Single(total => total.Material.Name == "Datamined Wake Exceptions").Needed);
        Assert.Equal(totals.Select(total => total.Material.Category), totals.Select(total => total.Material.Category).OrderBy(category => category));

        var withoutFsd = MaterialCalculator.Totals(Catalog, [Fsd() with { Enabled = false }, booster]);
        Assert.DoesNotContain(withoutFsd, total => total.Material.Name == "Datamined Wake Exceptions");
    }

    [Fact]
    public void WithInventory_ComputesTheShortfallCaseInsensitively()
    {
        var totals = MaterialCalculator.Totals(Catalog, [Fsd(experimental: null)]);
        var inventory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["dataminedwake"] = 3,
            ["arsenic"] = 0,
        };

        var lines = MaterialCalculator.WithInventory(totals, inventory);

        var datamined = lines.Single(line => line.Material.Name == "Datamined Wake Exceptions");
        Assert.Equal(3, datamined.Have);
        Assert.Equal(2, datamined.Missing);
        Assert.False(datamined.IsCovered);

        var arsenic = lines.Single(line => line.Material.Name == "Arsenic");
        Assert.Equal(0, arsenic.Have);
        Assert.Equal(5, arsenic.Missing);
        Assert.False(arsenic.IsCovered);

        var noInventory = MaterialCalculator.WithInventory(totals, null).Single(line => line.Material.Name == "Arsenic");
        Assert.Null(noInventory.Have);
        Assert.Equal(5, noInventory.Missing);
    }

    [Fact]
    public void ShoppingList_PrintsSectionsAndHaveShort()
    {
        var totals = MaterialCalculator.Totals(Catalog, [Fsd(experimental: null)]);
        var text = ShoppingList.Format(MaterialCalculator.WithInventory(totals, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Arsenic"] = 4 }));

        Assert.StartsWith("Raw", text, StringComparison.Ordinal);
        Assert.Contains("5 × Arsenic (G2, Rhenium) — have 4, short 1", text, StringComparison.Ordinal);
        Assert.Contains("\n\nEncoded\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Coverage_ForFelicityAlone()
    {
        var selected = new HashSet<string>(StringComparer.Ordinal) { "Felicity Farseer" };
        var coverage = EngineerCoverage.For(Catalog, selected);

        BlueprintCoverage Row(string type, string blueprint) => coverage.Single(row => row.ModuleType == type && row.Blueprint == blueprint);

        Assert.Equal(5, Row("fsd", "FSD_LongRange").MaxGrade);
        Assert.Equal(3, Row("t", "Engine_Dirty").MaxGrade);
        Assert.Equal(1, Row("pp", "PowerPlant_Boosted").MaxGrade);
        Assert.Equal(1, Row("sb", "ShieldBooster_HeavyDuty").MaxGrade);
        Assert.Null(Row("scb", "ShieldCellBank_Specialised").MaxGrade);
        Assert.True(Row("fsd", "FSD_LongRange").Experimentals);
        Assert.False(Row("scb", "ShieldCellBank_Specialised").Experimentals);
    }

    [Fact]
    public void ServiceFor_ReportsWhoCanRollAndApply()
    {
        var selected = new HashSet<string>(StringComparer.Ordinal) { "Felicity Farseer" };
        var biweave = PlanEntry.Create(Catalog, "bsg", "ShieldGenerator_Reinforced", 3, "special_shield_regenerative", PlanSource.Manual);

        var fsdService = EngineerCoverage.ServiceFor(Catalog, Fsd(), selected);
        Assert.Contains("Felicity Farseer", fsdService.SelectedCanRoll);
        Assert.Contains("Felicity Farseer", fsdService.SelectedCanApplyExperimental);

        var shieldService = EngineerCoverage.ServiceFor(Catalog, biweave, selected);
        Assert.NotEmpty(shieldService.CanRoll);
        Assert.Empty(shieldService.SelectedCanRoll);

        var unknown = EngineerCoverage.ServiceFor(Catalog, Fsd() with { ModuleType = null }, selected);
        Assert.Empty(unknown.CanRoll);
    }
}
