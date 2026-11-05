// Tower001 Level 1 combat authoring, copied numerically from BattleTowerLevels row 1 without progression systems.
using Godot;
using System;

[GlobalClass]
public partial class Tower001CombatConfig : Resource
{
    [ExportGroup("Level 1 Weapon")]
    [Export] public float Damage { get; set; } = 30;
    [Export] public float ShotsPerSecond { get; set; } = 1.68f;
    [Export] public float RangePixels { get; set; } = 323.4f;
    [Export] public float ProjectileSpeed { get; set; } = 2300;
    [ExportGroup("Aiming and Acquisition")]
    [Export(PropertyHint.Flags, "Ground,Air")] public EnemyTargetLayers TargetLayers { get; set; } = EnemyTargetLayers.Ground;
    [Export] public float RotationSpeedDegrees { get; set; } = 216;
    [Export] public float AimToleranceDegrees { get; set; } = 12;
    [Export] public float SearchInterval { get; set; } = .15f;
    [Export] public float SpreadDegrees { get; set; } = 2.5f;
    [ExportGroup("Simple Presentation")]
    [Export] public float ProjectileRadius { get; set; } = 1.5f;
    [Export] public float TracerLength { get; set; } = 18;

    // Bound acquisition extent and invalid floats before they enter combat/spatial loops.
    public void Validate()
    {
        if (!CombatLayers.Valid(TargetLayers) || !Positive(Damage) || !float.IsFinite(ShotsPerSecond) || ShotsPerSecond < 0 || ShotsPerSecond > 60
            || !Positive(RangePixels) || RangePixels > 4096 || !Positive(ProjectileSpeed)
            || !Positive(RotationSpeedDegrees) || !float.IsFinite(AimToleranceDegrees) || AimToleranceDegrees < 0 || AimToleranceDegrees > 180
            || !Positive(SearchInterval) || !float.IsFinite(SpreadDegrees) || SpreadDegrees < 0 || SpreadDegrees > 45
            || !Positive(ProjectileRadius) || ProjectileRadius > 64 || !Positive(TracerLength) || TracerLength > 256)
            throw new InvalidOperationException("Tower001 combat settings must be finite and within valid weapon bounds.");
        static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}
