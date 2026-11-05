// Registry/lifecycle bridge: every friendly placed building contributes one stationary fog source.
using Godot;
using System.Collections.Generic;

public partial class BuildingVisionSources : Node
{
    private FogOfWar _fog;
    private BuildingRegistry _registry;
    private Building _citadel;
    private readonly HashSet<Building> _registered = new();

    // One startup snapshot handles the existing CPU; future construction uses registry events only.
    public void Initialize(FogOfWar fog, BuildingRegistry registry, Building citadel)
    {
        if (_fog != null) throw new System.InvalidOperationException("Building vision sources are already initialized.");
        _fog = fog; _registry = registry; _citadel = citadel;
        registry.Added += OnAdded; registry.Removed += OnRemoved;
        foreach (Building building in registry.Buildings) OnAdded(building);
        SetProcess(false);
    }
    private float Radius(Building building) => building == _citadel ? _fog.Config.CitadelVisionRadius
        : building.VisionRadius ?? _fog.Config.DefaultBuildingVisionRadius;

    // Fog's queue coalesces changes per source and bounds circle updates across frames.
    private void OnAdded(Building building)
    {
        if (_registered.Contains(building)) return;
        _fog.RegisterSource(building.GetInstanceId(), building, Radius(building));
        _registered.Add(building); building.VisionChanged += OnVisionChanged;
    }
    private void OnVisionChanged(Building building)
    {
        _fog.SetSourceRadius(building.GetInstanceId(), Radius(building));
        _fog.RequestSourceUpdate(building.GetInstanceId());
    }
    private void OnRemoved(Building building)
    {
        if (!_registered.Remove(building)) return;
        building.VisionChanged -= OnVisionChanged;
        if (GodotObject.IsInstanceValid(_fog)) _fog.UnregisterSource(building.GetInstanceId());
    }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_registry)) { _registry.Added -= OnAdded; _registry.Removed -= OnRemoved; }
        foreach (Building building in _registered)
        {
            if (!GodotObject.IsInstanceValid(building)) continue;
            building.VisionChanged -= OnVisionChanged;
            if (GodotObject.IsInstanceValid(_fog)) _fog.UnregisterSource(building.GetInstanceId());
        }
        _registered.Clear(); _citadel = null; _registry = null; _fog = null;
    }
}
