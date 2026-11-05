// Per-level world and initial enemy tuning, without implementing future campaign/spawn-mode systems.
using Godot;

[GlobalClass]
public partial class LevelConfig : Resource
{
    [Export] public string LevelName { get; set; } = "Level001";
    [Export] public WorldGridConfig Grid { get; set; }
    [Export] public WorldGenerationConfig Generation { get; set; }
    [Export] public EnemyDefinition EnemyStats { get; set; }
    [ExportGroup("Construction / BM0")]
    [Export] public BuildingCatalogue Catalogue { get; set; }
    [Export(PropertyHint.Range, "0,2000000000,1")] public int StartingCredits { get; set; } = 1000;
    [Export] public string[] AllowedBuildingIds { get; set; } = System.Array.Empty<string>();
    [Export] public bool FreeDebugPlacement { get; set; }
    [ExportGroup("Optional Player / PL0")]
    [Export] public bool SpawnPlayer { get; set; }
    [Export] public PackedScene PlayerScene { get; set; }
    [ExportGroup("Future Defeat Conditions")]
    [Export] public bool PlayerRequiredForDefeat { get; set; }
    [Export] public bool CitadelRequiredForDefeat { get; set; } = true;
}
