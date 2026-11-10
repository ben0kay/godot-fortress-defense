// Relay authoring shared by the placed building and the catalogue preview.
using Godot;
using System;

[GlobalClass]
public partial class PowerRelayDefinition : BuildingDefinition
{
    [Export(PropertyHint.Range, "1,4096,1,or_greater")] public float LinkRadius { get; set; } = 320;
    [Export(PropertyHint.Range, "0.1,120,0.1,or_greater")] public float ConstructionSeconds { get; set; } = 3;

    // Reject invalid relay authoring during building initialization, before a purchase commits.
    internal float CheckedConstructionSeconds()
    {
        if (!float.IsFinite(LinkRadius) || LinkRadius <= 0
            || !float.IsFinite(ConstructionSeconds) || ConstructionSeconds <= 0)
            throw new InvalidOperationException("Relay range and construction duration must be finite and positive.");
        return ConstructionSeconds;
    }
}
