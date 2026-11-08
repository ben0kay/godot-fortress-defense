// Pure shield/armour/hull calculation in base damage units, with no actor or scene mutation.
using System;
using Godot;

public static class DamageResolver
{
    // Missing response/affinity profiles are neutral. Only an active shield splits the packet.
    public static DamageResult Resolve(DamageInfo hit, float shield, float armour, float hull,
        DamageResponseProfile response = null, DamageAffinityProfile affinity = null)
    {
        DamageInfo.ValidateNonnegative(shield, nameof(shield));
        DamageInfo.ValidateNonnegative(armour, nameof(armour));
        DamageInfo.ValidateNonnegative(hull, nameof(hull));
        if (hit.Amount == 0) return new(shield, armour, hull, 0, 0, 0);

        Vector3 coefficients = response?.Coefficients(hit.Type) ?? Vector3.One;
        Vector3 target = affinity?.Coefficients(hit.Type) ?? Vector3.One;
        double hullScale = (double)(hit.HullCoefficientOverride ?? coefficients.Z) * target.Z;
        if (shield == 0 && armour == 0)
        {
            float remainingHull = hullScale == 1 ? Math.Max(0, hull - hit.Amount)
                : (float)Math.Max(0, hull - hit.Amount * hullScale);
            return new(0, 0, remainingHull, 0, 0, hull - remainingHull);
        }

        double bypass = shield > 0 ? (double)hit.Amount * hit.ShieldPenetration : 0;
        double remaining = hit.Amount - bypass;
        float nextShield = Consume(shield, (double)(hit.ShieldCoefficientOverride ?? coefficients.X) * target.X, ref remaining);
        remaining += bypass;
        float nextArmour = Consume(armour, (double)(hit.ArmourCoefficientOverride ?? coefficients.Y) * target.Y, ref remaining);
        float nextHull = Consume(hull, hullScale, ref remaining);
        return new(nextShield, nextArmour, nextHull, shield - nextShield, armour - nextArmour, hull - nextHull);
    }

    // An empty pool is skipped. Immunity spends the ordinary packet without leaking it to the next layer.
    private static float Consume(float pool, double scale, ref double remaining)
    {
        if (pool == 0 || remaining == 0) return pool;
        if (scale == 0) { remaining = 0; return pool; }
        double effective = remaining * scale;
        if (effective < pool)
        {
            remaining = 0;
            return (float)(pool - effective);
        }
        remaining = Math.Max(0, remaining - pool / scale);
        return 0;
    }
}
