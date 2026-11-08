// Movement-independent, once-only intended-target contact damage/despawn, reusable by future ground impact units.
using Godot;

public sealed class EnemyImpactAttack
{
    private bool _resolved;

    // Sweep the whole step so a large delta cannot jump through the intended footprint.
    public bool TryResolve(Enemy enemy, WorldGrid grid, ICombatTarget target, Vector2 from, Vector2 to)
    {
        if (_resolved || enemy.IsDead || !CombatTarget.IsAlive(target)) return false;
        float entry = CombatGeometry.SegmentRectEntry(from, to, target.TargetBounds(grid).Grow(enemy.Radius));
        if (!float.IsFinite(entry)) return false;
        _resolved = true; enemy.SetMovementPosition(from.Lerp(to, entry));
        try { target.ApplyDamage(enemy.AttackHit); }
        finally { enemy.Despawn(); }
        return true;
    }
}
