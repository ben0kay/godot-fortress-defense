// Gradual bounded edge spawning; fixed target references, deterministic local RNG and explicit cleanup.
using Godot;

public partial class EdgeEnemySpawner : Node
{
    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public EnemyDefinition DefinitionOverride { get; set; }
    public EnemyTargetRoster TargetRoster { get; set; }
    public System.Func<Vector2, float, bool> AdditionalSpawnValidation { get; set; }
    public System.Func<Vector2I, bool> SpawnCellFilter { get; set; }
    private bool _spawningEnabled = true;
    [Export] public bool SpawningEnabled
    { get => _spawningEnabled; set { _spawningEnabled = value; RefreshProcessing(); } }
    [Export] public float SpawnInterval { get; set; } = .8f;
    private int _maxActiveEnemies = 20;
    [Export] public int MaxActiveEnemies
    { get => _maxActiveEnemies; set { _maxActiveEnemies = value; RefreshProcessing(); } }
    [Export] public int MaxDebugSpawnsPerFrame { get; set; } = 2;
    public int PendingDebugSpawns { get; private set; }
    public bool CanSpawn => _grid != null && EnemyTargeting.Live(_citadel);
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
            _definition = DefinitionOverride ?? enemy.Definition; _definition.Validate(grid.CellSize);
            if (_definition.AttackRange < (grid.ClearanceCells + .5f) * grid.CellSize)
                throw new System.InvalidOperationException("Enemy attack range cannot reach a legal approach on this grid.");
        }
        finally { prototype.Free(); }
        _random.Seed = Seed;
        _citadel.Detached += OnCitadelLost; _registry.Changed += RefreshProcessing; RefreshProcessing();
    }

    // Manual requests share the same placement/cap executor as natural spawning; no instant scene bursts.
    public int QueueDebugSpawns(int count)
    {
        if (!CanSpawn) return 0;
        int accepted = Mathf.Clamp(count, 0, 200 - _registry.Count - PendingDebugSpawns);
        PendingDebugSpawns += accepted;
        MaxActiveEnemies = Mathf.Max(MaxActiveEnemies, _registry.Count + PendingDebugSpawns);
        RefreshProcessing(); return accepted;
    }
    public void CancelDebugSpawns() { PendingDebugSpawns = 0; RefreshProcessing(); }

    // A manual batch is bounded per frame. Natural spawning retains its interval with no catch-up burst.
    public override void _Process(double delta)
    {
        if (!CanSpawn || _registry.Count >= Mathf.Clamp(MaxActiveEnemies, 0, 200)) return;
        if (PendingDebugSpawns > 0)
        {
            for (int i = 0; i < Mathf.Clamp(MaxDebugSpawnsPerFrame, 1, 8) && PendingDebugSpawns > 0
                && _registry.Count < Mathf.Clamp(MaxActiveEnemies, 0, 200); i++)
            { PendingDebugSpawns--; if (!TrySpawnOne()) { PendingDebugSpawns++; break; } }
            RefreshProcessing(); return;
        }
        if (!SpawningEnabled) return;
        _elapsed += delta;
        float interval = float.IsFinite(SpawnInterval) ? Mathf.Max(.05f, SpawnInterval) : .8f;
        if (_elapsed < interval) return;
        _elapsed = 0; TrySpawnOne();
    }

    // One bounded edge search; a blocked batch remains pending until space opens or it is cancelled.
    private bool TrySpawnOne()
    {
        int inset = _grid.ClearanceCells;
        int width = _grid.Dimensions.X - inset * 2, height = _grid.Dimensions.Y - inset * 2;
        if (width < 1 || height < 1) return false;
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
            if ((SpawnCellFilter != null && !SpawnCellFilter(cell)) || !_navigation.IsWalkable(cell) || !_grid.SegmentClear(position, position, _definition.Radius)
                || position.DistanceSquaredTo(_citadel.GlobalPosition) < Mathf.Pow(Mathf.Max(0, MinCitadelDistance), 2)
                || !_registry.SpawnSpaceFree(position, _definition.Radius)
                || !(AdditionalSpawnValidation?.Invoke(position, _definition.Radius) ?? true)) continue;
            Enemy enemy = EnemyScene.Instantiate<Enemy>(); enemy.Definition = _definition;
            enemy.Initialize(_grid, _registry, _requests, _citadel, TargetRoster);
            enemy.Position = _container.ToLocal(position); _container.AddChild(enemy);
            return true;
        }
        return false;
    }
    // Disabled/full spawners sleep; registry changes or Inspector setters wake them.
    private void RefreshProcessing()
    {
        if (_grid == null || !IsInsideTree()) return;
        SetProcess((SpawningEnabled || PendingDebugSpawns > 0) && EnemyTargeting.Live(_citadel) && _registry.Count < Mathf.Clamp(MaxActiveEnemies, 0, 200));
    }
    private void OnCitadelLost() { PendingDebugSpawns = 0; RefreshProcessing(); }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_citadel)) _citadel.Detached -= OnCitadelLost;
        if (GodotObject.IsInstanceValid(_registry)) _registry.Changed -= RefreshProcessing;
        PendingDebugSpawns = 0; SpawnCellFilter = null; AdditionalSpawnValidation = null; TargetRoster = null; _random.Dispose();
    }
}
