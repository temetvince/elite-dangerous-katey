using Katey.App.Logging;

namespace Katey.App.Tests;

public sealed class LogScrubberTests
{
    [Fact]
    public void Scrub_ReplacesTheProfileFolderTheAccountAndTheMachine_WhateverTheCase()
    {
        var scrubber = new LogScrubber([("Jane Doe", "~user"), ("DESKTOP-7", "~machine")]);

        // Every path loses its folders, whatever the drive, slash style or share; the file name stays.
        Assert.Equal("Could not open …/Journal.2026-11-08T121642.01.log.", scrubber.Scrub(@"Could not open C:\Users\Jane Doe\Saved Games\Frontier Developments\Elite Dangerous\Journal.2026-11-08T121642.01.log."));
        Assert.Equal("at …/state.json", scrubber.Scrub("at c:/users/jane doe/OneDrive/Katey/state.json"));
        Assert.Equal("backup to …/", scrubber.Scrub(@"backup to D:\Games\Backups\"));
        Assert.Equal("share …/x.log", scrubber.Scrub(@"share \\nas\media\elite\x.log"));
        Assert.Equal("on linux …/Status.json", scrubber.Scrub("on linux /home/jane/.steam/Elite Dangerous/Status.json"));
        Assert.Equal("Commander ~user on ~machine", scrubber.Scrub("Commander Jane Doe on desktop-7"));
        Assert.Equal("Spansh plotted 3 jump(s) from Koine to Sol.", scrubber.Scrub("Spansh plotted 3 jump(s) from Koine to Sol."));
        Assert.Equal("ratio 3:2 and time 10:30", scrubber.Scrub("ratio 3:2 and time 10:30"));
    }

    [Fact]
    public void Scrub_SkipsEmptyRules_AndThisMachinesScrubberHidesTheProfile()
    {
        var empty = new LogScrubber([("", "~")]);
        Assert.Equal("unchanged", empty.Scrub("unchanged"));

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (profile.Length > 0)
        {
            var scrubbed = LogScrubber.ForThisMachine().Scrub($"Saved to {Path.Combine(profile, "Documents", "x.json")}");
            Assert.DoesNotContain(profile, scrubbed, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Saved to …/x.json", scrubbed);
        }
    }
}
