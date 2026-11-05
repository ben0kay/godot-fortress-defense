// Composes the BF1/ST1 battlefield and manual controls; gameplay services remain outside DEBUG.
using Godot;

public partial class FoundationSandbox : Node2D
{
    [Export] public PackedScene CitadelScene { get; set; }
    [Export] public PackedScene WallScene { get; set; }
    [Export] public bool InitialObstacles { get; set; } = true;
    private WorldGrid _grid;
    private GroundNavigation _navigation;
    private BuildingPlacement _placement;
    private BuildingRegistry _buildings;
    private EnemyRegistry _enemies;
    private EdgeEnemySpawner _spawner;
    private GridDebugView _view;
    private Label _status;
    private Camera2D _camera;
    private Timer _statusTimer;
    private Building _citadel, _selected;
    private Vector2I _start, _goal, _hover, _wallFootprint, _lastPaint = new(-1, -1);
    private Vector2[] _path = System.Array.Empty<Vector2>();
    private bool _overlay, _painting, _closing;
    private string _notice = "Defend the CPU — place walls to redirect attackers";

    // Build only the initial encounter: deterministic natural cover, central CPU, edge pressure.
    public override void _Ready()
    {
        _grid = GetNode<WorldGrid>("WorldGrid"); _navigation = GetNode<GroundNavigation>("GroundNavigation");
        _placement = GetNode<BuildingPlacement>("BuildingPlacement"); _buildings = GetNode<BuildingRegistry>("BuildingRegistry");
        _enemies = GetNode<EnemyRegistry>("EnemyRegistry"); _spawner = GetNode<EdgeEnemySpawner>("EdgeEnemySpawner");
        var requests = GetNode<EnemyPathRequests>("EnemyPathRequests");
        _view = GetNode<GridDebugView>("GridDebugView"); _status = GetNode<Label>("FoundationPanel/Status");
        _camera = GetNode<Camera2D>("Camera2D"); _statusTimer = GetNode<Timer>("StatusTimer");
        _navigation.Initialize(_grid); requests.Initialize(_grid, _navigation);
        _placement.Initialize(_grid, _buildings, GetNode<Node2D>("Buildings"));
        _placement.AdditionalValidation = area => _enemies.CanPlace(_grid, area);
        _wallFootprint = SceneFootprint(WallScene); _view.Initialize(_grid, _navigation, _wallFootprint);
        Vector2I map = _grid.Dimensions;
        if (InitialObstacles && map.X >= 24 && map.Y >= 18)
        {
            _grid.TrySetTerrain(new Rect2I(map.X / 4, 3, 2, map.Y / 2 - 5), true);
            _grid.TrySetTerrain(new Rect2I(map.X * 3 / 4, map.Y / 2 + 2, 2, map.Y / 2 - 5), true);
            _grid.TrySetTerrain(new Rect2I(map.X / 2 + 3, 4, 4, 2), true);
            _grid.TrySetTerrain(new Rect2I(map.X / 2 - 7, map.Y - 6, 4, 2), true);
        }
        Vector2I cpuSize = SceneFootprint(CitadelScene);
        if (!_placement.TryPlace(CitadelScene, (map - cpuSize) / 2, out _citadel))
        { GD.PushError("Citadel does not fit the configured map/obstacles."); _notice = "Citadel placement failed; check map/footprint"; }
        else _spawner.Initialize(_grid, _navigation, _enemies, requests, GetNode<Node2D>("Enemies"), _citadel);
        _start = new(1, map.Y / 2); _goal = new(Mathf.Max(0, (map.X - cpuSize.X) / 2 - 1), map.Y / 2);
        _grid.TopologyChanged += OnTopologyChanged;
        _statusTimer.Timeout += RefreshDisplay; GetViewport().SizeChanged += FitCamera;
        FitCamera(); UpdateWorldView(); RefreshDisplay();
    }

    // Read shared scene data without entering the tree or activating any gameplay.
    private static Vector2I SceneFootprint(PackedScene scene)
    {
        Building prototype = scene.Instantiate<Building>();
        try { return prototype.Definition.Footprint; } finally { prototype.Free(); }
    }
    private void FitCamera()
    {
        Vector2 viewport = GetViewportRect().Size, map = (Vector2)_grid.Dimensions * _grid.CellSize;
        float zoom = Mathf.Max(.05f, Mathf.Min((viewport.X - 64) / map.X, (viewport.Y - 120) / map.Y));
        _camera.Zoom = Vector2.One * zoom;
        _camera.GlobalPosition = _grid.ToGlobal(map * .5f) - new Vector2(0, 35 / zoom);
    }
    private void OnTopologyChanged(Rect2I changed)
    {
        if (_closing || !GodotObject.IsInstanceValid(_view) || !_view.IsInsideTree()) return;
        if (_selected != null && !_selected.IsPlaced) Select(null);
        if (_overlay) _path = _navigation.FindPath(_start, _goal);
        UpdateWorldView(); RefreshDisplay();
    }

