// Active-only PWR1 allocation over the existing PWR0 memberships.
using Godot;
using System;
using System.Collections.Generic;

public partial class PowerNetworkManager
{
    [Export] public float CitadelEnergyPerSecond { get; set; } = 30;

    private const double ServiceInterval = .1;
    private readonly Dictionary<long, double> _generators = new();
    private readonly Dictionary<long, PowerCapacitor> _consumers = new();
    private readonly Dictionary<int, (double Supply, double Demand)> _budgets = new();
    private readonly List<(PowerCapacitor Buffer, int Network, double Request)> _requests = new();
    private double _elapsed, _supplyElapsed;

    // PWR1 producers are independent endpoints, even when sharing a building with a relay.
    internal void BindGenerator(Building owner, float radius, float rate)
    {
        if (!float.IsFinite(rate) || rate < 0) throw new ArgumentOutOfRangeException(nameof(rate));
        long id = BindBuilding(owner, PowerRole.Generator, radius);
        _generators.Add(id, rate);
        InvalidateDelivery();
    }

    // Pending construction registers a role but receives no allocation until operational.
    internal void BindConsumer(Building owner, float radius, PowerCapacitor buffer)
    {
        long id = BindBuilding(owner, PowerRole.Consumer, radius);
        _consumers.Add(id, buffer);
        InvalidateDelivery();
    }

    // Awake only when there is an active consumer. A shot can wake a full, dormant buffer.
    internal void WakeDelivery()
    {
        if (EndpointCount > 0 && _consumers.Count > 0 && IsInsideTree()) SetPhysicsProcess(true);
    }

    // New memberships cannot claim supply from before a topology change. Idle time still accrues.
    private void InvalidateDelivery()
    {
        _supplyElapsed = 0;
        WakeDelivery();
    }

    public override void _Ready() => SetPhysicsProcess(false);

    public override void _PhysicsProcess(double delta)
    {
        _elapsed += delta;
        _supplyElapsed += delta;
        if (_elapsed < ServiceInterval) return;
        double seconds = _elapsed, supplySeconds = _supplyElapsed;
        _elapsed = _supplyElapsed = 0;
        if (!Distribute(seconds, supplySeconds)) SetPhysicsProcess(false);
    }

    // Pool only this interval's generation per component and split it in proportion to capped requests.
    // There is no persistent network balance, and all scratch storage is reused.
    internal bool Distribute(double seconds, double supplySeconds)
    {
        _budgets.Clear();
        foreach (var pair in _generators)
        {
            int network = NetworkFor(pair.Key).NetworkId;
            if (network < 0 || pair.Value == 0) continue;
            _budgets.TryGetValue(network, out var budget);
            _budgets[network] = (budget.Supply + pair.Value * supplySeconds, 0);
        }

        foreach (var pair in _consumers)
        {
            if (!_entries[pair.Key].Active) continue;
            int network = NetworkFor(pair.Key).NetworkId;
            double request = pair.Value.Request(seconds, supplySeconds);
            if (_budgets.TryGetValue(network, out var budget))
                _budgets[network] = (budget.Supply, budget.Demand + request);
            _requests.Add((pair.Value, network, request));
        }

        bool needsService = false;
        foreach (var request in _requests)
        {
            bool supplied = _budgets.TryGetValue(request.Network, out var budget);
            double fraction = supplied && budget.Demand > 0 ? Math.Min(1, budget.Supply / budget.Demand) : 0;
            request.Buffer.Advance(seconds, request.Request * fraction);
            needsService |= request.Buffer.NeedsService(supplied);
        }
        _requests.Clear();
        return needsService;
    }

    // PWR0 removal and scene teardown also own delivery cleanup.
    private void RemoveDelivery(long id)
    {
        _generators.Remove(id);
        _consumers.Remove(id);
    }

    private void ClearDelivery()
    {
        _generators.Clear(); _consumers.Clear(); _budgets.Clear(); _requests.Clear();
        _elapsed = _supplyElapsed = 0;
    }
}
