// Ground enemy runtime/lifecycle; movement, contact attacks and route selection have separate owners.
using Godot;

public partial class Enemy : Node2D
{
    [Export] public EnemyDefinition Definition { get; set; }
    public Building StrategicTarget { get; private set; }
    public Building ActiveTarget { get; private set; }
    public bool IsBreaching => ActiveTarget != null && ActiveTarget != StrategicTarget;
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
    private bool _attached, _noRoute;

    // Spawner supplies all references/stats once, before entering the tree.
    public void Initialize(WorldGrid grid, EnemyRegistry registry, EnemyPathRequests requests, Building citadel)
    {
        if (_attached) throw new System.InvalidOperationException("Enemy already initialized.");
        if (Definition == null) throw new System.InvalidOperationException("Enemy definition is required.");
        Definition.Validate(grid.CellSize);
        _grid = grid; _registry = registry; _requests = requests; StrategicTarget = citadel;
        MaxHealth = Health = Definition.MaxHealth; Radius = Definition.Radius;
        Speed = Definition.MovementSpeed; TurnSpeed = Definition.TurnSpeed;
        AttackRange = Definition.AttackRange; AttackDamage = Definition.AttackDamage; AttackCooldown = Definition.AttackCooldown;
        _attached = true;
    }
    public override void _Ready()
    {
        if (!_attached) { GD.PushError("Enemy must be initialized by its spawner."); SetPhysicsProcess(false); return; }
        _grid.TopologyChanged += OnTopologyChanged; _registry.Add(this); RequestRoute();
    }

    #region Routing and topology
    private void RequestRoute()
    {
        if (IsDead || !EnemyTargeting.Live(StrategicTarget)) { SetPhysicsProcess(false); return; }
        _movement.Stop(); WaitingForPath = true; _noRoute = false;
        _requests.Request(this); SetPhysicsProcess(true);
    }
    // Result delivery never redirects the strategic objective; a breach target is only temporary.
    public void AcceptRoute(Building target, Vector2[] path)
    {
        if (IsDead) return;
        WaitingForPath = false; ActiveTarget = target;
        _movement.Assign(path); _noRoute = path.Length == 0 || !EnemyTargeting.Live(target);
        VisualChanged?.Invoke();
        if (_noRoute) SetPhysicsProcess(false); // Wake only on topology change, not endless failed retries.
    }
    private void OnTopologyChanged(Rect2I changed)
    {
        if (IsDead) return;
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
        _attack.Tick(delta);
        if (WaitingForPath || _noRoute) return;
        if (!EnemyTargeting.Live(ActiveTarget)) { RequestRoute(); return; }
        if (!_movement.HasRoute)
        {
            if (EnemyTargeting.CanAttackFrom(_grid, GlobalPosition, ActiveTarget, AttackRange))
            {
                Vector2 facing = ActiveTarget.GlobalPosition - GlobalPosition;
                Rotation = Mathf.LerpAngle(Rotation, facing.Angle(), Mathf.Min(1, TurnSpeed * (float)delta));
                if (_attack.TryAttack(this, _grid, ActiveTarget)) VisualChanged?.Invoke();
            }
            else RequestRoute();
            return;
        }
        if (!_movement.Advance(this, _grid, delta)) RequestRoute();
    }

    // All authoritative movement/teleports update the shared combat index at the same boundary.
    public void SetMovementPosition(Vector2 position)
    { GlobalPosition = position; if (_attached && GodotObject.IsInstanceValid(_registry)) _registry.UpdatePosition(this); }

    // Shared receiver used by real projectiles and developer damage controls.
    public void ApplyDamage(float amount)
    {
        if (IsDead || !float.IsFinite(amount) || amount <= 0) return;
        Health = Mathf.Max(0, Health - amount); VisualChanged?.Invoke();
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
        if (GodotObject.IsInstanceValid(_grid)) _grid.TopologyChanged -= OnTopologyChanged;
        if (GodotObject.IsInstanceValid(_requests)) _requests.Cancel(this);
        if (GodotObject.IsInstanceValid(_registry)) _registry.Remove(this);
    }
}
