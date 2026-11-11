using Katey.App.Navigation;
using Katey.App.Pages;
using Katey.App.Routing;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core.Exploration;

namespace Katey.App;

/// <summary>Fuel at one route stop, ready to print.</summary>
/// <param name="Text">Tonnes on arrival, or "out of fuel". A refill here shows in <paramref name="ScoopHere"/>, not in the text.</param>
/// <param name="Reachable">Whether the ship gets here.</param>
/// <param name="ScoopHere">Whether the projection refills here.</param>
public sealed record StopFuel(string Text, bool Reachable, bool ScoopHere);

/// <summary>
/// The fuel picture for the route ahead: the game's own fuel equation from the ship's loadout figures, or
/// a model fitted to its recent jumps when the loadout lacks them, run from the live tank reading along
/// the plotted legs; the tank itself against its capacity; and what the next jump takes out of it. The
/// tank figures are what the fuel meter draws: the whole reading, and the reading less the next jump, so
/// the difference reads as the jump's cost.
/// </summary>
/// <param name="SummaryParts">One line: jumps left without scooping, and the last scoopable star in reach as a link to its system, or why there is no estimate.</param>
/// <param name="Stops">Fuel at each route stop by stop index, or empty without an estimate.</param>
/// <param name="HasEstimate">Whether a model could be built.</param>
/// <param name="TankTonnes">The main tank's reading, tonnes, or <see langword="null"/> before the game reports the ship and the tank.</param>
/// <param name="TankCapacity">The main tank's size, tonnes, positive, or <see langword="null"/> with <paramref name="TankTonnes"/>.</param>
/// <param name="JumpsWithoutScooping">How many jumps ahead the tank covers before a scoop, or <see langword="null"/> without an estimate.</param>
/// <param name="NextJumpTonnes">Tonnes the next jump still to be made burns, positive, or <see langword="null"/> without an estimate or a jump ahead. During a jump that is the jump after arrival, since the tank already shows the one under way.</param>
public sealed record FuelEstimate(IReadOnlyList<PhrasePart> SummaryParts, IReadOnlyDictionary<int, StopFuel> Stops, bool HasEstimate, double? TankTonnes = null, double? TankCapacity = null, int? JumpsWithoutScooping = null, double? NextJumpTonnes = null)
{
    /// <summary>Gets the estimate before anything is known: no summary, no stops, no tank.</summary>
    public static FuelEstimate None { get; } = new(Phrase.Empty, new Dictionary<int, StopFuel>(), false);

    /// <summary>Gets <see cref="SummaryParts"/> as plain text.</summary>
    public string Summary => Phrase.Plain(SummaryParts);

    /// <summary>Gets whether the tank reading is known.</summary>
    public bool HasTank => TankTonnes is not null && TankCapacity is not null;

    /// <summary>Gets the tank's reading over its capacity, 0 to 1. 0 while the reading is unknown.</summary>
    public double TankFraction => TankTonnes is { } tonnes && TankCapacity is { } capacity ? Math.Clamp(tonnes / capacity, 0, 1) : 0;

    /// <summary>Gets the reading against the capacity, such as "18.3 / 32 t", or empty without a reading.</summary>
    public string TankText => TankTonnes is { } tonnes && TankCapacity is { } capacity ? Strings.Format(Keys.Fuel_Tank, ("tonnes", tonnes), ("capacity", capacity)) : "";

    /// <summary>Gets whether the tank is below a quarter, the point at which the figure turns a warning colour.</summary>
    public bool TankIsLow => HasTank && TankFraction < 0.25;

    /// <summary>Gets whether the next jump's burn is known.</summary>
    public bool HasNextJump => NextJumpTonnes is not null;

    /// <summary>Gets the next jump's burn over the capacity, 0 to <see cref="TankFraction"/>: a jump the tank cannot cover takes all of it.</summary>
    public double NextJumpFraction => NextJumpTonnes is { } burn && TankCapacity is { } capacity ? Math.Clamp(burn / capacity, 0, TankFraction) : 0;

    /// <summary>Gets what stays in the tank after the next jump, over the capacity, 0 to 1: <see cref="TankFraction"/> less <see cref="NextJumpFraction"/>.</summary>
    public double TankAfterJumpFraction => TankFraction - NextJumpFraction;

    /// <summary>Gets the next jump's burn as a figure to set beside the reading, such as "-4.2 t", or empty without one.</summary>
    public string NextJumpText => NextJumpTonnes is { } burn ? Strings.Format(Keys.Fuel_NextJump, ("tonnes", burn)) : "";

    /// <summary>Builds the estimate for the current ship and route.</summary>
    /// <param name="workspace">The shared state.</param>
    /// <param name="stops">The route rows, in route order. May be empty, in which case only the tank is reported.</param>
    /// <returns>The estimate, with a summary even when no model could be built.</returns>
    public static FuelEstimate For(Workspace workspace, IReadOnlyList<RouteStopRow> stops)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(stops);

        var none = new Dictionary<int, StopFuel>();
        var fleet = workspace.FleetSnapshot;
        var ship = fleet.Ships.FirstOrDefault(ship => ship.IsCurrent);
        var fuelNow = workspace.Monitor.Status?.FuelMain;
        if (stops.Count == 0)
        {
            return ship is not null && fuelNow is { } reading
                ? new FuelEstimate(Phrase.Empty, none, false, reading, ship.FuelCapacity ?? Math.Max(reading, 1))
                : None;
        }

