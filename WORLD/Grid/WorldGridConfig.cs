// Authored finite-grid settings; WorldGrid snapshots these at startup.
using Godot;
using System;

[GlobalClass]
public partial class WorldGridConfig : Resource
{
    [ExportGroup("Finite Map")]
    [Export] public int MapWidthCells { get; set; } = 32;
    [Export] public int MapHeightCells { get; set; } = 20;
    [Export] public float CellSize { get; set; } = 32;
    [ExportGroup("Ground Navigation")]
    [Export] public bool AllowDiagonalMovement { get; set; } = true;
    [Export] public int GroundClearanceCells { get; set; } = 0;

    // Bound memory and iteration before allocating the grid.
    public void Validate()
    {
        if (MapWidthCells < 4 || MapHeightCells < 4 || MapWidthCells > 512 || MapHeightCells > 512
            || !float.IsFinite(CellSize) || CellSize < 4 || CellSize > 256
            || GroundClearanceCells < 0 || GroundClearanceCells > 8)
            throw new InvalidOperationException("Grid requires 4–512 cells per axis, cell size 4–256 and clearance 0–8.");
    }
}
