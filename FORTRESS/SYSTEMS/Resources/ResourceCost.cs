// Typed positive purchase quantities; materials are schema-only until their gameplay passes.
using Godot;

[GlobalClass]
public partial class ResourceCost : Resource
{
    public enum ResourceId { Credits, Copper, Iron, Silicon }
    [Export] public ResourceId Id { get; set; } = ResourceId.Credits;
    [Export(PropertyHint.Range, "1,2000000000,1")] public int Quantity { get; set; } = 1;
}
