// Stationary translucent beam from the drone to the construction footprint.
using Godot;

public partial class ConstructionBeam : Polygon2D
{
    public void Deploy(Vector2 target, float halfWidth, Color color)
    {
        Vector2 tip = ToLocal(target);
        Vector2 side = tip.Normalized().Orthogonal();
        if (tip.IsZeroApprox()) { Hide(); return; }
        Polygon = new[] { -side * 3, side * 3, tip + side * halfWidth, tip - side * halfWidth };
        Color = color;
        Show();
    }
}
