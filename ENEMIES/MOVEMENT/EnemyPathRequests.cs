// ST1 coalescing FIFO navigation work; bounded flood visits and shared native A* calls per physics tick.
using Godot;
using System.Collections.Generic;
using System.Diagnostics;

public partial class EnemyPathRequests : Node
{
    [Export] public int MaxVisitedCellsPerFrame { get; set; } = 512;
    [Export] public int MaxPathsPerFrame { get; set; } = 1;
    [Export] public float SoftBudgetMilliseconds { get; set; } = 1.5f;
    private readonly LinkedList<Enemy> _queue = new();
    private readonly Dictionary<Enemy, LinkedListNode<Enemy>> _queued = new();
    private WorldGrid _grid;
    private GroundNavigation _navigation;
    private GroundReachability _probe;
    private Enemy _active;
    private Vector2I _origin, _goal;
    private ulong _revision;
    private ICombatTarget _chosen;
    private Building _breach;
    private Vector2I _breachGoal;
    private float _breachScore;
    private bool _readyForPath;

    public void Initialize(WorldGrid grid, GroundNavigation navigation)
    { _grid = grid; _navigation = navigation; _probe = new GroundReachability(grid, navigation); SetPhysicsProcess(false); }

    // Coalescing and FIFO avoid duplicate work/starvation without an ST2 priority framework.
    public void Request(Enemy enemy)
    {
        if (enemy.IsFlying || _active == enemy || _queued.ContainsKey(enemy)) return;
        _queued.Add(enemy, _queue.AddLast(enemy)); SetPhysicsProcess(true);
    }
    public void Cancel(Enemy enemy)
    {
        if (_queued.Remove(enemy, out var node)) _queue.Remove(node);
        if (_active == enemy) _active = null;
        if (_active == null && _queue.Count == 0) SetPhysicsProcess(false);
    }

    // Frozen origin is safe because an enemy pauses while its replacement request is queued.
    private void BeginNext()
    {
        _active = _queue.First.Value; _queued.Remove(_active); _queue.RemoveFirst();
        _origin = _grid.WorldToCell(_active.GlobalPosition); _revision = _grid.Revision;
        _chosen = _breach = null; _breachScore = float.PositiveInfinity; _readyForPath = false;
        _probe.Begin(_origin);
    }

    // Reachability proof is cooperative. Native A* is indivisible, so the time limit is a soft cap.
    public override void _PhysicsProcess(double delta)
    {
        if (_grid == null) return;
        long started = Stopwatch.GetTimestamp(); int visited = 0, paths = 0, completions = 0;
        int visitLimit = Mathf.Clamp(MaxVisitedCellsPerFrame, 1, 8192), pathLimit = Mathf.Clamp(MaxPathsPerFrame, 1, 8);
        while ((_active != null || _queue.Count > 0) && completions < pathLimit && visited < visitLimit)
        {
            if (SoftBudgetMilliseconds > 0 && Stopwatch.GetElapsedTime(started).TotalMilliseconds >= SoftBudgetMilliseconds) break;
            if (_active == null) BeginNext();
            if (!GodotObject.IsInstanceValid(_active) || _active.IsDead) { _active = null; continue; }
            if (!EnemyTargeting.Live(_active.StrategicTarget)) { Finish(null, System.Array.Empty<Vector2>()); completions++; continue; }
            if (_revision != _grid.Revision)
            { Enemy retry = _active; _active = null; Request(retry); continue; }
            if (_readyForPath)
            {
                if (paths >= pathLimit) break;
                Vector2[] route = _navigation.FindPath(_origin, _goal); paths++;
                Finish(route.Length == 0 ? null : _chosen, route); completions++; continue;
            }
            if (_probe.TryVisit(out Vector2I cell))
            {
                visited++;
                if (EnemyTargeting.CanAttackFrom(_grid, _grid.CellCenter(cell), _active.NavigationTarget, _active.AttackRange, _active.FiringClearance))
                { _chosen = _active.NavigationTarget; _goal = cell; _readyForPath = true; continue; }
                Building candidate = _active.NavigationTarget != _active.StrategicTarget ? null : EnemyTargeting.BreachAt(_grid, cell, _active.StrategicTarget, _active.AttackRange);
                if (candidate != null)
                {
                    float score = candidate.GlobalPosition.DistanceSquaredTo(_active.StrategicTarget.GlobalPosition)
                        + _grid.CellCenter(cell).DistanceSquaredTo(_active.GlobalPosition) * .1f;
                    if (score < _breachScore) { _breach = candidate; _breachGoal = cell; _breachScore = score; }
                }
            }
            else if (EnemyTargeting.Live(_breach))
            { _chosen = _breach; _goal = _breachGoal; _readyForPath = true; }
            else { Finish(null, System.Array.Empty<Vector2>()); completions++; }
        }
        if (_active == null && _queue.Count == 0) SetPhysicsProcess(false);
    }
    private void Finish(ICombatTarget target, Vector2[] route)
    { Enemy enemy = _active; _active = null; enemy.AcceptRoute(target, route); }
    public override void _ExitTree() { _queue.Clear(); _queued.Clear(); _active = null; }
}
