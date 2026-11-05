// Shared opt-in projectile tower: stationary base, rotating turret, layer-aware staggered acquisition and canonical building lifecycle.
using Godot;
using System.Collections.Generic;

public partial class ProjectileTower : Building, IBuildingCombat
{
    [Export] public ProjectileTowerConfig CombatConfig { get; set; }
    private ProjectileTowerConfig _stats;
    private FogOfWar _fog;
    // Sight defaults to the placed weapon snapshot; the building override permits scouts/artillery later.
    public override float? VisionRadius => base.VisionRadius ?? _stats?.RangePixels ?? CombatConfig?.RangePixels;
    private Sprite2D _baseSprite, _gun;
    private Node2D _visuals, _turret;
    private Marker2D _muzzle;
    private Polygon2D _flash;
    private WorldGrid _grid;
    private EnemyRegistry _enemies;
    private Node2D _projectiles;
    private Enemy _target;
    private readonly List<Enemy> _candidates = new();
    private readonly RandomNumberGenerator _random = new();
    private float _searchTimer, _shotTimer, _flashTimer;
    private float Phase => (GetInstanceId() % 97) / 97f;

    #region Lifecycle and dependencies
    // The inherited base owns health/collision/occupancy; only this optional weapon adds physics work.
    public override void _Ready()
    {
        base._Ready();
        _visuals = GetNode<Node2D>("Visuals"); _baseSprite = GetNode<Sprite2D>("Visuals/BaseSprite");
        _turret = GetNode<Node2D>("Visuals/TurretPivot"); _gun = GetNode<Sprite2D>("Visuals/TurretPivot/GunSprite");
        _muzzle = GetNode<Marker2D>("Visuals/TurretPivot/Muzzle"); _flash = GetNode<Polygon2D>("Visuals/TurretPivot/Muzzle/Flash");
        CombatConfig ??= new ProjectileTowerConfig(); CombatConfig.Validate();
        _stats = (ProjectileTowerConfig)CombatConfig.Duplicate(); _random.Seed = GetInstanceId();
        PresentationChanged += OnPresentation;
        Placed += OnPlaced; Detached += OnDetached; SelectionChanged += OnSelection; HealthChanged += QueueRedraw;
    }
    private void OnPlaced()
    {
        float scale = Mathf.Min(FootprintPixels.X, FootprintPixels.Y) / 64;
        _visuals.Scale = Vector2.One * scale; QueueRedraw();
    }

    // Placement composition supplies world-scoped services; no scene-path assumptions inside tower logic.
    public void BindCombat(WorldGrid grid, EnemyRegistry enemies, Node2D projectiles, FogOfWar fog)
    {
        if (_enemies != null) throw new System.InvalidOperationException("Projectile tower combat is already bound.");
        if (!GodotObject.IsInstanceValid(fog) || fog.Grid != grid)
            throw new System.InvalidOperationException("Tower combat requires this world's initialized fog visibility.");
        _fog = fog; _grid = grid; _enemies = enemies; _projectiles = projectiles;
        _enemies.Changed += OnEnemiesChanged; _searchTimer = Phase * Mathf.Max(.05f, _stats.SearchInterval);
        OnEnemiesChanged();
    }
    private void OnEnemiesChanged()
    {
        bool awake = _stats.TargetLayers != EnemyTargetLayers.None && IsPlaced && !IsDestroyed && GodotObject.IsInstanceValid(_enemies) && _enemies.Count > 0;
        if (!awake) { _target = null; _candidates.Clear(); _shotTimer = _flashTimer = 0; _flash.Visible = false; }
        SetPhysicsProcess(awake);
    }
    private void OnDetached()
    {
        if (IsDestroyed && IsInsideTree())
        {
            var pulse = new DestructionPulse { Radius = FootprintPixels.Length() * .5f, Tint = Colors.Cyan };
            GetTree().CurrentScene.AddChild(pulse); pulse.GlobalPosition = GlobalPosition;
        }
        Unbind();
    }
    private void Unbind()
    {
        if (GodotObject.IsInstanceValid(_enemies)) _enemies.Changed -= OnEnemiesChanged;
        _fog = null; _enemies = null; _grid = null; _projectiles = null; _target = null; _candidates.Clear();
        SetPhysicsProcess(false); if (GodotObject.IsInstanceValid(_flash)) _flash.Visible = false;
    }
    #endregion

