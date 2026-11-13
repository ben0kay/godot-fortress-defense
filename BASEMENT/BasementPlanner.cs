// Draws the standalone floor planner and handles its mouse, camera and toolbar input.
using Godot;
using System;
using System.IO;
using System.Text.Json;
using static godotfortressdefense.Basement.BasementLayout;

namespace godotfortressdefense.Basement;

public partial class BasementPlanner : Node2D
{
    private const float Tile = 64;
    private const float HeaderHeight = 120;
    private static readonly Color Background = new("#151e2b");
    private static readonly Color FloorColor = new("#29384a");
    private static readonly Color GridColor = new("#3b4c60");
    private static readonly Color WallColor = new("#d4dfe9");
    private static readonly Color DoorColor = new("#64d5ee");
    private static readonly Color ValidColor = new("#66da9e");
    private static readonly Color InvalidColor = new("#ff777f");

    private Layout _layout = Defaults();
    private Camera2D _camera;
    private Label _selection;
    private Label _status;
    private bool _grid = true;
    private bool _dragging;
    private bool _panning;
    private bool _valid;
    private int _item = -1;
    private int _door = -1;
    private Vector2 _grab;
    private float _doorGrabOffset;
    private Placement _candidate;
    private Door _doorCandidate;

    // Set up local controls and frame both rooms and the tray.
    public override void _Ready()
    {
        _camera = GetNode<Camera2D>("Camera2D");
        _selection = GetNode<Label>("Interface/Panel/Margin/Toolbar/Selection");
        _status = GetNode<Label>("Interface/Panel/Margin/Toolbar/Actions/Status");
        HBoxContainer actions = GetNode<HBoxContainer>("Interface/Panel/Margin/Toolbar/Actions");
        actions.GetNode<Button>("Save").Pressed += SaveLayout;
        actions.GetNode<Button>("Load").Pressed += LoadLayout;
        actions.GetNode<Button>("Reset").Pressed += ResetLayout;
        actions.GetNode<Button>("Fit").Pressed += FitView;
        actions.GetNode<CheckButton>("Grid").Toggled += enabled =>
        {
            _grid = enabled;
            QueueRedraw();
        };

        GetViewport().SizeChanged += FitView;
        FitView();
        UpdateSelection();
    }

    // Unsubscribe from the viewport, which can outlive this scene.
    public override void _ExitTree() => GetViewport().SizeChanged -= FitView;

    #region Input

    // Continue an active gesture over UI controls so mouse release always ends it.
    public override void _Input(InputEvent input)
    {
        if (!_dragging && !_panning)
            return;

        if (input is InputEventMouseMotion motion)
        {
            if (_panning)
                _camera.Position -= motion.Relative / _camera.Zoom;
            else
                UpdatePreview();
        }
        else if (input is InputEventMouseButton button && !button.Pressed &&
                 (_panning ? button.ButtonIndex == MouseButton.Middle : button.ButtonIndex == MouseButton.Left))
        {
            if (_dragging)
                FinishDrag(button.Position.Y >= HeaderHeight);
            _panning = false;
        }
        else
        {
            return;
        }
        GetViewport().SetInputAsHandled();
    }

