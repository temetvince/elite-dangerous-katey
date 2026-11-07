namespace Katey.Journal.Tests;

/// <summary>Checks that journal events replay into the right counts.</summary>
public sealed class MaterialInventoryTests
{
    private const string Snapshot = /*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:00Z","event":"Materials","Raw":[{"Name":"iron","Count":30},{"Name":"arsenic","Count":2}],"Manufactured":[{"Name":"galvanisingalloys","Name_Localised":"Galvanising Alloys","Count":12}],"Encoded":[{"Name":"dataminedwakeexceptions","Count":1}]}""";

    private static JournalEvent Event(string line) => JournalEvent.Parse(line) ?? throw new InvalidOperationException("fixture did not parse");

    [Fact]
    public void Parse_ReadsTimestampAndName_AndRejectsJunk()
    {
        var parsed = JournalEvent.Parse(Snapshot);

        Assert.NotNull(parsed);
        Assert.Equal("Materials", parsed.Name);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 20, 0, 0, TimeSpan.Zero), parsed.Timestamp);
        Assert.Null(JournalEvent.Parse(""));
        Assert.Null(JournalEvent.Parse("{not json"));
        Assert.Null(JournalEvent.Parse(/*lang=json,strict*/ """{"event":"Materials"}"""));
        Assert.Null(JournalEvent.Parse(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:00:00Z"}"""));
    }

    [Fact]
    public void Apply_SnapshotReplacesEverything_CaseInsensitively()
    {
        var inventory = new MaterialInventory();

        Assert.False(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T19:59:00Z","event":"MaterialCollected","Category":"Raw","Name":"iron","Count":3}""")));
        Assert.False(inventory.HasSnapshot);

        Assert.True(inventory.Apply(Event(Snapshot)));
        Assert.True(inventory.HasSnapshot);
        Assert.Equal(30, inventory.Counts["IRON"]);
        Assert.Equal(12, inventory.Counts["GalvanisingAlloys"]);
        Assert.Equal(4, inventory.Counts.Count);
    }

    [Fact]
    public void Apply_AdjustsForEveryKindOfEvent()
    {
        var inventory = new MaterialInventory();
        inventory.Apply(Event(Snapshot));

        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:01:00Z","event":"MaterialCollected","Category":"Raw","Name":"iron","Count":3}""")));
        Assert.Equal(33, inventory.Counts["iron"]);

        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:02:00Z","event":"MaterialDiscarded","Category":"Raw","Name":"iron","Count":40}""")));
        Assert.False(inventory.Counts.ContainsKey("iron"));

        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:03:00Z","event":"MaterialTrade","Paid":{"Material":"galvanisingalloys","Quantity":6},"Received":{"Material":"phasealloys","Quantity":1}}""")));
        Assert.Equal(6, inventory.Counts["galvanisingalloys"]);
        Assert.Equal(1, inventory.Counts["phasealloys"]);

        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:04:00Z","event":"EngineerCraft","Ingredients":[{"Name":"arsenic","Count":1},{"Name":"dataminedwakeexceptions","Count":1}]}""")));
        Assert.Equal(1, inventory.Counts["arsenic"]);
        Assert.False(inventory.Counts.ContainsKey("dataminedwakeexceptions"));

        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:05:00Z","event":"MissionCompleted","MaterialsReward":[{"Name":"iron","Count":5}]}""")));
        Assert.Equal(5, inventory.Counts["iron"]);

        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:06:00Z","event":"Synthesis","Materials":[{"Name":"iron","Count":2}]}""")));
        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:07:00Z","event":"TechnologyBroker","Materials":[{"Name":"iron","Count":1}]}""")));
        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:08:00Z","event":"ScientificResearch","Name":"iron","Count":1}""")));
        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:09:00Z","event":"EngineerContribution","Type":"Materials","Material":"iron","Quantity":1}""")));
        Assert.False(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:09:30Z","event":"EngineerContribution","Type":"Commodity","Commodity":"gold","Quantity":1}""")));
        Assert.False(inventory.Counts.ContainsKey("iron"));

        Assert.True(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:10:00Z","event":"LoadGame","Commander":"Casey"}""")));
        Assert.Equal("Casey", inventory.Commander);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 20, 10, 0, TimeSpan.Zero), inventory.AsOf);

        Assert.False(inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:11:00Z","event":"Music","MusicTrack":"NoTrack"}""")));

        var snapshot = inventory.Snapshot();
        inventory.Apply(Event(/*lang=json,strict*/ """{"timestamp":"2026-11-05T20:12:00Z","event":"MaterialCollected","Name":"iron","Count":1}"""));
        Assert.False(snapshot.Counts.ContainsKey("iron"));
    }
}
