// Shared battlefield composition/input: WG0 levels and DEBUG reuse the same grid/buildings/enemy services.
using Godot;

public partial class LevelController : Node2D
{
    [Export] public LevelConfig Config { get; set; }
    [Export] public PackedScene CitadelScene { get; set; }
    [Export] public PackedScene WallScene { get; set; }
    [Export] public PackedScene TowerScene { get; set; }
    private Node2D _projectiles;
    public string SelectedBuildingId { get; private set; }
    public bool FreeConstruction => Config == null || Config.FreeDebugPlacement;
    public event System.Action PlacementSelectionChanged;
    private BuildMenu _buildMenu;
    private bool _buildMenuOpen, _placementAffordable;
    public ResourceWallet Resources { get; private set; }
    public BuildingPlacement Placement => _placement;
    private string _creditDiagnostic = "Free DEBUG placement";
    public EnemyRegistry DebugEnemies => _enemies;
    public EdgeEnemySpawner DebugSpawner => _spawner;
    private WorldGrid _grid;
    private GroundNavigation _navigation;
    private BuildingPlacement _placement;
    private EnemyRegistry _enemies;
    private EdgeEnemySpawner _spawner;
    private TerrainRenderer _view;
    private Label _status, _worldDetails;
    private Control _worldPanel;
    private Button _newWorld, _sameWorld;
    private Camera2D _camera;
    private Timer _statusTimer;
    private Building _citadel, _selected;
    private Vector2I _start, _goal, _hover, _lastPaint = new(-1, -1);
    private Vector2[] _path = System.Array.Empty<Vector2>();
    private bool _overlay, _painting, _panning, _closing, _reloading;
    private string _notice = "Defend the CPU — place walls to redirect attackers";
    private long _seed;
    private WorldGenerator.Result _generated;
    // One-shot reload handoff is scene-scoped and consumed on entry; authored .tres files never change.
    private static string _pendingScene;
    private static long? _pendingSeed;

    // Parent enters before child Ready: configure grid dimensions before its arrays are allocated.
    public override void _EnterTree()
    {
        if (Config == null) return;
        if (Config.Grid == null || Config.Generation == null || Config.EnemyStats == null)
            throw new System.InvalidOperationException("Level config requires grid, generation and enemy settings.");
        GetNode<WorldGrid>("RootWorld/WorldGrid").Config = Config.Grid;
        GetNode<EdgeEnemySpawner>("RootEnemies/EdgeEnemySpawner").DefinitionOverride = Config.EnemyStats;
        _seed = _pendingScene == SceneFilePath && _pendingSeed.HasValue ? _pendingSeed.Value : Config.Generation.Seed;
        if (_pendingScene == SceneFilePath) { _pendingScene = null; _pendingSeed = null; }
    }

