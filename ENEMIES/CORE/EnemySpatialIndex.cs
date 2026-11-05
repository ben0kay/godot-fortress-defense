// Shared live-enemy buckets for combat queries; no duplicate gameplay state or per-frame scene scans.
using Godot;
using System.Collections.Generic;

public sealed class EnemySpatialIndex
{
    private const float BucketSize = 128;
    private readonly Dictionary<Vector2I, HashSet<Enemy>> _buckets = new();
    private readonly Dictionary<Enemy, Vector2I> _locations = new();
    public float MaxRadius { get; private set; }
    private static Vector2I Cell(Vector2 position) => new(Mathf.FloorToInt(position.X / BucketSize), Mathf.FloorToInt(position.Y / BucketSize));

    // Same-bucket movement exits immediately; only crossing a bucket boundary mutates the index.
    public void Update(Enemy enemy)
    {
        Vector2I cell = Cell(enemy.GlobalPosition);
        if (_locations.TryGetValue(enemy, out var old))
        { if (old == cell) return; RemoveBucket(enemy, old); }
        if (!_buckets.TryGetValue(cell, out var bucket)) _buckets.Add(cell, bucket = new());
        bucket.Add(enemy); _locations[enemy] = cell; MaxRadius = Mathf.Max(MaxRadius, enemy.Radius);
    }
    public void Remove(Enemy enemy)
    { if (_locations.Remove(enemy, out var old)) RemoveBucket(enemy, old); }
    private void RemoveBucket(Enemy enemy, Vector2I cell)
    { var bucket = _buckets[cell]; bucket.Remove(enemy); if (bucket.Count == 0) _buckets.Remove(cell); }

    // Callers own/reuse their scratch list; each enemy appears in exactly one bucket.
    public void Query(Rect2 bounds, List<Enemy> result)
    {
        result.Clear(); Vector2I start = Cell(bounds.Position), end = Cell(bounds.End);
        for (int y = start.Y; y <= end.Y; y++) for (int x = start.X; x <= end.X; x++)
            if (_buckets.TryGetValue(new(x, y), out var bucket))
                foreach (Enemy enemy in bucket) result.Add(enemy);
    }
    // Radius is a conservative world-lifetime upper bound; reset only when the registry becomes empty.
    public void Clear() { _locations.Clear(); _buckets.Clear(); MaxRadius = 0; }
}
