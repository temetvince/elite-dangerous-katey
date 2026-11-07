using Katey.Core.Exploration;

namespace Katey.App.Exploration;

/// <summary>A system lookup that could not be made: the service said no or could not be reached.</summary>
public sealed class SystemCatalogException : Exception
{
    /// <summary>Creates the exception.</summary>
    public SystemCatalogException()
    {
    }

    /// <summary>Creates the exception with a reason fit to show the user.</summary>
    /// <param name="message">Why the lookup failed, in plain words.</param>
    public SystemCatalogException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a reason and its cause.</summary>
    /// <param name="message">Why the lookup failed, in plain words.</param>
    /// <param name="innerException">What went wrong underneath.</param>
    public SystemCatalogException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Something that knows a system's bodies, usually by asking a service over the network.</summary>
public interface ISystemCatalog
{
    /// <summary>Looks a system's bodies up.</summary>
    /// <param name="systemAddress">The system's id, as the journal and the catalogues share it.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The bodies the catalogue knows, which may be none.</returns>
    /// <exception cref="SystemCatalogException">The lookup could not be made. The message is fit to show the user.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    Task<IReadOnlyList<CataloguedBody>> BodiesAsync(long systemAddress, CancellationToken cancellationToken);
}