    #region World composition
    // Terrain is committed before A* initialization and enemy creation, avoiding thousands of live invalidations.
    public override void _Ready()
    {
        _grid = GetNode<WorldGrid>("RootWorld/WorldGrid"); _navigation = GetNode<GroundNavigation>("RootWorld/GroundNavigation");
        _placement = GetNode<BuildingPlacement>("RootFortress/BuildingPlacement");
        _enemies = GetNode<EnemyRegistry>("RootEnemies/EnemyRegistry"); _spawner = GetNode<EdgeEnemySpawner>("RootEnemies/EdgeEnemySpawner");
        var requests = GetNode<EnemyPathRequests>("RootEnemies/EnemyPathRequests");
        _view = GetNodeOrNull<TerrainRenderer>("RootWorld/TerrainRenderer") ?? GetNode<TerrainRenderer>("RootWorld/GridDebugView"); _status = GetNode<Label>("RootUI/FoundationPanel/Status");
        _camera = GetNode<Camera2D>("RootCamera/Camera2D"); _statusTimer = GetNode<Timer>("RootUI/StatusTimer");
        Vector2I cpuSize = SceneFootprint(CitadelScene), map = _grid.Dimensions;
        Rect2I cpuArea = new((map - cpuSize) / 2, cpuSize);
        if (Config != null)
        {
            _generated = WorldGenerator.Generate(_grid, cpuSize, Config.Generation, _seed); cpuArea = _generated.CpuFootprint;
            if (!_grid.TryApplyTerrain(_generated.Solid)) throw new System.InvalidOperationException("Generated terrain could not be committed.");
        }
        else PopulateSandboxTerrain(_grid);
        _navigation.Initialize(_grid); requests.Initialize(_grid, _navigation);
        _placement.Initialize(_grid, GetNode<BuildingRegistry>("RootFortress/BuildingRegistry"), GetNode<Node2D>("RootFortress/Buildings"));
        _placement.AdditionalValidation = area => _enemies.CanPlace(_grid, area);
        Resources = GetNode<ResourceWallet>("RootFortress/Resources");
        Resources.Initialize(Config?.StartingCredits ?? 0);
        var catalogue = Config?.Catalogue ?? GD.Load<BuildingCatalogue>("res://FORTRESS/BUILDINGS/SHARED/BuildingCatalogue/DefaultBuildingCatalogue.tres");
        _placement.ConfigurePurchases(catalogue, Resources, Config?.AllowedBuildingIds ?? new[] { "wall001", "tower001" });
        _placement.CatalogueChanged += OnPlacementAvailabilityChanged;
        Resources.BalanceChanged += OnBalanceChanged; OnBalanceChanged();
        _projectiles = GetNode<Node2D>("RootCombat/Projectiles"); _placement.Placed += OnBuildingPlaced;
        _view.Initialize(_grid, _navigation);
        if (!_placement.TryPlaceFree(CitadelScene, cpuArea.Position, out _citadel))
            throw new System.InvalidOperationException("Citadel does not fit the level clearing.");
        if (_generated != null)
        {
            Rect2I markerArea = new(_generated.PlayerCell, Vector2I.One);
            if (!_grid.TryReserveGround(markerArea)) throw new System.InvalidOperationException("Player spawn could not be reserved.");
            var marker = GetNode<Marker2D>("RootWorld/PlayerSpawn"); marker.GlobalPosition = _grid.CellCenter(_generated.PlayerCell);
            _spawner.Seed = unchecked((ulong)_seed);
            _spawner.SpawnCellFilter = cell => _generated.ReachableFromCpu[cell.Y * map.X + cell.X];
        }
        _spawner.Initialize(_grid, _navigation, _enemies, requests, GetNode<Node2D>("RootEnemies/Enemies"), _citadel);
        _start = new(_grid.ClearanceCells, map.Y / 2);
        _goal = new(cpuArea.Position.X - _grid.ClearanceCells - 1, map.Y / 2);
        _grid.TopologyChanged += OnTopologyChanged;
        _statusTimer.Timeout += RefreshDisplay; GetViewport().SizeChanged += FitCamera;
        _buildMenu = GetNode<BuildMenu>("RootUI/BuildMenu"); _buildMenu.Bind(this);
        SetProcess(false);
        if (Config == null) RequestPlacement("wall001");
        WireWorldPanel(); FitCamera(); UpdateWorldView(); RefreshDisplay();
    }

