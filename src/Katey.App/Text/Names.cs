using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Katey.Core;
using Katey.Core.Fleet;
using Katey.Localization;

namespace Katey.App.Text;

/// <summary>
/// The game's own names in the language in force: materials, blueprints, experimental effects, module types,
/// unlocks and modules from the catalog tables, the body descriptors the journal writes, and the lifetime
/// figures the game reports. Every lookup falls back to the data's English when the language has no entry,
/// so a gap in a translation shows the game's word rather than a key.
/// </summary>
/// <remarks>The table is <c>CatalogNames.resx</c>, whose neutral file is empty by design: English is the data's. <see cref="CatalogNameKeys"/> lists what a translation must hold.</remarks>
public static partial class Names
{
    private static readonly char[] AtmosphereSeparator = [' '];

    /// <summary>Gets the localizer over the names table.</summary>
    public static Localizer Localizer { get; } = new(new ResourceTextSource(new ResourceManager("Katey.App.Text.CatalogNames", typeof(Names).Assembly)));

    /// <summary>A material's name.</summary>
    /// <param name="material">The material.</param>
    /// <returns>The name in the language in force, or the table's.</returns>
    public static string Material(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        return Or(CatalogNameKeys.Material(material.Symbol), material.Name);
    }

    /// <summary>A blueprint's name.</summary>
    /// <param name="blueprint">The blueprint.</param>
    /// <returns>The name in the language in force, or the table's.</returns>
    public static string Blueprint(Blueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        return Or(CatalogNameKeys.Blueprint(blueprint.Key), blueprint.Name);
    }

    /// <summary>An experimental effect's name.</summary>
    /// <param name="experimental">The effect.</param>
    /// <returns>The name in the language in force, or the table's.</returns>
    public static string Experimental(Experimental experimental)
    {
        ArgumentNullException.ThrowIfNull(experimental);
        return Or(CatalogNameKeys.Experimental(experimental.Key), experimental.Name);
    }

    /// <summary>A module type's name.</summary>
    /// <param name="type">The module type.</param>
    /// <returns>The name in the language in force, or the table's.</returns>
    public static string ModuleType(ModuleType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Or(CatalogNameKeys.ModuleType(type.Code), type.Name);
    }

    /// <summary>A technology broker unlock's name.</summary>
    /// <param name="unlock">The unlock.</param>
    /// <returns>The name in the language in force, or the table's.</returns>
    public static string Unlock(Unlock unlock)
    {
        ArgumentNullException.ThrowIfNull(unlock);
        return Or(CatalogNameKeys.Unlock(unlock.Key), unlock.Name);
    }

    /// <summary>
    /// A module's name. In a language with a name for the module's type, the name is composed from that, the
    /// module's class and rating, and its mount; a qualified variant puts its translated qualifier words with the
    /// type's name through the language's template, and a variant that goes by a name of its own keeps that
    /// name. Otherwise it is the module catalog's own.
    /// </summary>
    /// <param name="modules">The module catalog.</param>
    /// <param name="catalog">The engineering catalog, for the module type.</param>
    /// <param name="symbol">The item symbol, any letter case.</param>
    /// <returns>Never empty for a non-empty symbol.</returns>
    public static string Module(ModuleCatalog modules, Catalog catalog, string symbol)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(symbol);
        if (ModuleSymbolNames.ArmourKind(symbol) is { } armour)
        {
            return Or(CatalogNameKeys.Armour(armour), modules.NameOf(symbol));
        }

        var info = modules.Find(symbol);
        if (info is null || !catalog.ModuleTypes.TryGetValue(info.Group, out var type) || !Localizer.TryGet(CatalogNameKeys.ModuleType(type.Code), out var typeName))
        {
            return modules.NameOf(symbol);
        }

        var head = typeName;
        if (ModuleSymbolNames.IsNamedVariant(symbol))
        {
            head = ModuleSymbolNames.BareName(info.Name);
        }
        else if (ModuleSymbolNames.Qualifiers(symbol) is { Count: > 0 } words && Localizer.TryGet(CatalogNameKeys.ModuleQualifiedTemplate, out var qualified))
        {
            var qualifier = string.Join(' ', words.Select(word => Or(CatalogNameKeys.ModuleQualifier(word), word)));
            head = Template.Fill(qualified, (name, _) => name switch
            {
                "qualifier" => qualifier,
                "type" => typeName,
                _ => null,
            });
        }

