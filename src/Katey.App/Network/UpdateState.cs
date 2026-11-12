using Katey.App.Text;
using Microsoft.Extensions.Logging;

namespace Katey.App.Network;

/// <summary>
/// Whether a newer Katey is published: the version running, the latest release the feed named, and the check
/// in progress. Nothing is asked while <see cref="NetworkAccess.AllowUpdateCheck"/> is off. The feed is asked
/// without waiting, so a check started while the window is being built costs it nothing; every change is
/// raised on the UI thread through the dispatcher given.
/// </summary>
/// <remarks>One check runs at a time; a request while one is running is ignored.</remarks>
public sealed class UpdateState
{
    private readonly IReleaseFeed _feed;
    private readonly Action<Action> _dispatch;
    private readonly ILogger _logger;
    private readonly NetworkAccess _access;
    private ReleaseFeedException? _failure;
    private bool _checked;

    /// <summary>Creates the state.</summary>
    /// <param name="feed">Who knows the latest release.</param>
    /// <param name="dispatch">Runs an action on the UI thread.</param>
    /// <param name="logger">Where checks and failures go.</param>
    /// <param name="access">What the player allows.</param>
    /// <param name="current">The version running.</param>
    public UpdateState(IReleaseFeed feed, Action<Action> dispatch, ILogger logger, NetworkAccess access, Version current)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(current);
        _feed = feed;
        _dispatch = dispatch;
        _logger = logger;
        _access = access;
        Current = current;
    }

    /// <summary>Raised on the UI thread after a check started or finished, well or badly.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the version running.</summary>
    public Version Current { get; }

    /// <summary>Gets the latest release the feed last named, or <see langword="null"/> before a check has answered.</summary>
    public Release? Latest { get; private set; }

    /// <summary>Gets whether a check is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Gets whether <see cref="Latest"/> is newer than <see cref="Current"/>.</summary>
    public bool IsUpdateAvailable => Latest is { } latest && latest.Version > Current;

    /// <summary>Gets whether the last check answered and found nothing newer than <see cref="Current"/>: the release running is the latest.</summary>
    public bool IsUpToDate => _failure is null && Latest is { } latest && latest.Version <= Current;

    /// <summary>Gets one line on the check: that it is off, running, found a newer release, found none, or failed and why. Empty only while allowed and never run.</summary>
    public string Status
    {
        get
        {
            if (!_access.AllowUpdateCheck)
            {
                return Strings.Get(Keys.Update_NotAllowed);
            }

            if (IsBusy)
            {
                return Strings.Get(Keys.Update_Checking);
            }

            return _failure switch
            {
                { Failure: ReleaseLookupFailure.Http } failure => Strings.Format(Keys.Update_Http, ("code", failure.HttpCode)),
                { Failure: ReleaseLookupFailure.Unreachable } => Strings.Get(Keys.Update_Unreachable),
                { Failure: ReleaseLookupFailure.Unreadable } => Strings.Get(Keys.Update_Unreadable),
                { Failure: ReleaseLookupFailure.TimedOut } => Strings.Get(Keys.Update_TimedOut),
                { } => Strings.Get(Keys.Update_Unreachable),
                null when Latest is null => "",
                null => IsUpdateAvailable ? Strings.Format(Keys.Update_Available, ("latest", Latest.Text)) : Strings.Get(Keys.Update_UpToDate),
            };
        }
    }

    /// <summary>Checks once per run: nothing happens while checks are not allowed, while one is running, or once one has answered or failed this run.</summary>
    public void CheckOnce()
    {
        if (!_checked)
        {
            Check();
        }
    }

    /// <summary>Checks now. Nothing happens while checks are not allowed or one is running.</summary>
    public void Check()
    {
        if (!_access.AllowUpdateCheck || IsBusy)
        {
            return;
        }

        IsBusy = true;
        _checked = true;
        Changed?.Invoke(this, EventArgs.Empty);
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        Release? release = null;
        ReleaseFeedException? failure = null;
        try
        {
            release = await _feed.LatestAsync(CancellationToken.None).ConfigureAwait(false);
            Log.UpdateChecked(_logger, Current, release.Version);
        }
        catch (ReleaseFeedException exception)
        {
            Log.UpdateCheckFailed(_logger, exception);
            failure = exception;
        }

        _dispatch(() =>
        {
            if (release is not null)
            {
                Latest = release;
            }

            _failure = failure;
            IsBusy = false;
            Changed?.Invoke(this, EventArgs.Empty);
        });
    }
}
