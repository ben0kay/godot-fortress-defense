// Shared level build-input/selection integration; BM0 retains purchase and occupancy authority.
using Godot;

public partial class LevelController
{
    // One placement selection owner for both catalogue UI and legacy quick-select keys.
    public bool RequestPlacement(string id)
    {
        if (!_placement.IsAvailable(id)) { _notice = "Building locked or unavailable"; return false; }
        SelectedBuildingId = id; _painting = false; _lastPaint = new(-1, -1);
        Select(null); OnPlacementAvailabilityChanged();
        _notice = $"Placing {_placement.Catalogue[id].Definition.DisplayName} — LMB/drag builds; Esc/RMB cancels";
        PlacementSelectionChanged?.Invoke(); RefreshDisplay(); return true;
    }
    // Cancellation clears input state and the ghost, never submitting a purchase.
    public void CancelPlacement()
    {
        SelectedBuildingId = null; _painting = false; _panning = false; SetProcess(false);
        _buildMenu.Preview.Hide(); _notice = "Placement cancelled — Ctrl opens building menu";
        PlacementSelectionChanged?.Invoke(); RefreshDisplay();
    }
    // Browsing suspends world placement while preserving its selected definition.
    public void SetBuildMenuOpen(bool open)
    {
        _buildMenuOpen = open; _painting = false; _panning = false;
        UpdatePlacementActivity();
    }
    private void OnPlacementAvailabilityChanged()
    {
        _placementAffordable = SelectedBuildingId != null && _placement.IsAvailable(SelectedBuildingId)
            && (FreeConstruction || Resources.CanAfford(_placement.Catalogue[SelectedBuildingId].Definition.Cost));
        if (_buildMenu != null) UpdatePlacementActivity();
    }
    private void UpdatePlacementActivity()
    {
        bool active = SelectedBuildingId != null && !_buildMenuOpen;
        SetProcess(active);
        if (active) RefreshPlacementPreview(); else _buildMenu.Preview.Hide();
    }

    // Only the active ghost updates: actor overlap can change beneath a stationary cursor.
    public override void _Process(double delta) => RefreshPlacementPreview();
    private void RefreshPlacementPreview()
    {
        if (SelectedBuildingId == null || _buildMenuOpen) return;
        _hover = _grid.WorldToCell(GetGlobalMousePosition());
        var definition = _placement.Catalogue[SelectedBuildingId].Definition;
        bool show = _grid.Contains(_hover) && GetViewport().GuiGetHoveredControl() == null;
        bool valid = show && _placementAffordable && _placement.CanPlace(new Rect2I(_hover, definition.Footprint));
        _buildMenu.Preview.Refresh(_grid, definition, _hover, show, valid);
    }
    private void PaintBuilding()
    {
        if (SelectedBuildingId == null || _buildMenuOpen || _lastPaint == _hover) return;
        _lastPaint = _hover;
        if (!_placement.IsAvailable(SelectedBuildingId)) { _notice = "Building locked or unavailable"; return; }
        var entry = _placement.Catalogue[SelectedBuildingId];
        Building building; string failure = "Placement blocked (free debug)";
        bool placed = FreeConstruction
            ? _placement.TryPlaceFree(entry.Scene, _hover, out building)
            : _placement.TryPurchaseAndPlace(SelectedBuildingId, _hover, out building, out failure);
        if (placed) { Select(building); _notice = entry.Definition.DisplayName + " placed — continue building or Esc to cancel"; }
        else _notice = failure;
        RefreshPlacementPreview();
    }

    // Global build shortcuts work even with GUI focus; GUI left-clicks remain GUI-only.
    public override void _Input(InputEvent input)
    {
        if (_reloading || _buildMenu == null) return;
        if (input is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.Keycode == Key.Ctrl) { _buildMenu.Toggle(); GetViewport().SetInputAsHandled(); return; }
            if (key.Keycode == Key.Escape && (_buildMenu.IsOpen || SelectedBuildingId != null))
            { CancelPlacement(); _buildMenu.Close(); GetViewport().SetInputAsHandled(); return; }
        }
        if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }
            && (_buildMenu.IsOpen || SelectedBuildingId != null))
        { CancelPlacement(); _buildMenu.Close(); GetViewport().SetInputAsHandled(); return; }
        // A GUI-consumed release/press must never leave a world-paint drag armed.
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse
            && (!mouse.Pressed || GetViewport().GuiGetHoveredControl() != null)) _painting = false;
    }
}
