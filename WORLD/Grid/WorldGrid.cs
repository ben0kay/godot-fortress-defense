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
    private Building[] _occupants;
    private StaticBody2D _terrainBody;
    private readonly Dictionary<Vector2I, CollisionShape2D> _terrainShapes = new();

    // Initialize once; runtime config edits require a new world, never silently erase occupants.
    public override void _Ready()
    {
        Config ??= new WorldGridConfig(); Config.Validate();
        Dimensions = new(Config.MapWidthCells, Config.MapHeightCells); CellSize = Config.CellSize;
        AllowDiagonals = Config.AllowDiagonalMovement; ClearanceCells = Config.GroundClearanceCells;
        _terrain = new bool[Dimensions.X * Dimensions.Y]; _occupants = new Building[_terrain.Length];
        _terrainBody = new StaticBody2D { Name = "TerrainCollision", CollisionLayer = 1, CollisionMask = 0 };
        AddChild(_terrainBody);
    }

    #region Queries
    public bool Contains(Vector2I cell) => Bounds.HasPoint(cell);
    private int Index(Vector2I cell) => cell.Y * Dimensions.X + cell.X;
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
                if (IsBlocked(new(x, y))) return false;
        return true;
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

    // Natural terrain remains distinct from buildings for future traversal policies.
    public bool TrySetTerrain(Rect2I area, bool solid)
    {
        if (!Fits(area)) return false;
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++)
                if (BuildingAt(new(x, y)) != null) return false;
        bool changed = false;
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++)
            {
                Vector2I cell = new(x, y); int index = Index(cell);
                if (_terrain[index] == solid) continue;
                _terrain[index] = solid; changed = true;
                if (solid)
                {
                    var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = Vector2.One * CellSize },
                        Position = ((Vector2)cell + Vector2.One * .5f) * CellSize };
                    _terrainShapes.Add(cell, shape); _terrainBody.AddChild(shape);
                }
                else if (_terrainShapes.Remove(cell, out var removedShape))
                { removedShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true); removedShape.QueueFree(); }
            }
        if (changed) Publish(area);
        return true;
    }

    // Subscribers update derived navigation before later consumers redraw/query it.
    private void Publish(Rect2I area) { Revision++; TopologyChanged?.Invoke(area); }
    #endregion
}
