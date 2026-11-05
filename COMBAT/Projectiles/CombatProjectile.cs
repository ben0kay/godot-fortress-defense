// Straight amber tracer with immutable launch stats, spatial swept enemy contact and solid obstruction ordering.
using Godot;
using System.Collections.Generic;

public partial class CombatProjectile : Node2D
{
    private WorldGrid _grid;
    private EnemyRegistry _enemies;
    private Building _source;
    private ICombatTarget _intended;
    private EnemyTargetLayers _targetLayers;
    private ProjectileCollisionProfile _collision;
    private Vector2 _direction;
    private float _damage, _speed, _remainingDistance, _radius, _length;
    private bool _spent, _hostile;
    private readonly List<Enemy> _candidates = new();

    // No original target reference: missed rounds continue straight and may hit a different enemy.
    public void Initialize(WorldGrid grid, EnemyRegistry enemies, Building source, Vector2 direction,
        float damage, float speed, float distance, float radius, float length,
        EnemyTargetLayers targetLayers = EnemyTargetLayers.Ground, ProjectileCollisionProfile collision = ProjectileCollisionProfile.GroundObstructed)
    {
        if (!CombatLayers.Valid(targetLayers) || !System.Enum.IsDefined(collision) || collision == ProjectileCollisionProfile.IntendedGroundTarget)
            throw new System.ArgumentException("Invalid enemy-target projectile profile.");
        _targetLayers = targetLayers; _collision = collision;
        _grid = grid; _enemies = enemies; _source = source; _direction = direction.Normalized();
        _damage = damage; _speed = speed; _remainingDistance = distance; _radius = radius; _length = length;
        Rotation = _direction.Angle();
    }

    // Ground hostile shots use first-solid hits; explicit overflight shots retain only their intended ground target.
    public void InitializeHostile(WorldGrid grid, Vector2 direction, float damage, float speed, float lifetime, float radius, float length,
        ProjectileCollisionProfile collision = ProjectileCollisionProfile.GroundObstructed, ICombatTarget intended = null)
    {
        if (!System.Enum.IsDefined(collision) || collision == ProjectileCollisionProfile.Airborne
            || (collision == ProjectileCollisionProfile.IntendedGroundTarget && !CombatTarget.IsAlive(intended)))
            throw new System.ArgumentException("Invalid building-target projectile profile.");
        _collision = collision;
        if (collision == ProjectileCollisionProfile.IntendedGroundTarget)
        { _intended = intended; _intended.TargetLost += OnIntendedLost; }
        _grid = grid; _hostile = true; _direction = direction.Normalized(); _damage = damage;
        _speed = speed; _remainingDistance = speed * lifetime; _radius = radius; _length = length;
        Rotation = _direction.Angle();
    }

    // Swept entry prevents fast bullets tunnelling through enemies; nearer solids win over enemy contact.
    public override void _PhysicsProcess(double delta)
    {
        if (_spent || !GodotObject.IsInstanceValid(_grid) || (!_hostile && !GodotObject.IsInstanceValid(_enemies))) { QueueFree(); return; }
        Vector2 from = GlobalPosition; float travel = Mathf.Min(_speed * (float)delta, _remainingDistance);
        Vector2 end = from + _direction * travel;
        Building ignored = GodotObject.IsInstanceValid(_source) ? _source : null;
        Building buildingHit = null;
        float obstruction = _collision == ProjectileCollisionProfile.GroundObstructed
            ? _grid.FirstObstacleFraction(from, end, _radius, out buildingHit, ignored)
            : _grid.FirstWorldBoundaryFraction(from, end, _radius);
        float first = obstruction;
        if (_hostile)
        {
            if (_collision == ProjectileCollisionProfile.IntendedGroundTarget && CombatTarget.IsAlive(_intended))
            {
                float entry = CombatGeometry.SegmentRectEntry(from, end, _intended.TargetBounds(_grid).Grow(_radius));
                if (float.IsFinite(entry) && entry < obstruction)
                {
                    ICombatTarget hitTarget = _intended; _spent = true; OnIntendedLost();
                    GlobalPosition = from.Lerp(end, entry); QueueFree(); hitTarget.ApplyDamage(_damage); return;
                }
            }
            if (float.IsFinite(obstruction))
            {
                _spent = true; GlobalPosition = from.Lerp(end, obstruction); QueueFree();
                if (CombatTarget.IsAlive(buildingHit)) buildingHit.ApplyDamage(_damage);
                return;
            }
            GlobalPosition = end; _remainingDistance -= travel;
            if (_remainingDistance <= 0) { _spent = true; QueueFree(); }
            return;
        }
        Enemy hit = null;
        Rect2 bounds = new(new Vector2(Mathf.Min(from.X, end.X), Mathf.Min(from.Y, end.Y)),
            new Vector2(Mathf.Abs(end.X - from.X), Mathf.Abs(end.Y - from.Y)));
        _enemies.Query(bounds.Grow(_enemies.MaxEnemyRadius + _radius), _candidates);
        foreach (Enemy enemy in _candidates)
        {
            if (!GodotObject.IsInstanceValid(enemy) || enemy.IsDead || !CombatLayers.Allows(_targetLayers, enemy)) continue;
            float entry = CircleEntry(from, end, enemy.GlobalPosition, enemy.Radius + _radius);
            if (entry < first) { first = entry; hit = enemy; }
        }
        if (hit != null)
        { _spent = true; GlobalPosition = from.Lerp(end, first); QueueFree(); hit.ApplyDamage(_damage); return; }
        if (float.IsFinite(obstruction)) { _spent = true; QueueFree(); return; }
        GlobalPosition = end; _remainingDistance -= travel;
        if (_remainingDistance <= 0) { _spent = true; QueueFree(); }
    }

    // Earliest line-segment/circle intersection, including a launch already touching an enemy.
    private static float CircleEntry(Vector2 from, Vector2 end, Vector2 center, float radius)
    {
        Vector2 offset = from - center, segment = end - from;
        float c = offset.LengthSquared() - radius * radius;
        if (c <= 0) return 0;
        float a = segment.LengthSquared(); if (a <= .000001f) return float.PositiveInfinity;
        float b = offset.Dot(segment), discriminant = b * b - a * c;
        if (discriminant < 0) return float.PositiveInfinity;
        float t = (-b - Mathf.Sqrt(discriminant)) / a;
        return t >= 0 && t <= 1 ? t : float.PositiveInfinity;
    }
    public override void _Draw()
    {
        DrawLine(new(-_length, 0), Vector2.Zero, new Color(1f, .55f, .1f, .4f), 4, true);
        DrawLine(new(-_length, 0), Vector2.Zero, (_hostile ? new Color(1, .36f, .12f) : new Color(1f, .9f, .45f)), 1.5f, true);
    }
    // Target-only shots never acquire replacements; a lost target becomes a harmless miss until expiry.
    private void OnIntendedLost()
    {
        if (_intended != null && GodotObject.IsInstanceValid(_intended.TargetNode)) _intended.TargetLost -= OnIntendedLost;
        _intended = null;
    }
    public override void _ExitTree() { OnIntendedLost(); _spent = true; _source = null; _grid = null; _enemies = null; _candidates.Clear(); }
}
