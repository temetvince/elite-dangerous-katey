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
    }

    /// <summary>Raised after a switch changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets whether a route may be asked of Spansh.</summary>
    public bool AllowRoutes { get; private set; }

    /// <summary>Gets whether a system's bodies may be asked of Spansh.</summary>
    public bool AllowSystemBodies { get; private set; }

    /// <summary>Takes the switches from a saved state, for when the saved data is reloaded.</summary>
    /// <param name="saved">The saved state.</param>
    public void Load(PersistedState saved)
    {
        ArgumentNullException.ThrowIfNull(saved);
        Set(saved.AllowSpansh, saved.AllowSpanshBodies);
    }

    /// <summary>Sets the route switch.</summary>
    /// <param name="allowed">Whether routes may be asked.</param>
    public void SetRoutes(bool allowed) => Set(allowed, AllowSystemBodies);

    /// <summary>Sets the system bodies switch.</summary>
    /// <param name="allowed">Whether system bodies may be asked.</param>
    public void SetSystemBodies(bool allowed) => Set(AllowRoutes, allowed);

    private void Set(bool routes, bool bodies)
    {
        if (routes == AllowRoutes && bodies == AllowSystemBodies)
        {
            return;
        }

        AllowRoutes = routes;
        AllowSystemBodies = bodies;
        Log.NetworkAccessChanged(_logger, AllowRoutes, AllowSystemBodies);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
