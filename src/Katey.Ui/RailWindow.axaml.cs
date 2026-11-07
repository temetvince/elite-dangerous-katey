using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Katey.App.Rail;

namespace Katey.Ui;

/// <summary>
/// The live rail in a window of its own, always on top, over the same <see cref="RailViewModel"/> as the main
/// window. It remembers where it was, can be an overlay with a mostly transparent background, and can let
/// clicks pass through to the game underneath.
/// </summary>
/// <remarks>
/// Closing the window with its own close button brings the rail back into the main window. Click-through is a
/// Win32 extended window style, so it does nothing on other platforms. The window sits above a game running
/// in a window or borderless; an exclusive full-screen game covers every other window.
/// </remarks>
public sealed partial class RailWindow : Window
{
    private const int DefaultWidth = 380;
    private const int DefaultHeight = 720;
    private const int DefaultMargin = 24;

    private readonly DispatcherTimer _boundsTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private bool _closingForAttach;
    private bool _opened;

    /// <summary>Creates the window.</summary>
    public RailWindow()
    {
        InitializeComponent();
        _boundsTimer.Tick += (_, _) =>
        {
            _boundsTimer.Stop();
            RememberBounds();
        };
        Opened += (_, _) =>
        {
            // The decorations go on before the remembered bounds. Dropping or restoring the system frame resizes
            // the client area at once, and Avalonia writes that size into Width and Height, which would discard
            // a remembered size set before it was applied.
            ApplyOverlay();
            ApplyClickThrough();
            PlaceFromMemory();
            _opened = true;
        };
        PositionChanged += (_, _) => ScheduleRemember();
        SizeChanged += (_, _) => ScheduleRemember();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is RailViewModel rail)
            {
                rail.PropertyChanged += OnRailChanged;
            }
        };
        Closing += (_, _) =>
        {
            _boundsTimer.Stop();
            RememberBounds();
            if (DataContext is RailViewModel rail)
            {
                rail.PropertyChanged -= OnRailChanged;
                if (!_closingForAttach)
                {
                    rail.IsDetached = false;
                }
            }
        };
    }

    private RailViewModel? Rail => DataContext as RailViewModel;

    /// <summary>Closes the window because the rail is being docked again, without changing the view model.</summary>
    public void CloseForAttach()
    {
        _closingForAttach = true;
        Close();
    }

    private void CloseWindow(object? sender, RoutedEventArgs args) => Close();

    private void StripPressed(object? sender, PointerPressedEventArgs args) => WindowDrag.Handle(this, args, null);

    private void OnRailChanged(object? sender, PropertyChangedEventArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(RailViewModel.IsOverlay):
                ApplyOverlay();
                break;
            case nameof(RailViewModel.IsClickThrough):
                ApplyClickThrough();
                break;
            case nameof(RailViewModel.WindowBounds) when _opened && Rail is { WindowBounds.Length: 0 }:
                PlaceAtDefault();
                break;
            default:
                break;
        }
    }

    /// <summary>Puts the window where it was last, when that place is still on a screen, else at the default place.</summary>
    private void PlaceFromMemory()
    {
        if (Rail is { } rail
            && RailViewModel.TryParseBounds(rail.WindowBounds, out var x, out var y, out var width, out var height)
            && Screens.ScreenFromPoint(new PixelPoint(x + 40, y + 40)) is not null)
        {
            Position = new PixelPoint(x, y);
            Width = Math.Max(MinWidth, width);
            Height = Math.Max(MinHeight, height);
            return;
        }

        PlaceAtDefault();
    }

    /// <summary>Puts the window at its default size in the top right of the primary screen's working area.</summary>
    private void PlaceAtDefault()
    {
        Width = DefaultWidth;
        Height = DefaultHeight;
        if (Screens.Primary is { } screen)
        {
            var area = screen.WorkingArea;
            var scale = screen.Scaling;
            var x = area.Right - (int)Math.Round(DefaultWidth * scale) - DefaultMargin;
            var y = area.Y + DefaultMargin;
            Position = new PixelPoint(Math.Max(area.X, x), y);
        }

        _boundsTimer.Stop();
        RememberBounds();
    }

    private void ScheduleRemember()
    {
        if (_opened)
        {
            _boundsTimer.Stop();
            _boundsTimer.Start();
        }
    }

    private void RememberBounds()
    {
        if (_opened && Rail is { } rail && WindowState == WindowState.Normal)
        {
            rail.WindowBounds = string.Create(CultureInfo.InvariantCulture, $"{Position.X},{Position.Y},{(int)Math.Round(Width)},{(int)Math.Round(Height)}");
        }
    }

    /// <summary>A mostly transparent window, so the game shows through, or the rail's solid dark ground.</summary>
    private void ApplyOverlay()
    {
        var overlay = Rail?.IsOverlay == true;
        Classes.Set("overlay", overlay);
        TransparencyLevelHint = overlay ? [WindowTransparencyLevel.Transparent] : [WindowTransparencyLevel.None];
        Background = overlay ? Brushes.Transparent : this.FindResource("RailBg") as IBrush ?? Brushes.Black;
    }

    /// <summary>
    /// Lets clicks fall through to whatever is under the window, or takes them again. While clicks fall
    /// through, the system's frame goes too: nothing on the window can be grabbed, so the frame would only
    /// mark where the overlay ends.
    /// </summary>
    private void ApplyClickThrough()
    {
        var through = Rail?.IsClickThrough == true;
        WindowDecorations = through ? WindowDecorations.None : WindowDecorations.BorderOnly;
        CanResize = !through;
        if (!OperatingSystem.IsWindows() || TryGetPlatformHandle()?.Handle is not { } handle || handle == 0)
        {
            return;
        }

        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.ExStyle);
        var next = through
            ? style | NativeMethods.Layered | NativeMethods.Transparent
            : style & ~NativeMethods.Transparent;
        if (next != style)
        {
            _ = NativeMethods.SetWindowLongPtr(handle, NativeMethods.ExStyle, next);
        }
    }

    /// <summary>The two Win32 calls that turn a window's extended style bits on and off.</summary>
    [SupportedOSPlatform("windows")]
    private static partial class NativeMethods
    {
        public const int ExStyle = -20;
        public const nint Transparent = 0x20;
        public const nint Layered = 0x80000;

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        public static partial nint GetWindowLongPtr(nint window, int index);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        public static partial nint SetWindowLongPtr(nint window, int index, nint value);
    }
}
