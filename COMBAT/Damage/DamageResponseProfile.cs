// Shared defense response per damage type: X/Y/Z are shield/armour/hull.
using Godot;
using System;

[GlobalClass]
public partial class DamageResponseProfile : Resource
{
    private Vector3 _kinetic = Vector3.One;
    [Export] public Vector3 Kinetic { get => _kinetic; set => _kinetic = Validate(value); }
    private Vector3 _energy = Vector3.One;
    [Export] public Vector3 Energy { get => _energy; set => _energy = Validate(value); }
    private Vector3 _explosive = Vector3.One;
    [Export] public Vector3 Explosive { get => _explosive; set => _explosive = Validate(value); }
    private Vector3 _electric = Vector3.One;
    [Export] public Vector3 Electric { get => _electric; set => _electric = Validate(value); }
    private Vector3 _thermal = Vector3.One;
    [Export] public Vector3 Thermal { get => _thermal; set => _thermal = Validate(value); }
    private Vector3 _corrosive = Vector3.One;
    [Export] public Vector3 Corrosive { get => _corrosive; set => _corrosive = Validate(value); }

    // Resolve authored coefficients in shield/armour/hull order.
    internal Vector3 Coefficients(DamageType type) => type switch
    {
        DamageType.Kinetic => Kinetic,
        DamageType.Energy => Energy,
        DamageType.Explosive => Explosive,
        DamageType.Electric => Electric,
        DamageType.Thermal => Thermal,
        DamageType.Corrosive => Corrosive,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // Load once during actor initialization, outside the resolver and hit path.
    internal static DamageResponseProfile Default => _default ??= GD.Load<DamageResponseProfile>(
        "res://COMBAT/Damage/DefaultDamageProfile.tres") ?? throw new InvalidOperationException("Default damage profile is missing.");
    private static DamageResponseProfile _default;

    // Export assignments validate before replacing the previously valid value.
    private static Vector3 Validate(Vector3 value)
    {
        DamageInfo.ValidateNonnegative(value.X, nameof(value));
        DamageInfo.ValidateNonnegative(value.Y, nameof(value));
        DamageInfo.ValidateNonnegative(value.Z, nameof(value));
        return value;
    }
}
