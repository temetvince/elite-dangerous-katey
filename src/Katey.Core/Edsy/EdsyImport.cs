using System.Text.Json;

namespace Katey.Core.Edsy;

/// <summary>One engineered module read from a Loadout.</summary>
/// <param name="Slot">Slot name such as <c>FrameShiftDrive</c>.</param>
/// <param name="Item">Item symbol such as <c>int_hyperdrive_overcharge_size5_class5</c>.</param>
/// <param name="ModuleType">Module type code, or <see langword="null"/> when the item matched no pattern.</param>
/// <param name="Blueprint">Blueprint key. Exists in the catalog.</param>
/// <param name="Level">The export's level, 1–5. The plan clamps it to a grade the blueprint has.</param>
/// <param name="Experimental">Experimental key, or <see langword="null"/> when the module had none or an unknown one.</param>
public sealed record ImportedModule(
    string Slot,
    string Item,
    string? ModuleType,
    string Blueprint,
    int Level,
    string? Experimental);

/// <summary>Any module in a Loadout, engineered or not.</summary>
/// <param name="Slot">Slot name.</param>
/// <param name="Item">Item symbol, lower case.</param>
/// <param name="Blueprint">Blueprint key, or <see langword="null"/> for a stock module or an unknown blueprint.</param>
/// <param name="Level">Grade 1–5, or 0 when stock.</param>
/// <param name="Experimental">Experimental key, or <see langword="null"/>.</param>
public sealed record LoadoutModule(string Slot, string Item, string? Blueprint, int Level, string? Experimental);

/// <summary>A successfully parsed export.</summary>
/// <param name="Ship">Ship type symbol such as <c>mandalay</c>, lower case.</param>
/// <param name="ShipName">Ship name, or the ship type when the export had none.</param>
/// <param name="Modules">Only the engineered modules, in export order.</param>
/// <param name="Warnings">Modules skipped or imported incompletely, one sentence each.</param>
/// <param name="AllModules">Every module with an item, stock ones included, in export order.</param>
/// <param name="Profile">The jump numbers, when the export carries <c>UnladenMass</c> and <c>FuelCapacity</c> and a module is a frame shift drive; otherwise <see langword="null"/>.</param>
public sealed record ParsedLoadout(
    string Ship,
    string ShipName,
    IReadOnlyList<ImportedModule> Modules,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<LoadoutModule> AllModules,
    Routing.JumpProfile? Profile = null);

/// <summary>Why an EDSY export could not be read.</summary>
public enum EdsyParseError
{
    /// <summary>Nothing was pasted.</summary>
    Empty,

    /// <summary>The text is not JSON.</summary>
    NotJson,

    /// <summary>The JSON holds no <c>Loadout</c> record.</summary>
    NoLoadout,

    /// <summary>The <c>Loadout</c> record has no <c>Modules</c> list.</summary>
    NoModules,
}

/// <summary>Outcome of <see cref="EdsyImport.Parse"/>: a loadout or why not.</summary>
/// <param name="Loadout">The loadout, or <see langword="null"/> on failure.</param>
/// <param name="Error">Why parsing failed, or <see langword="null"/> on success.</param>
public sealed record EdsyParseResult(ParsedLoadout? Loadout, EdsyParseError? Error)
{
    /// <summary>Gets whether a loadout was read.</summary>
    public bool Ok => Loadout is not null;
}

/// <summary>
/// Parses the text of an EDSY "Export to journal Loadout", or a journal <c>Loadout</c> event, into the
/// engineered modules it lists.
/// </summary>
/// <remarks>
/// Accepts an array of <c>{ header, data }</c> objects, one such object, or a bare Loadout. A module is
/// imported when it has an <c>Engineering</c> block whose <c>BlueprintName</c> is a known blueprint and
/// whose <c>Level</c> is 1–5. A module without <c>Engineering</c> is ignored silently. A module with an
/// unknown blueprint or level is skipped with a warning; an unknown experimental is dropped with a
/// warning while the module is still imported. The ship's <c>UnladenMass</c>, <c>FuelCapacity</c> and
/// each module's <c>Engineering.Modifiers</c>, which EDSY writes as the game does, give the jump profile
/// when they are present. Never throws for bad input.
/// </remarks>
/// <param name="catalog">The catalog to resolve names against.</param>
public sealed class EdsyImport(Catalog catalog)
{
    private readonly Catalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    private readonly ItemResolver _resolver = new(catalog);

