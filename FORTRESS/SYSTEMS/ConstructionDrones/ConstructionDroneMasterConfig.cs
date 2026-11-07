// Inspector defaults for one level's Citadel construction service.
using Godot;
using System;

[GlobalClass]
public partial class ConstructionDroneMasterConfig : Resource
{
    [ExportGroup("Service")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool ShowVisuals { get; set; } = true;
    [Export] public bool RequireDroneToBuild { get; set; }
    [Export(PropertyHint.Range, "1,64,1")] public int MaxActiveDrones { get; set; } = 4;
    [Export(PropertyHint.Range, "1,64,1")] public int DispatchBudget { get; set; } = 2;

    [ExportGroup("Flight")]
    [Export] public float TravelSpeed { get; set; } = 280;
    [Export] public float Acceleration { get; set; } = 800;
    [Export] public float Deceleration { get; set; } = 800;
    [Export] public float TurnDegreesPerSecond { get; set; } = 240;
    [Export] public float ArrivalRadius { get; set; } = 2;
    [Export] public Vector2 HoverOffset { get; set; } = new(0, -38);

    [ExportGroup("Beam")]
    [Export] public float BeamDeploymentSeconds { get; set; } = .25f;
    [Export] public Color BeamColor { get; set; } = new(.25f, .95f, 1, .45f);

    public void Validate()
    {
        if (MaxActiveDrones is < 1 or > 64 || DispatchBudget is < 1 or > 64
            || !Positive(TravelSpeed) || !Positive(Acceleration) || !Positive(Deceleration)
            || !Positive(TurnDegreesPerSecond) || !float.IsFinite(ArrivalRadius) || ArrivalRadius < 0
            || !float.IsFinite(BeamDeploymentSeconds) || BeamDeploymentSeconds < 0 || !HoverOffset.IsFinite() || HoverOffset.IsZeroApprox())
            throw new InvalidOperationException("Invalid construction drone configuration.");
    }

    private static bool Positive(float value) => float.IsFinite(value) && value > 0;
}
