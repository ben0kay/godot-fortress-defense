// Shared bounded decisions: priority acquisition/recovery, fair routine scans, and optional cached player targeting.
using Godot;
using System.Collections.Generic;

public partial class EnemyTargetRoster : Node
{
    [Export] public int MaxDecisionsPerFrame { get; set; } = 4;
    [Export] public int MaxGlobalSearchesPerFrame { get; set; } = 1;
    [Export] public float DecisionInterval { get; set; } = .6f;
    public ICombatTarget MobileTarget { get; private set; }
    public BuildingRegistry Buildings { get; private set; }
    public Node2D Projectiles { get; private set; }
    public int PendingAcquisitions => _priority.Count;
    public long GlobalSearches { get; private set; }
    public long LocalSearches { get; private set; }
    private readonly LinkedList<Enemy> _routine = new(), _priority = new();
    private readonly Dictionary<Enemy, LinkedListNode<Enemy>> _index = new(), _priorityIndex = new();
    private readonly HashSet<Enemy> _hunters = new(), _waiting = new(), _globalPending = new();
    private readonly List<Building> _scratch = new();
    private int _activeHunters, _priorityBurst;
    private double _clock;

    public override void _Ready() => SetProcess(false);
    public void Initialize(BuildingRegistry buildings, Node2D projectiles)
    {
        Buildings = buildings; Projectiles = projectiles; Buildings.Added += OnBuildingAdded;
    }

    // Player loss invalidates cached references immediately; expensive route execution remains queued/staggered.
    public void SetMobileTarget(ICombatTarget target)
    {
        if (MobileTarget == target) return;
        if (MobileTarget != null && GodotObject.IsInstanceValid(MobileTarget.TargetNode)) MobileTarget.TargetLost -= OnTargetLost;
        MobileTarget = EnemyTargeting.Live(target) ? target : null;
        if (MobileTarget != null) MobileTarget.TargetLost += OnTargetLost;
        else foreach (Enemy enemy in _routine) if (!enemy.HuntsBuildings) enemy.ConsiderMobileTarget(null);
        RefreshProcessing();
    }
    public void Register(Enemy enemy)
    {
        if (enemy.HuntsBuildings) { _hunters.Add(enemy); RequestAcquisition(enemy, true); return; }
        if (enemy.TargetRule != EnemyDefinition.TargetPriority.CitadelOnly) AddRoutine(enemy);
        RefreshProcessing();
    }
    private void AddRoutine(Enemy enemy)
    {
        if (_index.ContainsKey(enemy)) return;
        enemy.NextTargetDecision = _clock;
        _index.Add(enemy, _routine.AddLast(enemy)); if (enemy.HuntsBuildings) _activeHunters++;
    }
    private void RemoveRoutine(Enemy enemy)
    {
        if (_index.Remove(enemy, out var node)) { _routine.Remove(node); if (enemy.HuntsBuildings) _activeHunters--; }
    }
    public void Unregister(Enemy enemy)
    {
        RemoveRoutine(enemy); _hunters.Remove(enemy); _waiting.Remove(enemy); _globalPending.Remove(enemy);
        if (_priorityIndex.Remove(enemy, out var node)) _priority.Remove(node);
        RefreshProcessing();
    }

    // One coalesced acquisition per live hunter; destroyed goals bypass normal retarget cooldowns.
    public void RequestAcquisition(Enemy enemy, bool initialGlobal)
    {
        if (enemy.IsDead || !_hunters.Contains(enemy) || _priorityIndex.ContainsKey(enemy)) return;
        RemoveRoutine(enemy); _waiting.Remove(enemy);
        enemy.AcquisitionState = initialGlobal ? "Queued global acquisition" : "Queued local recovery";
        if (initialGlobal) _globalPending.Add(enemy);
        _priorityIndex.Add(enemy, _priority.AddLast(enemy)); RefreshProcessing();
    }
    private void OnBuildingAdded(Building building)
    {
        // Only sleepers wake. Existing locked hunters discover construction at their next local scan.
        foreach (Enemy enemy in _waiting)
        {
            enemy.AcquisitionState = "Queued local recovery";
            _priorityIndex.Add(enemy, _priority.AddLast(enemy));
        }
        _waiting.Clear(); RefreshProcessing();
    }
    private void RefreshProcessing() => SetProcess(_priority.Count > 0 || _activeHunters > 0 || (MobileTarget != null && _routine.Count > 0));
    private void OnTargetLost() => SetMobileTarget(null);

