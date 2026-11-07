using System.Text.Json;
using Katey.Core.Routing;

namespace Katey.Core.Tests;

/// <summary>Checks the jump profile, the plot request's form, and the reading of Spansh's replies.</summary>
public sealed class RoutingTests
{
    // The numbers Spansh's own plotter echoed back for a stock 5A drive on a light ship.
    private static readonly JumpProfile Stock5A = new(FuelPower: 2.45, FuelMultiplier: 0.012, OptimalMass: 1470, MaxFuelPerJump: 5, BaseMass: 400.63, TankSize: 32, InternalTankSize: 0.63, RangeBoost: 0, SuperchargeMultiplier: 4);

    private const string DoneReply = /*lang=json,strict*/ """
        {"job":"j-1","status":"ok","state":"completed","result":{"refuel_every_scoopable":true,"jumps":[
          {"name":"Sol","id64":10477373803,"x":0,"y":0,"z":0,"distance":0,"distance_to_destination":383.3,"fuel_used":0,"fuel_in_tank":32,"has_neutron":false,"is_scoopable":false,"must_refuel":false,"must_inject":0},
          {"name":"Tascheter Sector MN-T b3-6","id64":13864825529753,"x":-11.875,"y":-21.96875,"z":-29.03125,"distance":38.29,"distance_to_destination":346.33,"fuel_used":4.52,"fuel_in_tank":32,"has_neutron":false,"is_scoopable":true,"must_refuel":true,"must_inject":0},
          {"name":"Maia","id64":8216113749,"x":-81.78,"y":-149.44,"z":-343.38,"distance":136.69,"distance_to_destination":0,"fuel_used":3.42,"fuel_in_tank":28.58,"has_neutron":true,"is_scoopable":false,"must_refuel":false,"must_inject":0}
        ]}}
        """;

    [Fact]
    public void JumpProfile_RangeFollowsTheGamesEquation()
    {
        Assert.Equal(39.86, Stock5A.Range(), 0.01);
        Assert.True(Stock5A.Range(cargo: 50) < Stock5A.Range());
        var boosted = Stock5A with { RangeBoost = 10.5 };
        Assert.Equal(Stock5A.Range() + 10.5, boosted.Range(), 1e-9);
    }

    [Fact]
    public void JumpProfile_Build_CalibratesTheFuelCapToTheGamesRange()
    {
        // A Caspian Explorer's size 8 Mk II drive, Long Range G5, with a size 5 Guardian booster.
        var modules = new[]
        {
            new ProfileModule("int_hyperdrive_overcharge_size8_class5_overchargebooster_mkii", new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["FSDOptimalMass"] = 7528.04 }),
            new ProfileModule("int_guardianfsdbooster_size5", ProfileModule.None),
        };
        // The Mk II has its own row in the table: a 6.8 t cap, which the game states when a jump exceeds it, and its own curve.
        var fromTable = JumpProfile.Build(modules, 1299.9, 128, 1.14)!;
        Assert.Equal(6.8, fromTable.MaxFuelPerJump);
        Assert.Equal(2.5025, fromTable.FuelPower);
        Assert.Equal(0.011, fromTable.FuelMultiplier);
        Assert.Equal(6, fromTable.SuperchargeMultiplier);
        Assert.Equal(85.6, fromTable.MaxRange(), 0.3);
        Assert.Equal(79.3, fromTable.Range(), 0.3);

        // The game quotes 85.634 ly with one jump's fuel aboard; the cap solved from it lands on the same 6.8 t.
        var calibrated = JumpProfile.Build(modules, 1299.9, 128, 1.14, maxJumpRange: 85.634)!;
        Assert.Equal(85.634, calibrated.MaxRange(), 1e-6);
        Assert.Equal(6.8, calibrated.MaxFuelPerJump, 0.05);
        Assert.Equal(fromTable.OptimalMass, calibrated.OptimalMass);

        // The neutron ceiling is six times the range at the fuel aboard: 473 ly fit under it, 495 did not.
        var atJacksons = fromTable.FuelModel(125.68, 4);
        Assert.True(atJacksons.FuelFor(473.07, 6) < 6.8);
        Assert.InRange(6 * (atJacksons.DriveRangeLy + atJacksons.BoosterLy), 473.07, 495.12);

