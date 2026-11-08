// Optional drop-in chassis: keyboard movement, authoritative grid sweeps and typed health damage; no weapons.
using Godot;
using System;

public partial class Player : Node2D, ICombatTarget
{
    [ExportGroup("Movement")]
    [Export] public float MovementSpeed { get; set; } = 230;
    [Export] public float Acceleration { get; set; } = 1400;
    [Export] public float Radius { get; set; } = 12;
    [Export] public Key UpKey { get; set; } = Key.Up;
    [Export] public Key DownKey { get; set; } = Key.Down;
    [Export] public Key LeftKey { get; set; } = Key.Left;
    [Export] public Key RightKey { get; set; } = Key.Right;
    [ExportGroup("Health / Binding")]
    [Export] public float MaxHealth { get; set; } = 100;
    [Export] public WorldGrid World { get; set; }
    [ExportGroup("Damage Response / DMG0")]
    [Export] public DamageAffinityProfile DamageAffinity { get; set; }
    [Export] public StatusImmunityProfile StatusImmunity { get; set; }
    private DamageResponseProfile _damageResponse;
    private DamageAffinityProfile _damageAffinity;
    private StatusImmunityProfile _statusImmunity;
    public float Health { get; private set; }
    public bool IsDead { get; private set; }
    public Func<bool> InputBlocked { get; set; }
    internal Func<Key, bool> MovementKeyBlocked { get; set; }
    public event Action HealthChanged;
    public event Action Died;
    public event Action TargetLost;
    public Node2D TargetNode => this;
    public bool TargetAlive => _initialized && IsInsideTree() && !IsDead && !IsQueuedForDeletion();
    private Vector2 _velocity;
    private bool _initialized, _lost;
    private Tween _flash;

    // Level composition calls before AddChild; other scenes may supply World in the Inspector.
    public void Initialize(WorldGrid world)
    {
        if (_initialized) throw new InvalidOperationException("Player already initialized.");
        if (world == null || !float.IsFinite(MovementSpeed) || MovementSpeed <= 0 || !float.IsFinite(Acceleration) || Acceleration <= 0
            || !float.IsFinite(Radius) || Radius <= 0 || Radius >= world.CellSize * .5f || !float.IsFinite(MaxHealth) || MaxHealth <= 0)
            throw new InvalidOperationException("Invalid player stats or world binding.");
        _damageResponse = DamageResponseProfile.Default;
        _damageAffinity = DamageAffinity; _statusImmunity = StatusImmunity;
        World = world; Health = MaxHealth; _initialized = true;
    }
    public override void _Ready()
    {
        if (!_initialized && World != null) Initialize(World);
        SetPhysicsProcess(false); QueueRedraw();
    }
    public Rect2 TargetBounds(WorldGrid grid) => new(GlobalPosition - Vector2.One * Radius, Vector2.One * Radius * 2);

    // Wake only for movement keys; a chassis without a world remains harmless and dormant.
    public override void _Input(InputEvent input)
    {
        if (input is InputEventKey key && (key.Keycode == UpKey || key.Keycode == DownKey || key.Keycode == LeftKey || key.Keycode == RightKey)) WakeMovement();
    }
    public void WakeMovement() { if (_initialized && TargetAlive) SetPhysicsProcess(true); }

    private bool IsMovementKeyHeld(Key key) => !(MovementKeyBlocked?.Invoke(key) ?? false) && Input.IsPhysicalKeyPressed(key);

    // Full segment tests prevent tunnelling; separate axes permit simple wall sliding without a second collision grid.
    public override void _PhysicsProcess(double delta)
    {
        if (!TargetAlive || World == null || (InputBlocked?.Invoke() ?? false) || GetViewport().GuiGetFocusOwner() != null)
        { _velocity = Vector2.Zero; SetPhysicsProcess(false); return; }
        Vector2 direction = new((IsMovementKeyHeld(RightKey) ? 1 : 0) - (IsMovementKeyHeld(LeftKey) ? 1 : 0),
            (IsMovementKeyHeld(DownKey) ? 1 : 0) - (IsMovementKeyHeld(UpKey) ? 1 : 0));
        _velocity = _velocity.MoveToward(direction.Normalized() * MovementSpeed, Acceleration * (float)delta);
        Vector2 step = _velocity * (float)delta;
        MoveAxis(new(step.X, 0)); MoveAxis(new(0, step.Y));
        if (_velocity.LengthSquared() > .1f) Rotation = _velocity.Angle();
        if (direction == Vector2.Zero && _velocity == Vector2.Zero) SetPhysicsProcess(false);
    }
    private void MoveAxis(Vector2 step)
    {
        if (World.SegmentClear(GlobalPosition, GlobalPosition + step, Radius)) GlobalPosition += step;
    }

    // Placement and spawn preflights use the same conservative radius as movement, without making the player a nav obstacle.
    public bool BlocksPlacement(WorldGrid grid, Rect2I area) => TargetAlive
        && (grid.FootprintRect(area).Grow(Radius + .05f).HasPoint(GlobalPosition)
            || area.Grow(grid.ClearanceCells).HasPoint(grid.WorldToCell(GlobalPosition)));
    // Developer damage uses the same typed receiver as gameplay.
    public void ApplyDamage(float amount) => ApplyDamage(DamageInfo.FromLegacy(amount));

    // Independent effect eligibility for future delivery after a surviving hit.
    public bool AllowsStatusEffect(StatusEffectPayload effect) => StatusImmunityProfile.Allows(effect, _statusImmunity);

    // Resolve the hit, then let this actor publish health and lifecycle changes.
    public void ApplyDamage(DamageInfo hit)
    {
        if (!TargetAlive || hit.Amount == 0) return;
        DamageResult result = DamageResolver.Resolve(hit, 0, 0, Health, _damageResponse, _damageAffinity);
        if (result.HullLoss == 0) return;
        Health = result.Hull; HealthChanged?.Invoke(); QueueRedraw();
        if (Health <= 0)
        {
            IsDead = true; SetPhysicsProcess(false); Hide(); PublishLost(); Died?.Invoke(); QueueFree(); return;
        }
        _flash?.Kill(); Modulate = Colors.White * 1.8f;
        _flash = CreateTween(); _flash.TweenProperty(this, "modulate", Colors.White, .15);
    }

    // Vector TD's aqua chassis silhouette with a compact health arc; no imported weapon/upgrade systems.
    public override void _Draw()
    {
        float r = Radius;
        DrawColoredPolygon(new Vector2[] { new(r, 0), new(r * .35f, -r), new(-r * .8f, -r * .65f),
            new(-r * .8f, r * .65f), new(r * .35f, r) }, new Color("#69cddc"));
        DrawLine(new(-r * .3f, 0), new(r * .6f, 0), new Color("#122330"), 3);
        DrawArc(Vector2.Zero, r + 4, -.5f * Mathf.Pi, -.5f * Mathf.Pi + Mathf.Tau * (_initialized ? Health / MaxHealth : 1), 24, new Color("#69e4a3"), 2);
    }
    private void PublishLost() { if (_lost) return; _lost = true; TargetLost?.Invoke(); }
    // Losing focus cannot leave the chassis drifting on a stale held-key state.
    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusOut || what == NotificationApplicationFocusOut)
        { _velocity = Vector2.Zero; SetPhysicsProcess(false); }
    }
    public override void _ExitTree() { PublishLost(); _flash?.Kill(); InputBlocked = null; MovementKeyBlocked = null; }
}
