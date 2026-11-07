using Katey.App.Mvvm;

namespace Katey.App.Navigation;

/// <summary>A mention of some entity that opens its page when activated.</summary>
public sealed class Link
{
    /// <summary>Creates the link.</summary>
    /// <param name="label">The text shown.</param>
    /// <param name="target">The page it opens.</param>
    /// <param name="navigator">Where it opens.</param>
    public Link(string label, PageKey target, Navigator navigator)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(navigator);

        Label = label;
        Target = target;
        OpenCommand = new RelayCommand(() => navigator.Open(target));
    }

    /// <summary>Gets the text shown.</summary>
    public string Label { get; }

    /// <summary>Gets the page it opens.</summary>
    public PageKey Target { get; }

    /// <summary>Gets the command that opens the page.</summary>
    public RelayCommand OpenCommand { get; }

    /// <inheritdoc/>
    public override string ToString() => Label;
}
