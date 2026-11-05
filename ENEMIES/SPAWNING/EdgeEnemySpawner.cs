// Gradual bounded edge spawning; fixed target references, deterministic local RNG and explicit cleanup.
using Godot;

public partial class EdgeEnemySpawner : Node
{
    [Export] public PackedScene EnemyScene { get; set; }
    private bool _spawningEnabled = true;
    [Export] public bool SpawningEnabled
    { get => _spawningEnabled; set { _spawningEnabled = value; RefreshProcessing(); } }
    [Export] public float SpawnInterval { get; set; } = .8f;
    private int _maxActiveEnemies = 20;
    [Export] public int MaxActiveEnemies
    { get => _maxActiveEnemies; set { _maxActiveEnemies = value; RefreshProcessing(); } }
    [Export] public int MaxSpawnAttempts { get; set; } = 32;
    [Export] public float MinCitadelDistance { get; set; } = 160;
    [Export] public ulong Seed { get; set; } = 1;
    private WorldGrid _grid;
    private GroundNavigation _navigation;
    private EnemyRegistry _registry;
    private EnemyPathRequests _requests;
    private Node2D _container;
    private Building _citadel;
    private EnemyDefinition _definition;
    private readonly RandomNumberGenerator _random = new();
    private double _elapsed;

    public override void _Ready() => SetProcess(false);

    // Inspect the scene definition once; validation does not enter the scene tree.
    public void Initialize(WorldGrid grid, GroundNavigation navigation, EnemyRegistry registry, EnemyPathRequests requests, Node2D container, Building citadel)
    {
        _grid = grid; _navigation = navigation; _registry = registry; _requests = requests; _container = container; _citadel = citadel;
        Node prototype = EnemyScene.Instantiate();
        try
        {
            if (prototype is not Enemy enemy || enemy.Definition == null) throw new System.InvalidOperationException("Spawner requires an Enemy scene/definition.");
            _definition = enemy.Definition; _definition.Validate(grid.CellSize);
        }
        finally { prototype.Free(); }
        _random.Seed = Seed;
        _citadel.Detached += OnCitadelLost; _registry.Changed += RefreshProcessing; RefreshProcessing();
    }

    // No catch-up loop: at most one creation per interval/frame, even following a hitch.
    public override void _Process(double delta)
    {
        if (_grid == null || !SpawningEnabled || !EnemyTargeting.Live(_citadel) || _registry.Count >= Mathf.Clamp(MaxActiveEnemies, 0, 200)) return;
        _elapsed += delta;
        float interval = float.IsFinite(SpawnInterval) ? Mathf.Max(.05f, SpawnInterval) : .8f;
        if (_elapsed < interval) return;
        _elapsed = 0;
        int inset = _grid.ClearanceCells;
        int width = _grid.Dimensions.X - inset * 2, height = _grid.Dimensions.Y - inset * 2;
        if (width < 1 || height < 1) return;
        int perimeter = width * 2 + height * 2;
        int first = _random.RandiRange(0, perimeter - 1);
        for (int i = 0; i < Mathf.Clamp(MaxSpawnAttempts, 1, perimeter); i++)
        {
            int edge = (first + i) % perimeter;
            Vector2I cell = edge < width ? new(inset + edge, inset)
                : edge < width + height ? new(inset + width - 1, inset + edge - width)
                : edge < width * 2 + height ? new(inset + edge - width - height, inset + height - 1)
                : new(inset, inset + edge - width * 2 - height);
            Vector2 position = _grid.CellCenter(cell);
            if (!_navigation.IsWalkable(cell) || !_grid.SegmentClear(position, position, _definition.Radius)
                || position.DistanceSquaredTo(_citadel.GlobalPosition) < Mathf.Pow(Mathf.Max(0, MinCitadelDistance), 2)
                || !_registry.SpawnSpaceFree(position, _definition.Radius)) continue;
            Enemy enemy = EnemyScene.Instantiate<Enemy>();
            enemy.Initialize(_grid, _registry, _requests, _citadel);
            enemy.Position = _container.ToLocal(position); _container.AddChild(enemy);
            return;
        }
    }
    // Disabled/full spawners sleep; registry changes or Inspector setters wake them.
    private void RefreshProcessing()
    {
        if (_grid == null || !IsInsideTree()) return;
        SetProcess(SpawningEnabled && EnemyTargeting.Live(_citadel) && _registry.Count < Mathf.Clamp(MaxActiveEnemies, 0, 200));
    }
    private void OnCitadelLost() => RefreshProcessing();
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_citadel)) _citadel.Detached -= OnCitadelLost;
        if (GodotObject.IsInstanceValid(_registry)) _registry.Changed -= RefreshProcessing;
        _random.Dispose();
    }
}
