// Geometric contact targeting only; strategic Citadel identity is assigned once at spawn.
using Godot;

public static class EnemyTargeting
{
    public static bool Live(Building target) => GodotObject.IsInstanceValid(target) && target.IsPlaced && !target.IsDestroyed;

    // Attack range is center-to-footprint distance; another wall/terrain blocks contact line of sight.
    public static bool CanAttackFrom(WorldGrid grid, Vector2 position, Building target, float range)
    {
        if (!Live(target)) return false;
        Rect2 rect = grid.FootprintRect(target.OccupiedArea);
        Vector2 closest = position.Clamp(rect.Position, rect.End);
        return position.DistanceSquaredTo(closest) <= range * range
            && grid.SegmentClear(position, closest, 0, target);
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
