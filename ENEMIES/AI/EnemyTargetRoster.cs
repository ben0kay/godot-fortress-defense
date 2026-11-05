// Optional cached mobile objective and bounded round-robin decisions; no scene searches or private path grids.
using Godot;
using System.Collections.Generic;

public partial class EnemyTargetRoster : Node
{
    [Export] public int MaxDecisionsPerFrame { get; set; } = 4;
    [Export] public float DecisionInterval { get; set; } = .6f;
    public ICombatTarget MobileTarget { get; private set; }
    private readonly LinkedList<Enemy> _enemies = new();
    private readonly Dictionary<Enemy, LinkedListNode<Enemy>> _index = new();
    private double _clock;

    public override void _Ready() => SetProcess(false);

    // Explicit injection accepts any mobile damage target; absence leaves this service entirely dormant.
    public void SetMobileTarget(ICombatTarget target)
    {
        if (MobileTarget == target) return;
        if (MobileTarget != null && GodotObject.IsInstanceValid(MobileTarget.TargetNode)) MobileTarget.TargetLost -= OnTargetLost;
        MobileTarget = EnemyTargeting.Live(target) ? target : null;
        if (MobileTarget != null) MobileTarget.TargetLost += OnTargetLost;
        else foreach (Enemy enemy in _enemies) enemy.ConsiderMobileTarget(null);
        RefreshProcessing();
    }
    public void Register(Enemy enemy)
    {
        if (enemy.TargetRule == EnemyDefinition.TargetPriority.CitadelOnly || _index.ContainsKey(enemy)) return;
        enemy.NextTargetDecision = _clock;
        _index.Add(enemy, _enemies.AddLast(enemy)); RefreshProcessing();
    }
    public void Unregister(Enemy enemy)
    { if (_index.Remove(enemy, out var node)) _enemies.Remove(node); RefreshProcessing(); }
    private void RefreshProcessing() => SetProcess(MobileTarget != null && _enemies.Count > 0);
    private void OnTargetLost() => SetMobileTarget(null);

    // At most N inexpensive decisions per frame; any resulting routes still go through the existing bounded queue.
    public override void _Process(double delta)
    {
        _clock += delta;
        int limit = Mathf.Min(_enemies.Count, Mathf.Clamp(MaxDecisionsPerFrame, 1, 32));
        for (int i = 0; i < limit; i++)
        {
            var node = _enemies.First;
            if (node.Value.NextTargetDecision > _clock) break;
            _enemies.RemoveFirst(); _enemies.AddLast(node);
            node.Value.NextTargetDecision = _clock + (float.IsFinite(DecisionInterval) ? Mathf.Max(.1f, DecisionInterval) : .6f);
            node.Value.ConsiderMobileTarget(MobileTarget);
        }
    }
    public override void _ExitTree()
    {
        if (MobileTarget != null && GodotObject.IsInstanceValid(MobileTarget.TargetNode)) MobileTarget.TargetLost -= OnTargetLost;
        MobileTarget = null; _enemies.Clear(); _index.Clear();
    }
}