    // Start gestures only after the toolbar has had its chance to handle input.
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode == Key.R && _item >= 0)
                RotateItem();
            else if (key.Keycode == Key.Escape)
                CancelGesture();
            else
                return;
        }
        else if (input is InputEventMouseButton { Pressed: true } button && !_dragging && !_panning)
        {
            switch (button.ButtonIndex)
            {
                case MouseButton.Left: StartDrag(GetLocalMousePosition() / Tile); break;
                case MouseButton.Middle: _panning = true; break;
                case MouseButton.WheelUp: ZoomAtCursor(1.15f); break;
                case MouseButton.WheelDown: ZoomAtCursor(1f / 1.15f); break;
                default: return;
            }
        }
        else
        {
            return;
        }
        GetViewport().SetInputAsHandled();
    }

    // Pick door openings before furniture so their wall handles remain accessible.
    private void StartDrag(Vector2 mouse)
    {
        _item = -1;
        _door = -1;
        float tolerance = 10 / (_camera.Zoom.X * Tile);
        for (int i = 0; i < _layout.Doors.Length; i++)
        {
            Door door = _layout.Doors[i];
            Wall wall = Walls[door.WallIndex];
            Vector2 relative = mouse - wall.At(door.Offset);
            float along = relative.Dot(wall.Direction);
            if (along >= -tolerance && along <= DoorWidth + tolerance &&
                Mathf.Abs(relative.Cross(wall.Direction)) <= tolerance)
            {
                _door = i;
                _doorGrabOffset = Mathf.Clamp(along, 0, DoorWidth);
                break;
            }
        }

        for (int i = 0; _door < 0 && i < Items.Length; i++)
        {
            Rect2 bounds = DisplayFootprint(i);
            if (!bounds.HasPoint(mouse))
                continue;
            _item = i;
            _candidate = _layout.Items[i];
            _grab = mouse - bounds.Position;
            break;
        }

        _dragging = _item >= 0 || _door >= 0;
        if (_dragging)
        {
            SetStatus("");
            UpdatePreview();
        }
        UpdateSelection();
        QueueRedraw();
    }

    // Keep candidate state separate until the gesture is committed.
    private void UpdatePreview()
    {
        Vector2 mouse = GetLocalMousePosition() / Tile;
        if (_item >= 0)
        {
            Vector2 position = (mouse - _grab).Snapped(Vector2.One * 0.25f);
            _candidate = new(position.X, position.Y, _candidate.Rotated, true);
            _valid = CanPlace(_layout, _item, _candidate);
        }
        else
        {
            _doorCandidate = FindDoorCandidate(mouse, _doorGrabOffset, out float distance);
            _valid = distance <= 0.45f && CanPlaceDoor(_layout, _door, _doorCandidate);
        }
        QueueRedraw();
    }

    // Project the grabbed point onto the closest wall long enough to host a door.
    private static Door FindDoorCandidate(Vector2 mouse, float grabOffset, out float distance)
    {
        distance = float.PositiveInfinity;
        Door candidate = default;
        for (int i = 0; i < Walls.Length; i++)
        {
            Wall wall = Walls[i];
            if (wall.Length < DoorWidth)
                continue;
            float along = (mouse - wall.Start).Dot(wall.Direction);
            float gap = mouse.DistanceTo(wall.At(Mathf.Clamp(along, 0, wall.Length)));
            if (gap >= distance)
                continue;
            distance = gap;
            candidate = new(i, Mathf.Snapped(along - grabOffset, 0.5f));
        }
        return candidate;
    }

    // Release commits a valid candidate and leaves the previous layout intact otherwise.
    private void FinishDrag(bool overCanvas)
    {
        UpdatePreview();
        if (_valid && overCanvas)
        {
            if (_item >= 0)
                _layout.Items[_item] = _candidate;
            else
                _layout.Doors[_door] = _doorCandidate;
            SetStatus("");
        }
        else
        {
            SetStatus("Placement cancelled.");
        }
        _dragging = false;
        UpdateSelection();
        QueueRedraw();
    }

    // Rotate around the item's center and snap its new top-left position.
    private void RotateItem()
    {
        Placement current = _dragging ? _candidate : _layout.Items[_item];
        Rect2 before = _dragging ? Footprint(_item, current) : DisplayFootprint(_item);
        Placement rotated = current with { Rotated = !current.Rotated };
        Vector2 size = Footprint(_item, rotated).Size;
        if (_dragging)
        {
            _grab += (size - before.Size) / 2;
            _candidate = rotated;
            UpdatePreview();
        }
        else
        {
            Vector2 position = (before.GetCenter() - size / 2).Snapped(Vector2.One * 0.25f);
            rotated = rotated with { X = position.X, Y = position.Y };
            if (!current.Placed || CanPlace(_layout, _item, rotated))
            {
                _layout.Items[_item] = rotated;
                SetStatus("");
            }
            else
            {
                SetStatus("Rotation blocked.");
            }
        }
        UpdateSelection();
        QueueRedraw();
    }

    // Cancel on Escape or when the window loses focus during a gesture.
    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusOut && (_dragging || _panning))
            CancelGesture();
    }

    private void CancelGesture()
    {
        _dragging = false;
        _panning = false;
        UpdateSelection();
        QueueRedraw();
    }

    // Frame the complete plan below the fixed toolbar.
    private void FitView()
    {
        Vector2 viewport = GetViewportRect().Size;
        float zoom = Mathf.Max(0.05f, Mathf.Min((viewport.X - 48) / (Tray.End.X * Tile),
            (viewport.Y - HeaderHeight - 48) / (Tray.End.Y * Tile)));
        _camera.Zoom = Vector2.One * zoom;
        _camera.Position = Tray.End * Tile / 2 - Vector2.Down * HeaderHeight / (2 * zoom);
        _camera.ForceUpdateScroll();
    }

    // Preserve the world position under the mouse when changing zoom.
    private void ZoomAtCursor(float factor)
    {
        Vector2 before = GetLocalMousePosition();
        _camera.Zoom = Vector2.One * Mathf.Clamp(_camera.Zoom.X * factor, 0.05f, 3);
        _camera.ForceUpdateScroll();
        _camera.Position += before - GetLocalMousePosition();
        _camera.ForceUpdateScroll();
    }

    #endregion

    #region Drawing

    // Redraw only when the plan, grid or drag preview changes.
    public override void _Draw()
    {
        DrawRect(new Rect2(-100000, -100000, 200000, 200000), Background);
        DrawRoom(MainRoom);
        DrawRoom(Annex);
        DrawRect(ToPixels(Tray), new Color("#212c3b"));
        DrawRect(ToPixels(Tray), GridColor, false, 2);
        DrawCentered("FURNITURE TRAY", new Rect2(Tray.Position, new Vector2(Tray.Size.X, 1.15f)), 22, WallColor);

        for (int i = 0; i < Walls.Length; i++)
            DrawWall(i);
        for (int i = 0; i < Items.Length; i++)
        {
            if (!_dragging || _item != i)
                DrawItem(i, DisplayFootprint(i), new Color(Items[i].Color), _item == i ? WallColor : GridColor);
        }

        if (_dragging && _item >= 0)
        {
            Color tint = _valid ? ValidColor : InvalidColor;
            DrawItem(_item, Footprint(_item, _candidate), new Color(tint, 0.55f), tint);
        }
        if (_dragging && _door >= 0)
        {
            Wall wall = Walls[_doorCandidate.WallIndex];
            DrawLine(wall.At(_doorCandidate.Offset) * Tile,
                wall.At(_doorCandidate.Offset + DoorWidth) * Tile, _valid ? ValidColor : InvalidColor, 8, true);
        }
    }

    // Floor and grid use the same tile-space dimensions as placement.
    private void DrawRoom(Rect2 room)
    {
        DrawRect(ToPixels(room), FloorColor);
        if (!_grid)
            return;

        for (float x = room.Position.X; x <= room.End.X; x++)
            DrawLine(new Vector2(x, room.Position.Y) * Tile, new Vector2(x, room.End.Y) * Tile, GridColor);
        for (float y = room.Position.Y; y <= room.End.Y; y++)
            DrawLine(new Vector2(room.Position.X, y) * Tile, new Vector2(room.End.X, y) * Tile, GridColor);
    }

    // Draw solid sections between sorted door openings, with a threshold and jambs at each cutout.
    private void DrawWall(int wallIndex)
    {
        Wall wall = Walls[wallIndex];
        float start = 0;
        while (true)
        {
            int next = -1;
            for (int i = 0; i < _layout.Doors.Length; i++)
            {
                Door door = _layout.Doors[i];
                if (door.WallIndex == wallIndex && door.Offset >= start &&
                    (next < 0 || door.Offset < _layout.Doors[next].Offset))
                    next = i;
            }

            float end = next < 0 ? wall.Length : _layout.Doors[next].Offset;
            if (end > start)
                DrawLine(wall.At(start) * Tile, wall.At(end) * Tile, WallColor, 5, true);
            if (next < 0)
                break;

            start = end + DoorWidth;
            Color color = _door == next ? Colors.White : DoorColor;
            DrawLine(wall.At(end) * Tile, wall.At(start) * Tile, color, 2, true);
            DrawCircle(wall.At(end) * Tile, 4, color);
            DrawCircle(wall.At(start) * Tile, 4, color);
        }
    }

    // Unplaced pieces use dedicated tray slots with room for either orientation.
    private Rect2 DisplayFootprint(int index)
    {
        Placement item = _layout.Items[index];
        Rect2 bounds = Footprint(index, item);
        if (!item.Placed)
        {
            float side = Mathf.Max(Items[index].Size.X, Items[index].Size.Y);
            bounds.Position = Items[index].Tray + (Vector2.One * side - bounds.Size) / 2;
        }
        return bounds;
    }

    private static Rect2 ToPixels(Rect2 bounds) => new(bounds.Position * Tile, bounds.Size * Tile);

    private void DrawItem(int index, Rect2 bounds, Color fill, Color outline)
    {
        DrawRect(ToPixels(bounds), fill);
        DrawRect(ToPixels(bounds), outline, false, 2);
        DrawCentered(Items[index].Label, bounds, 18, Colors.White);
    }

    // Shrink text to fit narrow pieces and center each line independently.
    private void DrawCentered(string label, Rect2 bounds, int maxSize, Color color)
    {
        Font font = ThemeDB.FallbackFont;
        string[] lines = label.Split('\n');
        Rect2 pixels = ToPixels(bounds);
        int size = maxSize;
        foreach (string line in lines)
        {
            float width = font.GetStringSize(line, fontSize: maxSize).X;
            size = Math.Min(size, (int)(maxSize * Mathf.Max(1, pixels.Size.X - 6) / width));
        }
        size = Math.Max(1, Math.Min(size, (int)((pixels.Size.Y - 4) / lines.Length / 1.25f)));
        float height = font.GetHeight(size);
        Vector2 center = pixels.GetCenter();
        float y = center.Y - lines.Length * height / 2 + font.GetAscent(size);
        foreach (string line in lines)
        {
            float width = font.GetStringSize(line, fontSize: size).X;
            DrawString(font, new Vector2(center.X - width / 2, y), line, fontSize: size, modulate: color);
            y += height;
        }
    }

    #endregion

    #region Toolbar

    // Report the selected footprint in tile units.
    private void UpdateSelection()
    {
        if (_item >= 0)
        {
            Vector2 size = Footprint(_item, _dragging ? _candidate : _layout.Items[_item]).Size;
            _selection.Text = FormattableString.Invariant($"{Items[_item].Label.Replace('\n', ' ')}   {size.X:0.##} × {size.Y:0.##} tiles");
        }
        else
        {
            _selection.Text = _door >= 0 ? $"DOOR {_door + 1}   2.5 tiles wide" : "No item selected";
        }
    }

    private void SetStatus(string message) => _status.Text = message;

    // Save committed positions and report filesystem errors in the toolbar.
    private void SaveLayout()
    {
        try
        {
            Save(_layout);
            SetStatus("Layout saved.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            SetStatus("Could not save the layout.");
        }
    }

    // Replace the layout only if the file is readable and every placement is valid.
    private void LoadLayout()
    {
        try
        {
            ReplaceLayout(Load(), "Layout loaded.");
        }
        catch (FileNotFoundException)
        {
            SetStatus("No saved layout yet.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            SetStatus("Could not read a valid layout.");
        }
    }

    private void ResetLayout() => ReplaceLayout(Defaults(), "Layout reset.");

    // Drop selection and any preview when loading or resetting.
    private void ReplaceLayout(Layout layout, string message)
    {
        _layout = layout;
        _item = -1;
        _door = -1;
        CancelGesture();
        SetStatus(message);
    }

    #endregion
}
