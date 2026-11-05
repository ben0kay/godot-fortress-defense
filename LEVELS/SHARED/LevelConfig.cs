// Per-level world and initial enemy tuning, without implementing future campaign/spawn-mode systems.
using Godot;

[GlobalClass]
public partial class LevelConfig : Resource
{
    [Export] public string LevelName { get; set; } = "Level001";
    [Export] public WorldGridConfig Grid { get; set; }
    [Export] public WorldGenerationConfig Generation { get; set; }
    [Export] public EnemyDefinition EnemyStats { get; set; }
}
