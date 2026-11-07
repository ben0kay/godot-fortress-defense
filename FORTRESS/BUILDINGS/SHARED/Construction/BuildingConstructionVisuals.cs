// Construction sprite under Building/Visuals. Stage changes are driven by work events.
using Godot;

public partial class BuildingConstructionVisuals : Sprite2D
{
    [Export] public BuildingConstructionArt Art { get; set; }
    private Building _building;

    public override void _Ready()
    {
        _building = GetParent().GetParent<Building>();
        _building.WorkProgressChanged += Refresh;
        _building.WorkCompleted += Refresh;
        Refresh(_building);
    }

    private void Refresh(Building building)
    {
        Visible = !building.IsOperational;
        if (!Visible || Art == null) return;
        Texture2D stage = Art.TextureAt(building.ConstructionProgress, out float nextThreshold);
        building.RequestWorkProgressAt(nextThreshold);
        if (Texture != stage) Texture = stage;
    }

    public override void _ExitTree()
    {
        if (!GodotObject.IsInstanceValid(_building)) return;
        _building.WorkProgressChanged -= Refresh;
        _building.WorkCompleted -= Refresh;
    }
}
