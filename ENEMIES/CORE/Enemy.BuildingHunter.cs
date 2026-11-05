// Building-policy state and ranged execution hooks on the existing Enemy runtime; no parallel AI controller.
using Godot;

public partial class Enemy
{
    public bool HuntsBuildings => TargetRule == EnemyDefinition.TargetPriority.BuildingHunter;
    public bool IsRanged => _ranged != null;
    public float FiringClearance => _ranged?.ProjectileRadius ?? 0;
    public float HunterDetectionRadius { get; private set; }
    public float HunterMinimumScanInterval { get; private set; }
    public float HunterMaximumScanInterval { get; private set; }
    public float HunterSwitchImprovement { get; private set; }
    public float HunterMinimumSwitchInterval { get; private set; }
    internal double NextStrategicSwitch { get; set; }
    public long LocalTargetScans { get; internal set; }
    public string AcquisitionState { get; internal set; } = "CPU";
    private EnemyRangedAttack _ranged;
    private Node2D _projectiles;

    // Capture optional attack/policy tuning once; Enemy001 has neither hunter scans nor a ranged executor.
    private void InitializeCapabilities()
    {
        if (HuntsBuildings)
        {
            if (_targets?.Buildings == null) throw new System.InvalidOperationException("Building hunters require initialized shared target services.");
            var hunter = Definition.BuildingHunting;
            HunterDetectionRadius = hunter.DetectionRadius; HunterMinimumScanInterval = hunter.MinimumScanInterval;
            HunterMaximumScanInterval = hunter.MaximumScanInterval; HunterSwitchImprovement = hunter.SwitchImprovement;
            HunterMinimumSwitchInterval = hunter.MinimumSwitchInterval;
        }
        if (Definition.RangedAttack != null)
        {
            _projectiles = _targets?.Projectiles;
            if (!GodotObject.IsInstanceValid(_projectiles)) throw new System.InvalidOperationException("Ranged enemies require a projectile container.");
            _ranged = new EnemyRangedAttack(Definition.RangedAttack);
        }
    }

    // A target change cancels one obsolete route and submits one replacement; unchanged targets never repath here.
    public void SetHuntedBuilding(Building target)
    {
        if (!HuntsBuildings || IsDead || StrategicTarget == target) return;
        if (GodotObject.IsInstanceValid(StrategicTarget)) StrategicTarget.TargetLost -= OnHuntedBuildingLost;
        StrategicTarget = target; NavigationTarget = target; ActiveTarget = null;
        _requests.Cancel(this); _movement.Stop(); WaitingForPath = false; _noRoute = false;
        if (EnemyTargeting.Live(target))
        {
            target.TargetLost += OnHuntedBuildingLost; AcquisitionState = "Locked: " + target.BuildingId; RequestRoute();
        }
        else { AcquisitionState = "Acquiring"; SetPhysicsProcess(false); }
        VisualChanged?.Invoke();
    }
    private void OnHuntedBuildingLost()
    {
        if (IsDead) return;
        SetHuntedBuilding(null); _targets.RequestAcquisition(this, false);
    }

    // Stopping can cancel an unneeded pending path, preserving the queue's frozen-origin invariant.
    private void ProcessRanged(double delta)
    {
        _ranged.Tick(delta);
        ICombatTarget target = EnemyTargeting.Live(ActiveTarget) ? ActiveTarget : NavigationTarget;
        if (_ranged.CanAttack(this, _grid, target))
        {
            if (WaitingForPath) _requests.Cancel(this);
            WaitingForPath = false; _noRoute = false; ActiveTarget = target; _movement.Stop();
            Rect2 bounds = target.TargetBounds(_grid);
            Vector2 aim = GlobalPosition.Clamp(bounds.Position, bounds.End) - GlobalPosition;
            Rotation = Mathf.LerpAngle(Rotation, aim.Angle(), Mathf.Min(1, TurnSpeed * (float)delta));
            if (_ranged.TryAttack(this, _grid, _projectiles, target)) VisualChanged?.Invoke();
            return;
        }
        if (WaitingForPath || _noRoute) return;
        if (!EnemyTargeting.Live(ActiveTarget)) { RequestRoute(); return; }
        if (!_movement.HasRoute || !_movement.Advance(this, _grid, delta)) RequestRoute();
    }
}
