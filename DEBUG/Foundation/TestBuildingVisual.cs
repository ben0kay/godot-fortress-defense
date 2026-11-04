// Event-redrawn prototype artwork; kept out of the universal building scene.
using Godot;

public partial class TestBuildingVisual : Node2D
{
    private Building _building;
    private Vector2 _size;

    // Parent Ready follows children; defer size lookup until placement has completed.
    public override void _Ready()
    {
        _building = GetParent().GetParent<Building>();
        _building.SelectionChanged += QueueRedraw; _building.HealthChanged += QueueRedraw;
        Callable.From(RefreshSize).CallDeferred();
    }

    private void RefreshSize()
    {
        if (!_building.IsPlaced) return;
        _size = ((RectangleShape2D)_building.GetNode<CollisionShape2D>("FootprintBody/CollisionShape2D").Shape).Size;
        QueueRedraw();
    }

    // Artwork matches the footprint but does not define occupancy or collision.
    public override void _Draw()
    {
        if (_size == Vector2.Zero) return;
        Rect2 rect = new(-_size * .5f, _size);
        DrawRect(rect.Grow(-3), new Color(.08f, .24f, .3f));
        DrawRect(rect.Grow(-3), _building.IsSelected ? Colors.Yellow : Colors.Cyan, false, 2);
        DrawLine(rect.Position + new Vector2(5, 8), rect.Position + new Vector2((_size.X - 10) * _building.Health / _building.MaxHealth + 5, 8), Colors.LimeGreen, 3);
    }

    public override void _ExitTree()
    {
        if (!GodotObject.IsInstanceValid(_building)) return;
        _building.SelectionChanged -= QueueRedraw; _building.HealthChanged -= QueueRedraw;
    }
}
