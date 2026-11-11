using System.Reflection;

namespace Katey.App;

/// <summary>Who this build is: Katey's version and the address of the project, as every outgoing request names them.</summary>
/// <remarks>
/// The version is the assembly's informational version with any build metadata dropped, so a build stamped
/// <c>1.2.0+abc123</c> reads as <c>1.2.0</c>. A build no script stamped reads as <c>1.0.0</c>, the SDK's default.
/// </remarks>
public static class AppVersion
{
    /// <summary>Where anyone reading a server's logs can find out what Katey is.</summary>
    public const string ProjectUrl = "https://github.com/temetvince/elite-dangerous-katey";

    /// <summary>Gets the version as text, such as <c>1.2.0</c>. Never empty.</summary>
    public static string Text { get; } = Read();

    /// <summary>Gets the version as a number for comparing with a release's, parsed from <see cref="Text"/>; <c>1.0.0</c> when the text is not a version.</summary>
    public static Version Current { get; } = Version.TryParse(Text, out var parsed) ? parsed : new Version(1, 0, 0);

    /// <summary>
    /// Gets the user agent every request carries: Katey's name, its version and the project's address, such as
    /// <c>Katey/1.2.0 (+https://github.com/temetvince/elite-dangerous-katey)</c>. Spansh asks this of tools that use its API.
    /// </summary>
    public static string UserAgent { get; } = $"Katey/{Text} (+{ProjectUrl})";

    private static string Read()
    {
        var informational = typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus > 0 ? informational[..plus] : informational;
    }
}
