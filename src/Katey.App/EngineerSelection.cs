using Katey.Core;

namespace Katey.App;

/// <summary>The engineers the commander is willing to visit.</summary>
/// <remarks>Names are compared ordinally, exactly as the tables spell them. Not thread-safe.</remarks>
public sealed class EngineerSelection
{
    private readonly Catalog _catalog;
    private readonly HashSet<string> _selected = new(StringComparer.Ordinal);

    /// <summary>Creates the selection.</summary>
    /// <param name="catalog">The catalog; names it does not know are ignored.</param>
    /// <param name="selected">Names to start with.</param>
    public EngineerSelection(Catalog catalog, IEnumerable<string> selected)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(selected);

        _catalog = catalog;
        foreach (var name in selected.Where(catalog.Engineers.ContainsKey))
        {
            _selected.Add(name);
        }
    }

    /// <summary>Raised after the selection changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the selected names. The set is live; copy it to keep it.</summary>
    public IReadOnlySet<string> Selected => _selected;

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
    /// <param name="names">The engineers to select. Names the catalog does not know are ignored.</param>
    public void Replace(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        _selected.Clear();
        _selected.UnionWith(names.Where(_catalog.Engineers.ContainsKey));
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
}
