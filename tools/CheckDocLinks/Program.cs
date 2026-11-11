using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CheckDocLinks;

/// <summary>
/// The CheckDocLinks tool: verifies the repo's docs link correctly and reports every broken link. It runs
/// two families of check. First, resolution — every relative Markdown link and heading anchor must resolve
/// (a link to a moved file, or a renamed "#heading-anchor", fails), with anchors slugged by GitHub's rules
/// so the check matches how links resolve on the repo host. Second, symbol links — a <c>[`Symbol`](target)</c>
/// link (visible text is one code span) must land where that code element is documented, failing a dangling
/// symbol (the name appears nowhere in the target file) or one that points into a solution guide section not
/// about it. It skips external (http/https/mailto/tel) links and anything under obj/ or bin/. Run from the
/// repo root; the exit code is 0 when all links resolve, 1 when problems are listed, and 2 when the repo
/// root cannot be found.
/// </summary>
internal static partial class Program
{
    private static readonly string[] SkipDirectories = ["obj", "bin", ".git", "node_modules"];

    // A Markdown link [text](target) that is not an image (![...]). The target runs up to the first ')' or
    // whitespace, so a trailing " title" or ")" is excluded.
    [GeneratedRegex(@"(?<!\!)\[[^\]]*\]\(\s*([^)\s]+)")]
    private static partial Regex LinkPattern();

    // An ATX heading line: one to six '#', a space, then the heading text (trailing '#'s trimmed).
    [GeneratedRegex(@"^(#{1,6})\s+(.*?)\s*#*\s*$")]
    private static partial Regex HeadingPattern();

    // A [text](url) span inside heading text, reduced to its visible text when slugging.
    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex InlineLinkPattern();

    // An inline code span (`...`). Its contents are literal, so a [x](y) inside backticks is documentation,
    // not a link — stripped before scanning a line so example link syntax is not mistaken for a real link.
    [GeneratedRegex("`[^`]*`")]
    private static partial Regex InlineCodePattern();

    // A link whose visible text is a single inline-code span — [`Symbol`](target). These name a code
    // element, so the target should be where that element is *documented*, not merely a page that mentions
    // it. Scanned on the raw line (the inline-code strip above would erase the backticks that mark them).
    [GeneratedRegex(@"(?<!\!)\[`([^`\]]+)`\]\(\s*([^)\s]+)")]
    private static partial Regex SymbolLinkPattern();

    // A solution *guide* defers to per-project reference READMEs with "> Reference: [..](..)" lines; a
    // per-project README (the reference itself) carries none. A `Symbol` link belongs on the reference, so
    // a guide section is never the right landing place for one.
    [GeneratedRegex(@"(?m)^\s*>\s*Reference:\s*\[")]
    private static partial Regex GuideMarkerPattern();

