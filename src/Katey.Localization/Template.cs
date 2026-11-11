using System.Collections.Concurrent;
using System.Text;

namespace Katey.Localization;

/// <summary>One piece of a template: a run of text, or a placeholder to fill.</summary>
/// <param name="Text">The literal text; empty for a placeholder.</param>
/// <param name="Name">The placeholder's name, or <see langword="null"/> for text.</param>
/// <param name="Format">The placeholder's format, the part after the colon, or <see langword="null"/> for none.</param>
public readonly record struct TemplateSegment(string Text, string? Name, string? Format)
{
    /// <summary>Gets whether this segment is a placeholder.</summary>
    public bool IsPlaceholder => Name is not null;

    /// <summary>A literal segment.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The segment.</returns>
    public static TemplateSegment Literal(string text) => new(text, null, null);

    /// <summary>A placeholder segment.</summary>
    /// <param name="name">The name.</param>
    /// <param name="format">The format, or <see langword="null"/>.</param>
    /// <returns>The segment.</returns>
    public static TemplateSegment Placeholder(string name, string? format) => new("", name, format);
}

/// <summary>
/// Templates with named placeholders: <c>{name}</c> or <c>{name:format}</c>, with <c>{{</c> and <c>}}</c> for literal
/// braces. Names let a translation put the placeholders in any order. Parsed templates are cached, since the
/// same template is filled many times.
/// </summary>
public static class Template
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<TemplateSegment>> Cache = new(StringComparer.Ordinal);

    /// <summary>Splits a template into literal runs and placeholders.</summary>
    /// <param name="template">The template.</param>
    /// <returns>The segments in order; a template with no placeholders is one literal segment, and an empty template is empty.</returns>
    /// <exception cref="FormatException">A brace is unbalanced or a placeholder is empty.</exception>
    public static IReadOnlyList<TemplateSegment> Parse(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Cache.GetOrAdd(template, ParseUncached);
    }

    /// <summary>The names of a template's placeholders, in first-seen order.</summary>
    /// <param name="template">The template.</param>
    /// <returns>Distinct names.</returns>
    /// <exception cref="FormatException">A brace is unbalanced or a placeholder is empty.</exception>
    public static IReadOnlyList<string> Placeholders(string template)
        => [.. Parse(template).Where(segment => segment.IsPlaceholder).Select(segment => segment.Name!).Distinct(StringComparer.Ordinal)];

    /// <summary>Fills a template.</summary>
    /// <param name="template">The template.</param>
    /// <param name="resolve">Gives the text for a placeholder's name and format, or <see langword="null"/> to leave the placeholder as written.</param>
    /// <returns>The text.</returns>
    /// <exception cref="FormatException">A brace is unbalanced or a placeholder is empty.</exception>
    public static string Fill(string template, Func<string, string?, string?> resolve)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        var segments = Parse(template);
        if (segments.Count == 1 && !segments[0].IsPlaceholder)
        {
            return segments[0].Text;
        }

        var builder = new StringBuilder();
        foreach (var segment in segments)
        {
            if (segment.IsPlaceholder)
            {
                builder.Append(resolve(segment.Name!, segment.Format) ?? AsWritten(segment));
            }
            else
            {
                builder.Append(segment.Text);
            }
        }

        return builder.ToString();
    }

    /// <summary>A placeholder as it is written in a template, <c>{name}</c> or <c>{name:format}</c>.</summary>
    /// <param name="segment">A placeholder segment.</param>
    /// <returns>The text.</returns>
    public static string AsWritten(TemplateSegment segment)
        => segment.Format is null ? "{" + segment.Name + "}" : "{" + segment.Name + ":" + segment.Format + "}";

    private static IReadOnlyList<TemplateSegment> ParseUncached(string template)
    {
        var segments = new List<TemplateSegment>();
        var literal = new StringBuilder();
        var i = 0;
        while (i < template.Length)
        {
            var c = template[i];
            if (c == '{')
            {
                if (i + 1 < template.Length && template[i + 1] == '{')
                {
                    literal.Append('{');
                    i += 2;
                    continue;
                }

                var close = template.IndexOf('}', i + 1);
                if (close < 0)
                {
                    throw new FormatException($"Unbalanced '{{' at {i} in \"{template}\".");
                }

                var inner = template.AsSpan(i + 1, close - i - 1);
                var colon = inner.IndexOf(':');
                var name = colon < 0 ? inner : inner[..colon];
                if (name.IsEmpty || name.IsWhiteSpace())
                {
                    throw new FormatException($"Empty placeholder at {i} in \"{template}\".");
                }

                if (literal.Length > 0)
                {
                    segments.Add(TemplateSegment.Literal(literal.ToString()));
                    literal.Clear();
                }

                segments.Add(TemplateSegment.Placeholder(name.ToString(), colon < 0 ? null : inner[(colon + 1)..].ToString()));
                i = close + 1;
                continue;
            }

            if (c == '}')
            {
                if (i + 1 < template.Length && template[i + 1] == '}')
                {
                    literal.Append('}');
                    i += 2;
                    continue;
                }

                throw new FormatException($"Unbalanced '}}' at {i} in \"{template}\".");
            }

            literal.Append(c);
            i++;
        }

        if (literal.Length > 0 || segments.Count == 0)
        {
            segments.Add(TemplateSegment.Literal(literal.ToString()));
        }

        return segments;
    }
}
