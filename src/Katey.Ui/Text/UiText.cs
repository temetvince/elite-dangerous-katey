using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Utilities;
using Katey.Localization;

namespace Katey.Ui.Text;

/// <summary>
/// The window chrome's words, from <c>UiStrings.resx</c> and its translations: headings, buttons, tooltips,
/// the folder picker's titles and the clipboard notices. Code-behind reads <see cref="Get"/>; XAML binds
/// through <see cref="TExtension"/>, whose texts re-evaluate when <see cref="Language.Changed"/> fires.
/// </summary>
public sealed class UiText : INotifyPropertyChanged
{
    private static readonly ConditionalWeakTable<object, List<LocalizedText>> Anchors = [];
    private static readonly List<LocalizedText> Unanchored = [];

    private UiText() => Language.Changed += (_, _) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

    /// <summary>Raised with an empty property name when the language changes: every text is stale.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the one instance every bound text listens to.</summary>
    public static UiText Instance { get; } = new();

    /// <summary>Gets the localizer over the chrome's table.</summary>
    public static Localizer Localizer { get; } = new(new ResourceTextSource(new ResourceManager("Katey.Ui.Text.UiStrings", typeof(UiText).Assembly)));

    /// <summary>The text for a key.</summary>
    /// <param name="key">A key from <see cref="UiKeys"/>.</param>
    /// <returns>The text in the language in force, or the key when the table lacks it.</returns>
    public static string Get(string key) => Localizer.Get(key);

    /// <summary>A template filled by name.</summary>
    /// <param name="key">A key from <see cref="UiKeys"/> whose text has placeholders.</param>
    /// <param name="args">Name → value.</param>
    /// <returns>The text.</returns>
    public static string Format(string key, params (string Name, object? Value)[] args) => Localizer.Format(key, args);

    /// <summary>
    /// Keeps a bound text alive as long as the object it is bound on. Avalonia holds a binding's source weakly, so
    /// a text held by nothing else is collected and stops following the language; the control it serves holds it
    /// here instead, and lets go of it when the control itself goes.
    /// </summary>
    /// <param name="target">The object the text is bound on, or <see langword="null"/> when the markup gave none; such a text is kept for the life of the process.</param>
    /// <param name="text">The bound text.</param>
    internal static void Anchor(object? target, LocalizedText text)
    {
        if (target is null)
        {
            lock (Unanchored)
            {
                Unanchored.Add(text);
            }

            return;
        }

        var texts = Anchors.GetOrCreateValue(target);
        lock (texts)
        {
            texts.Add(text);
        }
    }
}

/// <summary>
/// One bound text: a key's words in the language in force, in the case the markup asked for. It listens to
/// <see cref="UiText.Instance"/> through a weak subscription, so a window that comes and goes, such as the rail's,
/// leaves nothing behind, and raises its own change when the language does.
/// </summary>
public sealed class LocalizedText : INotifyPropertyChanged, IWeakEventSubscriber<PropertyChangedEventArgs>
{
    /// <summary>Creates the text and starts listening for language changes.</summary>
    /// <param name="key">A key from <see cref="UiKeys"/>.</param>
    /// <param name="upper">Whether the text is set in capitals.</param>
    /// <param name="lower">Whether the text is set in lower case.</param>
    public LocalizedText(string key, bool upper, bool lower)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
        Upper = upper;
        Lower = lower;
        WeakEvents.ThreadSafePropertyChanged.Subscribe(UiText.Instance, this);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the key.</summary>
    public string Key { get; }

    /// <summary>Gets whether the text is set in capitals.</summary>
    public bool Upper { get; }

    /// <summary>Gets whether the text is set in lower case.</summary>
    public bool Lower { get; }

    /// <summary>Gets the words, upper- or lower-cased in the formatting culture when asked.</summary>
    public string Value
    {
        get
        {
            var text = UiText.Get(Key);
            return Upper ? text.ToUpper(CultureInfo.CurrentCulture) : Lower ? text.ToLower(CultureInfo.CurrentCulture) : text;
        }
    }

    /// <inheritdoc/>
    public void OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
}

/// <summary>
/// The XAML markup extension that binds a text to a key: <c>{loc:T Key}</c>, <c>{loc:T Key, Upper=True}</c> for a
/// heading set in capitals, or <c>{loc:T Key, Lower=True}</c> for a small label set in lower case. Each use binds to
/// its own <see cref="LocalizedText"/>, anchored to the control it is set on, which follows the language switch.
/// </summary>
public sealed class TExtension
{
    /// <summary>Creates the extension for a key.</summary>
    /// <param name="key">A key from <see cref="UiKeys"/>.</param>
    public TExtension(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
    }

    /// <summary>Gets the key.</summary>
    public string Key { get; }

    /// <summary>Gets or sets whether the text is set in capitals, as a column heading is. The table keeps the words in their natural case.</summary>
    public bool Upper { get; set; }

    /// <summary>Gets or sets whether the text is set in lower case, as the plan drawer's small column labels are.</summary>
    public bool Lower { get; set; }

    /// <summary>The binding XAML uses.</summary>
    /// <param name="serviceProvider">The XAML services; the <see cref="IProvideValueTarget"/> among them names the object the text is set on, which then keeps the text alive.</param>
    /// <returns>A one-way binding to a <see cref="LocalizedText"/> for the key.</returns>
    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var text = new LocalizedText(Key, Upper, Lower);
        UiText.Anchor((serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget)?.TargetObject, text);
        return new ReflectionBinding(nameof(LocalizedText.Value))
        {
            Source = text,
            Mode = BindingMode.OneWay,
        };
    }
}
