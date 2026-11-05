// Direct flight: map-boundary clamping and spatial-index movement only, with no ground geometry/navigation queries.
using Godot;

public sealed class EnemyFlyingMovement
{
    public Vector2 NextPosition(Enemy enemy, WorldGrid grid, Vector2 destination, double delta)
    {
        Rect2 bounds = grid.FootprintRect(grid.Bounds).Grow(-enemy.Radius);
        return enemy.GlobalPosition.MoveToward(destination, enemy.Speed * (float)delta).Clamp(bounds.Position, bounds.End);
    }
    public void Apply(Enemy enemy, Vector2 destination, Vector2 next, double delta)
    {
        Vector2 direction = destination - enemy.GlobalPosition;
        if (direction.LengthSquared() > .0001f)
            enemy.Rotation = Mathf.LerpAngle(enemy.Rotation, direction.Angle(), Mathf.Min(1, enemy.TurnSpeed * (float)delta));
        enemy.SetMovementPosition(next);
    }
}
