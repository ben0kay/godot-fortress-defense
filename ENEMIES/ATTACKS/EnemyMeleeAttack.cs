// Contact-attack cooldown is separate from path search and movement, including offscreen combat.
public sealed class EnemyMeleeAttack
{
    private float _remaining;
    public void Tick(double delta) => _remaining = System.Math.Max(0, _remaining - (float)delta);

    // At most one hit per physics tick; no accumulated burst after a pause or long frame.
    public bool TryAttack(Enemy enemy, WorldGrid grid, ICombatTarget target, bool ignoreGround = false)
    {
        if (_remaining > 0 || !EnemyTargeting.CanAttackFrom(grid, enemy.GlobalPosition, target, enemy.AttackRange, 0, ignoreGround)) return false;
        _remaining = enemy.AttackCooldown; target.ApplyDamage(enemy.AttackDamage); return true;
    }
}
