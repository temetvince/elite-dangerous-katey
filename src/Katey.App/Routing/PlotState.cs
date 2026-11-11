using Katey.App.Network;
using Katey.App.Text;
using Katey.App.ViewModels;
using Katey.Core.Routing;
using Microsoft.Extensions.Logging;

namespace Katey.App.Routing;

/// <summary>
/// The plotted route Katey asked a plotter for, whether asking is allowed, and the plot in progress. Every
/// change is raised on the UI thread through the dispatcher given.
/// </summary>
/// <remarks>
/// Asking a plotter sends the two system names and the ship's jump numbers to a service on the network;
/// <see cref="Enabled"/> follows <see cref="NetworkAccess.AllowRoutes"/>, off until the user allows it.
/// One plot runs at a time: a new request while one is running is ignored.
/// </remarks>
public sealed class PlotState
{
    private readonly IRoutePlotter _plotter;
    private readonly Action<Action> _dispatch;
    private readonly ILogger _logger;
    private readonly NetworkAccess _access;
    private CancellationTokenSource? _running;
    private Outcome _outcome = new(OutcomeKind.None);

    /// <summary>Creates the state.</summary>
    /// <param name="plotter">Who plots.</param>
    /// <param name="dispatch">Runs an action on the UI thread.</param>
    /// <param name="logger">Where plot milestones go.</param>
    /// <param name="access">What the player allows; a plot in progress is cancelled when routes stop being allowed.</param>
    /// <param name="route">The route from the last run, or <see langword="null"/>.</param>
    public PlotState(IRoutePlotter plotter, Action<Action> dispatch, ILogger logger, NetworkAccess access, PlottedRoute? route)
    {
        ArgumentNullException.ThrowIfNull(plotter);
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(access);
        _plotter = plotter;
        _dispatch = dispatch;
        _logger = logger;
        _access = access;
        Route = route;
        access.Changed += (_, _) =>
        {
            if (!Enabled)
            {
                Cancel();
            }

            Changed?.Invoke(this, EventArgs.Empty);
        };
    }

    /// <summary>Raised on the UI thread after the route, the status, the busy flag or <see cref="Enabled"/> changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets whether Katey may ask the plotter: whether the player allows route requests.</summary>
    public bool Enabled => _access.AllowRoutes;

    /// <summary>Gets the route, or <see langword="null"/> when none is plotted.</summary>
    public PlottedRoute? Route { get; private set; }

    /// <summary>Gets one line on the last plot: what is happening, or why it failed. Empty when nothing has been asked.</summary>
    public string Status => _outcome.Kind switch
    {
        OutcomeKind.Asking => Strings.Format(Keys.Plot_Asking, ("from", _outcome.From), ("to", _outcome.To)),
        OutcomeKind.Plotted => Strings.Format(Keys.Plot_Plotted, ("jumps", Display.Plural(Keys.Plural_Jumps, _outcome.Route!.Jumps)), ("from", _outcome.Route.Source), ("to", _outcome.Route.Destination)),
        OutcomeKind.Cancelled => Strings.Get(Keys.Plot_Cancelled),
        OutcomeKind.Failed => Describe(_outcome.Failure!),
        OutcomeKind.None => "",
        _ => "",
    };

    /// <summary>Gets whether a plot is running.</summary>
    public bool IsBusy => _running is not null;

    /// <summary>Starts a plot. Does nothing while disabled or while one is running.</summary>
    /// <param name="request">What to plot.</param>
    public void Plot(PlotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enabled || _running is not null)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _running = cancellation;
        _outcome = new(OutcomeKind.Asking, request.Source, request.Destination);
        Changed?.Invoke(this, EventArgs.Empty);
        _ = RunAsync(request, cancellation);
    }

    /// <summary>Stops the running plot, if any. The route already held stays.</summary>
    public void Cancel()
    {
        if (_running is { } running)
        {
            running.Cancel();
        }
    }

    /// <summary>Replaces the route with a saved one, for when the saved data is reloaded. Stops any plot running.</summary>
    /// <param name="route">The route, or <see langword="null"/> for none.</param>
    public void Restore(PlottedRoute? route)
    {
        Cancel();
        Route = route;
        _outcome = new(OutcomeKind.None);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Forgets the route.</summary>
    public void Clear()
    {
        Cancel();
        if (Route is not null || _outcome.Kind != OutcomeKind.None)
        {
            Route = null;
            _outcome = new(OutcomeKind.None);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task RunAsync(PlotRequest request, CancellationTokenSource cancellation)
    {
        PlottedRoute? route = null;
        Outcome outcome;
        try
        {
            route = await _plotter.PlotAsync(request, cancellation.Token).ConfigureAwait(false);
            outcome = new(OutcomeKind.Plotted, Route: route);
        }
        catch (OperationCanceledException)
        {
            outcome = new(OutcomeKind.Cancelled);
        }
        catch (RoutePlotException exception)
        {
            Log.PlotFailed(_logger, request.Source, request.Destination, exception);
            outcome = new(OutcomeKind.Failed, Failure: exception);
        }

        _dispatch(() =>
        {
            if (!ReferenceEquals(_running, cancellation))
            {
                return;
            }

            _running = null;
            cancellation.Dispose();
            if (route is not null)
            {
                Route = route;
            }

            _outcome = outcome;
            Changed?.Invoke(this, EventArgs.Empty);
        });
    }

    private static string Describe(RoutePlotException failure)
        => failure.Failure switch
        {
            PlotFailure.Unreachable => Strings.Get(Keys.Spansh_Unreachable),
            PlotFailure.NoJob => Strings.Get(Keys.Spansh_NoJob),
            PlotFailure.StillWorking => Strings.Format(Keys.Spansh_StillWorking, ("seconds", failure.Waited?.TotalSeconds ?? 0)),
            PlotFailure.Rejected => failure.PlotterError is null
                            ? Strings.Get(Keys.Spansh_CouldNotPlot)
                            : failure.HttpCode is { } code ? Strings.Format(Keys.Spansh_Http, ("error", failure.PlotterError), ("code", code)) : failure.PlotterError,
            _ => failure.Message,
        };

    private enum OutcomeKind
    {
        None,
        Asking,
        Plotted,
        Cancelled,
        Failed,
    }

    /// <summary>What the last plot did, kept as facts so <see cref="Status"/> can be worded in whatever language is in force when it is read.</summary>
    private sealed record Outcome(OutcomeKind Kind, string From = "", string To = "", PlottedRoute? Route = null, RoutePlotException? Failure = null);
}
