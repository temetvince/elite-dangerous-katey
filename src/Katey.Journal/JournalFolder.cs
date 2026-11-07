using System.Globalization;
using System.Text.RegularExpressions;

namespace Katey.Journal;

/// <summary>Finds the game's journal folder and lists the journal files in it.</summary>
public static partial class JournalFolder
{
    private const string FrontierSubPath = "Frontier Developments/Elite Dangerous";

    /// <summary>
    /// Folders the game is known to write journals to on this machine, most likely first: the Windows
    /// "Saved Games" location, then the Proton prefixes Steam uses on Linux. Not every candidate exists.
    /// </summary>
    /// <returns>Absolute paths.</returns>
    public static IReadOnlyList<string> Candidates()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new List<string>
        {
            Path.Combine(home, "Saved Games", FrontierSubPath),
        };

        foreach (var steam in new[]
        {
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
        })
        {
            candidates.Add(Path.Combine(
                steam, "steamapps", "compatdata", "359320", "pfx", "drive_c", "users", "steamuser", "Saved Games", FrontierSubPath));
        }

        return [.. candidates.Select(path => path.Replace('/', Path.DirectorySeparatorChar))];
    }

    /// <summary>The first candidate folder that exists.</summary>
    /// <returns>An absolute path, or <see langword="null"/> when none exists.</returns>
    public static string? FindDefault() => Candidates().FirstOrDefault(Directory.Exists);

    /// <summary>
    /// Journal files in a folder, oldest first: by the start time each file's name carries, so the history
    /// replays in the order it happened even when a sync or a restore has disturbed the files' write times.
    /// </summary>
    /// <param name="folder">An existing folder.</param>
    /// <returns>Absolute paths of every <c>Journal.*.log</c>. A file whose name carries no start time sorts by its last write time. Files with the same start time sort by name.</returns>
    /// <exception cref="DirectoryNotFoundException"><paramref name="folder"/> does not exist.</exception>
    public static IReadOnlyList<string> JournalFiles(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        return
        [
            .. new DirectoryInfo(folder)
                .EnumerateFiles("Journal.*.log")
                .OrderBy(file => StartedAt(file.Name) ?? file.LastWriteTimeUtc)
                .ThenBy(file => file.Name, StringComparer.Ordinal)
                .Select(file => file.FullName),
        ];
    }

    /// <summary>Whether a file name is a journal's, <c>Journal.*.log</c>, as opposed to one of the live files the game rewrites whole.</summary>
    /// <param name="fileName">A file name or path.</param>
    /// <returns><see langword="true"/> for a journal file name, in any letter case.</returns>
    public static bool IsJournal(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        var name = Path.GetFileName(fileName);
        return name.StartsWith("Journal.", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".log", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>When a journal file was started, read from its name.</summary>
    /// <param name="fileName">A file name or path in either format the game has used: <c>Journal.2026-11-05T100000.01.log</c> or <c>Journal.251104100000.01.log</c>.</param>
    /// <returns>The start time the name carries, on the clock the game wrote it with, or <see langword="null"/> when the name is in neither format.</returns>
    public static DateTime? StartedAt(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        var match = NamePattern().Match(Path.GetFileName(fileName));
        if (!match.Success)
        {
            return null;
        }

        var stamp = match.Groups[1].Value;
        var format = stamp.Length == 17 ? "yyyy-MM-dd'T'HHmmss" : "yyMMddHHmmss";
        return DateTime.TryParseExact(stamp, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var started)
            ? started
            : null;
    }

    [GeneratedRegex(@"^Journal\.([0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{6}|[0-9]{12})\.[0-9]+\.log$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern();
}
