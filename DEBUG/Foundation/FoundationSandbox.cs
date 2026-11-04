// Composes the BF0/ST0 manual sandbox; gameplay owners remain reusable outside DEBUG.
using Godot;

public partial class FoundationSandbox : Node2D
{
    [Export] public PackedScene TestBuildingScene { get; set; }
    [Export] public bool InitialObstacles { get; set; } = true;
    private WorldGrid _grid;
    private GroundNavigation _navigation;
    private BuildingPlacement _placement;
    private BuildingRegistry _registry;
    private GridDebugView _view;
    private Label _status;
    private Camera2D _camera;
    private Building _selected;
    private Vector2I _start, _goal, _hover;
    private Vector2[] _path = System.Array.Empty<Vector2>();
    private string _notice = "Ready";

    // Explicit local composition and deterministic obstacles; no procedural generator yet.
    public override void _Ready()
    {
        _grid = GetNode<WorldGrid>("WorldGrid"); _navigation = GetNode<GroundNavigation>("GroundNavigation");
        _placement = GetNode<BuildingPlacement>("BuildingPlacement"); _registry = GetNode<BuildingRegistry>("BuildingRegistry");
        _view = GetNode<GridDebugView>("GridDebugView"); _status = GetNode<Label>("FoundationPanel/Status");
        _camera = GetNode<Camera2D>("Camera2D");
        _navigation.Initialize(_grid); _placement.Initialize(_grid, _registry, GetNode<Node2D>("Buildings"));
        _view.Initialize(_grid, _navigation);
        Vector2I map = _grid.Dimensions;
        _start = new(1, map.Y / 2); _goal = new(map.X - 2, map.Y / 2);
        if (InitialObstacles && map.X >= 12 && map.Y >= 12)
        {
            int x = map.X / 3, gap = map.Y / 2;
            _grid.TrySetTerrain(new Rect2I(x, 2, 1, gap - 3), true);
            _grid.TrySetTerrain(new Rect2I(x, gap + 2, 1, map.Y - gap - 4), true);
        }
        _placement.TryPlace(TestBuildingScene, new(map.X / 2, map.Y / 2 - 1), out _);
        _grid.TopologyChanged += OnTopologyChanged;
        GetViewport().SizeChanged += FitCamera;
        FitCamera(); RefreshRoute();
    }

    // Fit once and on window resize; keeps configurable finite maps inspectable.
    private void FitCamera()
    {
        Vector2 viewport = GetViewportRect().Size;
        Vector2 map = (Vector2)_grid.Dimensions * _grid.CellSize;
        float zoom = Mathf.Max(.05f, Mathf.Min((viewport.X - 64) / map.X, (viewport.Y - 150) / map.Y));
        _camera.Zoom = Vector2.One * zoom;
        _camera.GlobalPosition = _grid.ToGlobal(map * .5f) - new Vector2(0, 45 / zoom);
    }

    // One demonstration route per explicit edit; not an enemy scheduler or mass-repath loop.
    private void OnTopologyChanged(Rect2I changed)
    {
        if (_selected != null && !_selected.IsPlaced) Select(null);
        RefreshRoute();
    }
    private void RefreshRoute() { _path = _navigation.FindPath(_start, _goal); RefreshDisplay(); }

    // UI and drawing refresh only on user input or topology/health notifications.
    private void RefreshDisplay()
    {
        _view.ShowState(_path, _start, _goal, _hover);
        string selection = _selected == null ? "none" : $"{_selected.BuildingId}: {_selected.Health:0}/{_selected.MaxHealth:0} HP";
        _status.Text = $"BF0 + ST0 | Revision {_grid.Revision} | Buildings {_registry.Count} | Route: {(_path.Length == 0 ? "UNREACHABLE" : _path.Length + " points")}\nSelected: {selection} | {_notice}";
    }

    // Select by authoritative occupied cell, including every part of a multi-cell structure.
    private void Select(Building building)
    {
        if (_selected == building) return;
        if (GodotObject.IsInstanceValid(_selected)) { _selected.HealthChanged -= RefreshDisplay; _selected.SetSelected(false); }
        _selected = building;
        if (_selected != null) { _selected.SetSelected(true); _selected.HealthChanged += RefreshDisplay; }
    }

    // Input-driven foundation controls; no build menu, combat or stress spawner in this phase.
    public override void _UnhandledInput(InputEvent input)
    {
        _hover = _grid.WorldToCell(GetGlobalMousePosition());
        if (input is InputEventMouseButton mouse && mouse.Pressed)
        {
            Building building = _grid.BuildingAt(_hover);
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                if (building != null) { Select(building); _notice = "Selected"; }
                else
                {
                    bool placed = _placement.TryPlace(TestBuildingScene, _hover, out var added);
                    if (placed) Select(added);
                    _notice = placed ? "Placed 2x3 footprint" : "Placement rejected (bounds, terrain or overlap)";
                }
            }
            else if (mouse.ButtonIndex == MouseButton.Right)
            { building?.Remove(); _notice = "Remove at cursor"; }
            else if (mouse.ButtonIndex == MouseButton.Middle && _grid.Contains(_hover))
                _notice = _grid.TrySetTerrain(new Rect2I(_hover, Vector2I.One), !_grid.IsTerrain(_hover))
                    ? "Terrain toggled" : "Terrain edit rejected (building occupied)";
        }
        if (input is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.S && _grid.Contains(_hover)) { _start = _hover; RefreshRoute(); }
            if (key.Keycode == Key.G && _grid.Contains(_hover)) { _goal = _hover; RefreshRoute(); }
            if (key.Keycode == Key.H) _selected?.ApplyDamage(25);
            if (key.Keycode == Key.Delete) _selected?.Remove();
        }
        RefreshDisplay();
    }

    // Detach before scene children leave; no callbacks into a closing debug scene.
    public override void _ExitTree()
    {
        Select(null);
        if (GodotObject.IsInstanceValid(_grid)) _grid.TopologyChanged -= OnTopologyChanged;
        GetViewport().SizeChanged -= FitCamera;
    }
}
