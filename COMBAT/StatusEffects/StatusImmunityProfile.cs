// Optional receiver policy for effect identity only; no chance rolls or damage dependency.
using Godot;
using System;

[GlobalClass]
public partial class StatusImmunityProfile : Resource
{
    [Flags]
    public enum ImmuneEffects
    {
        None = 0,
        Stagger = 1 << (int)StatusEffectId.Stagger,
        Burn = 1 << (int)StatusEffectId.Burn,
        Corrosion = 1 << (int)StatusEffectId.Corrosion,
        Disruption = 1 << (int)StatusEffectId.Disruption,
        CryoSlow = 1 << (int)StatusEffectId.CryoSlow,
        StasisFreeze = 1 << (int)StatusEffectId.StasisFreeze,
        All = Stagger | Burn | Corrosion | Disruption | CryoSlow | StasisFreeze
    }

    private ImmuneEffects _immunities;
    [Export] public ImmuneEffects Immunities
    {
        get => _immunities;
        set
        {
            if ((value & ~ImmuneEffects.All) != 0) throw new ArgumentOutOfRangeException(nameof(value));
            _immunities = value;
        }
    }

    // Check payload and immunity only. The future runtime owns hit survival, chance and application policy.
    public static bool Allows(StatusEffectPayload effect, StatusImmunityProfile profile = null) =>
        effect.IsValid && effect.Chance > 0
        && (profile == null || (profile.Immunities & (ImmuneEffects)(1 << (int)effect.Id)) == 0);
}
