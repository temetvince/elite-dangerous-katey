using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core.Fleet;

namespace Katey.App;

/// <summary>The pick-list options for the fleet's policy and filters, shared by every page that offers them.</summary>
public static class FleetChoices
{
    /// <summary>Gets the sharing policies, keyed by <see cref="SharingPolicy"/> name.</summary>
    public static IReadOnlyList<Choice> Policies
    => [
        new(nameof(SharingPolicy.Pooled), Strings.Get(Keys.Sharing_Pooled)),
        new(nameof(SharingPolicy.SameHull), Strings.Get(Keys.Sharing_SameHull)),
        new(nameof(SharingPolicy.None), Strings.Get(Keys.Sharing_None)),
    ];

    /// <summary>Gets the engineering filters, keyed by <see cref="EngineeringFilter"/> name.</summary>
    public static IReadOnlyList<Choice> Filters
    => [
        new(nameof(EngineeringFilter.All), Strings.Get(Keys.Filter_AllModules)),
        new(nameof(EngineeringFilter.UnengineeredOnly), Strings.Get(Keys.Filter_UnengineeredOnly)),
        new(nameof(EngineeringFilter.EngineeredOnly), Strings.Get(Keys.Filter_EngineeredOnly)),
    ];

    /// <summary>Gets the place filters, keyed by <see cref="PlaceFilter"/> name.</summary>
    public static IReadOnlyList<Choice> Places
    => [
        new(nameof(PlaceFilter.Anywhere), Strings.Get(Keys.Filter_FittedOrStored)),
        new(nameof(PlaceFilter.StoredOnly), Strings.Get(Keys.Filter_StoredOnly)),
        new(nameof(PlaceFilter.FittedOnly), Strings.Get(Keys.Filter_FittedOnly)),
    ];

    /// <summary>Gets the option that stands for every system.</summary>
    public static Choice Everywhere => new("", Strings.Get(Keys.Filter_Everywhere));

    /// <summary>
    /// "Everywhere" plus every system holding a ship or a stored module, alphabetical, and the saved system when
    /// nothing is there right now, so the filter in force is always on offer and never silently shown as another.
    /// </summary>
    /// <param name="fleet">The fleet.</param>
    /// <param name="saved">The saved system, or <see langword="null"/>.</param>
    /// <returns>The options.</returns>
    public static IReadOnlyList<Choice> Locations(Journal.FleetSnapshot fleet, string? saved)
    {
        ArgumentNullException.ThrowIfNull(fleet);
        var systems = fleet.Ships.Select(ship => ship.System)
            .Concat(fleet.StoredModules.Select(module => module.System))
            .Append(saved)
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
