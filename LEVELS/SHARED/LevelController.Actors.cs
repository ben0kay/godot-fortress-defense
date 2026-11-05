// Optional actor/camera composition; world/building/enemy owners never discover a player themselves.
using Godot;

public partial class LevelController
{
    public Player CurrentPlayer { get; private set; }
    public LevelObjectiveSignals Objectives { get; private set; }
    private EnemyTargetRoster _targetRoster;

    // A disabled player creates no actor or placeholder; Level001's authored benchmark stays player-free.
    private void InitializeOptionalActors()
    {
        _targetRoster = GetNode<EnemyTargetRoster>("RootEnemies/TargetRoster");
        if (Config?.PlayerRequiredForDefeat == true && !Config.SpawnPlayer)
            throw new System.InvalidOperationException("A player-required level must enable player spawning.");
        if (Config?.SpawnPlayer == true)
        {
            if (Config.PlayerScene == null) throw new System.InvalidOperationException("Enabled player spawning requires a PackedScene.");
            Node instance = Config.PlayerScene.Instantiate();
            if (instance is not Player player) { instance.Free(); throw new System.InvalidOperationException("Player scene root must derive from Player."); }
            try
            {
                player.Initialize(_grid);
                Vector2 position = GetNode<Marker2D>("RootWorld/PlayerSpawn").GlobalPosition;
                if (!_grid.SegmentClear(position, position, player.Radius) || !_enemies.SpawnSpaceFree(position, player.Radius))
                    throw new System.InvalidOperationException("Player spawn overlaps solids, bounds or another actor.");
                GetNode<Node2D>("RootPlayer").AddChild(player); player.GlobalPosition = position;
                CurrentPlayer = player; player.InputBlocked = () => _buildMenuOpen;
            }
            catch { player.Free(); throw; }
        }
        _targetRoster.Initialize(GetNode<BuildingRegistry>("RootFortress/BuildingRegistry"), _projectiles);
        _targetRoster.SetMobileTarget(CurrentPlayer);
        _spawner.TargetRoster = _targetRoster;
        _spawner.AdditionalSpawnValidation = (position, radius) => !PlayerAlive
            || position.DistanceSquaredTo(CurrentPlayer.GlobalPosition) >= Mathf.Pow(CurrentPlayer.Radius + radius + 2, 2);
        _camera.InputBlocked = () => _buildMenuOpen;
        _camera.PanAllowed = () => Config != null || !_overlay; // DEBUG's B + MMB terrain edit remains available.
        _camera.Notice += OnCameraNotice;
        _camera.Initialize(_grid.FootprintRect(_grid.Bounds), CurrentPlayer);
        Objectives = GetNode<LevelObjectiveSignals>("Objectives");
        Objectives.Bind(_citadel, CurrentPlayer, Config?.PlayerRequiredForDefeat ?? false, Config?.CitadelRequiredForDefeat ?? true);
    }
    private bool PlayerAlive => GodotObject.IsInstanceValid(CurrentPlayer) && CurrentPlayer.TargetAlive;
    private bool CanPlaceActors(Rect2I area) => _enemies.CanPlace(_grid, area) && (!PlayerAlive || !CurrentPlayer.BlocksPlacement(_grid, area));
    private void OnCameraNotice(string text) { _notice = text; if (_status != null && !_closing) RefreshDisplay(); }
}
