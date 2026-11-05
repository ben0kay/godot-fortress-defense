// Optional enemy projectile launch settings; damage/range/cooldown remain shared EnemyDefinition values.
using Godot;
using System;

[GlobalClass]
public partial class EnemyRangedConfig : Resource
{
    [Export] public float ProjectileSpeed { get; set; } = 650;
    [Export] public float ProjectileLifetime { get; set; } = 2;
    [Export] public float ProjectileRadius { get; set; } = 2;
    [Export] public float TracerLength { get; set; } = 14;
    [Export] public float AimToleranceDegrees { get; set; } = 12;

    public void Validate(float range, float radius)
    {
        if (!float.IsFinite(ProjectileSpeed) || ProjectileSpeed <= 0 || !float.IsFinite(ProjectileLifetime) || ProjectileLifetime <= 0
            || !float.IsFinite(ProjectileRadius) || ProjectileRadius <= 0 || !float.IsFinite(TracerLength) || TracerLength <= 0
            || !float.IsFinite(AimToleranceDegrees) || AimToleranceDegrees <= 0 || AimToleranceDegrees > 180
            || !float.IsFinite(ProjectileSpeed * ProjectileLifetime) || ProjectileSpeed * ProjectileLifetime < range + radius * 2)
            throw new InvalidOperationException("Invalid enemy projectile stats or lifetime too short for attack range.");
    }
}
