// One startup flood validates CPU approaches and edge regions with the same cell-clearance policy as navigation.
using Godot;
using System.Collections.Generic;

public static class SpawnAreaValidator
{
    // The CPU is treated as occupied during validation, never as a passable shortcut through the center.
    public static bool Validate(bool[] solid, bool[] protectedCells, WorldGrid grid, Rect2I cpu, Vector2I player,
        WorldGenerationConfig config, out bool[] reachable, out string failure)
    {
        Vector2I size = grid.Dimensions;
        reachable = new bool[solid.Length]; failure = "";
        for (int i = 0; i < solid.Length; i++)
            if (protectedCells[i] && solid[i]) { failure = "Protected clearing/border was obstructed"; return false; }
        if (!Open(player)) { failure = "Player marker is obstructed"; return false; }
        int nearbyGround = 0;
        Vector2 center = (Vector2)cpu.Position + (Vector2)cpu.Size * .5f;
        for (int y = 0; y < size.Y; y++) for (int x = 0; x < size.X; x++)
            if (((Vector2)new Vector2I(x, y) + Vector2.One * .5f).DistanceSquaredTo(center) <= config.CpuClearingRadiusCells * config.CpuClearingRadiusCells
                && !cpu.HasPoint(new(x, y)) && !solid[y * size.X + x]) nearbyGround++;
        if (nearbyGround < 24) { failure = "Insufficient nearby buildable ground"; return false; }
        var queue = new Queue<Vector2I>(); bool[] visited = reachable;
        int gap = grid.ClearanceCells + 1;
        for (int x = cpu.Position.X; x < cpu.End.X; x++)
        { Add(new(x, cpu.Position.Y - gap)); Add(new(x, cpu.End.Y - 1 + gap)); }
        for (int y = cpu.Position.Y; y < cpu.End.Y; y++)
        { Add(new(cpu.Position.X - gap, y)); Add(new(cpu.End.X - 1 + gap, y)); }
        if (queue.Count == 0) { failure = "No legal CPU approach"; return false; }
        while (queue.Count > 0)
        {
            Vector2I cell = queue.Dequeue();
            foreach (Vector2I direction in GroundReachability.CardinalDirections) Add(cell + direction);
        }
        if (!visited[player.Y * size.X + player.X]) { failure = "Player marker is isolated"; return false; }
        int inset = grid.ClearanceCells; int[] sides = new int[4];
        for (int x = inset; x < size.X - inset; x++)
        { if (visited[inset * size.X + x]) sides[0]++; if (visited[(size.Y - inset - 1) * size.X + x]) sides[2]++; }
        for (int y = inset; y < size.Y - inset; y++)
        { if (visited[y * size.X + inset]) sides[3]++; if (visited[y * size.X + size.X - inset - 1]) sides[1]++; }
        foreach (int count in sides)
            if (count < config.MinReachableEdgeCellsPerSide) { failure = "An edge region cannot reach the CPU"; return false; }
        return true;

        void Add(Vector2I cell)
        {
            if (!Open(cell)) return;
            int i = cell.Y * size.X + cell.X;
            if (visited[i]) return;
            visited[i] = true; queue.Enqueue(cell);
        }
        bool Open(Vector2I cell)
        {
            int radius = grid.ClearanceCells;
            for (int y = cell.Y - radius; y <= cell.Y + radius; y++) for (int x = cell.X - radius; x <= cell.X + radius; x++)
                if (x < 0 || y < 0 || x >= size.X || y >= size.Y || solid[y * size.X + x] || cpu.HasPoint(new(x, y))) return false;
            return true;
        }
    }
}
