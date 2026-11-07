namespace Katey.Core.Exploration;

/// <summary>How a body's surface gravity bears on landing.</summary>
public enum GravityBand
{
    /// <summary>Below <see cref="SurfaceGravity.HighG"/>: an ordinary landing.</summary>
    Light,

    /// <summary>From <see cref="SurfaceGravity.HighG"/> up: the ship falls fast and thrusters strain; approach slowly.</summary>
    High,

    /// <summary>From <see cref="SurfaceGravity.ExtremeG"/> up: a heavy ship may not be able to arrest its descent.</summary>
    Extreme,
}

/// <summary>Bands surface gravity for a landing warning.</summary>
public static class SurfaceGravity
{
    /// <summary>Gravity in g from which a landing counts as <see cref="GravityBand.High"/>.</summary>
    public const double HighG = 1.0;

    /// <summary>Gravity in g from which a landing counts as <see cref="GravityBand.Extreme"/>.</summary>
    public const double ExtremeG = 1.5;

    /// <summary>Bands a surface gravity.</summary>
    /// <param name="gravityG">Surface gravity in g, or <see langword="null"/> when unknown.</param>
    /// <returns>The band; <see cref="GravityBand.Light"/> when unknown.</returns>
    public static GravityBand Band(double? gravityG) => gravityG switch
    {
        >= ExtremeG => GravityBand.Extreme,
        >= HighG => GravityBand.High,
        _ => GravityBand.Light,
    };
}
