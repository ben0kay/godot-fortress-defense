// Explicit placement boundary: instantiate inherited building scenes, validate, claim and register.
using Godot;

public partial class BuildingPlacement : Node
{
    private WorldGrid _grid;
    private BuildingRegistry _registry;
    private Node2D _container;

    // World composition supplies dependencies; this service has no global scene searches.
    public void Initialize(WorldGrid grid, BuildingRegistry registry, Node2D container)
    { _grid = grid; _registry = registry; _container = container; }

    // Failed placement frees the detached/unused instance and never leaves a partial claim.
    public bool TryPlace(PackedScene scene, Vector2I anchor, out Building building)
    {
        building = null;
        if (scene == null || _grid == null || !GodotObject.IsInstanceValid(_container)) return false;
        Node instance = scene.Instantiate();
        if (instance is not Building candidate) { instance.Free(); return false; }
        try
        {
            candidate.Initialize();
            if (!_grid.CanOccupy(new Rect2I(anchor, candidate.Footprint))) { candidate.Free(); return false; }
            _container.AddChild(candidate);
            if (!candidate.TryPlace(_grid, _registry, anchor)) { candidate.Free(); return false; }
            building = candidate; return true;
        }
        catch (System.Exception error)
        { candidate.Free(); GD.PushError("Building placement failed: " + error.Message); return false; }
    }
}
