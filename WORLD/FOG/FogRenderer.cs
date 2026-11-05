// One texel per world cell, dirty CPU writes and batched small GPU uploads; no simulation ownership.
using Godot;

public partial class FogRenderer : Node2D
{
    public ShaderMaterial DynamicMaterial { get; private set; }
    private FogOfWar _fog;
    private Image _image;
    private ImageTexture _texture;
    private ShaderMaterial _overlayMaterial;
    private TextureRect _overlay;
    private bool _upload;

    // A shared shader clips dynamic primitives by world position without per-enemy visibility polling.
    public void Initialize(FogOfWar fog)
    {
        _fog = fog;
        _image = Image.CreateEmpty(fog.Dimensions.X, fog.Dimensions.Y, false, Image.Format.Rgba8);
        _image.Fill(new Color(0, 0, 0, 1)); _texture = ImageTexture.CreateFromImage(_image);
        DynamicMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://WORLD/FOG/FogDynamic.gdshader") };
        _overlayMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://WORLD/FOG/FogOverlay.gdshader") };
        Rect2 bounds = fog.Grid.FootprintRect(fog.Grid.Bounds);
        foreach (ShaderMaterial material in new[] { DynamicMaterial, _overlayMaterial })
        {
            material.SetShaderParameter("fog_mask", _texture);
            material.SetShaderParameter("mask_size", (Vector2)fog.Dimensions);
            material.SetShaderParameter("world_size", bounds.Size);
        }
        DynamicMaterial.SetShaderParameter("world_origin", bounds.Position);
        _overlayMaterial.SetShaderParameter("unexplored_opacity", fog.Config.UnexploredOpacity);
        _overlayMaterial.SetShaderParameter("explored_opacity", fog.Config.ExploredOpacity);
        _overlayMaterial.SetShaderParameter("edge_softness", fog.Config.EdgeSoftnessPixels);
        // The parent is a plain Node: this CanvasItem is in world coordinates, above world art, below CanvasLayer HUDs.
        ZIndex = 4000;
        _overlay = new TextureRect { Texture = _texture, Position = bounds.Position, Size = bounds.Size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, MouseFilter = Control.MouseFilterEnum.Ignore, Material = _overlayMaterial };
        AddChild(_overlay);
        fog.VisibilityChanged += OnChanged; fog.PresentationChanged += OnPresentationChanged;
        OnPresentationChanged();
    }
    private void WriteCell(int index)
    {
        FogCellState state = _fog.CellStates.Span[index];
        _image.SetPixel(index % _fog.Dimensions.X, index / _fog.Dimensions.X,
            new Color(state == FogCellState.Visible ? 1 : 0, state != FogCellState.Unexplored ? 1 : 0, 0, 1));
    }
    private void OnChanged(Rect2I area)
    {
        if (!_fog.PresentationEnabled) return;
        foreach (int index in _fog.ChangedCells) WriteCell(index);
        ScheduleUpload();
    }
    // Presentation bypass skips texture writes/uploads; enabling resynchronizes the small mask once.
    private void OnPresentationChanged()
    {
        _overlay.Visible = _fog.PresentationEnabled;
        DynamicMaterial.SetShaderParameter("fog_enabled", _fog.PresentationEnabled);
        _upload = false; SetProcess(false);
        if (!_fog.PresentationEnabled) return;
        for (int index = 0; index < _fog.CellStates.Length; index++) WriteCell(index);
        _texture.Update(_image);
    }
    private void ScheduleUpload()
    {
        if (_fog.Config.RendererUpdateMode == FogMasterConfig.UpdateMode.Immediate) _texture.Update(_image);
        else { _upload = true; SetProcess(true); }
    }
    public override void _Process(double delta)
    { if (_upload) _texture.Update(_image); _upload = false; SetProcess(false); }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_fog)) { _fog.VisibilityChanged -= OnChanged; _fog.PresentationChanged -= OnPresentationChanged; }
        if (GodotObject.IsInstanceValid(_overlay)) { _overlay.Material = null; _overlay.Texture = null; }
        // Dynamic consumers share the material until the world finishes leaving the tree.
        DynamicMaterial?.Dispose(); _overlayMaterial?.Dispose(); _texture?.Dispose(); _image?.Dispose();
    }
}
