// Gradual bounded edge spawning; fixed target references, deterministic local RNG and explicit cleanup.
using Godot;
using System.Collections.Generic;

public partial class EdgeEnemySpawner : Node
{
    [Export] public PackedScene EnemyScene { get; set; }
    [Export] public Godot.Collections.Array<PackedScene> DebugEnemyScenes { get; set; } = new();
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
    public bool CanSpawn => CanSpawnProfile(_automatic);
    public string AutomaticEnemyType => _automatic?.Definition.EnemyId ?? "none";
    public IReadOnlyList<string> DebugEnemyTypes => _debugNames;
    public bool CanSpawnDebugType(int index) => index >= 0 && index < _debugProfiles.Count && CanSpawnProfile(_debugProfiles[index]);
    [Export] public int MaxSpawnAttempts { get; set; } = 32;
    [Export] public float MinCitadelDistance { get; set; } = 160;
    [Export] public ulong Seed { get; set; } = 1;
    private WorldGrid _grid;
    private GroundNavigation _navigation;
    private EnemyRegistry _registry;
    private EnemyPathRequests _requests;
    private Node2D _container;
    private Building _citadel;
    private sealed record SpawnProfile(PackedScene Scene, EnemyDefinition Definition);
    private sealed class SpawnBatch
    {
        public SpawnProfile Profile; public int Count;
        public SpawnBatch(SpawnProfile profile, int count) { Profile = profile; Count = count; }
    }
    private SpawnProfile _automatic;
    private readonly List<SpawnProfile> _debugProfiles = new();
    private readonly List<string> _debugNames = new();
    private readonly Queue<SpawnBatch> _pending = new();
    private bool CanSpawnProfile(SpawnProfile profile) => _grid != null && profile != null
        && (profile.Definition.TargetRule == EnemyDefinition.TargetPriority.BuildingHunter || EnemyTargeting.Live(_citadel));
    private readonly RandomNumberGenerator _random = new();
    private double _elapsed;

    public override void _Ready() => SetProcess(false);

    // Inspect the scene definition once; validation does not enter the scene tree.
    public void Initialize(WorldGrid grid, GroundNavigation navigation, EnemyRegistry registry, EnemyPathRequests requests, Node2D container, Building citadel)
    {
        _grid = grid; _navigation = navigation; _registry = registry; _requests = requests; _container = container; _citadel = citadel;
        _automatic = Inspect(EnemyScene);
        var debugScenes = DebugEnemyScenes.Count == 0 ? new Godot.Collections.Array<PackedScene> { EnemyScene } : DebugEnemyScenes;
        foreach (PackedScene scene in debugScenes)
        { var profile = Inspect(scene); _debugProfiles.Add(profile); _debugNames.Add(profile.Definition.EnemyId); }
        _random.Seed = Seed;
        _citadel.Detached += OnCitadelLost; _registry.Changed += RefreshProcessing; RefreshProcessing();
    }

    // Cache each scene's own stats once; level tuning only overrides the matching enemy identity.
    private SpawnProfile Inspect(PackedScene scene)
    {
        if (scene == null) throw new System.InvalidOperationException("Enemy spawn scene is required.");
        Node prototype = scene.Instantiate();
        try
        {
            if (prototype is not Enemy enemy || enemy.Definition == null) throw new System.InvalidOperationException("Spawner requires an Enemy scene/definition.");
            EnemyDefinition definition = DefinitionOverride?.EnemyId == enemy.Definition.EnemyId ? DefinitionOverride : enemy.Definition;
            definition.Validate(_grid.CellSize);
            if (definition.Radius * 2 >= Mathf.Min(_grid.Dimensions.X, _grid.Dimensions.Y) * _grid.CellSize)
                throw new System.InvalidOperationException("Enemy radius does not fit map bounds.");
            if (definition.MovementLayer == EnemyMovementLayer.Ground && definition.AttackRange < (_grid.ClearanceCells + .5f) * _grid.CellSize)
                throw new System.InvalidOperationException("Enemy attack range cannot reach a legal approach on this grid.");
            return new SpawnProfile(scene, definition);
        }
        finally { prototype.Free(); }
    }

