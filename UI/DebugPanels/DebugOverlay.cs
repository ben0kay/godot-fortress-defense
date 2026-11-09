// Reusable opt-in debug UI: real engine FPS, registry counts and bounded manual spawn requests.
using Godot;

public partial class DebugOverlay : CanvasLayer
{
    [Export] public FogOfWar Fog { get; set; }
    [Export] public EnemyVisibilityCoordinator EnemyVisibility { get; set; }
    private CheckBox _optimizeVisuals;
    private Label _visibilityStats;
    private CheckBox _fogEnabled;
    private Label _fogStats;
    [Export] public EnemyRegistry Registry { get; set; }
    [Export] public EdgeEnemySpawner Spawner { get; set; }
    [Export] public Key ToggleKey { get; set; } = Key.F2;
    [Export] public bool StartExpanded { get; set; }
    private LevelController _level;
    private Label _stats, _message;
    private VBoxContainer _controls;
    private SpinBox _amount;
    private CheckBox _automatic;
    private OptionButton _enemyType;
    private Label _targeting;
    private EdgeEnemySpawner _listedSpawner;
    private Timer _refresh;
    private Button[] _spawnButtons;

    // Explicit Inspector bindings work in arbitrary scenes; level ancestors bind once after initialization.
    public override void _Ready()
    {
        _stats = GetNode<Label>("Panel/Layout/Stats"); _message = GetNode<Label>("Panel/Layout/Controls/Message");
        _controls = GetNode<VBoxContainer>("Panel/Layout/Controls"); _controls.Visible = StartExpanded;
        _fogEnabled = GetNode<CheckBox>("Panel/Layout/Controls/FogEnabled");
        _fogStats = GetNode<Label>("Panel/Layout/Controls/FogStats");
        _fogEnabled.Toggled += OnFogEnabled;
        _optimizeVisuals = GetNode<CheckBox>("Panel/Layout/Controls/OptimizeVisuals");
        _visibilityStats = GetNode<Label>("Panel/Layout/Controls/VisibilityStats");
        _optimizeVisuals.Toggled += OnOptimizeVisuals;
        _enemyType = GetNode<OptionButton>("Panel/Layout/Controls/EnemyType");
        _targeting = GetNode<Label>("Panel/Layout/Controls/Targeting");
        _enemyType.ItemSelected += OnEnemyTypeSelected;
        _amount = GetNode<SpinBox>("Panel/Layout/Controls/Custom/Amount");
        _automatic = GetNode<CheckBox>("Panel/Layout/Controls/Automatic"); _automatic.Toggled += OnAutomatic;
        GetNode<Button>("Panel/Layout/Toggle").Text = $"Debug spawn controls ({ToggleKey})";
        GetNode<Button>("Panel/Layout/Toggle").Pressed += Toggle;
        _spawnButtons = new[] { Button("Presets/Add10"), Button("Presets/Add50"), Button("MorePresets/Add100"), Button("MorePresets/Add200"), Button("Custom/Add") };
        _spawnButtons[0].Pressed += () => Spawn(10); _spawnButtons[1].Pressed += () => Spawn(50);
        _spawnButtons[2].Pressed += () => Spawn(100); _spawnButtons[3].Pressed += () => Spawn(200);
        _spawnButtons[4].Pressed += () => Spawn((int)_amount.Value);
        Button("Clear").Pressed += Clear;
        _refresh = GetNode<Timer>("Refresh"); _refresh.Timeout += Refresh;
        for (Node parent = GetParent(); parent != null; parent = parent.GetParent())
            if (parent is LevelController level)
            { _level = level; if (level.IsNodeReady()) BindLevel(); else level.Ready += BindLevel; break; }
        Refresh();
    }
    private Button Button(string path) => GetNode<Button>("Panel/Layout/Controls/" + path);
    private void BindLevel()
    {
        EnemyVisibility ??= _level.EnemyVisibility; Fog ??= _level.Fog; Registry ??= _level.DebugEnemies; Spawner ??= _level.DebugSpawner;
        _level.Ready -= BindLevel; Refresh();
    }

