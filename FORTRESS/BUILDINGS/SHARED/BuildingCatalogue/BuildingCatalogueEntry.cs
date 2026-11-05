// Authored scene/definition pair; menus read metadata without creating building instances.
using Godot;

[GlobalClass]
public partial class BuildingCatalogueEntry : Resource
{
    [Export] public BuildingDefinition Definition { get; set; }
    [Export] public PackedScene Scene { get; set; }
}