    // A small 4 Hz status readout is ordinary encounter feedback, not ST2 profiling.
    private void RefreshDisplay()
    {
        if (_closing) return;
        bool alive = EnemyTargeting.Live(_citadel);
        string cpu = alive ? $"CPU {_citadel.Health:0}/{_citadel.MaxHealth:0}" : "CPU DESTROYED — R to restart";
        string selected = EnemyTargeting.Live(_selected) ? $" | Selected {_selected.BuildingId} {_selected.Health:0}/{_selected.MaxHealth:0} HP" : "";
        _status.Text = $"{cpu} | Enemies {_enemies.Count} | Spawning {(_spawner.SpawningEnabled && alive ? "ON" : "OFF")}{selected}\n{_notice}";
        if (_overlay) _status.Text += $" | Grid revision {_grid.Revision}; preview route {(_path.Length > 0 ? "reachable" : "unreachable")}";
    }
    private void UpdateWorldView()
    { _view.ShowNavigation = _overlay; _view.ShowState(_path, _start, _goal, _hover); }
    private void Select(Building building)
    {
        if (_selected == building) return;
        if (GodotObject.IsInstanceValid(_selected)) _selected.SetSelected(false);
        _selected = building; _selected?.SetSelected(true);
    }
    private void PaintWall()
    {
        if (_lastPaint == _hover) return;
        _lastPaint = _hover;
        if (_placement.TryPlace(WallScene, _hover, out var wall))
        { Select(wall); _notice = "Wall placed"; }
        else _notice = "Placement blocked by terrain, buildings, enemies or map edge";
    }

    // Left-drag paints solid walls; debug terrain/path tools stay optional behind B.
    public override void _UnhandledInput(InputEvent input)
    {
        _hover = _grid.WorldToCell(GetGlobalMousePosition());
        if (input is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                _painting = mouse.Pressed;
                if (mouse.Pressed)
                {
                    _lastPaint = new(-1, -1); Building building = _grid.BuildingAt(_hover);
                    if (building != null) { Select(building); _painting = false; }
                    else PaintWall();
                }
            }
            if (mouse.Pressed && mouse.ButtonIndex == MouseButton.Right)
            { Building building = _grid.BuildingAt(_hover); if (building != _citadel) building?.Remove(); }
            if (_overlay && mouse.Pressed && mouse.ButtonIndex == MouseButton.Middle && _grid.Contains(_hover))
            {
                Rect2I area = new(_hover, Vector2I.One);
                if (_grid.IsTerrain(_hover) || _enemies.CanPlace(_grid, area)) _grid.TrySetTerrain(area, !_grid.IsTerrain(_hover));
            }
        }
        if (input is InputEventMouseMotion && _painting) PaintWall();
        if (input is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.B) { _overlay = !_overlay; _path = _overlay ? _navigation.FindPath(_start, _goal) : System.Array.Empty<Vector2>(); }
            if (_overlay && key.Keycode == Key.S && _grid.Contains(_hover)) { _start = _hover; _path = _navigation.FindPath(_start, _goal); }
            if (_overlay && key.Keycode == Key.G && _grid.Contains(_hover)) { _goal = _hover; _path = _navigation.FindPath(_start, _goal); }
            if (key.Keycode == Key.H) _selected?.ApplyDamage(25);
            if (key.Keycode == Key.Delete && _selected != _citadel) _selected?.Remove();
            if (key.Keycode == Key.P) _spawner.SpawningEnabled = !_spawner.SpawningEnabled;
            if (key.Keycode == Key.C) { _spawner.SpawningEnabled = false; _enemies.ClearEnemies(); _notice = "Enemies cleared; P resumes spawning"; }
            if (key.Keycode == Key.K)
            {
                Enemy hovered = null;
                foreach (Enemy enemy in _enemies.Enemies)
                    if (enemy.GlobalPosition.DistanceSquaredTo(GetGlobalMousePosition()) <= Mathf.Pow(enemy.Radius * 2, 2))
                    { hovered = enemy; break; }
                hovered?.ApplyDamage(hovered.MaxHealth);
            }
            if (key.Keycode == Key.R) { GetTree().ReloadCurrentScene(); return; }
        }
        UpdateWorldView(); RefreshDisplay();
    }
    public override void _ExitTree()
    {
        _closing = true; Select(null);
        if (GodotObject.IsInstanceValid(_grid)) _grid.TopologyChanged -= OnTopologyChanged;
        if (GodotObject.IsInstanceValid(_statusTimer)) _statusTimer.Timeout -= RefreshDisplay;
        GetViewport().SizeChanged -= FitCamera;
    }
}
