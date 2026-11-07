using Katey.App.ViewModels;
using Katey.Core.Fleet;

namespace Katey.App;

/// <summary>The pick-list options for the fleet's policy and filters, shared by every page that offers them.</summary>
public static class FleetChoices
{
    /// <summary>Gets the sharing policies, keyed by <see cref="SharingPolicy"/> name.</summary>
    public static IReadOnlyList<Choice> Policies { get; } =
    [
        new(nameof(SharingPolicy.Pooled), "Share modules across every fit"),
        new(nameof(SharingPolicy.SameHull), "Share only between fits for the same hull"),
        new(nameof(SharingPolicy.None), "Every fit needs its own modules"),
    ];

    /// <summary>Gets the engineering filters, keyed by <see cref="EngineeringFilter"/> name.</summary>
    public static IReadOnlyList<Choice> Filters { get; } =
    [
        new(nameof(EngineeringFilter.All), "All modules"),
        new(nameof(EngineeringFilter.UnengineeredOnly), "Unengineered only"),
        new(nameof(EngineeringFilter.EngineeredOnly), "Engineered only"),
    ];

    /// <summary>Gets the place filters, keyed by <see cref="PlaceFilter"/> name.</summary>
    public static IReadOnlyList<Choice> Places { get; } =
    [
        new(nameof(PlaceFilter.Anywhere), "Fitted or stored"),
        new(nameof(PlaceFilter.StoredOnly), "Stored only, not on a ship"),
        new(nameof(PlaceFilter.FittedOnly), "Fitted to a ship only"),
    ];

    /// <summary>Gets the option that stands for every system.</summary>
    public static Choice Everywhere { get; } = new("", "Everywhere");

    /// <summary>"Everywhere" plus every system holding a ship or a stored module, alphabetical.</summary>
    /// <param name="fleet">The fleet.</param>
    /// <returns>The options.</returns>
    public static IReadOnlyList<Choice> Locations(Journal.FleetSnapshot fleet)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        var systems = fleet.Ships.Select(ship => ship.System)
            .Concat(fleet.StoredModules.Select(module => module.System))
            .Where(system => !string.IsNullOrEmpty(system))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(system => system, StringComparer.OrdinalIgnoreCase)
            .Select(system => new Choice(system!, system!));
        return [Everywhere, .. systems];
    }

    /// <summary>The option whose key matches a saved location, or <see cref="Everywhere"/>.</summary>
    /// <param name="locations">Options from <see cref="Locations"/>.</param>
    /// <param name="location">The saved system, or <see langword="null"/>.</param>
    /// <returns>One of <paramref name="locations"/>.</returns>
    public static Choice LocationChoice(IReadOnlyList<Choice> locations, string? location)
    {
        ArgumentNullException.ThrowIfNull(locations);
        return locations.FirstOrDefault(choice => string.Equals(choice.Key, location ?? "", StringComparison.OrdinalIgnoreCase)) ?? Everywhere;
    }
}
