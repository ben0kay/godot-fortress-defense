// Battle Tower Level 1 opts into the shared initial construction clock.
using Godot;

public partial class Tower001 : ProjectileTower
{
    [ExportGroup("Construction")]
    [Export] public bool ConstructionEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0.1,120,0.1,or_greater")] public float ConstructionSeconds { get; set; } = 3;

    protected override float InitialConstructionSeconds => ConstructionEnabled ? ConstructionSeconds : 0;
}
