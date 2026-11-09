using System.Globalization;
using System.Net;
using System.Reflection;
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

    /// <summary>Where Spansh, or anyone reading their logs, can find out what Katey is.</summary>
    public const string ProjectUrl = "https://github.com/temetvince/elite-dangerous-katey";

    /// <summary>
    /// Gets the user agent every request carries, as Spansh asks of tools that use its API: Katey's name, its
    /// version and the project's address, such as <c>Katey/1.2.0 (+https://github.com/temetvince/elite-dangerous-katey)</c>.
    /// The version is the assembly's informational version with any build metadata dropped.
    /// </summary>
    public static string UserAgent { get; } = $"Katey/{Version()} (+{ProjectUrl})";

    private readonly HttpClient _http;
    private readonly ILogger<SpanshPlotter> _logger;

    /// <summary>Creates the plotter over an HTTP client.</summary>
    /// <param name="http">The client. Not disposed here. Its user agent is replaced by <see cref="UserAgent"/>.</param>
    /// <param name="logger">Where requests and failures go.</param>
    public SpanshPlotter(HttpClient http, ILogger<SpanshPlotter> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(logger);
        _http = http;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.Clear();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    private static string Version()
    {
        var informational = typeof(SpanshPlotter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus > 0 ? informational[..plus] : informational;
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
                throw new RoutePlotException("Spansh accepted the plot but gave no job to wait for.");
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new RoutePlotException($"Spansh is still working after {MaxWait.TotalSeconds:0} seconds. Try again later.");
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
            throw new RoutePlotException(job.Error ?? "Spansh could not plot that route.");
        }

        Log.PlotReceived(_logger, request.Source, request.Destination, job.Route.Jumps, (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
        return job.Route;
    }

    private static RoutePlotException Unreachable(Exception exception)
        => new("Spansh could not be reached. Check the connection and try again.", exception);

    private static async Task<PlotJob> ReadAsync(HttpResponseMessage response, PlotRequest request, DateTimeOffset startedAt, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var job = SpanshJson.Read(body, request.Source, request.Destination, startedAt);
        if (job.Status == PlotJobStatus.Failed && !response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Accepted)
        {
            var code = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
            return job with { Error = $"{job.Error} (Spansh answered HTTP {code}.)" };
        }

        return job;
    }
}
