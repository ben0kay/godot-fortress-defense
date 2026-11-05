// Catalogue-driven category browser; sends selection requests, never owns placement or balances.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BuildMenu : Node
{
    public bool IsOpen => _shell.Visible;
    public PlacementPreview Preview { get; private set; }
    private LevelController _level;
    private Control _shell;
    private Label _credits, _heading, _details, _hint;
    private TextureRect _icon;
    private HBoxContainer _categories, _buttons;
    private readonly Dictionary<string, Button> _tiles = new();
    private BuildingDefinition.BuildingCategory? _category;
    private string _inspected;

    // Explicitly bound by shared level composition after BM0 services are initialized.
    public void Bind(LevelController level)
    {
        _level = level; Preview = GetNode<PlacementPreview>("Preview");
        _shell = GetNode<Control>("HUD/Screen/Menu");
        _hint = GetNode<Label>("HUD/Screen/Hint");
        _credits = _shell.GetNode<Label>("Tray/Body/Header/Credits");
        _heading = _shell.GetNode<Label>("Tray/Body/Heading");
        _categories = _shell.GetNode<HBoxContainer>("Tray/Body/CategoryScroll/Categories");
        _buttons = _shell.GetNode<HBoxContainer>("Tray/Body/Buildings/Buttons");
        _details = _shell.GetNode<Label>("Inspection/Body/Details");
        _icon = _shell.GetNode<TextureRect>("Inspection/Body/Icon");
        foreach (string path in new[] { "Tray", "Inspection" })
            _shell.GetNode<PanelContainer>(path).AddThemeStyleboxOverride("panel", BuildMenuStyle.Box(new Color("#122330"), BuildMenuStyle.Accent));
        foreach (Label label in new[] { _credits, _heading, _details, _hint }) label.AddThemeColorOverride("font_color", BuildMenuStyle.Text);
        _heading.AddThemeColorOverride("font_color", BuildMenuStyle.Accent);
        _shell.GetNode<Label>("Tray/Body/Header/Title").AddThemeColorOverride("font_color", BuildMenuStyle.Accent);
        foreach (var category in Enum.GetValues<BuildingDefinition.BuildingCategory>())
        {
            if (category == BuildingDefinition.BuildingCategory.Core) continue;
            var button = new Button { Text = category.ToString(), ToggleMode = true };
            BuildMenuStyle.Apply(button); button.Pressed += () => ShowCategory(category); _categories.AddChild(button);
        }
        _level.Resources.BalanceChanged += RefreshAvailability;
        _level.Placement.CatalogueChanged += OnCatalogueChanged;
        _level.PlacementSelectionChanged += OnSelectionChanged;
        _shell.Hide(); OnSelectionChanged(); RefreshAvailability();
    }

    // Opening always starts with categories; Ctrl-close resumes the level's existing placement choice.
    public void Toggle()
    {
        bool open = !IsOpen; _shell.Visible = open;
        if (open)
        {
            _category = null; _inspected = null; ClearTiles();
            foreach (Button button in _categories.GetChildren()) button.SetPressedNoSignal(false);
            _heading.Text = "CHOOSE A CATEGORY"; ShowDetails(null); RefreshAvailability();
        }
        else GetViewport().GuiReleaseFocus();
        _level.SetBuildMenuOpen(open);
    }
    public void Close()
    {
        _shell.Hide(); GetViewport().GuiReleaseFocus(); _level.SetBuildMenuOpen(false);
    }

    // Build tiles only on category/catalogue changes, never on every balance update.
    private void ShowCategory(BuildingDefinition.BuildingCategory category)
    {
        _category = category; _inspected = null; ClearTiles();
        foreach (Button button in _categories.GetChildren()) button.SetPressedNoSignal(button.Text == category.ToString());
        foreach (var entry in _level.Placement.Catalogue.Values.OrderBy(entry => entry.Definition.DisplayName, StringComparer.Ordinal))
        {
            var definition = entry.Definition;
            if (definition.Category != category) continue;
            string id = definition.BuildingId;
            var tile = new Button { CustomMinimumSize = new Vector2(210, 115), Icon = definition.Icon,
                ExpandIcon = true, IconAlignment = HorizontalAlignment.Center, VerticalIconAlignment = VerticalAlignment.Top,
                Alignment = HorizontalAlignment.Center, ToggleMode = true };
            tile.AddThemeConstantOverride("icon_max_width", 40); BuildMenuStyle.Apply(tile);
            tile.TooltipText = $"{definition.DisplayName}\n{definition.Description}\nFootprint {definition.Footprint.X} × {definition.Footprint.Y}";
            tile.MouseEntered += () => ShowDetails(id); tile.FocusEntered += () => ShowDetails(id);
            tile.Pressed += () => Choose(id); _tiles.Add(id, tile); _buttons.AddChild(tile);
        }
        _heading.Text = _tiles.Count == 0 ? $"{category.ToString().ToUpperInvariant()} — NO BUILDINGS AVAILABLE YET" : category.ToString().ToUpperInvariant();
        ShowDetails(null); RefreshAvailability();
    }
    private void ClearTiles()
    {
        _tiles.Clear();
        foreach (Node child in _buttons.GetChildren()) { _buttons.RemoveChild(child); child.QueueFree(); }
    }

    // Locked/unaffordable tiles stay visible with a reason and inspectable hover details.
    private void RefreshAvailability()
    {
        _credits.Text = $"CREDITS {_level.Resources.GetBalance(ResourceCost.ResourceId.Credits):N0}"
            + (_level.FreeConstruction ? "  ·  FREE DEBUG" : "");
        foreach (var pair in _tiles)
        {
            var definition = _level.Placement.Catalogue[pair.Key].Definition;
            string state = State(definition);
            pair.Value.Text = $"{definition.DisplayName}\n{Price(definition)}\n{state}";
            pair.Value.Disabled = !_level.Placement.IsAvailable(pair.Key) || (!_level.FreeConstruction && !_level.Resources.CanAfford(definition.Cost));
            pair.Value.SetPressedNoSignal(_level.SelectedBuildingId == pair.Key);
        }
        ShowDetails(_inspected);
    }
    private string State(BuildingDefinition definition) => !_level.Placement.IsAvailable(definition.BuildingId) ? "LOCKED"
        : _level.FreeConstruction ? "FREE DEBUG PLACEMENT"
        : _level.Resources.CanAfford(definition.Cost) ? "READY TO BUILD" : "INSUFFICIENT RESOURCES";
    private static string Price(BuildingDefinition definition)
    {
        if (!ResourceWallet.TryTotals(definition.Cost, out var totals)) return "INVALID PRICE";
        return totals.Count == 0 ? "Free" : string.Join(" + ", totals.Select(pair => $"{pair.Value:N0} {pair.Key}"));
    }
    private void ShowDetails(string id)
    {
        _inspected = id;
        if (id == null || !_level.Placement.Catalogue.TryGetValue(id, out var entry))
        {
            _icon.Texture = null;
            _details.Text = "BUILDING DETAILS\n\nChoose a category, then hover or focus a building to inspect it.\n\nClick an available building to begin repeated placement.";
            return;
        }
        var definition = entry.Definition; _icon.Texture = definition.Icon;
        _details.Text = $"{definition.DisplayName}\n{definition.Category}  ·  {definition.Footprint.X} × {definition.Footprint.Y} cells\n\n{definition.Description}\n\nCost: {Price(definition)}\n{State(definition)}";
    }
    private void Choose(string id)
    {
        if (!_level.RequestPlacement(id)) { RefreshAvailability(); return; }
        Close();
    }
    private void OnCatalogueChanged()
    { if (_category.HasValue) ShowCategory(_category.Value); else RefreshAvailability(); }
    private void OnSelectionChanged()
    {
        _hint.Text = _level.SelectedBuildingId == null ? "CTRL  BUILD MENU  ·  W/T  QUICK SELECT"
            : $"PLACING {_level.SelectedBuildingId.ToUpperInvariant()}  ·  LMB/DRAG BUILD  ·  ESC/RMB CANCEL  ·  CTRL MENU";
        RefreshAvailability();
    }

    // Scene-scoped subscriptions never retain an unloaded level or wallet.
    public override void _ExitTree()
    {
        if (!GodotObject.IsInstanceValid(_level)) return;
        _level.PlacementSelectionChanged -= OnSelectionChanged;
        if (GodotObject.IsInstanceValid(_level.Resources)) _level.Resources.BalanceChanged -= RefreshAvailability;
        if (GodotObject.IsInstanceValid(_level.Placement)) _level.Placement.CatalogueChanged -= OnCatalogueChanged;
    }
}
