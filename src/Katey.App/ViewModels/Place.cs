using Katey.App.Navigation;
using Katey.App.Text;

namespace Katey.App.ViewModels;

/// <summary>Somewhere in the galaxy, printed as the system's link followed by a detail: "Deciat · Farseer Inc".</summary>
/// <param name="System">The system's page, or <see langword="null"/> when the system is unknown.</param>
/// <param name="Detail">The rest of the place: a base, a station, a ship, or a state such as "in transit". Empty when the system says it all.</param>
public sealed record Place(Link? System, string Detail)
{
    /// <summary>Gets a place with no system, only text.</summary>
    /// <param name="detail">The text.</param>
    /// <returns>The place.</returns>
    public static Place Only(string detail) => new(null, detail);

    /// <summary>Gets whether the system is known, so there is a link to show.</summary>
    public bool HasSystem => System is not null;

    /// <summary>Gets whether there is a detail to show after the system.</summary>
    public bool HasDetail => Detail.Length > 0;

    /// <summary>Gets whether both a system and a detail show, so a separator goes between them.</summary>
    public bool HasBoth => HasSystem && HasDetail;

    /// <summary>Gets the place as one line of text, without the link.</summary>
    public string Text => System is null ? Detail : Detail.Length == 0 ? System.Label : Strings.Format(Keys.Place_Text, ("system", System.Label), ("detail", Detail));
}
