// Continuous waypoint following with immediate conservative grid collision checks, independent of AI frequency.
using Godot;

public sealed class EnemyMovement
{
    private Vector2[] _path = System.Array.Empty<Vector2>();
    private int _index;
    public bool HasRoute => _index < _path.Length;

    // Requests stop only the affected enemy; unaffected enemies retain their cached routes.
    public void Stop() { _path = System.Array.Empty<Vector2>(); _index = 0; }
    public void Assign(Vector2[] path) { _path = path; _index = 0; }

    // Conservative remaining-segment bounds ignore cells already passed, without A* or scene searches.
    public bool Intersects(Vector2 current, Rect2 changed)
    {
        for (int i = _index; i < _path.Length; i++)
        {
            Vector2 end = _path[i];
            Rect2 segment = new(new Vector2(Mathf.Min(current.X, end.X), Mathf.Min(current.Y, end.Y)),
                new Vector2(Mathf.Abs(end.X - current.X), Mathf.Abs(end.Y - current.Y)));
            if (segment.Grow(.01f).Intersects(changed, true)) return true;
            current = end;
        }
        return false;
    }

    // Bound unusually large delta/speed work; every actual step is swept against CURRENT occupancy.
    public bool Advance(Enemy enemy, WorldGrid grid, double delta)
    {
        float remaining = enemy.Speed * (float)delta;
        for (int steps = 0; HasRoute && steps < 8; steps++)
        {
            Vector2 offset = _path[_index] - enemy.GlobalPosition; float distance = offset.Length();
            if (distance < .01f)
            {
                if (!grid.SegmentClear(enemy.GlobalPosition, _path[_index], enemy.Radius)) return false;
                enemy.GlobalPosition = _path[_index++]; continue;
            }
            Vector2 next = enemy.GlobalPosition + offset / distance * Mathf.Min(remaining, distance);
            if (!grid.SegmentClear(enemy.GlobalPosition, next, enemy.Radius)) return false;
            enemy.GlobalPosition = next;
            enemy.Rotation = Mathf.LerpAngle(enemy.Rotation, offset.Angle(), Mathf.Min(1, enemy.TurnSpeed * (float)delta));
            remaining -= Mathf.Min(remaining, distance);
            if (next.DistanceSquaredTo(_path[_index]) < .0001f)
            { enemy.GlobalPosition = _path[_index]; _index++; }
            if (remaining <= 0) break;
        }
        return true;
    }
}