    // Freeze the selected type in each bounded batch: changing the selector never rewrites already queued units.
    public int QueueDebugSpawns(int count, int typeIndex = 0)
    {
        if (!CanSpawnDebugType(typeIndex)) return 0;
        int accepted = Mathf.Clamp(count, 0, 200 - _registry.Count - PendingDebugSpawns);
        if (accepted > 0) _pending.Enqueue(new SpawnBatch(_debugProfiles[typeIndex], accepted));
        PendingDebugSpawns += accepted;
        MaxActiveEnemies = Mathf.Max(MaxActiveEnemies, _registry.Count + PendingDebugSpawns);
        RefreshProcessing(); return accepted;
    }
    public void CancelDebugSpawns() { _pending.Clear(); PendingDebugSpawns = 0; RefreshProcessing(); }

    // One executor/cap serves both types. Natural spawning retains the authored scene, never a hidden mixed pool.
    public override void _Process(double delta)
    {
        if (_grid == null || _registry.Count >= Mathf.Clamp(MaxActiveEnemies, 0, 200)) return;
        if (PendingDebugSpawns > 0)
        {
            for (int i = 0; i < Mathf.Clamp(MaxDebugSpawnsPerFrame, 1, 8) && _pending.Count > 0
                && _registry.Count < Mathf.Clamp(MaxActiveEnemies, 0, 200); i++)
            {
                SpawnBatch batch = _pending.Peek();
                if (!CanSpawnProfile(batch.Profile)) { PendingDebugSpawns -= batch.Count; _pending.Dequeue(); continue; }
                if (!TrySpawnOne(batch.Profile)) break;
                PendingDebugSpawns--; if (--batch.Count == 0) _pending.Dequeue();
            }
            RefreshProcessing(); return;
        }
        if (!SpawningEnabled || !CanSpawn) return;
        _elapsed += delta;
        float interval = float.IsFinite(SpawnInterval) ? Mathf.Max(.05f, SpawnInterval) : .8f;
        if (_elapsed < interval) return;
        _elapsed = 0; TrySpawnOne(_automatic);
    }

    // One bounded edge search; a blocked batch remains pending until space opens or it is cancelled.
    private bool TrySpawnOne(SpawnProfile profile)
    {
        EnemyDefinition definition = profile.Definition;
        bool flying = definition.MovementLayer == EnemyMovementLayer.Flying;
        int inset = flying ? Mathf.Max(0, Mathf.CeilToInt(definition.Radius / _grid.CellSize - .5f)) : _grid.ClearanceCells;
        Rect2 airBounds = _grid.FootprintRect(_grid.Bounds).Grow(-definition.Radius);
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
            bool movementValid = flying ? airBounds.HasPoint(position)
                : (SpawnCellFilter?.Invoke(cell) ?? true) && _navigation.IsWalkable(cell) && _grid.SegmentClear(position, position, definition.Radius);
            if (!movementValid
                || (GodotObject.IsInstanceValid(_citadel) && position.DistanceSquaredTo(_citadel.GlobalPosition) < Mathf.Pow(Mathf.Max(0, MinCitadelDistance), 2))
                || !_registry.SpawnSpaceFree(position, definition.Radius)
                || !(AdditionalSpawnValidation?.Invoke(position, definition.Radius) ?? true)) continue;
            Enemy enemy = profile.Scene.Instantiate<Enemy>(); enemy.Definition = definition;
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
        SetProcess(((SpawningEnabled && CanSpawn) || PendingDebugSpawns > 0) && _registry.Count < Mathf.Clamp(MaxActiveEnemies, 0, 200));
    }
    private void OnCitadelLost()
    {
        int batches = _pending.Count;
        for (int i = 0; i < batches; i++)
        { var batch = _pending.Dequeue(); if (CanSpawnProfile(batch.Profile)) _pending.Enqueue(batch); else PendingDebugSpawns -= batch.Count; }
        RefreshProcessing();
    }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_citadel)) _citadel.Detached -= OnCitadelLost;
        if (GodotObject.IsInstanceValid(_registry)) _registry.Changed -= RefreshProcessing;
        _pending.Clear(); _debugProfiles.Clear(); _debugNames.Clear(); PendingDebugSpawns = 0; SpawnCellFilter = null; AdditionalSpawnValidation = null; TargetRoster = null; _random.Dispose();
    }
}
