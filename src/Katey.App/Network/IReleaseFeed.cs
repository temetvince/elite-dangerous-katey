namespace Katey.App.Network;

/// <summary>Why a release lookup gave no answer. The application words each kind; the exception carries only the facts.</summary>
public enum ReleaseLookupFailure
{
    /// <summary>The feed could not be reached.</summary>
    Unreachable,

    /// <summary>The feed answered with an HTTP status in <see cref="ReleaseFeedException.HttpCode"/> other than success.</summary>
    Http,

    /// <summary>The feed's answer was not readable as a release: not the expected JSON, or a tag that is not a version.</summary>
    Unreadable,

    /// <summary>The feed did not answer in time.</summary>
    TimedOut,
}

/// <summary>A release lookup that gave no answer. Its message is developer text for the log; the user's sentence is worded from <see cref="Failure"/>.</summary>
/// <remarks>Creates the exception.</remarks>
/// <param name="failure">What went wrong.</param>
/// <param name="httpCode">The HTTP status for an <see cref="ReleaseLookupFailure.Http"/> failure, or <see langword="null"/>.</param>
/// <param name="innerException">The transport or parsing failure behind it, or <see langword="null"/>.</param>
public sealed class ReleaseFeedException(ReleaseLookupFailure failure, int? httpCode = null, Exception? innerException = null) : Exception(Describe(failure, httpCode), innerException)
{
    /// <summary>Gets what went wrong.</summary>
    public ReleaseLookupFailure Failure { get; } = failure;

    /// <summary>Gets the HTTP status the feed answered with, or <see langword="null"/>.</summary>
    public int? HttpCode { get; } = httpCode;

    private static string Describe(ReleaseLookupFailure failure, int? httpCode)
        => failure switch
        {
            ReleaseLookupFailure.Unreachable => "The release feed could not be reached.",
            ReleaseLookupFailure.Http => $"The release feed answered HTTP {httpCode}.",
            ReleaseLookupFailure.Unreadable => "The release feed's answer could not be read as a release.",
            ReleaseLookupFailure.TimedOut => "The release feed did not answer in time.",
            _ => $"The release feed failed: {failure}.",
        };
}

/// <summary>A published release of Katey.</summary>
/// <param name="Version">The release's version, parsed from its tag.</param>
/// <param name="Text">The version as the release names it, such as <c>1.4.0</c>, for showing.</param>
/// <param name="Page">The release's page, where the build is.</param>
public sealed record Release(Version Version, string Text, Uri Page);

/// <summary>Knows the latest published release of Katey.</summary>
public interface IReleaseFeed
{
    /// <summary>The latest release that is neither a draft nor a pre-release.</summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The release.</returns>
    /// <exception cref="ReleaseFeedException">The lookup gave no answer; <see cref="ReleaseFeedException.Failure"/> says why.</exception>
    Task<Release> LatestAsync(CancellationToken cancellationToken);
}
