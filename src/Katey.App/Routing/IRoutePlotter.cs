using Katey.Core.Routing;

namespace Katey.App.Routing;

/// <summary>Why a plot did not produce a route. The application words each kind; the exception carries only the facts.</summary>
public enum PlotFailure
{
    /// <summary>The plotter could not be reached, or did not answer in time.</summary>
    Unreachable,

    /// <summary>The plotter accepted the request but gave no job to wait for.</summary>
    NoJob,

    /// <summary>The plotter was still working when <see cref="RoutePlotException.Waited"/> ran out.</summary>
    StillWorking,

    /// <summary>The plotter answered and refused, with its own words in <see cref="RoutePlotException.PlotterError"/> when it gave any.</summary>
    Rejected,
}

/// <summary>A plot that produced no route. Its message is developer text for the log; the user's sentence is worded from <see cref="Failure"/> and the facts beside it.</summary>
/// <remarks>Creates the exception.</remarks>
/// <param name="failure">What went wrong.</param>
/// <param name="plotterError">The plotter's own words for a <see cref="PlotFailure.Rejected"/> plot, shown as given, or <see langword="null"/>.</param>
/// <param name="httpCode">The HTTP status the plotter answered with when the refusal came with one, or <see langword="null"/>.</param>
/// <param name="waited">How long the plotter was given for a <see cref="PlotFailure.StillWorking"/> plot, or <see langword="null"/>.</param>
/// <param name="innerException">The transport failure behind an <see cref="PlotFailure.Unreachable"/> plot, or <see langword="null"/>.</param>
public sealed class RoutePlotException(PlotFailure failure, string? plotterError = null, int? httpCode = null, TimeSpan? waited = null, Exception? innerException = null) : Exception(Describe(failure, plotterError, httpCode), innerException)
{

    /// <summary>Gets what went wrong.</summary>
    public PlotFailure Failure { get; } = failure;

    /// <summary>Gets the plotter's own words for the refusal, or <see langword="null"/> when it gave none.</summary>
    public string? PlotterError { get; } = plotterError;

    /// <summary>Gets the HTTP status the refusal came with, or <see langword="null"/>.</summary>
    public int? HttpCode { get; } = httpCode;

    /// <summary>Gets how long the plotter was given before the plot was abandoned, or <see langword="null"/>.</summary>
    public TimeSpan? Waited { get; } = waited;

    private static string Describe(PlotFailure failure, string? plotterError, int? httpCode)
        => failure switch
        {
            PlotFailure.Unreachable => "The route plotter could not be reached.",
            PlotFailure.NoJob => "The route plotter accepted the plot but gave no job to wait for.",
            PlotFailure.StillWorking => "The route plotter was still working when the wait ran out.",
            PlotFailure.Rejected => httpCode is { } code ? $"The route plotter refused the plot with HTTP {code}: {plotterError}" : $"The route plotter refused the plot: {plotterError ?? "no route"}",
            _ => $"The route plotter failed: {failure}.",
        };
}

/// <summary>Plots a route between two systems for a ship.</summary>
public interface IRoutePlotter
{
    /// <summary>Plots a route.</summary>
    /// <param name="request">What to plot.</param>
    /// <param name="cancellationToken">Cancels the plot; the task then faults with <see cref="OperationCanceledException"/>.</param>
    /// <returns>The route, which has at least the two end systems.</returns>
    /// <exception cref="RoutePlotException">The plot produced no route; <see cref="RoutePlotException.Failure"/> says why.</exception>
    Task<PlottedRoute> PlotAsync(PlotRequest request, CancellationToken cancellationToken);
}
