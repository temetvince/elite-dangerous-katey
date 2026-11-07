using Katey.Core.Routing;
using Katey.Journal;

namespace Katey.App.Routing;

/// <summary>Jump profiles for the ships the journal knows.</summary>
public static class JumpProfiles
{
    /// <summary>The jump profile of an owned ship.</summary>
    /// <param name="ship">The ship.</param>
    /// <returns>
    /// The profile, or <see langword="null"/> when the journal has not reported the ship's loadout, its mass or
    /// its tank, or the loadout holds no frame shift drive. Engineering on the drive is read from its
    /// modifiers, as <see cref="JumpProfile.Build"/> does.
    /// </returns>
    public static JumpProfile? For(OwnedShip ship)
    {
        ArgumentNullException.ThrowIfNull(ship);
        return ship.UnladenMass is { } unladen && ship.FuelCapacity is { } tank
            ? JumpProfile.Build(ship.Modules.Select(module => new ProfileModule(module.Item, module.Modifiers)), unladen, tank, ship.ReserveCapacity ?? 0, ship.MaxJumpRange)
            : null;
    }
}
