using Katey.App.Network;
using Katey.App.Text;
using Katey.Core.Exploration;
using Microsoft.Extensions.Logging;

namespace Katey.App.Exploration;

/// <summary>
/// The bodies a catalogue knows for the systems the ship has been in, asked for as the ship arrives and kept
/// for the run. Nothing is asked while <see cref="NetworkAccess.AllowSystemBodies"/> is off. A system is
/// asked about once; after a failed lookup it is not asked about again for <see cref="RetryAfter"/>, so a
/// service that is down is not hammered by every journal event. Every change is raised on the UI thread
/// through the dispatcher given.
/// </summary>
public sealed class SystemBodiesState
{
    private readonly ISystemCatalog _catalog;
    private readonly Action<Action> _dispatch;
    private readonly ILogger _logger;
    private readonly NetworkAccess _access;
    private readonly Func<DateTimeOffset> _now;
    private readonly Dictionary<long, IReadOnlyList<CataloguedBody>> _known = [];
    private readonly Dictionary<long, DateTimeOffset> _failed = [];
    private readonly HashSet<long> _running = [];
    private SystemCatalogException? _failure;

    /// <summary>Creates the state.</summary>
    /// <param name="catalog">Who knows the bodies.</param>
    /// <param name="dispatch">Runs an action on the UI thread.</param>
    /// <param name="logger">Where failures go.</param>
    /// <param name="access">What the player allows.</param>
    /// <param name="now">The clock, for the retry wait; the system clock when <see langword="null"/>.</param>
    public SystemBodiesState(ISystemCatalog catalog, Action<Action> dispatch, ILogger logger, NetworkAccess access, Func<DateTimeOffset>? now = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(access);
        _catalog = catalog;
        _dispatch = dispatch;
        _logger = logger;
        _access = access;
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>How long a system that could not be looked up waits before it is asked about again.</summary>
    public static TimeSpan RetryAfter { get; } = TimeSpan.FromMinutes(5);

    /// <summary>Raised on the UI thread after a lookup finished, well or badly.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets one line on the last lookup that failed, or empty.</summary>
    public string Status => _failure switch
    {
        null => "",
        { Failure: SystemLookupFailure.Http } failure => Strings.Format(Keys.Lookup_Http, ("code", failure.HttpCode)),
        { Failure: SystemLookupFailure.Unreachable } => Strings.Get(Keys.Lookup_Unreachable),
        { Failure: SystemLookupFailure.Unreadable } => Strings.Get(Keys.Lookup_Unreadable),
        _ => Strings.Get(Keys.Lookup_TimedOut),
    };

    /// <summary>The bodies known for a system.</summary>
    /// <param name="systemAddress">The system's id.</param>
    /// <returns>The bodies, or <see langword="null"/> when the system has not been looked up, or the lookup failed.</returns>
    public IReadOnlyList<CataloguedBody>? BodiesFor(long systemAddress) => _known.GetValueOrDefault(systemAddress);

    /// <summary>Asks for a system's bodies, once: nothing happens while lookups are not allowed, while one is running for the system, once the system is known, or within <see cref="RetryAfter"/> of a failed lookup of it.</summary>
    /// <param name="systemAddress">The system's id. 0 asks nothing.</param>
    public void Request(long systemAddress)
    {
        if (systemAddress == 0 || !_access.AllowSystemBodies || _known.ContainsKey(systemAddress))
        {
            return;
        }

        if (_failed.TryGetValue(systemAddress, out var failedAt) && _now() - failedAt < RetryAfter)
        {
            return;
        }

        if (!_running.Add(systemAddress))
        {
            return;
        }

        _ = RunAsync(systemAddress);
    }

    private async Task RunAsync(long systemAddress)
    {
        IReadOnlyList<CataloguedBody>? bodies = null;
        SystemCatalogException? failure = null;
        try
        {
            bodies = await _catalog.BodiesAsync(systemAddress, CancellationToken.None).ConfigureAwait(false);
        }
        catch (SystemCatalogException exception)
        {
            Log.SystemLookupFailed(_logger, systemAddress, exception);
            failure = exception;
        }

        _dispatch(() =>
        {
            _running.Remove(systemAddress);
            if (bodies is not null)
            {
                _known[systemAddress] = bodies;
                _failed.Remove(systemAddress);
            }
            else
            {
                _failed[systemAddress] = _now();
            }

            _failure = failure;
            Changed?.Invoke(this, EventArgs.Empty);
        });
    }
}
