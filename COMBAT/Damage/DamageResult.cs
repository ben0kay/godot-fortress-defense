// Resulting pools and actual losses after clamping to available durability.
public readonly record struct DamageResult(float Shield, float Armour, float Hull,
    float ShieldLoss, float ArmourLoss, float HullLoss);
