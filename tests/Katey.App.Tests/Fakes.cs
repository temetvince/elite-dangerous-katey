using Katey.App.Exploration;
using Katey.App.Network;
using Katey.App.Persistence;
using Katey.App.Routing;
using Katey.Core.Exploration;
using Katey.Core.Routing;
using Katey.Journal;

namespace Katey.App.Tests;

/// <summary>A store that keeps the state in memory and counts saves. Moving it to a folder finds whatever the test put there.</summary>
internal sealed class FakeStateStore : IStateStore
{
    public PersistedState State { get; set; } = PersistedState.Empty;

    public int Saves { get; private set; }

    public string Location { get; private set; } = "memory";

    /// <summary>Gets the states already sitting in folders, by folder.</summary>
    public Dictionary<string, PersistedState> AtFolders { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Relocate(string folder)
    {
        Location = Path.Combine(folder, JsonFileStateStore.FileName);
        if (AtFolders.TryGetValue(folder, out var found))
        {
            State = found;
            return true;
        }

        return false;
    }

    public PersistedState Load() => State;

    public void Save(PersistedState state)
    {
        State = state;
        Saves++;
    }
}

/// <summary>A plotter that answers from what the test sets: a route to return, or a failure to raise. Records every request.</summary>
internal sealed class FakeRoutePlotter : IRoutePlotter
{
    public List<PlotRequest> Requests { get; } = [];

    public PlottedRoute? Route { get; set; }

    public string? Failure { get; set; }

    public Task<PlottedRoute> PlotAsync(PlotRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Failure is { } reason
            ? Task.FromException<PlottedRoute>(new RoutePlotException(PlotFailure.Rejected, reason))
            : Task.FromResult(Route ?? throw new InvalidOperationException("the fake plotter has no route to answer with"));
    }
}

/// <summary>A system catalog that answers from what the test sets, and records every system asked about.</summary>
internal sealed class FakeSystemCatalog : ISystemCatalog
{
    public Dictionary<long, IReadOnlyList<CataloguedBody>> Bodies { get; } = [];

    public List<long> Requests { get; } = [];

    public SystemLookupFailure? Failure { get; set; }

    public Task<IReadOnlyList<CataloguedBody>> BodiesAsync(long systemAddress, CancellationToken cancellationToken)
    {
        Requests.Add(systemAddress);
        return Failure is { } reason
            ? Task.FromException<IReadOnlyList<CataloguedBody>>(new SystemCatalogException(reason))
            : Task.FromResult(Bodies.GetValueOrDefault(systemAddress) ?? (IReadOnlyList<CataloguedBody>)[]);
    }
}

/// <summary>A release feed that answers with the release the test sets, or fails as told, and counts the asks.</summary>
internal sealed class FakeReleaseFeed : IReleaseFeed
{
    public Release Latest { get; set; } = new(new Version(1, 4, 0), "1.4.0", new Uri("https://github.com/temetvince/elite-dangerous-katey/releases/tag/v1.4.0"));

    public ReleaseLookupFailure? Failure { get; set; }

    public int? HttpCode { get; set; }

    public int Requests { get; private set; }

    /// <summary>When set, a lookup stays open until the test completes it, so a check can be observed in flight.</summary>
    public TaskCompletionSource<Release>? Hold { get; set; }

    public Task<Release> LatestAsync(CancellationToken cancellationToken)
    {
        Requests++;
        return Hold is { } hold
            ? hold.Task
            : Failure is { } reason
            ? Task.FromException<Release>(new ReleaseFeedException(reason, HttpCode))
            : Task.FromResult(Latest);
    }
}

/// <summary>A backup that records each call and answers with a fixed result.</summary>
internal sealed class FakeJournalBackup : IJournalBackup
{
    public List<(string Source, string Destination)> Calls { get; } = [];

    public BackupResult Result { get; set; } = new(3, 2, 300, []);

    public BackupResult Copy(string sourceFolder, string destinationFolder)
    {
        Calls.Add((sourceFolder, destinationFolder));
        return Result;
    }
}

/// <summary>A monitor the test drives by hand: it applies events to the sinks and raises the updates.</summary>
internal sealed class FakeJournalMonitor(JournalSinks sinks) : IJournalMonitor
{
    public event EventHandler? Updated;

    public event EventHandler? StateChanged;

    public string? Folder { get; private set; }

    public MonitorState State { get; private set; }

    public CatchUpProgress Progress { get; private set; } = CatchUpProgress.None;

    public NavRoute? Route { get; set; }

    public ShipStatus? Status { get; set; }

    public int Starts { get; private set; }

    public void Start(string folder)
    {
        Folder = folder;
        Starts++;
        State = MonitorState.Watching;
        Progress = new CatchUpProgress(1, 1);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        State = MonitorState.Stopped;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Puts the monitor partway through a replay and raises the state change.</summary>
    public void CatchUp(int filesDone, int fileCount)
    {
        State = MonitorState.CatchingUp;
        Progress = new CatchUpProgress(filesDone, fileCount);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Feeds journal lines to every interested sink, then raises <see cref="Updated"/>.</summary>
    public void Feed(params string[] lines)
    {
        foreach (var line in lines)
        {
            var journalEvent = JournalEvent.Parse(line) ?? throw new InvalidOperationException("fixture did not parse: " + line);
            foreach (var sink in sinks.All)
            {
                if (sink.InterestedEvents is null || sink.InterestedEvents.Contains(journalEvent.Name))
                {
                    sink.Apply(journalEvent);
                }
            }
        }

        RaiseUpdated();
    }

    public void RaiseUpdated() => Updated?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
    }
}
