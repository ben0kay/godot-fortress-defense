// Reusable free/follow Camera2D; local adaptation of the reference RTS camera's cursor zoom and viewport-aware bounds.
using Godot;
using System;

public partial class BattlefieldCamera : Camera2D
{
    [ExportGroup("Zoom")]
    [Export] public bool StartFullMap { get; set; } = true;
    [Export] public float StartingZoom { get; set; } = .5f;
    [Export] public float MinimumZoom { get; set; } = .05f;
    [Export] public float MaximumZoom { get; set; } = 2;
    [Export] public float ZoomStep { get; set; } = 1.15f;
    [Export] public float FollowZoom { get; set; } = .8f;
    [Export] public Vector2 FramePadding { get; set; } = new(64, 120);
    [ExportGroup("Movement / Bounds")]
    [Export] public MouseButton PanButton { get; set; } = MouseButton.Middle;
    [Export] public float KeyboardPanSpeed { get; set; } = 900;
    [Export] public float PanSensitivity { get; set; } = 1;
    [Export] public float FollowSmoothing { get; set; } = 10;
    [Export] public Vector2 FollowOffset { get; set; }
    [Export] public bool KeepInsideWorld { get; set; } = true;
    [Export] public Key ToggleKey { get; set; } = Key.C;
    [Export] public Key FullMapKey { get; set; } = Key.Home;
    public bool IsFollowing { get; private set; }
    public Func<bool> InputBlocked { get; set; }
    public Func<bool> PanAllowed { get; set; }
    public event Action ModeChanged;
    public event Action<string> Notice;
    private ICombatTarget _target;
    private Rect2 _bounds;
    private bool _initialized, _dragging, _fullMap;
    private Vector2 _freePosition;
    private float _freeZoom;

    public override void _Ready() => SetProcess(false);

    // Explicit bounds and target injection allow reuse without any player/level scene-tree dependency.
    public void Initialize(Rect2 bounds, ICombatTarget target = null)
    {
        if (_initialized || !float.IsFinite(MinimumZoom) || !float.IsFinite(MaximumZoom) || MinimumZoom <= 0 || MaximumZoom < MinimumZoom
            || !float.IsFinite(StartingZoom) || StartingZoom <= 0 || !float.IsFinite(FollowZoom) || FollowZoom <= 0
            || !float.IsFinite(ZoomStep) || ZoomStep <= 1 || !float.IsFinite(FollowSmoothing) || FollowSmoothing < 0
            || !float.IsFinite(KeyboardPanSpeed) || KeyboardPanSpeed <= 0
            || !float.IsFinite(PanSensitivity) || PanSensitivity <= 0 || bounds.Size.X <= 0 || bounds.Size.Y <= 0)
            throw new InvalidOperationException("Invalid battlefield camera settings/bounds.");
        _bounds = bounds; _initialized = true; BindTarget(target); MakeCurrent();
        Zoom = Vector2.One * Mathf.Clamp(StartingZoom, MinimumZoom, MaximumZoom);
        if (StartFullMap) FrameWorld(); else GlobalPosition = ClampPosition(bounds.GetCenter());
        GetViewport().SizeChanged += OnViewportResized;
    }
    public void BindTarget(ICombatTarget target)
    {
        if (_target != null && GodotObject.IsInstanceValid(_target.TargetNode)) _target.TargetLost -= OnTargetLost;
        _target = CombatTarget.IsAlive(target) ? target : null;
        if (_target != null) _target.TargetLost += OnTargetLost;
        if (_target == null && IsFollowing) ReturnToFree();
    }

    // C never clears enemies; no-target toggles are harmless and surfaced through an optional notice callback.
    public void ToggleFollow()
    {
        if (IsFollowing) { ReturnToFree(); return; }
        if (!CombatTarget.IsAlive(_target)) { Notice?.Invoke("Free camera — no living player to follow"); return; }
        _freePosition = GlobalPosition; _freeZoom = Zoom.X; _fullMap = false; _dragging = false;
        IsFollowing = true; Zoom = Vector2.One * Mathf.Clamp(FollowZoom, MinimumZoom, MaximumZoom);
        SetProcess(true); ModeChanged?.Invoke(); Notice?.Invoke("Camera: Player Follow (C: Free Roam)");
    }
    private void ReturnToFree()
    {
        IsFollowing = false; _dragging = false;
        Zoom = Vector2.One * Mathf.Clamp(_freeZoom, MinimumZoom, MaximumZoom); GlobalPosition = ClampPosition(_freePosition);
        SetProcess(true); ModeChanged?.Invoke(); Notice?.Invoke("Camera: Free Roam (C: follow; Home: full map)");
    }
    private void OnTargetLost() => BindTarget(null);

