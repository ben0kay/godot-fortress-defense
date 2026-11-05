// One passive terrain CanvasItem; logical cells/collision stay in WorldGrid, overlays remain optional.
using Godot;

public partial class TerrainRenderer : Node2D
{
    [Export] public TerrainDefinition GroundDefinition { get; set; }
    [Export] public TerrainDefinition DeadDefinition { get; set; }
    public bool ShowNavigation { get; set; }
    private WorldGrid _grid;
    private GroundNavigation _navigation;
    private Vector2[] _path = System.Array.Empty<Vector2>();
    private Vector2I _start, _goal, _hover, _previewSize;

    // One renderer works in production levels and the existing debug sandbox.
    public void Initialize(WorldGrid grid, GroundNavigation navigation, Vector2I previewSize)
    {
        _grid = grid; _navigation = navigation; _previewSize = previewSize;
        if ((GroundDefinition != null && GroundDefinition.Type != TerrainType.Ground)
            || (DeadDefinition != null && DeadDefinition.Type != TerrainType.DeadCell))
            throw new System.InvalidOperationException("Terrain appearance definitions do not match their logical types.");
    }
    public void ShowState(Vector2[] path, Vector2I start, Vector2I goal, Vector2I hover)
    { _path = path; _start = start; _goal = goal; _hover = hover; QueueRedraw(); }

    // Merged rectangles share exact coverage with the passive physical shapes; no per-cell nodes.
    public override void _Draw()
    {
        if (_grid == null) return;
        float size = _grid.CellSize; Vector2 origin = ToLocal(_grid.ToGlobal(Vector2.Zero));
        Rect2 bounds = new(origin, (Vector2)_grid.Dimensions * size);
        DrawRect(bounds, GroundDefinition?.Tint ?? new Color(.025f, .045f, .065f));
        foreach (Rect2I solid in _grid.TerrainRectangles)
            DrawRect(new Rect2(origin + (Vector2)solid.Position * size, (Vector2)solid.Size * size), DeadDefinition?.Tint ?? new Color(.16f, .23f, .27f));
        DrawRect(bounds, new Color(.15f, .28f, .35f), false, 2);
        if (ShowNavigation)
        {
            for (int y = 0; y < _grid.Dimensions.Y; y++) for (int x = 0; x < _grid.Dimensions.X; x++)
            {
                Vector2I cell = new(x, y); Rect2 rect = new(origin + (Vector2)cell * size, Vector2.One * size);
                if (!_grid.IsBlocked(cell) && !_navigation.IsWalkable(cell)) DrawRect(rect, new Color(.19f, .12f, .15f));
                DrawRect(rect, new Color(.12f, .18f, .23f), false, 1);
            }
            if (_path.Length > 1)
            {
                var points = new Vector2[_path.Length];
                for (int i = 0; i < points.Length; i++) points[i] = ToLocal(_path[i]);
                DrawPolyline(points, Colors.LimeGreen, 2, true);
            }
            DrawCircle(ToLocal(_grid.CellCenter(_start)), size * .18f, Colors.LimeGreen);
            DrawCircle(ToLocal(_grid.CellCenter(_goal)), size * .18f, Colors.Orange);
        }
        if (_grid.Contains(_hover)) DrawRect(new Rect2(origin + (Vector2)_hover * size, (Vector2)_previewSize * size),
            _grid.CanOccupy(new Rect2I(_hover, _previewSize)) ? new Color(.2f, .75f, .9f, .55f) : new Color(.9f, .2f, .2f, .55f), false, 1);
    }
}
