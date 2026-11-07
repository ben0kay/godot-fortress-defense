// Curved direct flight using a retained arc-length table and fixed acceleration/braking rates.
using Godot;

internal sealed class ConstructionDroneMotion
{
    private const int Samples = 32;
    private readonly float[] _lengths = new float[Samples + 1];
    private Vector2 _from, _bend, _to;
    private float _elapsed, _duration, _speed, _acceleration, _deceleration;

    // A quadratic bend gives the short trip a readable departure and arrival arc.
    public void Begin(Vector2 from, Vector2 to, ConstructionDroneMasterConfig config)
    {
        _from = from;
        _to = to;
        Vector2 offset = to - from;
        _bend = (from + to) * .5f + offset.Normalized().Orthogonal() * Mathf.Min(100, offset.Length() * .35f);
        Vector2 previous = from;
        for (int i = 1; i <= Samples; i++)
        {
            Vector2 next = Point((float)i / Samples);
            _lengths[i] = _lengths[i - 1] + next.DistanceTo(previous);
            previous = next;
        }

        _elapsed = 0;
        _acceleration = config.Acceleration;
        _deceleration = config.Deceleration;
        float distance = _lengths[Samples];
        float ramps = 1 / _acceleration + 1 / _deceleration;
        _speed = Mathf.Min(config.TravelSpeed, Mathf.Sqrt(2 * distance / ramps));
        _duration = _speed > 0 ? _speed * ramps + Mathf.Max(0, distance - .5f * _speed * _speed * ramps) / _speed : 0;
    }

    public bool Advance(float delta, float arrivalRadius, out Vector2 position)
    {
        _elapsed = Mathf.Min(_duration, _elapsed + delta);
        if (_elapsed >= _duration) { position = _to; return true; }
        float accelerationTime = _speed / _acceleration;
        float remainingTime = _duration - _elapsed;
        float distance = _elapsed < accelerationTime ? .5f * _acceleration * _elapsed * _elapsed
            : remainingTime < _speed / _deceleration ? _lengths[Samples] - .5f * _deceleration * remainingTime * remainingTime
            : .5f * _speed * accelerationTime + _speed * (_elapsed - accelerationTime);

        // Permit a small arrival tolerance during the braking leg.
        if (remainingTime <= _speed / _deceleration && _lengths[Samples] - distance <= arrivalRadius)
        { position = _to; return true; }

        int low = 0, high = Samples;
        while (high - low > 1)
        {
            int middle = (low + high) / 2;
            if (_lengths[middle] < distance) low = middle; else high = middle;
        }
        float span = _lengths[high] - _lengths[low];
        float fraction = span > 0 ? (distance - _lengths[low]) / span : 0;
        position = Point((low + fraction) / Samples);
        return false;
    }

    private Vector2 Point(float t) => _from.Lerp(_bend, t).Lerp(_bend.Lerp(_to, t), t);
}
