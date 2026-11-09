using Avalonia;
using Katey.App.Logging;
using Katey.App.Persistence;

namespace Katey.Ui;

/// <summary>The desktop entry point.</summary>
internal static class Program
{
    /// <summary>
    /// Gets the log file every logger also writes to: <c>katey.log</c> beside the default saved data, milestones
    /// and trouble only. An exception that ends the process is written there first, so a crash leaves a trace
    /// without a debugger.
    /// </summary>
    public static FileLoggerProvider FileLog { get; } = new(Path.Combine(Path.GetDirectoryName(JsonFileStateStore.DefaultPath())!, "katey.log"));

    /// <summary>Starts the application.</summary>
    /// <param name="args">Command-line arguments, passed through to Avalonia.</param>
    /// <returns>The process exit code.</returns>
    [STAThread]
    public static int Main(string[] args)
    {
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
}
