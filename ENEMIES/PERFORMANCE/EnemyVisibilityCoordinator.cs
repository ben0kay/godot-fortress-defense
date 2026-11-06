// Cosmetic-only visibility cache/counters; the registry and fog retain all gameplay authority.
using Godot;
using System.Collections.Generic;

public partial class EnemyVisibilityCoordinator : Node
{
    public bool IsInitialized => _fog != null && _registry != null;
    public bool OptimizationEnabled { get; private set; }
    public bool OptimizationEffective => IsInitialized && OptimizationEnabled && _fog.PresentationEnabled;
    public int TotalEnemies => _entries.Count;
    public int VisibleEnemies { get; private set; }
    public int HiddenEnemies => TotalEnemies - VisibleEnemies;
    private FogOfWar _fog;
    private EnemyRegistry _registry;
    private readonly Dictionary<Enemy, Entry> _entries = new();
    private readonly List<Enemy> _scratch = new();
    private sealed class Entry
    {
        public Vector2I Cell;
        public bool Visible;
        public EnemyVectorVisual Visual;
    }

    // Composition binds once after fog initialization; no autonomous discovery or processing loop.
    public void Initialize(FogOfWar fog, EnemyRegistry registry)
    {
        if (IsInitialized) throw new System.InvalidOperationException("Enemy visibility is already initialized.");
        _fog = fog; _registry = registry; OptimizationEnabled = fog.Config.OptimizeHiddenEnemyVisuals;
        registry.Added += OnAdded; registry.Removed += OnRemoved; registry.PositionChanged += OnPositionChanged;
        fog.VisibilityChanged += OnFogChanged; fog.PresentationChanged += OnPresentationChanged;
        fog.TreeExiting += Shutdown; registry.TreeExiting += Shutdown;
        foreach (Enemy enemy in registry.Enemies) OnAdded(enemy);
        SetProcess(false); SetPhysicsProcess(false);
    }
    private void OnAdded(Enemy enemy)
    {
        if (_entries.ContainsKey(enemy)) return;
        Vector2I cell = _fog.Grid.WorldToCell(enemy.GlobalPosition);
        var entry = new Entry { Cell = cell, Visible = _fog.IsVisible(cell), Visual = enemy.GetNodeOrNull<EnemyVectorVisual>("VectorVisual") };
        _entries.Add(enemy, entry);
        if (entry.Visible) VisibleEnemies++;
        Apply(entry);
    }
    private void OnRemoved(Enemy enemy)
    {
        if (!_entries.Remove(enemy, out var entry)) return;
        if (entry.Visible) VisibleEnemies--;
        if (GodotObject.IsInstanceValid(entry.Visual)) entry.Visual.SetCosmeticDormant(false);
    }

    // Existing movement/index notifications: same fog cell requires no visibility query or allocation.
    private void OnPositionChanged(Enemy enemy)
    {
        if (!_entries.TryGetValue(enemy, out var entry)) return;
        Vector2I cell = _fog.Grid.WorldToCell(enemy.GlobalPosition);
        if (cell == entry.Cell) return;
        entry.Cell = cell; Classify(entry);
    }
    // Fog dirty regions use existing spatial buckets, followed by an exact center-cell region check.
    private void OnFogChanged(Rect2I area)
    {
        _registry.Query(_fog.Grid.FootprintRect(area), _scratch);
        foreach (Enemy enemy in _scratch)
            if (_entries.TryGetValue(enemy, out var entry) && area.HasPoint(entry.Cell)) Classify(entry);
        _scratch.Clear();
    }
    private void Classify(Entry entry)
    {
        bool visible = _fog.IsVisible(entry.Cell);
        if (entry.Visible == visible) return;
        entry.Visible = visible; VisibleEnemies += visible ? 1 : -1;
        Apply(entry);
    }
    private void Apply(Entry entry)
    {
        if (GodotObject.IsInstanceValid(entry.Visual)) entry.Visual.SetCosmeticDormant(OptimizationEffective && !entry.Visible);
    }

    // Runtime override is world-local and never writes the authored FogMasterConfig resource.
    public void SetOptimizationEnabled(bool enabled)
    {
        if (OptimizationEnabled == enabled) return;
        OptimizationEnabled = enabled; OnPresentationChanged();
    }
    private void OnPresentationChanged()
    {
        // One pass only for an explicit debug toggle; counters/classifications remain logical fog values.
        foreach (Entry entry in _entries.Values) Apply(entry);
    }
    private void Shutdown()
    {
        if (GodotObject.IsInstanceValid(_fog))
        { _fog.VisibilityChanged -= OnFogChanged; _fog.PresentationChanged -= OnPresentationChanged; _fog.TreeExiting -= Shutdown; }
        if (GodotObject.IsInstanceValid(_registry))
        { _registry.Added -= OnAdded; _registry.Removed -= OnRemoved; _registry.PositionChanged -= OnPositionChanged; _registry.TreeExiting -= Shutdown; }
        foreach (Entry entry in _entries.Values)
            if (GodotObject.IsInstanceValid(entry.Visual)) entry.Visual.SetCosmeticDormant(false);
        _entries.Clear(); _scratch.Clear(); VisibleEnemies = 0; _fog = null; _registry = null;
    }
    public override void _ExitTree() => Shutdown();
}
