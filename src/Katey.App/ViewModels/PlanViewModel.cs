using System.Collections.ObjectModel;
using Katey.App.Mvvm;
using Katey.Core;
using Katey.Core.Planning;

namespace Katey.App.ViewModels;

/// <summary>
/// The list of modules to engineer. Clearing takes two clicks: the first arms a confirmation, the second
/// clears.
/// </summary>
public sealed class PlanViewModel : ObservableObject
{
    private readonly Catalog _catalog;
    private readonly Func<IReadOnlySet<string>> _selectedEngineers;
    private readonly Func<string, EngineerLink> _engineerLink;

    /// <summary>Creates an empty plan.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="selectedEngineers">Reads the current engineer selection.</param>
    /// <param name="engineerLink">Makes an engineer's name as Katey shows it everywhere; see <see cref="Workspace.EngineerLinkFor"/>.</param>
    public PlanViewModel(Catalog catalog, Func<IReadOnlySet<string>> selectedEngineers, Func<string, EngineerLink> engineerLink)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(selectedEngineers);
        ArgumentNullException.ThrowIfNull(engineerLink);

        _catalog = catalog;
        _selectedEngineers = selectedEngineers;
        _engineerLink = engineerLink;
        ArmClearCommand = new RelayCommand(() => IsConfirmingClear = true, () => Entries.Count > 0);
        CancelClearCommand = new RelayCommand(() => IsConfirmingClear = false);
        ConfirmClearCommand = new RelayCommand(Clear);
    }

    /// <summary>Raised after any entry is added, removed or edited.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the entries in plan order.</summary>
    public ObservableCollection<PlanEntryViewModel> Entries { get; } = [];

    /// <summary>Gets whether there are no entries.</summary>
    public bool IsEmpty => Entries.Count == 0;

    /// <summary>Gets how many entries count toward the totals.</summary>
    public int EnabledCount => Entries.Count(entry => entry.Enabled);

    /// <summary>Gets whether the clear button has been pressed once and awaits confirmation.</summary>
    public bool IsConfirmingClear
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that asks for confirmation before clearing.</summary>
    public RelayCommand ArmClearCommand { get; }

    /// <summary>Gets the command that clears every entry.</summary>
    public RelayCommand ConfirmClearCommand { get; }

    /// <summary>Gets the command that keeps the plan.</summary>
    public RelayCommand CancelClearCommand { get; }

    /// <summary>The entries as plain records.</summary>
    /// <returns>A fresh list in plan order.</returns>
    public IReadOnlyList<PlanEntry> Snapshot() => [.. Entries.Select(entry => entry.Entry)];

    /// <summary>Words every row again in the language in force. Called after a language switch; the plan itself is unchanged, so <see cref="Changed"/> is not raised.</summary>
    public void Reword()
    {
        foreach (var row in Entries)
        {
            row.Reword();
        }
    }

    /// <summary>Whether the plan already holds an entry for the same work, by <see cref="PlanEntry.IsSameWorkAs"/>.</summary>
    /// <param name="candidate">The entry a page would add.</param>
    /// <returns><see langword="true"/> when adding it would repeat a job already planned.</returns>
    public bool Contains(PlanEntry candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return Entries.Any(row => row.Entry.IsSameWorkAs(candidate));
    }

    /// <summary>Appends entries.</summary>
    /// <param name="entries">Entries whose blueprints, or unlocks, exist in the catalog.</param>
    public void Add(IEnumerable<PlanEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var entry in entries)
        {
            Entries.Add(new PlanEntryViewModel(_catalog, entry, _selectedEngineers(), _engineerLink, _ => RaiseChanged(), Remove));
        }

        RaiseChanged();
    }

    /// <summary>
    /// Replaces every entry, dropping any whose blueprint, experimental or unlock the catalog lacks. A
    /// module entry saved without a <see cref="PlanEntry.CraftsAfter"/> is anchored now, so the engineering
    /// in the history does not count toward it.
    /// </summary>
    /// <param name="entries">The new plan.</param>
    public void Load(IEnumerable<PlanEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var now = DateTimeOffset.UtcNow;
        Entries.Clear();
        Add(entries
            .Where(entry => entry.Unlock is { } unlock
                ? _catalog.Unlocks.ContainsKey(unlock)
                : _catalog.Blueprints.ContainsKey(entry.Blueprint)
                    && (entry.Experimental is null || _catalog.Experimentals.ContainsKey(entry.Experimental)))
            .Select(entry => entry.IsUnlock || entry.CraftsAfter is not null ? entry : entry with { CraftsAfter = now }));
    }

    /// <summary>
    /// Brings the entries in line with a plan the progress rule produced: an entry missing from it is
    /// removed, one that differs is replaced in place, and the order of the rest is kept. Raises
    /// <see cref="Changed"/> once when anything differed.
    /// </summary>
    /// <param name="entries">The plan afterwards, by entry id. Ids not already in the plan are ignored.</param>
    public void Update(IReadOnlyList<PlanEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var byId = entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
        var changed = false;
        foreach (var row in Entries.ToList())
        {
            if (!byId.TryGetValue(row.Entry.Id, out var entry))
            {
                Entries.Remove(row);
                changed = true;
            }
            else if (!ReferenceEquals(entry, row.Entry))
            {
                row.Replace(entry);
                changed = true;
            }
        }

        if (changed)
        {
            RaiseChanged();
        }
    }

    /// <summary>Recomputes every entry's engineers and warnings, for when the selection changes.</summary>
    public void RefreshEngineers()
    {
        var selected = _selectedEngineers();
        foreach (var entry in Entries)
        {
            entry.Refresh(selected);
        }
    }

    private void Remove(PlanEntryViewModel entry)
    {
        Entries.Remove(entry);
        RaiseChanged();
    }

    private void Clear()
    {
        Entries.Clear();
        IsConfirmingClear = false;
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EnabledCount));
        ArmClearCommand.RaiseCanExecuteChanged();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
