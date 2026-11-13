// Owns the basement geometry, object definitions, placement rules and saved layout.
using Godot;
using System;
using System.IO;
using System.Text.Json;

namespace godotfortressdefense.Basement;

internal static class BasementLayout
{
    internal readonly record struct Furniture(string Label, Vector2 Size, Vector2 Tray, string Color);
    internal readonly record struct Wall(Vector2 Start, Vector2 Direction, float Length)
    {
        public Vector2 At(float offset) => Start + Direction * offset;
    }

    public readonly record struct Placement(float X, float Y, bool Rotated, bool Placed)
    {
        internal Vector2 Position => new(X, Y);
    }

    public readonly record struct Door(int WallIndex, float Offset);

    public sealed record Layout(Placement[] Items, Door[] Doors);

    internal static readonly Rect2 MainRoom = new(0, 0, 11, 13);
    internal static readonly Rect2 Annex = new(11, 0, 4, 5);
    internal static readonly Rect2 Tray = new(16, 0, 10, 16.5f);
    internal const float DoorWidth = 2.5f;
    private const string SavePath = "user://basement_layout.json";

    internal static readonly Furniture[] Items =
    {
        new("FRIDGE", new(2, 2), new(16.5f, 6.75f), "#447eaa"),
        new("SOFA", new(5, 2), new(16.5f, 1.5f), "#725aa5"),
        new("TV\nSTAND", new(1, 4), new(21.75f, 1.5f), "#476c84"),
        new("SPORTS\nAPPARATUS", new(3.5f, 2.5f), new(21.75f, 5.75f), "#9b653a"),
        new("SMALL SHELF", new(2.25f, 1), new(19, 6.75f), "#866844"),
        new("LONG\nCOUNTERTOP", new(1.5f, 7), new(16.5f, 9.25f), "#3d796a"),
        new("SHORT\nCOUNTERTOP", new(1.5f, 2), new(23.75f, 9.5f), "#526f55")
    };

    // The last two segments border the permanent opening from y=1 to y=4.
    internal static readonly Wall[] Walls =
    {
        new(new(0, 0), Vector2.Right, 15),
        new(new(15, 0), Vector2.Down, 5),
        new(new(11, 5), Vector2.Right, 4),
        new(new(11, 5), Vector2.Down, 8),
        new(new(0, 13), Vector2.Right, 11),
        new(new(0, 0), Vector2.Down, 13),
        new(new(11, 0), Vector2.Down, 1),
        new(new(11, 4), Vector2.Down, 1)
    };

    // Start with all furniture in the tray and three exterior door openings.
    internal static Layout Defaults() => new(new Placement[Items.Length], new Door[]
    {
        new(4, 4), new(5, 2), new(1, 1)
    });

    // Return the footprint in tile coordinates at the supplied position.
    internal static Rect2 Footprint(int index, Placement item)
    {
        Vector2 size = Items[index].Size;
        if (item.Rotated)
            size = new(size.Y, size.X);
        return new(item.Position, size);
    }

    // Validate the full footprint against the floor union, shared wall and placed items.
    internal static bool CanPlace(Layout layout, int index, Placement candidate)
    {
        if (!OnGrid(candidate.X, 4) || !OnGrid(candidate.Y, 4))
            return false;

        Rect2 bounds = Footprint(index, candidate);
        float mainEdge = MainRoom.End.X;
        if (bounds.Position.X < 0 || bounds.Position.Y < 0 || bounds.End.X > Annex.End.X ||
            bounds.End.Y > (bounds.End.X > mainEdge ? Annex.End.Y : MainRoom.End.Y))
            return false;

        if (bounds.Position.X < mainEdge && bounds.End.X > mainEdge &&
            (bounds.Position.Y < 1 || bounds.End.Y > 4))
            return false;

        for (int i = 0; i < layout.Items.Length; i++)
        {
            if (i != index && layout.Items[i].Placed && bounds.Intersects(Footprint(i, layout.Items[i])))
                return false;
        }
        return true;
    }

    // Doors occupy a half-tile-snapped interval on a single wall segment.
    internal static bool CanPlaceDoor(Layout layout, int index, Door candidate)
    {
        if (candidate.WallIndex < 0 || candidate.WallIndex >= Walls.Length ||
            !OnGrid(candidate.Offset, 2) || candidate.Offset < 0 ||
            candidate.Offset + DoorWidth > Walls[candidate.WallIndex].Length)
            return false;

        for (int i = 0; i < layout.Doors.Length; i++)
        {
            Door other = layout.Doors[i];
            if (i != index && other.WallIndex == candidate.WallIndex &&
                candidate.Offset < other.Offset + DoorWidth && other.Offset < candidate.Offset + DoorWidth)
                return false;
        }
        return true;
    }

    private static bool OnGrid(float value, int subdivisions) =>
        float.IsFinite(value) && value * subdivisions == MathF.Round(value * subdivisions);

    // Replace the save only after writing the complete new layout.
    internal static void Save(Layout layout)
    {
        string path = ProjectSettings.GlobalizePath(SavePath);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(layout));
        File.Move(path + ".tmp", path, overwrite: true);
    }

    // Validate the entire file before the caller replaces its current layout.
    internal static Layout Load()
    {
        string path = ProjectSettings.GlobalizePath(SavePath);
        Layout layout = JsonSerializer.Deserialize<Layout>(File.ReadAllText(path));
        if (layout?.Items?.Length != Items.Length || layout.Doors?.Length != 3)
            throw new InvalidDataException("Unexpected furniture or door count.");

        for (int i = 0; i < layout.Items.Length; i++)
        {
            if (layout.Items[i].Placed && !CanPlace(layout, i, layout.Items[i]))
                throw new InvalidDataException("Invalid furniture placement.");
        }
        for (int i = 0; i < layout.Doors.Length; i++)
        {
            if (!CanPlaceDoor(layout, i, layout.Doors[i]))
                throw new InvalidDataException("Invalid door placement.");
        }
        return layout;
    }
}
