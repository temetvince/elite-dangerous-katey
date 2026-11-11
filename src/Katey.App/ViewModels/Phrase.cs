using Katey.App.Navigation;
using Katey.App.Text;
using Katey.Localization;

namespace Katey.App.ViewModels;

/// <summary>How a link in running text is coloured.</summary>
public enum LinkTone
{
    /// <summary>The link colour, as a system name reads.</summary>
    Accent,

    /// <summary>The HUD blue, as a ticked engineer reads while the engineers switch is on.</summary>
    Hud,

    /// <summary>The ink of the surrounding text, as an engineer reads with the engineers switch off.</summary>
    Ink,

    /// <summary>The ink set back, as an engineer you have not ticked reads while the switch is on.</summary>
    Dim,
}

/// <summary>One part of a sentence: a run of text, or a link set in the running text.</summary>
/// <param name="Text">The words, spaces included; a link's label when <paramref name="Link"/> is set.</param>
/// <param name="Link">The page the part opens, or <see langword="null"/> for plain text.</param>
/// <param name="Tone">How a link is coloured; meaningless for plain text.</param>
public sealed record PhrasePart(string Text, Link? Link = null, LinkTone Tone = LinkTone.Accent)
{
    /// <summary>Gets whether the part opens a page.</summary>
    public bool IsLink => Link is not null;

    /// <summary>A link as a part, labelled as the link is, in the link colour.</summary>
    /// <param name="link">The link.</param>
    /// <returns>The part.</returns>
    public static PhrasePart Of(Link link)
    {
        ArgumentNullException.ThrowIfNull(link);
        return new(link.Label, link);
    }

    /// <summary>An engineer as a part: their name, opening their page, in the tone their tick and the engineers switch give it.</summary>
    /// <param name="engineer">The engineer.</param>
    /// <returns>The part.</returns>
    public static PhrasePart Of(EngineerLink engineer)
    {
        ArgumentNullException.ThrowIfNull(engineer);
        return new(engineer.Name, engineer.Link, engineer.Highlighted ? LinkTone.Hud : engineer.Dimmed ? LinkTone.Dim : LinkTone.Ink);
    }
}

/// <summary>
/// A sentence that can carry links, so a system named in running text opens its page. The view sets the
/// parts as one wrapping text block; a caller that needs the words alone reads <see cref="Plain"/>.
/// </summary>
public static class Phrase
{
    /// <summary>Gets a sentence with nothing in it.</summary>
    public static IReadOnlyList<PhrasePart> Empty { get; } = [];

    /// <summary>A sentence of plain text.</summary>
    /// <param name="text">The text; empty gives <see cref="Empty"/>.</param>
    /// <returns>The parts.</returns>
    public static IReadOnlyList<PhrasePart> Text(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Length == 0 ? Empty : [new PhrasePart(text)];
    }

    /// <summary>A sentence from its parts, in order: a <see cref="string"/> is text, a <see cref="Link"/> is a link, an <see cref="EngineerLink"/> an engineer in their tone, anything else is written out as text.</summary>
    /// <param name="parts">The parts; <see langword="null"/> and empty strings are skipped.</param>
    /// <returns>The parts.</returns>
    public static IReadOnlyList<PhrasePart> Of(params object?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var result = new List<PhrasePart>();
        foreach (var part in parts)
        {
            switch (part)
            {
                case null:
                case string { Length: 0 }:
                    break;
                case Link link:
                    result.Add(PhrasePart.Of(link));
                    break;
                case EngineerLink engineer:
                    result.Add(PhrasePart.Of(engineer));
                    break;
                case string text:
                    result.Add(new PhrasePart(text));
                    break;
                default:
                    result.Add(new PhrasePart(part.ToString() ?? ""));
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// A sentence from a keyed template, its placeholders filled by name: a <see cref="Link"/> becomes a link, an
    /// <see cref="EngineerLink"/> an engineer in their tone, a sentence is spliced in, and anything else is written
    /// out as text with the placeholder's format. The template comes from <see cref="Strings"/>, so a translation
    /// can put the links in any order.
    /// </summary>
    /// <param name="key">A key from <see cref="Keys"/>.</param>
    /// <param name="args">Name → value. A placeholder no argument names stays as written; a <see langword="null"/> value writes nothing.</param>
    /// <returns>The parts.</returns>
    public static IReadOnlyList<PhrasePart> Format(string key, params (string Name, object? Value)[] args)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(args);
        var result = new List<PhrasePart>();
        foreach (var segment in Template.Parse(Strings.Get(key)))
        {
            if (!segment.IsPlaceholder)
            {
                result.Add(new PhrasePart(segment.Text));
                continue;
            }

            var found = false;
            foreach (var (name, value) in args)
            {
                if (!string.Equals(name, segment.Name, StringComparison.Ordinal))
                {
                    continue;
                }

                found = true;
                switch (value)
                {
                    case null:
                        break;
                    case Link link:
                        result.Add(PhrasePart.Of(link));
                        break;
                    case EngineerLink engineer:
                        result.Add(PhrasePart.Of(engineer));
                        break;
                    case PhrasePart part:
                        result.Add(part);
                        break;
                    case IReadOnlyList<PhrasePart> parts:
                        result.AddRange(parts);
                        break;
                    default:
                        var text = Localizer.Write(value, segment.Format);
                        if (text.Length > 0)
                        {
                            result.Add(new PhrasePart(text));
                        }

                        break;
                }

                break;
            }

            if (!found)
            {
                result.Add(new PhrasePart(Template.AsWritten(segment)));
            }
        }

        return result;
    }

    /// <summary>Joins sentences with a separator, skipping empty ones.</summary>
    /// <param name="separator">The text between two sentences.</param>
    /// <param name="sentences">The sentences.</param>
    /// <returns>One sentence.</returns>
    public static IReadOnlyList<PhrasePart> Join(string separator, IEnumerable<IReadOnlyList<PhrasePart>> sentences)
    {
        ArgumentNullException.ThrowIfNull(separator);
        ArgumentNullException.ThrowIfNull(sentences);
        var result = new List<PhrasePart>();
        foreach (var sentence in sentences.Where(sentence => sentence.Count > 0))
        {
            if (result.Count > 0)
            {
                result.Add(new PhrasePart(separator));
            }

            result.AddRange(sentence);
        }

        return result;
    }

    /// <summary>The words alone.</summary>
    /// <param name="parts">The sentence.</param>
    /// <returns>Every part's text, run together.</returns>
    public static string Plain(IReadOnlyList<PhrasePart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return string.Concat(parts.Select(part => part.Text));
    }
}
