using System.Collections.Frozen;
using System.Text.Json;

namespace Katey.Core.Fleet;

/// <summary>One purchasable module.</summary>
/// <param name="Symbol">Item symbol, lower case, such as <c>int_hyperdrive_size5_class5</c>.</param>
/// <param name="Name">Display name with size and rating, such as "Frame Shift Drive 5A".</param>
/// <param name="Group">Module type code, as the engineering tables use.</param>
/// <param name="Class">Size 0–8, or <see langword="null"/> for armour.</param>
/// <param name="Rating">Rating letter, or <see langword="null"/>.</param>
/// <param name="Mount">Fixed, Gimballed or Turreted for weapons, else <see langword="null"/>.</param>
public sealed record ModuleInfo(string Symbol, string Name, string Group, int? Class, string? Rating, string? Mount)
{
    /// <summary>
    /// Gets whether this row stands for an item the table could not identify rather than a module the game
    /// sells. The table marks such rows with <c>_missing_</c> in the symbol.
    /// </summary>
    public bool IsPlaceholder => Symbol.Contains("_missing_", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Every module the game sells, by item symbol, for naming fits and owned modules.</summary>
/// <remarks>Lookups are case-insensitive. Loaded once from the embedded <c>modules.json</c>.</remarks>
public sealed class ModuleCatalog
{
    private readonly FrozenDictionary<string, ModuleInfo> _modules;
    private readonly FrozenSet<string> _lexicon;

    private ModuleCatalog(FrozenDictionary<string, ModuleInfo> modules)
    {
        _modules = modules;
        _lexicon = modules.Values
            .SelectMany(module => ModuleSymbolNames.WordsOf(module.Name))
            .Concat(ModuleSymbolNames.ExtraWords)
            .ToFrozenSet(StringComparer.Ordinal);
    }

    /// <summary>Gets how many modules the catalog knows.</summary>
    public int Count => _modules.Count;

    /// <summary>Loads the table embedded in this assembly.</summary>
    /// <returns>The catalog.</returns>
    /// <exception cref="CatalogException">The embedded table is missing or malformed. Never expected from a shipped build.</exception>
    public static ModuleCatalog LoadEmbedded()
    {
        using var stream = typeof(ModuleCatalog).Assembly.GetManifestResourceStream("Katey.Core.Data.modules.json")
            ?? throw new CatalogException("modules.json: embedded resource missing");
        using var document = JsonDocument.Parse(stream);
        var modules = new Dictionary<string, ModuleInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var value = property.Value;
            var name = value.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
            var group = value.TryGetProperty("group", out var g) && g.ValueKind == JsonValueKind.String ? g.GetString() : null;
            if (name is null || group is null)
            {
                throw new CatalogException($"modules.json \"{property.Name}\": needs name and group");
            }

            modules[property.Name] = new ModuleInfo(
                property.Name,
                name,
                group,
                value.TryGetProperty("class", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : null,
                value.TryGetProperty("rating", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString() : null,
                value.TryGetProperty("mount", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null);
        }

        return new ModuleCatalog(modules.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Whether an item symbol names a cosmetic or other non-module item the game lists among a ship's
    /// modules: paint jobs, decals, nameplates, bobbleheads, ship kit parts, engine and weapon colours, voice
    /// packs, string lights, and the cockpit entry. A ship kit part carries the hull name first, as in
    /// <c>anacondamkii_shipkita_bumper2</c>, so it is matched anywhere in the symbol.
    /// </summary>
    /// <param name="symbol">Item symbol, any letter case.</param>
    /// <returns><see langword="true"/> for an item that is not a module.</returns>
    public static bool IsCosmetic(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        var lower = symbol.Trim().ToUpperInvariant().ToLowerInvariant();
        return lower.StartsWith("paintjob", StringComparison.Ordinal)
            || lower.StartsWith("decal", StringComparison.Ordinal)
            || lower.StartsWith("nameplate", StringComparison.Ordinal)
            || lower.StartsWith("bobble", StringComparison.Ordinal)
            || lower.Contains("shipkit", StringComparison.Ordinal)
            || lower.StartsWith("enginecustomisation", StringComparison.Ordinal)
            || lower.StartsWith("weaponcustomisation", StringComparison.Ordinal)
            || lower.StartsWith("voicepack", StringComparison.Ordinal)
            || lower.StartsWith("string_lights", StringComparison.Ordinal)
            || lower.EndsWith("_cockpit", StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether a symbol is a fixture every ship carries rather than a module the player chose: the cargo
    /// hatch and the planetary approach suite. A fixture is still compared when a fit names it, but it is
    /// never an owned module going spare.
    /// </summary>
    /// <param name="symbol">Item symbol, any letter case.</param>
    /// <returns><see langword="true"/> for a cargo hatch or an approach suite of any kind.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="symbol"/> is <see langword="null"/>.</exception>
    public static bool IsFixture(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        var lower = ModuleSymbolNames.Canonical(symbol);
        return lower == "modularcargobaydoor" || lower.StartsWith("int_planetapproachsuite", StringComparison.Ordinal);
    }

    /// <summary>Looks a module up.</summary>
    /// <param name="symbol">Item symbol, any letter case.</param>
    /// <returns>The module, or <see langword="null"/> when unknown.</returns>
    public ModuleInfo? Find(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return _modules.GetValueOrDefault(symbol);
    }

    /// <summary>The modules the game sells of one module type.</summary>
    /// <param name="group">Module type code, as the engineering tables use, any letter case.</param>
    /// <returns>
    /// Placeholder rows are left out. Ordered by class, then by display name. Empty for a code no module
    /// carries, which includes ship armour: the table does not list bulkheads.
    /// </returns>
    public IReadOnlyList<ModuleInfo> OfType(string group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return
        [
            .. _modules.Values
                .Where(module => !module.IsPlaceholder && string.Equals(module.Group, group, StringComparison.OrdinalIgnoreCase))
                .OrderBy(module => module.Class)
                .ThenBy(module => module.Name, StringComparer.Ordinal),
        ];
    }

    /// <summary>
    /// A display name for any symbol: the catalog's when known; the armour's name for hull armour; the
    /// catalog's again for a symbol that differs only by a <c>_free</c> or <c>_default</c> suffix; else the
    /// symbol read as words by <see cref="ModuleSymbolNames.Describe"/> over the catalog's own vocabulary.
    /// </summary>
    /// <param name="symbol">Item symbol, any letter case.</param>
    /// <returns>Never empty for a non-empty symbol.</returns>
    public string NameOf(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        if (_modules.TryGetValue(symbol, out var module))
        {
            return module.Name;
        }

        if (ModuleSymbolNames.ArmourName(symbol) is { } armour)
        {
            return armour;
        }

        var bare = ModuleSymbolNames.WithoutSuffix(symbol);
        if (!string.Equals(bare, symbol, StringComparison.Ordinal) && _modules.TryGetValue(bare, out var sold))
        {
            return sold.Name;
        }

        var described = ModuleSymbolNames.Describe(symbol, _lexicon);
        return described.Length == 0 ? symbol : described;
    }
}
