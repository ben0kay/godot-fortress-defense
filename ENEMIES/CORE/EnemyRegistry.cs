// Per-battlefield enemy lifetime lookup and click-time overlap checks, without scene-tree queries.
using Godot;
using System;
using System.Collections.Generic;

public partial class EnemyRegistry : Node
{
    private readonly HashSet<Enemy> _enemies = new();
    private readonly EnemySpatialIndex _spatial = new();
    public float MaxEnemyRadius => _spatial.MaxRadius;
    public void Query(Rect2 bounds, List<Enemy> result) => _spatial.Query(bounds, result);
    internal void UpdatePosition(Enemy enemy) { if (_enemies.Contains(enemy)) _spatial.Update(enemy); }
    public IEnumerable<Enemy> Enemies => _enemies;
    public int Count => _enemies.Count;
    public event Action Changed;
    internal void Add(Enemy enemy) { if (_enemies.Add(enemy)) { _spatial.Update(enemy); Changed?.Invoke(); } }
    internal void Remove(Enemy enemy) { if (_enemies.Remove(enemy)) { _spatial.Remove(enemy); if (_enemies.Count == 0) _spatial.Clear(); Changed?.Invoke(); } }

    // Only run on a placement click, not per enemy per frame. Prevent an enemy being sealed inside a new solid.
    public bool CanPlace(WorldGrid grid, Rect2I area)
    {
        Rect2 rect = grid.FootprintRect(area);
        foreach (Enemy enemy in _enemies)
            if (!enemy.IsDead && (rect.Grow(enemy.Radius + .05f).HasPoint(enemy.GlobalPosition)
                || area.Grow(grid.ClearanceCells).HasPoint(grid.WorldToCell(enemy.GlobalPosition)))) return false;
        return true;
    }
    public bool SpawnSpaceFree(Vector2 position, float radius)
    {
        foreach (Enemy enemy in _enemies)
            if (!enemy.IsDead && enemy.GlobalPosition.DistanceSquaredTo(position) < Mathf.Pow(enemy.Radius + radius + 2, 2)) return false;
        return true;
    }

    // Snapshot only on explicit manual clear; each enemy removes its own queue subscriptions.
    public void ClearEnemies()
    {
        var snapshot = new Enemy[_enemies.Count]; _enemies.CopyTo(snapshot);
        foreach (Enemy enemy in snapshot) enemy.Despawn();
    }
    public override void _ExitTree() { _enemies.Clear(); _spatial.Clear(); }
}
