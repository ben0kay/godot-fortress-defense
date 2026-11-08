// Serialized effect IDs. Keep these values stable, independent of damage type IDs.
public enum StatusEffectId
{
    Stagger = 0,      // Movement control: short interruption.
    Burn = 1,         // Damage over time.
    Corrosion = 2,    // Damage over time.
    Disruption = 3,   // System impairment.
    CryoSlow = 4,     // Movement control: speed multiplier.
    StasisFreeze = 5  // Movement control: immobilization.
}
