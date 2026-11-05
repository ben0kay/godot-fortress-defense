// Static battlefield presentation with optional navigation overlay; redraw only after edits/input.
using Godot;

public partial class GridDebugView : Node2D
{
    public bool ShowNavigation { get; set; }
    private WorldGrid _grid;
    private GroundNavigation _navigation;
    private Vector2[] _path = System.Array.Empty<Vector2>();
    private Vector2I _start, _goal, _hover, _previewSize;
    public void Initialize(WorldGrid grid, GroundNavigation navigation, Vector2I previewSize)
    { _grid = grid; _navigation = navigation; _previewSize = previewSize; }
    public void ShowState(Vector2[] path, Vector2I start, Vector2I goal, Vector2I hover)
    { _path = path; _start = start; _goal = goal; _hover = hover; QueueRedraw(); }

    // A quiet dark field and solid cover are always visible; debug cell lines are opt-in.
    public override void _Draw()
    {
        if (_grid == null) return;
        float size = _grid.CellSize; Vector2 origin = ToLocal(_grid.ToGlobal(Vector2.Zero));
        Rect2 bounds = new(origin, (Vector2)_grid.Dimensions * size);
        DrawRect(bounds, new Color(.025f, .045f, .065f)); DrawRect(bounds, new Color(.15f, .28f, .35f), false, 2);
        for (int y = 0; y < _grid.Dimensions.Y; y++)
            for (int x = 0; x < _grid.Dimensions.X; x++)
            {
                Vector2I cell = new(x, y); Rect2 rect = new(origin + (Vector2)cell * size, Vector2.One * size);
                if (_grid.IsTerrain(cell))
                { DrawRect(rect.Grow(-1), new Color(.16f, .23f, .27f)); DrawRect(rect.Grow(-4), new Color(.25f, .32f, .34f), false, 1); }
                else if (ShowNavigation && !_grid.IsBlocked(cell) && !_navigation.IsWalkable(cell))
                    DrawRect(rect, new Color(.19f, .12f, .15f));
                if (ShowNavigation) DrawRect(rect, new Color(.12f, .18f, .23f), false, 1);
            }
        if (ShowNavigation)
        {
            if (_path.Length > 1)
            {
                var points = new Vector2[_path.Length];
                for (int i = 0; i < points.Length; i++) points[i] = ToLocal(_path[i]);
                DrawPolyline(points, Colors.LimeGreen, 2, true);
            }
            DrawCircle(ToLocal(_grid.CellCenter(_start)), size * .18f, Colors.LimeGreen);
            DrawCircle(ToLocal(_grid.CellCenter(_goal)), size * .18f, Colors.Orange);
        }
        if (_grid.Contains(_hover))
            DrawRect(new Rect2(origin + (Vector2)_hover * size, (Vector2)_previewSize * size),
                _grid.CanOccupy(new Rect2I(_hover, _previewSize)) ? new Color(.2f, .75f, .9f, .55f) : new Color(.9f, .2f, .2f, .55f), false, 1);
    }
}
