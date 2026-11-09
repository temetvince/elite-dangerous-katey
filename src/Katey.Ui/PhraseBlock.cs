using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Katey.App.ViewModels;

namespace Katey.Ui;

/// <summary>
/// A text block that sets a sentence of <see cref="PhrasePart"/>s: every part is a run of the same
/// paragraph, a link drawn in its tone, underlined while the pointer is over it, and opened on a click. The
/// links are text, not controls set into the text, so the paragraph wraps and aligns as any other and
/// nothing is templated while it is drawn. It is styled as a <see cref="TextBlock"/>, so every text class
/// applies; <see cref="LinkBrush"/>, <see cref="InkBrush"/> and <see cref="HudBrush"/> let a style, the rail's say, recolour the links.
/// </summary>
public sealed class PhraseBlock : TextBlock
{
    /// <summary>Defines the <see cref="Parts"/> property.</summary>
    public static readonly StyledProperty<IReadOnlyList<PhrasePart>?> PartsProperty =
        AvaloniaProperty.Register<PhraseBlock, IReadOnlyList<PhrasePart>?>(nameof(Parts));

    /// <summary>Defines the <see cref="LinkBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> LinkBrushProperty =
        AvaloniaProperty.Register<PhraseBlock, IBrush?>(nameof(LinkBrush));

    /// <summary>Defines the <see cref="InkBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> InkBrushProperty =
        AvaloniaProperty.Register<PhraseBlock, IBrush?>(nameof(InkBrush));

    /// <summary>Defines the <see cref="HudBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> HudBrushProperty =
        AvaloniaProperty.Register<PhraseBlock, IBrush?>(nameof(HudBrush));

    private readonly List<(int Start, int End, Run Run, PhrasePart Part)> _links = [];
    private int _hovered = -1;

    /// <summary>Gets or sets the sentence; <see langword="null"/> or empty shows nothing.</summary>
    public IReadOnlyList<PhrasePart>? Parts
    {
        get => GetValue(PartsProperty);
        set => SetValue(PartsProperty, value);
    }

    /// <summary>Gets or sets the brush for a link in the <see cref="LinkTone.Accent"/> tone; <see langword="null"/> takes the <c>KAccent</c> resource.</summary>
    public IBrush? LinkBrush
    {
        get => GetValue(LinkBrushProperty);
        set => SetValue(LinkBrushProperty, value);
    }

    /// <summary>Gets or sets the brush for a link in the <see cref="LinkTone.Ink"/> tone, and at seven tenths for <see cref="LinkTone.Dim"/>; <see langword="null"/> takes the <c>KInk</c> resource.</summary>
    public IBrush? InkBrush
    {
        get => GetValue(InkBrushProperty);
        set => SetValue(InkBrushProperty, value);
    }

    /// <summary>Gets or sets the brush for a link in the <see cref="LinkTone.Hud"/> tone; <see langword="null"/> takes the <c>KBurn</c> resource, the HUD blue.</summary>
    public IBrush? HudBrush
    {
        get => GetValue(HudBrushProperty);
        set => SetValue(HudBrushProperty, value);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(TextBlock);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PartsProperty || change.Property == LinkBrushProperty || change.Property == InkBrushProperty || change.Property == HudBrushProperty)
        {
            Rebuild();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Hover(LinkAt(e.GetPosition(this)));
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Hover(-1);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        var index = LinkAt(e.GetPosition(this));
        if (index >= 0 && _links[index].Part.Link is { } link && link.OpenCommand.CanExecute(null))
        {
            link.OpenCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Rebuild()
    {
        _links.Clear();
        _hovered = -1;
        var inlines = new InlineCollection();
        var accent = LinkBrush ?? Resource("KAccent") ?? Brushes.DodgerBlue;
        var ink = InkBrush ?? Resource("KInk") ?? Foreground ?? Brushes.White;
        var hud = HudBrush ?? Resource("KBurn") ?? accent;
        var dim = ink is ISolidColorBrush solid ? new ImmutableSolidColorBrush(solid.Color, 0.7) : ink;
        var offset = 0;
        foreach (var part in Parts ?? [])
        {
            var run = new Run(part.Text);
            if (part.IsLink)
            {
                run.Foreground = part.Tone switch
                {
                    LinkTone.Ink => ink,
                    LinkTone.Dim => dim,
                    LinkTone.Hud => hud,
                    LinkTone.Accent => accent,
                    _ => accent,
                };
                _links.Add((offset, offset + part.Text.Length, run, part));
            }

            inlines.Add(run);
            offset += part.Text.Length;
        }

        Inlines = inlines;
        // A block with links takes the pointer over its whole box; one without is left alone.
        Background = _links.Count > 0 ? Brushes.Transparent : null;
    }

    private IBrush? Resource(string key) => this.TryFindResource(key, ActualThemeVariant, out var found) && found is IBrush brush ? brush : null;

    /// <summary>The link under a point, or -1.</summary>
    private int LinkAt(Point point)
    {
        if (_links.Count == 0)
        {
            return -1;
        }

        var hit = TextLayout.HitTestPoint(new Point(point.X - Padding.Left, point.Y - Padding.Top));
        return hit.IsInside ? _links.FindIndex(link => hit.TextPosition >= link.Start && hit.TextPosition < link.End) : -1;
    }

    /// <summary>Underlines the link under the pointer and shows the hand, or clears both.</summary>
    private void Hover(int index)
    {
        if (index == _hovered)
        {
            return;
        }

        if (_hovered >= 0)
        {
            _links[_hovered].Run.TextDecorations = null;
        }

        _hovered = index;
        if (index >= 0)
        {
            _links[index].Run.TextDecorations = Avalonia.Media.TextDecorations.Underline;
        }

        Cursor = index >= 0 ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
    }
}
