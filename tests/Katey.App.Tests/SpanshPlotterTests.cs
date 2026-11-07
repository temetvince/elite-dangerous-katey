using System.Net;
using Katey.App.Routing;
using Katey.Core.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Katey.App.Tests;

public sealed class SpanshPlotterTests
{
    private const string DoneReply = /*lang=json,strict*/ """
        {"job":"j-1","status":"ok","state":"completed","result":{"refuel_every_scoopable":false,"jumps":[
          {"name":"Deciat","id64":3,"x":122.625,"y":-0.8125,"z":-47.28125,"distance":0,"distance_to_destination":130,"fuel_used":0,"fuel_in_tank":32,"has_neutron":false,"is_scoopable":true,"must_refuel":false},
          {"name":"Sol","id64":1,"x":0,"y":0,"z":0,"distance":130,"distance_to_destination":0,"fuel_used":4,"fuel_in_tank":28,"has_neutron":false,"is_scoopable":true,"must_refuel":false}
        ]}}
        """;

    [Fact]
    public async Task PlotAsync_NamesKateyToSpansh_OnEveryRequest()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Something/0.1");
        var plotter = new SpanshPlotter(http, NullLogger<SpanshPlotter>.Instance);
        var ship = new JumpProfile(2.45, 0.012, 1050, 5, 400, 32, 0.5, 0, 4);

        var route = await plotter.PlotAsync(new PlotRequest("Deciat", "Sol", ship), CancellationToken.None);

        Assert.Equal(2, route.Stops.Count);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(SpanshPlotter.SubmitUrl, request.Url);
        Assert.StartsWith("Katey/", request.UserAgent, StringComparison.Ordinal);
        Assert.Contains(SpanshPlotter.ProjectUrl, request.UserAgent, StringComparison.Ordinal);
        Assert.DoesNotContain("Something", request.UserAgent, StringComparison.Ordinal);
        Assert.Equal(SpanshPlotter.UserAgent, request.UserAgent);
    }

    private sealed record Sent(string Url, string UserAgent);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<Sent> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new Sent(request.RequestUri!.ToString(), request.Headers.UserAgent.ToString()));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(DoneReply) });
        }
    }
}
