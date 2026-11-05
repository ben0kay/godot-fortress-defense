// Lightweight flight/impact integration on the shared Enemy root; target choice remains in the existing policy owner.
using Godot;

public partial class Enemy
{
    public EnemyMovementLayer MovementLayer { get; private set; }
    public bool IsFlying => MovementLayer == EnemyMovementLayer.Flying;
    public bool IsImpact { get; private set; }
    private EnemyFlyingMovement _flight;
    private EnemyImpactAttack _impact;
    private Building _watchedFlightObjective;

    // Flying CPU seekers observe loss directly instead of subscribing to every ground topology edit.
    private void WatchFlightObjective()
    {
        if (!IsFlying || HuntsBuildings || !EnemyTargeting.Live(StrategicTarget)) return;
        _watchedFlightObjective = StrategicTarget; _watchedFlightObjective.TargetLost += OnFlightObjectiveLost;
    }
    private void OnFlightObjectiveLost()
    {
        UnwatchFlightObjective(); StrategicTarget = null; NavigationTarget = null; ActiveTarget = null;
        WaitingForPath = false; _movement.Stop(); SetPhysicsProcess(false); VisualChanged?.Invoke();
    }
    private void UnwatchFlightObjective()
    {
        if (GodotObject.IsInstanceValid(_watchedFlightObjective)) _watchedFlightObjective.TargetLost -= OnFlightObjectiveLost;
        _watchedFlightObjective = null;
    }
    private void BeginFlight()
    {
        if (!EnemyTargeting.Live(NavigationTarget)) NavigationTarget = StrategicTarget;
        ActiveTarget = NavigationTarget; WaitingForPath = false; _noRoute = false;
        SetPhysicsProcess(EnemyTargeting.Live(ActiveTarget)); VisualChanged?.Invoke();
    }

    // Direct flight never queries ground geometry. Attack behavior is chosen independently from movement.
    private void ProcessFlying(double delta)
    {
        ICombatTarget target = NavigationTarget;
        if (!EnemyTargeting.Live(target)) { BeginFlight(); return; }
        if (IsRanged)
        {
            _ranged.Tick(delta);
            if (_ranged.CanAttack(this, _grid, target))
            {
                Rotation = Mathf.LerpAngle(Rotation, (target.TargetNode.GlobalPosition - GlobalPosition).Angle(), Mathf.Min(1, TurnSpeed * (float)delta));
                if (_ranged.TryAttack(this, _grid, _projectiles, target)) VisualChanged?.Invoke();
                return;
            }
        }
        else if (!IsImpact)
        {
            _attack.Tick(delta);
            if (EnemyTargeting.CanAttackFrom(_grid, GlobalPosition, target, AttackRange, 0, true))
            { if (_attack.TryAttack(this, _grid, target, true)) VisualChanged?.Invoke(); return; }
        }
        Vector2 from = GlobalPosition, destination = target.TargetNode.GlobalPosition;
        Vector2 next = _flight.NextPosition(this, _grid, destination, delta);
        if (IsImpact && _impact.TryResolve(this, _grid, target, from, next)) return;
        _flight.Apply(this, destination, next, delta);
    }

    // The same impact receiver can finish a ground route without duplicating damage/despawn logic.
    private void ProcessGroundImpact(double delta)
    {
        if (WaitingForPath || _noRoute) return;
        if (!EnemyTargeting.Live(ActiveTarget)) { RequestRoute(); return; }
        Vector2 from = GlobalPosition;
        if (_movement.HasRoute)
        { if (!_movement.Advance(this, _grid, delta)) { RequestRoute(); return; } }
        else
        {
            Rect2 bounds = ActiveTarget.TargetBounds(_grid);
            Vector2 destination = GlobalPosition.Clamp(bounds.Position, bounds.End);
            Vector2 next = GlobalPosition.MoveToward(destination, Speed * (float)delta);
            if (!_grid.SegmentClear(GlobalPosition, next, Radius, ActiveTarget as Building)) { RequestRoute(); return; }
            SetMovementPosition(next);
        }
        _impact.TryResolve(this, _grid, ActiveTarget, from, GlobalPosition);
    }
}
