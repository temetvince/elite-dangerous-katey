using Avalonia;
using Katey.App.Logging;
using Katey.App.Persistence;
using Microsoft.Extensions.Logging;

namespace Katey.Ui;

/// <summary>The desktop entry point.</summary>
internal static class Program
{
    /// <summary>
    /// Gets the log file every logger also writes to: <c>katey.log</c> beside the default saved data, milestones
    /// and trouble only unless the command line's <c>--log-level</c> names another floor, or <c>--verbose</c> asks for debug, every line scrubbed of
    /// folder paths and of the account and machine names. An exception that ends the process is written there
    /// first, so a crash leaves a trace without a debugger. <see cref="Main"/> opens it; the designer, which
    /// does not run <see cref="Main"/>, gets one at the default level.
    /// </summary>
    public static FileLoggerProvider FileLog { get; private set; } = OpenLog(LogArguments.DefaultLevel);

    /// <summary>Starts the application.</summary>
    /// <param name="args">Command-line arguments: <c>--log-level &lt;level&gt;</c> or <c>--verbose</c> sets the log file's floor; the rest pass through to Avalonia.</param>
    /// <returns>The process exit code.</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        var level = LogArguments.FileLevel(args);
        if (level != FileLog.Minimum)
        {
            FileLog.Dispose();
            FileLog = OpenLog(level);
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) => FileLog.WriteCrash(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => FileLog.WriteCrash(e.Exception);
        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            FileLog.WriteCrash(exception);
            throw;
        }
    }

    /// <summary>Configures Avalonia. Also used by the designer.</summary>
    /// <returns>The configured builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static FileLoggerProvider OpenLog(LogLevel level)
        => new(Path.Combine(Path.GetDirectoryName(JsonFileStateStore.DefaultPath())!, "katey.log"), level, scrub: LogScrubber.ForThisMachine().Scrub);
}
