using Katey.Core.Exploration;

namespace Katey.Core.Tests;

/// <summary>Checks the drive facts, the fitted fuel model and the route projection.</summary>
public sealed class FuelModelTests
{
    [Fact]
    public void FrameShiftDrives_ReadSymbolsAndTables()
    {
        Assert.Equal((5, 5), FrameShiftDrives.Parse("int_hyperdrive_size5_class5"));
        Assert.Equal((6, 5), FrameShiftDrives.Parse("INT_HYPERDRIVE_OVERCHARGE_SIZE6_CLASS5"));
        Assert.Equal((8, 5), FrameShiftDrives.Parse("int_hyperdrive_overcharge_size8_class5_overchargebooster_mkii"));
        Assert.Null(FrameShiftDrives.Parse("int_engine_size5_class5"));
        // The table: 30 standard drives, 35 SCO drives and the size 8 Mk II, each with its own figures.
        Assert.Equal(66, FrameShiftDrives.Known.Count);
        Assert.Equal(2.30, FrameShiftDrives.FuelPowerOf("int_hyperdrive_size4_class1"));
        Assert.Equal(2.45, FrameShiftDrives.FuelPowerOf("int_hyperdrive_size5_class5"));
        Assert.Equal(0.012, FrameShiftDrives.FuelMultiplierOf("int_hyperdrive_size5_class5"));
        Assert.Equal(0.008, FrameShiftDrives.FuelMultiplierOf("int_hyperdrive_size5_class3"));
        Assert.Equal(1050, FrameShiftDrives.OptimalMassOf("int_hyperdrive_size5_class5"));
        Assert.Equal(5.0, FrameShiftDrives.MaxFuelPerJumpOf("INT_HYPERDRIVE_SIZE5_CLASS5"));
        Assert.Equal(0.6, FrameShiftDrives.MaxFuelPerJumpOf("int_hyperdrive_size2_class1"));
        // SCO drives differ from the standard ones of the same size and rating.
        Assert.Equal(2000, FrameShiftDrives.OptimalMassOf("int_hyperdrive_overcharge_size6_class5"));
        Assert.Equal(8.3, FrameShiftDrives.MaxFuelPerJumpOf("int_hyperdrive_overcharge_size6_class5"));
        Assert.Equal(0.013, FrameShiftDrives.FuelMultiplierOf("int_hyperdrive_overcharge_size6_class5"));
        Assert.Equal(2.9, FrameShiftDrives.FuelPowerOf("int_hyperdrive_overcharge_size8_class1"));
        Assert.Equal(2800, FrameShiftDrives.OptimalMassOf("int_hyperdrive_overcharge_size8_class1"));
        Assert.Equal(20.7, FrameShiftDrives.MaxFuelPerJumpOf("int_hyperdrive_overcharge_size8_class5"));
        Assert.Equal(4, FrameShiftDrives.SuperchargeMultiplier("int_hyperdrive_overcharge_size6_class5"));
        // The size 8 Mk II: the game's 6.8 t cap, its own curve, and a six-times neutron boost.
        var mkii = FrameShiftDrives.Figures("int_hyperdrive_overcharge_size8_class5_overchargebooster_mkii")!;
        Assert.Equal(6.8, mkii.MaxFuelPerJump);
        Assert.Equal(2.5025, mkii.FuelPower);
        Assert.Equal(0.011, mkii.FuelMultiplier);
        Assert.Equal(4670, mkii.OptimalMass);
        Assert.Equal(6, mkii.SuperchargeMultiplier);
        Assert.Null(FrameShiftDrives.Figures("int_engine_size5_class5"));
        Assert.Throws<ArgumentException>(() => FrameShiftDrives.FuelPowerOf("int_engine_size5_class5"));
        Assert.Equal(10.5, FrameShiftDrives.GuardianBoosterLy("int_guardianfsdbooster_size5"));
        Assert.Equal(0, FrameShiftDrives.GuardianBoosterLy("int_cargorack_size5_class1"));
        Assert.Equal(4.0, FrameShiftDrives.BoostFrom(StarKind.Neutron));
        Assert.Equal(1.5, FrameShiftDrives.BoostFrom(StarKind.WhiteDwarf));
        Assert.Equal(1.0, FrameShiftDrives.BoostFrom(StarKind.Scoopable));
    }

