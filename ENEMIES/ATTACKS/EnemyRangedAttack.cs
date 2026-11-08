// Lightweight reusable ranged execution; Enemy owns target choice, navigation and health, COMBAT owns bullets.
using Godot;

public sealed class EnemyRangedAttack
{
    private readonly float _speed, _lifetime, _radius, _length, _aimTolerance;
    private float _remaining;
    private readonly ProjectileCollisionProfile _collision;
    public float ProjectileRadius => _radius;
    public EnemyRangedAttack(EnemyRangedConfig settings)
    {
        _collision = settings.CollisionProfile; _speed = settings.ProjectileSpeed; _lifetime = settings.ProjectileLifetime; _radius = settings.ProjectileRadius;
        _length = settings.TracerLength; _aimTolerance = Mathf.DegToRad(settings.AimToleranceDegrees);
    }
    public void Tick(double delta) => _remaining = Mathf.Max(0, _remaining - (float)delta);

    // A positive-radius sight segment and exact footprint range must both be satisfied before stopping/firing.
    public bool CanAttack(Enemy enemy, WorldGrid grid, ICombatTarget target) =>
        EnemyTargeting.CanAttackFrom(grid, enemy.GlobalPosition, target, enemy.AttackRange, _radius, _collision != ProjectileCollisionProfile.GroundObstructed);
    public bool TryAttack(Enemy enemy, WorldGrid grid, Node2D projectiles, ICombatTarget target)
    {
        if (_remaining > 0 || !GodotObject.IsInstanceValid(projectiles) || !CanAttack(enemy, grid, target)) return false;
        Rect2 bounds = target.TargetBounds(grid);
        Vector2 aim = enemy.GlobalPosition.Clamp(bounds.Position, bounds.End), direction = aim - enemy.GlobalPosition;
        if (direction.LengthSquared() < .0001f || Mathf.Abs(Mathf.AngleDifference(enemy.Rotation, direction.Angle())) > _aimTolerance) return false;
        Vector2 muzzle = enemy.GlobalPosition + Vector2.Right.Rotated(enemy.Rotation) * (enemy.Radius * 1.7f);
        direction = aim - muzzle;
        if (direction.LengthSquared() < .0001f || (_collision == ProjectileCollisionProfile.GroundObstructed && !grid.SegmentClear(muzzle, aim, _radius, target as Building))) return false;
        var bullet = new CombatProjectile();
        bullet.InitializeHostile(grid, direction, enemy.AttackHit, _speed, _lifetime, _radius, _length, _collision, target);
        projectiles.AddChild(bullet); bullet.GlobalPosition = muzzle;
        _remaining = enemy.AttackCooldown; return true;
    }
}
