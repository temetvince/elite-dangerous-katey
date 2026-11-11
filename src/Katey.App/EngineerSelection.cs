using Katey.Core;

namespace Katey.App;

/// <summary>The engineers the commander is willing to visit.</summary>
/// <remarks>
/// In memory the selection is a set of names, compared ordinally, exactly as the module tables spell them,
/// since that is how the tables name who offers a grade. It is saved and restored as the game's engineer
/// ids, <see cref="SelectedIds"/>, which do not depend on a spelling. Not thread-safe.
/// </remarks>
public sealed class EngineerSelection
{
    private readonly Catalog _catalog;
    private readonly HashSet<string> _selected = new(StringComparer.Ordinal);

    /// <summary>Creates the selection.</summary>
    /// <param name="catalog">The catalog; ids it does not know are ignored.</param>
    /// <param name="selected">Engineer ids to start with.</param>
    public EngineerSelection(Catalog catalog, IEnumerable<long> selected)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(selected);

        _catalog = catalog;
        _selected.UnionWith(Names(selected));
    }

    /// <summary>Raised after the selection changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the selected names. The set is live; copy it to keep it.</summary>
    public IReadOnlySet<string> Selected => _selected;

    /// <summary>Gets the selected engineers' ids, ascending: the form the selection is saved in.</summary>
    public IReadOnlyList<long> SelectedIds => [.. _selected.Select(name => _catalog.Engineers[name].Id).Order()];

    /// <summary>Gets how many are selected.</summary>
    public int Count => _selected.Count;

    /// <summary>Whether an engineer is selected.</summary>
    /// <param name="name">The engineer's name.</param>
    /// <returns><see langword="true"/> when selected.</returns>
    public bool Contains(string name) => _selected.Contains(name);

    /// <summary>Selects or deselects one engineer.</summary>
    /// <param name="name">An engineer the catalog knows. Unknown names are ignored.</param>
    /// <param name="selected">The new state.</param>
    public void Set(string name, bool selected)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!_catalog.Engineers.ContainsKey(name))
        {
            return;
        }

        var changed = selected ? _selected.Add(name) : _selected.Remove(name);
        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Replaces the whole selection.</summary>
    /// <param name="ids">The engineers to select, by id. Ids the catalog does not know are ignored.</param>
    public void Replace(IEnumerable<long> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        _selected.Clear();
        _selected.UnionWith(Names(ids));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Selects every engineer, or none.</summary>
    /// <param name="selected">The new state for everyone.</param>
    public void SetAll(bool selected)
    {
        _selected.Clear();
        if (selected)
        {
            _selected.UnionWith(_catalog.Engineers.Keys);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private IEnumerable<string> Names(IEnumerable<long> ids)
        => ids.Select(id => _catalog.EngineersById.GetValueOrDefault(id)?.Name).OfType<string>();
}
