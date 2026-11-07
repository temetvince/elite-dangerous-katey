namespace Katey.Core.Exploration;

/// <summary>A point in the galaxy in the game's coordinate frame: light years from Sol along each axis.</summary>
/// <param name="X">Light years along the galactic x axis.</param>
/// <param name="Y">Light years along the galactic y axis.</param>
/// <param name="Z">Light years along the galactic z axis.</param>
public readonly record struct GalacticPosition(double X, double Y, double Z)
{
    /// <summary>Gets Sol, the origin of the frame.</summary>
    public static GalacticPosition Sol { get; } = new(0, 0, 0);

    /// <summary>Straight-line distance to another point.</summary>
    /// <param name="other">The other point.</param>
    /// <returns>Light years, never negative.</returns>
    public double DistanceTo(GalacticPosition other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }
}
