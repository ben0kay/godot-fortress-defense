// Optional immutable effect authoring data; damage resolution does not run effects.
using System;

public readonly record struct StatusEffectPayload
{
    public StatusEffectId Id { get; }
    public float Chance { get; }
    public float DurationSeconds { get; }
    public float Strength { get; }
    public float? TickIntervalSeconds { get; }
    public DamageSource Source { get; }

    // Duration distinguishes a constructed effect from a default-initialized value.
    internal bool IsValid => DurationSeconds > 0;

    // Cryo strength is a speed multiplier in (0,1); freeze uses its duration for immobilization.
    public StatusEffectPayload(StatusEffectId id, float chance, float durationSeconds, float strength,
        float? tickIntervalSeconds = null, DamageSource source = null)
    {
        if ((uint)id > (uint)StatusEffectId.StasisFreeze) throw new ArgumentOutOfRangeException(nameof(id));
        if (!float.IsFinite(chance) || chance < 0 || chance > 1) throw new ArgumentOutOfRangeException(nameof(chance));
        if (!float.IsFinite(durationSeconds) || durationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        DamageInfo.ValidateNonnegative(strength, nameof(strength));
        if (id == StatusEffectId.CryoSlow && (strength <= 0 || strength >= 1))
            throw new ArgumentOutOfRangeException(nameof(strength));
        if (tickIntervalSeconds is float interval && (!float.IsFinite(interval) || interval <= 0
            || (id != StatusEffectId.Burn && id != StatusEffectId.Corrosion)))
            throw new ArgumentOutOfRangeException(nameof(tickIntervalSeconds));
        Id = id; Chance = chance; DurationSeconds = durationSeconds; Strength = strength;
        TickIntervalSeconds = tickIntervalSeconds; Source = source;
    }
}
