// Arrow silhouette and brief attack/hit highlight; no idle animation polling.
using Godot;

public partial class EnemyVectorVisual : Node2D
{
    private Enemy _enemy;
    private float _flash;
    public bool CosmeticDormant { get; private set; }

    // Hidden cosmetics discard past flashes; waking redraws current HP/state without replaying events.
    public void SetCosmeticDormant(bool dormant)
    {
        if (CosmeticDormant == dormant) return;
        CosmeticDormant = dormant; _flash = 0; SetProcess(false);
        if (!dormant) QueueRedraw();
    }
    public override void _Ready()
    { _enemy = GetParent<Enemy>(); _enemy.VisualChanged += OnChanged; SetProcess(false); }
    private void OnChanged()
    { if (CosmeticDormant) return; _flash = .12f; SetProcess(true); QueueRedraw(); }
    public override void _Process(double delta)
    {
        if (CosmeticDormant) { SetProcess(false); return; }
        _flash = Mathf.Max(0, _flash - (float)delta); QueueRedraw(); if (_flash == 0) SetProcess(false);
    }
    public override void _Draw()
    {
        if (_enemy == null || CosmeticDormant) return;
        float r = _enemy.Radius;
        Color color = _flash > 0 ? Colors.White : _enemy.IsBreaching ? Colors.Orange : new Color(1, .25f, .32f);
        Vector2[] arrow = { new(r * 1.5f, 0), new(-r, -r), new(-r * .45f, 0), new(-r, r) };
        if (_enemy.IsFlying)
        {
            Color air = _flash > 0 ? Colors.White : new Color(.3f, .85f, 1);
            DrawCircle(new(4, 7), r * .8f, new Color(0, 0, 0, .25f));
            Vector2[] wings = { new(r * 1.45f, 0), new(-r * .75f, -r * 1.3f), new(-r * .35f, 0), new(-r * .75f, r * 1.3f) };
            DrawColoredPolygon(wings, air);
            DrawLine(new(-r * .35f, 0), new(r * .8f, 0), Colors.White, 2, true);
            DrawCircle(new(-r * .45f, 0), r * .25f, new Color(.06f, .2f, .3f));
        }
        else if (_enemy.IsRanged)
        {
            color = _flash > 0 ? Colors.White : new Color(1, .6f, .12f);
            DrawColoredPolygon(new Vector2[] { new(r, 0), new(-r, -r * 1.15f), new(-r * .65f, 0), new(-r, r * 1.15f) }, color);
            DrawLine(new(r * .15f, 0), new(r * 1.7f, 0), color.Lightened(.25f), 4, true);
        }
        else DrawColoredPolygon(arrow, color);
        if (_enemy.Health < _enemy.MaxHealth) DrawArc(Vector2.Zero, r + 4, -.5f * Mathf.Pi,
            -.5f * Mathf.Pi + Mathf.Tau * _enemy.Health / _enemy.MaxHealth, 16, Colors.LimeGreen, 2);
    }
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_enemy)) _enemy.VisualChanged -= OnChanged; }
}
