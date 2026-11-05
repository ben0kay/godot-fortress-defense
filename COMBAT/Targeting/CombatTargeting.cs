// Shared closest-target acquisition and up-facing turret rotation, independent of any tower catalog.
using Godot;
using System.Collections.Generic;

public static class CombatTargeting
{
    public static bool Valid(Enemy enemy, Vector2 origin, float range) => GodotObject.IsInstanceValid(enemy)
        && !enemy.IsDead && enemy.IsInsideTree() && !enemy.IsQueuedForDeletion() && origin.DistanceSquaredTo(enemy.GlobalPosition) <= range * range;

    // Only nearby buckets are inspected, using a caller-owned scratch list and deterministic distance ties.
    public static Enemy Closest(EnemyRegistry registry, WorldGrid grid, Building source, float range, List<Enemy> scratch)
    {
        Vector2 origin = source.GlobalPosition;
        registry.Query(new Rect2(origin - Vector2.One * range, Vector2.One * range * 2), scratch);
        Enemy best = null; float score = range * range;
        foreach (Enemy enemy in scratch)
        {
            if (!Valid(enemy, origin, range)) continue;
            float distance = origin.DistanceSquaredTo(enemy.GlobalPosition);
            if (distance > score || (distance == score && best != null && enemy.GetInstanceId() >= best.GetInstanceId())) continue;
            if (!grid.SegmentClear(origin, enemy.GlobalPosition, 0, source)) continue;
            score = distance; best = enemy;
        }
        return best;
    }

    // The original Battle gun points upward, so the pivot includes a +90-degree forward correction.
    public static bool Track(Node2D pivot, Vector2 target, float speedDegrees, float toleranceDegrees, double delta)
    {
        Vector2 direction = target - pivot.GlobalPosition;
        if (direction.LengthSquared() < .0001f) return true;
        float desired = direction.Angle() + Mathf.Pi * .5f;
        float difference = Mathf.AngleDifference(pivot.GlobalRotation, desired);
        float step = Mathf.DegToRad(speedDegrees) * (float)delta;
        pivot.GlobalRotation += Mathf.Clamp(difference, -step, step);
        return Mathf.Abs(Mathf.AngleDifference(pivot.GlobalRotation, desired)) <= Mathf.DegToRad(toleranceDegrees);
    }
}
