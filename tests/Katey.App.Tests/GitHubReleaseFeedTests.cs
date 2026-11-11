using System.Net;
using Katey.App.Network;
using Microsoft.Extensions.Logging.Abstractions;

namespace Katey.App.Tests;

/// <summary>The GitHub client: what it sends, what it reads, and how each failure is reported.</summary>
public sealed class GitHubReleaseFeedTests
{
    private const string Reply = /*lang=json,strict*/ """{"url":"https://api.github.com/repos/temetvince/elite-dangerous-katey/releases/1","html_url":"https://github.com/temetvince/elite-dangerous-katey/releases/tag/v1.4.0","tag_name":"v1.4.0","name":"Katey v1.4.0","draft":false,"prerelease":false}""";

    [Fact]
    public async Task LatestAsync_AsksTheLatestReleaseEndpoint_NamingKatey()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Reply) });
        using var http = new HttpClient(handler);
        var feed = new GitHubReleaseFeed(http, NullLogger<GitHubReleaseFeed>.Instance);

        var release = await feed.LatestAsync(CancellationToken.None);

        Assert.Equal(new Version(1, 4, 0), release.Version);
        Assert.Equal("1.4.0", release.Text);
        Assert.Equal("https://github.com/temetvince/elite-dangerous-katey/releases/tag/v1.4.0", release.Page.ToString());
        var request = Assert.Single(handler.Requests);
        Assert.Equal(GitHubReleaseFeed.LatestUrl, request.Url);
        Assert.Equal(AppVersion.UserAgent, request.UserAgent);
        Assert.Contains("application/vnd.github+json", request.Accept, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LatestAsync_ReportsAnHttpFailureWithItsCode()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        using var http = new HttpClient(handler);
        var feed = new GitHubReleaseFeed(http, NullLogger<GitHubReleaseFeed>.Instance);

        var failure = await Assert.ThrowsAsync<ReleaseFeedException>(() => feed.LatestAsync(CancellationToken.None));

        Assert.Equal(ReleaseLookupFailure.Http, failure.Failure);
        Assert.Equal(403, failure.HttpCode);
    }

    [Fact]
    public async Task LatestAsync_ReportsAnUnreachableFeed()
    {
        var handler = new RecordingHandler(new HttpRequestException("no route to host"));
        using var http = new HttpClient(handler);
        var feed = new GitHubReleaseFeed(http, NullLogger<GitHubReleaseFeed>.Instance);

        var failure = await Assert.ThrowsAsync<ReleaseFeedException>(() => feed.LatestAsync(CancellationToken.None));

        Assert.Equal(ReleaseLookupFailure.Unreachable, failure.Failure);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData(/*lang=json,strict*/ """{"tag_name":"v1.4.0"}""")]
    [InlineData(/*lang=json,strict*/ """{"tag_name":"nightly","html_url":"https://example.org/r"}""")]
    [InlineData(/*lang=json,strict*/ """{"tag_name":"v1.4.0","html_url":"not a url"}""")]
    public void Read_RefusesWhatIsNotARelease(string json)
    {
        var failure = Assert.Throws<ReleaseFeedException>(() => GitHubReleaseFeed.Read(json));
        Assert.Equal(ReleaseLookupFailure.Unreadable, failure.Failure);
    }

    [Theory]
    [InlineData("v1.4.0", "1.4.0")]
    [InlineData("1.4.0", "1.4.0")]
    [InlineData("V2.0", "2.0")]
    public void Read_TakesTheVersionFromTheTag(string tag, string expected)
    {
        var release = GitHubReleaseFeed.Read($$$"""{"tag_name":"{{{tag}}}","html_url":"https://example.org/r"}""");
        Assert.Equal(expected, release.Text);
        Assert.Equal(Version.Parse(expected), release.Version);
    }

    private sealed record Sent(string Url, string UserAgent, string Accept);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;

        public RecordingHandler(HttpResponseMessage response) => _response = response;

        public RecordingHandler(Exception exception) => _exception = exception;

        public List<Sent> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new Sent(request.RequestUri!.ToString(), request.Headers.UserAgent.ToString(), request.Headers.Accept.ToString()));
            return _exception is { } exception ? Task.FromException<HttpResponseMessage>(exception) : Task.FromResult(_response!);
        }
    }
}
