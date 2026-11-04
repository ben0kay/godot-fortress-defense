// Event-driven grid/route visualization for the foundation sandbox, not a gameplay renderer.
using Godot;

public partial class GridDebugView : Node2D
{
    private WorldGrid _grid;
    private GroundNavigation _navigation;
    private Vector2[] _path = System.Array.Empty<Vector2>();
    private Vector2I _start, _goal, _hover;
    private Vector2I _previewSize = new(2, 3);

    public void Initialize(WorldGrid grid, GroundNavigation navigation)
    { _grid = grid; _navigation = navigation; }

    // Sandbox sends changes; no mouse polling or per-frame full-map redraw.
    public void ShowState(Vector2[] path, Vector2I start, Vector2I goal, Vector2I hover)
    { _path = path; _start = start; _goal = goal; _hover = hover; QueueRedraw(); }

    // One CanvasItem draws the debug map; terrain collision is owned by WorldGrid.
    public override void _Draw()
    {
        if (_grid == null) return;
        float size = _grid.CellSize;
        Vector2 origin = ToLocal(_grid.ToGlobal(Vector2.Zero));
        DrawRect(new Rect2(origin, (Vector2)_grid.Dimensions * size), new Color(.035f, .05f, .07f));
        for (int y = 0; y < _grid.Dimensions.Y; y++)
            for (int x = 0; x < _grid.Dimensions.X; x++)
            {
                Vector2I cell = new(x, y);
                Rect2 rect = new(origin + (Vector2)cell * size, Vector2.One * size);
                if (_grid.IsTerrain(cell)) DrawRect(rect, new Color(.35f, .38f, .43f));
                else if (!_grid.IsBlocked(cell) && !_navigation.IsWalkable(cell))
                    DrawRect(rect, new Color(.19f, .12f, .15f));
                DrawRect(rect, new Color(.13f, .18f, .23f), false, 1);
            }
        if (_path.Length > 1)
        {
            var points = new Vector2[_path.Length];
            for (int i = 0; i < points.Length; i++) points[i] = ToLocal(_path[i]);
            DrawPolyline(points, Colors.LimeGreen, 3, true);
        }
        DrawCircle(ToLocal(_grid.CellCenter(_start)), size * .18f, Colors.LimeGreen);
        DrawCircle(ToLocal(_grid.CellCenter(_goal)), size * .18f, Colors.Orange);
        Rect2I preview = new(_hover, _previewSize);
        DrawRect(new Rect2(origin + (Vector2)_hover * size, (Vector2)_previewSize * size),
            _grid.CanOccupy(preview) ? Colors.Cyan : Colors.IndianRed, false, 2);
    }
}
