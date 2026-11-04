// Shared authored building metadata; mutable health and placement live on Building.
using Godot;
using System;

[GlobalClass]
public partial class BuildingDefinition : Resource
{
    public enum BuildingCategory { Core, Defense, Energy, Extraction, Economy, Industry, Infrastructure }
    [ExportGroup("Identity")]
    [Export] public string BuildingId { get; set; } = "building";
    [Export] public string DisplayName { get; set; } = "Building";
    [Export] public BuildingCategory Category { get; set; }
    [ExportGroup("Foundation")]
    [Export] public Vector2I Footprint { get; set; } = Vector2I.One;
    [Export] public float MaxHealth { get; set; } = 100;

    // Definitions are read once on spawn; a footprint never silently changes while reserved.
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BuildingId) || BuildingId != BuildingId.Trim()
            || !Enum.IsDefined(Category) || Footprint.X < 1 || Footprint.Y < 1
            || Footprint.X > 512 || Footprint.Y > 512 || !float.IsFinite(MaxHealth) || MaxHealth <= 0)
            throw new InvalidOperationException("Invalid building identity, category, footprint or health.");
    }
}
