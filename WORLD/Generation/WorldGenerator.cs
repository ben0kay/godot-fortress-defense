// Staged WG0 generation on plain cell arrays, bounded retries, explicit deterministic repair and acceptance metadata.
using Godot;
using System;

public static class WorldGenerator
{
    public sealed class Result
    {
        public bool[] Solid { get; init; }
        public bool[] ReachableFromCpu { get; init; }
        public Rect2I CpuFootprint { get; init; }
        public Vector2I PlayerCell { get; init; }
        public long Seed { get; init; }
        public int DerivedSeed { get; init; }
        public int Attempts { get; init; }
        public int RejectedAttempts { get; init; }
        public bool Repaired { get; init; }
        public float Coverage { get; init; }
    }

    // CPU and border protection is constructed before growth and independently checked before commit.
    public static Result Generate(WorldGrid grid, Vector2I cpuSize, WorldGenerationConfig config, long seed)
    {
        config.Validate(grid, cpuSize);
        Vector2I size = grid.Dimensions;
        Rect2I cpu = new((size - cpuSize) / 2, cpuSize);
        if (!grid.Fits(cpu)) throw new InvalidOperationException("CPU footprint does not fit the level.");
        Vector2I player = new(cpu.End.X + grid.ClearanceCells + 1, cpu.Position.Y + cpu.Size.Y / 2);
        if (!grid.Contains(player)) throw new InvalidOperationException("Player marker does not fit beside the CPU.");
        var protectedCells = new bool[size.X * size.Y];
        Vector2 center = (Vector2)cpu.Position + (Vector2)cpu.Size * .5f;
        for (int y = 0; y < size.Y; y++) for (int x = 0; x < size.X; x++)
        {
            Vector2I cell = new(x, y);
            protectedCells[y * size.X + x] = x < config.EdgeSpawnClearanceCells || y < config.EdgeSpawnClearanceCells
                || x >= size.X - config.EdgeSpawnClearanceCells || y >= size.Y - config.EdgeSpawnClearanceCells
                || ((Vector2)cell + Vector2.One * .5f).DistanceSquaredTo(center) <= config.CpuClearingRadiusCells * config.CpuClearingRadiusCells
                || cpu.Grow(grid.ClearanceCells + 1).HasPoint(cell) || cell == player;
        }
        bool[] solid = null; int derived = 0; string failure = "";
        for (int attempt = 0; attempt < config.MaxGenerationAttempts; attempt++)
        {
            derived = DeriveSeed(seed, attempt);
            solid = TerrainClusterGenerator.Generate(size, protectedCells, config, derived);
            if (SpawnAreaValidator.Validate(solid, protectedCells, grid, cpu, player, config, out bool[] reachable, out failure))
                return Accept(solid, reachable, attempt + 1, attempt, false);
        }
        // Final bounded fallback carves four clearance-wide ground connections, never rerolls forever.
        int corridor = grid.ClearanceCells + Mathf.CeilToInt(config.MinReachableEdgeCellsPerSide * .5f);
        int cx = cpu.Position.X + cpu.Size.X / 2, cy = cpu.Position.Y + cpu.Size.Y / 2;
        for (int y = 0; y < size.Y; y++) for (int x = 0; x < size.X; x++)
            if (Mathf.Abs(x - cx) <= corridor || Mathf.Abs(y - cy) <= corridor) solid[y * size.X + x] = false;
        TerrainClusterGenerator.RemoveFragments(solid, size, config.MinimumFragmentSize);
        if (SpawnAreaValidator.Validate(solid, protectedCells, grid, cpu, player, config, out bool[] repairedReachable, out failure))
            return Accept(solid, repairedReachable, config.MaxGenerationAttempts + 1, config.MaxGenerationAttempts, true);
        throw new InvalidOperationException("WG0 validation failed after bounded attempts and repair: " + failure);

        Result Accept(bool[] accepted, bool[] reachable, int attempts, int rejected, bool repaired)
        {
            int count = 0; foreach (bool value in accepted) if (value) count++;
            return new Result { Solid = accepted, ReachableFromCpu = reachable, CpuFootprint = cpu, PlayerCell = player,
                Seed = seed, DerivedSeed = derived, Attempts = attempts, RejectedAttempts = rejected, Repaired = repaired,
                Coverage = count / (float)accepted.Length };
        }
    }

    // Full original seed and attempt survive in Result; mixing does not consume gameplay/spawn RNG.
    private static int DeriveSeed(long seed, int attempt)
    {
        ulong value = unchecked((ulong)seed + (ulong)attempt * 0x9E3779B97F4A7C15UL);
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return unchecked((int)(value ^ (value >> 31)));
    }
}
