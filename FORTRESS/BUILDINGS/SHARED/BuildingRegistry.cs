// Per-world placed-building lookup; no global scans, static state or processing loop.
using Godot;
using System;
using System.Collections.Generic;

public partial class BuildingRegistry : Node
{
    private readonly HashSet<Building> _buildings = new();
    private readonly BuildingSpatialIndex _spatial = new();
    public void Query(Rect2 bounds, List<Building> result) => _spatial.Query(bounds, result);
    public int Count => _buildings.Count;
    public IEnumerable<Building> Buildings => _buildings;
    public event Action<Building> Added;
    public event Action<Building> Removed;

    // Lifecycle calls registration once after a successful footprint claim.
    internal void Register(Building building) { if (_buildings.Add(building)) { _spatial.Add(building); Added?.Invoke(building); } }
    internal void Unregister(Building building) { if (_buildings.Remove(building)) { _spatial.Remove(building); Removed?.Invoke(building); } }
    public override void _ExitTree() { _buildings.Clear(); _spatial.Clear(); }
}
