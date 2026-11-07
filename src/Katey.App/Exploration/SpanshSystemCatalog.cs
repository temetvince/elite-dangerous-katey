using System.Globalization;
using System.Net;
using System.Text.Json;
using Katey.App.Routing;
using Katey.Core.Exploration;
using Katey.Core.Routing;
using Microsoft.Extensions.Logging;

namespace Katey.App.Exploration;

/// <summary>Asks Spansh for a system's bodies: one GET of its system dump, read by <see cref="SpanshBodiesJson"/>.</summary>
/// <remarks>The request carries only the system's id, and the user agent <see cref="SpanshPlotter.UserAgent"/> that names Katey.</remarks>
public sealed class SpanshSystemCatalog : ISystemCatalog
{
    /// <summary>Where a system's dump is read, with the system id appended.</summary>
    public const string DumpUrl = "https://spansh.co.uk/api/dump/";

    private readonly HttpClient _http;
    private readonly ILogger<SpanshSystemCatalog> _logger;

    /// <summary>Creates the catalog over an HTTP client.</summary>
    /// <param name="http">The client. Not disposed here. Its user agent is replaced by <see cref="SpanshPlotter.UserAgent"/>.</param>
    /// <param name="logger">Where lookups and failures go.</param>
    public SpanshSystemCatalog(HttpClient http, ILogger<SpanshSystemCatalog> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(logger);
        _http = http;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.Clear();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(SpanshPlotter.UserAgent);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CataloguedBody>> BodiesAsync(long systemAddress, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(new Uri(DumpUrl + systemAddress.ToString(CultureInfo.InvariantCulture)), cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.SystemLookedUp(_logger, systemAddress, 0);
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new SystemCatalogException($"Spansh answered {(int)response.StatusCode} for the system lookup.");
            }

            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var bodies = SpanshBodiesJson.Read(text);
            Log.SystemLookedUp(_logger, systemAddress, bodies.Count);
            return bodies;
        }
        catch (HttpRequestException exception)
        {
            throw new SystemCatalogException("Spansh could not be reached for the system lookup.", exception);
        }
        catch (JsonException exception)
        {
            throw new SystemCatalogException("Spansh's answer to the system lookup could not be read.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SystemCatalogException("Spansh took too long to answer the system lookup.", exception);
        }
    }
}
