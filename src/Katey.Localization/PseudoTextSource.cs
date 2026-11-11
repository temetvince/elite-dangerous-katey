using System.Globalization;
using System.Text;

namespace Katey.Localization;

/// <summary>
/// A pseudo-locale over another source: every text comes back about a third longer with accented letters,
/// so clipped columns and strings that never went through a table show up before any translation exists.
/// Placeholders are left alone, so templates still fill.
/// </summary>
public sealed class PseudoTextSource : ITextSource
{
    private const string Plain = "aeiouycAEIOUYC";
    private const string Accented = "áéíóúýçÁÉÍÓÚÝÇ";

    private readonly ITextSource _inner;

    /// <summary>Creates the pseudo-locale.</summary>
    /// <param name="inner">The real source.</param>
    public PseudoTextSource(ITextSource inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc/>
    public string? Lookup(string key, CultureInfo culture)
    {
        var text = _inner.Lookup(key, culture);
        return text is null ? null : Transform(text);
    }

    /// <inheritdoc/>
    public IReadOnlySet<string> Keys(CultureInfo culture) => _inner.Keys(culture);

    /// <summary>Lengthens and accents a text, leaving its placeholders as written.</summary>
    /// <param name="text">The text, a template or plain.</param>
    /// <returns>The pseudo-localized text; an empty text stays empty.</returns>
    public static string Transform(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return text;
        }

        IReadOnlyList<TemplateSegment> segments;
        try
        {
            segments = Template.Parse(text);
        }
        catch (FormatException)
        {
            segments = [TemplateSegment.Literal(text)];
        }

        var builder = new StringBuilder("[");
        var letters = 0;
        foreach (var segment in segments)
        {
            if (segment.IsPlaceholder)
            {
                builder.Append(Template.AsWritten(segment));
                continue;
            }

            foreach (var c in segment.Text)
            {
                var at = Plain.IndexOf(c, StringComparison.Ordinal);
                builder.Append(at < 0 ? c : Accented[at]);
                if (char.IsLetter(c))
                {
                    letters++;
                }
            }
        }

        var extra = (int)Math.Ceiling(letters * 0.3);
        if (extra > 0)
        {
            builder.Append(' ').Append('~', extra);
        }

        return builder.Append(']').ToString();
    }
}
