// Opt-in vector building art with health/selection feedback; only hit flashes temporarily process.
using Godot;

public partial class BuildingVectorVisual : Node2D
{
    [Export] public bool CitadelStyle { get; set; }
    [Export] public Color Accent { get; set; } = new(.25f, .85f, 1f);
    private Building _building;
    private float _flash;
    private float _lastHealth;

    // Art subscribes to the common lifecycle; no timing assumptions about parent Ready/placement.
    public override void _Ready()
    {
        _building = GetParent().GetParent<Building>(); _lastHealth = _building.Health;
        _building.PresentationChanged += QueueRedraw;
        _building.Placed += QueueRedraw; _building.SelectionChanged += QueueRedraw;
        _building.HealthChanged += OnHealthChanged; _building.Detached += OnDetached;
        SetProcess(false);
    }
    private void OnHealthChanged()
    {
        if (_building.Health < _lastHealth) { _flash = .16f; SetProcess(true); }
        _lastHealth = _building.Health; QueueRedraw();
    }
    // A short detached visual makes destruction visible while the building's cells release immediately.
    private void OnDetached()
    {
        if (_building.IsDestroyed && IsInsideTree())
        {
            var pulse = new DestructionPulse { GlobalPosition = _building.GlobalPosition,
                Radius = _building.FootprintPixels.Length() * .5f, Tint = Accent };
            GetTree().CurrentScene.AddChild(pulse); pulse.GlobalPosition = _building.GlobalPosition;
        }
    }
    public override void _Process(double delta)
    { _flash = Mathf.Max(0, _flash - (float)delta); QueueRedraw(); if (_flash == 0) SetProcess(false); }

    // Static wall plates and a central CPU/core silhouette; art never changes collision.
    public override void _Draw()
    {
        if (_building == null || !_building.IsPlaced) return;
        Vector2 size = _building.FootprintPixels;
        Rect2 body = new(-size * .5f + Vector2.One * 2, size - Vector2.One * 4);
        Color edge = _flash > 0 && _building.HasCurrentVision ? Colors.White : Accent;
        DrawRect(body, new Color(.06f, .12f, .18f)); DrawRect(body, edge, false, 2);
        if (CitadelStyle)
        {
            DrawRect(body.Grow(-7), Accent.Darkened(.5f), false, 2);
            float radius = Mathf.Min(size.X, size.Y) * .27f;
            DrawCircle(Vector2.Zero, radius, Accent.Darkened(.65f));
            DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, 32, edge, 3, true);
            DrawRect(new Rect2(-Vector2.One * radius * .45f, Vector2.One * radius * .9f), edge);
            for (int i = -1; i <= 1; i++)
            { float x = i * size.X * .23f; DrawLine(new(x, body.Position.Y), new(x, -radius), edge, 2); }
        }
        else
        {
            DrawLine(body.Position + new Vector2(4, 4), body.End - new Vector2(4, 4), edge.Darkened(.3f), 2);
            DrawLine(new(body.End.X - 4, body.Position.Y + 4), new(body.Position.X + 4, body.End.Y - 4), edge.Darkened(.3f), 2);
        }
        if (!_building.HasCurrentVision) return;
        if (_building.IsSelected) DrawRect(body.Grow(4), Colors.Yellow, false, 2);
        Rect2 bar = new(new(-size.X * .5f, -size.Y * .5f - 7), new(size.X, 4));
        DrawRect(bar, new Color(.12f, .15f, .2f));
        bar.Size = new(size.X * _building.Health / _building.MaxHealth, 4); DrawRect(bar, Colors.LimeGreen);
    }
    public override void _ExitTree()
    {
        if (!GodotObject.IsInstanceValid(_building)) return;
        _building.PresentationChanged -= QueueRedraw;
        _building.Placed -= QueueRedraw; _building.SelectionChanged -= QueueRedraw;
        _building.HealthChanged -= OnHealthChanged; _building.Detached -= OnDetached;
    }
}
