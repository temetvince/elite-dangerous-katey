using Katey.App.Exploration;
using Katey.App.Mvvm;
using Katey.App.Navigation;
using Katey.App.Network;
using Katey.App.Pages;
using Katey.App.Persistence;
using Katey.App.Rail;
using Katey.App.ViewModels;
using Katey.Core;
using Katey.Core.Exploration;
using Katey.Core.Fleet;
using Katey.Core.Planning;
using Katey.Core.Routing;
using Katey.Journal;
using Microsoft.Extensions.Logging.Abstractions;

namespace Katey.App.Tests;

/// <summary>
/// Checks the workbench's remembered choices and the features built on them: sorting, sections, group
/// state, the materials toggles, the fit comparison, experimental-only entries, the ship page's pickers,
/// cosmetic filtering, and fuel along the route.
/// </summary>
public sealed class WorkbenchFeatureTests
{
    private static readonly Catalog Catalog = Catalog.LoadEmbedded();
    private static readonly ModuleCatalog Modules = ModuleCatalog.LoadEmbedded();
    private static readonly ExobiologyPredictor Predictor = ExobiologyPredictor.LoadEmbedded();

    private const string Loadout = /*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:01Z","event":"Loadout","Ship":"anacondamkii","ShipID":3,"ShipName":"Ironic","ShipIdent":"AA-24M","UnladenMass":1250.5,"CargoCapacity":64,"FuelCapacity":{"Main":64,"Reserve":1.08},"Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_overcharge_size6_class5","On":true,"Priority":0,"Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"Modifiers":[{"Label":"MaxFuelPerJump","Value":9.6}]}},{"Slot":"MainEngines","Item":"int_engine_size7_class5","On":true,"Priority":0},{"Slot":"PowerPlant","Item":"int_powerplant_size7_class5","On":true,"Priority":0,"Engineering":{"BlueprintName":"PowerPlant_Boosted","Level":3,"ExperimentalEffect":"special_powerplant_highcharge"}},{"Slot":"ShipCockpit","Item":"anacondamkii_cockpit","On":true,"Priority":1},{"Slot":"Bobble01","Item":"bobble_station_coriolis","On":true,"Priority":1},{"Slot":"PaintJob","Item":"paintjob_anacondamkii_tactical_grey","On":true,"Priority":1}]}""";

    /// <summary>The same ship with the fixtures every ship carries, the approach suite and the cargo hatch, which the unused list must leave out.</summary>
    private static readonly string LoadoutWithFixtures = Loadout.Replace(
        """{"Slot":"ShipCockpit","Item":"anacondamkii_cockpit","On":true,"Priority":1},""",
        """{"Slot":"ShipCockpit","Item":"anacondamkii_cockpit","On":true,"Priority":1},{"Slot":"PlanetaryApproachSuite","Item":"int_planetapproachsuite_advanced","On":true,"Priority":1},{"Slot":"CargoHatch","Item":"modularcargobaydoorfdl","On":true,"Priority":1},""",
        StringComparison.Ordinal);

    private static (ShellViewModel Shell, FakeJournalMonitor Monitor, FakeStateStore Store) Create(PersistedState? state = null, FakeRoutePlotter? plotter = null, FakeJournalBackup? backup = null, FakeSystemCatalog? systems = null, FakeReleaseFeed? releases = null)
    {
        var store = new FakeStateStore { State = state ?? PersistedState.Empty };
        var sinks = new JournalSinks();
        var monitor = new FakeJournalMonitor(sinks);
        var shell = new ShellViewModel(Catalog, Modules, Predictor, store, monitor, sinks, plotter ?? new FakeRoutePlotter(), systems ?? new FakeSystemCatalog(), releases ?? new FakeReleaseFeed(), backup ?? new FakeJournalBackup(), action => action(), NullLogger<ShellViewModel>.Instance);
        return (shell, monitor, store);
    }

    private static T Open<T>(ShellViewModel shell, PageKind kind, string id = "")
        where T : Page
    {
        shell.Navigator.Open(new PageKey(kind, id));
        return Assert.IsType<T>(shell.Navigator.Current);
    }

    [Fact]
    public void Plan_CountsDownAsTheGameRolls_AndDropsTheEntryWhenItIsDone()
    {
        var (shell, monitor, store) = Create();
        var anchor = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var plant = PlanEntry.Create(Catalog, "pp", "PowerPlant_Boosted", 1, "special_powerplant_highcharge", PlanSource.Manual) with { CraftsAfter = anchor };
        var fsd = PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, null, PlanSource.Manual) with { CraftsAfter = anchor };
        shell.Workspace.Plan.Add([plant, fsd]);

        // A roll from before the entries existed changes nothing.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-10-20T12:00:00Z","event":"EngineerCraft","Slot":"PowerPlant","Module":"int_powerplant_size5_class5","Engineer":"Felicity Farseer","BlueprintName":"PowerPlant_Boosted","Level":1,"Quality":1.0,"ExperimentalEffect":"special_powerplant_highcharge"}""");
        Assert.Equal(2, shell.Workspace.Plan.Entries.Count);
        Assert.Equal(1, shell.Workspace.Plan.Entries[0].Entry.RollsAt(1));

        // The grade roll zeroes the grade and keeps the entry, wanting the effect; the drawer's rolls follow.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-07T17:02:00Z","event":"EngineerCraft","Slot":"PowerPlant","Module":"int_powerplant_size8_class5","Ingredients":[{"Name":"sulphur","Count":1}],"Engineer":"Felicity Farseer","BlueprintName":"PowerPlant_Boosted","Level":1,"Quality":1.0,"Modifiers":[]}""");
        var row = shell.Workspace.Plan.Entries[0];
        Assert.Equal(0, row.Entry.RollsAt(1));
        Assert.Equal(0, Assert.Single(row.Rolls).Count);
        Assert.Equal(new DateTimeOffset(2026, 11, 7, 17, 2, 0, TimeSpan.Zero), row.Entry.CraftsAfter);
        Assert.Equal(0, store.State.Plan[0].RollsAt(1));

        // The effect retires the entry, the save follows, and the other entry is untouched.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-07T17:02:09Z","event":"EngineerCraft","Slot":"PowerPlant","Module":"int_powerplant_size8_class5","ApplyExperimentalEffect":"special_powerplant_highcharge","Engineer":"Felicity Farseer","BlueprintName":"PowerPlant_Boosted","Level":1,"Quality":1.0,"ExperimentalEffect":"special_powerplant_highcharge","Modifiers":[]}""");
        var left = Assert.Single(shell.Workspace.Plan.Entries);
        Assert.Equal(fsd.Id, left.Entry.Id);
        Assert.Equal(5, left.Entry.RollsAt(5));
        Assert.Equal([fsd.Id], store.State.Plan.Select(entry => entry.Id));

        // An entry saved before anchors were recorded is anchored on load, so the history does not eat it.
        var (again, monitorAgain, _) = Create(new PersistedState([fsd with { CraftsAfter = null }], [], null, [], SharingPolicy.Pooled));
        Assert.NotNull(again.Workspace.Plan.Entries[0].Entry.CraftsAfter);
        monitorAgain.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-07T17:02:09Z","event":"EngineerCraft","Slot":"FrameShiftDrive","Module":"int_hyperdrive_size6_class5","Engineer":"Felicity Farseer","BlueprintName":"FSD_LongRange","Level":5,"Quality":1.0}""");
        Assert.Equal(5, Assert.Single(again.Workspace.Plan.Entries).Entry.RollsAt(5));
    }

    [Fact]
    public void Sorting_IsRemembered_AndAppliedOnEveryPageThatHasATable()
    {
        var (shell, _, store) = Create();
        var engineers = Open<EngineersPage>(shell, PageKind.Engineers);

        engineers.Sort["name"].Command.Execute(null);
        Assert.True(engineers.Sort["name"].IsActive);
        Assert.Equal("▲", engineers.Sort["name"].Indicator);
        var names = engineers.Engineers.Select(row => row.Name).ToList();
        Assert.Equal(names.Order(StringComparer.OrdinalIgnoreCase), names);
        engineers.Sort["name"].Command.Execute(null);
        Assert.Equal("▼", engineers.Sort["name"].Indicator);
        Assert.Equal(names.OrderDescending(StringComparer.OrdinalIgnoreCase), engineers.Engineers.Select(row => row.Name));
        Assert.StartsWith("name:desc;", store.State.Preferences!["sort.engineers"], StringComparison.Ordinal);

        engineers.Filter = "deciat";
        Assert.Equal(["Felicity Farseer"], engineers.Engineers.Select(row => row.Name));

        var (again, _, _) = Create(store.State);
        var restored = Open<EngineersPage>(again, PageKind.Engineers);
        Assert.Equal("▼", restored.Sort["name"].Indicator);
        Assert.Equal(names.OrderDescending(StringComparer.OrdinalIgnoreCase), restored.Engineers.Select(row => row.Name));
    }

    [Fact]
    public void ModuleGroups_StartClosed_OpenFromBreadcrumbs_AndRememberTheirState()
    {
        var (shell, _, store) = Create();
        var modules = Open<ModulesPage>(shell, PageKind.Modules);
        Assert.All(modules.Groups, group => Assert.False(group.IsExpanded));

        modules.Groups.Single(group => group.Group == ModuleGroup.Hardpoint).IsExpanded = true;
        Assert.Equal("Hardpoint", store.State.Preferences!["modules.expanded"]);

        var beams = Open<ModuleTypePage>(shell, PageKind.ModuleType, "bl");
        Assert.Equal(["Modules", "Hardpoints"], beams.Breadcrumbs.Select(link => link.Label));
        beams.Breadcrumbs[1].OpenCommand.Execute(null);
        var landed = Assert.IsType<ModulesPage>(shell.Navigator.Current);
        Assert.True(landed.Groups.Single(group => group.Group == ModuleGroup.Hardpoint).IsExpanded);

        landed.ExpandAllCommand.Execute(null);
        Assert.All(landed.Groups, group => Assert.True(group.IsExpanded));
        landed.CollapseAllCommand.Execute(null);
        Assert.All(landed.Groups, group => Assert.False(group.IsExpanded));
        Assert.Equal("", store.State.Preferences!["modules.expanded"]);

        var fsd = Open<ModuleTypePage>(shell, PageKind.ModuleType, "fsd");
        Assert.Equal("blueprints", fsd.Sections.Current);
        fsd.Sections.Show("owned");
        Assert.True(fsd.Sections["owned"].IsCurrent);
        Assert.Equal("owned", store.State.Preferences!["section.moduleType"]);
        var material = Open<MaterialPage>(shell, PageKind.Material, "Arsenic");
        Assert.Equal(["Materials", "Raw"], material.Breadcrumbs.Select(link => link.Label));
    }

    [Fact]
    public void MaterialsToggles_StartAllOn_AndAreRemembered()
    {
        var (shell, _, store) = Create();
        var materials = Open<MaterialsPage>(shell, PageKind.Materials);
        Assert.Equal(["Raw", "Manufactured", "Encoded"], materials.Sections.Select(section => section.Label));
        Assert.All(materials.Categories, toggle => Assert.True(toggle.IsShown));

        materials.Categories.Single(toggle => toggle.Category == MaterialCategory.Manufactured).IsShown = false;
        Assert.Equal(["Raw", "Encoded"], materials.Sections.Select(section => section.Label));
        Assert.Equal("Manufactured", store.State.Preferences!["materials.hidden"]);

        var (again, _, _) = Create(store.State);
        var restored = Open<MaterialsPage>(again, PageKind.Materials);
        Assert.Equal(["Raw", "Encoded"], restored.Sections.Select(section => section.Label));

        var viaCrumb = Open<MaterialsPage>(again, PageKind.Materials, "Manufactured");
        Assert.Equal(["Raw", "Manufactured", "Encoded"], viaCrumb.Sections.Select(section => section.Label));
    }

    [Fact]
    public void Reserves_AreSetOnTheMaterialsPage_AndDriveTheTradesOnThePageAndInTheDrawer()
    {
        var (shell, monitor, store) = Create();
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Materials","Raw":[{"Name":"carbon","Count":5},{"Name":"iron","Count":300}],"Manufactured":[],"Encoded":[]}""");

        var materials = Open<MaterialsPage>(shell, PageKind.Materials);
        Assert.StartsWith("Nothing to trade for", materials.Trades.Headline, StringComparison.Ordinal);
        var carbon = materials.Sections[0].Rows.Single(row => row.Name == "Carbon");
        Assert.Equal(MaterialCategory.Raw, materials.Sections[0].Category);
        Assert.False(carbon.HasMeter);
        carbon.KeepText = "20";
        var saved = Assert.Single(store.State.Reserves!);
        Assert.Equal(("carbon", 20, false), (saved.Symbol.ToLowerInvariant(), saved.Keep, saved.NeverTrade));

        Assert.True(materials.Trades.HasTrades);
        var group = Assert.Single(materials.Trades.Groups);
        Assert.Equal("At a raw materials trader", group.Heading);
        var trade = Assert.Single(group.Trades);
        Assert.Equal("Give 90 Iron", trade.GiveText);
        Assert.Equal("get 15 Carbon", trade.GetText);
        Assert.Equal("G1 → G1 at 6:1", trade.RateText);
        Assert.Equal("Iron family → Carbon family", trade.FamilyText);
        Assert.Equal("Give 90", trade.GiveCount);
        Assert.Equal(PageKind.Materials, trade.From.Target.Kind);
        Assert.Equal("iron", trade.From.Target.Id, ignoreCase: true);
        Assert.Equal("Carbon", trade.To.Label);
        trade.To.OpenCommand.Execute(null);
        var landed = Assert.IsType<MaterialsPage>(shell.Navigator.Current);
        Assert.Equal("carbon", landed.HighlightedSymbol, ignoreCase: true);
        var highlighted = Assert.Single(landed.Sections.SelectMany(section => section.Rows), row => row.IsHighlighted);
        Assert.Equal("Carbon", highlighted.Name);
        landed.ShortOnly = true;
        Assert.Contains(landed.Sections.SelectMany(section => section.Rows), row => row.IsHighlighted);
        landed.ShortOnly = false;
        materials = Open<MaterialsPage>(shell, PageKind.Materials);
        Assert.Equal("Back to your reserve: Carbon.", materials.Trades.ReserveNote);
        Assert.Equal("1 trade covers everything you are short of.", shell.PlanDrawer.Trades.Headline);

        var iron = materials.Sections[0].Rows.Single(row => row.Name == "Iron");
        iron.MayTrade = false;
        Assert.False(materials.Trades.HasTrades);
        Assert.Contains("never trade", Assert.Single(Assert.Single(materials.Trades.Groups).Unmet).Why, StringComparison.Ordinal);
        Assert.Equal(2, store.State.Reserves!.Count);

        var (again, _, _) = Create(store.State);
        var restored = Open<MaterialsPage>(again, PageKind.Materials);
        Assert.Equal("20", restored.Sections[0].Rows.Single(row => row.Name == "Carbon").KeepText);
        Assert.False(restored.Sections[0].Rows.Single(row => row.Name == "Iron").MayTrade);

        carbon.KeepText = "";
        Assert.Single(store.State.Reserves!);
    }

    [Fact]
    public void PrebuiltModules_AreFlaggedOnlyWhereEngineeringIsPlanned()
    {
        var fitting = new Fitting("fit-2", "anacondamkii", "Dirty drives",
        [
            new FitModule("MainEngines", "int_engine_size7_class5", new Engineering("Engine_Dirty", 5, null)),
            new FitModule("ShipCockpit", "anacondamkii_cockpit", null),
        ]);
        var (shell, monitor, store) = Create(PersistedState.Empty with { Fittings = [fitting] });
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T19:59:00Z","event":"ShipRedeemed","ShipType":"anacondamkii","NewShipID":3}""",
            Loadout);

        var ship = Open<ShipPage>(shell, PageKind.Ship, "3");
        Assert.Equal("Prebuilt ship - Still as redeemed: selling it and redeeming it again loses nothing.", ship.PrebuiltText);
        Assert.True(ship.Prebuilt!.IsUnchanged);
        Assert.True(ship.HasPrebuiltLineAlone);
        Assert.Empty(ship.PrebuiltChanges);
        Assert.All(ship.Modules, row => Assert.True(row.IsPrebuilt));
        Assert.True(ship.Modules.Single(row => row.Slot == "MainEngines").WarnPrebuilt);
        Assert.Equal(["FrameShiftDrive", "MainEngines", "PowerPlant"], ship.Modules.Where(row => row.WarnPrebuilt).Select(row => row.Slot));

        var fit = Open<FitPage>(shell, PageKind.Fit, "fit-2");
        var engines = fit.Differences.Single(row => row.Slot == "MainEngines");
        Assert.True(engines.NeedsEngineering);
        Assert.True(engines.WarnPrebuilt);
        Assert.Contains("1 slot to engineer holds a prebuilt module, which cannot leave this ship.", fit.CompareSummary, StringComparison.Ordinal);

        var (plain, plainMonitor, _) = Create(PersistedState.Empty with { Fittings = [fitting] });
        plainMonitor.Feed(Loadout);
        var plainShip = Open<ShipPage>(plain, PageKind.Ship, "3");
        Assert.All(plainShip.Modules, row => Assert.False(row.WarnPrebuilt));
        Assert.False(plainShip.HasPrebuiltText);

        // Swap the thrusters and the paint job: only the thrusters count as a change.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:05:00Z","event":"Loadout","Ship":"anacondamkii","ShipID":3,"ShipName":"Ironic","ShipIdent":"AA-24M","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_overcharge_size6_class5","On":true,"Priority":0,"Engineering":{"BlueprintName":"FSD_LongRange","Level":5}},{"Slot":"MainEngines","Item":"int_engine_size7_class5","On":true,"Priority":0,"Value":5000000,"Engineering":{"BlueprintName":"Engine_Dirty","Level":5}},{"Slot":"PowerPlant","Item":"int_powerplant_size7_class5","On":true,"Priority":0,"Engineering":{"BlueprintName":"PowerPlant_Boosted","Level":3,"ExperimentalEffect":"special_powerplant_highcharge"}},{"Slot":"PaintJob","Item":"paintjob_anacondamkii_tactical_red","On":true,"Priority":1}]}""");
        var changed = Open<ShipPage>(shell, PageKind.Ship, "3");
        Assert.Equal("Prebuilt ship - Changed since redemption: 1 slot differs from how it came, and selling it would lose that.", changed.PrebuiltText);
        Assert.True(changed.HasPrebuiltChanges);
        Assert.False(changed.HasPrebuiltLineAlone);
        Assert.True(changed.Prebuilt!.IsChanged);
        Assert.Equal("Changed since redemption", changed.Prebuilt.State);
        Assert.False(changed.ShowPrebuiltChanges);
        changed.ShowPrebuiltChanges = true;
        Assert.Equal("true", store.State.Preferences!["ship.prebuiltChanges"]);
        var row = Assert.Single(changed.PrebuiltChanges);
        Assert.Equal("Main Engines", row.Slot);
        Assert.EndsWith(", stock", row.Before, StringComparison.Ordinal);
        Assert.Contains(Catalog.Blueprints["Engine_Dirty"].Name + " G5", row.After, StringComparison.Ordinal);
        Assert.False(changed.Modules.Single(row => row.Slot == "MainEngines").IsPrebuilt);
        var plainFit = Open<FitPage>(plain, PageKind.Fit, "fit-2");
        Assert.DoesNotContain("prebuilt", plainFit.CompareSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void ExperimentalOnly_IsGradeZero_InBothDirections()
    {
        var entry = PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, "special_fsd_heavy", PlanSource.Manual);
        var (shell, _, _) = Create(PersistedState.Empty with { Plan = [entry] });
        var row = shell.Workspace.Plan.Entries[0];
        Assert.False(row.ExperimentalOnly);
        Assert.Equal([0, 1, 2, 3, 4, 5], row.Grades);

        row.ExperimentalOnly = true;
        Assert.Equal(0, row.TargetGrade);
        Assert.True(row.Entry.IsExperimentalOnly);
        Assert.Empty(row.Rolls);
        Assert.Equal(Catalog.Experimentals["special_fsd_heavy"].Components.Count, row.Materials.Count);
        Assert.StartsWith("Experimental only:", Phrase.Plain(row.CanRollParts), StringComparison.Ordinal);
        Assert.False(row.HasWarning);

        row.TargetGrade = 3;
        Assert.False(row.ExperimentalOnly);
        Assert.Equal(3, row.Rolls.Count);

        row.TargetGrade = 0;
        Assert.True(row.ExperimentalOnly);
        row.SelectedExperimental = row.Experimentals[0];
        Assert.True(row.HasWarning);
        Assert.Contains("needs nothing", row.Warning, StringComparison.Ordinal);

        row.ExperimentalOnly = false;
        Assert.Equal(5, row.TargetGrade);
    }

    [Fact]
    public void FitComparison_RanksShips_PlansTheGap_AndCountsOwnedModulesElsewhere()
    {
        var fitting = new Fitting("fit-1", "anacondamkii", "Dream",
        [
            new FitModule("FrameShiftDrive", "int_hyperdrive_overcharge_size6_class5", new Engineering("FSD_LongRange", 5, "special_fsd_heavy")),
            new FitModule("MainEngines", "int_engine_size7_class5", new Engineering("Engine_Dirty", 5, "special_engine_overloaded")),
            new FitModule("PowerPlant", "int_powerplant_size7_class5", new Engineering("PowerPlant_Boosted", 3, "special_powerplant_highcharge")),
            new FitModule("Slot01_Size7", "int_cargorack_size7_class1", null),
        ]);
        var (shell, monitor, _) = Create(PersistedState.Empty with { Fittings = [fitting], EngineerIds = [300100] });
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:00Z","event":"Location","StarSystem":"Deciat","SystemAddress":3,"StarPos":[122.625,-0.8125,-47.28125],"Docked":true,"StationName":"Farseer Inc"}""",
            Loadout,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:02Z","event":"StoredModules","MarketID":1,"StationName":"Farseer Inc","StarSystem":"Deciat","Items":[{"Name":"$int_engine_size7_class5_name;","Name_Localised":"Thrusters","StorageSlot":1,"StarSystem":"Deciat","MarketID":1,"EngineerModifications":"Engine_Dirty","Level":5,"Quality":1,"Hot":false}]}""");

        var fit = Open<FitPage>(shell, PageKind.Fit, "fit-1");
        Assert.True(fit.HasShips);
        var ship = Assert.Single(fit.Ships);
        Assert.Contains("Ironic (AA-24M) · Anaconda Mk II · 1 of 4 match", ship.Label, StringComparison.Ordinal);
        Assert.Equal("Anaconda Mk II", fit.HullName);
        Assert.Equal("", fit.HullNote);

        var kinds = fit.Differences.ToDictionary(row => row.Slot, row => row.KindText);
        Assert.Equal("experimental missing", kinds["FrameShiftDrive"]);
        Assert.Equal("not engineered", kinds["MainEngines"]);
        Assert.Equal("empty slot", kinds["Slot01_Size7"]);
        Assert.DoesNotContain("PowerPlant", kinds.Keys);

        var engines = fit.Differences.Single(row => row.Slot == "MainEngines");
        Assert.True(engines.HasOwned);
        Assert.False(engines.OwnedCovers);
        Assert.Contains("may already own one", engines.OwnedText, StringComparison.Ordinal);
        var fsd = fit.Differences.Single(row => row.Slot == "FrameShiftDrive");
        Assert.True(fsd.NeedsEngineering);
        Assert.True(fsd.CoveredBySelection);
        Assert.StartsWith("Your engineers: Felicity Farseer", Phrase.Plain(fsd.EngineersParts), StringComparison.Ordinal);
        // The name is a link, and the engineer is ticked, so it reads in the HUD blue only while the switch narrows the lists.
        var farseer = fsd.EngineersParts.Single(part => part.IsLink);
        Assert.Equal(shell.Workspace.Scope.IsNarrowed ? LinkTone.Hud : LinkTone.Ink, farseer.Tone);
        Assert.False(engines.CoveredBySelection);
        Assert.Contains("None of your engineers", Phrase.Plain(engines.EngineersParts), StringComparison.Ordinal);
        Assert.Equal(2, fit.EngineeringCount);
        Assert.Equal(2, fit.UnplannedDifferences);
        Assert.Equal("Plan the 2 engineering changes for this ship", fit.PlanDifferencesText);
        Assert.Contains("1 of 4 modules already match.", fit.CompareSummary, StringComparison.Ordinal);
        Assert.Contains("1 of them with engineers you will visit", fit.CompareSummary, StringComparison.Ordinal);

        fit.PlanDifferencesCommand.Execute(null);
        var entries = shell.Workspace.Plan.Entries.Select(row => row.Entry).ToList();
        Assert.Equal(2, entries.Count);
        Assert.Equal(0, fit.UnplannedDifferences);
        Assert.Equal("Every engineering change for this ship is in the plan", fit.PlanDifferencesText);
        Assert.False(fit.PlanDifferencesCommand.CanExecute(null));
        var planFsd = entries.Single(entry => entry.Blueprint == "FSD_LongRange");
        Assert.True(planFsd.IsExperimentalOnly);
        Assert.Equal("special_fsd_heavy", planFsd.Experimental);
        Assert.Equal("Ironic (AA-24M)", planFsd.Source.Ship);
        var planEngines = entries.Single(entry => entry.Blueprint == "Engine_Dirty");
        Assert.Equal(5, planEngines.TargetGrade);

        // The ship page stamps its entries the same way, so it sees the fit page's work as planned for this ship, and another ship's page would not.
        var ironic = Open<ShipPage>(shell, PageKind.Ship, "3");
        var ironicFsd = ironic.Modules.Single(row => row.Slot == "FrameShiftDrive");
        ironicFsd.SelectedExperimental = ironicFsd.Experimentals.Single(choice => choice.Key == "special_fsd_heavy");
        Assert.True(ironicFsd.IsPlanned);
        Assert.False(PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 0, "special_fsd_heavy", new PlanSource("Other ship", "FrameShiftDrive", planFsd.Source.Item)).IsSameWorkAs(planFsd));
        fit = Open<FitPage>(shell, PageKind.Fit, "fit-1");

        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:03Z","event":"StoredModules","MarketID":1,"StationName":"Farseer Inc","StarSystem":"Deciat","Items":[{"Name":"$int_engine_size7_class5_name;","StorageSlot":1,"StarSystem":"Deciat","MarketID":1,"EngineerModifications":"Engine_Dirty","Level":5,"Quality":1,"Hot":false}]}""");
        fit = Assert.IsType<FitPage>(shell.Navigator.Current);
        fit.UseOwned = false;
        Assert.False(fit.Differences.Single(row => row.Slot == "MainEngines").HasOwned);
        fit.UseOwned = true;
        fit.SelectedPlace = fit.Places.Single(choice => choice.Key == nameof(PlaceFilter.FittedOnly));
        fit = Assert.IsType<FitPage>(shell.Navigator.Current);
        Assert.False(fit.Differences.Single(row => row.Slot == "MainEngines").HasOwned);
        Assert.Equal(PlaceFilter.FittedOnly, shell.Workspace.Fleet.Place);
    }

    [Fact]
    public void UnfinishedGrade_ShowsItsProgress_AndPlansOnlyTheRollsStillOwed()
    {
        // The ship carries the plant at grade 3 and a quarter; the fit wants grade 3 complete with an effect.
        var fitting = new Fitting("fit-4", "anacondamkii", "Boosted", [new FitModule("PowerPlant", "int_powerplant_size7_class5", new Engineering("PowerPlant_Boosted", 3, "special_powerplant_highcharge"))]);
        var (shell, monitor, _) = Create(PersistedState.Empty with { Fittings = [fitting] });
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:01Z","event":"Loadout","Ship":"anacondamkii","ShipID":3,"ShipName":"Ironic","ShipIdent":"AA-24M","UnladenMass":1250.5,"FuelCapacity":{"Main":64,"Reserve":1.08},"Modules":[{"Slot":"PowerPlant","Item":"int_powerplant_size7_class5","On":true,"Priority":1,"Value":1,"Engineering":{"BlueprintName":"PowerPlant_Boosted","Level":3,"Quality":0.25}}]}""");

        var ship = Open<ShipPage>(shell, PageKind.Ship, "3");
        Assert.Equal(Catalog.Blueprints["PowerPlant_Boosted"].Name + " G3, 25%", ship.Modules.Single(row => row.Slot == "PowerPlant").Engineering);

        var fit = Open<FitPage>(shell, PageKind.Fit, "fit-4");
        var line = Assert.Single(fit.Lines);
        Assert.Equal(MatchQuality.Partial, line.Quality);
        Assert.Equal(StatusTone.Change, line.Tone);
        Assert.Equal("partial: fitted to Ironic, G3 at 25%", line.Status);
        Assert.Equal("grade unfinished", Assert.Single(fit.Differences).KindText);

        // The plan starts from where the module stands: nothing at grades 1 and 2, three quarters of grade 3's three rolls, rounded up to three, and the effect.
        fit.PlanDifferencesCommand.Execute(null);
        var planned = Assert.Single(shell.Workspace.Plan.Entries).Entry;
        Assert.Equal(3, planned.TargetGrade);
        Assert.Equal([0, 0, 3], planned.RolledGrades(Catalog).Select(planned.RollsAt));
        Assert.Equal("special_powerplant_highcharge", planned.Experimental);
    }

    [Fact]
    public void FitLineStatus_IsWordedAndColouredByWhereTheModuleIs()
    {
        var required = new FitModule("PowerPlant", "int_powerplant_size5_class5", null);
        OwnedModule At(ModulePlace place, string? hull, bool prebuilt = false) => new("m", required.Item, null, place, "Deciat", place == ModulePlace.Stored ? "Farseer Inc" : "Ironic", hull, prebuilt);

        var fitted = new FitLine(required, At(ModulePlace.Installed, "mandalay"), MatchQuality.Exact);
        Assert.Equal("fitted to Ironic", FleetPage.StatusText(fitted));
        Assert.Equal(StatusTone.Fitted, FleetPage.ToneOf(fitted, "mandalay"));
        Assert.Equal(StatusTone.Elsewhere, FleetPage.ToneOf(fitted, "anaconda"));

        var prebuilt = new FitLine(required, At(ModulePlace.Installed, "mandalay", prebuilt: true), MatchQuality.Exact);
        Assert.Equal("fitted to Ironic, prebuilt", FleetPage.StatusText(prebuilt));
        Assert.Equal(StatusTone.Fitted, FleetPage.ToneOf(prebuilt, "mandalay"));

        var stored = new FitLine(required, At(ModulePlace.Stored, null), MatchQuality.Exact);
        Assert.Equal("stored at Farseer Inc", FleetPage.StatusText(stored));
        Assert.Equal(StatusTone.Stored, FleetPage.ToneOf(stored, "mandalay"));

        var doubtful = new FitLine(required, At(ModulePlace.Stored, null), MatchQuality.ExperimentalUnknown);
        Assert.Equal(StatusTone.Change, FleetPage.ToneOf(doubtful, "mandalay"));
        Assert.Equal(StatusTone.Change, FleetPage.ToneOf(new FitLine(required, At(ModulePlace.Installed, "mandalay"), MatchQuality.Partial), "mandalay"));
        Assert.Equal(StatusTone.Missing, FleetPage.ToneOf(new FitLine(required, null, MatchQuality.Missing), "mandalay"));
    }

    [Fact]
    public void Farness_IsZeroHere_LightYearsToAVisitedSystem_AndInfiniteElsewhere()
    {
        var (shell, monitor, _) = Create();
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:00Z","event":"Location","StarSystem":"Deciat","SystemAddress":3,"StarPos":[122.625,-0.8125,-47.28125],"Docked":true,"StationName":"Farseer Inc"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:02Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625],"JumpDist":49.028,"FuelUsed":9.6,"FuelLevel":20.0}""");
        static OwnedModule At(string? system) => new("m", "int_powerplant_size5_class5", null, ModulePlace.Stored, system, "storage", null);

        Assert.Equal(0, shell.Workspace.Farness(At("koine")));
        Assert.Equal(49.03, shell.Workspace.Farness(At("Deciat")), 0.01);
        Assert.Equal(double.PositiveInfinity, shell.Workspace.Farness(At("Sol")));
        Assert.Equal(double.PositiveInfinity, shell.Workspace.Farness(At(null)));
    }

    [Fact]
    public void PlanButtons_RecogniseEachOthersEntries_AcrossTheFitAndTheShip()
    {
        // The fit wants the ship's own drive with an experimental it lacks: one gap in what you own, one change for this ship, the same job.
        var fitting = new Fitting("fit-3", "anacondamkii", "Heavy", [new FitModule("FrameShiftDrive", "int_hyperdrive_overcharge_size6_class5", new Engineering("FSD_LongRange", 5, "special_fsd_heavy"))]);
        var (shell, monitor, _) = Create(PersistedState.Empty with { Fittings = [fitting] });
        monitor.Feed(Loadout);

        var fit = Open<FitPage>(shell, PageKind.Fit, "fit-3");
        Assert.Equal(1, fit.MissingEngineered);
        Assert.Equal(1, fit.UnplannedDifferences);

        // Planning the gap in what you own stands both buttons down, since the ship's change is that same work.
        fit.PlanMissingCommand.Execute(null);
        Assert.Single(shell.Workspace.Plan.Entries);
        Assert.Equal(0, fit.MissingEngineered);
        Assert.Equal(0, fit.UnplannedDifferences);
        Assert.False(fit.PlanDifferencesCommand.CanExecute(null));
        fit.PlanDifferencesCommand.Execute(null);
        Assert.Single(shell.Workspace.Plan.Entries);

        // The ship page counts a fit's entry for its hull as this ship's job too.
        var ship = Open<ShipPage>(shell, PageKind.Ship, "3");
        var fsd = ship.Modules.Single(row => row.Slot == "FrameShiftDrive");
        fsd.SelectedExperimental = fsd.Experimentals.Single(choice => choice.Key == "special_fsd_heavy");
        Assert.True(fsd.IsPlanned);
        Assert.False(fsd.AddCommand.CanExecute(null));
    }

    [Fact]
    public void ShipPage_HidesCosmetics_AndPlansFromEachModule()
    {
        var (shell, monitor, _) = Create();
        monitor.Feed(LoadoutWithFixtures);
        var fleet = Open<FleetPage>(shell, PageKind.Fleet);
        Assert.Equal("Anaconda Mk II", Assert.Single(fleet.Ships).TypeName);
        Assert.Equal("ships", fleet.Sections.Current);
        fleet.Sections.Show("unused");
        Assert.DoesNotContain(fleet.Unused, row => row.Name.Contains("cockpit", StringComparison.OrdinalIgnoreCase) || row.Name.Contains("Paintjob", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fleet.Unused, row => row.Name.Contains("Approach Suite", StringComparison.OrdinalIgnoreCase) || row.Name.Contains("Cargo Bay Door", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fleet.Unused, row => row.Name.StartsWith("Power Plant", StringComparison.Ordinal));
        fleet.SelectedPlace = fleet.Places.Single(choice => choice.Key == nameof(PlaceFilter.StoredOnly));
        fleet = Assert.IsType<FleetPage>(shell.Navigator.Current);
        Assert.Empty(fleet.Unused);

        Assert.Equal("location unknown", Assert.Single(Open<FleetPage>(shell, PageKind.Fleet).Ships).Where.Text);
        var ship = Open<ShipPage>(shell, PageKind.Ship, "3");
        Assert.Equal("Anaconda Mk II", ship.TypeName);
        Assert.False(ship.Where.HasSystem);
        Assert.Equal(["CargoHatch", "FrameShiftDrive", "MainEngines", "PlanetaryApproachSuite", "PowerPlant"], ship.Modules.Select(row => row.Slot));
        Assert.Equal(["Cargo Hatch", "Frame Shift Drive", "Main Engines", "Planetary Approach Suite", "Power Plant"], ship.Modules.Select(row => row.SlotText));
        Assert.True(ship.CanExport);
        Assert.Contains("\"MaxFuelPerJump\"", ship.EdsyJson, StringComparison.Ordinal);
        shell.Navigator.Open(new PageKey(PageKind.Fit, "none"));
        Assert.IsType<NotFoundPage>(shell.Navigator.Current);
        ship = Open<ShipPage>(shell, PageKind.Ship, "3");

        var fsd = ship.Modules.Single(row => row.Slot == "FrameShiftDrive");
        Assert.True(fsd.CanPlan);
        Assert.Equal("FSD_LongRange", fsd.SelectedBlueprint?.Key);
        Assert.Equal(0, fsd.SelectedGrade);
        fsd.SelectedExperimental = fsd.Experimentals.Single(choice => choice.Key == "special_fsd_heavy");
        Assert.False(fsd.IsPlanned);
        Assert.Equal("Add to plan", fsd.AddText);
        fsd.AddCommand.Execute(null);

        // The rows are rebuilt as the plan changes: the same choice now reads as planned and its button stands down, while another choice does not.
        var fsdAgain = ship.Modules.Single(row => row.Slot == "FrameShiftDrive");
        fsdAgain.SelectedExperimental = fsdAgain.Experimentals.Single(choice => choice.Key == "special_fsd_heavy");
        Assert.True(fsdAgain.IsPlanned);
        Assert.Equal("In the plan", fsdAgain.AddText);
        Assert.False(fsdAgain.AddCommand.CanExecute(null));
        fsdAgain.SelectedGrade = 5;
        Assert.False(fsdAgain.IsPlanned);

        var powerPlant = ship.Modules.Single(row => row.Slot == "PowerPlant");
        Assert.Equal(3, powerPlant.SelectedGrade);
        Assert.Equal("special_powerplant_highcharge", powerPlant.SelectedExperimental.Key);
        powerPlant.SelectedGrade = 5;
        powerPlant.AddCommand.Execute(null);

        var engines = ship.Modules.Single(row => row.Slot == "MainEngines");
        Assert.Equal(Catalog.Blueprints[engines.SelectedBlueprint!.Key].MaxGrade, engines.SelectedGrade);

        var entries = shell.Workspace.Plan.Entries.Select(row => row.Entry).ToList();
        Assert.Equal(2, entries.Count);
        Assert.True(entries[0].IsExperimentalOnly);
        Assert.Equal("Ironic (AA-24M)", entries[0].Source.Ship);
        Assert.Equal(5, entries[1].TargetGrade);
    }

    [Fact]
    public void Route_ShowsTravelledDistance_AndFuelAlongTheWay()
    {
        var (shell, monitor, _) = Create();
        monitor.Route = new NavRoute(DateTimeOffset.UtcNow,
        [
            new("Koine", 9, "M", 107.40625, -40.59375, -71.5625),
            new("Deciat", 3, "K", 122.625, -0.8125, -47.28125),
            new("Sol", 1, "G", 0, 0, 0),
        ]);
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x10, null, null, 15.0);
        monitor.Feed(
            Loadout,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:02Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625],"JumpDist":49.028,"FuelUsed":9.6,"FuelLevel":20.0}""");

        var route = Open<RoutePage>(shell, PageKind.Route);
        Assert.Equal("Jump 1 of 3, 2 to go.", route.Summary);
        Assert.StartsWith("0.0 ly of ", route.TravelledText, StringComparison.Ordinal);
        Assert.EndsWith(" travelled (0%).", route.TravelledText, StringComparison.Ordinal);
        Assert.Equal(["here", "next", ""], route.Stops.Select(stop => stop.StageText));
        Assert.Equal(49.0, route.Stops[1].CumulativeLy, 0.1);
        Assert.Equal(route.Stops[2].CumulativeLy, route.Stops[1].CumulativeLy + route.Stops[2].JumpLy, 1e-9);

        Assert.Contains("Fuel for 1 more jump without scooping.", route.Fuel.Summary, StringComparison.Ordinal);
        Assert.Contains("Scoop by Deciat, stop 2.", route.Fuel.Summary, StringComparison.Ordinal);
        Assert.Equal("Deciat", route.Fuel.SummaryParts.Single(part => part.IsLink).Text);
        Assert.Equal("", route.Stops[0].FuelText);
        Assert.Equal("5.4 t", route.Stops[1].FuelText);
        Assert.True(route.Stops[1].ScoopHere);
        Assert.False(route.Stops[2].IsOutOfFuel);
        Assert.Equal(shell.Rail.Fuel.Summary, route.Fuel.Summary);
        Assert.Equal(shell.Rail.TravelledText, route.TravelledText);

        // Mid-jump the tank already shows the burn: the stop being arrived at shows the reading, and the blue bar is the jump after.
        var beforeJump = route.Fuel.NextJumpTonnes;
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x40000000, null, null, 9.6);
        route = Open<RoutePage>(shell, PageKind.Route);
        Assert.Equal("9.6 t", route.Stops[1].FuelText);
        Assert.True(route.Fuel.HasNextJump);
        Assert.True(beforeJump > 0);
        // The projection starts at Deciat with the reading aboard: the Sol jump costs the drive's whole cap, leaving nothing.
        Assert.Equal("0.0 t", route.Stops[2].FuelText);
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x10, null, null, 15.0);
        route = Open<RoutePage>(shell, PageKind.Route);

        // The tank reads 15 t of the Anaconda's 64 t: the meter and its label agree on both surfaces.
        Assert.True(route.Fuel.HasTank);
        Assert.Equal("15.0 / 64 t", route.Fuel.TankText);
        Assert.Equal(15.0 / 64, route.Fuel.TankFraction, 1e-9);
        Assert.True(route.Fuel.TankIsLow);
        Assert.Equal(route.Fuel.TankText, shell.Rail.Fuel.TankText);
        Assert.Equal(route.Fuel.TankFraction, shell.Rail.Fuel.TankFraction, 1e-9);

        // The next jump is beyond what the drive can make on one burn, so it costs the drive's maximum, 9.6 t: the blue part of the bar, leaving 5.4 t in orange.
        Assert.True(route.Fuel.HasNextJump);
        Assert.Equal("-9.6 t", route.Fuel.NextJumpText);
        Assert.Equal(5.4 / 64, route.Fuel.TankAfterJumpFraction, 1e-3);
        Assert.Equal(route.Fuel.TankAfterJumpFraction, shell.Rail.Fuel.TankAfterJumpFraction, 1e-9);
        Assert.Equal("K", route.Stops[1].StarClass);
        Assert.Equal("K", shell.Rail.RouteStops[1].StarClass);

        // The start page's figures: two jumps left with fuel for one, so the route tile is a caution.
        var home = Open<HomePage>(shell, PageKind.Home);
        var jumps = home.Stats.Single(stat => stat.Label == "JUMPS LEFT");
        Assert.Equal("2", jumps.Value);
        Assert.Equal(StatTone.Warn, jumps.Tone);
        Assert.Contains("1 jump before you must scoop", jumps.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Unlocks_JoinThePlanWhenTicked_AndTheBudgetFollows()
    {
        var (shell, monitor, store) = Create();
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Materials","Raw":[],"Manufactured":[{"Name":"guardian_powercell","Count":30},{"Name":"guardian_techcomponent","Count":25},{"Name":"focuscrystals","Count":24},{"Name":"phasealloys","Count":18}],"Encoded":[{"Name":"guardian_moduleblueprint","Count":2},{"Name":"ancientbiologicaldata","Count":20}]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:01Z","event":"TechnologyBroker","BrokerType":"guardian","MarketID":1,"ItemsUnlocked":[{"Name":"Int_GuardianShieldReinforcement_Size1_Class1"}],"Commodities":[],"Materials":[]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:02Z","event":"Loadout","Ship":"krait_mkii","ShipID":7,"ShipName":"Owl","ShipIdent":"OW-01","Modules":[{"Slot":"Slot03_Size5","Item":"int_guardianpowerplant_size5_class1","On":true,"Priority":0}]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:03Z","event":"ModuleBuy","Slot":"Slot06_Size3","BuyItem":"$int_guardianhullreinforcement_size3_class2_name;","BuyItem_Localised":"Guardian Hull Reinforcement","MarketID":1,"BuyPrice":1,"Ship":"krait_mkii","ShipID":7}""");

        var page = Open<UnlocksPage>(shell, PageKind.Unlocks);
        Assert.Equal("guardian", page.Sections.Current);
        Assert.Equal(29, page.Rows.Count);
        Assert.Equal(["Modules", "Weapons", "Modified weapons", "Fighters"], page.Groups.Select(group => group.Label));
        Assert.All(page.Groups, group => Assert.True(group.IsExpanded));
        Assert.Equal(6, page.Groups[0].Rows.Count);
        Assert.StartsWith("6 unlocks, 3 bought", page.Groups[0].Summary, StringComparison.Ordinal);
        page.CollapseAllCommand.Execute(null);
        Assert.All(page.Groups, group => Assert.False(group.IsExpanded));
        Assert.Equal("", store.State.Preferences!["unlocks.expanded"]);
        page.Groups[0].IsExpanded = true;
        var booster = page.Rows.Single(row => row.Key == "guardian_fsd_booster");
        var distributor = page.Rows.Single(row => row.Key == "guardian_power_distributor");
        var shield = page.Rows.Single(row => row.Key == "guardian_shield_reinforcement");
        Assert.Equal(UnlockState.Affordable, booster.State);
        Assert.Equal(UnlockState.Affordable, distributor.State);
        Assert.Equal(UnlockState.Unlocked, shield.State);
        Assert.Equal("The journal records the purchase.", shield.UnlockedBecause);
        Assert.False(shield.CanSelect);
        Assert.False(shield.IsDim);
        // The power plant's purchase predates the history, but the module sits on a ship, which proves it.
        var powerPlant = page.Rows.Single(row => row.Key == "guardian_hybrid_power_plant");
        Assert.Equal(UnlockState.Unlocked, powerPlant.State);
        Assert.Equal("You own one, so it is unlocked.", powerPlant.UnlockedBecause);
        // The hull reinforcement was bought at outfitting without a broker event in the history: that proves it too.
        var hull = page.Rows.Single(row => row.Key == "guardian_hull_reinforcement");
        Assert.Equal(UnlockState.Unlocked, hull.State);
        Assert.Equal("You have bought one at outfitting, so it is unlocked.", hull.UnlockedBecause);
        Assert.Equal("", booster.UnlockedBecause);
        Assert.Equal("Guardian FSD Booster", booster.TypeLink?.Label);
        Assert.Contains("8× HN Shock Mount", booster.CommoditiesText, StringComparison.Ordinal);
        Assert.False(page.HasPlanned);

        // Ticking the booster puts it in the plan; the plan claims its 21 power cells first, so the distributor falls out of reach.
        booster.IsSelected = true;
        page = Assert.IsType<UnlocksPage>(shell.Navigator.Current);
        booster = page.Rows.Single(row => row.Key == "guardian_fsd_booster");
        distributor = page.Rows.Single(row => row.Key == "guardian_power_distributor");
        Assert.Equal(UnlockState.Planned, booster.State);
        Assert.True(booster.IsSelected);
        Assert.Equal(UnlockState.OutOfReach, distributor.State);
        Assert.True(distributor.IsDim);
        Assert.True(distributor.CanSelect);
        Assert.Equal("Short 15 Guardian Power Cell.", distributor.ShortText);
        Assert.True(page.HasPlanned);
        Assert.Equal(["HN Shock Mount"], page.Commodities.Select(total => total.Name));
        Assert.Contains(page.Materials, row => row.Name == "Guardian Power Cell" && row.IsCovered);

        var entry = Assert.Single(shell.Workspace.Plan.Entries);
        Assert.True(entry.IsUnlock);
        Assert.Equal("Guardian FSD Booster", entry.Title);
        Assert.Equal("Guardian technology broker", entry.SourceLabel);
        Assert.Empty(entry.Grades);
        Assert.Contains("HN Shock Mount", entry.CommoditiesText, StringComparison.Ordinal);
        Assert.Equal(8, shell.PlanDrawer.Totals.Commodities.Single().Needed);
        Assert.Contains("Buy at a market", shell.PlanDrawer.Totals.Text, StringComparison.Ordinal);
        Assert.Equal("guardian_fsd_booster", Assert.Single(store.State.Plan).Unlock);

        // The dimmed one can still be ticked; it is then a short entry like any other.
        distributor.IsSelected = true;
        page = Assert.IsType<UnlocksPage>(shell.Navigator.Current);
        Assert.Equal(2, shell.Workspace.Plan.Entries.Count);
        Assert.Contains(page.Materials, row => row.Name == "Guardian Power Cell" && row.IsShort);

        // Unticking the booster removes its entry; the distributor, still planned, now claims the power cells, so the booster is the one out of reach.
        page.Rows.Single(row => row.Key == "guardian_fsd_booster").IsSelected = false;
        page = Assert.IsType<UnlocksPage>(shell.Navigator.Current);
        Assert.Equal(UnlockState.OutOfReach, page.Rows.Single(row => row.Key == "guardian_fsd_booster").State);
        Assert.Equal(UnlockState.Planned, page.Rows.Single(row => row.Key == "guardian_power_distributor").State);

        // A saved unlock entry comes back on the next start.
        var (restarted, _, _) = Create(store.State);
        var restored = Assert.Single(restarted.Workspace.Plan.Entries);
        Assert.Equal("guardian_power_distributor", restored.Entry.Unlock);

        // Unticking the distributor too brings both back into reach.
        page.Rows.Single(row => row.Key == "guardian_power_distributor").IsSelected = false;
        page = Assert.IsType<UnlocksPage>(shell.Navigator.Current);
        Assert.Empty(shell.Workspace.Plan.Entries);
        Assert.Equal(UnlockState.Affordable, page.Rows.Single(row => row.Key == "guardian_fsd_booster").State);
        Assert.Equal(UnlockState.Affordable, page.Rows.Single(row => row.Key == "guardian_power_distributor").State);

        page.Sections.Show("human");
        page = Assert.IsType<UnlocksPage>(shell.Navigator.Current);
        Assert.Equal(21, page.Rows.Count);
        Assert.Contains(page.Rows, row => row.Name == "Shock Cannon (Fixed, Small)" && row.HasType);
        Assert.Contains(page.Rows, row => row.Key == "engineered_fsd_v1" && row.HasDetectNote);

        shell.Search.Query = "gauss";
        Assert.Contains(shell.Search.Results, result => result.Kind == "Unlock" && result.Link.Label == "Guardian Gauss Cannon (Fixed, Small)");
    }

    [Fact]
    public void ScanProgress_CountsBodiesKnownFromAnEarlierVisit_OnTheRailThePageAndTheHomeTile()
    {
        var (shell, monitor, _) = Create();
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:47:26Z","event":"FSDJump","StarSystem":"Jackson's Lighthouse","SystemAddress":5532807773,"StarPos":[157,-27,-70]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:47:45Z","event":"FSSDiscoveryScan","Progress":1.0,"BodyCount":7,"NonBodyCount":54,"SystemName":"Jackson's Lighthouse","SystemAddress":5532807773}""");

        // Scanned on a visit before the history began: the honk found every body and the game wrote no Scan.
        Assert.Equal("7 of 7 scanned", shell.Rail.ScanText);
        Assert.Equal("This system · 7 of 7 scanned", shell.Rail.SystemHeading);
        Assert.Equal(1.0, shell.Rail.ScanFraction);
        Assert.Equal("No journal details for 7 bodies scanned on an earlier visit.", shell.Rail.BodiesNote);

        var systemPage = Open<SystemPage>(shell, PageKind.System);
        Assert.Equal("You are here. 7 of 7 bodies scanned.", Phrase.Plain(systemPage.WhereParts));
        Assert.Equal("No journal details for 7 bodies scanned on an earlier visit.", systemPage.Progress);

        var home = Open<HomePage>(shell, PageKind.Home);
        var scanned = home.Stats.Single(stat => stat.Label == "BODIES SCANNED");
        Assert.Equal("7/7", scanned.Value);
        Assert.Equal(StatTone.Good, scanned.Tone);
        Assert.Equal("in this system.", scanned.Detail);
        // The system's name is the link under the figure, and the browse tile's detail is the same link.
        Assert.Equal("Jackson's Lighthouse", scanned.Link.Label);
        var tile = home.Browse.Single(tile => tile.Link.Label == "This system");
        Assert.Equal("Jackson's Lighthouse", tile.Detail);
        Assert.True(tile.HasDetailLink);
        Assert.Equal("Jackson's Lighthouse", tile.DetailLink!.Label);

        // A fresh system counts what this visit scans; the partial honk figure is weighted and counts for nothing.
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:00Z","event":"FSDJump","StarSystem":"Fresh","SystemAddress":77,"StarPos":[158,-27,-70]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Fresh","BodyID":0,"StarType":"K","DistanceFromArrivalLS":0,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:10Z","event":"FSSDiscoveryScan","Progress":0.639665,"BodyCount":8,"NonBodyCount":0,"SystemName":"Fresh","SystemAddress":77}""");
        Assert.Equal("1 of 8 scanned", shell.Rail.ScanText);
        Assert.Equal("You are here. 1 of 8 bodies scanned.", Phrase.Plain(Open<SystemPage>(shell, PageKind.System).WhereParts));

        // Bodies list star by star: each star with its class in words, then what orbits it by distance; a star has no mapping note.
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:30Z","event":"Scan","ScanType":"Detailed","BodyName":"Fresh B","BodyID":2,"StarType":"N","DistanceFromArrivalLS":60000,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:31Z","event":"Scan","ScanType":"Detailed","BodyName":"Fresh B 1","BodyID":3,"Parents":[{"Star":2}],"PlanetClass":"Rocky body","DistanceFromArrivalLS":60100,"Landable":true,"SurfaceGravity":10.5,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:32Z","event":"Scan","ScanType":"Detailed","BodyName":"Fresh 1","BodyID":1,"Parents":[{"Star":0}],"PlanetClass":"Icy body","DistanceFromArrivalLS":900,"Landable":true,"SurfaceGravity":15.69,"SystemAddress":77}""");
        var bodies = Open<SystemPage>(shell, PageKind.System).Bodies;
        Assert.Equal(["Fresh", "1", "B", "B 1"], bodies.Select(body => body.Name));
        Assert.Equal("Class K star", bodies[0].Class);
        Assert.True(bodies[0].IsScoopable);
        Assert.Equal("Neutron star", bodies[2].Class);
        Assert.True(bodies[2].IsUnscoopableStar);
        Assert.False(bodies[1].IsUnscoopableStar);
        Assert.False(bodies[0].FirstMappingOpen);
        Assert.Equal("", bodies[0].MappingStatus);
        Assert.Equal("", bodies[0].FootfallStatus);
        Assert.True(bodies[1].FirstMappingOpen);
        Assert.Equal("unmapped", bodies[1].MappingStatus);
        Assert.True(bodies[1].MappingYours);
        Assert.Equal("discovered by you", bodies[1].DiscoveryStatus);
        Assert.True(bodies[1].DiscoveryYours);
        Assert.Equal("no footfall", bodies[1].FootfallStatus);
        Assert.True(bodies[1].FootfallOpen);
        Assert.True(bodies[1].FootfallYours);
        Assert.Equal("", bodies[1].StatusDetail);
        Assert.True(bodies[1].FirstLogLikely);
        Assert.Equal("4 of 8 scanned", shell.Rail.ScanText);

        // Gravity is banded for the landing: 1.60 g is extreme, 1.07 g high, and a star has none.
        Assert.Equal("", bodies[0].GravityText);
        Assert.Equal("1.60g", bodies[1].GravityText);
        Assert.True(bodies[1].IsExtremeGravity);
        Assert.Equal("extreme gravity", bodies[1].GravityWarning);
        Assert.Equal("1.07g", bodies[3].GravityText);
        Assert.True(bodies[3].IsHighGravity);
        Assert.False(bodies[3].IsExtremeGravity);

        // Biological signals before the scanner names the genera: the value is predicted from the conditions and marked as such.
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:40Z","event":"Scan","ScanType":"Detailed","BodyName":"Fresh 2","BodyID":4,"Parents":[{"Star":0}],"PlanetClass":"Rocky body","Atmosphere":"thin carbon dioxide atmosphere","Landable":true,"SurfaceGravity":3.92,"SurfaceTemperature":210,"SurfacePressure":900,"DistanceFromArrivalLS":1200,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:41Z","event":"FSSBodySignals","BodyName":"Fresh 2","BodyID":4,"SystemAddress":77,"Signals":[{"Type":"$SAA_SignalType_Biological;","Count":2}]}""");
        var living = Open<SystemPage>(shell, PageKind.System).Bodies.Single(body => body.Name == "2");
        Assert.True(living.PredictedExobiologyCredits > 0);
        Assert.True(living.IsExobiologyPredicted);
        // Nobody had mapped it, so the first-logged bonus is likely and the figure is five times the base.
        Assert.True(living.FirstLogLikely);
        Assert.Equal(living.PredictedExobiologyCredits * 5, living.ExobiologyRank);
        Assert.NotEmpty(living.PossibleLife);
        Assert.Equal("2 bio", living.BiologyCountText);
        Assert.StartsWith("Exobiology likely about ", living.ExobiologyText, StringComparison.Ordinal);
        Assert.Contains($"{Display.Credits(living.PredictedExobiologyCredits)}, {Display.Credits(living.ExobiologyRank)} with the first-logged bonus by the life these conditions allow; map it to know.", living.ExobiologyText, StringComparison.Ordinal);
        Assert.DoesNotContain("nobody", living.ExobiologyText, StringComparison.Ordinal);
        Assert.Equal(2, living.ExobiologyRuns.Count(run => run.IsFigure));
        // The rail keeps it to one line before the scanner names the genera, and shows no separate value.
        var railRow = shell.Rail.Bodies.Single(body => body.Name == "2");
        Assert.Equal("", railRow.ValueText);
        Assert.Matches(@"^\d+ gen(us|era) could live here, likely about [\d.]+[kM]? Cr x5 = [\d.]+[kM]? Cr with the first-logged bonus\.$", railRow.Life);
        Assert.Equal("2 bio", railRow.BiologyTag);
        // The Landable view lists every landable body, the predicted one among them.
        Assert.All(shell.Rail.Bodies, body => Assert.True(body.Landable));

        // The system page's Landable column puts landable bodies first and then sorts by the same metric, so the predicted body leads there too.
        var page = Open<SystemPage>(shell, PageKind.System);
        page.Sort.SortBy("bio");
        Assert.Equal("2", page.Bodies[0].Name);
    }

    [Fact]
    public void ShipPage_StandsDownForEngineeringTheModuleAlreadyHas()
    {
        var (shell, monitor, _) = Create();
        monitor.Feed(Loadout);
        var ship = Open<ShipPage>(shell, PageKind.Ship, "3");

        // The power plant is Boosted G3 with an experimental; the picker opens on exactly that, which is no job.
        var plant = ship.Modules.Single(row => row.Slot == "PowerPlant");
        Assert.Equal("PowerPlant_Boosted", plant.SelectedBlueprint!.Key);
        Assert.Equal(3, plant.SelectedGrade);
        Assert.True(plant.IsAlreadyDone);
        Assert.Equal("Already engineered", plant.AddText);
        Assert.False(plant.AddCommand.CanExecute(null));

        // A higher grade is a job; so is another experimental.
        plant.SelectedGrade = 5;
        Assert.False(plant.IsAlreadyDone);
        Assert.Equal("Add to plan", plant.AddText);
        Assert.True(plant.AddCommand.CanExecute(null));
        plant.SelectedGrade = 3;
        plant.SelectedExperimental = plant.Experimentals.First(choice => choice.Key.Length > 0 && choice.Key != "special_powerplant_highcharge");
        Assert.False(plant.IsAlreadyDone);

        // A module at the blueprint's top grade with an experimental: the picker opens on it, which is no job.
        var finished = new FittedModuleRow(
            Catalog,
            new FitModule("PowerPlant", "int_powerplant_size7_class5", new Engineering("PowerPlant_Boosted", 5, "special_powerplant_highcharge")),
            "Power Plant",
            Catalog.ModuleTypes["pp"],
            null,
            "Boosted G5",
            "Ironic",
            _ => { },
            shell.Workspace.Scope,
            false,
            null);
        Assert.Equal("Already engineered", finished.AddText);

        // Unfinished grade: the same slot at G5 but a quarter of the way through is still a job.
        var partway = new FittedModuleRow(
            Catalog,
            new FitModule("PowerPlant", "int_powerplant_size7_class5", new Engineering("PowerPlant_Boosted", 5, "special_powerplant_highcharge", Quality: 0.25)),
            "Power Plant",
            Catalog.ModuleTypes["pp"],
            null,
            "Boosted G5, 25%",
            "Ironic",
            _ => { },
            shell.Workspace.Scope,
            false,
            null);
        Assert.False(partway.IsAlreadyDone);
    }

    [Fact]
    public void Home_ShowsTheFiguresThatMatterToday_WithATonePerFigure()
    {
        var (shell, monitor, _) = Create();
        var empty = Open<HomePage>(shell, PageKind.Home);
        Assert.Equal(["MATERIALS SHORT", "EARNED, 7 DAYS", "JUMPS LEFT", "BODIES SCANNED", "DATA ABOARD"], empty.Stats.Select(stat => stat.Label));
        Assert.All(empty.Stats, stat => Assert.Equal(StatTone.Muted, stat.Tone));
        Assert.Equal("—", empty.Stats[0].Value);

        shell.Workspace.Plan.Add([PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 1, null, PlanSource.Manual)]);
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Materials","Raw":[],"Manufactured":[],"Encoded":[{"Name":"disruptedwakeechoes","Count":0}]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-01T10:00:00Z","event":"RedeemVoucher","Type":"bounty","Amount":150000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"SellExplorationData","Systems":["A"],"Discovered":["A"],"BaseValue":100000,"Bonus":20000,"TotalEarnings":120000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625],"JumpDist":49.028,"FuelUsed":9.6,"FuelLevel":20.0}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:30Z","event":"FSSDiscoveryScan","Progress":0.0,"BodyCount":8,"NonBodyCount":0,"SystemName":"Koine","SystemAddress":9}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:03:00Z","event":"Scan","ScanType":"Detailed","BodyName":"Koine","BodyID":0,"DistanceFromArrivalLS":0,"StarType":"M","WasDiscovered":true,"WasMapped":false}""");

        var home = Open<HomePage>(shell, PageKind.Home);
        var materials = home.Stats.Single(stat => stat.Label == "MATERIALS SHORT");
        Assert.Equal("1", materials.Value);
        Assert.Equal(StatTone.Bad, materials.Tone);
        Assert.Equal("Materials", materials.Link.Label);

        // Both payments fall within the week before the last event read, so both count.
        var earned = home.Stats.Single(stat => stat.Label == "EARNED, 7 DAYS");
        Assert.Equal("270k Cr", earned.Value);
        Assert.Equal(StatTone.Accent, earned.Tone);
        Assert.Contains("2 payments", earned.Detail, StringComparison.Ordinal);

        var scanned = home.Stats.Single(stat => stat.Label == "BODIES SCANNED");
        Assert.Equal("1/8", scanned.Value);
        Assert.Equal(StatTone.Accent, scanned.Tone);
        Assert.Equal(1 / 8d, shell.Rail.ScanFraction, 1e-9);

        // The star scanned after the sale is aboard and unsold, valued by the cartographics estimate; the stats page shows the same.
        var aboard = home.Stats.Single(stat => stat.Label == "DATA ABOARD");
        Assert.Equal(StatTone.Accent, aboard.Tone);
        Assert.NotEqual("—", aboard.Value);
        Assert.StartsWith("1 body in 1 system unsold", aboard.Detail, StringComparison.Ordinal);
        var stats = Open<StatisticsPage>(shell, PageKind.Statistics);
        Assert.Equal("Aboard, unsold", stats.ExplorationGroups[0].Title);
        Assert.Equal("1", stats.ExplorationGroups[0].Rows.Single(row => row.Label == "Bodies scanned").Value);
        Assert.Equal("Aboard, unsold", stats.ExobiologyGroups[0].Title);
        Assert.Equal("0", stats.ExobiologyGroups[0].Rows.Single(row => row.Label == "Samples").Value);
    }

    [Fact]
    public void TableSort_RemembersEachColumnsDirection_AcrossColumnsAndRestarts()
    {
        var preferences = new UiPreferences(null);
        var columns = new List<SortColumn<int>>
        {
            new("value", "Value", row => row, DescendingFirst: true),
            new("name", "Name", row => row),
        };
        var sort = new TableSort<int>(columns, "name", preferences, "sort.test");
        Assert.Equal("name", sort.ActiveKey);
        Assert.False(sort.Descending);

        // Flip name to descending, go to value, come back: name is still descending.
        sort.SortBy("name");
        Assert.True(sort.Descending);
        sort.SortBy("value");
        Assert.True(sort.Descending);
        sort.SortBy("name");
        Assert.Equal("name", sort.ActiveKey);
        Assert.True(sort.Descending);
        Assert.Equal([3, 2, 1], sort.Apply([1, 3, 2]));

        // Value, flipped to ascending, stays ascending after a round trip through name, and across a restart.
        sort.SortBy("value");
        sort.SortBy("value");
        Assert.False(sort.Descending);
        sort.SortBy("name");
        var restored = new TableSort<int>(columns, "name", preferences, "sort.test");
        Assert.Equal("name", restored.ActiveKey);
        Assert.True(restored.Descending);
        restored.SortBy("value");
        Assert.False(restored.Descending);

        // A saved value in any other form, an older one say, is ignored and the defaults stand.
        preferences.Set("sort.old", "value:asc");
        var older = new TableSort<int>(columns, "name", preferences, "sort.old");
        Assert.Equal("name", older.ActiveKey);
        Assert.False(older.Descending);
    }

    [Fact]
    public void Preferences_DropKeysKateyNoLongerWrites_OnLoadAndOnReplace()
    {
        var saved = new Dictionary<string, string>
        {
            ["rail.bodyView"] = "minerals",
            ["sort.anything"] = "name:asc;name=asc",
            ["section.route"] = "plot",
            ["rail.bodySort"] = "biology",
            ["plot.margin"] = "2",
        };
        var preferences = new UiPreferences(saved);
        Assert.Equal(["rail.bodyView", "section.route", "sort.anything"], preferences.Snapshot().Keys.Order(StringComparer.Ordinal));
        preferences.Replace(saved);
        Assert.Equal(["rail.bodyView", "section.route", "sort.anything"], preferences.Snapshot().Keys.Order(StringComparer.Ordinal));
        Assert.True(PreferenceKeys.IsCurrent("sort.ships"));
        Assert.False(PreferenceKeys.IsCurrent("sort."));
        Assert.False(PreferenceKeys.IsCurrent("rail.bodySort"));
    }

    [Fact]
    public void SystemValue_ShowsBaseAndBonusFigures_AndWhatIsGatheredOnceASpeciesIsComplete()
    {
        var (shell, monitor, _) = Create();
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T10:00:00Z","event":"FSDJump","StarSystem":"Fresh","SystemAddress":77,"StarPos":[0,0,0],"Population":0}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T10:00:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Fresh","BodyID":0,"StarType":"K","StellarMass":0.8,"DistanceFromArrivalLS":0,"WasDiscovered":false,"WasMapped":false,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T10:00:02Z","event":"Scan","ScanType":"Detailed","BodyName":"Fresh 1","BodyID":1,"Parents":[{"Star":0}],"PlanetClass":"High metal content body","DistanceFromArrivalLS":500,"Landable":true,"Atmosphere":"thin sulphur dioxide atmosphere","SurfaceGravity":2.9,"SurfaceTemperature":300,"MassEM":0.3,"WasDiscovered":false,"WasMapped":false,"WasFootfalled":false,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T10:00:03Z","event":"SAASignalsFound","BodyName":"Fresh 1","BodyID":1,"SystemAddress":77,"Signals":[{"Type":"$SAA_SignalType_Biological;","Count":1}],"Genuses":[{"Genus":"$Codex_Ent_Stratum_Genus_Name;","Genus_Localised":"Stratum"}]}""");

        // Probed but not sampled: the exobiology is an estimate, so "about", and the bonus figure follows it.
        var page = Open<SystemPage>(shell, PageKind.System);
        Assert.StartsWith("Cartographics about ", page.ValueSummary, StringComparison.Ordinal);
        Assert.Contains(" with the first-discovery bonus; exobiology about ", page.ValueSummary, StringComparison.Ordinal);
        Assert.Contains(" with the first-logged bonus.", page.ValueSummary, StringComparison.Ordinal);
        // Unsold is what is aboard: the scans with their discovery bonus and no species yet; nothing is sold.
        var cartographics = page.Bodies.Sum(row => row.ScanCredits);
        Assert.True(cartographics > 0);
        Assert.Equal($"Exploration data unsold: {Display.Credits(cartographics)}", shell.Rail.ValueText);
        Assert.StartsWith($"Exploration data unsold: {Display.Credits(cartographics)}, ", page.UnsoldSummary, StringComparison.Ordinal);
        Assert.Contains(" of it for being first.", page.UnsoldSummary, StringComparison.Ordinal);
        Assert.DoesNotContain("; sold:", page.UnsoldSummary, StringComparison.Ordinal);
        var body = page.Bodies.Single(row => row.Name == "1");
        Assert.StartsWith("Exobiology about ", body.ExobiologyText, StringComparison.Ordinal);
        Assert.False(body.IsExobiologyKnown);
        Assert.False(body.HasGathered);

        // One species fully sampled: the exobiology is exact, and the gathered line shows it in hand, times five.
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T10:10:00Z","event":"Touchdown","PlayerControlled":true,"StarSystem":"Fresh","SystemAddress":77,"Body":"Fresh 1","BodyID":1,"OnPlanet":true}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T10:11:00Z","event":"ScanOrganic","ScanType":"Log","Genus":"$Codex_Ent_Stratum_Genus_Name;","Genus_Localised":"Stratum","Species":"$Codex_Ent_Stratum_07_Name;","Species_Localised":"Stratum Tectonicas","Body":1,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T10:12:00Z","event":"ScanOrganic","ScanType":"Sample","Genus":"$Codex_Ent_Stratum_Genus_Name;","Genus_Localised":"Stratum","Species":"$Codex_Ent_Stratum_07_Name;","Species_Localised":"Stratum Tectonicas","Body":1,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T10:13:00Z","event":"ScanOrganic","ScanType":"Analyse","Genus":"$Codex_Ent_Stratum_Genus_Name;","Genus_Localised":"Stratum","Species":"$Codex_Ent_Stratum_07_Name;","Species_Localised":"Stratum Tectonicas","Body":1,"SystemAddress":77}""");
        page = Open<SystemPage>(shell, PageKind.System);
        var reward = Predictor.Reward("$Codex_Ent_Stratum_07_Name;")!.Value;
        Assert.Contains($"; exobiology {Display.Credits(reward)}, {Display.Credits(reward * 5)} with the first-logged bonus.", page.ValueSummary, StringComparison.Ordinal);
        Assert.DoesNotContain("exobiology about", page.ValueSummary, StringComparison.Ordinal);
        // The species sampled in full joins the unsold figure, with the bonus the game said nobody had logged it.
        Assert.Equal($"Exploration data unsold: {Display.Credits(cartographics + (reward * 5))}", shell.Rail.ValueText);
        Assert.StartsWith($"Exploration data unsold: {Display.Credits(cartographics + (reward * 5))}, ", page.UnsoldSummary, StringComparison.Ordinal);
        // The body's own sentence drops "about" too, and its gathered line follows it, the figures marked as earned.
        body = page.Bodies.Single(row => row.Name == "1");
        Assert.True(body.IsExobiologyKnown);
        Assert.Equal($"Exobiology {Display.Credits(reward)}, {Display.Credits(reward * 5)} with the first-logged bonus.", body.ExobiologyText);
        Assert.True(body.HasGathered);
        Assert.Equal($"You've gathered {Display.Credits(reward)}, {Display.Credits(reward * 5)} with the first-logged bonus.", string.Concat(body.GatheredRuns.Select(run => run.Text)));
        Assert.Equal([false, true, false, true, false, false], body.GatheredRuns.Select(run => run.IsEarned));
        var genus = Assert.Single(body.Genera);
        Assert.True(genus.IsComplete);
        // The genus row shows the figure with the bonus figure in brackets, each in a fixed width so a column of them lines up.
        Assert.True(genus.HasFirstLogBonus);
        Assert.Equal($"{Display.Millions(reward),6} {$"({Display.Millions(reward * 5)})",8} Cr", genus.ExpectedText);
        Assert.Equal(Display.CreditsMillions(reward), (genus with { HasFirstLogBonus = false }).ExpectedText);
        Assert.Equal("0.8M Cr", Display.CreditsMillions(812_345));
        Assert.Equal("  2.1M  (10.5M) Cr", (genus with { ExpectedCredits = 2_100_000 }).ExpectedText);

        // Selling both kinds of data: the sold figure is the sale's own earnings plus what the species paid.
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T12:00:00Z","event":"MultiSellExplorationData","Discovered":[{"SystemName":"Fresh","NumBodies":2}],"BaseValue":200000,"Bonus":100000,"TotalEarnings":300000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-09T12:00:01Z","event":"SellOrganicData","MarketID":1,"BioData":[{"Genus":"$Codex_Ent_Stratum_Genus_Name;","Species":"$Codex_Ent_Stratum_07_Name;","Value":19010800,"Bonus":76043200}]}""");
        page = Open<SystemPage>(shell, PageKind.System);
        var soldCredits = 300000 + 19010800 + 76043200;
        Assert.Equal($"Exploration data unsold: 0 Cr; sold: {Display.Credits(soldCredits)}.", page.UnsoldSummary);
        Assert.Equal("Exploration data unsold: 0 Cr", shell.Rail.ValueText);
        Assert.True(Assert.Single(page.Bodies.Single(row => row.Name == "1").Genera).IsSold);

        // Coming back after the sale, the game's flags report your own record; Katey keeps the firsts yours.
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-10T10:00:00Z","event":"FSDJump","StarSystem":"Elsewhere","SystemAddress":78,"StarPos":[5,0,0],"Population":0}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-10T11:00:00Z","event":"FSDJump","StarSystem":"Fresh","SystemAddress":77,"StarPos":[0,0,0],"Population":0}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-10T11:00:02Z","event":"Scan","ScanType":"Detailed","BodyName":"Fresh 1","BodyID":1,"Parents":[{"Star":0}],"PlanetClass":"High metal content body","DistanceFromArrivalLS":500,"Landable":true,"Atmosphere":"thin sulphur dioxide atmosphere","SurfaceGravity":2.9,"SurfaceTemperature":300,"MassEM":0.3,"WasDiscovered":true,"WasMapped":true,"WasFootfalled":true,"SystemAddress":77}""");
        page = Open<SystemPage>(shell, PageKind.System);
        body = page.Bodies.Single(row => row.Name == "1");
        Assert.Equal("discovered by you", body.DiscoveryStatus);
        Assert.Equal("footfall by you", body.FootfallStatus);
        // This body's exobiology has been sold, so nothing is assumed for a species not yet sampled; the sampled one keeps the game's word.
        Assert.True(body.ExobiologySold);
        Assert.False(body.FirstLogLikely);
        Assert.True(Assert.Single(body.Genera).HasFirstLogBonus);
        Assert.Equal($"Exploration data unsold: 0 Cr; sold: {Display.Credits(soldCredits)}.", page.UnsoldSummary);
    }

    [Fact]
    public void SystemBodies_ComeFromSpansh_OnlyWhenAllowed_AndNeverOverrideYourOwnScans()
    {
        var systems = new FakeSystemCatalog();
        systems.Bodies[9] =
        [
            new(0, "Koine", true, null, "M", 0, false, "", "", null, 2800, null, 0.4, false, false, new Dictionary<string, double>(), 0, 0, [], null),
            new(1, "Koine 1", false, 0, "High metal content body", 50, true, "", "", 0.3, 300, null, 0.4, false, false, new Dictionary<string, double> { ["Iron"] = 20 }, 2, 0, ["$Codex_Ent_Bacterial_Genus_Name;"], null),
            new(2, "Koine 2", false, 0, "Icy body", 900, true, "Thin Argon", "", 0.2, 80, 1000, 0.1, false, false, new Dictionary<string, double>(), 0, 0, [], null),
        ];
        var (shell, monitor, store) = Create(systems: systems);
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T18:20:00Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T18:20:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Koine","BodyID":0,"StarType":"M","DistanceFromArrivalLS":0,"SystemAddress":9}""");

        // Both switches off, the default: nothing is asked and the system shows only what you scanned.
        var settings = Open<SettingsPage>(shell, PageKind.Settings);
        Assert.False(settings.AllowSpansh);
        Assert.False(settings.AllowSpanshBodies);
        Assert.Empty(systems.Requests);
        Assert.Equal(["Koine"], shell.Workspace.System.Bodies.Select(body => body.Name));

        // The bodies switch on: the system is asked for at once, and its bodies join the list marked as the catalogue's.
        settings.AllowSpanshBodies = true;
        Assert.False(settings.AllowSpansh);
        Assert.Equal([9L], systems.Requests);
        Assert.True(store.State.AllowSpanshBodies);
        Assert.False(store.State.AllowSpansh);
        var bodies = shell.Workspace.System.Bodies;
        Assert.Equal(["Koine", "Koine 1", "Koine 2"], bodies.Select(body => body.Name));
        Assert.False(bodies[0].FromCatalogue);
        Assert.True(bodies[1].FromCatalogue);
        Assert.True(bodies[1].WasMapped);
        Assert.Equal(2, bodies[1].BiologicalSignals);
        var page = Open<SystemPage>(shell, PageKind.System);
        Assert.Contains(page.Bodies, row => row.FromCatalogue && row.Name == "1");

        // The route switch is its own; turning the bodies switch off hides the catalogue's bodies again without asking twice.
        settings.AllowSpansh = true;
        Assert.True(store.State.AllowSpansh);
        Assert.True(shell.Workspace.Plot.Enabled);
        settings.AllowSpansh = false;
        Assert.False(shell.Workspace.Plot.Enabled);
        settings.AllowSpanshBodies = false;
        Assert.False(store.State.AllowSpanshBodies);
        Assert.Equal(["Koine"], shell.Workspace.System.Bodies.Select(body => body.Name));
        Assert.Single(systems.Requests);
    }

    [Fact]
    public void UpdateCheck_AsksGitHubOnceAtStartup_OnlyWhenAllowed_AndSaysWhatItFound()
    {
        // Off by default: nothing is asked, and the settings page says so.
        var releases = new FakeReleaseFeed();
        var (shell, _, store) = Create(releases: releases);
        var settings = Open<SettingsPage>(shell, PageKind.Settings);
        Assert.False(settings.AllowUpdateCheck);
        Assert.Equal(0, releases.Requests);
        Assert.Equal("Katey does not look for newer releases until you tick that switch under Outgoing connections.", settings.UpdateStatus);
        Assert.False(settings.IsUpdateAvailable);
        Assert.Null(settings.ReleasePage);
        Assert.False(settings.CheckUpdatesCommand.CanExecute(null));
        Assert.StartsWith("You are running Katey ", SettingsPage.VersionText, StringComparison.Ordinal);

        // The switch on: GitHub is asked at once, the answer is worded against the version running, and the switch is saved.
        settings.AllowUpdateCheck = true;
        Assert.Equal(1, releases.Requests);
        Assert.True(store.State.AllowUpdateCheck);
        Assert.False(store.State.AllowSpansh);
        settings = Open<SettingsPage>(shell, PageKind.Settings);
        Assert.Equal("Katey 1.4.0 is available.", settings.UpdateStatus);
        Assert.True(settings.IsUpdateAvailable);
        Assert.Equal(releases.Latest.Page, settings.ReleasePage);
        Assert.True(settings.CheckUpdatesCommand.CanExecute(null));

        // Check now asks again; toggling the switch does not.
        settings.CheckUpdatesCommand.Execute(null);
        Assert.Equal(2, releases.Requests);
        settings.AllowUpdateCheck = false;
        settings.AllowUpdateCheck = true;
        Assert.Equal(2, releases.Requests);
    }

    [Fact]
    public void UpdateCheck_RunsAtStartupWhenSaved_AndWordsAFailure()
    {
        var releases = new FakeReleaseFeed { Failure = ReleaseLookupFailure.Http, HttpCode = 403 };
        var (shell, _, _) = Create(PersistedState.Empty with { AllowUpdateCheck = true }, releases: releases);
        Assert.Equal(1, releases.Requests);
        var settings = Open<SettingsPage>(shell, PageKind.Settings);
        Assert.Equal("GitHub answered HTTP 403 to the release check.", settings.UpdateStatus);
        Assert.False(settings.IsUpdateAvailable);
        Assert.True(settings.CheckUpdatesCommand.CanExecute(null));

        // A later answer replaces the failure.
        releases.Failure = null;
        settings.CheckUpdatesCommand.Execute(null);
        Assert.Equal(2, releases.Requests);
        Assert.True(shell.Workspace.Updates.IsUpdateAvailable);
    }

    [Fact]
    public void SystemBodies_AskOncePerSystem_AndWaitBeforeRetryingAFailedLookup()
    {
        var systems = new FakeSystemCatalog { Failure = SystemLookupFailure.Unreachable };
        var (shell, monitor, _) = Create(PersistedState.Empty with { AllowSpanshBodies = true }, systems: systems);
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-08T18:20:00Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625]}""");
        Assert.Equal([9L], systems.Requests);
        Assert.Equal("Spansh could not be reached for the system lookup.", shell.Workspace.SystemBodies.Status);

        // Every later journal event in the same system asks nothing more: the failure waits, a success is kept.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-08T18:20:05Z","event":"Scan","ScanType":"AutoScan","BodyName":"Koine","BodyID":0,"StarType":"M","DistanceFromArrivalLS":0,"SystemAddress":9}""");
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-08T18:20:06Z","event":"Music","MusicTrack":"Exploration"}""");
        Assert.Equal([9L], systems.Requests);

        systems.Failure = null;
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-08T18:30:00Z","event":"FSDJump","StarSystem":"Sol","SystemAddress":10,"StarPos":[0,0,0]}""");
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-08T18:30:05Z","event":"Scan","ScanType":"AutoScan","BodyName":"Sol","BodyID":0,"StarType":"G","DistanceFromArrivalLS":0,"SystemAddress":10}""");
        Assert.Equal([9L, 10L], systems.Requests);
        Assert.Equal("", shell.Workspace.SystemBodies.Status);
    }

    [Fact]
    public void SpanshPlot_SaysTheDriveIsSupercharged_WhenItIsOrWillBe()
    {
        var plotter = new FakeRoutePlotter
        {
            Route = new PlottedRoute("Jackson's Lighthouse", "Sol", new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            [
                new("Jackson's Lighthouse", 5532807773, 157, -27, -70, 0, 100, 0, 64, true, false, false),
                new("Sol", 1, 0, 0, 0, 100, 0, 9, 52, false, true, true),
            ]),
        };
        var (shell, monitor, _) = Create(plotter: plotter);
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:47:26Z","event":"FSDJump","StarSystem":"Jackson's Lighthouse","SystemAddress":5532807773,"StarPos":[157,-27,-70]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:47:27Z","event":"Scan","ScanType":"AutoScan","BodyName":"Jackson's Lighthouse","BodyID":0,"StarType":"N","DistanceFromArrivalLS":0,"SystemAddress":5532807773}""",
            Loadout);
        Open<SettingsPage>(shell, PageKind.Settings).AllowSpansh = true;
        var route = Open<RoutePage>(shell, PageKind.Route, RoutePage.PlotSection);
        route.PlotTo = "Sol";

        // At a neutron star with neutron routing on: the first jump will be boosted.
        Assert.True(route.UseSupercharge);
        route.PlotCommand.Execute(null);
        Assert.True(plotter.Requests[^1].IsSupercharged);

        // Neutron routing off: nothing is assumed, unless the boost has already been taken.
        route.UseSupercharge = false;
        route.PlotCommand.Execute(null);
        Assert.False(plotter.Requests[^1].IsSupercharged);
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-08T15:48:00Z","event":"JetConeBoost","BoostValue":6.0}""");
        route = Open<RoutePage>(shell, PageKind.Route, RoutePage.PlotSection);
        route.PlotCommand.Execute(null);
        Assert.True(plotter.Requests[^1].IsSupercharged);
        route.UseSupercharge = true;
    }

    [Fact]
    public void SpanshPlot_IsOffUntilAllowed_ThenPlotsFromTheShip_AndFollowsTheJumps()
    {
        var plotted = new PlottedRoute("Deciat", "Sol", new DateTimeOffset(2026, 11, 5, 20, 5, 0, TimeSpan.Zero),
        [
            new("Deciat", 3, 122.625, -0.8125, -47.28125, 0, 130, 0, 64, false, true, false),
            new("Kamarada", 5, 100, -5, -30, 30, 100, 3, 61, true, false, false),
            new("Sol", 1, 0, 0, 0, 100, 0, 9, 52, false, true, true),
        ]);
        var plotter = new FakeRoutePlotter { Route = plotted };
        var (shell, monitor, store) = Create(plotter: plotter);
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:00Z","event":"Location","StarSystem":"Deciat","SystemAddress":3,"StarPos":[122.625,-0.8125,-47.28125],"Docked":true,"StationName":"Farseer Inc"}""",
            Loadout);

        var route = Open<RoutePage>(shell, PageKind.Route, RoutePage.PlotSection);
        Assert.Equal(RoutePage.PlotSection, route.Sections.Current);
        Assert.False(route.PlotEnabled);
        route.PlotTo = "Sol";
        Assert.False(route.PlotCommand.CanExecute(null));
        Assert.False(shell.Rail.HasPlot);

        var settings = Open<SettingsPage>(shell, PageKind.Settings);
        settings.AllowSpansh = true;
        Assert.True(store.State.AllowSpansh);

        // Two saved fits with masses and one without: the picker starts on the ship being flown and lists the chosen target first, the ship being flown second, then the chosen hull's fits, the ships and the other fits.
        var condaFit = new Fitting("conda-fit", "anacondamkii", "Long legs", [], /*lang=json,strict*/ """{"event":"Loadout","Ship":"anacondamkii","ShipName":"Long legs","UnladenMass":1000,"FuelCapacity":{"Main":64,"Reserve":1.08},"Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size6_class5","Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"Modifiers":[{"Label":"FSDOptimalMass","Value":3000},{"Label":"MaxFuelPerJump","Value":10}]}},{"Slot":"Slot05_Size5","Item":"int_guardianfsdbooster_size5"}]}""");
        var mandalayFit = new Fitting("mandalay-fit", "mandalay", "Dream fit", [], /*lang=json,strict*/ """{"event":"Loadout","Ship":"mandalay","ShipName":"Dream fit","UnladenMass":400.5,"FuelCapacity":{"Main":32,"Reserve":0.5},"Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size5_class5","Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"Modifiers":[{"Label":"FSDOptimalMass","Value":1692.6},{"Label":"MaxFuelPerJump","Value":5.2}]}}]}""");
        shell.Workspace.Fleet.Add(mandalayFit);
        shell.Workspace.Fleet.Add(condaFit);
        shell.Workspace.Fleet.Add(new Fitting("no-masses", "mandalay", "Matching only", []));

        route = Open<RoutePage>(shell, PageKind.Route, RoutePage.PlotSection);
        Assert.True(route.PlotEnabled);
        Assert.True(route.HasPlotTargets);
        Assert.Equal(PlotTarget.ShipKind, route.SelectedPlotTarget!.Kind);
        Assert.True(route.SelectedPlotTarget.IsFlown);
        Assert.Contains("Ironic", route.SelectedPlotTarget.Label, StringComparison.Ordinal);
        Assert.All(route.PlotTargets.Where(target => target.Key != "ship:3"), target => Assert.False(target.IsFlown));
        Assert.Equal(["ship:3", "fit:conda-fit", "fit:mandalay-fit"], route.PlotTargets.Select(target => target.Key));

        // Choosing the Mandalay fit puts it first and the ship being flown second; choosing the ship puts its hull's fits right after it again.
        route.SelectedPlotTarget = route.PlotTargets[2];
        Assert.Equal(["fit:mandalay-fit", "ship:3", "fit:conda-fit"], route.PlotTargets.Select(target => target.Key));
        Assert.Contains("Full-tank range", route.PlotRangeText, StringComparison.Ordinal);
        Assert.Equal("Dream fit · Mandalay · ", route.SelectedPlotTarget.Label[.."Dream fit · Mandalay · ".Length]);
        route.SelectedPlotTarget = route.PlotTargets.Single(target => target.Key == "ship:3");
        Assert.Equal(["ship:3", "fit:conda-fit", "fit:mandalay-fit"], route.PlotTargets.Select(target => target.Key));

        // The search narrows the list by the line shown, keeping the chosen target; clearing it brings everything back.
        route.PlotFilter = "dream";
        Assert.Equal(["ship:3", "fit:mandalay-fit"], route.PlotTargets.Select(target => target.Key));
        route.PlotFilter = "ANACONDA";
        Assert.Equal(["ship:3", "fit:conda-fit"], route.PlotTargets.Select(target => target.Key));
        route.PlotFilter = "";
        Assert.Equal(3, route.PlotTargets.Count);
        Assert.Equal("Deciat", route.PlotFrom);
        Assert.StartsWith("Full-tank range", route.PlotRangeText, StringComparison.Ordinal);
        route.PlotTo = "Sol";
        route.PlotCargo = "10";
        Assert.True(route.PlotCommand.CanExecute(null));
        route.PlotCommand.Execute(null);

        var request = Assert.Single(plotter.Requests);
        Assert.Equal("Deciat", request.Source);
        Assert.Equal("Sol", request.Destination);
        Assert.Equal(10, request.Cargo);
        Assert.Equal(2.60, request.Ship.FuelPower);
        Assert.Equal(9.6, request.Ship.MaxFuelPerJump);
        Assert.Equal(2000, request.Ship.OptimalMass);
        Assert.Equal(1250.5 + 1.08, request.Ship.BaseMass, 1e-9);
        Assert.Equal(64, request.Ship.TankSize);
        Assert.Equal(4, request.Ship.SuperchargeMultiplier);
        Assert.Equal("Sol", store.State.Preferences!["plot.to"]);

        // The routing and the options are saved as they change, not only when a plot is asked for, and are back on the next visit.
        route.ExcludeSecondary = true;
        Assert.Equal("true", store.State.Preferences!["plot.secondary"]);
        route.SelectedAlgorithm = route.Algorithms[^1];
        Assert.Equal(route.Algorithms[^1].Key, store.State.Preferences!["plot.algorithm"]);

        // The chosen target and the cargo stay as set across pages while the plot is held; switching the target leaves the cargo alone.
        route.SelectedPlotTarget = route.PlotTargets.Single(target => target.Key == "fit:mandalay-fit");
        Assert.Equal("10", route.PlotCargo);
        route.PlotCargo = "12";
        route = Open<RoutePage>(shell, PageKind.Route, RoutePage.PlotSection);
        Assert.True(route.ExcludeSecondary);
        Assert.Equal(route.Algorithms[^1].Key, route.SelectedAlgorithm.Key);
        Assert.Equal("fit:mandalay-fit", route.SelectedPlotTarget!.Key);
        Assert.Equal("12", route.PlotCargo);
        route.SelectedPlotTarget = route.PlotTargets.Single(target => target.Key == "ship:3");
        Assert.Equal("12", route.PlotCargo);
        route.SelectedPlotTarget = route.PlotTargets.Single(target => target.Key == "fit:mandalay-fit");

        // Forgetting the plot forgets both: the page opens on the ship being flown with the cargo it carries.
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x10, null, null, 15.0, 7);
        route.ClearPlotCommand.Execute(null);
        Assert.False(route.HasPlot);
        Assert.Equal("ship:3", route.SelectedPlotTarget!.Key);
        Assert.Equal("7", route.PlotCargo);
        route = Open<RoutePage>(shell, PageKind.Route, RoutePage.PlotSection);
        Assert.Equal("ship:3", route.SelectedPlotTarget!.Key);
        Assert.Equal("7", route.PlotCargo);
        route.PlotTo = "Sol";
        route.PlotCommand.Execute(null);
        Assert.Equal(2, plotter.Requests.Count);
        Assert.Equal(7, plotter.Requests[1].Cargo);

        // Plotting for a fit sends the fit's own numbers.
        route.SelectedPlotTarget = route.PlotTargets.Single(target => target.Key == "fit:mandalay-fit");
        route.PlotCommand.Execute(null);
        Assert.Equal(3, plotter.Requests.Count);
        Assert.Equal(1692.6, plotter.Requests[2].Ship.OptimalMass);
        Assert.Equal(5.2, plotter.Requests[2].Ship.MaxFuelPerJump);
        Assert.Equal(401.0, plotter.Requests[2].Ship.BaseMass, 1e-9);
        Assert.Equal(32, plotter.Requests[2].Ship.TankSize);
        route.SelectedPlotTarget = route.PlotTargets.Single(target => target.Key == "ship:3");

        Assert.True(route.HasPlot);
        Assert.Contains("Plotted 2 jumps from Deciat to Sol.", route.PlotStatus, StringComparison.Ordinal);
        Assert.Equal(["here", "next", ""], route.PlotStops.Select(stop => stop.StageText));
        Assert.Equal("Kamarada", route.NextWaypoint);
        Assert.Equal("Next waypoint Kamarada, 2 jumps to Sol.", Phrase.Plain(route.PlotProgressParts));
        Assert.Equal(["Kamarada", "Sol"], route.PlotProgressParts.Where(part => part.IsLink).Select(part => part.Text));
        Assert.Equal("neutron", route.PlotStops[1].NotesText);
        Assert.Equal("refuel here", route.PlotStops[2].NotesText);
        Assert.True(shell.Rail.HasPlot);
        Assert.Equal("Kamarada", shell.Rail.NextWaypoint);
        Assert.Equal(3, store.State.Plot!.Stops.Count);

        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:10:00Z","event":"FSDJump","StarSystem":"Kamarada","SystemAddress":5,"StarPos":[100,-5,-30],"JumpDist":30,"FuelUsed":3,"FuelLevel":61}""");
        route = Assert.IsType<RoutePage>(shell.Navigator.Current);
        Assert.Equal("Sol", route.NextWaypoint);
        Assert.Equal(["done", "here", "next"], route.PlotStops.Select(stop => stop.StageText));

        // A detour off the plot keeps the waypoint after the last one reached, and marks that one.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:15:00Z","event":"FSDJump","StarSystem":"Scoop Stop","SystemAddress":77,"StarPos":[90,-5,-30],"JumpDist":10,"FuelUsed":1,"FuelLevel":60}""");
        route = Assert.IsType<RoutePage>(shell.Navigator.Current);
        Assert.Equal("Sol", route.NextWaypoint);
        Assert.Equal("Off the plot. Last known waypoint Kamarada, next Sol. ~1 jump to Sol.", Phrase.Plain(route.PlotProgressParts));
        Assert.Equal(["done", "last known", "next"], route.PlotStops.Select(stop => stop.StageText));
        Assert.Equal("Sol", shell.Rail.NextWaypoint);

        // A plot made later, from somewhere the ship is not: nothing is done or last known, however many of its
        // waypoints the ship saw before the plot existed.
        shell.Workspace.Plot.Restore(new PlottedRoute("Deciat", "Sol", new DateTimeOffset(2026, 11, 5, 20, 20, 0, TimeSpan.Zero), plotted.Stops));
        route = Open<RoutePage>(shell, PageKind.Route);
        Assert.Equal("Off the plot. First waypoint Deciat, 2 jumps to Sol.", Phrase.Plain(route.PlotProgressParts));
        Assert.Equal(["", "", ""], route.PlotStops.Select(stop => stop.StageText));
        Assert.Equal("Deciat", route.NextWaypoint);

        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:20:00Z","event":"FSDJump","StarSystem":"Sol","SystemAddress":1,"StarPos":[0,0,0],"JumpDist":100,"FuelUsed":9,"FuelLevel":52}""");
        route = Assert.IsType<RoutePage>(shell.Navigator.Current);
        Assert.Equal("Arrived at Sol.", Phrase.Plain(route.PlotProgressParts));
        Assert.False(route.HasNextWaypoint);
        Assert.False(shell.Rail.HasNextWaypoint);

        var (again, _, _) = Create(store.State, plotter);
        var restored = Open<RoutePage>(again, PageKind.Route, RoutePage.PlotSection);
        Assert.True(restored.HasPlot);
        Assert.Equal(3, restored.PlotStops.Count);

        plotter.Failure = "Unable to find route";
        route.PlotTo = "Nowhere";
        route.PlotCommand.Execute(null);
        Assert.Equal("Unable to find route", route.PlotStatus);
        Assert.True(route.HasPlot);

        route.ClearPlotCommand.Execute(null);
        Assert.False(route.HasPlot);
        Assert.Null(store.State.Plot);
        Assert.False(shell.Rail.HasPlot);

        settings = Open<SettingsPage>(shell, PageKind.Settings);
        settings.AllowSpansh = false;
        Assert.False(Open<RoutePage>(shell, PageKind.Route).PlotEnabled);
    }

    [Fact]
    public void JournalBackup_RunsFromSettings_AndOnClose_WhenAFolderIsSet()
    {
        var backup = new FakeJournalBackup();
        var (shell, _, store) = Create(PersistedState.Empty with { JournalFolder = @"C:\journals" }, backup: backup);

        var settings = Open<SettingsPage>(shell, PageKind.Settings);
        Assert.True(settings.BackupOnClose);
        Assert.False(settings.BackupNowCommand.CanExecute(null));
        Assert.Null(shell.Shutdown());
        Assert.Empty(backup.Calls);

        settings.BackupFolder = @"D:\OneDrive\Elite";
        Assert.Equal(@"D:\OneDrive\Elite", store.State.BackupFolder);
        Assert.True(settings.BackupNowCommand.CanExecute(null));
        settings.BackupNowCommand.Execute(null);
        SpinWait.SpinUntil(() => !shell.Workspace.Backup.IsBusy, TimeSpan.FromSeconds(10));
        var call = Assert.Single(backup.Calls);
        Assert.Equal((@"C:\journals", @"D:\OneDrive\Elite"), call);
        settings = Assert.IsType<SettingsPage>(shell.Navigator.Current);
        Assert.Contains("3 files copied, 2 already current.", settings.BackupStatus, StringComparison.Ordinal);

        var onClose = shell.Shutdown();
        Assert.NotNull(onClose);
        Assert.Equal(2, backup.Calls.Count);

        settings.BackupOnClose = false;
        Assert.False(store.State.BackupOnClose);
        Assert.Null(shell.Shutdown());
        Assert.Equal(2, backup.Calls.Count);

        backup.Result = new BackupResult(0, 5, 0, ["Journal.log: in use"]);
        settings.BackupNowCommand.Execute(null);
        SpinWait.SpinUntil(() => Assert.IsType<SettingsPage>(shell.Navigator.Current).BackupStatus.Contains("failed", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        settings = Assert.IsType<SettingsPage>(shell.Navigator.Current);
        Assert.Contains("1 file failed: Journal.log: in use", settings.BackupStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void Statistics_ChartsIncomeWithToggles_AndShowsExobiologyPowerplayAndLifetimeFigures()
    {
        var (shell, monitor, store) = Create();
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Statistics","Bank_Account":{"Current_Wealth":123456789,"Spent_On_Ships":50000000,"Spent_On_Outfitting":0},"Exploration":{"Systems_Visited":2345,"Time_Played":360000,"Total_Hyperspace_Distance":98765.4},"Crafting":{"Count_Of_Used_Engineers":0}}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"RedeemVoucher","Type":"bounty","Amount":150000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:03:00Z","event":"SellOrganicData","MarketID":1,"BioData":[{"Genus":"$Codex_Ent_Stratum_Genus_Name;","Species":"$Codex_Ent_Stratum_07_Name;","Species_Localised":"Stratum Tectonicas","Value":19010800,"Bonus":76043200},{"Genus":"$Codex_Ent_Bacterial_Genus_Name;","Species":"$Codex_Ent_Bacterial_01_Name;","Species_Localised":"Bacterium Aurasus","Value":1000000,"Bonus":0}]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:05:00Z","event":"MarketSell","MarketID":1,"Type":"painite","Type_Localised":"Painite","Count":5,"SellPrice":200000,"TotalSale":1000000,"AvgPricePaid":0}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:06:00Z","event":"Powerplay","Power":"Zemina Torval","Rank":3,"Merits":1200,"Votes":0,"TimePledged":864000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:07:00Z","event":"PowerplayMerits","Power":"Zemina Torval","MeritsGained":150,"TotalMerits":1350}""");

        var stats = Open<StatisticsPage>(shell, PageKind.Statistics);
        Assert.DoesNotContain("Stats", shell.Nav.Select(item => item.Label));
        Assert.True(shell.StatsNav.IsCurrent);
        Assert.Equal(3, stats.Slices.Count);
        Assert.True(stats.HasSlices);
        var exobiology = stats.Activities.Single(activity => activity.Kind == IncomeKind.Exobiology);
        Assert.Equal("96,054,000 Cr", exobiology.AmountText);
        Assert.Equal("98.8%", exobiology.ShareText);
        Assert.Equal("2 payments", exobiology.CountText);
        Assert.Contains("97,204,000 Cr from the activities switched on.", stats.IncomeTotalText, StringComparison.Ordinal);
        Assert.Equal("Exobiology", stats.IncomeGroups[0].Rows.Single(row => row.Label == "Best activity").Value);

        exobiology.IsShown = false;
        Assert.Equal(2, stats.Slices.Count);
        Assert.Equal("", exobiology.ShareText);
        Assert.Contains("Exobiology", store.State.Preferences!["stats.hidden"], StringComparison.Ordinal);
        Assert.Equal("1,150,000 Cr from the activities switched on.", stats.IncomeTotalText);

        var sold = stats.ExobiologyGroups[1].Rows.ToDictionary(row => row.Label, row => row.Value);
        Assert.Equal("96,054,000 Cr", sold["With the first-logged bonus"]);
        Assert.Equal("20,010,800 Cr", sold["Without the bonus"]);
        Assert.Equal("76,043,200 Cr", sold["The bonus alone"]);
        Assert.Equal("2", sold["Species sold"]);
        Assert.Equal("Stratum Tectonicas", stats.Species[0].Species);
        Assert.Equal("19,010,800 Cr", stats.Species[0].BaseText);

        Assert.Equal(new StatRow("Pledged to", "Zemina Torval"), stats.PowerplayGroups[0].Rows[0]);
        Assert.Equal("10d 0h", stats.PowerplayGroups[0].Rows.Single(row => row.Label == "Time pledged").Value);
        Assert.Equal("150", stats.PowerplayGroups[1].Rows.Single(row => row.Label == "Merits earned").Value);

        var exploration = stats.LifetimeGroups.Single(group => group.Title == "Exploration");
        Assert.Equal("4d 4h", exploration.Rows.Single(row => row.Label == "Time played").Value);
        Assert.Equal("98,765 ly", exploration.Rows.Single(row => row.Label == "Total hyperspace distance").Value);
        Assert.Equal("50,000,000 Cr", stats.LifetimeGroups.Single(group => group.Title == "Bank account").Rows.Single(row => row.Label == "Spent on ships").Value);
        Assert.False(stats.ShowZeroFigures);
        Assert.DoesNotContain(stats.LifetimeGroups.Single(group => group.Title == "Bank account").Rows, row => row.Label == "Spent on outfitting");
        Assert.DoesNotContain(stats.LifetimeGroups, group => group.Title == "Crafting");
        stats.ShowZeroFigures = true;
        Assert.Equal("true", store.State.Preferences!["stats.zeros"]);
        Assert.Equal("0 Cr", stats.LifetimeGroups.Single(group => group.Title == "Bank account").Rows.Single(row => row.Label == "Spent on outfitting").Value);
        Assert.Contains(stats.LifetimeGroups, group => group.Title == "Crafting");
        stats.ShowZeroFigures = false;
        Assert.Equal("Bounty hunting profit", StatisticsPage.LabelOf("Bounty_Hunting_Profit"));
        Assert.Equal("Thargoid encounter wakes", StatisticsPage.LabelOf("TG_ENCOUNTER_WAKES"));

        stats.SelectedPeriod = stats.Periods.Single(choice => choice.Key == "365");
        Assert.Equal("365", store.State.Preferences!["stats.period"]);

        var home = Open<HomePage>(shell, PageKind.Home);
        Assert.Equal("4d 4h played", home.Browse.Single(tile => tile.Link.Label == "Stats").Detail);
    }

    [Fact]
    public void Statistics_ListsWhoIncomeWasSharedWith_AndTheWingMissions()
    {
        var (shell, monitor, _) = Create();
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"RedeemVoucher","Type":"bounty","Amount":150000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"WingJoin","Others":["Alice"]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:30Z","event":"MissionAccepted","Faction":"F","Name":"Mission_MassacreWing","LocalisedName":"Massacre 20 pirates","MissionID":7,"Reward":500000,"Wing":true}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"Bounty","Rewards":[{"Faction":"A","Reward":40000}],"Target":"anaconda","TotalReward":40000,"VictimFaction":"V","SharedWithOthers":1}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:03:00Z","event":"SellExplorationData","Systems":["A"],"Discovered":["A"],"BaseValue":100000,"Bonus":20000,"TotalEarnings":120000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:31:00Z","event":"WingLeave"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:40:00Z","event":"RedeemVoucher","Type":"trade","Amount":25000}""");

        var stats = Open<StatisticsPage>(shell, PageKind.Statistics);
        Assert.Equal("295,000 Cr", stats.IncomeGroups[0].Rows.Single(row => row.Label == "Earned").Value);
        Assert.Equal("150,000 Cr", stats.Activities.Single(activity => activity.Kind == IncomeKind.Bounties).AmountText);
        Assert.Contains("Vouchers redeemed", stats.CombatGroups[0].Rows.Select(row => row.Label));
        Assert.StartsWith("3 payments since", stats.LedgerNote, StringComparison.Ordinal);
        Assert.True(stats.HasWingMissions);
        var mission = Assert.Single(stats.WingMissions);
        Assert.Equal("Massacre 20 pirates", mission.Name);
        Assert.Equal("Alice", mission.WithText);
        Assert.Equal("open", mission.OutcomeText);
        Assert.True(mission.IsActive);

        var together = stats.TogetherGroups.SelectMany(group => group.Rows).ToDictionary(row => row.Label, row => row.Value);
        Assert.Equal("1h 30m", together["In wings"]);
        Assert.Equal("1", together["Wing sessions"]);
        Assert.Equal("65,000 Cr", together["Everything shared"]);
        Assert.Equal("25,000 Cr", together["Trade dividends"]);
        var alice = Assert.Single(stats.Partners);
        Assert.Equal("Alice", alice.Name);
        Assert.Equal("1h 30m", alice.TogetherText);
        Assert.Equal("40,000 Cr", alice.SharedText);
        Assert.True(stats.HasPartners);
    }

    [Fact]
    public void CatchingUp_ShowsOnlyProgress_UntilTheReplayIsDone()
    {
        var (shell, monitor, _) = Create();
        monitor.CatchUp(3, 10);
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:00Z","event":"Location","StarSystem":"Deciat","SystemAddress":3,"StarPos":[122.625,-0.8125,-47.28125],"Docked":true,"StationName":"Farseer Inc"}""");

        Assert.True(shell.IsCatchingUp);
        Assert.Equal("Reading the journal history, 3 of 10 files. Live figures appear when it finishes.", shell.CatchUpText);
        Assert.True(shell.Rail.IsCatchingUp);
        Assert.Equal(shell.CatchUpText, shell.Rail.CatchUpText);
        Assert.Null(shell.Workspace.Route.CurrentSystem);
        Assert.Equal("No position yet", shell.Rail.SystemName);
        Assert.Equal("Deciat", shell.Workspace.Sinks.Route.Snapshot().CurrentSystem);

        monitor.Start("C:\\journals");
        Assert.False(shell.IsCatchingUp);
        Assert.Equal("", shell.CatchUpText);
        Assert.False(shell.Rail.IsCatchingUp);
        Assert.Equal("Deciat", shell.Rail.SystemName);
        Assert.Equal("Deciat", shell.Workspace.Route.CurrentSystem);
    }

    [Fact]
    public void GradePickers_StartAtTheTop_OrAtTheReach_WhenOnlyMyEngineersIsOn()
    {
        var (shell, monitor, _) = Create(PersistedState.Empty with { EngineerIds = [300100] });
        var selected = shell.Workspace.Engineers.Selected;
        var (type, key, reach) = Catalog.EngineerableModuleTypes()
            .SelectMany(type => Catalog.BlueprintsFor(type.Code).Select(blueprint => (Type: type, blueprint.Key, Reach: EngineerCoverage.Reach(type, blueprint.Key, selected))))
            .First(item => item.Reach is { } grade && grade < Catalog.Blueprints[item.Key].MaxGrade);

        var wide = Open<ModuleTypePage>(shell, PageKind.ModuleType, type.Code).Blueprints.Single(row => row.Key == key);
        Assert.Equal(reach, wide.ReachableGrade);
        Assert.Equal(Catalog.Blueprints[key].MaxGrade, wide.SelectedGrade);
        Assert.Equal(Catalog.Blueprints[key].GradeNumbers.Count + 1, wide.Grades.Count);

        var engineers = Open<EngineersPage>(shell, PageKind.Engineers);
        Assert.StartsWith("Off:", engineers.ScopeStatus, StringComparison.Ordinal);
        engineers.OnlyMyEngineers = true;
        Assert.StartsWith("On: narrowed to what your 1 ticked engineer can roll", engineers.ScopeStatus, StringComparison.Ordinal);
        var page = Open<ModuleTypePage>(shell, PageKind.ModuleType, type.Code);
        var partial = page.Blueprints.Single(row => row.Key == key);
        Assert.Equal(reach, partial.SelectedGrade);
        Assert.Equal(reach, partial.Grades[^1]);
        Assert.All(page.Blueprints, row => Assert.NotNull(row.ReachableGrade));
        var fsd = Open<ModuleTypePage>(shell, PageKind.ModuleType, "fsd");
        Assert.Equal(5, fsd.Blueprints.Single(row => row.Key == "FSD_LongRange").SelectedGrade);
        Assert.Contains("engineers page", fsd.ScopeNote, StringComparison.Ordinal);

        var blueprint = Open<BlueprintPage>(shell, PageKind.Blueprint, key);
        Assert.Equal(EngineerCoverage.StartingGrade(Catalog, Catalog.ModuleTypes[blueprint.SelectedModuleType!.Key], key, selected), blueprint.SelectedGrade);

        monitor.Feed(Loadout);
        var ship = Open<ShipPage>(shell, PageKind.Ship, "3");
        var engines = ship.Modules.Single(row => row.Slot == "MainEngines");
        Assert.All(engines.Blueprints, choice => Assert.NotNull(EngineerCoverage.Reach(Catalog.ModuleTypes["t"], choice.Key, selected)));
        Assert.Equal(EngineerCoverage.StartingGrade(Catalog, Catalog.ModuleTypes["t"], engines.SelectedBlueprint!.Key, selected), engines.SelectedGrade);
        Assert.Equal(EngineerCoverage.Reach(Catalog.ModuleTypes["t"], engines.SelectedBlueprint.Key, selected), engines.Grades[^1]);
        engines.SelectedBlueprint = engines.Blueprints[^1];
        Assert.Equal(EngineerCoverage.StartingGrade(Catalog, Catalog.ModuleTypes["t"], engines.SelectedBlueprint.Key, selected), engines.SelectedGrade);
        Assert.All(ship.Modules, row => Assert.True(row.ShowsPicker && !row.ShowsNoChoices));

        var shipTypeCodes = new List<string> { "fsd", "t", "pp" };
        var gunsmith = Catalog.Engineers.Keys.First(name => shipTypeCodes.All(code => !EngineerCoverage.OffersAny(Catalog.ModuleTypes[code], new HashSet<string>(StringComparer.Ordinal) { name })));
        shell.Workspace.Engineers.SetAll(false);
        shell.Workspace.Engineers.Set(gunsmith, true);
        var unserved = Open<ShipPage>(shell, PageKind.Ship, "3");
        Assert.All(unserved.Modules, row => Assert.True(row.CanPlan && row.ShowsNoChoices && !row.ShowsPicker));
        shell.Workspace.Engineers.SetAll(false);
        shell.Workspace.Engineers.Set("Felicity Farseer", true);

        var unreachable = Catalog.Blueprints.Values.First(candidate => Catalog.ModuleTypesWithBlueprint(candidate.Key).All(candidateType => EngineerCoverage.Reach(candidateType, candidate.Key, selected) is null));
        var blocked = Open<BlueprintPage>(shell, PageKind.Blueprint, unreachable.Key);
        Assert.Empty(blocked.ModuleTypeChoices);
        Assert.Null(blocked.SelectedModuleType);
        Assert.False(blocked.AddCommand.CanExecute(null));
        Assert.NotEmpty(blocked.Grades);
        Assert.Contains("cannot be added", blocked.ScopeNote, StringComparison.Ordinal);

        var foreign = Catalog.EngineerableModuleTypes().First(candidate => !EngineerCoverage.OffersAny(candidate, selected) && candidate.Experimentals.Count > 0);
        var nothing = Open<ModuleTypePage>(shell, PageKind.ModuleType, foreign.Code);
        Assert.Empty(nothing.Blueprints);
        Assert.Equal([Choice.None], nothing.Experimentals);

        Open<EngineersPage>(shell, PageKind.Engineers).OnlyMyEngineers = false;
        Assert.Equal(Catalog.BlueprintsFor(foreign.Code).Count, Open<ModuleTypePage>(shell, PageKind.ModuleType, foreign.Code).Blueprints.Count);
        Assert.Equal("", Open<ModuleTypePage>(shell, PageKind.ModuleType, "fsd").ScopeNote);
    }

    [Fact]
    public void BlueprintPage_AddsToThePlan_OnAModuleType_AtAGrade_WithAnExperimental()
    {
        var (shell, _, _) = Create();
        var page = Open<BlueprintPage>(shell, PageKind.Blueprint, "FSD_LongRange");
        Assert.Contains(page.ModuleTypeChoices, choice => choice.Key == "fsd");
        Assert.Equal("fsd", page.SelectedModuleType!.Key);
        Assert.Equal(5, page.SelectedGrade);
        Assert.Contains(page.Experimentals, choice => choice.Key == "special_fsd_heavy");

        page.AddCommand.Execute(null);
        var first = Assert.Single(shell.Workspace.Plan.Entries).Entry;
        Assert.Equal("FSD_LongRange", first.Blueprint);
        Assert.Equal(5, first.TargetGrade);
        Assert.Null(first.Experimental);
        Assert.Contains("Increased range G5 on Frame Shift Drive", page.AddedText, StringComparison.Ordinal);

        page.SelectedExperimental = page.Experimentals.Single(choice => choice.Key == "special_fsd_heavy");
        page.Grades.Single(row => row.Grade == 3).AddCommand.Execute(null);
        Assert.Equal(2, shell.Workspace.Plan.Entries.Count);
        var second = shell.Workspace.Plan.Entries[1].Entry;
        Assert.Equal(3, second.TargetGrade);
        Assert.Equal("special_fsd_heavy", second.Experimental);

        page.SelectedGrade = 0;
        page.AddCommand.Execute(null);
        Assert.True(shell.Workspace.Plan.Entries[2].Entry.IsExperimentalOnly);
        page.SelectedExperimental = page.Experimentals[0];
        Assert.False(page.AddCommand.CanExecute(null));
    }

    [Fact]
    public void Modules_OnlyMyEngineers_NarrowsToWhatTheSelectionCanRoll_AndIsRemembered()
    {
        var (shell, _, store) = Create(PersistedState.Empty with { EngineerIds = [300100] });
        var modules = Open<ModulesPage>(shell, PageKind.Modules);
        var all = modules.Groups.Sum(group => group.Types.Count);
        Assert.Equal("", modules.FilterText);

        Open<EngineersPage>(shell, PageKind.Engineers).OnlyMyEngineers = true;
        Assert.Equal("true", store.State.Preferences!["engineers.onlyMine"]);
        modules = Open<ModulesPage>(shell, PageKind.Modules);
        var narrowed = modules.Groups.Sum(group => group.Types.Count);
        Assert.InRange(narrowed, 1, all - 1);
        Assert.All(modules.Groups, group => Assert.All(group.Types, type => Assert.True(type.Reachable > 0)));
        Assert.Contains("1 ticked engineer can roll", modules.FilterText, StringComparison.Ordinal);

        var again = Open<ModulesPage>(Create(store.State).Shell, PageKind.Modules);
        Assert.Equal(narrowed, again.Groups.Sum(group => group.Types.Count));

        var nobody = Open<ModulesPage>(Create(store.State with { EngineerIds = [] }).Shell, PageKind.Modules);
        Assert.Equal(all, nobody.Groups.Sum(group => group.Types.Count));
        Assert.Contains("nobody is ticked", nobody.FilterText, StringComparison.Ordinal);
    }

    [Fact]
    public void SavedData_MovesToAFolder_AndLoadsAFileAlreadyThere()
    {
        var (shell, _, store) = Create();
        var settings = Open<SettingsPage>(shell, PageKind.Settings);
        Assert.Equal("", settings.DataFolder);
        Assert.Equal("", settings.DataStatus);

        var savesBefore = store.Saves;
        settings.DataFolder = "";
        Assert.Equal("memory", store.Location);
        Assert.Equal(savesBefore, store.Saves);

        settings.DataFolder = @"D:\OneDrive\Katey";
        Assert.Equal(Path.Combine(@"D:\OneDrive\Katey", JsonFileStateStore.FileName), store.Location);
        Assert.True(store.Saves > savesBefore);
        Assert.StartsWith("The saved data now lives in", settings.DataStatus, StringComparison.Ordinal);
        Assert.Equal(@"D:\OneDrive\Katey", settings.DataFolder);

        savesBefore = store.Saves;
        settings.DataFolder = @"D:\OneDrive\Katey";
        Assert.Equal(savesBefore, store.Saves);

        var entry = PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, null, PlanSource.Manual);
        store.AtFolders[@"E:\Other"] = PersistedState.Empty with { EngineerIds = [300100], Plan = [entry], AllowSpansh = true, Preferences = new Dictionary<string, string> { ["rail.bodyFilter"] = "bacteria" } };
        settings = Assert.IsType<SettingsPage>(shell.Navigator.Current);
        settings.DataFolder = @"E:\Other";
        Assert.StartsWith("Loaded the Katey data already in", settings.DataStatus, StringComparison.Ordinal);
        Assert.True(shell.Workspace.Engineers.Contains("Felicity Farseer"));
        Assert.Equal("FSD_LongRange", Assert.Single(shell.Workspace.Plan.Entries).Entry.Blueprint);
        Assert.True(shell.Workspace.Plot.Enabled);
        Assert.Equal(BodyFilter.SkipBacteria, shell.Rail.Filter);
        Assert.Equal([300100], store.State.EngineerIds);
        Assert.Equal(@"E:\Other", Assert.IsType<SettingsPage>(shell.Navigator.Current).DataFolder);
    }

    [Fact]
    public void Navigator_RemembersScrollFractionsPerEntry()
    {
        var (shell, _, _) = Create();
        shell.Navigator.Open(new PageKey(PageKind.Modules));
        shell.Navigator.RecordScroll(0.6);
        shell.Navigator.Open(new PageKey(PageKind.Engineers));
        Assert.Equal(0, shell.Navigator.ScrollFraction);

        shell.Navigator.BackCommand.Execute(null);
        Assert.Equal(0.6, shell.Navigator.ScrollFraction);
        shell.Navigator.RecordScroll(7);
        Assert.Equal(1, shell.Navigator.ScrollFraction);
        shell.Navigator.ForwardCommand.Execute(null);
        Assert.Equal(0, shell.Navigator.ScrollFraction);

        Assert.Equal(["Engineers", "Modules", "Materials", "Unlocks", "Fleet", "Route", "System"], shell.Nav.Select(item => item.Label));
    }
}
