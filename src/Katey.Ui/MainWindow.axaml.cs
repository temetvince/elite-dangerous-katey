using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Katey.App;
using Katey.App.Pages;
using Katey.App.Rail;
using Katey.App.Search;
using Katey.App.Text;
using Katey.Ui.Text;

namespace Katey.Ui;

/// <summary>The main window: the search box, the workbench, the docked rail and the plan drawer, over <see cref="ShellViewModel"/>.</summary>
/// <remarks>
/// Everything that needs the platform lives here: opening and closing the rail's own window, the folder
/// picker, the clipboard, the keyboard shortcuts for the search box, and remembering how far each page
/// was scrolled so back and forward return to the same place.
/// </remarks>
public sealed partial class MainWindow : Window
{
    /// <summary>The narrowest the pages look right at; the window adds the docked rail's width to it.</summary>
    private const double PagesMinWidth = 960;

    /// <summary>The docked rail's width, as the window's markup fixes it.</summary>
    private const double DockedRailWidth = 340;

    private RailWindow? _railWindow;
    private bool _wired;

    /// <summary>Creates the window.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            ApplyMinWidth();
            PlaceOnPrimaryScreen();
            if (Shell is { } shell && !_wired)
            {
                _wired = true;
                shell.Rail.PropertyChanged += OnRailChanged;
                shell.Navigator.Navigating += (_, _) => RecordScroll();
                shell.Navigator.Navigated += (_, _) => RestoreScroll();
                SyncRailWindow();
                shell.Start();
            }
        };
        Closing += (_, _) => _railWindow?.CloseForAttach();
        KeyDown += OnWindowKeyDown;
    }

    private ShellViewModel? Shell => DataContext as ShellViewModel;

    /// <summary>Keeps the pages at least <see cref="PagesMinWidth"/> wide: the window's minimum grows by the rail's width while the rail is docked, and shrinks again when it pops out.</summary>
    private void ApplyMinWidth()
    {
        var min = Shell?.Rail.IsDetached == true ? PagesMinWidth : PagesMinWidth + DockedRailWidth;
        MinWidth = min;
        if (WindowState == WindowState.Normal && Width < min)
        {
            Width = min;
        }
    }

    /// <summary>Opens the window centred on the primary screen, no larger than its working area except where the minimum width says otherwise, whatever screen the system would have chosen.</summary>
    private void PlaceOnPrimaryScreen()
    {
        if (WindowState != WindowState.Normal || Screens.Primary is not { } screen)
        {
            return;
        }

        var area = screen.WorkingArea;
        var scale = screen.Scaling;
        Width = Math.Min(Width, Math.Floor(area.Width / scale));
        Height = Math.Min(Height, Math.Floor(area.Height / scale));
        var x = area.X + (int)Math.Round((area.Width - (Width * scale)) / 2);
        var y = area.Y + (int)Math.Round((area.Height - (Height * scale)) / 2);
        Position = new PixelPoint(Math.Max(area.X, x), Math.Max(area.Y, y));
    }

    private ScrollViewer? Workbench => this.FindControl<ScrollViewer>("WorkbenchScroll");

    /// <summary>A language chosen in the picker: the shell switches to it and the picker closes. The choice is read off the clicked button, as the plot picker reads its entries, since a command bound inside a flyout's popup does not reach the shell.</summary>
    /// <param name="sender">The language's button, whose data context is the option.</param>
    /// <param name="args">The click.</param>
    private void LanguageChosen(object? sender, RoutedEventArgs args)
    {
        if (Shell is { } shell && sender is Button { DataContext: LanguageOption option })
        {
            shell.SetLanguageCommand.Execute(option);
        }

        this.FindControl<Button>("LanguageButton")?.Flyout?.Hide();
    }

    private void RecordScroll()
    {
        if (Shell is not { } shell || Workbench is not { } scroll)
        {
            return;
        }

        var scrollable = scroll.Extent.Height - scroll.Viewport.Height;
        shell.Navigator.RecordScroll(scrollable > 0 ? scroll.Offset.Y / scrollable : 0);
    }

    private void RestoreScroll()
    {
        if (Shell is not { } shell || Workbench is not { } scroll)
        {
            return;
        }

        var fraction = shell.Navigator.ScrollFraction;
        // The new page has not been measured yet; apply the offset once layout has run, and once more a
        // frame later for content whose height settles after the first pass.
        Dispatcher.UIThread.Post(() => Apply(scroll, fraction), DispatcherPriority.Loaded);
        Dispatcher.UIThread.Post(() => Apply(scroll, fraction), DispatcherPriority.Background);

        static void Apply(ScrollViewer scroll, double fraction)
        {
            var scrollable = Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height);
            scroll.Offset = scroll.Offset.WithY(fraction * scrollable);
        }
    }

    private void OnRailChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(RailViewModel.IsDetached))
        {
            SyncRailWindow();
            ApplyMinWidth();
        }
    }

    private void SyncRailWindow()
    {
        if (Shell is not { } shell)
        {
            return;
        }

        if (shell.Rail.IsDetached && _railWindow is null)
        {
            _railWindow = new RailWindow { DataContext = shell.Rail };
            _railWindow.Closed += (_, _) => _railWindow = null;
            _railWindow.Show();
        }
        else if (!shell.Rail.IsDetached && _railWindow is { } window)
        {
            _railWindow = null;
            window.CloseForAttach();
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.K && args.KeyModifiers.HasFlag(KeyModifiers.Control) && this.FindControl<TextBox>("SearchBox") is { } box)
        {
            box.Focus();
            box.SelectAll();
            args.Handled = true;
        }
    }

    private Flyout? _plotPicker;

    /// <summary>The plot's ship-or-fit picker opened: the search box starts empty and takes the keyboard, so typing narrows the list at once.</summary>
    private void PlotPickerOpened(object? sender, EventArgs args)
    {
        _plotPicker = sender as Flyout;
        if (_plotPicker?.Content is Control content)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (content.FindDescendantOfType<TextBox>() is { } search)
                {
                    search.Text = "";
                    search.Focus();
                }
            });
        }
    }

    /// <summary>A click on an entry in the picker's list chooses it and closes the panel. Nothing else changes the choice.</summary>
    private void PlotPickerClicked(object? sender, PointerReleasedEventArgs args)
    {
        if (sender is ListBox { DataContext: RoutePage page }
            && (args.Source as Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true) is { DataContext: PlotTarget target })
        {
            page.SelectedPlotTarget = target;
            _plotPicker?.Hide();
        }
    }

    /// <summary>Enter in the picker's search box chooses the first entry listed; Escape closes the panel.</summary>
    private void PlotPickerKeyDown(object? sender, KeyEventArgs args)
    {
        if (sender is not TextBox { DataContext: RoutePage page })
        {
            return;
        }

        if (args.Key == Key.Enter)
        {
            if (page.PlotTargets.FirstOrDefault(target => target.Key != page.SelectedPlotTarget?.Key || page.PlotTargets.Count == 1) is { } first)
            {
                page.SelectedPlotTarget = first;
            }

            _plotPicker?.Hide();
            args.Handled = true;
        }
        else if (args.Key == Key.Escape)
        {
            _plotPicker?.Hide();
            args.Handled = true;
        }
    }

    private void SearchKeyDown(object? sender, KeyEventArgs args)
    {
        if (Shell is not { } shell)
        {
            return;
        }

        if (args.Key == Key.Enter)
        {
            shell.Search.OpenFirstCommand.Execute(null);
            args.Handled = true;
        }
        else if (args.Key == Key.Escape)
        {
            shell.Search.Query = "";
            args.Handled = true;
        }
    }

    private void ResultClicked(object? sender, RoutedEventArgs args)
    {
        if (Shell is { } shell && sender is Button { DataContext: SearchResult result })
        {
            shell.Search.Open(result);
        }
    }

    private async void BrowseFolder(object? sender, RoutedEventArgs args)
    {
        if (Shell?.Navigator.Current is SettingsPage settings && await PickFolderAsync(UiText.Get(UiKeys.Picker_JournalFolder)) is { } path)
        {
            settings.Folder = path;
        }
    }

    private async void BrowseBackupFolder(object? sender, RoutedEventArgs args)
    {
        if (Shell?.Navigator.Current is SettingsPage settings && await PickFolderAsync(UiText.Get(UiKeys.Picker_BackupFolder)) is { } path)
        {
            settings.BackupFolder = path;
        }
    }

    private async void BrowseDataFolder(object? sender, RoutedEventArgs args)
    {
        if (Shell?.Navigator.Current is SettingsPage settings && await PickFolderAsync(UiText.Get(UiKeys.Picker_DataFolder)) is { } path)
        {
            settings.DataFolder = path;
        }
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });
        return folders.Count > 0 && folders[0].TryGetLocalPath() is { } path ? path : null;
    }

    /// <summary>Scrolls the materials page to the row it landed on, once that row exists.</summary>
    private void MaterialRowLoaded(object? sender, RoutedEventArgs args)
    {
        if (sender is Control { DataContext: InventoryMaterialRow { IsHighlighted: true } } row)
        {
            row.BringIntoView();
        }
    }

    private async void CopySystemName(object? sender, RoutedEventArgs args)
    {
        if (Shell?.Navigator.Current is not SystemPage page || !page.HasName)
        {
            return;
        }

        page.CopyStatus = await ClipboardText.CopyAsync(this, page.Name) ? UiText.Format(UiKeys.Clipboard_CopiedName, ("name", page.Name)) : UiText.Get(UiKeys.Clipboard_Unavailable);
    }

    private async void CopyEdsy(object? sender, RoutedEventArgs args)
    {
        if (Shell?.Navigator.Current is not IEdsyExportable page || page.EdsyJson.Length == 0)
        {
            return;
        }

        page.ExportStatus = await ClipboardText.CopyAsync(this, page.EdsyJson) ? UiText.Get(UiKeys.Clipboard_CopiedForEdsy) : UiText.Get(UiKeys.Clipboard_Unavailable);
    }

    private async void CopyTotals(object? sender, RoutedEventArgs args)
    {
        if (Shell is not { } shell || this.FindControl<TextBlock>("CopyStatus") is not { } status)
        {
            return;
        }

        status.Text = await ClipboardText.CopyAsync(this, shell.PlanDrawer.Totals.Text) ? UiText.Get(UiKeys.Clipboard_Copied) : UiText.Get(UiKeys.Clipboard_Unavailable);
    }

    private async void CopyNextWaypoint(object? sender, RoutedEventArgs args)
    {
        if (Shell?.Navigator.Current is not RoutePage route || route.NextWaypoint.Length == 0)
        {
            return;
        }

        var copied = await ClipboardText.CopyAsync(this, route.NextWaypoint);
        if (this.FindControl<TextBlock>("WaypointStatus") is { } status)
        {
            status.Text = copied ? UiText.Format(UiKeys.Clipboard_CopiedName, ("name", route.NextWaypoint)) : UiText.Get(UiKeys.Clipboard_Unavailable);
        }
    }

    private async void CopyWaypoint(object? sender, RoutedEventArgs args)
    {
        if (sender is Button { DataContext: PlotStopRow stop })
        {
            await ClipboardText.CopyAsync(this, stop.System);
        }
    }

    private void MinimizeWindow(object? sender, RoutedEventArgs args) => WindowState = WindowState.Minimized;

    private void ToggleMaximize(object? sender, RoutedEventArgs args) => ToggleMaximize();

    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseWindow(object? sender, RoutedEventArgs args) => Close();

    private void TopBarPressed(object? sender, PointerPressedEventArgs args) => WindowDrag.Handle(this, args, ToggleMaximize);
}