        var size = info.Class is { } cls && info.Rating is { } rating ? string.Create(CultureInfo.CurrentCulture, $"{cls}{rating}") : "";
        var mount = info.Mount is { Length: > 0 } kind ? Or(CatalogNameKeys.Mount(kind), kind) : "";
        return (size.Length > 0, mount.Length > 0) switch
        {
            (true, true) => Strings.Format(Keys.Module_WithSizeAndMount, ("type", head), ("size", size), ("mount", mount)),
            (true, false) => Strings.Format(Keys.Module_WithSize, ("type", head), ("size", size)),
            (false, true) => Strings.Format(Keys.Module_WithMount, ("type", head), ("mount", mount)),
            (false, false) => head,
        };
    }

    /// <summary>The name of a material family, which the traders group by.</summary>
    /// <param name="line">The family as the catalog spells it, such as <c>Wake Scans</c>.</param>
    /// <returns>The family's name in the language in force, or the catalog's spelling when the table lacks it.</returns>
    public static string Family(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return Or(CatalogNameKeys.Family(line), line);
    }

    /// <summary>The name of a surface signal type.</summary>
    /// <param name="type">The game's symbol, such as <c>$SAA_SignalType_Human;</c>.</param>
    /// <param name="label">The game client's words for it, shown when the table has no entry for the symbol.</param>
    /// <returns>The name in the language in force, or <paramref name="label"/>.</returns>
    public static string Signal(string type, string label)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(label);
        return Or(CatalogNameKeys.Signal(type), label);
    }

    /// <summary>A planet class as the journal spells it, in words.</summary>
    /// <param name="planetClass">The class, such as <c>High metal content body</c>, or <see langword="null"/>.</param>
    /// <returns>The name in the language in force, or the journal's; empty for <see langword="null"/>.</returns>
    public static string PlanetClass(string? planetClass)
        => planetClass is null ? "" : Or(CatalogNameKeys.PlanetClass(planetClass), planetClass);

    /// <summary>
    /// An atmosphere as the journal spells it, in words: <c>hot thick carbon dioxide rich atmosphere</c> is read
    /// as its heat and density words and its gas, and written back through the language's template.
    /// </summary>
    /// <param name="atmosphere">The journal's text, or empty.</param>
    /// <returns>The text in the language in force; the journal's when any part has no entry.</returns>
    public static string Atmosphere(string atmosphere)
    {
        ArgumentNullException.ThrowIfNull(atmosphere);
        if (atmosphere.Length == 0 || !Localizer.TryGet(CatalogNameKeys.AtmosphereTemplate, out var template))
        {
            return atmosphere;
        }

        var words = atmosphere.Split(AtmosphereSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (words.Count < 2 || !string.Equals(words[^1], "atmosphere", StringComparison.OrdinalIgnoreCase))
        {
            return atmosphere;
        }

        words.RemoveAt(words.Count - 1);
        var modifiers = new List<string>();
        while (words.Count > 1 && CatalogNameKeys.AtmosphereModifiers.Contains(words[0], StringComparer.OrdinalIgnoreCase))
        {
            if (!Localizer.TryGet(CatalogNameKeys.AtmosphereModifier(words[0]), out var modifier))
            {
                return atmosphere;
            }

            modifiers.Add(modifier);
            words.RemoveAt(0);
        }

        return !Localizer.TryGet(CatalogNameKeys.AtmosphereGas(string.Join(' ', words)), out var gas)
            ? atmosphere
            : Template.Fill(template, (name, _) => name switch
        {
            "modifiers" => string.Concat(modifiers.Select(modifier => modifier + " ")),
            "gas" => gas,
            _ => null,
        });
    }

    /// <summary>
    /// Volcanism as the journal spells it, in words: <c>minor water magma volcanism</c> is read as its magnitude
    /// and its kind, and written back through the language's template.
    /// </summary>
    /// <param name="volcanism">The journal's text, or empty.</param>
    /// <returns>The text in the language in force; the journal's when any part has no entry.</returns>
    public static string Volcanism(string volcanism)
    {
        ArgumentNullException.ThrowIfNull(volcanism);
        if (volcanism.Length == 0 || !Localizer.TryGet(CatalogNameKeys.VolcanismTemplate, out var template))
        {
            return volcanism;
        }

        var words = volcanism.Split(AtmosphereSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (words.Count < 2 || !string.Equals(words[^1], "volcanism", StringComparison.OrdinalIgnoreCase))
        {
            return volcanism;
        }

        words.RemoveAt(words.Count - 1);
        var magnitude = "";
        if (CatalogNameKeys.VolcanismMagnitudes.Contains(words[0], StringComparer.OrdinalIgnoreCase))
        {
            if (!Localizer.TryGet(CatalogNameKeys.VolcanismMagnitude(words[0]), out var word))
            {
                return volcanism;
            }

            magnitude = word + " ";
            words.RemoveAt(0);
        }

        return !Localizer.TryGet(CatalogNameKeys.VolcanismKind(string.Join(' ', words)), out var kind)
            ? volcanism
            : Template.Fill(template, (name, _) => name switch
        {
            "magnitude" => magnitude,
            "kind" => kind,
            _ => null,
        });
    }

    /// <summary>
    /// A hull's name: the names table's in the language in force, else the journal's own localised name when
    /// it offers one, else <see cref="ShipNames.Display"/> of the symbol.
    /// </summary>
    /// <param name="symbol">The hull symbol, any letter case, such as <c>anacondamkii</c>.</param>
    /// <param name="journalName">The journal's localised name, or <see langword="null"/> or the symbol itself when it offers none.</param>
    /// <returns>Never empty for a non-empty symbol.</returns>
    public static string Hull(string symbol, string? journalName)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return Or(CatalogNameKeys.Hull(symbol), ShipNames.Prefer(symbol, journalName));
    }

    /// <summary>
    /// A lifetime figure's label. A passenger figure the game counts by type, class and outcome, such as
    /// <c>Passengers_Missions_Refugee_bulk_delivered</c>, is composed through the language's template when it
    /// names every part.
    /// </summary>
    /// <param name="key">The journal key, such as <c>Bounty_Hunting_Profit</c>.</param>
    /// <param name="fallback">The label when the language has none, read off the key.</param>
    /// <returns>The label.</returns>
    public static string Figure(string key, string fallback)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (PassengerFigurePattern().Match(key) is { Success: true } match
            && Localizer.TryGet(CatalogNameKeys.PassengerFigureTemplate, out var template)
            && Localizer.TryGet(CatalogNameKeys.PassengerType(match.Groups[1].Value), out var type)
            && Localizer.TryGet(CatalogNameKeys.PassengerClass(match.Groups[2].Value.ToLowerInvariant()), out var passengerClass)
            && Localizer.TryGet(CatalogNameKeys.PassengerOutcome(match.Groups[3].Value.ToLowerInvariant()), out var outcome))
        {
            return Template.Fill(template, (name, _) => name switch
            {
                "type" => type,
                "class" => passengerClass,
                "outcome" => outcome,
                _ => null,
            });
        }

        return Or(CatalogNameKeys.Figure(key), fallback);
    }

    /// <summary>The shape of a passenger figure's key: the type, then the class, then the outcome.</summary>
    [GeneratedRegex("^Passengers_Missions_([A-Za-z]+)_(bulk|vip)_(delivered|ejected)$", RegexOptions.CultureInvariant)]
    private static partial Regex PassengerFigurePattern();

    /// <summary>A lifetime section's label.</summary>
    /// <param name="section">The journal section, such as <c>Bank_Account</c>.</param>
    /// <param name="fallback">The label when the language has none, read off the name.</param>
    /// <returns>The label.</returns>
    public static string FigureSection(string section, string fallback)
    {
        ArgumentNullException.ThrowIfNull(section);
        return Or(CatalogNameKeys.FigureSection(section), fallback);
    }

    private static string Or(string key, string fallback) => Localizer.TryGet(key, out var text) ? text : fallback;
}
