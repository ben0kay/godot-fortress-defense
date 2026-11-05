// One main-thread ground A* view; updates only the edited rectangle plus clearance halo.
using Godot;
using System.Collections.Generic;

public partial class GroundNavigation : Node
{
    private WorldGrid _grid;
    private AStarGrid2D _astar;
    public ulong Revision => _grid?.Revision ?? 0;

    // One shared profile, no per-agent AStar objects or automatic queries.
    public void Initialize(WorldGrid grid)
    {
        if (_grid != null) throw new System.InvalidOperationException("Navigation already initialized.");
        _grid = grid;
        _astar = new AStarGrid2D
        {
            Region = grid.Bounds, CellSize = Vector2.One * grid.CellSize,
            Offset = Vector2.One * grid.CellSize * .5f,
            DiagonalMode = grid.AllowDiagonals ? AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles : AStarGrid2D.DiagonalModeEnum.Never,
            DefaultComputeHeuristic = grid.AllowDiagonals ? AStarGrid2D.Heuristic.Octile : AStarGrid2D.Heuristic.Manhattan,
            DefaultEstimateHeuristic = grid.AllowDiagonals ? AStarGrid2D.Heuristic.Octile : AStarGrid2D.Heuristic.Manhattan
        };
        _astar.Update(); Refresh(grid.Bounds); grid.TopologyChanged += Refresh;
    }

    // Conservative square clearance; out-of-map cells count as solid.
    public bool IsWalkable(Vector2I cell)
    {
        if (!_grid.Contains(cell)) return false;
        int radius = _grid.ClearanceCells;
        for (int y = cell.Y - radius; y <= cell.Y + radius; y++)
            for (int x = cell.X - radius; x <= cell.X + radius; x++)
                if (_grid.IsBlocked(new(x, y))) return false;
        return true;
    }

    // SetPointSolid preserves existing point data; Update would erase solids.
    private void Refresh(Rect2I changed)
    {
        Rect2I area = changed.Grow(_grid.ClearanceCells).Intersection(_grid.Bounds);
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++)
            { Vector2I cell = new(x, y); _astar.SetPointSolid(cell, !IsWalkable(cell)); }
    }

    // Shared synchronous primitive; ST1 gameplay calls this through the bounded enemy request queue.
    public Vector2[] FindPath(Vector2I start, Vector2I goal)
    {
        if (_astar == null || !IsWalkable(start) || !IsWalkable(goal)) return System.Array.Empty<Vector2>();
        Vector2[] points = _astar.GetPointPath(start, goal);
        for (int i = 0; i < points.Length; i++) points[i] = _grid.ToGlobal(points[i]);
        return points;
    }

    // Return legal cardinal approach cells outside a blocked target; caller schedules reachability searches.
    public List<Vector2I> GetApproachCells(Rect2I footprint)
    {
        var result = new List<Vector2I>();
        if (!_grid.Fits(footprint)) return result;
        int gap = _grid.ClearanceCells + 1;
        for (int x = footprint.Position.X; x < footprint.End.X; x++)
        { Add(new(x, footprint.Position.Y - gap)); Add(new(x, footprint.End.Y - 1 + gap)); }
        for (int y = footprint.Position.Y; y < footprint.End.Y; y++)
        { Add(new(footprint.Position.X - gap, y)); Add(new(footprint.End.X - 1 + gap, y)); }
        return result;
        void Add(Vector2I cell) { if (IsWalkable(cell)) result.Add(cell); }
    }

    // Detach world subscriptions and release the native reference on scene exit.
    public override void _ExitTree()
    {
        if (_grid != null) _grid.TopologyChanged -= Refresh;
        _astar?.Dispose(); _astar = null; _grid = null;
    }
}
