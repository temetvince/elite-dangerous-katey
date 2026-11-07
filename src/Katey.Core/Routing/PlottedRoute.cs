using System.Text.Json.Serialization;
using Katey.Core.Exploration;

namespace Katey.Core.Routing;

/// <summary>One system on a plotted route.</summary>
/// <param name="Name">System name as the plotter spells it.</param>
/// <param name="Id64">The game's 64-bit system id.</param>
/// <param name="X">Galactic x, light years.</param>
/// <param name="Y">Galactic y, light years.</param>
/// <param name="Z">Galactic z, light years.</param>
/// <param name="JumpLy">Distance from the previous stop, light years; 0 for the first.</param>
/// <param name="RemainingLy">Straight-line distance from here to the destination, light years.</param>
/// <param name="FuelUsed">Tonnes the jump here burned.</param>
/// <param name="FuelInTank">Tonnes in the main tank on arrival, before any scooping here.</param>
/// <param name="HasNeutron">Whether the arrival star is a neutron star, so the jump out is boosted.</param>
/// <param name="IsScoopable">Whether the ship can refuel here.</param>
/// <param name="MustRefuel">Whether the plot depends on refuelling here.</param>
public sealed record PlottedStop(
    string Name,
    long Id64,
    double X,
    double Y,
    double Z,
    double JumpLy,
    double RemainingLy,
    double FuelUsed,
    double FuelInTank,
    bool HasNeutron,
    bool IsScoopable,
    bool MustRefuel)
{
    /// <summary>Gets where the system is.</summary>
    [JsonIgnore]
    public GalacticPosition Position => new(X, Y, Z);
}

/// <summary>A route a plotter worked out: the stops from departure to destination.</summary>
/// <param name="Source">The departure system's name as asked.</param>
/// <param name="Destination">The destination system's name as asked.</param>
/// <param name="PlottedAt">When the plot was made.</param>
/// <param name="Stops">Every stop, departure first. At least one.</param>
public sealed record PlottedRoute(string Source, string Destination, DateTimeOffset PlottedAt, IReadOnlyList<PlottedStop> Stops)
{
    /// <summary>Gets the whole route's length along its jumps, light years.</summary>
    [JsonIgnore]
    public double TotalLy => Stops.Sum(stop => stop.JumpLy);

    /// <summary>Gets how many jumps the route is.</summary>
    [JsonIgnore]
    public int Jumps => Math.Max(Stops.Count - 1, 0);

    /// <summary>Gets how many stops are neutron stars.</summary>
    [JsonIgnore]
    public int NeutronStops => Stops.Count(stop => stop.HasNeutron);

    /// <summary>Gets how many stops the plot refuels at.</summary>
    [JsonIgnore]
    public int RefuelStops => Stops.Count(stop => stop.MustRefuel);

    /// <summary>The index of a system on the route.</summary>
    /// <param name="id64">The system's id, or 0 to match by name only.</param>
    /// <param name="name">The system's name, compared case-insensitively, or <see langword="null"/>.</param>
    /// <returns>The last stop that is the system, or -1 when the system is not on the route.</returns>
    public int IndexOf(long id64, string? name)
    {
        for (var index = Stops.Count - 1; index >= 0; index--)
        {
            var stop = Stops[index];
            if ((id64 != 0 && stop.Id64 == id64) || (name is not null && string.Equals(stop.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return index;
            }
        }

        return -1;
    }
}
