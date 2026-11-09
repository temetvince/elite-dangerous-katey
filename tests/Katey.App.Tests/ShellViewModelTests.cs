using Katey.App.Navigation;
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

/// <summary>Checks the shell end to end with fakes for the store and the journal: pages, plan, rail, search and saving.</summary>
public sealed class ShellViewModelTests
{
    private const string Snapshot = /*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:00Z","event":"Materials","Raw":[{"Name":"arsenic","Count":0}],"Manufactured":[],"Encoded":[{"Name":"dataminedwake","Count":3}]}""";

    private static readonly Catalog Catalog = Catalog.LoadEmbedded();
    private static readonly ModuleCatalog Modules = ModuleCatalog.LoadEmbedded();
    private static readonly ExobiologyPredictor Predictor = ExobiologyPredictor.LoadEmbedded();

    private static (ShellViewModel Shell, FakeJournalMonitor Monitor) Create(FakeStateStore store)
    {
        var sinks = new JournalSinks();
        var monitor = new FakeJournalMonitor(sinks);
        var shell = new ShellViewModel(Catalog, Modules, Predictor, store, monitor, sinks, new FakeRoutePlotter(), new FakeSystemCatalog(), new FakeJournalBackup(), action => action(), NullLogger<ShellViewModel>.Instance);
        return (shell, monitor);
    }

    private static PersistedState StateWith(params PlanEntry[] entries) => PersistedState.Empty with { Plan = entries };

    private static T Open<T>(ShellViewModel shell, PageKind kind, string id = "")
        where T : Page
    {
        shell.Navigator.Open(new PageKey(kind, id));
        return Assert.IsType<T>(shell.Navigator.Current);
    }

    [Fact]
    public void Rail_TellsItsChoiceButtons_BeforeAnySystemIsKnown()
    {
        // A choice restored at start, before the journal is read, must reach the radios: the notifications do not wait for a system.
        var (shell, _) = Create(new FakeStateStore());
        var rail = shell.Rail;
        var raised = new List<string>();
        rail.PropertyChanged += (_, args) => raised.Add(args.PropertyName ?? "");
        rail.ShowViewCommand.Execute(RailBodyView.Biology);
        Assert.True(rail.ShowsBiology);
        Assert.False(rail.ShowsLandable);
        Assert.Contains(nameof(RailViewModel.ShowsBiology), raised);
        Assert.Contains(nameof(RailViewModel.ShowsLandable), raised);
        Assert.Contains(nameof(RailViewModel.IsBiologyView), raised);
        raised.Clear();
        rail.FilterCommand.Execute(BodyFilter.SkipBacteria);
        Assert.True(rail.SkipsBacteria);
        Assert.Contains(nameof(RailViewModel.SkipsBacteria), raised);
        Assert.Contains(nameof(RailViewModel.ShowsAll), raised);
    }

    [Fact]
    public void Construction_RestoresTheSavedState_AndDropsEntriesTheCatalogLacks()
    {
        var kept = PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, "special_fsd_heavy", PlanSource.Manual);
        var dropped = kept with { Id = "x", Blueprint = "Gone_Blueprint" };
        var store = new FakeStateStore
        {
            State = new PersistedState([kept, dropped], ["Felicity Farseer", "Nobody"], @"C:\journals", [], SharingPolicy.SameHull, RailDetached: true, Preferences: new Dictionary<string, string> { ["rail.bodyFilter"] = "bacteria" }),
        };

        var (shell, _) = Create(store);

        Assert.Single(shell.Workspace.Plan.Entries);
        Assert.Equal(["Felicity Farseer"], shell.Workspace.Engineers.Selected);
        Assert.Equal(@"C:\journals", shell.Workspace.JournalFolder);
        Assert.Equal(SharingPolicy.SameHull, shell.Workspace.Fleet.Policy);
        Assert.Equal(BodyFilter.SkipBacteria, shell.Rail.Filter);
        Assert.True(shell.Rail.IsDetached);
        Assert.IsType<HomePage>(shell.Navigator.Current);
        Assert.Equal("Elite Dangerous - Katey", ShellViewModel.Title);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public void AddingFromAModulePage_UpdatesTheDrawer_AndSaves()
    {
        var store = new FakeStateStore();
        var (shell, _) = Create(store);
        Assert.True(shell.PlanDrawer.Totals.IsEmpty);

        var page = Open<ModuleTypePage>(shell, PageKind.ModuleType, "fsd");
        page.SelectedExperimental = page.Experimentals.Single(choice => choice.Key == "special_fsd_heavy");
        var row = page.Blueprints.Single(row => row.Key == "FSD_LongRange");
        Assert.Equal(5, row.SelectedGrade);
        row.AddCommand.Execute(null);

        var entry = Assert.Single(shell.Workspace.Plan.Entries);
        Assert.Equal("special_fsd_heavy", entry.Entry.Experimental);
        Assert.False(shell.PlanDrawer.Totals.IsEmpty);
        Assert.Contains(shell.PlanDrawer.Totals.Sections.Single(section => section.Label == "Encoded").Rows, row => row.Name == "Datamined Wake Exceptions" && row.Needed == 5);
        Assert.Equal("1", shell.PlanDrawer.Badge);
        Assert.Equal(1, store.Saves);
        Assert.Single(store.State.Plan);
        Assert.Contains("1 plan entry", page.PlanText, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectingEngineers_RefreshesWarnings_Pages_AndCoverage()
    {
        var store = new FakeStateStore { State = StateWith(PlanEntry.Create(Catalog, "bsg", "ShieldGenerator_Reinforced", 3, null, PlanSource.Manual)) };
        var (shell, _) = Create(store);
        var entry = shell.Workspace.Plan.Entries[0];
        Assert.False(entry.HasWarning);

        var engineers = Open<EngineersPage>(shell, PageKind.Engineers);
        var farseer = engineers.Engineers.Single(row => row.Name == "Felicity Farseer");
        Assert.Equal("Deciat", farseer.Where.System!.Label);
        Assert.Equal("Deciat · Farseer Inc", farseer.Where.Text);
        farseer.Where.System.OpenCommand.Execute(null);
        var deciat = Assert.IsType<SystemPage>(shell.Navigator.Current);
        Assert.Equal("Deciat", deciat.Name);
        Assert.True(deciat.HasName);
        farseer.Selected = true;

        Assert.True(entry.HasWarning);
        Assert.Contains("None of your selected engineers offers grade 3", entry.Warning, StringComparison.Ordinal);
        Assert.Equal(["Felicity Farseer"], store.State.Engineers);
        shell.Workspace.Scope.OnlyMyEngineers = true;
        var modules = Open<ModulesPage>(shell, PageKind.Modules);
        Assert.True(modules.HasSelection);
        Assert.DoesNotContain(modules.Groups, group => group.Label == "Hardpoints");
        var fsd = modules.Groups.Single(group => group.Label == "Core internals").Types.Single(type => type.Link.Label == "Frame Shift Drive");
        Assert.True(fsd.Reachable > 0);

        var felicity = Open<EngineerPage>(shell, PageKind.Engineer, "Felicity Farseer");
        Assert.True(felicity.Selected);
        Assert.Contains(felicity.Offers, group => group.ModuleType.Label == "Frame Shift Drive");
        felicity.Selected = false;
        Assert.False(entry.HasWarning);
        Assert.Empty(shell.Workspace.Engineers.Selected);
    }

    [Fact]
    public void Import_AddsTheTickedModules_AndOpensTheDrawer()
    {
        var (shell, _) = Create(new FakeStateStore());
        var import = shell.PlanDrawer.Import;
        import.Text = /*lang=json,strict*/ """[{"header":{"appName":"EDSY"},"data":{"event":"Loadout","Ship":"mandalay","ShipName":"Ironic","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_overcharge_size5_class5","Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"ExperimentalEffect":"special_fsd_heavy"}},{"Slot":"MainEngines","Item":"int_engine_size5_class5","Engineering":{"BlueprintName":"Engine_Dirty","Level":5}}]}}]""";
        import.ReadCommand.Execute(null);
        Assert.Equal(2, import.Modules.Count);
        import.Modules[1].Include = false;
        import.AddCommand.Execute(null);

