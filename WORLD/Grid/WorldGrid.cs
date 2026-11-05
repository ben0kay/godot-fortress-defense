// Sole authority for finite terrain, rectangular building occupancy and topology revisions.
using Godot;
using System;
using System.Collections.Generic;

public partial class WorldGrid : Node2D
{
    [Export] public WorldGridConfig Config { get; set; }
    public Vector2I Dimensions { get; private set; }
    public float CellSize { get; private set; }
    public bool AllowDiagonals { get; private set; }
    public int ClearanceCells { get; private set; }
    public ulong Revision { get; private set; }
    public Rect2I Bounds => new(Vector2I.Zero, Dimensions);
    public event Action<Rect2I> TopologyChanged;
    private bool[] _terrain;
    private bool[] _reservedGround;
    public System.Collections.Generic.IReadOnlyList<Rect2I> TerrainRectangles { get; private set; } = System.Array.Empty<Rect2I>();
    public int TerrainCollisionShapeCount => TerrainRectangles.Count;
    private Building[] _occupants;
    private StaticBody2D _terrainBody;
    private readonly List<CollisionShape2D> _terrainShapes = new();

    // Initialize once; runtime config edits require a new world, never silently erase occupants.
    public override void _Ready()
    {
        Config ??= new WorldGridConfig(); Config.Validate();
        Dimensions = new(Config.MapWidthCells, Config.MapHeightCells); CellSize = Config.CellSize;
        AllowDiagonals = Config.AllowDiagonalMovement; ClearanceCells = Config.GroundClearanceCells;
        _terrain = new bool[Dimensions.X * Dimensions.Y]; _reservedGround = new bool[_terrain.Length];
        _occupants = new Building[_terrain.Length];
        _terrainBody = new StaticBody2D { Name = "TerrainCollision", CollisionLayer = 1, CollisionMask = 0 };
        AddChild(_terrainBody);
    }

    #region Queries
    public bool Contains(Vector2I cell) => Bounds.HasPoint(cell);
    private int Index(Vector2I cell) => cell.Y * Dimensions.X + cell.X;
    public TerrainType TerrainAt(Vector2I cell) => IsTerrain(cell) ? TerrainType.DeadCell : TerrainType.Ground;
    public bool IsBuildable(Vector2I cell) => !IsBlocked(cell) && !_reservedGround[Index(cell)];
    public bool IsTerrain(Vector2I cell) => !Contains(cell) || _terrain[Index(cell)];
    public Building BuildingAt(Vector2I cell) => Contains(cell) ? _occupants[Index(cell)] : null;
    public bool IsBlocked(Vector2I cell) => IsTerrain(cell) || BuildingAt(cell) != null;
    public Vector2I WorldToCell(Vector2 point) => new(Mathf.FloorToInt(ToLocal(point).X / CellSize), Mathf.FloorToInt(ToLocal(point).Y / CellSize));
    public Vector2 CellCenter(Vector2I cell) => ToGlobal(((Vector2)cell + Vector2.One * .5f) * CellSize);
    public Vector2 FootprintCenter(Rect2I area) => ToGlobal(((Vector2)area.Position + (Vector2)area.Size * .5f) * CellSize);

    // Subtraction avoids overflow for an invalid large anchor/size.
    public bool Fits(Rect2I area) => area.Size.X > 0 && area.Size.Y > 0 && area.Position.X >= 0 && area.Position.Y >= 0
        && area.Size.X <= Dimensions.X && area.Size.Y <= Dimensions.Y
        && area.Position.X <= Dimensions.X - area.Size.X && area.Position.Y <= Dimensions.Y - area.Size.Y;

