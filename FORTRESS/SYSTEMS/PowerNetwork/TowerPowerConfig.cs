// Inspector settings for an opt-in weapon's private energy buffer.
using Godot;
using System;

[GlobalClass]
public partial class TowerPowerConfig : Resource
{
    [Export] public float ConnectionRadius { get; set; } = 400;
    [Export] public float Capacity { get; set; } = 30;
    [Export] public float IdlePerSecond { get; set; } = 2;
    [Export] public float EnergyPerShot { get; set; } = 4;
    [Export] public float MaxRechargePerSecond { get; set; } = 12;
    [Export] public float RestartCharge { get; set; } = 8;

    // Snapshot once at binding so Inspector resources are never used as mutable runtime state.
    internal PowerCapacitor CreateCapacitor()
    {
        if (!float.IsFinite(ConnectionRadius) || ConnectionRadius <= 0
            || !float.IsFinite(Capacity) || Capacity <= 0
            || !float.IsFinite(IdlePerSecond) || IdlePerSecond < 0
            || !float.IsFinite(EnergyPerShot) || EnergyPerShot <= 0
            || !float.IsFinite(MaxRechargePerSecond) || MaxRechargePerSecond < 0
            || !float.IsFinite(RestartCharge) || RestartCharge < EnergyPerShot || RestartCharge > Capacity)
            throw new InvalidOperationException("Invalid tower power settings. Restart charge must cover one shot and fit in the capacitor.");
        return new(Capacity, IdlePerSecond, EnergyPerShot, MaxRechargePerSecond, RestartCharge);
    }
}
