using System.Globalization;
using System.Text.Json;

namespace Katey.Journal;

/// <summary>Tolerant readers for the loosely typed fields journal documents carry.</summary>
public static class JsonRead
{
    /// <summary>A string property, or <see langword="null"/> when absent or not a string.</summary>
    /// <param name="element">An object.</param>
    /// <param name="property">Property name, case-sensitive as the journal spells it.</param>
    /// <returns>The value or <see langword="null"/>.</returns>
    public static string? Text(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>An integer property, or 0 when absent or not a whole number.</summary>
    /// <param name="element">An object.</param>
    /// <param name="property">Property name.</param>
    /// <returns>The value or 0.</returns>
    public static int Count(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : 0;

    /// <summary>A 64-bit integer property, or 0 when absent or not a whole number.</summary>
    /// <param name="element">An object.</param>
    /// <param name="property">Property name.</param>
    /// <returns>The value or 0.</returns>
    public static long Identifier(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number)
            ? number
            : 0;

    /// <summary>A number property, or <see langword="null"/> when absent or not a number.</summary>
    /// <param name="element">An object.</param>
    /// <param name="property">Property name.</param>
    /// <returns>The value or <see langword="null"/>.</returns>
    public static double? Measure(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;

    /// <summary>A boolean property, or <see langword="false"/> when absent or not a boolean.</summary>
    /// <param name="element">An object.</param>
    /// <param name="property">Property name.</param>
    /// <returns>The value or <see langword="false"/>.</returns>
    public static bool Flag(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            && value.GetBoolean();

    /// <summary>An array property's elements, or none when absent or not an array.</summary>
    /// <param name="element">An object.</param>
    /// <param name="property">Property name.</param>
    /// <returns>The elements in order.</returns>
    public static IEnumerable<JsonElement> Items(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
            : [];

    /// <summary>An object property, or <see langword="null"/> when absent or not an object.</summary>
    /// <param name="element">An object.</param>
    /// <param name="property">Property name.</param>
    /// <returns>The object or <see langword="null"/>.</returns>
    public static JsonElement? Child(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : null;

    /// <summary>The document's <c>timestamp</c>, in UTC.</summary>
    /// <param name="element">An object.</param>
    /// <returns>The time, or <see langword="null"/> when absent or unparsable.</returns>
    public static DateTimeOffset? Timestamp(JsonElement element)
        => Text(element, "timestamp") is { } stamp
            && DateTimeOffset.TryParse(stamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;

    /// <summary>
    /// An item symbol property in its canonical form: lower case, without the <c>$</c> prefix and
    /// <c>_name;</c> suffix some events wrap it in.
    /// </summary>
    /// <param name="element">An object.</param>
    /// <param name="property">Property name.</param>
    /// <returns>
    /// The symbol, or <see langword="null"/> when absent, not a string, or empty once unwrapped. The same
    /// item spelt <c>Int_CargoRack_Size3_Class1</c> by one event and <c>$int_cargorack_size3_class1_name;</c>
    /// by another gives the same result.
    /// </returns>
    public static string? Symbol(JsonElement element, string property)
        => Text(element, property) is { } text ? NormalizeSymbol(text) : null;

    /// <summary>Puts an item symbol in its canonical form.</summary>
    /// <param name="symbol">The symbol as an event spells it.</param>
    /// <returns>Lower case, trimmed, without a leading <c>$</c> or a trailing <c>_name;</c>. <see langword="null"/> when nothing is left.</returns>
    public static string? NormalizeSymbol(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        var span = symbol.AsSpan().Trim();
        if (span.StartsWith("$"))
        {
            span = span[1..];
        }

        if (span.EndsWith("_name;", StringComparison.OrdinalIgnoreCase))
        {
            span = span[..^"_name;".Length];
        }

        return span.IsEmpty ? null : span.ToString().ToUpperInvariant().ToLowerInvariant();
    }

    /// <summary>The localised form of a name when the journal provides one, else the raw name.</summary>
    /// <param name="element">An object with <c>property</c> and optionally <c>property_Localised</c>.</param>
    /// <param name="property">Property name.</param>
    /// <returns>Display text, or <see langword="null"/> when neither is present.</returns>
    public static string? Localised(JsonElement element, string property)
        => Text(element, property + "_Localised") ?? Text(element, property);
}