    // Four-Hz reads only; enemy counts come from the registry rather than scene scans.
    private void Refresh()
    {
        bool visibility = GodotObject.IsInstanceValid(EnemyVisibility) && EnemyVisibility.IsInitialized;
        _optimizeVisuals.Disabled = !visibility;
        _optimizeVisuals.SetPressedNoSignal(visibility && EnemyVisibility.OptimizationEnabled);
        _visibilityStats.Text = visibility && _controls.Visible
            ? $"Enemies {EnemyVisibility.TotalEnemies} | Visible {EnemyVisibility.VisibleEnemies} | Hidden {EnemyVisibility.HiddenEnemies}\nHidden visuals {(EnemyVisibility.OptimizationEnabled ? "ON" : "OFF")}{(EnemyVisibility.OptimizationEnabled && !EnemyVisibility.OptimizationEffective ? " (fog bypass: cosmetics awake)" : "")}" : "";
        bool fog = GodotObject.IsInstanceValid(Fog);
        _fogEnabled.Disabled = !fog;
        _fogEnabled.SetPressedNoSignal(fog && Fog.PresentationEnabled);
        _fogStats.Text = fog && _controls.Visible ? $"Vision sources {Fog.SourceCount} | Visible {Fog.VisibleCells} | Explored {Fog.ExploredCells}\nFog update {Fog.LastUpdateMilliseconds:0.000} ms | Changed cells {Fog.LastUpdatedCells}" : "";
        bool registry = GodotObject.IsInstanceValid(Registry), spawner = GodotObject.IsInstanceValid(Spawner);
        _stats.Text = $"FPS {Engine.GetFramesPerSecond()}  |  Enemies {(registry ? Registry.Count.ToString() : "N/A")}"
            + (spawner ? $"  |  Queued {Spawner.PendingDebugSpawns}" : "");
        if (spawner && (_listedSpawner != Spawner || _enemyType.ItemCount != Spawner.DebugEnemyTypes.Count))
        {
            _enemyType.Clear();
            foreach (string id in Spawner.DebugEnemyTypes) _enemyType.AddItem(id);
            _listedSpawner = Spawner;
        }
        _enemyType.Disabled = !spawner || _enemyType.ItemCount == 0;
        bool canSpawn = spawner && registry && Spawner.CanSpawnDebugType(_enemyType.Selected);
        var targets = spawner ? Spawner.TargetRoster : null;
        string diagnostics = _controls.Visible && GodotObject.IsInstanceValid(targets)
            ? $"Target jobs {targets.PendingAcquisitions} | Global {targets.GlobalSearches} | Local {targets.LocalSearches}" : "";
        PowerNetworkManager power = _level?.PowerNetwork;
        if (_controls.Visible && GodotObject.IsInstanceValid(power))
            diagnostics += $"\nPower networks {power.NetworkCount} | Relays {power.NodeCount} | Endpoints {power.EndpointCount}";
        _targeting.Text = diagnostics;
        foreach (Button button in _spawnButtons) button.Disabled = !canSpawn;
        _automatic.Text = spawner ? $"Automatic spawning ({Spawner.AutomaticEnemyType})" : "Automatic spawning";
        _automatic.Disabled = !spawner || !Spawner.CanSpawn;
        _automatic.SetPressedNoSignal(spawner && Spawner.SpawningEnabled);
        Button("Clear").Disabled = !registry;
    }
    private void Spawn(int count)
    {
        if (!GodotObject.IsInstanceValid(Spawner)) return;
        int accepted = Spawner.QueueDebugSpawns(count, _enemyType.Selected);
        _message.Text = $"Queued {accepted} of {count}. Safety cap: 200 active + pending."; Refresh();
    }
    private void OnEnemyTypeSelected(long index) => Refresh();
    private void OnOptimizeVisuals(bool enabled)
    { if (GodotObject.IsInstanceValid(EnemyVisibility)) EnemyVisibility.SetOptimizationEnabled(enabled); Refresh(); }
    private void OnFogEnabled(bool enabled)
    { if (GodotObject.IsInstanceValid(Fog)) Fog.SetPresentationEnabled(enabled); Refresh(); }
    private void OnAutomatic(bool enabled)
    { if (GodotObject.IsInstanceValid(Spawner)) Spawner.SpawningEnabled = enabled; }
    private void Clear()
    {
        if (GodotObject.IsInstanceValid(Spawner)) { Spawner.SpawningEnabled = false; Spawner.CancelDebugSpawns(); }
        if (GodotObject.IsInstanceValid(Registry)) Registry.ClearEnemies();
        _message.Text = "Cleared. Automatic spawning paused; manual Add still works."; Refresh();
    }
    private void Toggle() { _controls.Visible = !_controls.Visible; Refresh(); }
    public override void _Input(InputEvent input)
    {
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == ToggleKey)
        { Toggle(); GetViewport().SetInputAsHandled(); }
    }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_optimizeVisuals)) _optimizeVisuals.Toggled -= OnOptimizeVisuals;
        if (GodotObject.IsInstanceValid(_fogEnabled)) _fogEnabled.Toggled -= OnFogEnabled;
        if (GodotObject.IsInstanceValid(_level)) _level.Ready -= BindLevel;
        if (GodotObject.IsInstanceValid(_enemyType)) _enemyType.ItemSelected -= OnEnemyTypeSelected;
        if (GodotObject.IsInstanceValid(_refresh)) _refresh.Timeout -= Refresh;
    }
}
