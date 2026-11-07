using Avalonia.Controls;

namespace Katey.Ui;

/// <summary>A table column heading bound to a <see cref="Katey.App.Mvvm.SortHeader"/>: click to sort, with the direction mark.</summary>
public sealed partial class SortHeaderView : UserControl
{
    /// <summary>Creates the view.</summary>
    public SortHeaderView() => InitializeComponent();
}