    // Keep aim/fire responsive, but acquire only at staggered intervals; no full-scene target search.
    public override void _PhysicsProcess(double delta)
    {
        if (!IsPlaced || IsDestroyed || !GodotObject.IsInstanceValid(_projectiles)) return;
        _flashTimer = Mathf.Max(0, _flashTimer - (float)delta); _flash.Visible = HasCurrentVision && _flashTimer > 0;
        _searchTimer -= (float)delta; _shotTimer = Mathf.Max(-.05f, _shotTimer - (float)delta);
        if (_searchTimer <= 0)
        {
            _target = CombatTargeting.Closest(_enemies, _grid, this, _stats.RangePixels, _candidates, _fog, _stats.TargetLayers);
            _searchTimer = Mathf.Max(.05f, _stats.SearchInterval);
        }
        // Check the cached reference before Track: hidden targets must not leak their current aim position.
        if (!CombatTargeting.Valid(_target, GlobalPosition, _stats.RangePixels, _fog, _stats.TargetLayers))
        { _target = null; return; }
        bool aligned = CombatTargeting.Track(_turret, _target.GlobalPosition, _stats.RotationSpeedDegrees, _stats.AimToleranceDegrees, delta);
        if (!aligned || _shotTimer > 0 || _stats.ShotsPerSecond == 0) return;
        ProjectileCollisionProfile collision = CombatLayers.ShotProfile(_target);
        if (!CombatTargeting.ClearShot(_grid, _muzzle.GlobalPosition, _target.GlobalPosition, _stats.ProjectileRadius, this, collision))
        { _target = null; return; }
        float spread = Mathf.DegToRad(_random.RandfRange(-_stats.SpreadDegrees, _stats.SpreadDegrees));
        Vector2 direction = Vector2.Up.Rotated(_turret.GlobalRotation + spread);
        var bullet = new CombatProjectile();
        bullet.Initialize(_grid, _enemies, this, direction, _stats.Damage, _stats.ProjectileSpeed,
            _stats.RangePixels + _grid.CellSize, _stats.ProjectileRadius, _stats.TracerLength, CombatLayers.Of(_target), collision);
        _projectiles.AddChild(bullet); bullet.GlobalPosition = _muzzle.GlobalPosition;
        _shotTimer = Mathf.Max(0, 1 / _stats.ShotsPerSecond + _shotTimer);
        _flashTimer = .045f; _flash.Visible = HasCurrentVision;
    }

    // Selection and health redraw on changes only; base platform never rotates with the turret.
    private void OnPresentation()
    {
        _gun.Visible = HasCurrentVision; _flash.Visible = HasCurrentVision && _flashTimer > 0;
        OnSelection();
    }
    private void OnSelection()
    {
        Color tint = IsSelected && HasCurrentVision ? new Color(1, .9f, .68f) : Colors.White;
        _baseSprite.Modulate = tint; _gun.Modulate = tint; QueueRedraw();
    }
    public override void _Draw()
    {
        if (!IsPlaced || _stats == null || !HasCurrentVision) return;
        if (IsSelected)
        {
            DrawArc(Vector2.Zero, _stats.RangePixels, 0, Mathf.Tau, 96, new Color(.3f, .8f, 1, .4f), 1, true);
            DrawRect(new Rect2(-FootprintPixels * .5f, FootprintPixels).Grow(2), Colors.Yellow, false, 1);
        }
        Rect2 bar = new(new(-FootprintPixels.X * .5f, -FootprintPixels.Y * .5f - 7), new(FootprintPixels.X, 4));
        DrawRect(bar, new Color(.12f, .15f, .2f)); bar.Size = new(FootprintPixels.X * Health / MaxHealth, 4); DrawRect(bar, Colors.LimeGreen);
    }
    public override void _ExitTree()
    {
        Unbind(); PresentationChanged -= OnPresentation; Placed -= OnPlaced; Detached -= OnDetached; SelectionChanged -= OnSelection; HealthChanged -= QueueRedraw;
        base._ExitTree(); _stats?.Dispose(); _random.Dispose();
    }
}
