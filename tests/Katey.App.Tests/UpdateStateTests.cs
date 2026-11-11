using Katey.App.Network;
using Katey.App.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace Katey.App.Tests;

/// <summary>The release check: what it asks, when, and how it words what it learns.</summary>
public sealed class UpdateStateTests
{
    private static (UpdateState Updates, NetworkAccess Access, FakeReleaseFeed Feed) Create(Version current, bool allowed = true, FakeReleaseFeed? feed = null)
    {
        var access = new NetworkAccess(PersistedState.Empty with { AllowUpdateCheck = allowed }, NullLogger.Instance);
        var releases = feed ?? new FakeReleaseFeed();
        return (new UpdateState(releases, action => action(), NullLogger.Instance, access, current), access, releases);
    }

    [Fact]
    public void Check_AsksNothingWhileNotAllowed()
    {
        var (updates, _, feed) = Create(new Version(1, 0, 0), allowed: false);
        updates.Check();
        updates.CheckOnce();
        Assert.Equal(0, feed.Requests);
        Assert.Null(updates.Latest);
        Assert.False(updates.IsUpdateAvailable);
        Assert.Equal("Katey does not look for newer releases until you tick that switch under Outgoing connections.", updates.Status);
    }

    [Fact]
    public void CheckOnce_AsksOncePerRun_AndCheckAsksAgain()
    {
        var (updates, _, feed) = Create(new Version(1, 0, 0));
        var raised = 0;
        updates.Changed += (_, _) => raised++;
        updates.CheckOnce();
        updates.CheckOnce();
        Assert.Equal(1, feed.Requests);
        Assert.Equal(2, raised);
        updates.Check();
        Assert.Equal(2, feed.Requests);
    }

    [Theory]
    [InlineData("1.0.0", true, "Katey 1.4.0 is available.")]
    [InlineData("1.4.0", false, "This is the latest release.")]
    [InlineData("1.5.0", false, "This is the latest release.")]
    public void Status_ComparesTheReleaseWithTheVersionRunning(string current, bool newer, string status)
    {
        var (updates, _, _) = Create(Version.Parse(current));
        updates.Check();
        Assert.False(updates.IsBusy);
        Assert.Equal(newer, updates.IsUpdateAvailable);
        Assert.Equal(status, updates.Status);
        Assert.Equal("1.4.0", updates.Latest!.Text);
    }

    [Theory]
    [InlineData(ReleaseLookupFailure.Unreachable, "GitHub could not be reached for the release check.")]
    [InlineData(ReleaseLookupFailure.Unreadable, "GitHub's answer to the release check could not be read.")]
    [InlineData(ReleaseLookupFailure.TimedOut, "GitHub took too long to answer the release check.")]
    public void Status_WordsEachFailure(ReleaseLookupFailure failure, string status)
    {
        var (updates, _, _) = Create(new Version(1, 0, 0), feed: new FakeReleaseFeed { Failure = failure });
        updates.Check();
        Assert.Equal(status, updates.Status);
        Assert.Null(updates.Latest);
        Assert.False(updates.IsUpdateAvailable);
    }

    [Fact]
    public void Status_KeepsTheLastReleaseThroughALaterFailure_ButShowsTheFailure()
    {
        var feed = new FakeReleaseFeed();
        var (updates, _, _) = Create(new Version(1, 0, 0), feed: feed);
        updates.Check();
        feed.Failure = ReleaseLookupFailure.Unreachable;
        updates.Check();
        Assert.Equal("GitHub could not be reached for the release check.", updates.Status);
        Assert.True(updates.IsUpdateAvailable);
    }
}
