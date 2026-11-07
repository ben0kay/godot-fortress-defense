// Single active construction clock for the battlefield. Queued sites are held by the dispatcher.
using Godot;
using System.Collections.Generic;

public partial class ConstructionWorkManager : Node
{
    private readonly HashSet<Building> _active = new();
    private readonly List<Building> _batch = new();

    public override void _Ready() => SetPhysicsProcess(false);

    public void Start(Building site)
    {
        if (!CombatTarget.IsAlive(site) || site.WorkPhase != BuildingWorkPhase.AwaitingDrone || !_active.Add(site)) return;
        site.WorkCancelled += Stop;
        SetPhysicsProcess(true);
        site.StartConstruction();
    }

    private void Stop(Building site)
    {
        if (!_active.Remove(site)) return;
        site.WorkCancelled -= Stop;
        if (_active.Count == 0) SetPhysicsProcess(false);
    }

    // Reuse a snapshot so completion or removal callbacks may change the active set.
    public override void _PhysicsProcess(double delta)
    {
        _batch.AddRange(_active);
        foreach (Building site in _batch)
        {
            if (!_active.Contains(site)) continue;
            if (!CombatTarget.IsAlive(site)) { Stop(site); continue; }
            site.AdvanceConstruction(delta);
            if (site.IsOperational) Stop(site);
        }
        _batch.Clear();
    }

    public override void _ExitTree()
    {
        foreach (Building site in _active)
            if (GodotObject.IsInstanceValid(site)) site.WorkCancelled -= Stop;
        _active.Clear();
        _batch.Clear();
    }
}
