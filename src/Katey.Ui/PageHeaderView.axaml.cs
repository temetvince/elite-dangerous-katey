using Avalonia;
using Avalonia.Controls;

namespace Katey.Ui;

/// <summary>
/// A page's heading: breadcrumb links, kicker and title, bound to a <see cref="Katey.App.Pages.Page"/>, with room
/// to the right of the title for whatever the page puts in <see cref="Beside"/>.
/// </summary>
public sealed partial class PageHeaderView : UserControl
{
    /// <summary>Defines the <see cref="Beside"/> property.</summary>
    public static readonly StyledProperty<object?> BesideProperty = AvaloniaProperty.Register<PageHeaderView, object?>(nameof(Beside));

    /// <summary>Creates the view.</summary>
    public PageHeaderView() => InitializeComponent();

    /// <summary>Gets or sets what sits to the right of the title, vertically centred on it, such as a page's copy button; <see langword="null"/> for nothing. It inherits the page as its data context.</summary>
    public object? Beside
    {
        get => GetValue(BesideProperty);
        set => SetValue(BesideProperty, value);
    }
}