    // Free Roam only processes while panning with the keyboard.
    public override void _Process(double delta)
    {
        if (!IsFollowing) { PanWithKeyboard(delta); return; }
        if (!CombatTarget.IsAlive(_target)) { ReturnToFree(); return; }
        float weight = FollowSmoothing <= 0 ? 1 : 1 - Mathf.Exp(-FollowSmoothing * (float)delta);
        GlobalPosition = ClampPosition(GlobalPosition.Lerp(_target.TargetNode.GlobalPosition + FollowOffset, weight));
    }
    private void PanWithKeyboard(double delta)
    {
        if ((InputBlocked?.Invoke() ?? false) || GetViewport().GuiGetFocusOwner() != null)
        { SetProcess(false); return; }

        Vector2 direction = new(
            (Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0),
            (Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0));
        if (direction == Vector2.Zero) { SetProcess(false); return; }

        Vector2 next = ClampPosition(GlobalPosition + direction.Normalized() * (KeyboardPanSpeed * (float)delta) / Zoom.X);
        if (next == GlobalPosition) return;
        GlobalPosition = next;
        _fullMap = false;
    }

    internal static bool IsPanKey(Key key) => key is Key.Up or Key.Down or Key.Left or Key.Right;

    public void FrameWorld()
    {
        if (IsFollowing) ReturnToFree();
        Vector2 size = GetViewportRect().Size - FramePadding;
        Zoom = Vector2.One * Mathf.Clamp(Mathf.Min(Mathf.Max(1, size.X) / _bounds.Size.X, Mathf.Max(1, size.Y) / _bounds.Size.Y), MinimumZoom, MaximumZoom);
        GlobalPosition = _bounds.GetCenter(); _fullMap = true; _dragging = false;
    }
    private void OnViewportResized() { if (_fullMap) FrameWorld(); else GlobalPosition = ClampPosition(GlobalPosition); }

    // Reserve global camera keys even when a debug control has focus; GUI mouse input remains GUI-owned.
    public override void _Input(InputEvent input)
    {
        if (!_initialized) return;
        if (input is InputEventKey { Pressed: true, Echo: false } key && (key.Keycode == ToggleKey || key.Keycode == FullMapKey))
        {
            if (key.Keycode == ToggleKey) ToggleFollow(); else FrameWorld();
            GetViewport().SetInputAsHandled();
        }
        if (!IsFollowing && input is InputEventKey arrow && IsPanKey(arrow.PhysicalKeycode == Key.None ? arrow.Keycode : arrow.PhysicalKeycode))
            SetProcess(true);
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == PanButton && !mouse.Pressed) _dragging = false;
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_initialized || (InputBlocked?.Invoke() ?? false) || GetViewport().GuiGetHoveredControl() != null)
        { _dragging = false; return; }
        if (input is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == PanButton && !IsFollowing && (PanAllowed?.Invoke() ?? true))
            { _dragging = mouse.Pressed; GetViewport().SetInputAsHandled(); }
            if (mouse.Pressed && (mouse.ButtonIndex == MouseButton.WheelUp || mouse.ButtonIndex == MouseButton.WheelDown))
            {
                Vector2 before = GetGlobalMousePosition();
                Zoom = Vector2.One * Mathf.Clamp(Zoom.X * (mouse.ButtonIndex == MouseButton.WheelUp ? ZoomStep : 1 / ZoomStep), MinimumZoom, MaximumZoom);
                ForceUpdateScroll();
                if (!IsFollowing) GlobalPosition = ClampPosition(GlobalPosition + before - GetGlobalMousePosition());
                _fullMap = false; ForceUpdateScroll(); GetViewport().SetInputAsHandled();
            }
        }
        if (input is InputEventMouseMotion motion && _dragging)
        {
            if (!Input.IsMouseButtonPressed(PanButton)) { _dragging = false; return; }
            GlobalPosition = ClampPosition(GlobalPosition - motion.Relative / Zoom * PanSensitivity);
            _fullMap = false; GetViewport().SetInputAsHandled();
        }
    }

    // When zoomed beyond the world extent, center that axis rather than clamping against inverted limits.
    private Vector2 ClampPosition(Vector2 position)
    {
        if (!KeepInsideWorld) return position;
        Vector2 half = GetViewportRect().Size * .5f / Zoom, center = _bounds.GetCenter();
        return new(_bounds.Size.X <= half.X * 2 ? center.X : Mathf.Clamp(position.X, _bounds.Position.X + half.X, _bounds.End.X - half.X),
            _bounds.Size.Y <= half.Y * 2 ? center.Y : Mathf.Clamp(position.Y, _bounds.Position.Y + half.Y, _bounds.End.Y - half.Y));
    }
    public override void _Notification(int what)
    {
        if (what != NotificationWMWindowFocusOut && what != NotificationApplicationFocusOut) return;
        _dragging = false;
        if (!IsFollowing) SetProcess(false);
    }
    public override void _ExitTree()
    {
        if (_target != null && GodotObject.IsInstanceValid(_target.TargetNode)) _target.TargetLost -= OnTargetLost;
        if (_initialized) GetViewport().SizeChanged -= OnViewportResized;
        InputBlocked = null; PanAllowed = null; _target = null;
    }
}
