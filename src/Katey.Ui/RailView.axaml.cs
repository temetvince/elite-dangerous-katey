using Avalonia.Controls;
using Avalonia.Interactivity;
using Katey.App.Rail;
using Katey.Ui.Text;

namespace Katey.Ui;

/// <summary>The live rail's view, used both docked in the main window and inside <see cref="RailWindow"/>.</summary>
public sealed partial class RailView : UserControl
{
    /// <summary>Creates the view.</summary>
    public RailView() => InitializeComponent();

    private async void CopyNextWaypoint(object? sender, RoutedEventArgs args)
    {
        if (DataContext is not RailViewModel rail || rail.NextWaypoint.Length == 0 || this.FindControl<TextBlock>("PlotCopyStatus") is not { } status)
        {
            return;
        }

        status.Text = await ClipboardText.CopyAsync(this, rail.NextWaypoint) ? UiText.Format(UiKeys.Clipboard_CopiedName, ("name", rail.NextWaypoint)) : UiText.Get(UiKeys.Clipboard_Unavailable);
    }
}
