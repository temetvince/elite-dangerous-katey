using Avalonia.Controls;

namespace Katey.Ui;

/// <summary>A page's heading: breadcrumb links, kicker and title, bound to a <see cref="Katey.App.Pages.Page"/>.</summary>
public sealed partial class PageHeaderView : UserControl
{
    /// <summary>Creates the view.</summary>
    public PageHeaderView() => InitializeComponent();
}
