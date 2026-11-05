// Connected directional frontier growth with smooth spatial noise; no scattered independent cell lottery.
using Godot;
using System;
using System.Collections.Generic;

public static class TerrainClusterGenerator
{
    // Per-cluster direction/elongation varies narrow branching formations and broader compact masses.
    public static bool[] Generate(Vector2I size, bool[] protectedCells, WorldGenerationConfig config, int seed)
    {
        var random = new Random(seed); var solid = new bool[size.X * size.Y];
        int target = Mathf.RoundToInt(solid.Length * config.TargetCoverage), placed = 0;
        for (int cluster = 0; cluster < config.ClusterCount && placed < target; cluster++)
        {
            Vector2I origin = default; bool found = false;
            for (int tries = 0; tries < 256; tries++)
            {
                origin = new(random.Next(size.X), random.Next(size.Y)); int index = origin.Y * size.X + origin.X;
                if (!protectedCells[index] && !solid[index]) { found = true; break; }
            }
            if (!found) break;
            int budget = Mathf.Clamp(Mathf.RoundToInt((target - placed) / (float)(config.ClusterCount - cluster)
                * (.65f + (float)random.NextDouble() * .7f)), config.MinClusterSize, config.MaxClusterSize);
            budget = Mathf.Min(budget, target - placed);
            float angle = (float)random.NextDouble() * Mathf.Tau;
            Vector2 axis = new(Mathf.Cos(angle), Mathf.Sin(angle));
            float elongation = 1 + (float)random.NextDouble() * (config.MaxElongation - 1);
            var frontier = new List<Vector2I>(); var queued = new bool[solid.Length];
            Add(origin);
            for (int grown = 0; grown < budget && frontier.Count > 0;)
            {
                int best = random.Next(frontier.Count); float bestScore = Score(frontier[best]);
                for (int sample = 0; sample < 7; sample++)
                { int pick = random.Next(frontier.Count); float score = Score(frontier[pick]); if (score < bestScore) { best = pick; bestScore = score; } }
                Vector2I cell = frontier[best]; frontier[best] = frontier[^1]; frontier.RemoveAt(frontier.Count - 1);
                int index = cell.Y * size.X + cell.X;
                if (solid[index] || protectedCells[index]) continue;
                solid[index] = true; grown++; placed++;
                foreach (Vector2I direction in GroundReachability.CardinalDirections) Add(cell + direction);
            }

            // Frontier members are always adjacent to existing cluster cells, hence connected growth.
            void Add(Vector2I cell)
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= size.X || cell.Y >= size.Y) return;
                int index = cell.Y * size.X + cell.X;
                if (queued[index] || solid[index] || protectedCells[index]) return;
                queued[index] = true; frontier.Add(cell);
            }
            float Score(Vector2I cell)
            {
                Vector2 offset = cell - origin;
                float along = offset.Dot(axis), across = offset.Cross(axis);
                int neighbors = NeighborCount(solid, size, cell);
                return Mathf.Abs(along) / elongation + Mathf.Abs(across)
                    + Noise(cell.X, cell.Y, seed) * config.Irregularity * 8
                    + neighbors * (config.Branching * 1.5f - .75f);
            }
        }
        for (int pass = 0; pass < config.SmoothingPasses; pass++)
        {
            bool[] next = (bool[])solid.Clone();
            for (int y = 0; y < size.Y; y++) for (int x = 0; x < size.X; x++)
            {
                int i = y * size.X + x; if (protectedCells[i]) { next[i] = false; continue; }
                int neighbors = NeighborCount(solid, size, new(x, y));
                if (solid[i] && neighbors == 0) next[i] = false;
                else if (!solid[i] && neighbors >= 3) next[i] = true;
            }
            solid = next;
        }
        RemoveFragments(solid, size, config.MinimumFragmentSize);
        return solid;
    }

    // Fragment cleanup is linear in map size and runs only during generation/repair.
    public static void RemoveFragments(bool[] solid, Vector2I size, int minimum)
    {
        var seen = new bool[solid.Length]; var component = new List<int>();
        for (int i = 0; i < solid.Length; i++)
        {
            if (seen[i] || !solid[i]) continue;
            component.Clear(); component.Add(i); seen[i] = true;
            for (int head = 0; head < component.Count; head++)
            {
                int index = component[head]; Vector2I cell = new(index % size.X, index / size.X);
                foreach (Vector2I direction in GroundReachability.CardinalDirections)
                {
                    Vector2I next = cell + direction;
                    if (next.X < 0 || next.Y < 0 || next.X >= size.X || next.Y >= size.Y) continue;
                    int n = next.Y * size.X + next.X;
                    if (seen[n] || !solid[n]) continue;
                    seen[n] = true; component.Add(n);
                }
            }
            if (component.Count < minimum) foreach (int index in component) solid[index] = false;
        }
    }
    private static int NeighborCount(bool[] solid, Vector2I size, Vector2I cell)
    {
        int count = 0;
        foreach (Vector2I direction in GroundReachability.CardinalDirections)
        {
            Vector2I n = cell + direction;
            if (n.X >= 0 && n.Y >= 0 && n.X < size.X && n.Y < size.Y && solid[n.Y * size.X + n.X]) count++;
        }
        return count;
    }

    // Smooth lattice-value noise perturbs the growth frontier without another runtime dependency.
    private static float Noise(int x, int y, int seed)
    {
        int gx = x / 5, gy = y / 5; float tx = x % 5 / 5f, ty = y % 5 / 5f;
        tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
        return Mathf.Lerp(Mathf.Lerp(Hash(gx, gy), Hash(gx + 1, gy), tx), Mathf.Lerp(Hash(gx, gy + 1), Hash(gx + 1, gy + 1), tx), ty);
        float Hash(int a, int b)
        {
            uint value = unchecked((uint)(a * 374761393 + b * 668265263 + seed));
            value = (value ^ (value >> 13)) * 1274126177u; return (value ^ (value >> 16)) / (float)uint.MaxValue;
        }
    }
}
