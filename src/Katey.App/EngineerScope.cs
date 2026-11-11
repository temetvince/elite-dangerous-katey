using Katey.App.Text;
using Katey.Core;
using Katey.Core.Planning;

namespace Katey.App;

/// <summary>
/// What the indexes and pickers may offer: everything the catalog has, or only what the selected engineers
/// can roll while the remembered switch is on and someone is selected.
/// </summary>
/// <remarks>The switch is a preference, so it is saved with the state and shared by every page. Not thread-safe.</remarks>
public sealed class EngineerScope
{
    private const string Key = "engineers.onlyMine";

    private readonly Catalog _catalog;
    private readonly EngineerSelection _engineers;
    private readonly UiPreferences _preferences;

    /// <summary>Creates the scope.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="engineers">The engineers the commander will visit.</param>
    /// <param name="preferences">Where the switch is remembered.</param>
    public EngineerScope(Catalog catalog, EngineerSelection engineers, UiPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(engineers);
        ArgumentNullException.ThrowIfNull(preferences);
        _catalog = catalog;
        _engineers = engineers;
        _preferences = preferences;
    }

    /// <summary>Gets or sets whether every index and picker shows only what the selected engineers can roll. Remembered. Has no effect while nobody is selected.</summary>
    public bool OnlyMyEngineers
    {
        get => _preferences.GetBool(Key, false);
        set => _preferences.SetBool(Key, value);
    }

    /// <summary>Gets whether the narrowing is in force: <see cref="OnlyMyEngineers"/> is on and an engineer is selected.</summary>
    public bool IsNarrowed => OnlyMyEngineers && _engineers.Count > 0;

    /// <summary>Gets one line telling a page's reader that it is narrowed and where to change that, or empty when it is not.</summary>
    public string Note => IsNarrowed ? Strings.Get(Keys.Scope_Narrowed) : "";

    /// <summary>The blueprints a picker offers for a module type.</summary>
    /// <param name="type">The module type.</param>
    /// <returns>Every blueprint the type takes, or only those a selected engineer offers on it when narrowed, in catalog order.</returns>
    public IReadOnlyList<Blueprint> Blueprints(ModuleType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return IsNarrowed ? EngineerCoverage.Blueprints(_catalog, type, _engineers.Selected) : _catalog.BlueprintsFor(type.Code);
    }

    /// <summary>The grades a picker offers for a blueprint on a module type, without the experimental-only 0.</summary>
    /// <param name="type">The module type.</param>
    /// <param name="blueprint">Blueprint key. Must exist in the catalog.</param>
    /// <returns>Every grade, or only up to the one the selected engineers reach when narrowed, lowest first. Empty when narrowed and nobody selected offers it.</returns>
    public IReadOnlyList<int> Grades(ModuleType type, string blueprint)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(blueprint);
        return IsNarrowed ? EngineerCoverage.Grades(_catalog, type, blueprint, _engineers.Selected) : _catalog.Blueprints[blueprint].GradeNumbers;
    }

    /// <summary>The experimental effects a picker offers for a module type.</summary>
    /// <param name="type">The module type.</param>
    /// <returns>Every effect the type takes, or none when narrowed and no selected engineer can apply them.</returns>
    public IReadOnlyList<Experimental> Experimentals(ModuleType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return IsNarrowed && !EngineerCoverage.OffersAny(type, _engineers.Selected) ? [] : _catalog.ExperimentalsFor(type.Code);
    }

    /// <summary>The module types a picker offers for a blueprint.</summary>
    /// <param name="blueprint">Blueprint key. Must exist in the catalog.</param>
    /// <returns>Every type that takes it, or only those a selected engineer offers it on when narrowed.</returns>
    public IReadOnlyList<ModuleType> ModuleTypes(string blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        return IsNarrowed ? EngineerCoverage.ModuleTypes(_catalog, blueprint, _engineers.Selected) : _catalog.ModuleTypesWithBlueprint(blueprint);
    }

    /// <summary>The grade a picker starts at for a blueprint on a module type: the blueprint's top grade, or when narrowed the highest the selected engineers reach.</summary>
    /// <param name="type">The module type.</param>
    /// <param name="blueprint">Blueprint key. Must exist in the catalog.</param>
    /// <returns>A grade the blueprint has.</returns>
    public int StartingGrade(ModuleType type, string blueprint)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(blueprint);
        return IsNarrowed ? EngineerCoverage.StartingGrade(_catalog, type, blueprint, _engineers.Selected) : _catalog.Blueprints[blueprint].MaxGrade;
    }
}
