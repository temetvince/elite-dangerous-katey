using System.Globalization;
using System.Text.Json;
using Katey.Core.Exploration;

namespace Katey.Core.Routing;

/// <summary>Reads the bodies out of Spansh's dump of one system, as its <c>api/dump</c> endpoint returns it. No IO.</summary>
public static class SpanshBodiesJson
{
    private const double StandardPressurePa = 101325.0;

    /// <summary>Reads a system dump's bodies.</summary>
    /// <param name="json">The reply text: an object with a <c>system</c> holding <c>bodies</c>.</param>
    /// <returns>Every star and planet, in the dump's order; barycentres and anything without a body id are left out.</returns>
    /// <exception cref="JsonException">The text is not JSON.</exception>
    public static IReadOnlyList<CataloguedBody> Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var system = root.TryGetProperty("system", out var inner) && inner.ValueKind == JsonValueKind.Object ? inner : root;
        if (!system.TryGetProperty("bodies", out var bodies) || bodies.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<CataloguedBody>();
        foreach (var body in bodies.EnumerateArray())
        {
            if (body.ValueKind != JsonValueKind.Object || ReadBody(body) is not { } read)
            {
                continue;
            }

            list.Add(read);
        }

        return list;
    }

    private static CataloguedBody? ReadBody(JsonElement body)
    {
        var type = Text(body, "type");
        var isStar = string.Equals(type, "Star", StringComparison.OrdinalIgnoreCase);
        if (!isStar && !string.Equals(type, "Planet", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!body.TryGetProperty("bodyId", out var idElement) || !idElement.TryGetInt32(out var bodyId) || Text(body, "name") is not { Length: > 0 } name)
        {
            return null;
        }

        var subType = Text(body, "subType");
        var signals = body.TryGetProperty("signals", out var signalsElement) && signalsElement.ValueKind == JsonValueKind.Object ? signalsElement : (JsonElement?)null;
        var counts = signals is { } s && s.TryGetProperty("signals", out var countsElement) && countsElement.ValueKind == JsonValueKind.Object ? countsElement : (JsonElement?)null;
        var genera = new List<string>();
        if (signals is { } g && g.TryGetProperty("genuses", out var generaElement) && generaElement.ValueKind == JsonValueKind.Array)
        {
            genera.AddRange(generaElement.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!));
        }

        var materials = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (body.TryGetProperty("materials", out var materialsElement) && materialsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var material in materialsElement.EnumerateObject())
            {
                if (material.Value.TryGetDouble(out var share))
                {
                    materials[material.Name] = share;
                }
            }
        }

        return new CataloguedBody(
            bodyId,
            name,
            isStar,
            isStar ? null : ParentStar(body),
            subType is null ? null : isStar ? CatalogueNames.StarClass(subType) : CatalogueNames.PlanetClass(subType),
            Number(body, "distanceToArrival") ?? 0,
            body.TryGetProperty("isLandable", out var landable) && landable.ValueKind == JsonValueKind.True,
            Text(body, "atmosphereType") ?? "",
            Text(body, "volcanismType") ?? "",
            isStar ? null : Number(body, "gravity"),
            Number(body, "surfaceTemperature"),
            Number(body, "surfacePressure") is { } atm ? atm * StandardPressurePa : null,
            isStar ? Number(body, "solarMasses") : Number(body, "earthMasses"),
            string.Equals(Text(body, "terraformingState"), "Candidate for terraforming", StringComparison.OrdinalIgnoreCase),
            body.TryGetProperty("rings", out var rings) && rings.ValueKind == JsonValueKind.Array && rings.GetArrayLength() > 0,
            materials,
            Count(counts, "$SAA_SignalType_Biological;"),
            Count(counts, "$SAA_SignalType_Geological;"),
            genera,
            Text(body, "updateTime") is { } updated && DateTimeOffset.TryParse(updated, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at) ? at : null);
    }

    private static int? ParentStar(JsonElement body)
    {
        if (!body.TryGetProperty("parents", out var parents) || parents.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var parent in parents.EnumerateArray())
        {
            if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty("Star", out var star) && star.TryGetInt32(out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static string? Text(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static double? Number(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) ? number : null;

    private static int Count(JsonElement? counts, string key)
        => counts is { } element && element.TryGetProperty(key, out var value) && value.TryGetInt32(out var count) ? count : 0;
}
