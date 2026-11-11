using System.Resources;
using Katey.Localization;

namespace Katey.App.Text;

/// <summary>
/// The application's words, from <c>Strings.resx</c> and its translations, in the language in force. Every
/// user-visible string a view model produces comes through here by a key from <see cref="Keys"/>; the
/// developer-facing text in logs and exceptions does not. Named for the table it reads, <c>Strings.resx</c>.
/// </summary>
/// <remarks>Static because the table and the language are process-wide; see <see cref="Language"/>. Thread-safe.</remarks>
public static class Strings
{
    /// <summary>Gets the localizer over the application's table, for callers that need more than the shortcuts below.</summary>
    public static Localizer Localizer { get; } = new(new ResourceTextSource(new ResourceManager("Katey.App.Text.Strings", typeof(Strings).Assembly)));

    /// <summary>The text for a key.</summary>
    /// <param name="key">A key from <see cref="Keys"/>.</param>
    /// <returns>The text in the language in force, or the key when the table lacks it.</returns>
    public static string Get(string key) => Localizer.Get(key);

    /// <summary>A template filled by name.</summary>
    /// <param name="key">A key from <see cref="Keys"/> whose text has placeholders.</param>
    /// <param name="args">Name → value, written with the placeholder's format in the formatting culture.</param>
    /// <returns>The text.</returns>
    public static string Format(string key, params (string Name, object? Value)[] args) => Localizer.Format(key, args);

    /// <summary>A counted noun or sentence in the plural form the language takes.</summary>
    /// <param name="key">A plural base key from <see cref="Keys"/>.</param>
    /// <param name="count">The count, written into <c>{count}</c>.</param>
    /// <param name="args">Further name → value pairs for the form's other placeholders.</param>
    /// <returns>The text.</returns>
    public static string Plural(string key, long count, params (string Name, object? Value)[] args) => Localizer.Plural(key, count, args);
}