    /// <summary>Parses export text.</summary>
    /// <param name="text">Raw export text.</param>
    /// <returns>The result.</returns>
    public EdsyParseResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            return new EdsyParseResult(null, EdsyParseError.Empty);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return new EdsyParseResult(null, EdsyParseError.NotJson);
        }

        using (document)
        {
            if (FindLoadout(document.RootElement) is not { } loadout)
            {
                return new EdsyParseResult(null, EdsyParseError.NoLoadout);
            }

            if (!loadout.TryGetProperty("Modules", out var modules) || modules.ValueKind != JsonValueKind.Array)
            {
                return new EdsyParseResult(null, EdsyParseError.NoModules);
            }

            var ship = (StringOrNull(loadout, "Ship") ?? "unknown ship").ToUpperInvariant().ToLowerInvariant();
            var shipName = StringOrNull(loadout, "ShipName") is { Length: > 0 } name && !string.IsNullOrWhiteSpace(name) ? name : ship;
            var imported = new List<ImportedModule>();
            var warnings = new List<string>();
            var all = new List<LoadoutModule>();
            var figures = new List<Routing.ProfileModule>();
            foreach (var module in modules.EnumerateArray())
            {
                ReadModule(module, imported, warnings, all, figures);
            }

            return new EdsyParseResult(new ParsedLoadout(ship, shipName, imported, warnings, all, ReadProfile(loadout, figures)), null);
        }
    }

    /// <summary>
    /// The jump profile an export text carries, without the catalog: its <c>UnladenMass</c> and
    /// <c>FuelCapacity</c> and the modules' items and modifiers, in any of the shapes <see cref="Parse"/>
    /// accepts. Never throws for bad input.
    /// </summary>
    /// <param name="text">Raw export text.</param>
    /// <returns>The profile, or <see langword="null"/> when the text is not a Loadout or lacks the masses or a frame shift drive.</returns>
    public static Routing.JumpProfile? ProfileOf(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            if (FindLoadout(document.RootElement) is not { } loadout
                || !loadout.TryGetProperty("Modules", out var modules) || modules.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var figures = new List<Routing.ProfileModule>();
            foreach (var module in modules.EnumerateArray())
            {
                if (module.ValueKind == JsonValueKind.Object && StringOrNull(module, "Item") is { Length: > 0 } item)
                {
                    var hasEngineering = module.TryGetProperty("Engineering", out var engineering) && engineering.ValueKind == JsonValueKind.Object;
                    figures.Add(new Routing.ProfileModule(item, hasEngineering ? ReadModifiers(engineering) : Routing.ProfileModule.None));
                }
            }

            return ReadProfile(loadout, figures);
        }
    }

    private static JsonElement? FindLoadout(JsonElement root)
    {
        var candidate = root.ValueKind == JsonValueKind.Array
            ? root.EnumerateArray().FirstOrDefault()
            : root;
        return candidate.ValueKind != JsonValueKind.Object
            ? null
            : candidate.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object ? data : candidate;
    }

    private static string? StringOrNull(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static double? NumberOrNull(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) ? number : null;

    /// <summary>The jump profile from the loadout's masses and tanks, or <see langword="null"/> when the export lacks them.</summary>
    private static Routing.JumpProfile? ReadProfile(JsonElement loadout, List<Routing.ProfileModule> figures)
    {
        return NumberOrNull(loadout, "UnladenMass") is not { } unladen || unladen <= 0
            || !loadout.TryGetProperty("FuelCapacity", out var tanks) || tanks.ValueKind != JsonValueKind.Object
            || NumberOrNull(tanks, "Main") is not { } main || main <= 0
            ? null
            : Routing.JumpProfile.Build(figures, unladen, main, NumberOrNull(tanks, "Reserve") ?? 0, NumberOrNull(loadout, "MaxJumpRange"));
    }

    private static Dictionary<string, double> ReadModifiers(JsonElement engineering)
    {
        var modifiers = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (engineering.TryGetProperty("Modifiers", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var modifier in list.EnumerateArray())
            {
                if (modifier.ValueKind == JsonValueKind.Object && StringOrNull(modifier, "Label") is { Length: > 0 } label && NumberOrNull(modifier, "Value") is { } value)
                {
                    modifiers[label] = value;
                }
            }
        }

        return modifiers;
    }

    private void ReadModule(JsonElement module, List<ImportedModule> imported, List<string> warnings, List<LoadoutModule> all, List<Routing.ProfileModule> figures)
    {
        if (module.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var slot = StringOrNull(module, "Slot") ?? "unknown slot";
        var item = (StringOrNull(module, "Item") ?? "").ToUpperInvariant().ToLowerInvariant();
        var hasEngineering = module.TryGetProperty("Engineering", out var engineering) && engineering.ValueKind == JsonValueKind.Object;
        if (item.Length > 0)
        {
            figures.Add(new Routing.ProfileModule(item, hasEngineering ? ReadModifiers(engineering) : Routing.ProfileModule.None));
            var blueprintKey = hasEngineering ? StringOrNull(engineering, "BlueprintName") : null;
            var effectKey = hasEngineering ? StringOrNull(engineering, "ExperimentalEffect") : null;
            all.Add(new LoadoutModule(
                slot,
                item,
                blueprintKey is not null && _catalog.Blueprints.ContainsKey(blueprintKey) ? blueprintKey : null,
                hasEngineering && engineering.TryGetProperty("Level", out var levelValue) && levelValue.TryGetInt32(out var parsedLevel) ? parsedLevel : 0,
                effectKey is not null && _catalog.Experimentals.ContainsKey(effectKey) ? effectKey : null));
        }

        if (!hasEngineering)
        {
            return;
        }

        var blueprintName = StringOrNull(engineering, "BlueprintName");
        if (blueprintName is null || !_catalog.Blueprints.ContainsKey(blueprintName))
        {
            warnings.Add($"{slot}: blueprint \"{blueprintName ?? "(none)"}\" is not in the data, so the module was skipped.");
            return;
        }

        if (!engineering.TryGetProperty("Level", out var levelElement)
            || levelElement.ValueKind != JsonValueKind.Number
            || !levelElement.TryGetInt32(out var level)
            || level is < 1 or > 5)
        {
            warnings.Add($"{slot}: level {levelElement} is not 1–5, so the module was skipped.");
            return;
        }

        string? experimental = null;
        if (StringOrNull(engineering, "ExperimentalEffect") is { Length: > 0 } effect)
        {
            if (_catalog.Experimentals.ContainsKey(effect))
            {
                experimental = effect;
            }
            else
            {
                warnings.Add($"{slot}: experimental \"{effect}\" is not in the data, so it was left off.");
            }
        }

        var moduleType = _resolver.Resolve(item);
        if (moduleType is null)
        {
            warnings.Add($"{slot}: item \"{item}\" matched no module type, so engineers cannot be listed for it.");
        }

        imported.Add(new ImportedModule(slot, item, moduleType, blueprintName, level, experimental));
    }
}
