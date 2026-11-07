using Avalonia.Controls;
using Avalonia.Layout;

namespace Katey.Ui;

/// <summary>
/// Keeps Katey to one running copy per user. Two copies would read the same journal and save the same
/// state file, each overwriting the other's changes, so the second one to start shows a notice and exits.
/// </summary>
public static class SingleInstance
{
    private const string MutexName = @"Local\Katey.SingleInstance";
    private static Mutex? _held;

    /// <summary>Claims the instance. Call once at start; the claim lasts until <see cref="Release"/> or the process ends.</summary>
    /// <returns><see langword="true"/> when this is the only Katey running for the user.</returns>
    public static bool TryClaim()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (createdNew)
        {
            _held = mutex;
            return true;
        }

        mutex.Dispose();
        return false;
    }

    /// <summary>Gives the claim up, so the next Katey to start may run. Safe to call without a claim.</summary>
    public static void Release()
    {
        _held?.ReleaseMutex();
        _held?.Dispose();
        _held = null;
    }

    /// <summary>A small window telling the player Katey is already open, with one button that closes it.</summary>
    /// <returns>The window, not yet shown.</returns>
    public static Window AlreadyRunningWindow()
    {
        var window = new Window
        {
            Title = "Katey",
            Width = 380,
            Height = 150,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        var close = new Button { Content = "Close", HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => window.Close();
        window.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 14,
            Children =
            {
                new TextBlock { Text = "Katey is already running. Use the window that is open; a second copy would overwrite its saved data.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                close,
            },
        };
        return window;
    }
}
