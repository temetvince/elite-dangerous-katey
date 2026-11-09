using Microsoft.Extensions.Logging;

namespace Katey.App.Logging;

/// <summary>Reads the log settings a command line carries: <c>--log-level &lt;level&gt;</c>, <c>--log-level=&lt;level&gt;</c>, or <c>--verbose</c> for debug.</summary>
public static class LogArguments
{
    /// <summary>The option that names the log file's floor.</summary>
    public const string LevelOption = "--log-level";

    /// <summary>The shorthand for <c>--log-level debug</c>.</summary>
    public const string VerboseOption = "--verbose";

    /// <summary>The floor when the command line names none.</summary>
    public const LogLevel DefaultLevel = LogLevel.Information;

    /// <summary>The level names the option accepts, lowest first.</summary>
    public static IReadOnlyList<string> LevelNames { get; } = ["trace", "debug", "information", "warning", "error"];

    /// <summary>The log file's floor from a command line.</summary>
    /// <param name="args">The arguments as given to the process.</param>
    /// <returns>The level after <see cref="LevelOption"/>, read without regard to case, <c>info</c> standing for information, or debug for <see cref="VerboseOption"/>; <see cref="DefaultLevel"/> when neither option is given or the value is not a level name. The last occurrence wins.</returns>
    public static LogLevel FileLevel(IEnumerable<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var level = DefaultLevel;
        string? pending = null;
        foreach (var arg in args)
        {
            if (pending is not null)
            {
                level = Parse(arg) ?? level;
                pending = null;
                continue;
            }

            if (string.Equals(arg, VerboseOption, StringComparison.OrdinalIgnoreCase))
            {
                level = LogLevel.Debug;
            }
            else if (string.Equals(arg, LevelOption, StringComparison.OrdinalIgnoreCase))
            {
                pending = arg;
            }
            else if (arg.StartsWith(LevelOption + "=", StringComparison.OrdinalIgnoreCase))
            {
                level = Parse(arg[(LevelOption.Length + 1)..]) ?? level;
            }
        }

        return level;
    }

    private static LogLevel? Parse(string text) => text.Trim().ToLowerInvariant() switch
    {
        "trace" => LogLevel.Trace,
        "debug" => LogLevel.Debug,
        "information" or "info" => LogLevel.Information,
        "warning" or "warn" => LogLevel.Warning,
        "error" => LogLevel.Error,
        _ => null,
    };
}
