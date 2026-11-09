using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Katey.App;
using Katey.App.Exploration;
using Katey.App.Persistence;
using Katey.App.Routing;
using Katey.Core;
using Katey.Core.Exploration;
using Katey.Core.Fleet;
using Katey.Journal;
using Microsoft.Extensions.Logging;

namespace Katey.Ui;

/// <summary>The application: one main window, an optional rail window, one journal monitor and one HTTP client for its lifetime.</summary>
public sealed partial class App : Application
{
    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!SingleInstance.TryClaim())
            {
                var hostLogger = Program.FileLog.CreateLogger(nameof(App));
                Log.AlreadyRunning(hostLogger);
                desktop.MainWindow = SingleInstance.AlreadyRunningWindow();
                base.OnFrameworkInitializationCompleted();
                return;
            }

            // The monitor and the logger factory live exactly as long as the desktop lifetime; its Exit
            // event is the one place they are disposed.
            // The debugger output costs a formatted line per message whether or not anything listens, so it is
            // attached only when a debugger is; otherwise the factory runs at the file's own floor.
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(Program.FileLog);
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    builder.AddDebug().SetMinimumLevel(LogLevel.Debug);
                }
                else
                {
                    builder.SetMinimumLevel(Program.FileLog.Minimum);
                }
            });
            var sinks = new JournalSinks();
            var monitor = new JournalMonitor(loggerFactory.CreateLogger<JournalMonitor>(), sinks.All);
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var shell = new ShellViewModel(
                Catalog.LoadEmbedded(),
                ModuleCatalog.LoadEmbedded(),
                ExobiologyPredictor.LoadEmbedded(),
                JsonFileStateStore.Configured(loggerFactory.CreateLogger<JsonFileStateStore>(), JsonFileStateStore.DefaultPath(), JsonFileStateStore.DefaultPointerPath()),
                monitor,
                sinks,
                new SpanshPlotter(http, loggerFactory.CreateLogger<SpanshPlotter>()),
                new SpanshSystemCatalog(http, loggerFactory.CreateLogger<SpanshSystemCatalog>()),
                new JournalBackup(loggerFactory.CreateLogger<JournalBackup>()),
                action => Dispatcher.UIThread.Post(action),
                loggerFactory.CreateLogger<ShellViewModel>());

            desktop.MainWindow = new MainWindow { DataContext = shell };
            desktop.Exit += (_, _) =>
            {
                shell.Shutdown();
                monitor.Dispose();
                http.Dispose();
                loggerFactory.Dispose();
                SingleInstance.Release();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
