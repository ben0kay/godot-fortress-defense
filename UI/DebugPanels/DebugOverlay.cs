// Reusable opt-in debug UI: real engine FPS, registry counts and bounded manual spawn requests.
using Godot;

public partial class DebugOverlay : CanvasLayer
{
    [Export] public EnemyRegistry Registry { get; set; }
    [Export] public EdgeEnemySpawner Spawner { get; set; }
    [Export] public Key ToggleKey { get; set; } = Key.F2;
    [Export] public bool StartExpanded { get; set; }
    private LevelController _level;
    private Label _stats, _message;
    private VBoxContainer _controls;
    private SpinBox _amount;
    private CheckBox _automatic;
    private Timer _refresh;
    private Button[] _spawnButtons;

    // Explicit Inspector bindings work in arbitrary scenes; level ancestors bind once after initialization.
    public override void _Ready()
    {
        _stats = GetNode<Label>("Panel/Layout/Stats"); _message = GetNode<Label>("Panel/Layout/Controls/Message");
        _controls = GetNode<VBoxContainer>("Panel/Layout/Controls"); _controls.Visible = StartExpanded;
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
        Registry ??= _level.DebugEnemies; Spawner ??= _level.DebugSpawner;
        _level.Ready -= BindLevel; Refresh();
    }

    // Four-Hz reads only; enemy counts come from the registry rather than scene scans.
    private void Refresh()
    {
        bool registry = GodotObject.IsInstanceValid(Registry), spawner = GodotObject.IsInstanceValid(Spawner);
        _stats.Text = $"FPS {Engine.GetFramesPerSecond()}  |  Enemies {(registry ? Registry.Count.ToString() : "N/A")}"
            + (spawner ? $"  |  Queued {Spawner.PendingDebugSpawns}" : "");
        bool canSpawn = spawner && Spawner.CanSpawn && registry;
        foreach (Button button in _spawnButtons) button.Disabled = !canSpawn;
        _automatic.Disabled = !canSpawn;
        _automatic.SetPressedNoSignal(spawner && Spawner.SpawningEnabled);
        Button("Clear").Disabled = !registry;
    }
    private void Spawn(int count)
    {
        if (!GodotObject.IsInstanceValid(Spawner)) return;
        int accepted = Spawner.QueueDebugSpawns(count);
        _message.Text = $"Queued {accepted} of {count}. Safety cap: 200 active + pending."; Refresh();
    }
    private void OnAutomatic(bool enabled)
    { if (GodotObject.IsInstanceValid(Spawner)) Spawner.SpawningEnabled = enabled; }
    private void Clear()
    {
        if (GodotObject.IsInstanceValid(Spawner)) { Spawner.SpawningEnabled = false; Spawner.CancelDebugSpawns(); }
        if (GodotObject.IsInstanceValid(Registry)) Registry.ClearEnemies();
        _message.Text = "Cleared. Automatic spawning paused; manual Add still works."; Refresh();
    }
    private void Toggle() => _controls.Visible = !_controls.Visible;
    public override void _Input(InputEvent input)
    {
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == ToggleKey)
        { Toggle(); GetViewport().SetInputAsHandled(); }
    }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_level)) _level.Ready -= BindLevel;
        if (GodotObject.IsInstanceValid(_refresh)) _refresh.Timeout -= Refresh;
    }
}
