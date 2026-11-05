// Deterministic row-run merging: exact solid coverage with fewer passive collision/render rectangles.
using Godot;
using System.Collections.Generic;

public static class TerrainRectangles
{
    // Vertically join identical horizontal runs; no solid geometry ever spans a ground hole.
    public static List<Rect2I> Build(bool[] solid, Vector2I dimensions)
    {
        var rectangles = new List<Rect2I>();
        var previous = new Dictionary<(int, int), int>();
        for (int y = 0; y < dimensions.Y; y++)
        {
            var current = new Dictionary<(int, int), int>();
            for (int x = 0; x < dimensions.X;)
            {
                if (!solid[y * dimensions.X + x]) { x++; continue; }
                int start = x;
                while (x < dimensions.X && solid[y * dimensions.X + x]) x++;
                var key = (start, x - start);
                if (previous.TryGetValue(key, out int index))
                {
                    Rect2I rect = rectangles[index]; rect.Size += Vector2I.Down;
                    rectangles[index] = rect; current.Add(key, index);
                }
                else { current.Add(key, rectangles.Count); rectangles.Add(new(start, y, x - start, 1)); }
            }
            previous = current;
        }
        return rectangles;
    }
}
