// Arrow silhouette and brief attack/hit highlight; no idle animation polling.
using Godot;

public partial class EnemyVectorVisual : Node2D
{
    private Enemy _enemy;
    private float _flash;
    public override void _Ready()
    { _enemy = GetParent<Enemy>(); _enemy.VisualChanged += OnChanged; SetProcess(false); }
    private void OnChanged() { _flash = .12f; SetProcess(true); QueueRedraw(); }
    public override void _Process(double delta)
    { _flash = Mathf.Max(0, _flash - (float)delta); QueueRedraw(); if (_flash == 0) SetProcess(false); }
    public override void _Draw()
    {
        if (_enemy == null) return;
        float r = _enemy.Radius;
        Color color = _flash > 0 ? Colors.White : _enemy.IsBreaching ? Colors.Orange : new Color(1, .25f, .32f);
        Vector2[] arrow = { new(r * 1.5f, 0), new(-r, -r), new(-r * .45f, 0), new(-r, r) };
        DrawColoredPolygon(arrow, color);
        if (_enemy.Health < _enemy.MaxHealth) DrawArc(Vector2.Zero, r + 4, -.5f * Mathf.Pi,
            -.5f * Mathf.Pi + Mathf.Tau * _enemy.Health / _enemy.MaxHealth, 16, Colors.LimeGreen, 2);
    }
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_enemy)) _enemy.VisualChanged -= OnChanged; }
}