        if (ship is null || fuelNow is null)
        {
            return new FuelEstimate(Phrase.Text(Strings.Get(Keys.Fuel_NeedsShipAndTank)), none, false);
        }

        var capacity = ship.FuelCapacity ?? Math.Max(fuelNow.Value, 1);

        var driveModule = ship.Modules.FirstOrDefault(module => FrameShiftDrives.Figures(module.Item) is not null);
        if (driveModule is null)
        {
            return new FuelEstimate(Phrase.Text(Strings.Get(Keys.Fuel_NeedsDrive)), none, false, fuelNow, capacity);
        }

        // The game's own equation from the loadout's masses and drive figures needs no jump first, and is asked again before each
        // jump at the fuel then aboard, with the cargo aboard now; a loadout without the figures falls back to the jumps the ship has made.
        var cargo = workspace.Monitor.Status?.Cargo ?? 0;
        Func<double, JumpFuelModel>? modelAt = JumpProfiles.For(ship) is { } profile ? fuel => profile.FuelModel(fuel, cargo) : null;
        if (modelAt is null)
        {
            var maxFuel = driveModule.Modifiers.TryGetValue("MaxFuelPerJump", out var modified) && modified > 0
                ? modified
                : FrameShiftDrives.MaxFuelPerJumpOf(driveModule.Item);
            var booster = ship.Modules.Select(module => FrameShiftDrives.GuardianBoosterLy(module.Item)).DefaultIfEmpty(0).Max();
            var observations = workspace.Route.RecentJumps.Select(jump => new JumpObservation(jump.DistanceLy, jump.FuelUsed, jump.Boost));
            if (JumpFuelModel.Calibrate(observations, FrameShiftDrives.FuelPowerOf(driveModule.Item), maxFuel, booster) is { } learned)
            {
                modelAt = _ => learned;
            }
        }

        if (modelAt is null)
        {
            return new FuelEstimate(Phrase.Text(Strings.Get(Keys.Fuel_NeedsMassOrJump)), none, false, fuelNow, capacity);
        }

        var hereIndex = RoutePage.HereIndex(workspace);
        var ahead = stops.Where(stop => stop.Index - 1 > hereIndex).ToList();
        var previousKind = hereIndex >= 0 ? stops[hereIndex].Kind : StarKind.Other;
        var byIndex = new Dictionary<int, StopFuel>();

        // Once the drive has fired, the game takes the jump's fuel at once and the tank reading already shows it,
        // so the stop being arrived at shows the reading and the projection starts from there.
        var jumping = workspace.Monitor.Status is { IsInHyperspace: true } || workspace.Route.InHyperspace;
        if (jumping && ahead.Count > 0)
        {
            var arriving = ahead[0];
            byIndex[arriving.Index] = new StopFuel(Strings.Format(Keys.Unit_Tonnes, ("value", fuelNow.Value)), true, arriving.IsScoopable);
            previousKind = arriving.Kind;
            ahead = [.. ahead.Skip(1)];
        }

        if (ahead.Count == 0)
        {
            return new FuelEstimate(Phrase.Empty, byIndex, true, fuelNow, capacity);
        }

        var legs = new List<FuelLeg>();
        foreach (var stop in ahead)
        {
            legs.Add(new FuelLeg(stop.JumpLy, FrameShiftDrives.BoostFrom(previousKind), stop.IsScoopable));
            previousKind = stop.Kind;
        }

        var projection = FuelPlanner.Project(modelAt, fuelNow.Value, capacity, legs);
        for (var i = 0; i < ahead.Count; i++)
        {
            var fuel = projection.Stops[i];
            byIndex[ahead[i].Index] = new StopFuel(
                !fuel.Reachable ? Strings.Get(Keys.Fuel_OutOfFuel) : Strings.Format(Keys.Unit_Tonnes, ("value", fuel.FuelOnArrival)),
                fuel.Reachable,
                fuel.ScoopHere);
        }

        var parts = new List<IReadOnlyList<PhrasePart>>
        {
            Phrase.Text(projection.JumpsWithoutScooping >= ahead.Count
                ? Strings.Get(Keys.Fuel_WholeRoute)
                : projection.JumpsWithoutScooping == 0
                    ? Strings.Get(Keys.Fuel_NotEnoughNext)
                    : Strings.Format(Keys.Fuel_ForJumps, ("jumps", Display.Plural(Keys.Plural_MoreJumps, projection.JumpsWithoutScooping)))),
        };
        if (projection.JumpsWithoutScooping < ahead.Count)
        {
            parts.Add(projection.MustScoopBy is { } must
                ? Phrase.Format(Keys.Fuel_ScoopBy, ("system", workspace.Navigator.LinkTo(ahead[must].System, PageKind.System, ahead[must].System)), ("index", ahead[must].Index))
                : Phrase.Text(Strings.Get(Keys.Fuel_NoScoopable)));
        }

        if (projection.RunsDry)
        {
            parts.Add(Phrase.Text(Strings.Get(Keys.Fuel_RunsDry)));
        }

        var nextJump = projection.Stops[0].Cost;
        return new FuelEstimate(Phrase.Join(" ", parts), byIndex, true, fuelNow, capacity, projection.JumpsWithoutScooping, nextJump > 0 ? nextJump : null);
    }
}
