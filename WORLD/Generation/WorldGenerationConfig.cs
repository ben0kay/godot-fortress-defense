// Inspector-authored bounded WG0 cluster and validation settings, separate from level/runtime state.
using Godot;
using System;

[GlobalClass]
public partial class WorldGenerationConfig : Resource
{
    [ExportGroup("Reproducibility")]
    [Export] public long Seed { get; set; } = 112358;
    [Export] public bool RandomizeSeedOnRegeneration { get; set; } = true;
    [Export] public int MaxGenerationAttempts { get; set; } = 6;
    [ExportGroup("Terrain Clusters")]
    [Export] public float TargetCoverage { get; set; } = .22f;
    [Export] public int ClusterCount { get; set; } = 12;
    [Export] public int MinClusterSize { get; set; } = 24;
    [Export] public int MaxClusterSize { get; set; } = 160;
    [Export] public float Irregularity { get; set; } = .65f;
    [Export] public float MaxElongation { get; set; } = 4;
    [Export] public float Branching { get; set; } = .35f;
    [Export] public int SmoothingPasses { get; set; } = 1;
    [Export] public int MinimumFragmentSize { get; set; } = 12;
    [ExportGroup("Playable Areas")]
    [Export] public int CpuClearingRadiusCells { get; set; } = 8;
    [Export] public int EdgeSpawnClearanceCells { get; set; } = 2;
    [Export] public int MinReachableEdgeCellsPerSide { get; set; } = 4;

    // Reject invalid settings once, never hide impossible authored inputs behind endless retries.
    public void Validate(WorldGrid grid, Vector2I cpuSize)
    {
        int shortest = Mathf.Min(grid.Dimensions.X, grid.Dimensions.Y);
        if (!float.IsFinite(TargetCoverage) || TargetCoverage < 0 || TargetCoverage > .65f
            || ClusterCount < 1 || ClusterCount > 128 || MinClusterSize < 1 || MaxClusterSize < MinClusterSize
            || MaxClusterSize > grid.Dimensions.X * grid.Dimensions.Y || MinimumFragmentSize < 1 || MinimumFragmentSize > MaxClusterSize
            || !Unit(Irregularity) || !Unit(Branching) || !float.IsFinite(MaxElongation) || MaxElongation < 1 || MaxElongation > 8
            || SmoothingPasses < 0 || SmoothingPasses > 4 || MaxGenerationAttempts < 1 || MaxGenerationAttempts > 16
            || EdgeSpawnClearanceCells < grid.ClearanceCells + 1 || EdgeSpawnClearanceCells > shortest / 4
            || MinReachableEdgeCellsPerSide < 1 || MinReachableEdgeCellsPerSide > shortest - grid.ClearanceCells * 2
            || CpuClearingRadiusCells < Mathf.CeilToInt(Mathf.Max(cpuSize.X, cpuSize.Y) * .5f) + grid.ClearanceCells + 2
            || CpuClearingRadiusCells >= shortest / 2 - EdgeSpawnClearanceCells)
            throw new InvalidOperationException("WG0 generation settings are invalid or the clearing/edge bands do not fit this grid.");
        static bool Unit(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
    }
}