    // A complete preflight means rejected placements never partially reserve cells.
    public bool CanOccupy(Rect2I area)
    {
        if (!Fits(area)) return false;
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++)
                if (!IsBuildable(new(x, y))) return false;
        return true;
    }

    // World-space bounds used by attacks, placement and visuals, independent of sprite dimensions.
    public Rect2 FootprintRect(Rect2I area) => new(ToGlobal((Vector2)area.Position * CellSize), (Vector2)area.Size * CellSize);

    // Conservative swept square around a circle: no tunnelling through new obstacles or corner gaps.
    // Only nearby segment cells are inspected. Ignore the attacked target, never another blocker.
    public bool SegmentClear(Vector2 from, Vector2 to, float radius, Building ignored = null)
    {
        Vector2 a = ToLocal(from), b = ToLocal(to);
        Vector2 min = new Vector2(Mathf.Min(a.X, b.X), Mathf.Min(a.Y, b.Y)) - Vector2.One * radius;
        Vector2 max = new Vector2(Mathf.Max(a.X, b.X), Mathf.Max(a.Y, b.Y)) + Vector2.One * radius;
        if (min.X < 0 || min.Y < 0 || max.X >= Dimensions.X * CellSize || max.Y >= Dimensions.Y * CellSize) return false;
        int left = Mathf.Max(0, Mathf.FloorToInt(min.X / CellSize));
        int top = Mathf.Max(0, Mathf.FloorToInt(min.Y / CellSize));
        int right = Mathf.Min(Dimensions.X - 1, Mathf.FloorToInt(max.X / CellSize));
        int bottom = Mathf.Min(Dimensions.Y - 1, Mathf.FloorToInt(max.Y / CellSize));
        for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
            {
                Vector2I cell = new(x, y);
                if (!IsTerrain(cell) && (BuildingAt(cell) == null || BuildingAt(cell) == ignored)) continue;
                Vector2 low = (Vector2)cell * CellSize - Vector2.One * radius;
                Vector2 high = ((Vector2)cell + Vector2.One) * CellSize + Vector2.One * radius;
                if (IntersectsBox(a, b, low, high)) return false;
            }
        return true;
    }

    // Airborne shots still respect finite world bounds, without inspecting any terrain/building cell.
    public float FirstWorldBoundaryFraction(Vector2 from, Vector2 to, float radius)
        => BoundaryFractionLocal(ToLocal(from), ToLocal(to), radius);
    private float BoundaryFractionLocal(Vector2 a, Vector2 b, float radius)
    {
        Vector2 delta = b - a;
        Vector2 low = Vector2.One * radius, high = (Vector2)Dimensions * CellSize - Vector2.One * radius;
        if (a.X < low.X || a.Y < low.Y || a.X > high.X || a.Y > high.Y) return 0;
        if (b.X >= low.X && b.Y >= low.Y && b.X <= high.X && b.Y <= high.Y) return float.PositiveInfinity;
        float enter = 0, exit = 1;
        Clip(a.X, delta.X, low.X, high.X, ref enter, ref exit);
        Clip(a.Y, delta.Y, low.Y, high.Y, ref enter, ref exit);
        return exit;
    }

    // First obstacle entry for fast projectiles, using the same solids as movement/collision.
    // PositiveInfinity means no contact; source footprint may be ignored during barrel exit.
    public float FirstObstacleFraction(Vector2 from, Vector2 to, float radius, Building ignored = null)
        => FirstObstacleFraction(from, to, radius, out _, ignored);

    // Hostile and friendly shots share the same earliest solid hit; report a building only if it precedes terrain/bounds.
    public float FirstObstacleFraction(Vector2 from, Vector2 to, float radius, out Building hitBuilding, Building ignored = null)
    {
        hitBuilding = null;
        Vector2 a = ToLocal(from), b = ToLocal(to), delta = b - a;
        float best = BoundaryFractionLocal(a, b, radius);
        if (best == 0) return 0;
        float enter, exit;
        int left = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.X, b.X) - radius) / CellSize), 0, Dimensions.X - 1);
        int right = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.X, b.X) + radius) / CellSize), 0, Dimensions.X - 1);
        int top = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.Y, b.Y) - radius) / CellSize), 0, Dimensions.Y - 1);
        int bottom = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.Y, b.Y) + radius) / CellSize), 0, Dimensions.Y - 1);
        for (int y = top; y <= bottom; y++) for (int x = left; x <= right; x++)
        {
            Vector2I cell = new(x, y);
            if (!IsTerrain(cell) && (BuildingAt(cell) == null || BuildingAt(cell) == ignored)) continue;
            Vector2 low = (Vector2)cell * CellSize - Vector2.One * radius;
            Vector2 high = ((Vector2)cell + Vector2.One) * CellSize + Vector2.One * radius;
            enter = 0; exit = 1;
            if (Clip(a.X, delta.X, low.X, high.X, ref enter, ref exit) && Clip(a.Y, delta.Y, low.Y, high.Y, ref enter, ref exit)
                && (enter < best || (enter == best && IsTerrain(cell))))
            { best = enter; hitBuilding = IsTerrain(cell) ? null : BuildingAt(cell); }
        }
        return best;
    }

    // Slab segment/AABB intersection; a tangent is conservatively blocked.
    private static bool IntersectsBox(Vector2 a, Vector2 b, Vector2 low, Vector2 high)
    {
        float enter = 0, exit = 1; Vector2 delta = b - a;
        return Clip(a.X, delta.X, low.X, high.X, ref enter, ref exit)
            && Clip(a.Y, delta.Y, low.Y, high.Y, ref enter, ref exit);
    }
    private static bool Clip(float origin, float delta, float low, float high, ref float enter, ref float exit)
    {
        if (Mathf.Abs(delta) < .00001f) return origin >= low && origin <= high;
        float a = (low - origin) / delta, b = (high - origin) / delta;
        enter = Mathf.Max(enter, Mathf.Min(a, b)); exit = Mathf.Min(exit, Mathf.Max(a, b));
        return enter <= exit;
    }
    #endregion

    #region Mutations
    // Placement owns lifecycle; one footprint edit publishes one coherent dirty rectangle.
    internal bool TryOccupy(Building building, Rect2I area)
    {
        if (building == null || !CanOccupy(area)) return false;
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++) _occupants[Index(new(x, y))] = building;
        Publish(area); return true;
    }

    // Release the stored rectangle and only cells still belonging to this instance.
    internal void Release(Building building, Rect2I area)
    {
        if (!Fits(area)) return;
        bool changed = false;
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++)
            {
                int index = Index(new(x, y));
                if (_occupants[index] != building) continue;
                _occupants[index] = null; changed = true;
            }
        if (changed) Publish(area);
    }

    // Reserving a player marker prevents construction but never blocks movement/navigation.
    public bool TryReserveGround(Rect2I area)
    {
        if (!CanOccupy(area)) return false;
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++) _reservedGround[Index(new(x, y))] = true;
        return true;
    }

    // Startup bulk commit: one owned terrain copy, collision rebuild and topology notification.
    public bool TryApplyTerrain(bool[] solid)
    {
        if (solid == null || solid.Length != _terrain.Length) return false;
        for (int i = 0; i < solid.Length; i++)
            if (_occupants[i] != null || _reservedGround[i]) return false;
        _terrain = (bool[])solid.Clone(); RebuildTerrainCollision(); Publish(Bounds); return true;
    }

    // Small debug edits still use the same logical cells; buildings never trigger terrain mesh rebuilds.
    public bool TrySetTerrain(Rect2I area, bool solid)
    {
        if (!Fits(area)) return false;
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++)
            {
                int index = Index(new(x, y));
                if (_occupants[index] != null || (solid && _reservedGround[index])) return false;
            }
        bool changed = false;
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++)
            {
                int index = Index(new(x, y));
                if (_terrain[index] == solid) continue;
                _terrain[index] = solid; changed = true;
            }
        if (changed) { RebuildTerrainCollision(); Publish(area); }
        return true;
    }

    // One passive shape per merged rectangle, never one scene/script/body per cell.
    private void RebuildTerrainCollision()
    {
        foreach (CollisionShape2D shape in _terrainShapes)
        { shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true); shape.QueueFree(); }
        _terrainShapes.Clear(); TerrainRectangles = global::TerrainRectangles.Build(_terrain, Dimensions);
        foreach (Rect2I rect in TerrainRectangles)
        {
            var shape = new CollisionShape2D
            {
                Position = ((Vector2)rect.Position + (Vector2)rect.Size * .5f) * CellSize,
                Shape = new RectangleShape2D { Size = (Vector2)rect.Size * CellSize }
            };
            _terrainShapes.Add(shape); _terrainBody.AddChild(shape);
        }
    }

    // Subscribers update derived navigation before later consumers redraw/query it.
    private void Publish(Rect2I area) { Revision++; TopologyChanged?.Invoke(area); }
    #endregion
}
