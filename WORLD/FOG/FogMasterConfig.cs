// Central FW0 tuning; logical visibility remains active when presentation is bypassed.
using Godot;
using System;

[GlobalClass]
public partial class FogMasterConfig : Resource
{
    public enum UpdateMode { Batched, Immediate }
    [ExportGroup("Presentation")]
    [Export] public bool EnabledByDefault { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,.01")] public float UnexploredOpacity { get; set; } = 1;
    [Export(PropertyHint.Range, "0,1,.01")] public float ExploredOpacity { get; set; } = .70f;
    [Export] public float EdgeSoftnessPixels { get; set; } = 48;
    [ExportGroup("Vision / World Pixels")]
    [Export] public float CitadelVisionRadius { get; set; } = 450;
    [Export] public float PlayerVisionRadius { get; set; } = 350;
    [Export] public bool VisionBlocksOnTerrain { get; set; }
    [ExportGroup("Scheduling")]
    [Export] public float VisibilityUpdateInterval { get; set; } = .15f;
    [Export] public int MaximumVisionUpdatesPerFrame { get; set; } = 4;
    [Export] public bool UpdateOnSourceCellChange { get; set; } = true;
    [Export] public UpdateMode RendererUpdateMode { get; set; } = UpdateMode.Batched;

    // Reject unsupported LOS and invalid settings rather than silently changing their meaning.
    public void Validate()
    {
        foreach (float value in new[] { UnexploredOpacity, ExploredOpacity, EdgeSoftnessPixels, CitadelVisionRadius, PlayerVisionRadius, VisibilityUpdateInterval })
            if (!float.IsFinite(value) || value < 0) throw new ArgumentException("Fog settings must be finite and nonnegative.");
        if (!Enum.IsDefined(RendererUpdateMode)) throw new ArgumentException("Unknown fog renderer update mode.");
        if (UnexploredOpacity > 1 || ExploredOpacity > 1 || VisibilityUpdateInterval < .01f || MaximumVisionUpdatesPerFrame < 1)
            throw new ArgumentException("Fog requires opacities 0–1, interval >= .01 and a positive update budget.");
        if (VisionBlocksOnTerrain) throw new ArgumentException("Terrain vision occlusion is not implemented in FW0; leave VisionBlocksOnTerrain off.");
    }
}
