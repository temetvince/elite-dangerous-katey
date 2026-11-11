using System.Net;
using Katey.Core.Routing;
using Microsoft.Extensions.Logging;

namespace Katey.App.Routing;

/// <summary>Asks Spansh's galaxy plotter for a route.</summary>
/// <remarks>
/// The request is one form post of the fields <see cref="PlotRequest.FormFields"/> lists: the two system
/// names and the ship's jump numbers, nothing else. Spansh answers with a job id, and the job is polled once
/// a second until it is done, fails, or <see cref="MaxWait"/> passes. The client is used by one plot at a
/// time; the caller serialises plots.
/// </remarks>
public sealed class SpanshPlotter : IRoutePlotter
{
    /// <summary>Where a plot is submitted.</summary>
    public const string SubmitUrl = "https://spansh.co.uk/api/generic/route";

    /// <summary>Where a job's result is read, with the job id appended.</summary>
    public const string ResultsUrl = "https://spansh.co.uk/api/results/";

    private readonly HttpClient _http;
    private readonly ILogger<SpanshPlotter> _logger;

    /// <summary>Creates the plotter over an HTTP client.</summary>
    /// <param name="http">The client. Not disposed here. Its user agent is replaced by <see cref="AppVersion.UserAgent"/>.</param>
    /// <param name="logger">Where requests and failures go.</param>
    public SpanshPlotter(HttpClient http, ILogger<SpanshPlotter> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(logger);
        _http = http;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.Clear();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(AppVersion.UserAgent);
    }

    /// <summary>Gets or sets how long to wait between polls of a queued job.</summary>
    /// <value>Positive. One second unless changed.</value>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets or sets how long a job may stay queued before the plot gives up.</summary>
    /// <value>Positive. Ninety seconds unless changed.</value>
    public TimeSpan MaxWait { get; set; } = TimeSpan.FromSeconds(90);

    /// <inheritdoc/>
    public async Task<PlottedRoute> PlotAsync(PlotRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var startedAt = DateTimeOffset.UtcNow;
        Log.PlotSubmitted(_logger, request.Source, request.Destination);
        PlotJob job;
        try
        {
            using var content = new FormUrlEncodedContent(request.FormFields());
            using var response = await _http.PostAsync(new Uri(SubmitUrl), content, cancellationToken).ConfigureAwait(false);
            job = await ReadAsync(response, request, startedAt, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw Unreachable(exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw Unreachable(exception);
        }

        var deadline = DateTimeOffset.UtcNow + MaxWait;
        while (job.Status == PlotJobStatus.Queued)
        {
            if (job.Job is null)
            {
                throw new RoutePlotException(PlotFailure.NoJob);
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new RoutePlotException(PlotFailure.StillWorking, waited: MaxWait);
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
            try
            {
                using var poll = await _http.GetAsync(new Uri(ResultsUrl + Uri.EscapeDataString(job.Job)), cancellationToken).ConfigureAwait(false);
                job = await ReadAsync(poll, request, startedAt, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                throw Unreachable(exception);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                throw Unreachable(exception);
            }
        }

        if (job.Status == PlotJobStatus.Failed || job.Route is null)
        {
            Log.PlotRefused(_logger, request.Source, request.Destination, job.Error ?? "no route");
            throw new RoutePlotException(PlotFailure.Rejected, job.Error);
        }

        Log.PlotReceived(_logger, request.Source, request.Destination, job.Route.Jumps, (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
        return job.Route;
    }

    private static RoutePlotException Unreachable(Exception exception)
        => new(PlotFailure.Unreachable, innerException: exception);

    private static async Task<PlotJob> ReadAsync(HttpResponseMessage response, PlotRequest request, DateTimeOffset startedAt, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var job = SpanshJson.Read(body, request.Source, request.Destination, startedAt);
        return job.Status == PlotJobStatus.Failed && !response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Accepted
            ? throw new RoutePlotException(PlotFailure.Rejected, job.Error, (int)response.StatusCode)
            : job;
    }
}
