// Explicit world bindings for visual masking only; registries, combat and AI remain untouched.
using Godot;
using System.Collections.Generic;

public partial class FogPresentation : Node
{
    private FogOfWar _fog;
    private EnemyRegistry _enemies;
    private BuildingRegistry _buildings;
    private Node _projectiles, _level;
    private readonly List<Building> _nearby = new();
    private ShaderMaterial _material;

    // Only initial registry snapshots and lifecycle signals; no global searches or enemy polling.
    public void Initialize(FogOfWar fog, EnemyRegistry enemies, BuildingRegistry buildings, Node projectiles, Node level)
    {
        _fog = fog; _enemies = enemies; _buildings = buildings; _projectiles = projectiles; _level = level;
        _material = fog.GetNode<FogRenderer>("Renderer").DynamicMaterial;
        _enemies.Added += OnEnemyAdded; _buildings.Added += OnBuildingAdded;
        _projectiles.ChildEnteredTree += OnProjectileAdded; _level.ChildEnteredTree += OnLevelChildAdded;
        _fog.VisibilityChanged += OnVisibilityChanged; _fog.PresentationChanged += OnPresentationChanged;
        foreach (Enemy enemy in enemies.Enemies) OnEnemyAdded(enemy);
        foreach (Building building in buildings.Buildings) OnBuildingAdded(building);
        foreach (Node node in projectiles.GetChildren()) OnProjectileAdded(node);
    }
    public void RegisterDynamic(CanvasItem visual) { visual.Material = _material; }
    private void OnEnemyAdded(Enemy enemy)
    {
        CanvasItem visual = enemy.GetNodeOrNull<CanvasItem>("VectorVisual");
        if (visual != null) RegisterDynamic(visual);
    }
    private void OnProjectileAdded(Node node) { if (node is CanvasItem visual) RegisterDynamic(visual); }
    private void OnLevelChildAdded(Node node) { if (node is DestructionPulse visual) RegisterDynamic(visual); }
    private void OnBuildingAdded(Building building)
    {
        _fog.MarkExplored(building.OccupiedArea); UpdateBuilding(building);
        if (building is ProjectileTower)
        {
            // Root draws only live HP/range feedback; child base sprites keep remembered artwork.
            RegisterDynamic(building);
            RegisterDynamic(building.GetNode<Sprite2D>("Visuals/TurretPivot/GunSprite"));
            RegisterDynamic(building.GetNode<Polygon2D>("Visuals/TurretPivot/Muzzle/Flash"));
        }
    }
    private void UpdateBuilding(Building building)
    { building.SetCurrentVisionPresentation(!_fog.PresentationEnabled || _fog.IsVisible(building.GlobalPosition)); }
    private void OnVisibilityChanged(Rect2I area)
    {
        if (!_fog.PresentationEnabled) return;
        _buildings.Query(_fog.Grid.FootprintRect(area), _nearby);
        foreach (Building building in _nearby) UpdateBuilding(building);
        _nearby.Clear();
    }
    private void OnPresentationChanged()
    { foreach (Building building in _buildings.Buildings) UpdateBuilding(building); }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_fog)) { _fog.VisibilityChanged -= OnVisibilityChanged; _fog.PresentationChanged -= OnPresentationChanged; }
        if (GodotObject.IsInstanceValid(_enemies)) _enemies.Added -= OnEnemyAdded;
        if (GodotObject.IsInstanceValid(_buildings))
        {
            _buildings.Added -= OnBuildingAdded;
            foreach (Building building in _buildings.Buildings) building.SetCurrentVisionPresentation(true);
        }
        if (GodotObject.IsInstanceValid(_projectiles)) _projectiles.ChildEnteredTree -= OnProjectileAdded;
        if (GodotObject.IsInstanceValid(_level)) _level.ChildEnteredTree -= OnLevelChildAdded;
        _nearby.Clear(); _material = null;
    }
}