    // At most two priority decisions in succession when a routine decision is due; global scans have a separate cap.
    public override void _Process(double delta)
    {
        _clock += delta; int globals = 0, limit = Mathf.Clamp(MaxDecisionsPerFrame, 1, 32);
        for (int i = 0; i < limit; i++)
        {
            bool routineDue = _routine.First != null && _routine.First.Value.NextTargetDecision <= _clock;
            bool priorityReady = _priority.First != null && (!_globalPending.Contains(_priority.First.Value)
                || globals < Mathf.Clamp(MaxGlobalSearchesPerFrame, 1, 8));
            if (priorityReady && (_priorityBurst < 2 || !routineDue))
            { ProcessAcquisition(ref globals); _priorityBurst++; }
            else if (routineDue) { ProcessRoutine(); _priorityBurst = 0; }
            else break;
        }
        RefreshProcessing();
    }
    private void ProcessAcquisition(ref int globals)
    {
        Enemy enemy = _priority.First.Value; _priority.RemoveFirst(); _priorityIndex.Remove(enemy);
        if (enemy.IsDead) return;
        Building target;
        if (_globalPending.Remove(enemy))
        { globals++; GlobalSearches++; target = BuildingHunterTargeting.Nearest(enemy.GlobalPosition, Buildings.Buildings); }
        else
        {
            LocalSearches++; enemy.LocalTargetScans++; target = BuildingHunterTargeting.Local(Buildings, enemy, _scratch);
            if (target == null && Buildings.Count > 0) { RequestAcquisition(enemy, true); return; }
        }
        if (target == null)
        { _waiting.Add(enemy); enemy.AcquisitionState = "Waiting for buildings"; return; }
        enemy.SetHuntedBuilding(target); enemy.NextStrategicSwitch = _clock + enemy.HunterMinimumSwitchInterval;
        AddRoutine(enemy); ScheduleNext(enemy);
    }
    private void ProcessRoutine()
    {
        var node = _routine.First; Enemy enemy = node.Value;
        _routine.RemoveFirst(); _routine.AddLast(node); ScheduleNext(enemy);
        if (!enemy.HuntsBuildings) { if (MobileTarget != null) enemy.ConsiderMobileTarget(MobileTarget); return; }
        if (!EnemyTargeting.Live(enemy.StrategicTarget)) { RequestAcquisition(enemy, false); return; }
        LocalSearches++; enemy.LocalTargetScans++;
        Building candidate = BuildingHunterTargeting.Local(Buildings, enemy, _scratch);
        if (candidate == null || candidate == enemy.StrategicTarget || _clock < enemy.NextStrategicSwitch) return;
        float ratio = 1 - enemy.HunterSwitchImprovement;
        if (BuildingHunterTargeting.DistanceSquared(enemy.GlobalPosition, candidate)
            >= BuildingHunterTargeting.DistanceSquared(enemy.GlobalPosition, enemy.StrategicTarget) * ratio * ratio) return;
        enemy.SetHuntedBuilding(candidate); enemy.NextStrategicSwitch = _clock + enemy.HunterMinimumSwitchInterval;
    }
    private void ScheduleNext(Enemy enemy)
    {
        float interval = float.IsFinite(DecisionInterval) ? Mathf.Max(.1f, DecisionInterval) : .6f;
        if (enemy.HuntsBuildings)
        {
            float phase = (float)((enemy.GetInstanceId() * 37 + (ulong)enemy.LocalTargetScans * 17) % 997) / 997;
            interval = Mathf.Lerp(enemy.HunterMinimumScanInterval, enemy.HunterMaximumScanInterval, phase);
        }
        enemy.NextTargetDecision = _clock + interval;
    }
    public override void _ExitTree()
    {
        if (MobileTarget != null && GodotObject.IsInstanceValid(MobileTarget.TargetNode)) MobileTarget.TargetLost -= OnTargetLost;
        if (GodotObject.IsInstanceValid(Buildings)) Buildings.Added -= OnBuildingAdded;
        MobileTarget = null; Buildings = null; Projectiles = null;
        _routine.Clear(); _priority.Clear(); _index.Clear(); _priorityIndex.Clear();
        _waiting.Clear(); _hunters.Clear(); _globalPending.Clear(); _scratch.Clear();
    }
}
