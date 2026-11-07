using Katey.Journal;

namespace Katey.App;

/// <summary>The nine trackers the window reads, bundled so the monitor and the view models share one set.</summary>
public sealed class JournalSinks
{
    /// <summary>Gets the material inventory.</summary>
    public MaterialInventory Materials { get; } = new();

    /// <summary>Gets the route position and visited systems.</summary>
    public RouteTracker Route { get; } = new();

    /// <summary>Gets the current system's bodies.</summary>
    public ExplorationTracker Exploration { get; } = new();

    /// <summary>Gets the ships and stored modules.</summary>
    public FleetTracker Fleet { get; } = new();

    /// <summary>Gets the recent-activity feed.</summary>
    public ActivityTracker Activity { get; } = new();

    /// <summary>Gets the lifetime figures, the income ledger and the Powerplay standing.</summary>
    public StatisticsTracker Statistics { get; } = new();

    /// <summary>Gets the technology broker purchases.</summary>
    public UnlockTracker Unlocks { get; } = new();

    /// <summary>Gets the engineering rolls.</summary>
    public CraftTracker Crafts { get; } = new();

    /// <summary>Gets the exploration and organic data aboard, unsold.</summary>
    public CarriedDataTracker Carried { get; } = new();

    /// <summary>Gets every sink, for the monitor.</summary>
    public IReadOnlyList<IJournalSink> All => [Materials, Route, Exploration, Fleet, Activity, Statistics, Unlocks, Crafts, Carried];
}
