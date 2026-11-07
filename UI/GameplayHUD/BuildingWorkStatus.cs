// Selected-site status for the existing battlefield HUD refresh.
public static class BuildingWorkStatus
{
    public static string Describe(Building building) => building.WorkPhase switch
    {
        BuildingWorkPhase.AwaitingDrone => "AWAITING DRONE",
        BuildingWorkPhase.Constructing => $"CONSTRUCTING {System.Math.Min(99, (int)(building.ConstructionProgress * 100))}%",
        _ => "OPERATIONAL"
    };
}
