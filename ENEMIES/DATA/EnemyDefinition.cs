// Authored ST1 ground seeker stats; runtime snapshots keep shared resources immutable.
using Godot;
using System;

[GlobalClass]
public partial class EnemyDefinition : Resource
{
    [ExportGroup("Identity and Vitals")]
    [Export] public string DisplayName { get; set; } = "Enemy";
    [Export] public string EnemyId { get; set; } = "enemy001";
    [Export] public float MaxHealth { get; set; } = 30;
    [Export] public float Radius { get; set; } = 7;
    [ExportGroup("Movement")]
    [Export] public EnemyMovementLayer MovementLayer { get; set; } = EnemyMovementLayer.Ground;
    [Export] public float MovementSpeed { get; set; } = 70;
    [Export] public float TurnSpeed { get; set; } = 12;
    public enum TargetPriority { CitadelOnly, PreferNearbyPlayer, NearestWithinRange, BuildingHunter }
    [ExportGroup("Targeting")]
    [Export] public TargetPriority TargetRule { get; set; } = TargetPriority.PreferNearbyPlayer;
    [Export] public float PlayerDetectionRange { get; set; } = 500;
    [Export] public BuildingHunterConfig BuildingHunting { get; set; }
    [ExportGroup("Attack")]
    [Export] public EnemyAttackBehavior AttackBehavior { get; set; } = EnemyAttackBehavior.Contact;
    [Export] public float AttackDamage { get; set; } = 8;
    [Export] public float AttackRange { get; set; } = 24;
    [Export] public float AttackCooldown { get; set; } = 1.2f;

    [ExportGroup("Optional Ranged Attack")]
    [Export] public EnemyRangedConfig RangedAttack { get; set; }

    // Ground radii must fit the shared ground profile; flyers only require positive radius and world fit at spawn.
    public void Validate(float cellSize)
    {
        if (string.IsNullOrWhiteSpace(EnemyId) || !Valid(MaxHealth) || !Valid(Radius) || (MovementLayer == EnemyMovementLayer.Ground && Radius >= cellSize * .5f)
            || !Enum.IsDefined(MovementLayer) || !Enum.IsDefined(AttackBehavior)
            || !Enum.IsDefined(TargetRule) || !Valid(PlayerDetectionRange) || !Valid(MovementSpeed) || !Valid(TurnSpeed) || !Valid(AttackDamage) || !Valid(AttackRange) || !Valid(AttackCooldown))
            throw new InvalidOperationException("Enemy stats must be finite/positive and ground radius smaller than half a grid cell.");
        if (TargetRule == TargetPriority.BuildingHunter)
        { if (BuildingHunting == null) throw new InvalidOperationException("Building hunter settings required."); BuildingHunting.Validate(); }
        if (AttackBehavior == EnemyAttackBehavior.Impact && RangedAttack != null)
            throw new InvalidOperationException("Choose impact or ranged execution, not both.");
        RangedAttack?.Validate(AttackRange, Radius);
        static bool Valid(float value) => float.IsFinite(value) && value > 0;
    }
}
