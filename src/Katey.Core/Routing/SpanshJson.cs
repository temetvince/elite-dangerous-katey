using System.Text.Json;

namespace Katey.Core.Routing;

/// <summary>What a plotting job is doing.</summary>
public enum PlotJobStatus
{
    /// <summary>Still being worked out.</summary>
    Queued,

    /// <summary>Finished, with a route.</summary>
    Done,

    /// <summary>Gave up, with a reason.</summary>
    Failed,
}

/// <summary>One reading of a plotting job.</summary>
/// <param name="Status">What the job is doing.</param>
/// <param name="Job">The job's id, or <see langword="null"/> when the reply carries none.</param>
/// <param name="Error">Why it failed, or <see langword="null"/>.</param>
/// <param name="Route">The route, once <paramref name="Status"/> is <see cref="PlotJobStatus.Done"/>; else <see langword="null"/>.</param>
public sealed record PlotJob(PlotJobStatus Status, string? Job, string? Error, PlottedRoute? Route);

/// <summary>Reads the JSON Spansh's galaxy plotter answers with. Does no IO.</summary>
public static class SpanshJson
{
    /// <summary>Reads a submission or results reply.</summary>
    /// <param name="json">The reply body.</param>
    /// <param name="source">The departure system asked for, kept on the route.</param>
    /// <param name="destination">The destination asked for, kept on the route.</param>
    /// <param name="plottedAt">When the plot was asked for, kept on the route.</param>
    /// <returns>
    /// The job. A body with <c>status</c> <c>queued</c> is queued; <c>ok</c> with a <c>result.jumps</c> list is
    /// done; anything else is failed, with the body's <c>error</c> or a note on what was wrong with the body.
    /// </returns>
    public static PlotJob Read(string json, string source, string destination, DateTimeOffset plottedAt)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new PlotJob(PlotJobStatus.Failed, null, "The plotter's reply was not a JSON object.", null);
            }

            var job = Text(root, "job");
            var error = Text(root, "error");
            var status = Text(root, "status");
            if (error is not null)
            {
                return new PlotJob(PlotJobStatus.Failed, job, error, null);
            }

            if (string.Equals(status, "queued", StringComparison.OrdinalIgnoreCase))
            {
                return new PlotJob(PlotJobStatus.Queued, job, null, null);
            }

            if (!string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase)
                || !root.TryGetProperty("result", out var result)
                || result.ValueKind != JsonValueKind.Object
                || !result.TryGetProperty("jumps", out var jumps)
                || jumps.ValueKind != JsonValueKind.Array)
            {
                return new PlotJob(PlotJobStatus.Failed, job, $"The plotter answered with status \"{status ?? "none"}\" and no route.", null);
            }

            var stops = new List<PlottedStop>();
            foreach (var jump in jumps.EnumerateArray())
            {
                if (jump.ValueKind != JsonValueKind.Object || Text(jump, "name") is not { } name)
                {
                    continue;
                }

                stops.Add(new PlottedStop(
                    name,
                    Integer(jump, "id64"),
                    Number(jump, "x"),
                    Number(jump, "y"),
                    Number(jump, "z"),
                    Number(jump, "distance"),
                    Number(jump, "distance_to_destination"),
                    Number(jump, "fuel_used"),
                    Number(jump, "fuel_in_tank"),
                    Flag(jump, "has_neutron"),
                    Flag(jump, "is_scoopable"),
                    Flag(jump, "must_refuel")));
            }

            return stops.Count == 0
                ? new PlotJob(PlotJobStatus.Failed, job, "The plotter answered with an empty route.", null)
                : new PlotJob(PlotJobStatus.Done, job, null, new PlottedRoute(source, destination, plottedAt, stops));
        }
        catch (JsonException exception)
        {
            return new PlotJob(PlotJobStatus.Failed, null, $"The plotter's reply was not JSON: {exception.Message}", null);
        }
    }

    private static string? Text(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static double Number(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) ? number : 0;

    private static long Integer(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : 0;

    private static bool Flag(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && (value.ValueKind == JsonValueKind.True || (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number != 0));
}
