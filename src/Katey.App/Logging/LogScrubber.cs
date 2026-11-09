using System.Text.RegularExpressions;

namespace Katey.App.Logging;

/// <summary>
/// Takes what identifies the player's machine out of a log line before it is written. Any file path, Windows
/// or POSIX, drive or network share, keeps only its last segment, so a folder name never reaches the file;
/// the account name and the machine name become placeholders. The rules are fixed when the scrubber is made,
/// so it is safe to share between threads.
/// </summary>
public sealed partial class LogScrubber
{

    private readonly IReadOnlyList<(Regex Find, string Replace)> _rules;

    /// <summary>A Windows path, by drive or network share. Folders may hold spaces, since a slash bounds each one; the final segment may not, so the words after a path are left alone.</summary>
    [GeneratedRegex(@"(?<![\w])(?:[A-Za-z]:|\\\\[^\\/\s""'<>|]+)[\\/](?:[^\\/\r\n""'<>|]+[\\/])*([^\\/\s""'<>|]*)", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsPath();

    /// <summary>A POSIX path under one of the folders a user's files live in.</summary>
    [GeneratedRegex(@"(?<![\w])/(?:home|Users|root|mnt|media|tmp|var|opt)/(?:[^/\r\n""'<>|]+/)*([^/\s""'<>|]*)", RegexOptions.CultureInvariant)]
    private static partial Regex PosixPath();

    /// <summary>Creates a scrubber from explicit rules.</summary>
    /// <param name="rules">Each text to find, matched without regard to case, and what to write instead. Empty texts are skipped.</param>
    public LogScrubber(IEnumerable<(string Find, string Replace)> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = [.. rules
            .Where(rule => !string.IsNullOrEmpty(rule.Find))
            .Select(rule => (new Regex(Regex.Escape(rule.Find), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), rule.Replace))];
    }

    /// <summary>
    /// The scrubber for the machine the process runs on: paths lose their folders, the account name becomes
    /// <c>~user</c> and the machine name <c>~machine</c>. A name shorter than three characters is left alone,
    /// since replacing it would mangle ordinary words.
    /// </summary>
    /// <returns>The scrubber.</returns>
    public static LogScrubber ForThisMachine()
    {
        var rules = new List<(string, string)>();
        var user = Environment.UserName;
        if (user.Length >= 3)
        {
            rules.Add((user, "~user"));
        }

        var machine = Environment.MachineName;
        if (machine.Length >= 3)
        {
            rules.Add((machine, "~machine"));
        }

        return new LogScrubber(rules);
    }

    /// <summary>Scrubs a text: every path keeps only its last segment, written as <c>…/name</c>, and then the rules apply.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The scrubbed text; unchanged when nothing matches.</returns>
    public string Scrub(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        text = WindowsPath().Replace(text, "…/$1");
        text = PosixPath().Replace(text, "…/$1");
        foreach (var (find, replace) in _rules)
        {
            text = find.Replace(text, replace);
        }

        return text;
    }
}
