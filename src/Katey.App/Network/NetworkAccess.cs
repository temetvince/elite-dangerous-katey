using Katey.App.Persistence;
using Microsoft.Extensions.Logging;

namespace Katey.App.Network;

/// <summary>
/// The one place that says what Katey may send over the network: a switch per kind of request, every one
/// off until the player turns it on. Everything that calls out consults this. Saved.
/// </summary>
public sealed class NetworkAccess
{
    private readonly ILogger _logger;

    /// <summary>Creates the access from the saved state.</summary>
    /// <param name="saved">The saved state.</param>
    /// <param name="logger">Where changes are logged.</param>
    public NetworkAccess(PersistedState saved, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(saved);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        AllowRoutes = saved.AllowSpansh;
        AllowSystemBodies = saved.AllowSpanshBodies;
        AllowUpdateCheck = saved.AllowUpdateCheck;
    }

    /// <summary>Raised after a switch changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets whether a route may be asked of Spansh.</summary>
    public bool AllowRoutes { get; private set; }

    /// <summary>Gets whether a system's bodies may be asked of Spansh.</summary>
    public bool AllowSystemBodies { get; private set; }

    /// <summary>Gets whether GitHub may be asked for the latest release.</summary>
    public bool AllowUpdateCheck { get; private set; }

    /// <summary>Takes the switches from a saved state, for when the saved data is reloaded.</summary>
    /// <param name="saved">The saved state.</param>
    public void Load(PersistedState saved)
    {
        ArgumentNullException.ThrowIfNull(saved);
        Set(saved.AllowSpansh, saved.AllowSpanshBodies, saved.AllowUpdateCheck);
    }

    /// <summary>Sets the route switch.</summary>
    /// <param name="allowed">Whether routes may be asked.</param>
    public void SetRoutes(bool allowed) => Set(allowed, AllowSystemBodies, AllowUpdateCheck);

    /// <summary>Sets the system bodies switch.</summary>
    /// <param name="allowed">Whether system bodies may be asked.</param>
    public void SetSystemBodies(bool allowed) => Set(AllowRoutes, allowed, AllowUpdateCheck);

    /// <summary>Sets the release check switch.</summary>
    /// <param name="allowed">Whether the latest release may be asked for.</param>
    public void SetUpdateCheck(bool allowed) => Set(AllowRoutes, AllowSystemBodies, allowed);

    private void Set(bool routes, bool bodies, bool updates)
    {
        if (routes == AllowRoutes && bodies == AllowSystemBodies && updates == AllowUpdateCheck)
        {
            return;
        }

        AllowRoutes = routes;
        AllowSystemBodies = bodies;
        AllowUpdateCheck = updates;
        Log.NetworkAccessChanged(_logger, AllowRoutes, AllowSystemBodies, AllowUpdateCheck);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
