using Katey.Core.Routing;

namespace Katey.App.Routing;

/// <summary>A route plot that could not be made: the service said no, could not be reached, or took too long.</summary>
public sealed class RoutePlotException : Exception
{
    /// <summary>Creates the exception.</summary>
    public RoutePlotException()
    {
    }

    /// <summary>Creates the exception with a reason fit to show the user.</summary>
    /// <param name="message">Why the plot failed, in plain words.</param>
    public RoutePlotException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a reason and its cause.</summary>
    /// <param name="message">Why the plot failed, in plain words.</param>
    /// <param name="innerException">What went wrong underneath.</param>
    public RoutePlotException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Something that turns a plot request into a route, usually by asking a service over the network.</summary>
public interface IRoutePlotter
{
    /// <summary>Plots a route.</summary>
    /// <param name="request">What to plot.</param>
    /// <param name="cancellationToken">Cancels the plot.</param>
    /// <returns>The route, with at least one stop.</returns>
    /// <exception cref="RoutePlotException">The plot could not be made. The message is fit to show the user.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    Task<PlottedRoute> PlotAsync(PlotRequest request, CancellationToken cancellationToken);
}
