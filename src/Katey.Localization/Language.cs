using System.Globalization;

namespace Katey.Localization;

/// <summary>
/// The one language switch. Setting it changes the UI culture of the process and raises <see cref="Changed"/>,
/// which every text table and every live view follows. The formatting culture, which decides how numbers
/// and dates look, is not touched: it stays the regional setting of the machine.
/// </summary>
/// <remarks>Static because the UI culture it mirrors is process-wide. Set from the UI thread; handlers run on the caller's thread.</remarks>
public static class Language
{
    /// <summary>Raised after the language changes.</summary>
    public static event EventHandler? Changed;

    /// <summary>Gets the language in force: the current UI culture.</summary>
    public static CultureInfo Current => CultureInfo.CurrentUICulture;

    /// <summary>Switches the language.</summary>
    /// <param name="culture">The culture every lookup uses from now on, for this thread and for every thread started after.</param>
    public static void Set(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Picks the language to run in: a saved choice when it names a supported culture, else the closest
    /// supported match to the machine's culture, else the first supported culture.
    /// </summary>
    /// <param name="saved">The saved culture name, such as <c>de</c> or <c>pt-BR</c>; <see langword="null"/> or empty to follow the machine.</param>
    /// <param name="machine">The machine's UI culture.</param>
    /// <param name="supported">The cultures tables exist for. The first is the fallback and must be present.</param>
    /// <returns>One of <paramref name="supported"/>.</returns>
    public static CultureInfo Resolve(string? saved, CultureInfo machine, IReadOnlyList<CultureInfo> supported)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(supported);
        if (supported.Count == 0)
        {
            throw new ArgumentException("At least one supported culture is required.", nameof(supported));
        }

        if (!string.IsNullOrWhiteSpace(saved) && Find(supported, saved) is { } chosen)
        {
            return chosen;
        }

        for (var culture = machine; culture.Name.Length > 0; culture = culture.Parent)
        {
            if (Find(supported, culture.Name) is { } match)
            {
                return match;
            }
        }

        return supported[0];
    }

    private static CultureInfo? Find(IReadOnlyList<CultureInfo> supported, string name)
        => supported.FirstOrDefault(culture => string.Equals(culture.Name, name, StringComparison.OrdinalIgnoreCase));
}
