using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Katey.Ui;

/// <summary>Puts text on the clipboard for whichever window a control is in.</summary>
internal static class ClipboardText
{
    /// <summary>Copies text.</summary>
    /// <param name="control">A control in the window whose clipboard is used.</param>
    /// <param name="text">What to copy.</param>
    /// <returns><see langword="true"/> when the text was copied; <see langword="false"/> when the platform has no clipboard.</returns>
    public static async Task<bool> CopyAsync(Control control, string text)
    {
        var clipboard = TopLevel.GetTopLevel(control)?.Clipboard;
        if (clipboard is null)
        {
            return false;
        }

        await clipboard.SetValueAsync(DataFormat.Text, text);
        return true;
    }
}
