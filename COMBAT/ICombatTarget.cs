// Minimal scalar-damage target contract shared by static buildings and optional mobile actors.
using Godot;
using System;

public interface ICombatTarget
{
    Node2D TargetNode { get; }
    bool TargetAlive { get; }
    Rect2 TargetBounds(WorldGrid grid);
    event Action TargetLost;
    void ApplyDamage(float amount);
}

// Cached-reference validity is shared by combat/camera callers, without an enemy or player dependency.
public static class CombatTarget
{
    public static bool IsAlive(ICombatTarget target) => target != null && GodotObject.IsInstanceValid(target.TargetNode) && target.TargetAlive;
}