    // DEBUG supplies its fixed obstacles through this hook, not a duplicate gameplay controller.
    protected virtual void PopulateSandboxTerrain(WorldGrid grid) { }
    private static Vector2I SceneFootprint(PackedScene scene)
    {
        Building prototype = scene.Instantiate<Building>();
        try { return prototype.Definition.Footprint; } finally { prototype.Free(); }
    }
    private void WireWorldPanel()
    {
        _worldPanel = GetNodeOrNull<Control>("RootUI/FoundationPanel/WorldDebug");
        if (_worldPanel == null) return;
        _worldDetails = _worldPanel.GetNode<Label>("Details");
        _newWorld = _worldPanel.GetNode<Button>("Actions/NewWorld"); _sameWorld = _worldPanel.GetNode<Button>("Actions/SameSeed");
        _newWorld.Pressed += RegenerateNew; _sameWorld.Pressed += RegenerateSame;
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
    #endregion

    #region Presentation and input
    // Four-Hz HP/count readout; static terrain redraw is separate and never runs from this timer.
    private void RefreshDisplay()
    {
        if (_closing) return;
        bool alive = EnemyTargeting.Live(_citadel);
        string cpu = alive ? $"CPU {_citadel.Health:0}/{_citadel.MaxHealth:0}" : "CPU DESTROYED — R to restart";
        string selected = EnemyTargeting.Live(_selected) ? $" | Selected {_selected.BuildingId} {_selected.Health:0}/{_selected.MaxHealth:0} HP" : "";
        _status.Text = $"{cpu} | {_creditDiagnostic} | Enemies {_enemies.Count} | Spawning {(_spawner.SpawningEnabled && alive ? "ON" : "OFF")}{selected}\n{_notice}";
        if (_generated != null)
        {
            _status.Text += $" | Seed {_generated.Seed} | Dead terrain {_generated.Coverage:P1} | F1: world details";
            if (_worldDetails != null) _worldDetails.Text = $"{Config.LevelName} — {_grid.Dimensions.X}×{_grid.Dimensions.Y}, {_grid.CellSize:0}px cells\n"
                + $"Seed {_generated.Seed}; derived {_generated.DerivedSeed}; attempts {_generated.Attempts}; accepted 1 / rejected {_generated.RejectedAttempts}; repaired {_generated.Repaired}\n"
                + $"Dead coverage {_generated.Coverage:P1}; merged terrain shapes {_grid.TerrainCollisionShapeCount}; player cell {_generated.PlayerCell}\n"
                + "Regeneration reloads the entire encounter. Same seed + settings reproduces the initial terrain.";
        }
    }
    // Wallet notifications update the cached diagnostic; the existing HP timer never queries balances.
    private void OnBalanceChanged()
    {
        _creditDiagnostic = $"Credits {Resources.GetBalance(ResourceCost.ResourceId.Credits)}"
            + (Config == null || Config.FreeDebugPlacement ? " | FREE DEBUG placement" : "");
        OnPlacementAvailabilityChanged();
        if (_citadel != null) RefreshDisplay();
    }
    private void UpdateWorldView() { _view.ShowNavigation = _overlay; _view.ShowState(_path, _start, _goal); }
    private void Select(Building building)
    {
        if (_selected == building) return;
        if (GodotObject.IsInstanceValid(_selected)) _selected.SetSelected(false);
        _selected = building; _selected?.SetSelected(true);
    }
    // Optional combat capabilities are bound at composition, never through hard-coded paths in buildings.
    private void OnBuildingPlaced(Building building)
    { if (building is IBuildingCombat combat) combat.BindCombat(_grid, _enemies, _projectiles); }
    // Wheel/middle-drag inspect the finite WG0 world; DEBUG retains its existing terrain-edit controls.
    public override void _UnhandledInput(InputEvent input)
    {
        if (_reloading) return;
        _hover = _grid.WorldToCell(GetGlobalMousePosition());
        if (input is InputEventMouseButton mouse)
        {
            if (_buildMenuOpen || GetViewport().GuiGetHoveredControl() != null) return;
            if (Config != null && mouse.Pressed && (mouse.ButtonIndex == MouseButton.WheelUp || mouse.ButtonIndex == MouseButton.WheelDown))
            {
                float old = _camera.Zoom.X, zoom = Mathf.Clamp(old * (mouse.ButtonIndex == MouseButton.WheelUp ? 1.15f : 1 / 1.15f), .1f, 2);
                Vector2 offset = GetViewport().GetMousePosition() - GetViewportRect().Size * .5f;
                _camera.GlobalPosition += offset * (1 / old - 1 / zoom); _camera.Zoom = Vector2.One * zoom;
            }
            if (Config != null && mouse.ButtonIndex == MouseButton.Middle) _panning = mouse.Pressed;
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                _painting = mouse.Pressed;
                if (mouse.Pressed)
                {
                    _lastPaint = new(-1, -1); Building building = _grid.BuildingAt(_hover);
                    if (SelectedBuildingId != null) PaintBuilding();
                    else { Select(building); _painting = false; }
                }
            }
            if (mouse.Pressed && mouse.ButtonIndex == MouseButton.Right)
            { Building building = _grid.BuildingAt(_hover); if (building != _citadel) building?.Remove(); }
            if (Config == null && _overlay && mouse.Pressed && mouse.ButtonIndex == MouseButton.Middle && _grid.Contains(_hover))
            {
                Rect2I area = new(_hover, Vector2I.One);
                if (_grid.IsTerrain(_hover) || _enemies.CanPlace(_grid, area)) _grid.TrySetTerrain(area, !_grid.IsTerrain(_hover));
            }
        }
        if (input is InputEventMouseMotion motion)
        {
            _painting &= Input.IsMouseButtonPressed(MouseButton.Left); _panning &= Input.IsMouseButtonPressed(MouseButton.Middle);
            if (_buildMenuOpen || GetViewport().GuiGetHoveredControl() != null) { _painting = false; _panning = false; }
            else if (_panning) _camera.GlobalPosition -= motion.Relative / _camera.Zoom;
            else if (_painting) PaintBuilding();
        }
        if (input is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.F1 && _worldPanel != null) _worldPanel.Visible = !_worldPanel.Visible;
            if (!_buildMenuOpen && key.Keycode == Key.T) RequestPlacement("tower001");
            if (!_buildMenuOpen && key.Keycode == Key.W) RequestPlacement("wall001");
            if (key.Keycode == Key.B) { _overlay = !_overlay; _path = _overlay ? _navigation.FindPath(_start, _goal) : System.Array.Empty<Vector2>(); }
            if (_overlay && key.Keycode == Key.S && _grid.Contains(_hover)) { _start = _hover; _path = _navigation.FindPath(_start, _goal); }
            if (_overlay && key.Keycode == Key.G && _grid.Contains(_hover)) { _goal = _hover; _path = _navigation.FindPath(_start, _goal); }
            if (key.Keycode == Key.H) _selected?.ApplyDamage(25);
            if (key.Keycode == Key.Delete && _selected != _citadel) _selected?.Remove();
            if (key.Keycode == Key.P) _spawner.SpawningEnabled = !_spawner.SpawningEnabled;
            if (key.Keycode == Key.C) { _spawner.SpawningEnabled = false; _spawner.CancelDebugSpawns(); _enemies.ClearEnemies(); _notice = "Enemies cleared; P resumes spawning"; }
            if (key.Keycode == Key.K)
            {
                Enemy hovered = null;
                foreach (Enemy enemy in _enemies.Enemies)
                    if (enemy.GlobalPosition.DistanceSquaredTo(GetGlobalMousePosition()) <= Mathf.Pow(enemy.Radius * 2, 2)) { hovered = enemy; break; }
                hovered?.ApplyDamage(hovered.MaxHealth);
            }
            if (key.Keycode == Key.R) { if (Config != null) RegenerateSame(); else GetTree().ReloadCurrentScene(); return; }
        }
        UpdateWorldView(); RefreshDisplay();
    }
    #endregion

    // Entire-scene reload ensures all world-scoped services/queues/collision subscriptions reset together.
    private void RegenerateNew()
    {
        long next = Config.Generation.RandomizeSeedOnRegeneration ? System.Random.Shared.NextInt64() : unchecked(_seed + 1);
        ReloadWithSeed(next == _seed ? unchecked(_seed + 1) : next);
    }
    private void RegenerateSame() => ReloadWithSeed(_seed);
    private void ReloadWithSeed(long seed)
    {
        if (_reloading || Config == null) return;
        _reloading = true; _pendingScene = SceneFilePath; _pendingSeed = seed;
        Callable.From(() =>
        {
            Error error = GetTree().ReloadCurrentScene();
            if (error != Error.Ok) { _pendingScene = null; _pendingSeed = null; _reloading = false; GD.PushError("Scene reload failed: " + error); }
        }).CallDeferred();
    }
    public override void _ExitTree()
    {
        _closing = true; Select(null);
        if (GodotObject.IsInstanceValid(Resources)) Resources.BalanceChanged -= OnBalanceChanged;
        if (GodotObject.IsInstanceValid(_placement))
        { _placement.Placed -= OnBuildingPlaced; _placement.CatalogueChanged -= OnPlacementAvailabilityChanged; }
        if (GodotObject.IsInstanceValid(_grid)) _grid.TopologyChanged -= OnTopologyChanged;
        if (GodotObject.IsInstanceValid(_statusTimer)) _statusTimer.Timeout -= RefreshDisplay;
        if (GodotObject.IsInstanceValid(_newWorld)) _newWorld.Pressed -= RegenerateNew;
        if (GodotObject.IsInstanceValid(_sameWorld)) _sameWorld.Pressed -= RegenerateSame;
        GetViewport().SizeChanged -= FitCamera;
    }
}
