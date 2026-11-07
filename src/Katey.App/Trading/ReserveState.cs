using Katey.Core.Trading;

namespace Katey.App.Trading;

/// <summary>The material reserves the player set: how many of each to keep, and which never to trade.</summary>
/// <remarks>Reserves are keyed by journal symbol, case-insensitively. An empty reserve is not kept.</remarks>
public sealed class ReserveState
{
    private readonly Dictionary<string, MaterialReserve> _reserves = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates the state from what was saved.</summary>
    /// <param name="saved">The saved reserves, or <see langword="null"/> for none. Empty ones are dropped.</param>
    public ReserveState(IEnumerable<MaterialReserve>? saved) => Replace(saved);

    /// <summary>Raised after a reserve changes, on the caller's thread.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets every reserve that says something, in no particular order.</summary>
    public IReadOnlyList<MaterialReserve> All => [.. _reserves.Values];

    /// <summary>The reserve on a material.</summary>
    /// <param name="symbol">The journal symbol.</param>
    /// <returns>The reserve, or one that says nothing.</returns>
    public MaterialReserve For(string symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return _reserves.GetValueOrDefault(symbol) ?? new MaterialReserve(symbol, 0, false);
    }

    /// <summary>Sets a material's reserve, dropping it when it says nothing.</summary>
    /// <param name="symbol">The journal symbol.</param>
    /// <param name="keep">How many to keep at least. Negative reads as zero.</param>
    /// <param name="neverTrade">Whether never to trade it away.</param>
    public void Set(string symbol, int keep, bool neverTrade)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var next = new MaterialReserve(symbol, Math.Max(0, keep), neverTrade);
        var current = For(symbol);
        if (current.Keep == next.Keep && current.NeverTrade == next.NeverTrade)
        {
            return;
        }

        if (next.IsEmpty)
        {
            _reserves.Remove(symbol);
        }
        else
        {
            _reserves[symbol] = next;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Replaces every reserve, as when another saved state is loaded.</summary>
    /// <param name="saved">The reserves, or <see langword="null"/> for none.</param>
    public void Replace(IEnumerable<MaterialReserve>? saved)
    {
        _reserves.Clear();
        foreach (var reserve in saved ?? [])
        {
            if (!reserve.IsEmpty && !string.IsNullOrWhiteSpace(reserve.Symbol))
            {
                _reserves[reserve.Symbol] = reserve;
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
