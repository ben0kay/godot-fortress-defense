// Geometric contact targeting only; strategic Citadel identity is assigned once at spawn.
using Godot;

public static class EnemyTargeting
{
    public static bool Live(ICombatTarget target) => CombatTarget.IsAlive(target);

    // Attack range is center-to-footprint distance; another wall/terrain blocks contact line of sight.
    public static bool CanAttackFrom(WorldGrid grid, Vector2 position, ICombatTarget target, float range, float projectileRadius = 0, bool ignoreGround = false)
    {
        if (!Live(target)) return false;
        Rect2 rect = target.TargetBounds(grid);
        Vector2 closest = position.Clamp(rect.Position, rect.End);
        return position.DistanceSquaredTo(closest) <= range * range
            && (ignoreGround || grid.SegmentClear(position, closest, projectileRadius, target as Building));
    }

    // Only explicit cached candidates are considered; target choice never owns movement or attack execution.
    public static ICombatTarget ChooseObjective(Enemy enemy, ICombatTarget mobile)
    {
        if (enemy.TargetRule == EnemyDefinition.TargetPriority.CitadelOnly || !Live(mobile)) return enemy.StrategicTarget;
        float playerDistance = enemy.GlobalPosition.DistanceSquaredTo(mobile.TargetNode.GlobalPosition);
        if (playerDistance > enemy.PlayerDetectionRange * enemy.PlayerDetectionRange) return enemy.StrategicTarget;
        if (enemy.TargetRule == EnemyDefinition.TargetPriority.NearestWithinRange && Live(enemy.StrategicTarget)
            && playerDistance >= enemy.GlobalPosition.DistanceSquaredTo(enemy.StrategicTarget.GlobalPosition)) return enemy.StrategicTarget;
        return mobile;
    }

    // Local boundary lookup, never a registry/world-wide target scan for every enemy.
    public static Building BreachAt(WorldGrid grid, Vector2I cell, Building strategic, float range)
    {
        int gap = grid.ClearanceCells + 1;
        foreach (Vector2I direction in GroundReachability.CardinalDirections)
        {
            Building building = grid.BuildingAt(cell + direction * gap);
            if (building != strategic && Live(building) && CanAttackFrom(grid, grid.CellCenter(cell), building, range)) return building;
        }
        return null;
    }
}
