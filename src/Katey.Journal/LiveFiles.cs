using System.Text.Json;

namespace Katey.Journal;

/// <summary>One star on a plotted route.</summary>
/// <param name="StarSystem">System name.</param>
/// <param name="SystemAddress">The game's 64-bit system id.</param>
/// <param name="StarClass">Spectral class of the arrival star, such as <c>K</c>, <c>DA</c> or <c>N</c>.</param>
/// <param name="X">Galactic x, light years.</param>
/// <param name="Y">Galactic y, light years.</param>
/// <param name="Z">Galactic z, light years.</param>
public sealed record RouteStop(string StarSystem, long SystemAddress, string StarClass, double X, double Y, double Z)
{
    /// <summary>Straight-line distance to another stop, in light years.</summary>
    /// <param name="other">The other stop.</param>
    /// <returns>Light years.</returns>
    public double DistanceTo(RouteStop other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }
}

/// <summary>The plotted route, as <c>NavRoute.json</c> holds it.</summary>
/// <param name="Timestamp">When the game wrote the file.</param>
/// <param name="Stops">Every system on the route, departure first. Empty after the route is cleared.</param>
public sealed record NavRoute(DateTimeOffset Timestamp, IReadOnlyList<RouteStop> Stops)
{
    /// <summary>Parses the file's text.</summary>
    /// <param name="text">The whole file.</param>
    /// <returns>The route, or <see langword="null"/> when the text is not a NavRoute document.</returns>
    public static NavRoute? Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("Route", out var route) || route.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var stops = new List<RouteStop>();
            foreach (var stop in route.EnumerateArray())
            {
                var name = JsonRead.Text(stop, "StarSystem");
                if (name is null)
                {
                    continue;
                }

                var pos = stop.TryGetProperty("StarPos", out var p) && p.ValueKind == JsonValueKind.Array && p.GetArrayLength() == 3
                    ? p.EnumerateArray().Select(v => v.GetDouble()).ToArray()
                    : [0, 0, 0];
                stops.Add(new RouteStop(name, JsonRead.Identifier(stop, "SystemAddress"), JsonRead.Text(stop, "StarClass") ?? "", pos[0], pos[1], pos[2]));
            }

            return new NavRoute(JsonRead.Timestamp(root) ?? DateTimeOffset.MinValue, stops);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>The live ship status, as <c>Status.json</c> holds it. Only the fields Katey shows.</summary>
/// <param name="Timestamp">When the game wrote the file.</param>
/// <param name="Flags">The <c>Flags</c> bit field.</param>
/// <param name="BodyName">The body the ship is at or near, or <see langword="null"/>.</param>
/// <param name="DestinationName">The selected destination's name, or <see langword="null"/>.</param>
/// <param name="FuelMain">Main tank fuel in tonnes, or <see langword="null"/> when not reported.</param>
/// <param name="Cargo">Tonnes of cargo aboard, or <see langword="null"/> when not reported.</param>
/// <param name="DestinationSystem">The id of the system the selected destination is in, or 0 with no destination. The file drops the destination the moment the player clears it.</param>
public sealed record ShipStatus(DateTimeOffset Timestamp, long Flags, string? BodyName, string? DestinationName, double? FuelMain, double? Cargo = null, long DestinationSystem = 0)
{
    /// <summary>Gets whether the ship is docked.</summary>
    public bool IsDocked => (Flags & 1) != 0;

    /// <summary>Gets whether the ship is landed on a surface.</summary>
    public bool IsLanded => (Flags & 2) != 0;

    /// <summary>Gets whether the ship is in supercruise.</summary>
    public bool IsInSupercruise => (Flags & 0x10) != 0;

    /// <summary>Gets whether the drive is charging for a jump.</summary>
    public bool IsFsdCharging => (Flags & 0x20000) != 0;

    /// <summary>Gets whether the ship is in hyperspace, between the charge and the arrival. The charging flag drops the moment the jump begins.</summary>
    public bool IsInHyperspace => (Flags & 0x40000000) != 0;

    /// <summary>Gets whether the ship is scooping fuel.</summary>
    public bool IsScooping => (Flags & 0x80) != 0;

    /// <summary>Parses the file's text.</summary>
    /// <param name="text">The whole file.</param>
    /// <returns>The status, or <see langword="null"/> when the text is not a Status document.</returns>
    public static ShipStatus? Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(nameof(Flags), out _))
            {
                return null;
            }

            string? destination = null;
            long destinationSystem = 0;
            if (root.TryGetProperty("Destination", out var dest) && dest.ValueKind == JsonValueKind.Object)
            {
                destination = JsonRead.Text(dest, "Name");
                destinationSystem = JsonRead.Identifier(dest, "System");
            }

            double? fuel = null;
            if (root.TryGetProperty("Fuel", out var fuelElement) && fuelElement.ValueKind == JsonValueKind.Object
                && fuelElement.TryGetProperty(nameof(FuelMain), out var main) && main.ValueKind == JsonValueKind.Number)
            {
                fuel = main.GetDouble();
            }

            return new ShipStatus(
                JsonRead.Timestamp(root) ?? DateTimeOffset.MinValue,
                JsonRead.Identifier(root, nameof(Flags)),
                JsonRead.Text(root, "BodyName"),
                destination,
                fuel,
                JsonRead.Measure(root, "Cargo"),
                destinationSystem);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
