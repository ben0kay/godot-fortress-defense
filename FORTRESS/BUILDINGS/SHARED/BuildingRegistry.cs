// Per-world placed-building lookup; no global scans, static state or processing loop.
using Godot;
using System;
using System.Collections.Generic;

public partial class BuildingRegistry : Node
{
    private readonly HashSet<Building> _buildings = new();
    public int Count => _buildings.Count;
    public IEnumerable<Building> Buildings => _buildings;
    public event Action<Building> Added;
    public event Action<Building> Removed;

    // Lifecycle calls registration once after a successful footprint claim.
    internal void Register(Building building) { if (_buildings.Add(building)) Added?.Invoke(building); }
    internal void Unregister(Building building) { if (_buildings.Remove(building)) Removed?.Invoke(building); }
    public override void _ExitTree() => _buildings.Clear();
}