        var entry = Assert.Single(shell.Workspace.Plan.Entries);
        Assert.Equal("FSD_LongRange", entry.Entry.Blueprint);
        Assert.True(shell.PlanDrawer.IsOpen);
        Assert.False(shell.PlanDrawer.ShowImport);
    }

    [Fact]
    public void DisablingAnEntry_RemovesItsMaterials_AndEditingRollsMultiplies()
    {
        var store = new FakeStateStore { State = StateWith(PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, null, PlanSource.Manual)) };
        var (shell, _) = Create(store);
        var entry = shell.Workspace.Plan.Entries[0];

        entry.Rolls.Single(roll => roll.Grade == 5).Count = 4;
        Assert.Equal(4, shell.PlanDrawer.Totals.Sections[2].Rows.Single(row => row.Name == "Datamined Wake Exceptions").Needed);

        entry.Enabled = false;
        Assert.True(shell.PlanDrawer.Totals.IsEmpty);
        Assert.Equal("empty", shell.PlanDrawer.Badge);

        entry.TargetGrade = 2;
        entry.Enabled = true;
        Assert.DoesNotContain(shell.PlanDrawer.Totals.Sections[2].Rows, row => row.Name == "Datamined Wake Exceptions");
        Assert.True(store.Saves >= 4);
    }

    [Fact]
    public void Inventory_LaysHaveAndShortAgainstThePlan_OnTheDrawer_TheMaterialPages_AndTheRail()
    {
        var store = new FakeStateStore { State = StateWith(PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, null, PlanSource.Manual)) };
        var (shell, monitor) = Create(store);
        Assert.False(shell.PlanDrawer.Totals.HasInventory);

        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T19:59:00Z","event":"LoadGame","Commander":"Casey","Ship":"Mandalay","ShipID":3}""", Snapshot);

        Assert.True(shell.PlanDrawer.Totals.HasInventory);
        Assert.Equal("1 · 10 short", shell.PlanDrawer.Badge);
        var datamined = shell.PlanDrawer.Totals.Sections.Single(section => section.Label == "Encoded").Rows.Single(row => row.Name == "Datamined Wake Exceptions");
        Assert.Equal("3", datamined.Have);
        Assert.Equal("2", datamined.Missing);

        var material = Open<MaterialPage>(shell, PageKind.Material, "Datamined Wake Exceptions");
        Assert.Equal("3", material.HaveText);
        Assert.Equal("5", material.NeededText);
        Assert.Equal("2", material.ShortText);
        Assert.Single(material.Spends);
        Assert.Contains(material.Uses, use => use.Blueprint.Label == "Increased range" && use.Grade == "G5");

        var materials = Open<MaterialsPage>(shell, PageKind.Materials);
        materials.ShortOnly = true;
        var shown = materials.Sections.SelectMany(section => section.Rows).ToList();
        Assert.Equal(10, shown.Count);
        Assert.All(shown, row => Assert.True(row.IsShort));
        materials.ShortOnly = false;
        materials.Filter = "wake";
        Assert.All(materials.Sections.SelectMany(section => section.Rows), row => Assert.Contains("Wake", row.Link.Label + row.Family, StringComparison.Ordinal));
    }

    [Fact]
    public void Start_WatchesTheSavedFolder_AndTheSettingsPageDrivesTheWatch()
    {
        var store = new FakeStateStore { State = PersistedState.Empty with { JournalFolder = @"C:\journals" } };
        var (shell, monitor) = Create(store);

        shell.Start();
        Assert.Equal(1, monitor.Starts);
        Assert.Equal(@"C:\journals", monitor.Folder);

        var settings = Open<SettingsPage>(shell, PageKind.Settings);
        Assert.True(settings.IsWatching);
        settings.Folder = @"D:\other";
        settings.WatchCommand.Execute(null);
        Assert.Equal(2, monitor.Starts);
        Assert.Equal(@"D:\other", store.State.JournalFolder);
        settings.StopCommand.Execute(null);
        Assert.False(settings.IsWatching);
    }

    [Fact]
    public void Fleet_ChecksSavedFitsAgainstOwnedModules_AcrossPages()
    {
        var store = new FakeStateStore();
        var (shell, monitor) = Create(store);

        monitor.Feed(
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:00Z","event":"Location","StarSystem":"Deciat","SystemAddress":3,"StarPos":[122.625,-0.8125,-47.28125],"Docked":true,"StationName":"Farseer Inc"}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:01Z","event":"Loadout","Ship":"mandalay","ShipID":3,"ShipName":"Ironic","ShipIdent":"AA-24M","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_overcharge_size5_class5","On":true,"Priority":0,"Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"ExperimentalEffect":"special_fsd_heavy"}},{"Slot":"MainEngines","Item":"int_engine_size5_class5","On":true,"Priority":0}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:02Z","event":"StoredModules","MarketID":1,"StationName":"Farseer Inc","StarSystem":"Deciat","Items":[{"Name":"$int_powerplant_size5_class5_name;","Name_Localised":"Power Plant","StorageSlot":1,"StarSystem":"Deciat","MarketID":1,"EngineerModifications":"PowerPlant_Boosted","Level":3,"Quality":1,"Hot":false},{"Name":"hpt_shieldbooster_size0_class5","StorageSlot":2,"StarSystem":"Sol","MarketID":2,"Hot":false},{"Name":"$hpt_beamlaser_gimbal_medium_name;","Name_Localised":"Beam Laser","StorageSlot":3,"StarSystem":"Deciat","MarketID":1,"EngineerModifications":"Weapon_Efficient","Level":5,"Quality":1,"Hot":false}]}""");

        var fleet = Open<FleetPage>(shell, PageKind.Fleet);
        var ship = Assert.Single(fleet.Ships);
        Assert.True(ship.IsCurrent);
        Assert.Contains("Deciat", fleet.Locations.Select(choice => choice.Key));

        fleet.PasteText = /*lang=json,strict*/ """{"event":"Loadout","Ship":"Mandalay","ShipName":"Dream fit","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_overcharge_size5_class5","Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"ExperimentalEffect":"special_fsd_heavy"}},{"Slot":"PowerPlant","Item":"int_powerplant_size5_class5","Engineering":{"BlueprintName":"PowerPlant_Boosted","Level":2,"ExperimentalEffect":"special_powerplant_highcharge"}},{"Slot":"MainEngines","Item":"int_engine_size5_class5","Engineering":{"BlueprintName":"Engine_Dirty","Level":5}}]}""";
        fleet.AddFittingCommand.Execute(null);
        Assert.False(fleet.HasAddError);
        Assert.Single(store.State.Fittings);

        // The same name on the same hull asks before replacing; the second press replaces in place, keeping the id.
        var savedId = store.State.Fittings[0].Id;
        fleet = Assert.IsType<FleetPage>(shell.Navigator.Current);
        fleet.PasteText = /*lang=json,strict*/ """{"event":"Loadout","Ship":"mandalay","ShipName":"DREAM FIT","UnladenMass":400,"FuelCapacity":{"Main":32,"Reserve":0.5},"Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_overcharge_size5_class5"}]}""";
        fleet.AddFittingCommand.Execute(null);
        Assert.True(fleet.IsConfirmingReplace);
        Assert.Equal("Replace the saved fit Dream fit for the Mandalay?", fleet.ReplaceText);
        Assert.Single(store.State.Fittings);
        Assert.Equal(3, store.State.Fittings[0].Modules.Count);
        var announced = new List<string?>();
        fleet.PropertyChanged += (_, args) => announced.Add(args.PropertyName);
        fleet.AddFittingCommand.Execute(null);
        Assert.False(fleet.IsConfirmingReplace);
        Assert.Contains(nameof(FleetPage.IsConfirmingReplace), announced);
        Assert.Contains(nameof(FleetPage.ReplaceText), announced);
        Assert.Equal("", fleet.PasteText);
        var replaced = Assert.Single(store.State.Fittings);
        Assert.Equal(savedId, replaced.Id);
        Assert.Equal("DREAM FIT", replaced.Name);
        Assert.Single(replaced.Modules);
        Assert.Equal(32, shell.Workspace.Fleet.ProfileOf(replaced)!.TankSize);
        Assert.StartsWith("{\"event\":\"Loadout\",\"Ship\":\"mandalay\",\"ShipName\":\"DREAM FIT\"", replaced.LoadoutJson, StringComparison.Ordinal);
        var replacedPage = Open<FitPage>(shell, PageKind.Fit, savedId);
        Assert.Equal(replaced.LoadoutJson, replacedPage.EdsyJson);
        Open<FleetPage>(shell, PageKind.Fleet);

        // Editing the paste withdraws the question, and the cancel button keeps the saved fit.
        fleet = Assert.IsType<FleetPage>(shell.Navigator.Current);
        fleet.PasteText = /*lang=json,strict*/ """{"event":"Loadout","Ship":"mandalay","ShipName":"Dream fit","Modules":[]}""";
        fleet.AddFittingCommand.Execute(null);
        Assert.True(fleet.IsConfirmingReplace);
        fleet.PasteText += " ";
        Assert.False(fleet.IsConfirmingReplace);
        fleet.AddFittingCommand.Execute(null);
        Assert.True(fleet.IsConfirmingReplace);
        fleet.CancelReplaceCommand.Execute(null);
        Assert.False(fleet.IsConfirmingReplace);
        Assert.Single(replaced.Modules);
        Assert.Single(store.State.Fittings);

        // The same name on another hull is a new fit, saved at once.
        fleet.PasteText = /*lang=json,strict*/ """{"event":"Loadout","Ship":"anaconda","ShipName":"Dream fit","Modules":[]}""";
        fleet.AddFittingCommand.Execute(null);
        Assert.False(fleet.IsConfirmingReplace);
        Assert.Equal(2, store.State.Fittings.Count);
        shell.Workspace.Fleet.Remove(store.State.Fittings[1].Id);
        shell.Workspace.Fleet.Update(new Fitting(savedId, "mandalay", "Dream fit", [
            new FitModule("FrameShiftDrive", "int_hyperdrive_overcharge_size5_class5", new Engineering("FSD_LongRange", 5, "special_fsd_heavy")),
            new FitModule("PowerPlant", "int_powerplant_size5_class5", new Engineering("PowerPlant_Boosted", 2, "special_powerplant_highcharge")),
            new FitModule("MainEngines", "int_engine_size5_class5", new Engineering("Engine_Dirty", 5, null)),
        ]));

        fleet = Assert.IsType<FleetPage>(shell.Navigator.Current);
        var fit = Assert.Single(fleet.Fits);
        Assert.Equal("1 missing, 1 with an unknown experimental.", fit.Summary);
        Assert.False(fit.AllOwned);
        Assert.Equal("1 MISSING", fit.Verdict);
        Assert.True(fit.IsBad);
        Assert.Contains(fleet.Unused, row => row.Name.StartsWith("Shield Booster", StringComparison.Ordinal) && !row.IsEngineered);
        Assert.Contains(fleet.Unused, row => row.Name == "Beam Laser 2D Gimballed" && row.IsEngineered && row.Link?.Label == "Beam Laser");

        fleet.SelectedLocation = fleet.Locations.Single(choice => choice.Key == "Deciat");
        fleet = Assert.IsType<FleetPage>(shell.Navigator.Current);
        Assert.DoesNotContain(fleet.Unused, row => row.Name.StartsWith("Shield Booster", StringComparison.Ordinal));
        fleet.SelectedFilter = fleet.Filters.Single(choice => choice.Key == nameof(EngineeringFilter.EngineeredOnly));
        fleet = Assert.IsType<FleetPage>(shell.Navigator.Current);
        var beam = Assert.Single(fleet.Unused);
        Assert.Equal("Efficient G5, experimental unknown", beam.Engineering);

        var fitPage = Open<FitPage>(shell, PageKind.Fit, store.State.Fittings[0].Id);
        Assert.Equal(3, fitPage.Lines.Count);
        Assert.Equal(1, fitPage.MissingEngineered);
        Assert.Equal("Plan the 1 gap in what you own", fitPage.PlanMissingText);
        fitPage.PlanMissingCommand.Execute(null);
        var planned = Assert.Single(shell.Workspace.Plan.Entries);
        Assert.Equal("Engine_Dirty", planned.Entry.Blueprint);
        Assert.Equal("Dream fit", planned.Entry.Source.Ship);

        // Once the gap is in the plan the button stands down and says so; pressing it again adds nothing.
        Assert.Equal(0, fitPage.MissingEngineered);
        Assert.Equal(1, fitPage.MissingEngineeredInAll);
        Assert.Equal("Every gap in what you own is in the plan", fitPage.PlanMissingText);
        Assert.False(fitPage.PlanMissingCommand.CanExecute(null));
        fitPage.PlanMissingCommand.Execute(null);
        Assert.Single(shell.Workspace.Plan.Entries);

        var beamPage = Open<ModuleTypePage>(shell, PageKind.ModuleType, "bl");
        Assert.Single(beamPage.Owned);
        Assert.Contains(beamPage.Variants, variant => variant.Name == "Beam Laser 2D Gimballed" && variant.Owned);
        Assert.Contains(beamPage.Variants, variant => variant.Name == "Beam Laser 1E Fixed" && !variant.Owned);

        var shipPage = Open<ShipPage>(shell, PageKind.Ship, "3");
        Assert.Equal("Ironic (AA-24M)", shipPage.Title);
        Assert.Equal(2, shipPage.Modules.Count);
        Assert.Single(shipPage.Fits);

        fitPage = Open<FitPage>(shell, PageKind.Fit, store.State.Fittings[0].Id);
        fitPage.RemoveCommand.Execute(null);
        Assert.Empty(store.State.Fittings);
        Assert.IsType<FitPage>(shell.Navigator.Current);
        Assert.Equal("Fit removed", shell.Navigator.Current.Title);
    }

    [Fact]
    public void Navigation_KeepsHistory_AndUnknownIdsLandOnNotFound()
    {
        var (shell, _) = Create(new FakeStateStore());
        Assert.False(shell.Navigator.CanGoBack);
        Assert.Equal(["Engineers", "Modules", "Materials", "Unlocks", "Fleet", "Stats", "Route", "System"], shell.Nav.Select(item => item.Label));
        Assert.All(shell.Nav, item => Assert.False(item.IsCurrent));
        Assert.Equal("Settings", shell.SettingsNav.Label);
        Assert.False(shell.SettingsNav.IsCurrent);
        shell.SettingsCommand.Execute(null);
        Assert.IsType<SettingsPage>(shell.Navigator.Current);
        Assert.True(shell.SettingsNav.IsCurrent);
        shell.SettingsCommand.Execute(null);
        Assert.IsType<HomePage>(shell.Navigator.Current);
        Assert.False(shell.SettingsNav.IsCurrent);

        shell.Nav[1].Link.OpenCommand.Execute(null);
        Assert.IsType<ModulesPage>(shell.Navigator.Current);
        Assert.True(shell.Nav[1].IsCurrent);
        shell.Navigator.Open(new PageKey(PageKind.Blueprint, "FSD_LongRange"));
        Assert.True(shell.Nav[1].IsCurrent);
        Assert.False(shell.Nav[0].IsCurrent);
        shell.HomeLink.OpenCommand.Execute(null);
        Assert.IsType<HomePage>(shell.Navigator.Current);
        Assert.All(shell.Nav, item => Assert.False(item.IsCurrent));

        shell.Navigator.Open(new PageKey(PageKind.Modules));
        shell.Navigator.Open(new PageKey(PageKind.ModuleType, "bl"));
        Assert.True(shell.Navigator.CanGoBack);
        Assert.False(shell.Navigator.CanGoForward);

        shell.Navigator.BackCommand.Execute(null);
        Assert.IsType<ModulesPage>(shell.Navigator.Current);
        Assert.True(shell.Navigator.CanGoForward);
        shell.Navigator.ForwardCommand.Execute(null);
        Assert.IsType<ModuleTypePage>(shell.Navigator.Current);

        shell.Navigator.BackCommand.Execute(null);
        shell.Navigator.Open(new PageKey(PageKind.Engineer, "Nobody"));
        Assert.False(shell.Navigator.CanGoForward);
        var notFound = Assert.IsType<NotFoundPage>(shell.Navigator.Current);
        Assert.Contains("Nobody", notFound.Message, StringComparison.Ordinal);

        var modules = Open<ModulesPage>(shell, PageKind.Modules);
        var hardpoints = modules.Groups.Single(group => group.Label == "Hardpoints");
        var beams = hardpoints.Types.Single(type => type.Link.Label == "Beam Laser");
        beams.Link.OpenCommand.Execute(null);
        Assert.Equal("Beam Laser", shell.Navigator.Current.Title);
    }

    [Fact]
    public void Search_FindsCatalogEntities_AndJournalEntitiesOnceTheyExist()
    {
        var (shell, monitor) = Create(new FakeStateStore());

        shell.Search.Query = "felic";
        var hit = Assert.Single(shell.Search.Results, result => result.Kind == "Engineer");
        Assert.Equal("Felicity Farseer", hit.Link.Label);
        shell.Search.Open(hit);
        Assert.IsType<EngineerPage>(shell.Navigator.Current);
        Assert.Equal("", shell.Search.Query);

        shell.Search.Query = "beam";
        Assert.Equal("Module", shell.Search.Results[0].Kind);
        shell.Search.Query = "datamined";
        Assert.Contains(shell.Search.Results, result => result.Kind == "Material");
        shell.Search.Query = "increased range";
        Assert.Contains(shell.Search.Results, result => result.Kind == "Blueprint" && result.Link.Label == "Increased range");
        shell.Search.Query = "koine";
        Assert.True(shell.Search.IsEmpty);

        monitor.Feed(
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:00Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:01Z","event":"Loadout","Ship":"mandalay","ShipID":3,"ShipName":"Ironic","ShipIdent":"AA-24M","Modules":[]}""");

        shell.Search.Query = "koine";
        Assert.Equal("System", Assert.Single(shell.Search.Results).Kind);
        shell.Search.Query = "ironic";
        Assert.Equal("Ship", Assert.Single(shell.Search.Results).Kind);
        shell.Search.OpenFirstCommand.Execute(null);
        Assert.IsType<ShipPage>(shell.Navigator.Current);
    }

    [Fact]
    public void Rail_ShowsAOneJumpRoute_WhenASystemIsTargetedWithoutAPlottedRoute()
    {
        var (shell, monitor) = Create(new FakeStateStore());
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T18:20:00Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T18:20:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Koine","BodyID":0,"StarType":"M","DistanceFromArrivalLS":0,"SystemAddress":9}""");
        Assert.False(shell.Rail.HasRoute);

        // Targeting Deciat in the galaxy map without plotting: the journal says so, the route file stays empty.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-08T18:21:00Z","event":"FSDTarget","Name":"Deciat","SystemAddress":3,"StarClass":"K"}""");
        monitor.Status = new ShipStatus(new DateTimeOffset(2026, 11, 8, 18, 21, 1, TimeSpan.Zero), 0x10, null, "Deciat", 15.0, DestinationSystem: 3);
        shell.Rail.Refresh();
        Assert.True(shell.Rail.HasRoute);
        Assert.True(shell.Rail.ShowsRoute);
        Assert.Equal(["here", "next"], shell.Rail.RouteStops.Select(stop => stop.Marker));
        Assert.Equal("Deciat", shell.Rail.RouteStops[1].Link.Label);
        Assert.True(shell.Rail.RouteStops[1].IsScoopable);
        var route = Open<RoutePage>(shell, PageKind.Route);
        Assert.Equal(2, route.Stops.Count);
        Assert.Equal(StopStage.Here, route.Stops[0].Stage);
        Assert.Equal("M", route.Stops[0].StarClass);
        Assert.Empty(route.DetourParts);

        // The journal has no position for Deciat, but the Spansh plot does, so the jump has its distance and its fuel.
        shell.Workspace.Plot.Restore(new PlottedRoute("Koine", "Deciat", new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
        [
            new("Koine", 9, 107.40625, -40.59375, -71.5625, 0, 49, 0, 32, false, true, false),
            new("Deciat", 3, 122.625, -0.8125, -47.28125, 49, 0, 3, 29, false, true, false),
        ]));
        route = Open<RoutePage>(shell, PageKind.Route);
        Assert.Equal(49.0, route.Stops[1].JumpLy, 0.1);
        Assert.StartsWith("0.0 ly of 49.0 ly travelled (0%)", route.TravelledText, StringComparison.Ordinal);

        // With a plotted route, a target further along drops the stops between; one off the route is a detour that the plotted route resumes after.
        monitor.Route = new NavRoute(new DateTimeOffset(2026, 11, 8, 18, 22, 0, TimeSpan.Zero),
        [
            new("Koine", 9, "M", 107.40625, -40.59375, -71.5625),
            new("Deciat", 3, "K", 122.625, -0.8125, -47.28125),
            new("Sol", 1, "G", 0, 0, 0),
        ]);
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-08T18:23:00Z","event":"FSDTarget","Name":"Sol","SystemAddress":1,"StarClass":"G"}""");
        route = Open<RoutePage>(shell, PageKind.Route);
        Assert.Equal(["Koine", "Sol"], route.Stops.Select(stop => stop.System));
        Assert.Empty(route.DetourParts);
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-08T18:24:00Z","event":"FSDTarget","Name":"Praea","SystemAddress":77,"StarClass":"K"}""");
        route = Open<RoutePage>(shell, PageKind.Route);
        Assert.Equal(["Koine", "Praea"], route.Stops.Select(stop => stop.System));
        Assert.Equal("Detour to Praea. The plotted route to Sol resumes after it, 2 jumps.", Phrase.Plain(route.DetourParts));
        Assert.Equal(["Praea", "Sol"], route.DetourParts.Where(part => part.IsLink).Select(part => part.Text));
        shell.Rail.Refresh();
        Assert.StartsWith("Detour to Praea.", Phrase.Plain(shell.Rail.RouteRestParts), StringComparison.Ordinal);
    }

    [Fact]
    public void Position_ComesFromTheRouteFile_WhenItIsNewerThanTheJournalsLastArrival()
    {
        var (shell, monitor) = Create(new FakeStateStore());
        monitor.Feed(
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T04:00:00Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T04:00:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Koine","BodyID":0,"StarType":"M","DistanceFromArrivalLS":0,"SystemAddress":9}""");
        Assert.Equal("Koine", shell.Workspace.Route.CurrentSystem);

        // A route file older than the arrival changes nothing.
        monitor.Route = new NavRoute(new DateTimeOffset(2026, 11, 8, 3, 0, 0, TimeSpan.Zero), [new("Sol", 1, "G", 0, 0, 0), new("Deciat", 3, "K", 122.625, -0.8125, -47.28125)]);
        Assert.Equal("Koine", shell.Workspace.Route.CurrentSystem);

        // The game rewrote the day's journal and the arrival at Praea is lost; the route file written since starts there.
        monitor.Route = new NavRoute(new DateTimeOffset(2026, 11, 8, 17, 51, 0, TimeSpan.Zero), [new("Praea Euq BF-V b30-2", 5078665078537, "M", 1, 2, 3), new("Praea Euq OC-M d7-66", 2278387894723, "N", 4, 5, 6)]);
        var route = shell.Workspace.Route;
        Assert.Equal("Praea Euq BF-V b30-2", route.CurrentSystem);
        Assert.Equal(5078665078537, route.CurrentSystemAddress);
        Assert.Equal(new StarPosition(1, 2, 3), route.CurrentPosition);
        var system = shell.Workspace.System;
        Assert.Equal("Praea Euq BF-V b30-2", system.SystemName);
        Assert.Empty(system.Bodies);
        shell.Rail.Refresh();
        Assert.Equal("Praea Euq BF-V b30-2", shell.Rail.SystemName);
    }

    [Fact]
    public void Rail_SummarisesPositionRouteAndSystem_AndFiltersBodies()
    {
        var store = new FakeStateStore { State = StateWith(PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, null, PlanSource.Manual)) };
        var (shell, monitor) = Create(store);
        Assert.Equal("No position yet", shell.Rail.SystemName);
        Assert.False(shell.Rail.HasRoute);

        monitor.Route = new NavRoute(DateTimeOffset.UtcNow,
        [
            new("Koine", 9, "M", 107.40625, -40.59375, -71.5625),
            new("Deciat", 3, "K", 122.625, -0.8125, -47.28125),
            new("Sol", 1, "G", 0, 0, 0),
        ]);
        monitor.Feed(
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:00Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:01Z","event":"FSDTarget","Name":"Deciat","StarClass":"K","RemainingJumpsInRoute":1}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:02Z","event":"FSSDiscoveryScan","SystemName":"Koine","BodyCount":3}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:03Z","event":"Scan","ScanType":"Detailed","BodyName":"Koine 1","BodyID":1,"DistanceFromArrivalLS":50,"PlanetClass":"Rocky body","Landable":true,"SurfaceGravity":3.9,"SurfaceTemperature":210,"Materials":[{"Name":"arsenic","Percent":2.3},{"Name":"iron","Percent":20.1}],"WasDiscovered":true,"WasMapped":false}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:04Z","event":"Scan","ScanType":"Detailed","BodyName":"Koine 2","BodyID":2,"DistanceFromArrivalLS":900,"PlanetClass":"Icy body","Landable":true,"Atmosphere":"thin argon atmosphere","SurfaceGravity":2.1,"SurfaceTemperature":80,"WasDiscovered":true,"WasMapped":false}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:05Z","event":"FSSBodySignals","BodyName":"Koine 2","BodyID":2,"Signals":[{"Type":"$SAA_SignalType_Biological;","Type_Localised":"Biological","Count":1}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:06Z","event":"SAASignalsFound","BodyName":"Koine 2","BodyID":2,"Signals":[{"Type":"$SAA_SignalType_Biological;","Type_Localised":"Biological","Count":1}],"Genuses":[{"Genus":"$Codex_Ent_Bacterial_Genus_Name;","Genus_Localised":"Bacterium"}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T20:00:07Z","event":"Scan","ScanType":"Detailed","BodyName":"Koine 3","BodyID":3,"DistanceFromArrivalLS":1200,"PlanetClass":"Gas giant with water based life","WasDiscovered":true,"WasMapped":true}""");

        var rail = shell.Rail;
        Assert.Equal("Koine", rail.SystemName);
        Assert.True(rail.HasRoute);
        Assert.Equal("Route · 2 jumps left", rail.RouteHeading);
        Assert.Equal(["here", "next", "3"], rail.RouteStops.Select(stop => stop.Marker));
        Assert.True(rail.RouteStops[1].IsScoopable);
        Assert.Equal("Felicity Farseer", rail.RouteStops[1].Engineer?.Name);
        // Koine's M star is scoopable, and so is Deciat's K star next.
        Assert.Contains("This system's arrival star is scoopable; the next star is scoopable too.", Phrase.Plain(rail.FuelParts), StringComparison.Ordinal);
        Assert.Equal("This system · 3 of 3 scanned", rail.SystemHeading);
        Assert.Equal("3 of 3 scanned", rail.ScanText);
        Assert.Equal(1.0, rail.ScanFraction);
        var systemPage = Open<SystemPage>(shell, PageKind.System);
        Assert.Contains("scanned bodies", systemPage.DiscoveryText, StringComparison.Ordinal);
        Assert.Contains("was discovered before you", systemPage.DiscoveryText, StringComparison.Ordinal);
        Assert.EndsWith("nobody has set foot on any of its 2 landable bodies.", systemPage.DiscoveryText, StringComparison.Ordinal);
        // Landing on Koine 1 counts as a landing, and the untrodden count comes down by one; the first footfall is the body card's to tell.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:07Z","event":"Touchdown","PlayerControlled":true,"StarSystem":"Koine","SystemAddress":9,"Body":"Koine 1","BodyID":1,"OnStation":false,"OnPlanet":true}""");
        systemPage = Open<SystemPage>(shell, PageKind.System);
        Assert.EndsWith("you have landed on 1 body; nobody has set foot on 1 of its 2 landable bodies.", systemPage.DiscoveryText, StringComparison.Ordinal);
        Assert.DoesNotContain("first", systemPage.DiscoveryText, StringComparison.Ordinal);
        var trodden = systemPage.Bodies.Single(row => row.Name == "1");
        Assert.Equal("footfall by you", trodden.FootfallStatus);
        Assert.True(trodden.FootfallYours);
        Assert.False(trodden.FootfallOpen);
        Assert.Equal("", trodden.StatusDetail);
        var routePage = Open<RoutePage>(shell, PageKind.Route);
        var here = routePage.Stops.Single(stop => stop.Stage == StopStage.Here);
        Assert.Equal("Koine", here.Link.Label);
        Assert.Equal(VisitState.Surveyed, here.Visit);
        Assert.Equal("surveyed", here.VisitedText);
        Assert.True(rail.ShowsRoute);
        Assert.False(rail.ShowsBodies);
        // Sol is further along the route: it becomes the next jump and Deciat drops out between.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:08Z","event":"FSDTarget","Name":"Sol","StarClass":"G"}""");
        Assert.True(rail.ShowsRoute);
        Assert.Equal(["here", "next"], rail.RouteStops.Select(stop => stop.Marker));
        Assert.Equal("Sol", rail.RouteStops[1].Link.Label);
        Assert.DoesNotContain("Detour", Phrase.Plain(rail.RouteRestParts), StringComparison.Ordinal);
        // A target the game reports as on the route counts even when the route file has not caught up with a replot.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:09Z","event":"FSDTarget","Name":"Birpinovit","StarClass":"K","RemainingJumpsInRoute":2}""");
        Assert.True(rail.ShowsRoute);
        // The drive charging shows the route whatever is targeted.
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:10Z","event":"FSDTarget","Name":"Sol","StarClass":"G"}""");
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x20010, null, null, 15.0);
        rail.Refresh();
        Assert.True(rail.ShowsRoute);
        // The charge has become the jump: the charging flag is gone, the hyperspace flag is up, and the route stays until arrival.
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x40000000, null, null, 15.0);
        rail.Refresh();
        Assert.True(rail.ShowsRoute);
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x10, null, null, 15.0);
        rail.Refresh();
        Assert.True(rail.ShowsBodies);
        // With the status file present, its destination decides: another system targeted shows the route, a
        // body here or nothing targeted shows the bodies, whatever the journal's last target said.
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x10, null, "Deciat", 15.0, DestinationSystem: 3);
        rail.Refresh();
        Assert.True(rail.ShowsRoute);
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x10, null, "Koine 2", 15.0, DestinationSystem: 9);
        rail.Refresh();
        Assert.True(rail.ShowsBodies);
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:10Z","event":"FSDTarget","Name":"Deciat","StarClass":"K","RemainingJumpsInRoute":1}""");
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x10, null, null, 15.0);
        rail.Refresh();
        Assert.True(rail.ShowsBodies);
        monitor.Status = null;
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:11Z","event":"FSDTarget","Name":"Deciat","StarClass":"K","RemainingJumpsInRoute":1}""");
        Assert.True(rail.ShowsRoute);

        // The Landable view by default, nearest first: every landable body with both its life and its materials.
        Assert.True(rail.ShowsLandable);
        Assert.False(rail.IsBiologyView);
        Assert.Equal(["1", "2"], rail.Bodies.Select(body => body.Name));
        Assert.True(rail.Bodies[0].MaterialsVisible);
        Assert.True(rail.Bodies[1].GeneraVisible);
        // Koine 2 is probed: its genus is a row with the likeliest species, the genus word dropped.
        var probed = rail.Bodies.Single(body => body.Name == "2");
        Assert.Equal("", probed.Life);
        var genus = Assert.Single(probed.GenusRows);
        Assert.Equal("Bacterium", genus.Name);
        Assert.DoesNotContain("Bacterium", genus.Species, StringComparison.Ordinal);
        Assert.Matches(@"^\S+ \d+%$", genus.Species);
        Assert.EndsWith(" Cr", genus.ValueText, StringComparison.Ordinal);
        Assert.Equal(0, genus.Stage);
        Assert.False(genus.IsFirstSample);
        Assert.False(genus.IsSecondSample);
        Assert.False(genus.IsComplete);
        Assert.Equal("Vesicula", GenusRow.ShortSpecies("Bacterium Vesicula", "Bacterium"));
        Assert.Equal("Tectonicas", GenusRow.ShortSpecies("Stratum Tectonicas", "Stratum"));

        // Biology lists only bodies with life, their life only; Minerals only those with materials, their life hidden.
        rail.View = RailBodyView.Biology;
        Assert.Equal("biology", store.State.Preferences!["rail.bodyView"]);
        Assert.True(rail.IsBiologyView);
        Assert.Equal(["2"], rail.Bodies.Select(body => body.Name));
        Assert.True(rail.Bodies[0].GeneraVisible);
        Assert.False(rail.Bodies[0].MaterialsVisible);
        Assert.True(rail.Bodies[0].ExobiologyCredits > 0);
        Assert.EndsWith(" Cr", rail.Bodies[0].ValueText, StringComparison.Ordinal);
        Assert.Equal("", rail.BodiesNote);
        rail.View = RailBodyView.Minerals;
        Assert.Equal(["1"], rail.Bodies.Select(body => body.Name));
        Assert.False(rail.Bodies[0].LifeVisible);
        Assert.True(rail.Bodies[0].MaterialsVisible);
        rail.View = RailBodyView.Biology;
        Assert.Equal(["2"], rail.Bodies.Select(body => body.Name));

        // One choice of three, remembered: skip bacteria hides Koine 2; valuable only hides it too, since bacteria cannot
        // reach 10M, with its own note; all life brings it back.
        Assert.True(rail.ShowsAll);
        rail.Filter = BodyFilter.SkipBacteria;
        Assert.Equal(BodyFilter.SkipBacteria, rail.Filter);
        Assert.Empty(rail.Bodies);
        Assert.Equal("1 body with only bacteria hidden.", rail.BodiesNote);
        Assert.Equal("bacteria", store.State.Preferences!["rail.bodyFilter"]);
        rail.Filter = BodyFilter.ValuableOnly;
        Assert.False(rail.SkipsBacteria);
        Assert.Empty(rail.Bodies);
        Assert.Equal("1 body hidden as not worth the stop: 1 probed and below 8.0M Cr a minute on the ground.", rail.BodiesNote);
        Assert.Equal("valuable", store.State.Preferences!["rail.bodyFilter"]);
        rail.Filter = BodyFilter.All;
        Assert.Equal(["2"], rail.Bodies.Select(body => body.Name));
        Assert.Equal("", rail.BodiesNote);

        Assert.True(rail.ShowsControls);
        rail.IsClickThrough = true;
        Assert.True(rail.ShowsControls);
        rail.IsDetached = true;
        Assert.False(rail.ShowsControls);
        Assert.Equal("true", store.State.Preferences!["rail.clickThrough"]);
        rail.IsOverlay = true;
        Assert.Equal("true", store.State.Preferences!["rail.overlay"]);
        rail.WindowBounds = " 10,20,380,720 ";
        Assert.Equal("10,20,380,720", store.State.Preferences!["rail.bounds"]);
        Assert.True(RailViewModel.TryParseBounds(rail.WindowBounds, out var left, out var top, out var width, out var height));
        Assert.Equal((10, 20, 380, 720), (left, top, width, height));
        Assert.False(RailViewModel.TryParseBounds("10,20,0,720", out _, out _, out _, out _));
        Assert.False(RailViewModel.TryParseBounds("", out _, out _, out _, out _));
        rail.ResetWindowCommand.Execute(null);
        Assert.Equal("", rail.WindowBounds);
        rail.IsClickThrough = false;
        rail.IsDetached = false;
        Assert.True(rail.ShowsControls);

        monitor.Feed(Snapshot);

        var engineers = Open<EngineersPage>(shell, PageKind.Engineers);
        Assert.Equal("Ram Tah", engineers.Engineers[0].Name);
        Assert.Equal("49.0 ly", engineers.Engineers.Single(row => row.Name == "Felicity Farseer").DistanceText);
        Assert.Equal(engineers.Engineers.Select(row => row.DistanceLy).Order(), engineers.Engineers.Select(row => row.DistanceLy));
        Assert.Equal("Distances from Koine.", Phrase.Plain(engineers.FromParts));
        Assert.Equal("Koine", engineers.FromParts.Single(part => part.IsLink).Link!.Label);

        var system = Open<SystemPage>(shell, PageKind.System);
        Assert.True(system.IsCurrent);
        Assert.Equal(3, system.Bodies.Count);
        var deciat = Open<SystemPage>(shell, PageKind.System, "Deciat");
        Assert.False(deciat.IsCurrent);
        Assert.StartsWith("49.0 ly from Koine.", Phrase.Plain(deciat.WhereParts), StringComparison.Ordinal);
        Assert.Equal("Koine", deciat.WhereParts.Single(part => part.IsLink).Link!.Label);
        var farseerPage = Open<EngineerPage>(shell, PageKind.Engineer, "Felicity Farseer");
        Assert.Equal("49.0 ly from Koine", Phrase.Plain(farseerPage.DistanceParts));
        Assert.True(farseerPage.DistanceParts[^1].IsLink);
        Assert.Single(deciat.Engineers);

        rail.IsDetached = true;
        Assert.True(store.State.RailDetached);

        // In the tunnel the status file has no destination and the game clears the route mid-jump on a route's
        // last leg; the journal's own jump keeps the route up, to the jump's destination, until arrival.
        monitor.Status = new ShipStatus(DateTimeOffset.UtcNow, 0x10, null, null, 15.0);
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:12Z","event":"StartJump","JumpType":"Hyperspace","StarSystem":"Deciat","SystemAddress":3,"StarClass":"K"}""");
        monitor.Route = null;
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:15Z","event":"NavRouteClear"}""");
        Assert.True(rail.ShowsRoute);
        Assert.Equal("Deciat", rail.RouteStops[1].Link.Label);
        monitor.Feed(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:30Z","event":"FSDJump","StarSystem":"Deciat","SystemAddress":3,"StarPos":[122.625,-0.8125,-47.28125]}""");
        Assert.True(rail.ShowsBodies);
        Assert.Equal("Deciat", rail.SystemName);
    }

    [Fact]
    public void JsonFileStateStore_RemembersAMovedLocationThroughThePointerFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "katey-tests", Guid.NewGuid().ToString("N"));
        var defaultPath = Path.Combine(root, "default", "state.json");
        var pointer = Path.Combine(root, "location.txt");
        try
        {
            var store = JsonFileStateStore.Configured(NullLogger<JsonFileStateStore>.Instance, defaultPath, pointer);
            Assert.Equal(defaultPath, store.Location);

            var moved = Path.Combine(root, "moved");
            Assert.False(store.Relocate(moved));
            Assert.Equal(Path.Combine(moved, JsonFileStateStore.FileName), store.Location);
            Assert.Equal(store.Location, File.ReadAllText(pointer));
            store.Save(PersistedState.Empty with { Engineers = ["Felicity Farseer"] });

            var again = JsonFileStateStore.Configured(NullLogger<JsonFileStateStore>.Instance, defaultPath, pointer);
            Assert.Equal(store.Location, again.Location);
            Assert.Equal(["Felicity Farseer"], again.Load().Engineers);
            Assert.True(again.Relocate(moved));
            Assert.False(again.Relocate(Path.Combine(root, "empty")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void JsonFileStateStore_RoundTripsThroughAFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "katey-tests", Guid.NewGuid().ToString("N"), "state.json");
        try
        {
            var store = new JsonFileStateStore(path, NullLogger<JsonFileStateStore>.Instance);
            Assert.Equal(PersistedState.Empty, store.Load());

            var entry = PlanEntry.Create(Catalog, "fsd", "FSD_LongRange", 5, "special_fsd_heavy", new PlanSource("Ship", "FrameShiftDrive", "int_hyperdrive_size5_class5")).WithRolls(3, 2);
            var fitting = new Fitting("fit-1", "mandalay", "Dream", [new FitModule("FrameShiftDrive", "int_hyperdrive_size5_class5", new Engineering("FSD_LongRange", 5, "special_fsd_heavy"))]);
            store.Save(new PersistedState([entry], ["Felicity Farseer"], @"C:\journals", [fitting], SharingPolicy.None, RailDetached: true, Preferences: new Dictionary<string, string> { ["rail.bodyFilter"] = "valuable" }));

            var loaded = new JsonFileStateStore(path, NullLogger<JsonFileStateStore>.Instance).Load();
            var restored = Assert.Single(loaded.Plan);
            Assert.Equal(entry.Blueprint, restored.Blueprint);
            Assert.Equal(2, restored.RollsAt(3));
            Assert.Equal(["Felicity Farseer"], loaded.Engineers);
            Assert.Equal(SharingPolicy.None, loaded.Policy);
            Assert.Equal("fit-1", Assert.Single(loaded.Fittings).Id);
            Assert.Equal("valuable", loaded.Preferences!["rail.bodyFilter"]);
            Assert.True(loaded.RailDetached);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void RoutePage_VisitState_TellsNewFromVisitedFromSurveyed()
    {
        var once = new VisitedSystem("Koine", 9, null, DateTimeOffset.UtcNow, 1);
        var progress = RouteProgress.Empty with { KnownSystems = new Dictionary<long, VisitedSystem> { [9] = once } };
        Assert.Equal(VisitState.New, RoutePage.VisitOf(progress, 9, "Koine", StopStage.Here));
        Assert.Equal(VisitState.Visited, RoutePage.VisitOf(progress, 9, "Koine", StopStage.Next));
        Assert.Equal(VisitState.Visited, RoutePage.VisitOf(progress, 0, "koine", StopStage.Done));
        Assert.Equal(VisitState.New, RoutePage.VisitOf(progress, 5, "Sol", StopStage.Next));

        var returned = progress with { KnownSystems = new Dictionary<long, VisitedSystem> { [9] = once with { Visits = 2 } } };
        Assert.Equal(VisitState.Visited, RoutePage.VisitOf(returned, 9, "Koine", StopStage.Here));

        var surveyed = progress with { KnownSystems = new Dictionary<long, VisitedSystem> { [9] = once with { Surveyed = true } } };
        Assert.Equal(VisitState.Surveyed, RoutePage.VisitOf(surveyed, 9, "Koine", StopStage.Here));
        Assert.Equal("surveyed", RoutePage.VisitLabel(VisitState.Surveyed));
        Assert.Equal("new to you", RoutePage.VisitLabel(VisitState.New));
    }
}
