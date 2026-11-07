// One authored progress threshold and its sprite.
using Godot;

[GlobalClass]
public partial class BuildingConstructionStage : Resource
{
    [Export(PropertyHint.Range, "0,0.99,0.01")] public float Progress { get; set; }
    [Export] public Texture2D Texture { get; set; }
}
