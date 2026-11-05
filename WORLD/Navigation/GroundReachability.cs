// Reusable cooperative flood workspace to prove reachability before choosing a solid-target approach.
using Godot;

public sealed class GroundReachability
{
    public static readonly Vector2I[] CardinalDirections = { Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left };
    private readonly WorldGrid _grid;
    private readonly GroundNavigation _navigation;
    private readonly int[] _marks;
    private readonly Vector2I[] _frontier;
    private int _stamp, _head, _tail;
    public bool Complete => _head >= _tail;

    public GroundReachability(WorldGrid grid, GroundNavigation navigation)
    {
        _grid = grid; _navigation = navigation;
        _marks = new int[grid.Dimensions.X * grid.Dimensions.Y]; _frontier = new Vector2I[_marks.Length];
    }

    // Stamp reuse avoids allocating/clearing a map for each path request.
    public void Begin(Vector2I origin)
    {
        if (_stamp == int.MaxValue) { System.Array.Clear(_marks); _stamp = 0; }
        _stamp++; _head = _tail = 0;
        if (_navigation.IsWalkable(origin)) Add(origin);
    }

    // Cardinal connectivity matches diagonals that require both side cells clear.
    // Caller bounds visits per physics frame; this method never searches the entire map at once.
    public bool TryVisit(out Vector2I cell)
    {
        cell = default;
        if (Complete) return false;
        cell = _frontier[_head++];
        foreach (Vector2I direction in CardinalDirections)
        {
            Vector2I next = cell + direction;
            if (!_grid.Contains(next) || _marks[Index(next)] == _stamp || !_navigation.IsWalkable(next)) continue;
            Add(next);
        }
        return true;
    }
    private int Index(Vector2I cell) => cell.Y * _grid.Dimensions.X + cell.X;
    private void Add(Vector2I cell) { _marks[Index(cell)] = _stamp; _frontier[_tail++] = cell; }
}
