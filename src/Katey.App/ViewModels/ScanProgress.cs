using System.Globalization;
using Katey.Journal;

namespace Katey.App.ViewModels;

/// <summary>
/// The current system's scan progress in words and as a share, the same on the home page, the system page
/// and the rail. A body scanned on an earlier visit is scanned, as the game counts it, so every figure here
/// follows <see cref="SystemSnapshot.BodiesKnown"/>; the journal's missing details are a separate note.
/// </summary>
public static class ScanProgress
{
    /// <summary>Words the progress briefly, for a heading.</summary>
    /// <param name="snapshot">The current system.</param>
    /// <returns>"7 of 8 scanned" after a honk gives the count, "2 scanned" before it, and empty when nothing is scanned.</returns>
    public static string Heading(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var scanned = snapshot.BodiesScanned;
        return snapshot.BodyCount is { } count
            ? $"{snapshot.BodiesKnown ?? scanned} of {count} scanned"
            : scanned > 0 ? $"{scanned} scanned" : "";
    }

    /// <summary>Words the progress in full, for a page.</summary>
    /// <param name="snapshot">The current system.</param>
    /// <returns>
    /// "7 of 8 bodies scanned", with ", all found" once the game says so. Before a honk, the scans alone with
    /// a prompt to honk, or "Nothing scanned yet.".
    /// </returns>
    public static string Sentence(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var scanned = snapshot.BodiesScanned;
        if (snapshot.BodyCount is not { } count)
        {
            return scanned > 0 ? $"{scanned} bodies scanned; honk for the count" : "Nothing scanned yet.";
        }

        var text = $"{snapshot.BodiesKnown ?? scanned} of {count} bodies scanned";
        return snapshot.AllBodiesFound ? text + ", all found" : text;
    }

    /// <summary>Words the known count as a figure, for a stat tile.</summary>
    /// <param name="snapshot">The current system, after a honk.</param>
    /// <returns>"7/8": bodies the game knows over the body count.</returns>
    public static string Figure(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return string.Create(CultureInfo.InvariantCulture, $"{snapshot.BodiesKnown ?? snapshot.BodiesScanned}/{snapshot.BodyCount ?? 0}");
    }

    /// <summary>Gets the share of the system's bodies the game knows, 0 to 1.</summary>
    /// <param name="snapshot">The current system.</param>
    /// <returns>Known bodies over the body count, or 0 before a honk gives the count.</returns>
    public static double Fraction(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.BodyCount is { } count && count > 0
            ? Math.Clamp((snapshot.BodiesKnown ?? snapshot.BodiesScanned) / (double)count, 0, 1)
            : 0;
    }

    /// <summary>Notes the bodies the journal has no details for: scanned on an earlier visit, so never described again.</summary>
    /// <param name="snapshot">The current system.</param>
    /// <returns>"No journal details for 7 bodies scanned on an earlier visit.", or empty when there are none.</returns>
    public static string EarlierNote(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var earlier = snapshot.BodiesKnownEarlier;
        return earlier == 0 ? "" : $"No journal details for {Display.Plural(earlier, "body", "bodies")} scanned on an earlier visit.";
    }
}
