// Immutable launch/attack snapshot in base damage units.
using System;

public readonly record struct DamageInfo
{
    public float Amount { get; }
    public DamageType Type { get; }
    public DamageSource Source { get; }
    public float? ShieldCoefficientOverride { get; }
    public float? ArmourCoefficientOverride { get; }
    public float? HullCoefficientOverride { get; }
    public float ShieldPenetration { get; }
    public StatusEffectPayload? Effect { get; }

    // Layer overrides replace the global response, with receiver affinity still applied.
    public DamageInfo(float amount, DamageType type, DamageSource source = null, float shieldPenetration = 0,
        float? shieldCoefficientOverride = null, float? armourCoefficientOverride = null, float? hullCoefficientOverride = null,
        StatusEffectPayload? effect = null)
    {
        ValidateNonnegative(amount, nameof(amount));
        if ((uint)type > (uint)DamageType.Corrosive) throw new ArgumentOutOfRangeException(nameof(type));
        if (!float.IsFinite(shieldPenetration) || shieldPenetration < 0 || shieldPenetration > 1)
            throw new ArgumentOutOfRangeException(nameof(shieldPenetration));
        ValidateNonnegative(shieldCoefficientOverride ?? 1, nameof(shieldCoefficientOverride));
        ValidateNonnegative(armourCoefficientOverride ?? 1, nameof(armourCoefficientOverride));
        ValidateNonnegative(hullCoefficientOverride ?? 1, nameof(hullCoefficientOverride));
        if (effect is { IsValid: false }) throw new ArgumentOutOfRangeException(nameof(effect));
        Amount = amount; Type = type; Source = source; ShieldPenetration = shieldPenetration;
        ShieldCoefficientOverride = shieldCoefficientOverride; ArmourCoefficientOverride = armourCoefficientOverride;
        HullCoefficientOverride = hullCoefficientOverride; Effect = effect;
    }

    // Legacy inputs still ignore invalid scalars and use neutral global coefficients.
    internal static DamageInfo FromLegacy(float amount) => float.IsFinite(amount) && amount > 0
        ? new(amount, DamageType.Kinetic, DamageSource.Debug, shieldCoefficientOverride: 1,
            armourCoefficientOverride: 1, hullCoefficientOverride: 1)
        : default;

    // Shared numeric boundary for authored and runtime damage values.
    internal static float ValidateNonnegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}
