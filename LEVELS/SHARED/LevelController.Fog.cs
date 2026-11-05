// Level composition owns source references; fog has no dependency on cameras or enemy decisions.
using Godot;

public partial class LevelController
{
    public FogOfWar Fog { get; private set; }
    private void InitializeFog()
    {
        Fog = GetNode<FogOfWar>("RootWorld/FogOfWar");
        if (Config?.Fog != null) Fog.Config = Config.Fog;
        Fog.Initialize(_grid, Config?.FogEnabled ?? true);
        Fog.GetNode<FogPresentation>("Presentation").Initialize(Fog, _enemies,
            GetNode<BuildingRegistry>("RootFortress/BuildingRegistry"), _projectiles, this);
        Fog.GetNode<BuildingVisionSources>("BuildingSources").Initialize(Fog,
            GetNode<BuildingRegistry>("RootFortress/BuildingRegistry"), _citadel);
        if (PlayerAlive) Fog.RegisterSource(CurrentPlayer.GetInstanceId(), CurrentPlayer, Fog.Config.PlayerVisionRadius, true);
    }
}
