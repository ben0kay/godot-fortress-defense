// Shared swept footprint contact for impact attacks and intended-target projectiles.
using Godot;

public static class CombatGeometry
{
    // Earliest inclusive segment/rectangle contact; callers consume an attack before invoking damage callbacks.
    public static float SegmentRectEntry(Vector2 from, Vector2 to, Rect2 rect)
    {
        float enter = 0, exit = 1; Vector2 direction = to - from;
        return Clip(from.X, direction.X, rect.Position.X, rect.End.X, ref enter, ref exit)
            && Clip(from.Y, direction.Y, rect.Position.Y, rect.End.Y, ref enter, ref exit) ? enter : float.PositiveInfinity;
    }
    private static bool Clip(float from, float delta, float low, float high, ref float enter, ref float exit)
    {
        if (Mathf.Abs(delta) < .00001f) return from >= low && from <= high;
        float a = (low - from) / delta, b = (high - from) / delta;
        enter = Mathf.Max(enter, Mathf.Min(a, b)); exit = Mathf.Min(exit, Mathf.Max(a, b));
        return enter <= exit;
    }
}
