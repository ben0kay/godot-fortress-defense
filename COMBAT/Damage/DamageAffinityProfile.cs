// Receiver affinities per type: X is the broad value; Y/Z/W optionally override shield/armour/hull.
using Godot;
using System;

[GlobalClass]
public partial class DamageAffinityProfile : Resource
{
    private Vector4 _kinetic = new(1, -1, -1, -1);
    [Export] public Vector4 Kinetic { get => _kinetic; set => _kinetic = Validate(value); }
    private Vector4 _energy = new(1, -1, -1, -1);
    [Export] public Vector4 Energy { get => _energy; set => _energy = Validate(value); }
    private Vector4 _explosive = new(1, -1, -1, -1);
    [Export] public Vector4 Explosive { get => _explosive; set => _explosive = Validate(value); }
    private Vector4 _electric = new(1, -1, -1, -1);
    [Export] public Vector4 Electric { get => _electric; set => _electric = Validate(value); }
    private Vector4 _thermal = new(1, -1, -1, -1);
    [Export] public Vector4 Thermal { get => _thermal; set => _thermal = Validate(value); }
    private Vector4 _corrosive = new(1, -1, -1, -1);
    [Export] public Vector4 Corrosive { get => _corrosive; set => _corrosive = Validate(value); }

    // Resolve authored coefficients in shield/armour/hull order.
    internal Vector3 Coefficients(DamageType type)
    {
        Vector4 values = type switch
        {
            DamageType.Kinetic => Kinetic,
            DamageType.Energy => Energy,
            DamageType.Explosive => Explosive,
            DamageType.Electric => Electric,
            DamageType.Thermal => Thermal,
            DamageType.Corrosive => Corrosive,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        return new(values.Y == -1 ? values.X : values.Y, values.Z == -1 ? values.X : values.Z,
            values.W == -1 ? values.X : values.W);
    }

    // Only the exact -1 sentinel inherits the broad affinity.
    private static Vector4 Validate(Vector4 value)
    {
        DamageInfo.ValidateNonnegative(value.X, nameof(value));
        if (value.Y != -1) DamageInfo.ValidateNonnegative(value.Y, nameof(value));
        if (value.Z != -1) DamageInfo.ValidateNonnegative(value.Z, nameof(value));
        if (value.W != -1) DamageInfo.ValidateNonnegative(value.W, nameof(value));
        return value;
    }
}
