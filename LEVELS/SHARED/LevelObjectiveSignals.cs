// Configurable future defeat hook only: publishes once, without stopping gameplay or opening results UI.
using Godot;
using System;

public partial class LevelObjectiveSignals : Node
{
    public enum DefeatReason { PlayerDied, RequiredPlayerRemoved, CitadelDestroyed }
    public event Action PlayerDied;
    public event Action CitadelDestroyed;
    public event Action<DefeatReason> DefeatRequested;
    public bool DefeatSignalled { get; private set; }
    private Building _citadel;
    private Player _player;
    private bool _playerRequired, _citadelRequired;

    // Required-player levels must explicitly enable spawning; optional/no-player levels do not defeat on absence.
    public void Bind(Building citadel, Player player, bool playerRequired, bool citadelRequired)
    {
        _citadel = citadel; _player = player; _playerRequired = playerRequired; _citadelRequired = citadelRequired;
        if (_citadel != null) _citadel.Destroyed += OnCitadelDestroyed;
        if (_player != null) { _player.Died += OnPlayerDied; _player.TreeExiting += OnPlayerRemoved; }
    }
    private void OnCitadelDestroyed() { CitadelDestroyed?.Invoke(); if (_citadelRequired) Publish(DefeatReason.CitadelDestroyed); }
    private void OnPlayerDied() { PlayerDied?.Invoke(); if (_playerRequired) Publish(DefeatReason.PlayerDied); }
    private void OnPlayerRemoved()
    { if (_playerRequired && IsInsideTree() && !_player.IsDead) Publish(DefeatReason.RequiredPlayerRemoved); }
    private void Publish(DefeatReason reason)
    { if (DefeatSignalled) return; DefeatSignalled = true; DefeatRequested?.Invoke(reason); }
    public override void _ExitTree() => EndSession();
    // Level reload calls this before actor removal; scene-owned teardown never requests defeat.
    public void EndSession()
    {
        if (GodotObject.IsInstanceValid(_citadel)) _citadel.Destroyed -= OnCitadelDestroyed;
        if (GodotObject.IsInstanceValid(_player)) { _player.Died -= OnPlayerDied; _player.TreeExiting -= OnPlayerRemoved; }
    }
}
