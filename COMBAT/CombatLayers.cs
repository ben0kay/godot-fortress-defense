// Shared offensive target eligibility and explicit 2D projectile-plane rules; no altitude simulation.
using System;

[Flags]
public enum EnemyTargetLayers { None = 0, Ground = 1, Air = 2, Both = Ground | Air }
public enum ProjectileCollisionProfile { GroundObstructed, Airborne, IntendedGroundTarget }

public static class CombatLayers
{
    public static EnemyTargetLayers Of(Enemy enemy) => enemy.MovementLayer == EnemyMovementLayer.Flying ? EnemyTargetLayers.Air : EnemyTargetLayers.Ground;
    public static bool Allows(EnemyTargetLayers layers, Enemy enemy) => (layers & Of(enemy)) != 0;
    public static bool Valid(EnemyTargetLayers layers) => (layers & ~EnemyTargetLayers.Both) == 0;
    public static ProjectileCollisionProfile ShotProfile(Enemy enemy) => enemy.MovementLayer == EnemyMovementLayer.Flying
        ? ProjectileCollisionProfile.Airborne : ProjectileCollisionProfile.GroundObstructed;
}
