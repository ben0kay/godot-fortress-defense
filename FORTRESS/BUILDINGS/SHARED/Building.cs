// Passive universal building root: immutable placement snapshot, health, selection and cleanup.
using Godot;
using System;

public partial class Building : Node2D
{
    [Export] public BuildingDefinition Definition { get; set; }
    public string BuildingId { get; private set; }
    public Vector2I Footprint { get; private set; }
    public Rect2I OccupiedArea { get; private set; }
    public float MaxHealth { get; private set; }
    public float Health { get; private set; }
    public bool IsDestroyed { get; private set; }
    public bool IsPlaced { get; private set; }
    public bool IsSelected { get; private set; }
    public event Action HealthChanged;
    public event Action SelectionChanged;
    public event Action Destroyed;
    public event Action Placed;
    public event Action Detached;
    public Vector2 FootprintPixels { get; private set; }
    private WorldGrid _grid;
    private BuildingRegistry _registry;
    private CollisionShape2D _collision;
    private bool _initialized;

    // Placement initializes before AddChild; inherited scene definitions remain shared authoring data.
    internal void Initialize()
    {
        if (_initialized) throw new InvalidOperationException("Building already initialized.");
        if (Definition == null) throw new InvalidOperationException("Building definition is required.");
        Definition.Validate(); BuildingId = Definition.BuildingId; Footprint = Definition.Footprint;
        MaxHealth = Health = Definition.MaxHealth; _initialized = true;
    }

    // A bare base scene is not self-placing; all insertion goes through the placement service.
    public override void _Ready()
    {
        _collision = GetNode<CollisionShape2D>("FootprintBody/CollisionShape2D");
        SetProcess(false); SetPhysicsProcess(false);
    }

    // Claim once; visual origin and physical rectangle share the exact grid footprint center.
    internal bool TryPlace(WorldGrid grid, BuildingRegistry registry, Vector2I anchor)
    {
        if (!_initialized || !IsInsideTree() || IsPlaced || IsDestroyed || IsQueuedForDeletion()) return false;
        OccupiedArea = new(anchor, Footprint);
        if (!grid.CanOccupy(OccupiedArea)) return false;
        _grid = grid; _registry = registry;
        GlobalPosition = grid.FootprintCenter(OccupiedArea);
        FootprintPixels = (Vector2)Footprint * grid.CellSize;
        _collision.Shape = new RectangleShape2D { Size = FootprintPixels };
        _collision.Disabled = false; IsPlaced = true;
        if (!grid.TryOccupy(this, OccupiedArea)) { Detach(); return false; }
        registry.Register(this); Placed?.Invoke(); return true;
    }

    // Consumers subscribe; selection creates no idle ticks or mandatory UI nodes.
    public void SetSelected(bool selected)
    {
        if (IsSelected == selected) return;
        IsSelected = selected; SelectionChanged?.Invoke();
    }

    // Scalar structural damage only; shields, resistances and weapons are later capabilities.
    public void ApplyDamage(float amount)
    {
        if (!IsPlaced || IsDestroyed || !float.IsFinite(amount) || amount <= 0) return;
        Health = Mathf.Max(0, Health - amount);
        if (Health <= 0) Destroy();
        else HealthChanged?.Invoke();
    }

    // Destruction releases navigation immediately and fires once, before deferred scene deletion.
    public void Destroy()
    {
        if (IsDestroyed) return;
        IsDestroyed = true; Health = 0; Detach(); Hide();
        HealthChanged?.Invoke(); Destroyed?.Invoke(); QueueFree();
    }

    // Ordinary removal also releases occupancy; both paths are idempotent with ExitTree.
    public void Remove() { Detach(); Hide(); QueueFree(); }
    public override void _ExitTree() => Detach();

    // Release the snapshotted rectangle, never a possibly edited Resource footprint.
    private void Detach()
    {
        if (!IsPlaced) return;
        IsPlaced = false;
        if (GodotObject.IsInstanceValid(_collision)) _collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        if (GodotObject.IsInstanceValid(_registry)) _registry.Unregister(this);
        if (GodotObject.IsInstanceValid(_grid)) _grid.Release(this, OccupiedArea);
        _grid = null; _registry = null;
        SetSelected(false); Detached?.Invoke();
    }
}
