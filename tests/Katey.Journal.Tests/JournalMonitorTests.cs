using Microsoft.Extensions.Logging.Abstractions;

namespace Katey.Journal.Tests;

/// <summary>Drives the monitor against a temporary folder: replay, tailing, switching files, and the live files.</summary>
public sealed class JournalMonitorTests : IDisposable
{
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(40);
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "katey-tests-" + Guid.NewGuid().ToString("N"));

    public JournalMonitorTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task Start_ReplaysHistory_ThenFollowsAppends_AndNewFiles()
    {
        WriteLines("Journal.2026-11-04T100000.01.log",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-04T10:00:00Z","event":"Materials","Raw":[{"Name":"iron","Count":99}],"Manufactured":[],"Encoded":[]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-04T10:00:01Z","event":"FSDJump","StarSystem":"Old Place","SystemAddress":7,"StarPos":[1,2,3]}""");
        WriteLines("Journal.2026-11-05T100000.01.log",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:00Z","event":"LoadGame","Commander":"Casey","Ship":"Mandalay","ShipID":3}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:00:01Z","event":"Materials","Raw":[{"Name":"iron","Count":10}],"Manufactured":[],"Encoded":[]}""",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T10:05:00Z","event":"MaterialCollected","Name":"iron","Count":2}""");

        var materials = new MaterialInventory();
        var route = new RouteTracker();
        using var monitor = new JournalMonitor(NullLogger<JournalMonitor>.Instance, [materials, route], Poll);
        var updates = new Queue<TaskCompletionSource>();
        for (var index = 0; index < 3; index++)
        {
            updates.Enqueue(new TaskCompletionSource());
        }

        var pending = updates.ToArray();
        var seen = 0;
        monitor.Updated += (_, _) =>
        {
            if (seen < pending.Length)
            {
                pending[seen++].TrySetResult();
            }
        };

        monitor.Start(_folder);
        await pending[0].Task.WaitAsync(Wait);
        Assert.Equal(MonitorState.Watching, monitor.State);
        Assert.True(monitor.Progress.IsComplete);
        Assert.Equal(2, monitor.Progress.FileCount);

        var first = materials.Snapshot();
        Assert.True(first.HasSnapshot);
        Assert.Equal(12, first.Counts["iron"]);
        Assert.Equal("Casey", first.Commander);
        Assert.True(route.Snapshot().HasVisited(7));

        AppendLine("Journal.2026-11-05T100000.01.log", /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:06:00Z","event":"MaterialCollected","Name":"iron","Count":3}""");
        await pending[1].Task.WaitAsync(Wait);
        Assert.Equal(15, materials.Snapshot().Counts["iron"]);

        await Task.Delay(50);
        WriteLines("Journal.2026-11-05T110000.01.log",
                                 /*lang=json,strict*/
                                 """{"timestamp":"2026-11-05T11:00:00Z","event":"Materials","Raw":[{"Name":"iron","Count":1}],"Manufactured":[],"Encoded":[]}""");
        await pending[2].Task.WaitAsync(Wait);
        Assert.Equal(1, materials.Snapshot().Counts["iron"]);

        monitor.Stop();
        Assert.Equal(MonitorState.Stopped, monitor.State);
    }

    [Fact]
    public async Task LiveFiles_AreReadWhenTheyChange()
    {
        File.WriteAllText(Path.Combine(_folder, "NavRoute.json"), /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"NavRoute","Route":[{"StarSystem":"Sol","SystemAddress":1,"StarPos":[0,0,0],"StarClass":"G"},{"StarSystem":"Deciat","SystemAddress":2,"StarPos":[3,4,0],"StarClass":"K"}]}""");
        File.WriteAllText(Path.Combine(_folder, "Status.json"), /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Status","Flags":16777240,"Fuel":{"FuelMain":12.5,"FuelReservoir":0.4},"Cargo":12,"BodyName":"Sol","Destination":{"System":2,"Body":0,"Name":"Deciat"}}""");
        using var monitor = new JournalMonitor(NullLogger<JournalMonitor>.Instance, [new MaterialInventory()], Poll);
        var updated = new TaskCompletionSource();
        monitor.Updated += (_, _) => updated.TrySetResult();

        monitor.Start(_folder);
        await updated.Task.WaitAsync(Wait);

        Assert.NotNull(monitor.Route);
        Assert.Equal(2, monitor.Route.Stops.Count);
        Assert.Equal(5, monitor.Route.Stops[1].DistanceTo(monitor.Route.Stops[0]));
        Assert.NotNull(monitor.Status);
        Assert.Equal(12.5, monitor.Status.FuelMain);
        Assert.Equal(12, monitor.Status.Cargo);
        Assert.Equal("Deciat", monitor.Status.DestinationName);
        Assert.True(monitor.Status.IsInSupercruise);
        Assert.False(monitor.Status.IsDocked);
    }

    [Fact]
    public async Task Poll_ReadsAJournalTheGameRewrote_FromItsStart()
    {
        WriteLines("Journal.2026-11-08T093719.01.log",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:37:00Z","event":"Materials","Raw":[{"Name":"iron","Count":10}],"Manufactured":[],"Encoded":[]}""",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T15:40:00Z","event":"MaterialCollected","Name":"iron","Count":2}""");
        var materials = new MaterialInventory();
        using var monitor = new JournalMonitor(NullLogger<JournalMonitor>.Instance, [materials], Poll);
        var updates = new Queue<TaskCompletionSource>([new(), new()]);
        var pending = updates.ToArray();
        var seen = 0;
        monitor.Updated += (_, _) =>
        {
            if (seen < pending.Length)
            {
                pending[seen++].TrySetResult();
            }
        };

        monitor.Start(_folder);
        await pending[0].Task.WaitAsync(Wait);
        Assert.Equal(12, materials.Snapshot().Counts["iron"]);

        // The game relaunched from a launcher left open: the same file, written again from the start, and shorter.
        WriteLines("Journal.2026-11-08T093719.01.log",
            /*lang=json,strict*/ """{"timestamp":"2026-11-08T17:51:10Z","event":"Materials","Raw":[{"Name":"iron","Count":5}],"Manufactured":[],"Encoded":[]}""");
        await pending[1].Task.WaitAsync(Wait);
        Assert.Equal(5, materials.Snapshot().Counts["iron"]);
    }

    [Fact]
    public void Start_WithAMissingFolder_ReportsIt()
    {
        using var monitor = new JournalMonitor(NullLogger<JournalMonitor>.Instance, [new MaterialInventory()], Poll);

        monitor.Start(Path.Combine(_folder, "nope"));

        Assert.Equal(MonitorState.FolderMissing, monitor.State);
    }

    [Fact]
    public async Task Start_WithoutASnapshot_WatchesUntilOneArrives()
    {
        WriteLines("Journal.2026-11-05T100000.01.log", /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:00Z","event":"Fileheader","part":1}""");
        var materials = new MaterialInventory();
        using var monitor = new JournalMonitor(NullLogger<JournalMonitor>.Instance, [materials], Poll);
        var changed = new TaskCompletionSource();
        monitor.Updated += (_, _) => changed.TrySetResult();

        monitor.Start(_folder);
        await Task.Delay(120);
        Assert.False(materials.Snapshot().HasSnapshot);

        AppendLine("Journal.2026-11-05T100000.01.log", /*lang=json,strict*/ """{"timestamp":"2026-11-05T10:00:05Z","event":"Materials","Raw":[],"Manufactured":[{"Name":"heatvanes","Count":4}],"Encoded":[]}""");
        await changed.Task.WaitAsync(Wait);
        Assert.Equal(4, materials.Snapshot().Counts["heatvanes"]);
    }

    [Fact]
    public void JournalFiles_ListsOnlyJournals_InTheOrderTheirNamesSay_WhateverTheWriteTimes()
    {
        WriteLines("Journal.2026-11-05T100000.01.log", "{}");
        File.WriteAllText(Path.Combine(_folder, "Status.json"), "{}");
        File.SetLastWriteTimeUtc(Path.Combine(_folder, "Journal.2026-11-05T100000.01.log"), new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        WriteLines("Journal.251104100000.01.log", "{}");
        File.SetLastWriteTimeUtc(Path.Combine(_folder, "Journal.251104100000.01.log"), new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc));
        WriteLines("Journal.2026-11-04T235959.01.log", "{}");
        File.SetLastWriteTimeUtc(Path.Combine(_folder, "Journal.2026-11-04T235959.01.log"), new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc));

        var files = JournalFolder.JournalFiles(_folder);

        Assert.Equal(3, files.Count);
        Assert.EndsWith("Journal.251104100000.01.log", files[0], StringComparison.Ordinal);
        Assert.EndsWith("Journal.2026-11-04T235959.01.log", files[1], StringComparison.Ordinal);
        Assert.EndsWith("Journal.2026-11-05T100000.01.log", files[2], StringComparison.Ordinal);
        Assert.Equal(new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc), JournalFolder.StartedAt(files[2]));
        Assert.Equal(new DateTime(2025, 11, 4, 10, 0, 0, DateTimeKind.Utc), JournalFolder.StartedAt("Journal.251104100000.01.log"));
        Assert.Null(JournalFolder.StartedAt("Journal.beta.log"));
    }

    [Fact]
    public void Candidates_StartWithSavedGames()
    {
        var candidates = JournalFolder.Candidates();

        Assert.NotEmpty(candidates);
        Assert.Contains("Saved Games", candidates[0], StringComparison.Ordinal);
        Assert.Contains(candidates, path => path.Contains("compatdata", StringComparison.Ordinal));
    }

    private void WriteLines(string file, params string[] lines)
        => File.WriteAllText(Path.Combine(_folder, file), string.Concat(lines.Select(line => line + "\r\n")));

    private void AppendLine(string file, string line)
    {
        using var stream = new FileStream(Path.Combine(_folder, file), FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(stream);
        writer.Write(line + "\r\n");
    }
}
