// Editor-run calculation smoke check for DMG0; no battlefield or ongoing processing.
using Godot;
using System;

public partial class DamageResolverSmoke : Node
{
    // Run this scene directly with F6; any failure names the violated contract.
    public override void _Ready()
    {
        var hit = new DamageInfo(20, DamageType.Kinetic, shieldPenetration: .25f);
        var response = new DamageResponseProfile { Kinetic = new(2, .5f, 3) };
        var affinity = new DamageAffinityProfile { Kinetic = new(.5f, -1, 4, 1) };

        Require(DamageResolver.Resolve(new(30, DamageType.Kinetic), 0, 0, 40, DamageResponseProfile.Default)
            == new DamageResult(0, 0, 10, 0, 0, 30), "neutral hull");
        Require(DamageResolver.Resolve(default, 6, 8, 100) == new DamageResult(6, 8, 100, 0, 0, 0), "zero hit");
        Require(DamageResolver.Resolve(hit, 6, 8, 100, response, affinity)
            == new DamageResult(0, 0, 70, 6, 8, 30), "base spillover and penetration");
        Require(DamageResolver.Resolve(hit, 0, 8, 100, response, affinity)
            == new DamageResult(0, 0, 52, 0, 8, 48), "armour only");
        Require(DamageResolver.Resolve(hit, 0, 0, 100, response, affinity).HullLoss == 60, "hull only");

        affinity.Kinetic = new(.5f, 0, 4, 1);
        Require(DamageResolver.Resolve(hit, 6, 8, 100, response, affinity)
            == new DamageResult(6, 0, 97, 0, 8, 3), "shield immunity blocks only ordinary damage");
        affinity.Kinetic = new(.5f, -1, 0, 1);
        Require(DamageResolver.Resolve(hit, 0, 8, 100, response, affinity) == new DamageResult(0, 8, 100, 0, 0, 0), "armour immunity");
        affinity.Kinetic = new(.5f, -1, 0, 0);
        var burn = new StatusEffectPayload(StatusEffectId.Burn, 1, 2, 1, .5f);
        var immunity = new StatusImmunityProfile { Immunities = StatusImmunityProfile.ImmuneEffects.Burn };
        Require(DamageResolver.Resolve(hit, 0, 0, 100, response, affinity).HullLoss == 0
            && StatusImmunityProfile.Allows(burn) && !StatusImmunityProfile.Allows(burn, immunity), "independent status immunity");

        Reject(() => _ = new DamageInfo(float.NaN, DamageType.Kinetic));
        Reject(() => _ = new DamageInfo(1, (DamageType)99));
        Reject(() => _ = new DamageInfo(1, DamageType.Kinetic, shieldPenetration: 2));
        Reject(() => affinity.Kinetic = new(1, -2, -1, -1));
        Reject(() => _ = new StatusEffectPayload(StatusEffectId.CryoSlow, 1, 2, .5f, .5f));
        GD.Print("DMG0 calculation smoke passed.");
        GetTree().Quit();
    }

    // Surface mismatches in the Godot output with the individual case name.
    private static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException($"DMG0 smoke failed: {name}");
    }

    // Malformed authored inputs must be rejected before a hit can change state.
    private static void Reject(Action action)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException) { return; }
        throw new InvalidOperationException("DMG0 smoke accepted invalid input.");
    }
}
