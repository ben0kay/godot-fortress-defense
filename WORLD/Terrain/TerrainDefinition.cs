// Terrain appearance and fixed WG0 semantics; art cannot silently change collision rules.
using Godot;

[GlobalClass]
public partial class TerrainDefinition : Resource
{
    [Export] public TerrainType Type { get; set; }
    [Export] public Color Tint { get; set; } = new(.025f, .045f, .065f);
    public bool Buildable => Type == TerrainType.Ground;
    public bool Traversable => Type == TerrainType.Ground;
}
