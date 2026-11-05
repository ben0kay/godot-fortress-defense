// One level-owned wallet; synchronous purchases publish changes only after a complete commit.
using Godot;
using System;
using System.Collections.Generic;

public partial class ResourceWallet : Node
{
    public event Action BalanceChanged;
    private readonly Dictionary<ResourceCost.ResourceId, int> _balances = new();
    private bool _purchasing;

    // Fresh session balances never mutate authored level resources.
    public void Initialize(int startingCredits)
    {
        if (_purchasing || startingCredits < 0) throw new InvalidOperationException("Invalid wallet initialization.");
        _balances.Clear(); _balances[ResourceCost.ResourceId.Credits] = startingCredits;
        BalanceChanged?.Invoke();
    }

    // Absent future materials have zero stock; electricity is deliberately not a material ID.
    public int GetBalance(ResourceCost.ResourceId id) => _balances.TryGetValue(id, out int amount) ? amount : 0;

    // Reject malformed recipes and overflow, combining duplicate resource IDs before payment.
    public static bool TryTotals(Godot.Collections.Array<ResourceCost> costs, out Dictionary<ResourceCost.ResourceId, int> totals)
    {
        totals = new();
        if (costs == null) return false;
        foreach (var cost in costs)
        {
            if (cost == null || !Enum.IsDefined(cost.Id) || cost.Quantity <= 0) return false;
            totals.TryGetValue(cost.Id, out int previous);
            long combined = (long)previous + cost.Quantity;
            if (combined > int.MaxValue) return false;
            totals[cost.Id] = (int)combined;
        }
        return true;
    }

    // Empty cost lists explicitly mean free; all listed quantities must be positive.
    public bool CanAfford(Godot.Collections.Array<ResourceCost> costs) =>
        !_purchasing && TryTotals(costs, out var totals) && Affordable(totals);
    private bool Affordable(Dictionary<ResourceCost.ResourceId, int> totals)
    {
        foreach (var pair in totals) if (GetBalance(pair.Key) < pair.Value) return false;
        return true;
    }

    // Claim the world before debiting: failure cannot charge, and reentrant purchases are rejected.
    // The callback owns rollback of any partial world claim and must not publish purchase success.
    internal bool TryPurchase(Godot.Collections.Array<ResourceCost> costs, Func<bool> claim)
    {
        if (_purchasing || claim == null || !TryTotals(costs, out var totals) || !Affordable(totals)) return false;
        _purchasing = true;
        try
        {
            if (!claim()) return false;
            foreach (var pair in totals) _balances[pair.Key] = GetBalance(pair.Key) - pair.Value;
        }
        finally { _purchasing = false; }
        return true;
    }

    // Placement publishes after both occupancy and all debits are final.
    internal void PublishBalanceChanged() => BalanceChanged?.Invoke();
}
