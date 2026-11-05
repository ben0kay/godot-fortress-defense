// Authored ST1 ground seeker stats; runtime snapshots keep shared resources immutable.
using Godot;
using System;

[GlobalClass]
public partial class EnemyDefinition : Resource
{
    [ExportGroup("Identity and Vitals")]
    [Export] public string EnemyId { get; set; } = "enemy001";
    [Export] public float MaxHealth { get; set; } = 30;
    [Export] public float Radius { get; set; } = 7;
    [ExportGroup("Movement")]
    [Export] public float MovementSpeed { get; set; } = 70;
    [Export] public float TurnSpeed { get; set; } = 12;
    [ExportGroup("Contact Attack")]
    [Export] public float AttackDamage { get; set; } = 8;
    [Export] public float AttackRange { get; set; } = 24;
    [Export] public float AttackCooldown { get; set; } = 1.2f;

    // Radius must fit the one shared ground profile; larger size classes are future work.
    public void Validate(float cellSize)
    {
        if (string.IsNullOrWhiteSpace(EnemyId) || !Valid(MaxHealth) || !Valid(Radius) || Radius >= cellSize * .5f
            || !Valid(MovementSpeed) || !Valid(TurnSpeed) || !Valid(AttackDamage) || !Valid(AttackRange) || !Valid(AttackCooldown))
            throw new InvalidOperationException("Enemy stats must be finite/positive and radius smaller than half a grid cell.");
        static bool Valid(float value) => float.IsFinite(value) && value > 0;
    }
}
