// Fixed-layout stress sandbox; shares level composition and gameplay, without campaign/generation configuration.
using Godot;

public partial class FoundationSandbox : LevelController
{
    [Export] public bool InitialObstacles { get; set; } = true;

    // Preserve the deterministic ST1 obstacle layout for regression/stress experimentation.
    protected override void PopulateSandboxTerrain(WorldGrid grid)
    {
        Vector2I map = grid.Dimensions;
        if (!InitialObstacles || map.X < 24 || map.Y < 18) return;
        grid.TrySetTerrain(new Rect2I(map.X / 4, 3, 2, map.Y / 2 - 5), true);
        grid.TrySetTerrain(new Rect2I(map.X * 3 / 4, map.Y / 2 + 2, 2, map.Y / 2 - 5), true);
        grid.TrySetTerrain(new Rect2I(map.X / 2 + 3, 4, 4, 2), true);
        grid.TrySetTerrain(new Rect2I(map.X / 2 - 7, map.Y - 6, 4, 2), true);
    }
}
