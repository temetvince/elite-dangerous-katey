using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Katey.App.Network;

/// <summary>Asks GitHub for the project's latest release: one GET of the releases API, read for its tag and its page.</summary>
/// <remarks>
/// The request carries nothing but the user agent <see cref="AppVersion.UserAgent"/> that names Katey and the
/// headers GitHub asks of API clients. GitHub's answer for the latest release leaves drafts and pre-releases out.
/// The whole lookup runs on the thread pool from its first instruction, so a check started on the UI thread
/// while the window is being built costs it nothing.
/// </remarks>
public sealed class GitHubReleaseFeed : IReleaseFeed
{
    /// <summary>Where the latest release is read.</summary>
    public const string LatestUrl = "https://api.github.com/repos/temetvince/elite-dangerous-katey/releases/latest";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly ILogger<GitHubReleaseFeed> _logger;

    /// <summary>Creates the feed over an HTTP client.</summary>
    /// <param name="http">The client. Not disposed here. Its user agent is replaced by <see cref="AppVersion.UserAgent"/>.</param>
    /// <param name="logger">Where lookups and failures go.</param>
    public GitHubReleaseFeed(HttpClient http, ILogger<GitHubReleaseFeed> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(logger);
        _http = http;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.Clear();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(AppVersion.UserAgent);
    }

    /// <summary>Reads a release from GitHub's JSON for one.</summary>
    /// <param name="json">The release object, with <c>tag_name</c> and <c>html_url</c>.</param>
    /// <returns>The release.</returns>
    /// <exception cref="ReleaseFeedException">The text is not a release object, or its tag is not a version; <see cref="ReleaseLookupFailure.Unreadable"/>.</exception>
    public static Release Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        GitHubRelease? release;
        try
        {
            release = JsonSerializer.Deserialize<GitHubRelease>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ReleaseFeedException(ReleaseLookupFailure.Unreadable, innerException: exception);
        }

        if (release?.Tag_Name is not { Length: > 0 } tag || release.Html_Url is not { Length: > 0 } page || !Uri.TryCreate(page, UriKind.Absolute, out var uri))
        {
            throw new ReleaseFeedException(ReleaseLookupFailure.Unreadable);
        }

        var text = tag.StartsWith('v') || tag.StartsWith('V') ? tag[1..] : tag;
        return Version.TryParse(text, out var version)
            ? new Release(version, text, uri)
            : throw new ReleaseFeedException(ReleaseLookupFailure.Unreadable);
    }

    /// <inheritdoc/>
    public Task<Release> LatestAsync(CancellationToken cancellationToken) => Task.Run(() => FetchAsync(cancellationToken), cancellationToken);

    private async Task<Release> FetchAsync(CancellationToken cancellationToken)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(LatestUrl));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new ReleaseFeedException(ReleaseLookupFailure.Http, (int)response.StatusCode);
            }

            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var release = Read(text);
            Log.ReleaseLookedUp(_logger, release.Text, started.ElapsedMilliseconds);
            return release;
        }
        catch (HttpRequestException exception)
        {
            throw new ReleaseFeedException(ReleaseLookupFailure.Unreachable, innerException: exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ReleaseFeedException(ReleaseLookupFailure.TimedOut, innerException: exception);
        }
    }

    /// <summary>The two fields read from GitHub's release object, named as GitHub spells them.</summary>
    private sealed record GitHubRelease(string? Tag_Name, string? Html_Url);
}
