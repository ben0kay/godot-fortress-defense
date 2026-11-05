// Short-lived cosmetic destruction ring; has no gameplay ownership or collision.
using Godot;

public partial class DestructionPulse : Node2D
{
    public float Radius { get; set; } = 20;
    public Color Tint { get; set; } = Colors.OrangeRed;
    private float _age;
    public override void _Process(double delta)
    { _age += (float)delta; if (_age >= .35f) QueueFree(); else QueueRedraw(); }
    public override void _Draw()
    { DrawArc(Vector2.Zero, Radius * (.5f + _age * 2), 0, Mathf.Tau, 24, new Color(Tint, 1 - _age / .35f), 3, true); }
}