        // A range the booster alone would exceed is nonsense and is ignored.
        Assert.Equal(6.8, JumpProfile.Build(modules, 1299.9, 128, 1.14, maxJumpRange: 5)!.MaxFuelPerJump);
    }

    [Fact]
    public void PlotRequest_WritesSpanshsFormFields()
    {
        var request = new PlotRequest("Sol", "Maia", Stock5A, Cargo: 8, Algorithm: PlotAlgorithm.Pessimistic, UseSupercharge: true, RefuelEveryScoopable: true);
        var fields = request.FormFields().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        Assert.Equal("Sol", fields["source"]);
        Assert.Equal("Maia", fields["destination"]);
        Assert.Equal("2.45", fields["fuel_power"]);
        Assert.Equal("0.012", fields["fuel_multiplier"]);
        Assert.Equal("1470", fields["optimal_mass"]);
        Assert.Equal("5", fields["max_fuel_per_jump"]);
        Assert.Equal("400.63", fields["base_mass"]);
        Assert.Equal("32", fields["tank_size"]);
        Assert.Equal("0.63", fields["internal_tank_size"]);
        Assert.Equal("4", fields["supercharge_multiplier"]);
        Assert.Equal("8", fields["cargo"]);
        Assert.Equal("pessimistic", fields["algorithm"]);
        Assert.Equal("1", fields["use_supercharge"]);
        Assert.Equal("0", fields["use_injections"]);
        Assert.Equal("1", fields["refuel_every_scoopable"]);
        Assert.Equal("0", fields["is_supercharged"]);
    }

    [Fact]
    public void SpanshJson_ReadsQueuedDoneAndFailedReplies()
    {
        var at = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

        var queued = SpanshJson.Read(/*lang=json,strict*/ """{"job":"j-1","status":"queued"}""", "Sol", "Maia", at);
        Assert.Equal(PlotJobStatus.Queued, queued.Status);
        Assert.Equal("j-1", queued.Job);
        Assert.Null(queued.Route);

        var done = SpanshJson.Read(DoneReply, "Sol", "Maia", at);
        Assert.Equal(PlotJobStatus.Done, done.Status);
        var route = done.Route!;
        Assert.Equal("Sol", route.Source);
        Assert.Equal("Maia", route.Destination);
        Assert.Equal(at, route.PlottedAt);
        Assert.Equal(3, route.Stops.Count);
        Assert.Equal(2, route.Jumps);
        Assert.Equal(174.98, route.TotalLy, 0.001);
        Assert.Equal(1, route.NeutronStops);
        Assert.Equal(1, route.RefuelStops);
        var scoop = route.Stops[1];
        Assert.Equal(13864825529753, scoop.Id64);
        Assert.Equal(38.29, scoop.JumpLy);
        Assert.Equal(-21.96875, scoop.Position.Y);
        Assert.True(scoop.IsScoopable);
        Assert.True(scoop.MustRefuel);
        Assert.True(route.Stops[2].HasNeutron);
        Assert.Equal(1, route.IndexOf(13864825529753, null));
        Assert.Equal(2, route.IndexOf(0, "maia"));
        Assert.Equal(-1, route.IndexOf(42, "Colonia"));

        var failed = SpanshJson.Read(/*lang=json,strict*/ """{"job":"j-2","status":"failed","error":"Unable to find route"}""", "Sol", "Nowhere", at);
        Assert.Equal(PlotJobStatus.Failed, failed.Status);
        Assert.Equal("Unable to find route", failed.Error);

        Assert.Equal(PlotJobStatus.Failed, SpanshJson.Read("<html>", "Sol", "Maia", at).Status);
        Assert.Equal(PlotJobStatus.Failed, SpanshJson.Read(/*lang=json,strict*/ """{"status":"ok","result":{"jumps":[]}}""", "Sol", "Maia", at).Status);
    }

    [Fact]
    public void PlottedRoute_RoundTripsThroughJson()
    {
        var route = SpanshJson.Read(DoneReply, "Sol", "Maia", DateTimeOffset.UtcNow).Route!;

        var json = JsonSerializer.Serialize(route);
        var back = JsonSerializer.Deserialize<PlottedRoute>(json);

        Assert.NotNull(back);
        Assert.Equal(route, back with { Stops = route.Stops });
        Assert.Equal(route.Stops, back.Stops);
        Assert.DoesNotContain("TotalLy", json, StringComparison.Ordinal);
    }
}
