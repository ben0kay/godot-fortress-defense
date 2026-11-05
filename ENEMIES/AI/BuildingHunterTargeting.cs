// Reusable exact footprint-distance building selection; global acquisition is called only by the bounded roster.
using Godot;
using System.Collections.Generic;

public static class BuildingHunterTargeting
{
    public static float DistanceSquared(Vector2 position, Building building)
    {
        Rect2 rect = building.WorldFootprint;
        return position.DistanceSquaredTo(position.Clamp(rect.Position, rect.End));
    }
    public static Building Nearest(Vector2 position, IEnumerable<Building> candidates, float maximumDistanceSquared = float.PositiveInfinity)
    {
        Building closest = null; float best = maximumDistanceSquared;
        foreach (Building building in candidates)
        {
            if (!EnemyTargeting.Live(building)) continue;
            float distance = DistanceSquared(position, building);
            if (distance > best) continue;
            if (distance == best && closest != null && building.GetInstanceId() >= closest.GetInstanceId()) continue;
            closest = building; best = distance;
        }
        return closest;
    }

    // Spatial broadphase is followed by exact circle/footprint filtering, using a caller-owned list.
    public static Building Local(BuildingRegistry buildings, Enemy enemy, List<Building> scratch)
    {
        float radius = enemy.HunterDetectionRadius;
        buildings.Query(new Rect2(enemy.GlobalPosition - Vector2.One * radius, Vector2.One * radius * 2), scratch);
        return Nearest(enemy.GlobalPosition, scratch, radius * radius);
    }
}
