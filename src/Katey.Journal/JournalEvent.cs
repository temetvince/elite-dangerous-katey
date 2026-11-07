using System.Globalization;
using System.Text.Json;

namespace Katey.Journal;

/// <summary>One line of a journal file.</summary>
/// <param name="Timestamp">The event's <c>timestamp</c>, in UTC.</param>
/// <param name="Name">The event's <c>event</c> field, such as <c>Materials</c>.</param>
/// <param name="Payload">The whole line as a detached JSON element, valid for the record's lifetime.</param>
public sealed record JournalEvent(DateTimeOffset Timestamp, string Name, JsonElement Payload)
{
    /// <summary>Parses one journal line.</summary>
    /// <param name="line">The line, with or without its trailing newline.</param>
    /// <returns>The event, or <see langword="null"/> when the line is blank, not JSON, or lacks a string <c>event</c> and a parsable <c>timestamp</c>.</returns>
    public static JournalEvent? Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            return root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("event", out var name)
                || name.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("timestamp", out var stamp)
                || stamp.ValueKind != JsonValueKind.String
                || !DateTimeOffset.TryParse(stamp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp)
                ? null
                : new JournalEvent(timestamp, name.GetString()!, root.Clone());
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
