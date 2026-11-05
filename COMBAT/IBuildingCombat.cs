// Optional building capability; passive buildings have no combat dependencies or weapon loop.
using Godot;

public interface IBuildingCombat
{
    void BindCombat(WorldGrid grid, EnemyRegistry enemies, Node2D projectiles);
}