    [Fact]
    public void Calibrate_FitsTheMedianScale_AndFuelForHonoursBoostAndCap()
    {
        var power = FrameShiftDrives.FuelPowerOf("int_hyperdrive_size5_class5");
        var model = JumpFuelModel.Calibrate(
            [new(49.028, 14.9286, 1), new(20, 1.4, 1), new(0, 1, 1), new(10, -1, 1)],
            power,
            maxFuelPerJump: 5.0,
            boosterLy: 0);

        Assert.NotNull(model);
        Assert.Equal(5.0, model.FuelFor(49.028, 1));
        Assert.True(model.FuelFor(20, 1) < model.FuelFor(30, 1));
        Assert.Equal(model.FuelFor(10, 1), model.FuelFor(40, 4), 1e-9);
        Assert.Null(JumpFuelModel.Calibrate([new(0, 0, 1)], power, 5.0, 0));

        // The booster stretches every jump by the same factor: the drive's range with it over the range without.
        var boosted = new JumpFuelModel(model.K, power, 5.0, BoosterLy: 10.5);
        var stretch = (boosted.DriveRangeLy + 10.5) / boosted.DriveRangeLy;
        Assert.Equal(model.FuelFor(20, 1), boosted.FuelFor(20 * stretch, 1), 1e-9);
        Assert.Equal(5.0, boosted.FuelFor(boosted.DriveRangeLy + 10.5, 1), 1e-9);
        Assert.True(boosted.FuelFor(5, 1) > 0);
    }

    [Fact]
    public void FuelModel_FromAProfile_IsTheGamesEquationAtTheMassAboard()
    {
        // A Mandalay-like profile: size 5 class A drive, 1692.6 t optimal mass, 401 t base mass, 32 t tank.
        var profile = new Routing.JumpProfile(2.45, 0.012, 1692.6, 5.2, 401, 32, 0.5, 0, 4);
        var model = profile.FuelModel(fuelAboard: 32, cargo: 8);

        Assert.Equal(2.45, model.Power);
        Assert.Equal(5.2, model.MaxFuelPerJump);
        Assert.Equal(0, model.BoosterLy);
        Assert.Equal(0.012 * Math.Pow(441 / 1692.6, 2.45), model.K, 1e-12);

        // The full-tank range from the profile is exactly the jump that burns the drive's maximum.
        Assert.Equal(5.2, model.FuelFor(profile.Range(8), 1), 1e-9);
        Assert.True(model.FuelFor(profile.Range(8) / 2, 1) < 5.2 / 4);

        // Negative figures count as nothing aboard.
        Assert.Equal(profile.FuelModel(0, 0).K, profile.FuelModel(-5, -1).K);

        // Along a route the planner asks for the model before each jump: the second of two equal legs costs less, since the first burn lightened the ship.
        var legs = new List<FuelLeg> { new(20, 1, Scoopable: false), new(20, 1, Scoopable: false) };
        var followed = FuelPlanner.Project(fuel => profile.FuelModel(fuel, 8), fuelNow: 32, capacity: 32, legs);
        Assert.True(followed.Stops[1].Cost < followed.Stops[0].Cost);
        Assert.Equal(32 - followed.Stops[0].Cost, followed.Stops[0].FuelOnArrival!.Value, 1e-9);
        var fixedModel = FuelPlanner.Project(model, fuelNow: 32, capacity: 32, legs);
        Assert.Equal(fixedModel.Stops[0].Cost, fixedModel.Stops[1].Cost);
        Assert.Equal(followed.Stops[0].Cost, fixedModel.Stops[0].Cost, 1e-9);
    }

    [Fact]
    public void Project_CountsJumpsWithoutScooping_NamesTheLastScoop_AndRefillsAtScoopableStars()
    {
        var model = new JumpFuelModel(K: 0.01, Power: 2.0, MaxFuelPerJump: 5.0, BoosterLy: 0);
        // Each 20 ly leg burns 0.01 × 400 = 4 t.
        var legs = new List<FuelLeg>
        {
            new(20, 1, Scoopable: false),
            new(20, 1, Scoopable: true),
            new(20, 1, Scoopable: false),
            new(20, 1, Scoopable: false),
            new(20, 1, Scoopable: false),
        };

        var projection = FuelPlanner.Project(model, fuelNow: 10, capacity: 16, legs);

        Assert.Equal(2, projection.JumpsWithoutScooping);
        Assert.Equal(1, projection.MustScoopBy);
        Assert.Equal(6, projection.Stops[0].FuelOnArrival);
        Assert.Equal(4, projection.Stops[0].Cost);
        Assert.True(projection.Stops[1].ScoopHere);
        Assert.Equal(2, projection.Stops[1].FuelOnArrival);
        Assert.Equal(12, projection.Stops[2].FuelOnArrival);
        Assert.Equal(8, projection.Stops[3].FuelOnArrival);
        Assert.Equal(4, projection.Stops[4].FuelOnArrival);
        Assert.False(projection.RunsDry);

        var dry = FuelPlanner.Project(model, fuelNow: 3, capacity: 16, legs);
        Assert.Equal(0, dry.JumpsWithoutScooping);
        Assert.Null(dry.MustScoopBy);
        Assert.True(dry.RunsDry);
        Assert.All(dry.Stops, stop => Assert.False(stop.Reachable));
    }
}
