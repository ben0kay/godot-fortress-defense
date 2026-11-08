// Shared enemy runtime/lifecycle; movement, contact attacks and route selection have separate owners.
using Godot;

public partial class Enemy : Node2D
{
    [Export] public EnemyDefinition Definition { get; set; }
    public Building StrategicTarget { get; private set; }
    public ICombatTarget ActiveTarget { get; private set; }
    public ICombatTarget NavigationTarget { get; private set; }
    public EnemyDefinition.TargetPriority TargetRule { get; private set; }
    public float PlayerDetectionRange { get; private set; }
    internal double NextTargetDecision { get; set; }
    private EnemyTargetRoster _targets;
    private Vector2I _routedMobileCell, _blockedMobileCell;
    private ulong _blockedMobileRevision;
    private bool _mobileBlocked;
    public bool IsBreaching => ActiveTarget is Building && ActiveTarget != NavigationTarget;
    public bool IsDead { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public float Radius { get; private set; }
    public float Speed { get; private set; }
    public float TurnSpeed { get; private set; }
    public float AttackRange { get; private set; }
    public float AttackDamage { get; private set; }
    public float AttackCooldown { get; private set; }
    public bool WaitingForPath { get; private set; }
    public event System.Action VisualChanged;
    private WorldGrid _grid;
    private EnemyRegistry _registry;
    private EnemyPathRequests _requests;
    private readonly EnemyMovement _movement = new();
    private readonly EnemyMeleeAttack _attack = new();
    private DamageResponseProfile _damageResponse;
    private DamageAffinityProfile _damageAffinity;
    private StatusImmunityProfile _statusImmunity;
    private DamageInfo _attackHit;
    internal DamageInfo AttackHit => _attackHit;
    private bool _attached, _noRoute;

    // Spawner supplies all references/stats once, before entering the tree.
    public void Initialize(WorldGrid grid, EnemyRegistry registry, EnemyPathRequests requests, Building citadel, EnemyTargetRoster targets = null)
    {
        if (_attached) throw new System.InvalidOperationException("Enemy already initialized.");
        if (Definition == null) throw new System.InvalidOperationException("Enemy definition is required.");
        Definition.Validate(grid.CellSize);
        _damageResponse = DamageResponseProfile.Default;
        _damageAffinity = Definition.DamageAffinity; _statusImmunity = Definition.StatusImmunity;
        _grid = grid; _registry = registry; _requests = requests; StrategicTarget = citadel; NavigationTarget = citadel; _targets = targets;
        MovementLayer = Definition.MovementLayer; IsImpact = Definition.AttackBehavior == EnemyAttackBehavior.Impact;
        if (RadiusDoesNotFitWorld(Definition.Radius, grid)) throw new System.InvalidOperationException("Enemy radius does not fit world bounds.");
        if (IsFlying) _flight = new EnemyFlyingMovement();
        if (IsImpact) _impact = new EnemyImpactAttack();
        TargetRule = Definition.TargetRule; PlayerDetectionRange = Definition.PlayerDetectionRange;
        MaxHealth = Health = Definition.MaxHealth; Radius = Definition.Radius;
        Speed = Definition.MovementSpeed; TurnSpeed = Definition.TurnSpeed;
        AttackRange = Definition.AttackRange; AttackDamage = Definition.AttackDamage; AttackCooldown = Definition.AttackCooldown;
        _attackHit = new(AttackDamage, DamageType.Kinetic, new DamageSource(DamageSourceKind.Enemy, this));
        InitializeCapabilities();
        if (HuntsBuildings) { StrategicTarget = null; NavigationTarget = null; AcquisitionState = "Initial acquisition"; }
        _attached = true;
    }
    public override void _Ready()
    {
        if (!_attached) { GD.PushError("Enemy must be initialized by its spawner."); SetPhysicsProcess(false); return; }
        if (!IsFlying) _grid.TopologyChanged += OnTopologyChanged;
        WatchFlightObjective(); _registry.Add(this); _targets?.Register(this);
        if (HuntsBuildings) SetPhysicsProcess(false); else RequestRoute();
    }

    #region Routing and topology
    private void RequestRoute()
    {
        if (IsDead || !EnemyTargeting.Live(StrategicTarget)) { SetPhysicsProcess(false); return; }
        if (IsFlying) { BeginFlight(); return; }
        if (!EnemyTargeting.Live(NavigationTarget)) NavigationTarget = StrategicTarget;
        if (NavigationTarget != StrategicTarget) _routedMobileCell = _grid.WorldToCell(NavigationTarget.TargetNode.GlobalPosition);
        _movement.Stop(); WaitingForPath = true; _noRoute = false;
        _requests.Request(this); SetPhysicsProcess(true);
    }
    // Result delivery never redirects the strategic objective; a breach target is only temporary.
    public void AcceptRoute(ICombatTarget target, Vector2[] path)
    {
        if (IsDead || IsFlying) return;
        if (path.Length == 0 && NavigationTarget != StrategicTarget)
        {
            _mobileBlocked = true; _blockedMobileCell = _routedMobileCell; _blockedMobileRevision = _grid.Revision;
            NavigationTarget = StrategicTarget; RequestRoute(); return;
        }
        WaitingForPath = false; ActiveTarget = target;
        _movement.Assign(path); _noRoute = path.Length == 0 || !EnemyTargeting.Live(target);
        VisualChanged?.Invoke();
        if (_noRoute) SetPhysicsProcess(false); // Wake only on topology change, not endless failed retries.
    }
    private void OnTopologyChanged(Rect2I changed)
    {
        if (IsDead) return;
        if (HuntsBuildings && !EnemyTargeting.Live(StrategicTarget)) { OnHuntedBuildingLost(); return; }
        if (!EnemyTargeting.Live(StrategicTarget))
        { _requests.Cancel(this); WaitingForPath = false; ActiveTarget = null; _movement.Stop(); SetPhysicsProcess(false); VisualChanged?.Invoke(); return; }
        Rect2 dirty = _grid.FootprintRect(changed).Grow(Radius + _grid.ClearanceCells * _grid.CellSize);
        if (_noRoute || IsBreaching || !EnemyTargeting.Live(ActiveTarget) || _movement.Intersects(GlobalPosition, dirty)) RequestRoute();
    }
    #endregion

    // Physics cadence stays independent of route requests; failed safety stops before any illegal step.
    public override void _PhysicsProcess(double delta)
    {
        if (!_attached || IsDead || !EnemyTargeting.Live(StrategicTarget)) return;
        if (IsFlying) { ProcessFlying(delta); return; }
        if (IsImpact) { ProcessGroundImpact(delta); return; }
        if (IsRanged) { ProcessRanged(delta); return; }
        _attack.Tick(delta);
        if (NavigationTarget != StrategicTarget && EnemyTargeting.CanAttackFrom(_grid, GlobalPosition, NavigationTarget, AttackRange))
        {
            _movement.Stop();
            Rotation = Mathf.LerpAngle(Rotation, (NavigationTarget.TargetNode.GlobalPosition - GlobalPosition).Angle(), Mathf.Min(1, TurnSpeed * (float)delta));
            if (_attack.TryAttack(this, _grid, NavigationTarget)) VisualChanged?.Invoke();
            return;
        }
        if (WaitingForPath || _noRoute) return;
        if (!EnemyTargeting.Live(ActiveTarget)) { RequestRoute(); return; }
        if (!_movement.HasRoute)
        {
            if (EnemyTargeting.CanAttackFrom(_grid, GlobalPosition, ActiveTarget, AttackRange))
            {
                Vector2 facing = ActiveTarget.TargetNode.GlobalPosition - GlobalPosition;
                Rotation = Mathf.LerpAngle(Rotation, facing.Angle(), Mathf.Min(1, TurnSpeed * (float)delta));
                if (_attack.TryAttack(this, _grid, ActiveTarget)) VisualChanged?.Invoke();
            }
            else if (NavigationTarget == StrategicTarget) RequestRoute(); // Mobile pursuit is refreshed by bounded target decisions.
            return;
        }
        if (!_movement.Advance(this, _grid, delta)) RequestRoute();
    }

    // Mobile-target choice and moving-goal refresh are staggered outside physics/path execution.
    public void ConsiderMobileTarget(ICombatTarget mobile)
    {
        if (HuntsBuildings || IsDead || !EnemyTargeting.Live(StrategicTarget)) return;
        ICombatTarget chosen = EnemyTargeting.ChooseObjective(this, mobile);
        if (chosen != StrategicTarget && _mobileBlocked && _blockedMobileRevision == _grid.Revision
            && _grid.WorldToCell(chosen.TargetNode.GlobalPosition) == _blockedMobileCell) chosen = StrategicTarget;
        if (NavigationTarget != chosen)
        {
            if (!IsFlying) _requests.Cancel(this); NavigationTarget = chosen; ActiveTarget = null; RequestRoute(); return;
        }
        if (IsFlying || chosen == StrategicTarget || WaitingForPath || EnemyTargeting.CanAttackFrom(_grid, GlobalPosition, chosen, AttackRange)) return;
        bool movedCell = _grid.WorldToCell(chosen.TargetNode.GlobalPosition) != _routedMobileCell;
        if (movedCell || (!_noRoute && !_movement.HasRoute && !EnemyTargeting.CanAttackFrom(_grid, GlobalPosition, chosen, AttackRange))) RequestRoute();
    }

    private static bool RadiusDoesNotFitWorld(float radius, WorldGrid grid) => radius * 2 >= Mathf.Min(grid.Dimensions.X, grid.Dimensions.Y) * grid.CellSize;

    // All authoritative movement/teleports update the shared combat index at the same boundary.
    public void SetMovementPosition(Vector2 position)
    { GlobalPosition = position; if (_attached && GodotObject.IsInstanceValid(_registry)) _registry.UpdatePosition(this); }

    // Developer damage uses the same typed receiver as gameplay.
    public void ApplyDamage(float amount) => ApplyDamage(DamageInfo.FromLegacy(amount));

    // Independent effect eligibility for future delivery after a surviving hit.
    public bool AllowsStatusEffect(StatusEffectPayload effect) => StatusImmunityProfile.Allows(effect, _statusImmunity);

    // Resolve the hit, then let this actor publish health and lifecycle changes.
    public void ApplyDamage(DamageInfo hit)
    {
        if (!_attached || IsDead || IsQueuedForDeletion() || hit.Amount == 0) return;
        DamageResult result = DamageResolver.Resolve(hit, 0, 0, Health, _damageResponse, _damageAffinity);
        if (result.HullLoss == 0) return;
        Health = result.Hull; VisualChanged?.Invoke();
        if (Health == 0)
        {
            var pulse = new DestructionPulse { Radius = Radius * 2, Tint = Colors.OrangeRed };
            GetTree().CurrentScene.AddChild(pulse); pulse.GlobalPosition = GlobalPosition; Despawn();
        }
    }
    public void Despawn() { if (IsDead) return; IsDead = true; Detach(); Hide(); SetPhysicsProcess(false); QueueFree(); }
    public override void _ExitTree() => Detach();
    private void Detach()
    {
        if (!_attached) return;
        _attached = false;
        if (HuntsBuildings && GodotObject.IsInstanceValid(StrategicTarget)) StrategicTarget.TargetLost -= OnHuntedBuildingLost;
        _projectiles = null;
        UnwatchFlightObjective();
        if (!IsFlying && GodotObject.IsInstanceValid(_grid)) _grid.TopologyChanged -= OnTopologyChanged;
        if (!IsFlying && GodotObject.IsInstanceValid(_requests)) _requests.Cancel(this);
        if (GodotObject.IsInstanceValid(_targets)) _targets.Unregister(this);
        if (GodotObject.IsInstanceValid(_registry)) _registry.Remove(this);
    }
}