    private static int Main(string[] args)
    {
        var root = ResolveRoot(args);
        if (root is null)
        {
            Console.Error.WriteLine("Could not locate the repo root (the directory holding .git). " +
                "Run from inside the repo, or pass the root path as the first argument.");
            return 2;
        }

        var files = MarkdownFiles(root).ToList();
        var headingsByFile = files.ToDictionary(f => f, HeadingsOf, StringComparer.OrdinalIgnoreCase);
        var anchorsByFile = headingsByFile.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value.Keys, StringComparer.Ordinal),
            StringComparer.OrdinalIgnoreCase);
        var guideFiles = new HashSet<string>(
            files.Where(f => GuideMarkerPattern().IsMatch(File.ReadAllText(f))),
            StringComparer.OrdinalIgnoreCase);

        var broken = new List<string>();
        foreach (var file in files)
        {
            CheckFile(file, anchorsByFile, headingsByFile, guideFiles, broken);
        }

        if (broken.Count > 0)
        {
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Broken doc links ({broken.Count}):"));
            foreach (var entry in broken.OrderBy(b => b, StringComparer.Ordinal))
            {
                Console.WriteLine("  " + entry);
            }

            return 1;
        }

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"All doc links resolve ({files.Count} files checked)."));
        return 0;
    }

    // An explicit root argument wins; otherwise walk up from the current directory (then the tool's own
    // location) to the repo root — the directory that holds `.git`. A game-agnostic marker (this engine
    // tool must not know the game's solution name); `.git` is a directory in a normal clone and a file in a
    // worktree or submodule, so Path.Exists (which matches either) is used.
    private static string? ResolveRoot(string[] args)
    {
        if (args.Length > 0 && Directory.Exists(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (Path.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    return dir.FullName;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> MarkdownFiles(string root)
        => Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(path => !IsSkipped(path));

    private static bool IsSkipped(string path)
    {
        var normalized = path.Replace('\\', '/');
        return SkipDirectories.Any(dir => normalized.Contains("/" + dir + "/", StringComparison.Ordinal));
    }

    // The GitHub-style anchor slug → heading-text map a file's headings define (duplicate slugs get -1,
    // -2, … suffixes, matching how the links resolve), skipping fenced code blocks so a '#' inside a code
    // sample is not read as a heading. The key set is the file's valid anchors; the values let the symbol
    // check see what a given section is titled.
    private static Dictionary<string, string> HeadingsOf(string file)
    {
        var headings = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var inFence = false;

        foreach (var line in File.ReadLines(file))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            var match = HeadingPattern().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var text = match.Groups[2].Value;
            var slug = Slug(text);
            var count = seen.GetValueOrDefault(slug);
            headings[count == 0 ? slug : string.Create(CultureInfo.InvariantCulture, $"{slug}-{count}")] = text;
            seen[slug] = count + 1;
        }

        return headings;
    }

    // GitHub's heading-to-anchor rule: strip inline-link syntax and emphasis markers, lowercase, keep only
    // letters, digits, hyphens, underscores, and spaces (dropping other punctuation, including em dashes),
    // then turn spaces into hyphens.
    private static string Slug(string text)
    {
        var stripped = InlineLinkPattern().Replace(text, "$1");
        var builder = new StringBuilder(stripped.Length);
        foreach (var raw in stripped)
        {
            var ch = char.ToLowerInvariant(raw);
            if (ch is '`' or '*')
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch) || ch is '-' or '_' or ' ')
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Trim().Replace(' ', '-');
    }

    private static void CheckFile(string file, IReadOnlyDictionary<string, HashSet<string>> anchorsByFile,
        IReadOnlyDictionary<string, Dictionary<string, string>> headingsByFile, IReadOnlySet<string> guideFiles,
        List<string> broken)
    {
        var directory = Path.GetDirectoryName(file)!;
        var inFence = false;
        var lineNumber = 0;

        foreach (var line in File.ReadLines(file))
        {
            lineNumber++;
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            var scannable = InlineCodePattern().Replace(line, string.Empty);
            foreach (Match match in LinkPattern().Matches(scannable))
            {
                var problem = Validate(match.Groups[1].Value, file, directory, anchorsByFile);
                if (problem is not null)
                {
                    broken.Add(string.Create(CultureInfo.InvariantCulture, $"{Rel(file)}:{lineNumber}: {problem}"));
                }
            }

            // Symbol links ([`X`](y)) are scanned on the raw line — the inline-code strip above would erase
            // the very backticks that mark them.
            foreach (Match match in SymbolLinkPattern().Matches(line))
            {
                var problem = ValidateSymbol(match.Groups[1].Value, match.Groups[2].Value, file, directory,
                    guideFiles, headingsByFile);
                if (problem is not null)
                {
                    broken.Add(string.Create(CultureInfo.InvariantCulture, $"{Rel(file)}:{lineNumber}: {problem}"));
                }
            }
        }
    }

    // Returns a description of why a link target does not resolve, or null when it is fine (or external).
    private static string? Validate(string target, string file, string directory,
        IReadOnlyDictionary<string, HashSet<string>> anchorsByFile)
    {
        if (IsExternal(target))
        {
            return null;
        }

        var hash = target.IndexOf('#');
        var filePart = hash < 0 ? target : target[..hash];
        var fragment = hash < 0 ? string.Empty : target[(hash + 1)..];

        if (filePart.Length == 0)
        {
            // A pure "#anchor" link into the same file.
            return fragment.Length > 0 && !anchorsByFile[file].Contains(fragment)
                ? $"no anchor '#{fragment}' in this file"
                : null;
        }

        var resolved = Path.GetFullPath(Path.Combine(directory, filePart));
        return !File.Exists(resolved) && !Directory.Exists(resolved)
            ? $"missing target '{filePart}'"
            : fragment.Length > 0 && resolved.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            && anchorsByFile.TryGetValue(resolved, out var anchors) && !anchors.Contains(fragment)
            ? $"no anchor '#{fragment}' in {Rel(resolved)}"
            : null;
    }

    // A [`Symbol`](target) link should land where that code element is documented. Two failure modes it
    // catches that plain resolution cannot: the element's name appears *nowhere* in the target file (a
    // dangling symbol — usually a rename or the wrong file), or the link points into a solution *guide*
    // section that is not about it (the reference lives in a per-project README, and the guide only mentions
    // the element in passing — the "takes you to the page but not the right place" failure). Returns a
    // description of the problem, or null when the link is fine, external, names a file rather than a code
    // element, or has an unresolved target (left to Validate to report, so it is not double-reported).
    private static string? ValidateSymbol(string text, string target, string file, string directory,
        IReadOnlySet<string> guideFiles, IReadOnlyDictionary<string, Dictionary<string, string>> headingsByFile)
    {
        if (IsExternal(target) || text.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var hash = target.IndexOf('#');
        var filePart = hash < 0 ? target : target[..hash];
        var fragment = hash < 0 ? string.Empty : target[(hash + 1)..];

        string resolved;
        if (filePart.Length == 0)
        {
            resolved = file;
        }
        else
        {
            resolved = Path.GetFullPath(Path.Combine(directory, filePart));
            if (!File.Exists(resolved))
            {
                return null;
            }
        }

        if (!resolved.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // The base identifier (drop generic args and call parens) and its trailing member segment, so
        // `NetworkedBundle.MakeDeterministic` and `Documents<T>()` compare on names that actually appear.
        var baseId = text;
        var cut = baseId.IndexOfAny(['<', '(']);
        if (cut >= 0)
        {
            baseId = baseId[..cut];
        }

        baseId = baseId.Trim();
        var leaf = baseId.Contains('.') ? baseId[(baseId.LastIndexOf('.') + 1)..] : baseId;

        var content = File.ReadAllText(resolved);
        if (!content.Contains(baseId, StringComparison.Ordinal) && !content.Contains(leaf, StringComparison.Ordinal))
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"symbol '{text}' appears nowhere in {Rel(resolved)} — link to where it is documented");
        }

        if (guideFiles.Contains(resolved) && fragment.Length > 0
            && headingsByFile.TryGetValue(resolved, out var headings)
            && headings.TryGetValue(fragment, out var heading)
            && !heading.Contains(baseId, StringComparison.Ordinal)
            && !heading.Contains(leaf, StringComparison.Ordinal))
        {
            var problem = string.Create(CultureInfo.InvariantCulture,
                $"symbol '{text}' points into guide section '{heading}', not its reference");
            return problem + " — link to the per-project README that documents it";
        }

        return null;
    }

    private static bool IsExternal(string target)
        => target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
        || target.StartsWith("tel:", StringComparison.OrdinalIgnoreCase);

    private static string Rel(string path)
        => Path.GetRelativePath(Directory.GetCurrentDirectory(), path).Replace('\\', '/');
}
