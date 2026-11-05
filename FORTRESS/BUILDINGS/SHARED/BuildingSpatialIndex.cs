// Stationary full-footprint buckets for building queries; BuildingRegistry remains the live authority.
using Godot;
using System.Collections.Generic;

public sealed class BuildingSpatialIndex
{
    private const float BucketSize = 256;
    private readonly Dictionary<Vector2I, HashSet<Building>> _buckets = new();
    private readonly Dictionary<Building, Rect2I> _locations = new();
    private readonly HashSet<Building> _seen = new();
    private static Vector2I Cell(Vector2 point) => new(Mathf.FloorToInt(point.X / BucketSize), Mathf.FloorToInt(point.Y / BucketSize));
    private static Rect2I Buckets(Rect2 rect)
    { Vector2I start = Cell(rect.Position), end = Cell(rect.End); return new(start, end - start + Vector2I.One); }

    // Cover every bucket touched by the rectangular footprint, including large structures.
    public void Add(Building building)
    {
        Rect2I area = Buckets(building.WorldFootprint); _locations.Add(building, area);
        for (int y = area.Position.Y; y < area.End.Y; y++) for (int x = area.Position.X; x < area.End.X; x++)
        {
            Vector2I cell = new(x, y);
            if (!_buckets.TryGetValue(cell, out var bucket)) _buckets.Add(cell, bucket = new());
            bucket.Add(building);
        }
    }
    public void Remove(Building building)
    {
        if (!_locations.Remove(building, out var area)) return;
        for (int y = area.Position.Y; y < area.End.Y; y++) for (int x = area.Position.X; x < area.End.X; x++)
        {
            Vector2I cell = new(x, y); var bucket = _buckets[cell]; bucket.Remove(building);
            if (bucket.Count == 0) _buckets.Remove(cell);
        }
    }

    // Main-thread sequential callers reuse a scratch list; multi-bucket buildings appear once.
    public void Query(Rect2 bounds, List<Building> result)
    {
        result.Clear(); _seen.Clear(); Rect2I area = Buckets(bounds);
        for (int y = area.Position.Y; y < area.End.Y; y++) for (int x = area.Position.X; x < area.End.X; x++)
            if (_buckets.TryGetValue(new(x, y), out var bucket))
                foreach (Building building in bucket) if (_seen.Add(building)) result.Add(building);
        _seen.Clear();
    }
    public void Clear() { _buckets.Clear(); _locations.Clear(); _seen.Clear(); }
}
