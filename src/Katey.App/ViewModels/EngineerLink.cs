using Katey.App.Navigation;

namespace Katey.App.ViewModels;

/// <summary>
/// An engineer's page and how their name reads, the same wherever Katey names one. A name is plain ink. While
/// the engineers switch narrows the lists to the engineers you ticked, a ticked engineer takes the link colour
/// and an unticked one is set back. Every name opens the engineer's page. <see cref="Workspace.EngineerLinkFor"/>
/// makes one from the current selection and switch.
/// </summary>
/// <param name="Link">Their page, labelled with their name.</param>
/// <param name="Selected">Whether you will visit them.</param>
/// <param name="Narrowed">Whether the engineers switch narrows the lists to the ones you ticked.</param>
public sealed record EngineerLink(Link Link, bool Selected, bool Narrowed)
{
    /// <summary>Gets the engineer's name.</summary>
    public string Name => Link.Label;

    /// <summary>Gets whether the name takes the link colour: ticked while the switch is on.</summary>
    public bool Highlighted => Narrowed && Selected;

    /// <summary>Gets whether the name is set back: not ticked while the switch is on.</summary>
    public bool Dimmed => Narrowed && !Selected;

    /// <summary>Names in a sentence, each a link in its own tone, separated by commas.</summary>
    /// <param name="engineers">The engineers, in the order to name them.</param>
    /// <param name="fallback">The text when there are none; empty gives an empty sentence.</param>
    /// <returns>The sentence.</returns>
    public static IReadOnlyList<PhrasePart> Names(IEnumerable<EngineerLink> engineers, string fallback)
    {
        ArgumentNullException.ThrowIfNull(engineers);
        ArgumentNullException.ThrowIfNull(fallback);
        var parts = new List<PhrasePart>();
        foreach (var engineer in engineers)
        {
            if (parts.Count > 0)
            {
                parts.Add(new PhrasePart(", "));
            }

            parts.Add(PhrasePart.Of(engineer));
        }

        return parts.Count > 0 ? parts : Phrase.Text(fallback);
    }
}
