// Authored stage sequence shared by every instance of a building.
using Godot;

[GlobalClass]
public partial class BuildingConstructionArt : Resource
{
    [Export] public Godot.Collections.Array<BuildingConstructionStage> Stages { get; set; } = new();

    // Use the highest matching threshold, including when one update crosses multiple stages.
    public Texture2D TextureAt(float progress, out float nextThreshold)
    {
        Texture2D texture = null;
        float threshold = -1;
        nextThreshold = float.PositiveInfinity;
        if (Stages == null) return null;
        foreach (BuildingConstructionStage stage in Stages)
        {
            if (stage?.Texture == null || stage.Progress is not (>= 0 and < 1)) continue;
            if (stage.Progress > progress)
            {
                nextThreshold = Mathf.Min(nextThreshold, stage.Progress);
                continue;
            }
            if (stage.Progress <= threshold) continue;
            threshold = stage.Progress;
            texture = stage.Texture;
        }
        return texture;
    }
}
