namespace Katey.Journal.Tests;

/// <summary>Checks the route, exploration and fleet trackers against hand-written journal lines.</summary>
public sealed class TrackerTests
{
    private static JournalEvent Event(string line) => JournalEvent.Parse(line) ?? throw new InvalidOperationException("fixture did not parse");

    private static void Feed(IJournalSink sink, params string[] lines)
    {
        foreach (var line in lines)
        {
            sink.Apply(Event(line));
        }
    }

    [Fact]
    public void RouteTracker_TracksPositionTargetAndHistory()
    {
        var tracker = new RouteTracker();
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-01T10:00:00Z","event":"FSDJump","StarSystem":"Deciat","SystemAddress":3,"StarPos":[122.625,-0.8125,-47.28125]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:00Z","event":"Location","StarSystem":"Sol","SystemAddress":1,"StarPos":[0,0,0]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:01Z","event":"FSDTarget","Name":"Alpha Centauri","SystemAddress":2,"StarClass":"G","RemainingJumpsInRoute":3}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:02Z","event":"StartJump","JumpType":"Hyperspace","StarSystem":"Alpha Centauri","SystemAddress":2,"StarClass":"G"}""");

        var mid = tracker.Snapshot();
        Assert.Equal("Sol", mid.CurrentSystem);
        Assert.Equal("Alpha Centauri", mid.TargetSystem);
        Assert.Equal("G", mid.TargetStarClass);
        Assert.Equal(3, mid.RemainingJumps);
        Assert.True(mid.InHyperspace);
        Assert.Equal([1L, 3L], mid.KnownSystems.Keys.Order());
        Assert.Equal(new StarPosition(0, 0, 0), mid.CurrentPosition);
        var deciat = mid.FindByName("deciat");
        Assert.NotNull(deciat);
        Assert.Equal(new StarPosition(122.625, -0.8125, -47.28125), deciat.Position);
        Assert.Equal(1, deciat.Visits);

        // The game clears the route in the middle of a route's last jump; the jump's destination stands until arrival.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:09Z","event":"NavRouteClear"}""");
        var cleared = tracker.Snapshot();
        Assert.Equal("Alpha Centauri", cleared.TargetSystem);
        Assert.Equal("G", cleared.TargetStarClass);
        Assert.Null(cleared.RemainingJumps);
        Assert.True(cleared.InHyperspace);

        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"FSDJump","StarSystem":"Alpha Centauri","SystemAddress":2,"StarPos":[1,1,1]}""");
        var after = tracker.Snapshot();
        Assert.Equal("Alpha Centauri", after.CurrentSystem);
        Assert.False(after.InHyperspace);
        Assert.Null(after.TargetSystem);

        // A clear outside a jump forgets the target.
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"FSDTarget","Name":"Deciat","SystemAddress":3,"StarClass":"K"}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:05Z","event":"NavRouteClear"}""");
        Assert.Null(tracker.Snapshot().TargetSystem);
        Assert.Null(after.TargetSystem);
        Assert.True(after.HasVisited(2));
        Assert.Equal(new StarPosition(1, 1, 1), after.CurrentPosition);

        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"NavRouteClear"}""");
        Assert.Null(tracker.Snapshot().RemainingJumps);

        tracker.Reset();
        Assert.Empty(tracker.Snapshot().KnownSystems);
    }

    [Fact]
    public void RouteTracker_HoldsASuperchargeUntilTheNextJump()
    {
        var tracker = new RouteTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:47:26Z","event":"FSDJump","StarSystem":"Jackson's Lighthouse","SystemAddress":5532807773,"StarPos":[157,-27,-70]}""");
        Assert.False(tracker.Snapshot().Supercharged);
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:48:00Z","event":"JetConeBoost","BoostValue":6.0}""");
        Assert.True(tracker.Snapshot().Supercharged);
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:13:36Z","event":"FSDJump","StarSystem":"Col 359 Sector FN-I b11-5","SystemAddress":99,"StarPos":[160,-27,-70],"JumpDist":473.073,"FuelUsed":6.729,"FuelLevel":118.95,"BoostUsed":4}""");
        Assert.False(tracker.Snapshot().Supercharged);
    }

    [Fact]
    public void RouteTracker_RemembersWhichSystemsWereSurveyed()
    {
        var tracker = new RouteTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"StarPos":[107.40625,-40.59375,-71.5625]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"FSSDiscoveryScan","SystemName":"Koine","SystemAddress":9,"BodyCount":3,"NonBodyCount":1,"Progress":0.5}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:10:00Z","event":"FSDJump","StarSystem":"Deciat","SystemAddress":2,"StarPos":[122.625,-0.8125,-47.28125]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:20:00Z","event":"FSDJump","StarSystem":"Sol","SystemAddress":1,"StarPos":[0,0,0]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:21:00Z","event":"NavBeaconScan","SystemAddress":1,"NumBodies":40}""");

        var known = tracker.Snapshot().KnownSystems;
        Assert.True(known[9].Surveyed);
        Assert.False(known[2].Surveyed);
        Assert.True(known[1].Surveyed);
        Assert.Equal(1, known[9].Visits);
    }

    [Fact]
    public void RouteTracker_KeepsRecentJumps_AndForgetsThemOnAShipChange()
    {
        var tracker = new RouteTracker();
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:00Z","event":"FSDJump","StarSystem":"Koine","SystemAddress":9,"JumpDist":49.028,"FuelUsed":14.9286,"FuelLevel":49.07}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:05:00Z","event":"FSDJump","StarSystem":"Deciat","SystemAddress":3,"JumpDist":180.2,"FuelUsed":9.6,"FuelLevel":39.4,"BoostUsed":4}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:06:00Z","event":"FSDJump","StarSystem":"Sol","SystemAddress":1}""");

        var jumps = tracker.Snapshot().RecentJumps;
        Assert.Equal(2, jumps.Count);
        Assert.Equal(4, jumps[0].Boost);
        Assert.Equal(180.2, jumps[0].DistanceLy);
        Assert.Equal(1, jumps[1].Boost);
        Assert.Equal(14.9286, jumps[1].FuelUsed);

        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:07:00Z","event":"ShipyardSwap","ShipType":"mandalay","ShipID":2,"StoreOldShip":"anaconda","StoreShipID":1}""");
        Assert.Empty(tracker.Snapshot().RecentJumps);

        for (var index = 0; index < RouteTracker.RecentJumpCount + 3; index++)
        {
            Feed(tracker, /*lang=json,strict*/ $$$"""{"timestamp":"2026-11-05T11:00:{{{index % 60:00}}}Z","event":"FSDJump","StarSystem":"S{{{index}}}","SystemAddress":{{{index + 100}}},"JumpDist":10,"FuelUsed":1}""");
        }

        Assert.Equal(RouteTracker.RecentJumpCount, tracker.Snapshot().RecentJumps.Count);

        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:30:00Z","event":"Loadout","Ship":"mandalay","ShipID":2,"Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size5_class5"}]}""");
        Assert.Equal(RouteTracker.RecentJumpCount, tracker.Snapshot().RecentJumps.Count);
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:31:00Z","event":"Loadout","Ship":"mandalay","ShipID":2,"Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size5_class5"}]}""");
        Assert.Equal(RouteTracker.RecentJumpCount, tracker.Snapshot().RecentJumps.Count);
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:32:00Z","event":"Loadout","Ship":"mandalay","ShipID":2,"Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size5_class5","Engineering":{"BlueprintName":"FSD_LongRange","Level":5}}]}""");
        Assert.Empty(tracker.Snapshot().RecentJumps);
    }

    [Fact]
    public void FleetTracker_ReadsFuelCapacityAndModifiers()
    {
        var tracker = new FleetTracker();
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Loadout","Ship":"anacondamkii","ShipID":3,"ShipName":"Ironic","ShipIdent":"AA","UnladenMass":1250.5,"CargoCapacity":64,"MaxJumpRange":61.2,"FuelCapacity":{"Main":64,"Reserve":1.08},"Modules":[{"Slot":"FrameShiftDrive","Item":"Int_Hyperdrive_Overcharge_Size6_Class5","Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"Modifiers":[{"Label":"MaxFuelPerJump","Value":9.6,"OriginalValue":8},{"Label":"FSDOptimalMass","Value":2900}]}},{"Slot":"MainEngines","Item":"int_engine_size7_class5"}]}""");

        var ship = Assert.Single(tracker.Snapshot().Ships);
        Assert.Equal(64, ship.FuelCapacity);
        Assert.Equal(1.08, ship.ReserveCapacity);
        Assert.Equal(1250.5, ship.UnladenMass);
        Assert.Equal(64, ship.CargoCapacity);
        Assert.Equal(61.2, ship.MaxJumpRange);
        Assert.Equal("anacondamkii", ship.Type);
        var fsd = ship.Modules[0];
        Assert.Equal("int_hyperdrive_overcharge_size6_class5", fsd.Item);
        Assert.Equal(9.6, fsd.Modifiers["MaxFuelPerJump"]);
        Assert.Equal(2900, fsd.Modifiers["fsdoptimalmass"]);
        Assert.Empty(ship.Modules[1].Modifiers);
    }

    [Fact]
    public void FleetTracker_MarksAShipRedeemedFromABundle_AndTheValuelessModulesItCameWith()
    {
        var tracker = new FleetTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"ShipRedeemed","ShipType":"cobramkv","ShipType_Localised":"Cobra Mk V","NewShipID":4}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"Loadout","Ship":"cobramkv","ShipID":4,"ShipName":"","ShipIdent":"","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size4_class5","On":true,"Priority":0,"Health":1},{"Slot":"MainEngines","Item":"int_engine_size4_class5","On":true,"Priority":0,"Health":1,"Value":1610080}]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"Loadout","Ship":"anaconda","ShipID":1,"ShipName":"","ShipIdent":"","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size6_class5","On":true,"Priority":0,"Health":1}]}""");

        var ships = tracker.Snapshot().Ships.ToDictionary(ship => ship.ShipId);
        Assert.True(ships[4].IsPrebuilt);
        Assert.True(ships[4].Modules.Single(module => module.Slot == "FrameShiftDrive").IsPrebuilt);
        Assert.False(ships[4].Modules.Single(module => module.Slot == "MainEngines").IsPrebuilt);
        Assert.False(ships[1].IsPrebuilt);
        Assert.False(ships[1].Modules.Single().IsPrebuilt);

        // A later Loadout keeps the mark while the slot holds what came with the ship, value or no value, and a free module bought later is not mistaken for it.
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:03:00Z","event":"Loadout","Ship":"cobramkv","ShipID":4,"ShipName":"","ShipIdent":"","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size4_class5","On":true,"Priority":0,"Health":1},{"Slot":"Slot09_Size1","Item":"int_dockingcomputer_advanced","On":true,"Priority":0,"Health":1}]}""");
        var later = tracker.Snapshot().Ships.Single(ship => ship.ShipId == 4);
        Assert.True(later.Modules.Single(module => module.Slot == "FrameShiftDrive").IsPrebuilt);
        Assert.False(later.Modules.Single(module => module.Slot == "Slot09_Size1").IsPrebuilt);

        // Retrieving a module into the slot ends the mark, even when the game lists the newcomer without a value; a swap moves a mark with its module.
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:04:00Z","event":"ModuleRetrieve","MarketID":1,"Slot":"FrameShiftDrive","RetrievedItem":"$int_hyperdrive_size4_class5_name;","Ship":"cobramkv","ShipID":4,"Hot":false}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:04:01Z","event":"Loadout","Ship":"cobramkv","ShipID":4,"ShipName":"","ShipIdent":"","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size4_class5","On":true,"Priority":0,"Health":1},{"Slot":"MainEngines","Item":"int_engine_size4_class5","On":true,"Priority":0,"Health":1}]}""");
        var replaced = tracker.Snapshot().Ships.Single(ship => ship.ShipId == 4);
        Assert.False(replaced.Modules.Single(module => module.Slot == "FrameShiftDrive").IsPrebuilt);
        Assert.True(replaced.IsPrebuilt);

        var swapped = new FleetTracker();
        Feed(swapped,
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"ShipRedeemed","ShipType":"cobramkv","ShipType_Localised":"Cobra Mk V","NewShipID":4}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"Loadout","Ship":"cobramkv","ShipID":4,"ShipName":"","ShipIdent":"","Modules":[{"Slot":"Slot01_Size5","Item":"int_cargorack_size5_class1","On":true,"Priority":0,"Health":1},{"Slot":"Slot02_Size4","Item":"int_cargorack_size4_class1","On":true,"Priority":0,"Health":1,"Value":1000}]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"ModuleSwap","MarketID":1,"FromSlot":"Slot01_Size5","ToSlot":"Slot02_Size4","FromItem":"$int_cargorack_size5_class1_name;","ToItem":"$int_cargorack_size4_class1_name;","Ship":"cobramkv","ShipID":4}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:01Z","event":"Loadout","Ship":"cobramkv","ShipID":4,"ShipName":"","ShipIdent":"","Modules":[{"Slot":"Slot01_Size5","Item":"int_cargorack_size4_class1","On":true,"Priority":0,"Health":1,"Value":1000},{"Slot":"Slot02_Size4","Item":"int_cargorack_size5_class1","On":true,"Priority":0,"Health":1}]}""");
        var afterSwap = swapped.Snapshot().Ships.Single();
        Assert.True(afterSwap.Modules.Single(module => module.Slot == "Slot02_Size4").IsPrebuilt);
        Assert.False(afterSwap.Modules.Single(module => module.Slot == "Slot01_Size5").IsPrebuilt);
    }

    [Fact]
    public void UnlockTracker_RecordsEveryTechnologyBrokerPurchase()
    {
        var tracker = new UnlockTracker();
        Assert.Empty(tracker.Snapshot().Records);
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:00Z","event":"TechnologyBroker","BrokerType":"guardian","MarketID":1,"ItemsUnlocked":[{"Name":"Int_GuardianFSDBooster_Size5","Name_Localised":"Guardian FSD Booster"}],"Commodities":[{"Name":"hnshockmount","Count":8}],"Materials":[{"Name":"guardian_powercell","Category":"Manufactured","Count":21}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:01:00Z","event":"TechnologyBroker","BrokerType":"Human","MarketID":1,"ItemsUnlocked":[{"Name":"Hpt_PlasmaShockCannon_Fixed_Small"},{"Name":"Hpt_PlasmaShockCannon_Gimbal_Small"}],"Commodities":[],"Materials":[]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:02:00Z","event":"ModuleBuy","Slot":"Slot03_Size5","BuyItem":"$int_guardianpowerplant_size5_class1_name;","BuyItem_Localised":"Guardian Hybrid Power Plant","MarketID":1,"BuyPrice":1,"Ship":"krait_mkii","ShipID":7}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:03:00Z","event":"ModuleBuy","Slot":"Slot04_Size5","BuyItem":"$int_guardianfsdbooster_size5_name;","MarketID":1,"BuyPrice":1,"Ship":"krait_mkii","ShipID":7}""");

        var snapshot = tracker.Snapshot();
        Assert.Equal(4, snapshot.Records.Count);
        Assert.Equal("guardian", snapshot.Records[0].Broker);
        Assert.Equal(UnlockEvidence.Broker, snapshot.Records[0].Evidence);
        Assert.Equal(["int_guardianfsdbooster_size5"], snapshot.Records[0].Items);
        Assert.Equal("human", snapshot.Records[1].Broker);
        Assert.Equal(UnlockEvidence.Outfitting, snapshot.Records[2].Evidence);
        Assert.Equal(["int_guardianpowerplant_size5_class1"], snapshot.Records[2].Items);
        Assert.True(snapshot.HasItem(["int_guardianfsdbooster_"]));
        Assert.True(snapshot.HasItem(["hpt_plasmashockcannon_gimbal_small"]));
        Assert.False(snapshot.HasItem(["hpt_plasmashockcannon_turret_small"]));
        Assert.False(snapshot.HasItem([]));
        // The broker record wins over the later outfitting purchase of the same item.
        Assert.Equal(UnlockEvidence.Broker, snapshot.Find(["int_guardianfsdbooster_"])!.Evidence);
        Assert.Equal(UnlockEvidence.Outfitting, snapshot.Find(["int_guardianpowerplant_"])!.Evidence);

        tracker.Reset();
        Assert.Empty(tracker.Snapshot().Records);
    }

    [Fact]
    public void ActivityTracker_KeepsTheNewestFirst_AndDropsBeyondCapacity()
    {
        var tracker = new ActivityTracker();
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:00Z","event":"FSDTarget","Name":"Deciat","StarClass":"K","RemainingJumpsInRoute":2}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Koine A"}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:02Z","event":"SAASignalsFound","BodyName":"Koine 4 a","Signals":[{"Type":"$SAA_SignalType_Biological;","Type_Localised":"Biological","Count":2}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:03Z","event":"MaterialCollected","Category":"Raw","Name":"arsenic","Name_Localised":"Arsenic","Count":3}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:04Z","event":"SellOrganicData","BioData":[{"Value":1000,"Bonus":4000},{"Value":500,"Bonus":0}]}""");

        var items = tracker.Snapshot();
        Assert.Equal(4, items.Count);
        Assert.Equal(ActivityKind.OrganicSold, items[0].Kind);
        Assert.Equal("5500", items[0].Detail);
        Assert.Equal(("Arsenic", "3"), (items[1].Subject, items[1].Detail));
        Assert.Equal(("Koine 4 a", "2 Biological"), (items[2].Subject, items[2].Detail));
        Assert.Equal(("Deciat", "K"), (items[3].Subject, items[3].Detail));

        for (var index = 0; index < ActivityTracker.Capacity + 5; index++)
        {
            Feed(tracker, /*lang=json,strict*/ $$$"""{"timestamp":"2026-11-05T11:00:{{{index % 60:00}}}Z","event":"FSDJump","StarSystem":"System {{{index}}}","SystemAddress":{{{index + 10}}}}""");
        }

        Assert.Equal(ActivityTracker.Capacity, tracker.Snapshot().Count);
        tracker.Reset();
        Assert.Empty(tracker.Snapshot());
    }

    [Fact]
    public void CarriedDataTracker_FollowsWhatIsAboard_UntilSoldOrLost()
    {
        var tracker = new CarriedDataTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:00:00Z","event":"FSDJump","StarSystem":"Alpha","SystemAddress":1,"StarPos":[0,0,0]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:00:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Alpha","BodyID":0,"StarType":"K","StellarMass":0.8,"DistanceFromArrivalLS":0,"WasDiscovered":true,"WasMapped":false,"SystemAddress":1}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:00:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Alpha A Belt Cluster 1","BodyID":5,"DistanceFromArrivalLS":10,"SystemAddress":1}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:00:10Z","event":"Scan","ScanType":"Detailed","BodyName":"Alpha 1","BodyID":1,"PlanetClass":"High metal content body","TerraformState":"Terraformable","MassEM":0.5,"DistanceFromArrivalLS":300,"WasDiscovered":false,"WasMapped":false,"SystemAddress":1}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:05:00Z","event":"SAAScanComplete","BodyName":"Alpha 1","BodyID":1,"SystemAddress":1,"ProbesUsed":4,"EfficiencyTarget":6}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:10:00Z","event":"ScanOrganic","ScanType":"Log","Genus":"$Codex_Ent_Stratum_Genus_Name;","Genus_Localised":"Stratum","Species":"$Codex_Ent_Stratum_07_Name;","Species_Localised":"Stratum Tectonicas","SystemAddress":1,"Body":1,"WasLogged":false}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:12:00Z","event":"ScanOrganic","ScanType":"Analyse","Genus":"$Codex_Ent_Stratum_Genus_Name;","Genus_Localised":"Stratum","Species":"$Codex_Ent_Stratum_07_Name;","Species_Localised":"Stratum Tectonicas","SystemAddress":1,"Body":1,"WasLogged":false}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:12:00Z","event":"ScanOrganic","ScanType":"Analyse","Genus":"$Codex_Ent_Stratum_Genus_Name;","Genus_Localised":"Stratum","Species":"$Codex_Ent_Stratum_07_Name;","Species_Localised":"Stratum Tectonicas","SystemAddress":1,"Body":1,"WasLogged":false}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T11:00:00Z","event":"FSDJump","StarSystem":"Beta","SystemAddress":2,"StarPos":[10,0,0]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T11:00:05Z","event":"Scan","ScanType":"Detailed","BodyName":"Beta 1","BodyID":1,"PlanetClass":"Icy body","MassEM":0.1,"DistanceFromArrivalLS":900,"WasDiscovered":true,"WasMapped":true,"SystemAddress":2}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T11:10:00Z","event":"ScanOrganic","ScanType":"Analyse","Genus":"$Codex_Ent_Bacterial_Genus_Name;","Genus_Localised":"Bacterium","Species":"$Codex_Ent_Bacterial_01_Name;","Species_Localised":"Bacterium Aurasus","SystemAddress":2,"Body":1,"WasLogged":true}""");

        // Two systems, three bodies with the belt cluster left out, one mapped; two samples, the repeated analysis counted once.
        // A nav beacon's bodies are known but not yours to sell, so they do not count as data aboard.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-08T11:30:00Z","event":"Scan","ScanType":"NavBeaconDetail","BodyName":"Beta 2","BodyID":2,"PlanetClass":"Rocky body","DistanceFromArrivalLS":50,"WasDiscovered":true,"WasMapped":true,"SystemAddress":2}""");
        var aboard = tracker.Snapshot();
        Assert.Equal(["Alpha", "Beta"], aboard.Systems.Select(system => system.Name));
        Assert.Equal(3, aboard.BodyCount);
        Assert.Equal(1, aboard.MappedCount);
        var planet = aboard.Systems[0].Bodies.Single(body => body.Name == "Alpha 1");
        Assert.True(planet.Mapped);
        Assert.True(planet.Terraformable);
        Assert.False(planet.WasDiscovered);
        Assert.Equal(2, aboard.Samples.Count);
        Assert.Equal("Alpha 1", aboard.Samples[0].BodyName);
        Assert.Equal("Alpha", aboard.Samples[0].SystemName);
        Assert.False(aboard.Samples[0].WasLogged);
        Assert.True(aboard.Samples[1].WasLogged);

        // Selling one system keeps the other; selling organic data clears every sample.
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T12:00:00Z","event":"MultiSellExplorationData","Discovered":[{"SystemName":"Alpha","NumBodies":2}],"BaseValue":1000,"Bonus":500,"TotalEarnings":1500}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T12:00:01Z","event":"SellOrganicData","MarketID":1,"BioData":[{"Genus":"$Codex_Ent_Stratum_Genus_Name;","Species":"$Codex_Ent_Stratum_07_Name;","Value":19010800,"Bonus":76043200}]}""");
        aboard = tracker.Snapshot();
        Assert.Equal(["Beta"], aboard.Systems.Select(system => system.Name));
        Assert.Empty(aboard.Samples);

        // Death loses the rest.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-08T13:00:00Z","event":"Died"}""");
        Assert.True(tracker.Snapshot().IsEmpty);
    }

    [Fact]
    public void ExplorationTracker_ReadsFootfall_AndKnowsAPopulatedSystemCannotClaimIt()
    {
        var tracker = new ExplorationTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:00:00Z","event":"FSDJump","StarSystem":"Busy","SystemAddress":21,"StarPos":[0,0,0],"Population":120000}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:00:05Z","event":"Scan","ScanType":"Detailed","BodyName":"Busy 1","BodyID":1,"PlanetClass":"Rocky body","DistanceFromArrivalLS":300,"Landable":true,"WasDiscovered":true,"WasMapped":true,"WasFootfalled":false,"SystemAddress":21}""");
        var busy = tracker.Snapshot();
        Assert.True(busy.Populated);
        Assert.False(busy.Bodies.Single().WasFootfalled);
        Assert.False(busy.Bodies.Single().FromNavBeacon);

        // The nav beacon hands over the rest of the system, marked as the beacon's rather than the player's.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-08T10:01:00Z","event":"Scan","ScanType":"NavBeaconDetail","BodyName":"Busy 2","BodyID":2,"PlanetClass":"Icy body","DistanceFromArrivalLS":900,"WasDiscovered":true,"WasMapped":true,"WasFootfalled":false,"SystemAddress":21}""");
        var beacon = tracker.Snapshot().Bodies.Single(body => body.Name == "Busy 2");
        Assert.True(beacon.FromNavBeacon);
        Assert.Equal("Icy body", beacon.Class);

        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T11:00:00Z","event":"FSDJump","StarSystem":"Quiet","SystemAddress":22,"StarPos":[10,0,0],"Population":0}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T11:00:05Z","event":"Scan","ScanType":"Detailed","BodyName":"Quiet 1","BodyID":1,"PlanetClass":"Icy body","DistanceFromArrivalLS":900,"Landable":true,"WasDiscovered":true,"WasMapped":false,"WasFootfalled":true,"SystemAddress":22}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T11:00:06Z","event":"Scan","ScanType":"Detailed","BodyName":"Quiet 2","BodyID":2,"PlanetClass":"Icy body","DistanceFromArrivalLS":1200,"Landable":true,"WasDiscovered":false,"WasMapped":false,"WasFootfalled":false,"SystemAddress":22}""");
        var quiet = tracker.Snapshot();
        Assert.False(quiet.Populated);
        Assert.True(quiet.Bodies.Single(body => body.Name == "Quiet 1").WasFootfalled);
        Assert.False(quiet.Bodies.Single(body => body.Name == "Quiet 2").WasFootfalled);

        // Going back to the populated system brings its flag back with its bodies.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-08T12:00:00Z","event":"FSDJump","StarSystem":"Busy","SystemAddress":21,"StarPos":[0,0,0],"Population":120000}""");
        Assert.True(tracker.Snapshot().Populated);
    }

    [Fact]
    public void ExplorationTracker_BringsASystemsBodiesBack_OnAReturnVisit()
    {
        var tracker = new ExplorationTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-03T10:00:00Z","event":"FSDJump","StarSystem":"Alpha","SystemAddress":1,"StarPos":[0,0,0]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-03T10:00:05Z","event":"FSSDiscoveryScan","Progress":0.3,"BodyCount":4,"NonBodyCount":0,"SystemName":"Alpha","SystemAddress":1}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-03T10:01:00Z","event":"Scan","ScanType":"Detailed","BodyName":"Alpha 1","BodyID":1,"PlanetClass":"Icy body","DistanceFromArrivalLS":300,"SystemAddress":1}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-03T11:00:00Z","event":"FSDJump","StarSystem":"Beta","SystemAddress":2,"StarPos":[10,0,0]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-03T11:00:05Z","event":"Scan","ScanType":"Detailed","BodyName":"Beta 1","BodyID":1,"PlanetClass":"Rocky body","DistanceFromArrivalLS":100,"SystemAddress":2}""");
        Assert.Equal(["Beta 1"], tracker.Snapshot().Bodies.Select(body => body.Name));

        // Back in Alpha days later: the body scanned before is listed with its own scan time, under this visit's arrival time.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-08T09:00:00Z","event":"FSDJump","StarSystem":"Alpha","SystemAddress":1,"StarPos":[0,0,0]}""");
        var back = tracker.Snapshot();
        Assert.Equal(["Alpha 1"], back.Bodies.Select(body => body.Name));
        Assert.Equal(new DateTimeOffset(2026, 11, 3, 10, 1, 0, TimeSpan.Zero), back.Bodies[0].ScannedAt);
        Assert.Equal(new DateTimeOffset(2026, 11, 8, 9, 0, 0, TimeSpan.Zero), back.ArrivedAt);
        Assert.Equal(4, back.BodyCount);
        Assert.Equal(1, back.BodiesScanned);
        Assert.False(back.Bodies[0].FromCatalogue);
    }

    [Fact]
    public void ExplorationTracker_CountsEveryBodyAfterAFullHonk_AndOnlyStarsAndPlanetsOtherwise()
    {
        var tracker = new ExplorationTracker();
        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:47:26Z","event":"FSDJump","StarSystem":"Jackson's Lighthouse","SystemAddress":5532807773,"StarPos":[157,-27,-70]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:47:45Z","event":"FSSDiscoveryScan","Progress":1.0,"BodyCount":7,"NonBodyCount":54,"SystemName":"Jackson's Lighthouse","SystemAddress":5532807773}""");

        // Scanned on a visit before the history began: the game writes no Scan, not even the arrival star's.
        var revisited = tracker.Snapshot();
        Assert.Empty(revisited.Bodies);
        Assert.Equal(7, revisited.BodiesKnown);
        Assert.Equal(7, revisited.BodiesKnownEarlier);

        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:00Z","event":"FSDJump","StarSystem":"Fresh","SystemAddress":77,"StarPos":[158,-27,-70]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Fresh","BodyID":0,"StarType":"K","DistanceFromArrivalLS":0,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Fresh A Belt Cluster 1","BodyID":5,"DistanceFromArrivalLS":12,"SystemAddress":77}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:10Z","event":"FSSDiscoveryScan","Progress":0.639665,"BodyCount":8,"NonBodyCount":0,"SystemName":"Fresh","SystemAddress":77}""");
        // The belt cluster is recorded but is not in the game's body count; the weighted progress figure counts for nothing.
        var arrived = tracker.Snapshot();
        Assert.Equal(2, arrived.Bodies.Count);
        Assert.Equal(1, arrived.BodiesScanned);
        Assert.Equal(1, arrived.BodiesKnown);
        Assert.Equal(0, arrived.BodiesKnownEarlier);

        Feed(tracker,
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:00:20Z","event":"Scan","ScanType":"Detailed","BodyName":"Fresh 1","BodyID":1,"Parents":[{"Star":0}],"PlanetClass":"Icy body","DistanceFromArrivalLS":900,"SystemAddress":77}""");
        var scanning = tracker.Snapshot();
        Assert.Equal(3, scanning.Bodies.Count);
        Assert.Equal(2, scanning.BodiesKnown);
        Assert.Null(scanning.Bodies.Single(body => body.Name == "Fresh").ParentStarId);
        Assert.Equal(0, scanning.Bodies.Single(body => body.Name == "Fresh 1").ParentStarId);
        Assert.Equal(0, scanning.BodiesKnownEarlier);

        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-08T16:01:00Z","event":"FSSAllBodiesFound","SystemName":"Fresh","SystemAddress":77,"Count":8}""");
        Assert.Equal(8, tracker.Snapshot().BodiesKnown);
    }

    [Fact]
    public void ExplorationTracker_BuildsTheSystem_AndRemembersLandingsAndSales()
    {
        var tracker = new ExplorationTracker();
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-01T10:00:00Z","event":"FSDJump","StarSystem":"Old","SystemAddress":5,"StarPos":[0,0,0]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-01T10:00:01Z","event":"Touchdown","Body":"Old 2 a","BodyID":9,"SystemAddress":5,"PlayerControlled":true}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-01T10:00:02Z","event":"SellOrganicData","BioData":[{"Genus":"$Codex_Ent_Bacterial_Genus_Name;","Species":"$Codex_Ent_Bacterial_01_Name;","Species_Localised":"Bacterium Aurasus","Value":1000000,"Bonus":4000000}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:00Z","event":"FSDJump","StarSystem":"New","SystemAddress":6,"StarPos":[1,1,1]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:01Z","event":"FSSDiscoveryScan","Progress":0.5,"BodyCount":3,"NonBodyCount":1,"SystemName":"New","SystemAddress":6}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:02Z","event":"Scan","ScanType":"Detailed","BodyName":"New 1","BodyID":1,"PlanetClass":"High metal content body","DistanceFromArrivalLS":500,"Landable":true,"Atmosphere":"","Volcanism":"minor silicate vapour geysers volcanism","SurfaceGravity":9.80665,"SurfaceTemperature":400,"SurfacePressure":0,"TerraformState":"Terraformable","MassEM":0.9,"Rings":[{"Name":"New 1 A Ring"}],"Materials":[{"Name":"iron","Percent":22.1},{"Name":"nickel","Percent":16.7}],"WasDiscovered":true,"WasMapped":false,"SystemAddress":6}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:03Z","event":"FSSBodySignals","BodyName":"New 1","BodyID":1,"SystemAddress":6,"Signals":[{"Type":"$SAA_SignalType_Biological;","Count":2},{"Type":"$SAA_SignalType_Geological;","Count":4}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:04Z","event":"SAASignalsFound","BodyName":"New 1","BodyID":1,"SystemAddress":6,"Signals":[{"Type":"$SAA_SignalType_Biological;","Count":2}],"Genuses":[{"Genus":"$Codex_Ent_Bacterial_Genus_Name;","Genus_Localised":"Bacterium"},{"Genus":"$Codex_Ent_Stratum_Genus_Name;","Genus_Localised":"Stratum"}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:05Z","event":"SAAScanComplete","BodyName":"New 1","BodyID":1,"SystemAddress":6,"ProbesUsed":4,"EfficiencyTarget":6}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:06Z","event":"ApproachBody","StarSystem":"New","SystemAddress":6,"Body":"New 1","BodyID":1}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:07Z","event":"Touchdown","Body":"New 1","BodyID":1,"StarSystem":"New","SystemAddress":6,"PlayerControlled":true}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:08Z","event":"ScanOrganic","ScanType":"Log","Genus":"$Codex_Ent_Bacterial_Genus_Name;","Genus_Localised":"Bacterium","Species":"$Codex_Ent_Bacterial_01_Name;","Species_Localised":"Bacterium Aurasus","Variant":"$Codex_Ent_Bacterial_01_O_Name;","Variant_Localised":"Bacterium Aurasus - Turquoise","SystemAddress":6,"Body":1}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:09Z","event":"ScanOrganic","ScanType":"Sample","Genus":"$Codex_Ent_Bacterial_Genus_Name;","Species":"$Codex_Ent_Bacterial_01_Name;","Species_Localised":"Bacterium Aurasus","SystemAddress":6,"Body":1}""");

        var snapshot = tracker.Snapshot();
        Assert.Equal("New", snapshot.SystemName);
        Assert.Equal(3, snapshot.BodyCount);
        Assert.Equal(1, snapshot.NonBodyCount);
        // A partial honk is weighted, not a count, so only the body scanned this visit counts.
        Assert.Equal(1, snapshot.BodiesKnown);
        Assert.Equal(0, snapshot.BodiesKnownEarlier);
        Assert.Equal("New 1", snapshot.CurrentBody);
        var body = Assert.Single(snapshot.Bodies);
        Assert.Equal("High metal content body", body.Class);
        Assert.True(body.Landable);
        Assert.Equal(1.0, body.SurfaceGravityG!.Value, 3);
        Assert.True(body.Terraformable);
        Assert.True(body.HasRings);
        Assert.Equal(22.1, body.Materials["IRON"]);
        Assert.True(body.WasDiscovered);
        Assert.False(body.WasMapped);
        Assert.True(body.MappedByYou);
        Assert.True(body.LandedByYou);
        Assert.Equal(2, body.BiologicalSignals);
        Assert.Equal(4, body.GeologicalSignals);
        Assert.Equal(["Bacterium", "Stratum"], body.Genuses.Select(genus => genus.Name));
        var organic = Assert.Single(body.Organics);
        Assert.Equal("Bacterium Aurasus", organic.SpeciesName);
        Assert.Equal(2, organic.Stage);
        Assert.Equal("Bacterium Aurasus - Turquoise", organic.VariantName);
        Assert.Equal(1000000, snapshot.LearnedSpecies["$Codex_Ent_Bacterial_01_Name;"].Value);
        Assert.Equal(4000000, snapshot.LearnedSpecies["$Codex_Ent_Bacterial_01_Name;"].Bonus);

        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T11:00:00Z","event":"FSDJump","StarSystem":"Old","SystemAddress":5,"StarPos":[0,0,0]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T11:00:01Z","event":"Scan","ScanType":"AutoScan","BodyName":"Old 2 a","BodyID":9,"PlanetClass":"Icy body","DistanceFromArrivalLS":1200,"Landable":true,"WasDiscovered":true,"WasMapped":true,"SystemAddress":5}""");
        var back = tracker.Snapshot();
        Assert.Equal("Old", back.SystemName);
        Assert.Null(back.BodyCount);
        Assert.True(Assert.Single(back.Bodies).LandedByYou);
    }

    [Theory]
    [InlineData("$int_cargorack_size3_class1_name;", "int_cargorack_size3_class1")]
    [InlineData("Int_CargoRack_Size3_Class1", "int_cargorack_size3_class1")]
    [InlineData(" $Hpt_HeatSinkLauncher_Turret_Tiny_NAME; ", "hpt_heatsinklauncher_turret_tiny")]
    [InlineData("$_name;", null)]
    public void NormalizeSymbol_StripsTheWrapperAndLowercases(string raw, string? expected)
        => Assert.Equal(expected, JsonRead.NormalizeSymbol(raw));

    [Fact]
    public void CraftTracker_RecordsEveryRoll_WithGradeProgressAndTheEffect()
    {
        var tracker = new CraftTracker();
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-07T17:02:00Z","event":"EngineerCraft","Slot":"PowerPlant","Module":"$int_powerplant_size8_class5_name;","Ingredients":[{"Name":"sulphur","Count":1}],"Engineer":"Felicity Farseer","EngineerID":300100,"BlueprintID":128673765,"BlueprintName":"PowerPlant_Boosted","Level":1,"Quality":1.0,"Modifiers":[]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-07T17:02:09Z","event":"EngineerCraft","Slot":"PowerPlant","Module":"int_powerplant_size8_class5","ApplyExperimentalEffect":"special_powerplant_highcharge","Ingredients":[],"Engineer":"Felicity Farseer","BlueprintName":"PowerPlant_Boosted","Level":1,"Quality":1.0,"ExperimentalEffect":"special_powerplant_highcharge","ExperimentalEffect_Localised":"Monstered","Modifiers":[]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-07T17:03:00Z","event":"EngineerCraft","Slot":"Radar","Module":"int_sensors_size8_class5","Ingredients":[],"Engineer":"Felicity Farseer","BlueprintName":"Sensor_WideAngle","Level":2,"Quality":0.5,"Modifiers":[]}""");

        var crafts = tracker.Snapshot().Records;
        Assert.Equal(3, crafts.Count);
        Assert.Equal("int_powerplant_size8_class5", crafts[0].Module);
        Assert.Equal("PowerPlant", crafts[0].Slot);
        Assert.Equal("PowerPlant_Boosted", crafts[0].Blueprint);
        Assert.Equal(1, crafts[0].Level);
        Assert.Equal(1.0, crafts[0].Quality);
        Assert.Null(crafts[0].Experimental);
        Assert.Equal("special_powerplant_highcharge", crafts[1].Experimental);
        Assert.Equal(0.5, crafts[2].Quality);
        Assert.Single(tracker.Snapshot().After(new DateTimeOffset(2026, 11, 7, 17, 2, 9, TimeSpan.Zero)));

        tracker.Reset();
        Assert.Empty(tracker.Snapshot().Records);
    }

    [Fact]
    public void FleetTracker_AppliesEngineerCraftToTheFittedModule()
    {
        var tracker = new FleetTracker();
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-07T16:34:41Z","event":"Loadout","Ship":"anacondamkii","ShipID":23,"ShipName":"Borrowed Time","ShipIdent":"AA-02A","Modules":[{"Slot":"PowerPlant","Item":"int_powerplant_size8_class5","On":true,"Priority":1,"Value":116739873},{"Slot":"Radar","Item":"int_sensors_size8_class5","On":true,"Priority":0,"Value":1}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-07T17:02:00Z","event":"EngineerCraft","Slot":"PowerPlant","Module":"int_powerplant_size8_class5","Ingredients":[{"Name":"sulphur","Count":1}],"Engineer":"Felicity Farseer","EngineerID":300100,"BlueprintID":128673765,"BlueprintName":"PowerPlant_Boosted","Level":1,"Quality":1.0,"Modifiers":[{"Label":"Integrity","Value":156.75,"OriginalValue":165.0,"LessIsGood":0},{"Label":"PowerCapacity","Value":40.32,"OriginalValue":36.0,"LessIsGood":0}]}""");

        // The grade lands on the plant without a Loadout, and the other module is untouched.
        var ship = Assert.Single(tracker.Snapshot().Ships);
        Assert.Equal(2, ship.Modules.Count);
        var plant = ship.Modules.Single(module => module.Slot == "PowerPlant");
        Assert.Equal("int_powerplant_size8_class5", plant.Item);
        Assert.Equal("PowerPlant_Boosted", plant.Engineering!.Blueprint);
        Assert.Equal(1, plant.Engineering.Level);
        Assert.Equal(1.0, plant.Engineering.Quality);
        Assert.Null(plant.Engineering.Experimental);
        Assert.True(plant.Engineering.ExperimentalKnown);
        Assert.Equal(40.32, plant.Modifiers["PowerCapacity"]);
        Assert.Null(ship.Modules.Single(module => module.Slot == "Radar").Engineering);
        Assert.Equal(new DateTimeOffset(2026, 11, 7, 17, 2, 0, TimeSpan.Zero), ship.FitAsOf);

        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-07T17:02:09Z","event":"EngineerCraft","Slot":"PowerPlant","Module":"int_powerplant_size8_class5","ApplyExperimentalEffect":"special_powerplant_highcharge","Ingredients":[],"Engineer":"Felicity Farseer","EngineerID":300100,"BlueprintID":128673765,"BlueprintName":"PowerPlant_Boosted","Level":1,"Quality":1.0,"ExperimentalEffect":"special_powerplant_highcharge","ExperimentalEffect_Localised":"Monstered","Modifiers":[{"Label":"Mass","Value":88.0,"OriginalValue":80.0,"LessIsGood":1}]}""");

        // The experimental follows, and the module keeps the one engineering block, not two.
        plant = Assert.Single(tracker.Snapshot().Ships).Modules.Single(module => module.Slot == "PowerPlant");
        Assert.Equal("special_powerplant_highcharge", plant.Engineering!.Experimental);
        Assert.Equal(88.0, plant.Modifiers["Mass"]);
        Assert.False(plant.Modifiers.ContainsKey("PowerCapacity"));

        // Before a ship's first Loadout there is no fit to amend.
        var fresh = new FleetTracker();
        Feed(fresh,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-07T16:33:55Z","event":"LoadGame","Commander":"Casey","Ship":"AnacondaMkII","Ship_Localised":"Anaconda Mk II","ShipID":23}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-07T17:02:00Z","event":"EngineerCraft","Slot":"PowerPlant","Module":"int_powerplant_size8_class5","Engineer":"Felicity Farseer","BlueprintName":"PowerPlant_Boosted","Level":1}""");
        Assert.Empty(Assert.Single(fresh.Snapshot().Ships).Modules);
    }

    [Fact]
    public void FleetTracker_KeepsStorageAndShipsCurrent_BetweenTheGamesSnapshots()
    {
        var tracker = new FleetTracker();
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:00Z","event":"LoadGame","Commander":"Casey","Ship":"Anaconda","Ship_Localised":"Anaconda","ShipID":1,"ShipName":"Borrowed Time","ShipIdent":"AA-02A"}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:01Z","event":"Location","StarSystem":"Deciat","SystemAddress":3,"Docked":true,"StationName":"Farseer Inc"}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:02Z","event":"Loadout","Ship":"anaconda","ShipID":1,"ShipName":"Borrowed Time","ShipIdent":"AA-02A","Modules":[{"Slot":"FrameShiftDrive","Item":"int_hyperdrive_size6_class5","On":true,"Priority":0,"Value":1,"Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"ExperimentalEffect":"special_fsd_heavy"}},{"Slot":"PowerPlant","Item":"int_powerplant_size7_class5","On":true,"Priority":1,"Value":1},{"Slot":"Slot01_Size7","Item":"int_cargorack_size7_class1","On":true,"Priority":1,"Value":1}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:03Z","event":"StoredModules","MarketID":1,"StationName":"Farseer Inc","StarSystem":"Deciat","Items":[{"Name":"int_fuelscoop_size7_class5","StorageSlot":1,"StarSystem":"Sol","MarketID":9,"Hot":false}]}""");

        // Buying a drive puts the old one in storage with the engineering the ship knew, experimental included; the game writes no StoredModules until the storage screen opens.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:00Z","event":"ModuleBuy","Slot":"FrameShiftDrive","StoredItem":"$int_hyperdrive_size6_class5_name;","StoredItem_Localised":"Frame Shift Drive","BuyItem":"$int_hyperdrive_overcharge_size6_class5_name;","BuyItem_Localised":"Frame Shift Drive (SCO)","MarketID":1,"BuyPrice":1,"Ship":"anaconda","ShipID":1}""");
        var stored = tracker.Snapshot().StoredModules;
        Assert.Equal(2, stored.Count);
        var oldDrive = stored.Single(module => module.Item == "int_hyperdrive_size6_class5");
        Assert.Equal("Deciat", oldDrive.System);
        Assert.Equal(1, oldDrive.MarketId);
        Assert.Equal("special_fsd_heavy", oldDrive.Engineering!.Experimental);
        Assert.True(oldDrive.Engineering.ExperimentalKnown);

        // The next storage snapshot says blueprint and grade but not the effect; the effect learned as the drive left the ship is kept.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:30Z","event":"StoredModules","MarketID":1,"StationName":"Farseer Inc","StarSystem":"Deciat","Items":[{"Name":"int_fuelscoop_size7_class5","StorageSlot":1,"StarSystem":"Sol","MarketID":9,"Hot":false},{"Name":"$int_hyperdrive_size6_class5_name;","Name_Localised":"Frame Shift Drive","StorageSlot":2,"StarSystem":"Deciat","MarketID":1,"EngineerModifications":"FSD_LongRange","Level":5,"Quality":1,"Hot":false}]}""");
        var recalled = tracker.Snapshot().StoredModules.Single(module => module.Item == "int_hyperdrive_size6_class5");
        Assert.Equal("special_fsd_heavy", recalled.Engineering!.Experimental);
        Assert.True(recalled.Engineering.ExperimentalKnown);

        // A stored module's grade progress comes from the snapshot's Quality figure.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:01:40Z","event":"StoredModules","MarketID":1,"StationName":"Farseer Inc","StarSystem":"Deciat","Items":[{"Name":"int_fuelscoop_size7_class5","StorageSlot":1,"StarSystem":"Sol","MarketID":9,"Hot":false},{"Name":"$int_hyperdrive_size6_class5_name;","Name_Localised":"Frame Shift Drive","StorageSlot":2,"StarSystem":"Deciat","MarketID":1,"EngineerModifications":"FSD_LongRange","Level":5,"Quality":1,"Hot":false},{"Name":"$int_shieldgenerator_size8_class3_fast_name;","Name_Localised":"Bi-Weave Shield","StorageSlot":3,"StarSystem":"Deciat","MarketID":1,"EngineerModifications":"ShieldGenerator_Reinforced","Level":3,"Quality":0.25,"Hot":false}]}""");
        var biweave = tracker.Snapshot().StoredModules.Single(module => module.Item == "int_shieldgenerator_size8_class3_fast");
        Assert.Equal(0.25, biweave.Engineering!.Quality);

        // Retrieving a module into a filled slot swaps the fitted one out into storage.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:00Z","event":"ModuleRetrieve","MarketID":1,"Slot":"PowerPlant","RetrievedItem":"$int_powerplant_size7_class3_name;","RetrievedItem_Localised":"Power Plant","Ship":"anaconda","ShipID":1,"Hot":false,"SwapOutItem":"$int_powerplant_size7_class5_name;","SwapOutItem_Localised":"Power Plant"}""");
        Assert.Contains(tracker.Snapshot().StoredModules, module => module.Item == "int_powerplant_size7_class5" && module.Engineering is null);

        // A module bought straight into storage, never fitted, is in storage from that moment, stock, at this market.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:02:30Z","event":"ModuleBuyAndStore","BuyItem":"$int_dronecontrol_collection_size7_class5_name;","BuyItem_Localised":"Collector","MarketID":1,"BuyPrice":6998400,"Ship":"anaconda","ShipID":1}""");
        var collector = tracker.Snapshot().StoredModules.Single(module => module.Item == "int_dronecontrol_collection_size7_class5");
        Assert.Equal("Deciat", collector.System);
        Assert.Equal(1, collector.MarketId);
        Assert.Null(collector.Engineering);
        Assert.False(collector.InTransit);

        // Storing everything at once stores every item listed.
        Feed(tracker, /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:03:00Z","event":"MassModuleStore","MarketID":1,"Ship":"anaconda","ShipID":1,"Items":[{"Slot":"Slot01_Size7","Name":"$int_cargorack_size7_class1_name;","Name_Localised":"Cargo Rack","Hot":false}]}""");
        Assert.Contains(tracker.Snapshot().StoredModules, module => module.Item == "int_cargorack_size7_class1" && module.System == "Deciat");

        // Sending for a module from another station puts it in transit to this system; a ship sent for likewise.
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:04:00Z","event":"FetchRemoteModule","StorageSlot":1,"StoredItem":"$int_fuelscoop_size7_class5_name;","StoredItem_Localised":"Fuel Scoop","ServerId":1,"TransferCost":100,"TransferTime":60,"Ship":"anaconda","ShipID":1}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:04:01Z","event":"ShipyardTransfer","ShipType":"cobramkv","ShipType_Localised":"Cobra Mk V","ShipID":9,"System":"Sol","ShipMarketID":9,"Distance":100,"TransferPrice":1000,"TransferTime":600,"MarketID":1}""");
        var snapshot = tracker.Snapshot();
        var scoop = snapshot.StoredModules.Single(module => module.Item == "int_fuelscoop_size7_class5");
        Assert.True(scoop.InTransit);
        Assert.Equal("Deciat", scoop.System);
        var cobra = snapshot.Ships.Single(ship => ship.ShipId == 9);
        Assert.True(cobra.InTransit);
        Assert.Equal("Deciat", cobra.System);
        Assert.Equal("Farseer Inc", cobra.Station);
    }

    [Fact]
    public void FleetTracker_FollowsShipsAndStoredModules()
    {
        var tracker = new FleetTracker();
        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:00Z","event":"LoadGame","Commander":"Casey","Ship":"Anaconda","Ship_Localised":"Anaconda","ShipID":1,"ShipName":"Borrowed Time","ShipIdent":"AA-02A"}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:01Z","event":"Location","StarSystem":"Deciat","SystemAddress":3,"Docked":true,"StationName":"Farseer Inc"}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:02Z","event":"Loadout","Ship":"anaconda","ShipID":1,"ShipName":"Borrowed Time","ShipIdent":"AA-02A","Modules":[{"Slot":"FrameShiftDrive","Item":"Int_Hyperdrive_Size6_Class5","On":true,"Priority":0,"Engineering":{"BlueprintName":"FSD_LongRange","Level":5,"ExperimentalEffect":"special_fsd_heavy"}}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:03Z","event":"StoredShips","StationName":"Farseer Inc","MarketID":1,"StarSystem":"Deciat","ShipsHere":[{"ShipID":2,"ShipType":"mandalay","ShipType_Localised":"Mandalay","Name":"Ironic","Value":1000,"Hot":false}],"ShipsRemote":[{"ShipID":5,"ShipType":"cobramkiii","ShipType_Localised":"Cobra Mk III","Value":100,"Hot":false,"StarSystem":"Sol","ShipMarketID":9,"TransferPrice":10,"TransferTime":100}]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:04Z","event":"StoredModules","MarketID":1,"StationName":"Farseer Inc","StarSystem":"Deciat","Items":[{"Name":"$int_powerplant_size5_class5_name;","Name_Localised":"Power Plant","StorageSlot":1,"StarSystem":"Deciat","MarketID":1,"EngineerModifications":"PowerPlant_Boosted","Level":3,"Quality":1,"Hot":false},{"Name":"hpt_shieldbooster_size0_class5","StorageSlot":2,"StarSystem":"Sol","MarketID":2,"Hot":true}]}""");

        var snapshot = tracker.Snapshot();
        Assert.Equal(1, snapshot.CurrentShipId);
        Assert.Equal("Deciat", snapshot.CurrentSystem);
        Assert.Equal("Farseer Inc", snapshot.CurrentStation);
        Assert.Equal(3, snapshot.Ships.Count);
        var current = snapshot.Ships[0];
        Assert.True(current.IsCurrent);
        Assert.Equal("anaconda", current.Type);
        Assert.Equal("Deciat", current.System);
        var fsd = Assert.Single(current.Modules);
        Assert.Equal("int_hyperdrive_size6_class5", fsd.Item);
        Assert.Equal("special_fsd_heavy", fsd.Engineering!.Experimental);
        Assert.True(fsd.Engineering.ExperimentalKnown);
        var mandalay = snapshot.Ships.Single(ship => ship.Type == "mandalay");
        Assert.Equal("Farseer Inc", mandalay.Station);
        Assert.Equal("Cobra Mk III", snapshot.Ships.Single(ship => ship.Type == "cobramkiii").TypeName);
        Assert.Equal(2, snapshot.StoredModules.Count);
        Assert.Equal("int_powerplant_size5_class5", snapshot.StoredModules[0].Item);
        Assert.Equal("Power Plant", snapshot.StoredModules[0].Name);
        Assert.Equal("PowerPlant_Boosted", snapshot.StoredModules[0].Engineering!.Blueprint);
        Assert.False(snapshot.StoredModules[0].Engineering!.ExperimentalKnown);
        Assert.True(snapshot.StoredModules[1].Hot);

        Feed(tracker,
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:01:00Z","event":"ShipyardSwap","ShipType":"mandalay","ShipID":2,"StoreOldShip":"anaconda","StoreShipID":1}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:01:01Z","event":"ModuleRetrieve","Slot":"PowerPlant","RetrievedItem":"$int_powerplant_size5_class5_name;","RetrievedItem_Localised":"Power Plant","MarketID":1}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:01:02Z","event":"ModuleStore","Slot":"TinyHardpoint1","StoredItem":"$Hpt_ChaffLauncher_Tiny_Name;","StoredItem_Localised":"Chaff Launcher","MarketID":1}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:01:03Z","event":"SetUserShipName","Ship":"mandalay","ShipID":2,"UserShipName":"Renamed","UserShipId":"RE-01"}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:01:04Z","event":"ShipyardSell","ShipType":"cobramkiii","SellShipID":5,"ShipPrice":100}""");

        var after = tracker.Snapshot();
        Assert.Equal(2, after.CurrentShipId);
        Assert.Equal(2, after.Ships.Count);
        Assert.Equal("Renamed", after.Ships[0].Name);
        var stored = after.Ships.Single(ship => ship.ShipId == 1);
        Assert.False(stored.IsCurrent);
        Assert.Equal("Farseer Inc", stored.Station);
        Assert.Equal(["hpt_shieldbooster_size0_class5", "hpt_chafflauncher_tiny"], after.StoredModules.Select(module => module.Item));
    }
}
