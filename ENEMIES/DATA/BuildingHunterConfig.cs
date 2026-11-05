// Authored building-hunter hysteresis and scan cadence, independent from movement and attacks.
using Godot;
using System;

[GlobalClass]
public partial class BuildingHunterConfig : Resource
{
    [Export] public float DetectionRadius { get; set; } = 400;
    [Export] public float MinimumScanInterval { get; set; } = .75f;
    [Export] public float MaximumScanInterval { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0.01,0.9,0.01")] public float SwitchImprovement { get; set; } = .15f;
    [Export] public float MinimumSwitchInterval { get; set; } = 1;

    public void Validate()
    {
        if (!float.IsFinite(DetectionRadius) || DetectionRadius <= 0 || !float.IsFinite(MinimumScanInterval) || MinimumScanInterval < .1f
            || !float.IsFinite(MaximumScanInterval) || MaximumScanInterval < MinimumScanInterval
            || !float.IsFinite(SwitchImprovement) || SwitchImprovement <= 0 || SwitchImprovement >= 1
            || !float.IsFinite(MinimumSwitchInterval) || MinimumSwitchInterval < 0)
            throw new InvalidOperationException("Invalid building-hunter scan/hysteresis settings.");
    }
}
