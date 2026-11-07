// Level-owned FIFO dispatcher for committed sites, pooled flight and Citadel-loss fallback.
using Godot;
using System.Collections.Generic;

public partial class ConstructionDroneManager : Node2D
{
    [Export] public ConstructionDroneMasterConfig Config { get; set; }
    [Export] public PackedScene DroneScene { get; set; }

    private readonly LinkedList<Building> _pending = new();
    private readonly Dictionary<Building, LinkedListNode<Building>> _pendingBySite = new();
    private readonly List<ConstructionDrone> _active = new();
    private readonly Stack<ConstructionDrone> _parked = new();
    private Building _citadel;
    private ConstructionWorkManager _work;
    private FogPresentation _fog;
    private BuildingPlacement _placement;
    private Rect2 _bounds;
    private bool _sourceLost;

    private bool CanDispatch => Config.Enabled && !_sourceLost && CombatTarget.IsAlive(_citadel);

    public override void _Ready() => SetPhysicsProcess(false);

    public void Initialize(BuildingPlacement placement, Building citadel, ConstructionWorkManager work, FogPresentation fog, Rect2 bounds)
    {
        if (Config == null || DroneScene == null)
            throw new System.InvalidOperationException("Construction drone config and scene are required.");
        Config.Validate();
        _placement = placement;
        _citadel = citadel;
        _work = work;
        _fog = fog;
        _bounds = bounds;
        placement.ConstructionRequested += Enqueue;
        citadel.TargetLost += OnSourceLost;
    }

    // Called only after the placement transaction succeeds.
    private void Enqueue(Building site)
    {
        if (!CombatTarget.IsAlive(site) || site.WorkPhase != BuildingWorkPhase.AwaitingDrone) return;
        site.WorkCancelled += ReleaseSite;
        site.WorkCompleted += ReleaseSite;
        QueueSite(site);
        RefreshProcessing();
    }

    private void QueueSite(Building site) => _pendingBySite.Add(site, _pending.AddLast(site));

    private void ReleaseSite(Building site)
    {
        Unwatch(site);
        if (_pendingBySite.Remove(site, out var queued)) _pending.Remove(queued);
        foreach (ConstructionDrone drone in _active)
        {
            if (drone.Site != site) continue;
            if (CanDispatch) drone.ReturnHome(_citadel.GlobalPosition, Config);
            else drone.Park();
            break;
        }
        RefreshProcessing();
    }

    private void OnSourceLost()
    {
        _sourceLost = true;
        RefreshProcessing();
    }

    private void Unwatch(Building site)
    {
        if (!GodotObject.IsInstanceValid(site)) return;
        site.WorkCancelled -= ReleaseSite;
        site.WorkCompleted -= ReleaseSite;
    }

    // Flying and deploying drones release the site on source loss. An already deployed beam finishes its work.
    public override void _PhysicsProcess(double delta)
    {
        bool dispatch = CanDispatch;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            ConstructionDrone drone = _active[i];
            if (!dispatch && drone.Phase is (ConstructionDronePhase.Flying or ConstructionDronePhase.Deploying or ConstructionDronePhase.Returning))
            {
                Building site = drone.Site;
                if (CombatTarget.IsAlive(site)) QueueSite(site);
                else Unwatch(site);
                drone.Park();
            }
            else if (drone.Advance((float)delta, Config))
                _work.Start(drone.Site);

            if (drone.Phase != ConstructionDronePhase.Parked) continue;
            _active.RemoveAt(i);
            _parked.Push(drone);
        }

        int budget = Config.DispatchBudget;
        while (budget-- > 0 && _pending.First != null && (dispatch ? _active.Count < Config.MaxActiveDrones : !Config.RequireDroneToBuild))
        {
            Building site = _pending.First.Value;
            _pending.RemoveFirst();
            _pendingBySite.Remove(site);
            if (!CombatTarget.IsAlive(site)) { Unwatch(site); continue; }
            if (dispatch) Launch(site);
            else { Unwatch(site); _work.Start(site); }
        }
        RefreshProcessing();
    }

    private void Launch(Building site)
    {
        if (!_parked.TryPop(out ConstructionDrone drone))
        {
            drone = DroneScene.Instantiate<ConstructionDrone>();
            AddChild(drone);
            drone.BindFog(_fog);
        }
        Vector2 hover = (site.GlobalPosition + Config.HoverOffset).Clamp(_bounds.Position + Vector2.One, _bounds.End - Vector2.One);
        drone.Launch(site, _citadel.GlobalPosition, hover, Config);
        _active.Add(drone);
    }

    // Parked drones and drones holding a steady beam have no flight ticks.
    private void RefreshProcessing()
    {
        bool dispatch = CanDispatch;
        if (_pending.Count > 0 && (dispatch ? _active.Count < Config.MaxActiveDrones : !Config.RequireDroneToBuild))
        { SetPhysicsProcess(true); return; }
        foreach (ConstructionDrone drone in _active)
        {
            if (drone.Phase == ConstructionDronePhase.Building) continue;
            SetPhysicsProcess(true);
            return;
        }
        SetPhysicsProcess(false);
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_placement)) _placement.ConstructionRequested -= Enqueue;
        if (GodotObject.IsInstanceValid(_citadel)) _citadel.TargetLost -= OnSourceLost;
        foreach (Building site in _pending) Unwatch(site);
        foreach (ConstructionDrone drone in _active)
            if (GodotObject.IsInstanceValid(drone)) Unwatch(drone.Site);
        _pending.Clear();
        _pendingBySite.Clear();
        _active.Clear();
        _parked.Clear();
    }
}
