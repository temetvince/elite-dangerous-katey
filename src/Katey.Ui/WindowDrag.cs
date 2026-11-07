using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Katey.Ui;

/// <summary>Moves a window by its own title bar, for windows that draw no system title bar.</summary>
internal static class WindowDrag
{
    /// <summary>Handles a press on a title bar: a left-button drag moves the window, a double click runs the maximise action.</summary>
    /// <param name="window">The window.</param>
    /// <param name="args">The press.</param>
    /// <param name="onDoubleClick">What a double click does, or <see langword="null"/> for nothing.</param>
    /// <remarks>A press that lands on a button or a text box is left to that control.</remarks>
    public static void Handle(Window window, PointerPressedEventArgs args, Action? onDoubleClick)
    {
        if (!args.GetCurrentPoint(window).Properties.IsLeftButtonPressed || IsInteractive(args.Source))
        {
            return;
        }

        if (args.ClickCount == 2)
        {
            onDoubleClick?.Invoke();
        }
        else
        {
            window.BeginMoveDrag(args);
        }

        args.Handled = true;
    }

    private static bool IsInteractive(object? source)
        => source is Visual visual
            && (visual.FindAncestorOfType<Button>(true) is not null
                || visual.FindAncestorOfType<TextBox>(true) is not null
                || visual.FindAncestorOfType<ToggleSwitch>(true) is not null);
}
