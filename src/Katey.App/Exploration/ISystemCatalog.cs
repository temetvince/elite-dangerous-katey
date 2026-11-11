using Katey.Core.Exploration;

namespace Katey.App.Exploration;

/// <summary>Why a system lookup gave no answer. The application words each kind; the exception carries only the facts.</summary>
public enum SystemLookupFailure
{
    /// <summary>The catalog could not be reached.</summary>
    Unreachable,

    /// <summary>The catalog answered with an HTTP status in <see cref="SystemCatalogException.HttpCode"/> other than success or not-found.</summary>
    Http,

    /// <summary>The catalog's answer was not readable as the expected JSON.</summary>
    Unreadable,

    /// <summary>The catalog did not answer in time.</summary>
    TimedOut,
}

/// <summary>A system lookup that gave no answer. Its message is developer text for the log; the user's sentence is worded from <see cref="Failure"/>.</summary>
/// <remarks>Creates the exception.</remarks>
/// <param name="failure">What went wrong.</param>
/// <param name="httpCode">The HTTP status for an <see cref="SystemLookupFailure.Http"/> failure, or <see langword="null"/>.</param>
/// <param name="innerException">The transport or parsing failure behind it, or <see langword="null"/>.</param>
public sealed class SystemCatalogException(SystemLookupFailure failure, int? httpCode = null, Exception? innerException = null) : Exception(Describe(failure, httpCode), innerException)
{

    /// <summary>Gets what went wrong.</summary>
    public SystemLookupFailure Failure { get; } = failure;

    /// <summary>Gets the HTTP status the catalog answered with, or <see langword="null"/>.</summary>
    public int? HttpCode { get; } = httpCode;

    private static string Describe(SystemLookupFailure failure, int? httpCode)
        => failure switch
        {
            SystemLookupFailure.Unreachable => "The system catalog could not be reached.",
            SystemLookupFailure.Http => $"The system catalog answered HTTP {httpCode}.",
            SystemLookupFailure.Unreadable => "The system catalog's answer could not be read.",
            SystemLookupFailure.TimedOut => "The system catalog did not answer in time.",
            _ => $"The system catalog failed: {failure}.",
        };
}

/// <summary>Looks a system's bodies up in a catalog outside the journal.</summary>
public interface ISystemCatalog
{
    /// <summary>The bodies a catalog knows for a system.</summary>
    /// <param name="systemAddress">The game's system address.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The bodies, empty when the catalog does not know the system.</returns>
    /// <exception cref="SystemCatalogException">The lookup gave no answer; <see cref="SystemCatalogException.Failure"/> says why.</exception>
    Task<IReadOnlyList<CataloguedBody>> BodiesAsync(long systemAddress, CancellationToken cancellationToken);
}
