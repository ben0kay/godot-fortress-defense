// A single world-space footprint/icon ghost; no building instances or gameplay state live here.
using Godot;

public partial class PlacementPreview : Node2D
{
    private Vector2 _pixels;
    private Texture2D _icon;
    private bool _valid;
    private Vector2I _cell = new(int.MinValue, int.MinValue);
    private BuildingDefinition _definition;

    // Called only during active placement; stationary unchanged ghosts do not redraw.
    public void Refresh(WorldGrid grid, BuildingDefinition definition, Vector2I cell, bool show, bool valid)
    {
        Visible = show;
        if (!show) return;
        GlobalPosition = grid.FootprintCenter(new Rect2I(cell, definition.Footprint));
        Vector2 pixels = (Vector2)definition.Footprint * grid.CellSize;
        if (_cell == cell && _definition == definition && _valid == valid && _pixels == pixels) return;
        _cell = cell; _definition = definition; _valid = valid; _pixels = pixels; _icon = definition.Icon; QueueRedraw();
    }

    // Green/red footprint and tinted local icon follow the reference's translucent placement ghost.
    public override void _Draw()
    {
        Color color = _valid ? new Color(.45f, 1, .73f, .75f) : new Color(1, .35f, .31f, .75f);
        Rect2 rect = new(-_pixels * .5f, _pixels);
        DrawRect(rect, new Color(color.R, color.G, color.B, .16f));
        if (_icon != null)
        {
            Vector2 texture = _icon.GetSize();
            float scale = Mathf.Min(_pixels.X / texture.X, _pixels.Y / texture.Y) * .85f;
            DrawTextureRect(_icon, new Rect2(-texture * scale * .5f, texture * scale), false, color);
        }
        DrawRect(rect, color, false, 3);
    }
}
