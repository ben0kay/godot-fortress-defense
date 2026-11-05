// World-scoped compact visibility state, overlapping source contributions and bounded scheduling.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public enum FogCellState : byte { Unexplored, Explored, Visible }

public partial class FogOfWar : Node
{
    [Export] public FogMasterConfig Config { get; set; }
    public WorldGrid Grid { get; private set; }
    public Vector2I Dimensions => Grid.Dimensions;
    public ReadOnlyMemory<FogCellState> CellStates => _states;
    public int VisibleCells { get; private set; }
    public int ExploredCells { get; private set; }
    public int SourceCount => _sources.Count;
    public int LastUpdatedCells { get; private set; }
    public double LastUpdateMilliseconds { get; private set; }
    public bool PresentationEnabled { get; private set; }
    public event Action<Rect2I> VisibilityChanged;
    public event Action PresentationChanged;
    // Transient batch, valid during VisibilityChanged only; consumers must not retain it.
    public IReadOnlyList<int> ChangedCells => _dirty;
    private FogCellState[] _states;
    private ushort[] _counts;
    private bool[] _dirtyFlags;
    private readonly List<int> _dirty = new();
    private readonly Dictionary<ulong, Source> _sources = new();
    private readonly LinkedList<Source> _pending = new(), _moving = new();
    private LinkedListNode<Source> _cursor;
    private bool _closing;

    private sealed class Source
    {
        public ulong Id;
        public Node2D Owner;
        public float Radius;
        public bool Enabled = true, Applied;
        public Vector2 Position;
        public double NextCheck;
        public Action Lost;
        public ICombatTarget Target;
        public List<int> Cells = new(), Scratch = new();
        public LinkedListNode<Source> Pending, Moving;
    }

    // Composition supplies the same authoritative grid used by terrain, movement and placement.
    public void Initialize(WorldGrid grid, bool enabled)
    {
        if (Grid != null) throw new InvalidOperationException("Fog is already initialized.");
        Grid = grid; Config ??= GD.Load<FogMasterConfig>("res://WORLD/FOG/FogMasterConfig.tres"); Config.Validate();
        int length = Dimensions.X * Dimensions.Y;
        _states = new FogCellState[length]; _counts = new ushort[length]; _dirtyFlags = new bool[length];
        PresentationEnabled = enabled && Config.EnabledByDefault;
        GetNode<FogRenderer>("Renderer").Initialize(this);
        SetProcess(false);
    }
    public bool IsVisible(Vector2 position) => IsVisible(Grid.WorldToCell(position));
    public bool IsVisible(Vector2I cell) => Grid.Contains(cell) && _states[cell.Y * Dimensions.X + cell.X] == FogCellState.Visible;
    public bool IsExplored(Vector2I cell) => Grid.Contains(cell) && _states[cell.Y * Dimensions.X + cell.X] != FogCellState.Unexplored;
    public bool IsExplored(Vector2 position) => IsExplored(Grid.WorldToCell(position));

    // Stable IDs belong to callers; sources may opt into bounded movement sampling.
    public void RegisterSource(ulong id, Node2D owner, float radius, bool moving = false)
    {
        if (!GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree()) throw new ArgumentException("Vision source must be in the world.");
        ValidateRadius(radius);
        if (_sources.ContainsKey(id)) throw new ArgumentException("Duplicate vision source ID.");
        var source = new Source { Id = id, Owner = owner, Radius = radius, Target = owner as ICombatTarget,
            NextCheck = Time.GetTicksMsec() * .001 + (id % 97) / 97.0 * Config.VisibilityUpdateInterval };
        source.Lost = () => UnregisterSource(id);
        _sources.Add(id, source); owner.TreeExiting += source.Lost;
        if (source.Target != null) source.Target.TargetLost += source.Lost;
        if (moving) source.Moving = _moving.AddLast(source);
        Enqueue(source);
    }
    public void SetSourceEnabled(ulong id, bool enabled)
    { if (_sources.TryGetValue(id, out var source) && source.Enabled != enabled) { source.Enabled = enabled; Enqueue(source); } }
    public void SetSourceRadius(ulong id, float radius)
    { ValidateRadius(radius); if (_sources.TryGetValue(id, out var source) && source.Radius != radius) { source.Radius = radius; Enqueue(source); } }
    // Explicitly refresh a relocated static source; moving sources are sampled automatically.
    public void RequestSourceUpdate(ulong id)
    { if (_sources.TryGetValue(id, out var source)) Enqueue(source); }
    private static void ValidateRadius(float radius)
    { if (!float.IsFinite(radius) || radius < 0) throw new ArgumentException("Vision radius must be finite and nonnegative."); }

    // Remove only this source's contribution; other overlapping sources remain visible.
    public void UnregisterSource(ulong id)
    {
        if (!_sources.Remove(id, out var source)) return;
        if (GodotObject.IsInstanceValid(source.Owner)) source.Owner.TreeExiting -= source.Lost;
        if (source.Target != null && GodotObject.IsInstanceValid(source.Owner)) source.Target.TargetLost -= source.Lost;
        if (source.Pending != null) _pending.Remove(source.Pending);
        if (source.Moving != null)
        {
            if (_cursor == source.Moving) _cursor = source.Moving.Next;
            _moving.Remove(source.Moving);
        }
        if (!_closing) { foreach (int index in source.Cells) RemoveCell(index); SetProcess(true); }
    }
    private void Enqueue(Source source)
    { source.Pending ??= _pending.AddLast(source); SetProcess(true); }

    // Source decisions and circle recalculations share one frame budget; static sources do not poll.
    public override void _Process(double delta)
    {
        if (Grid == null || _closing) return;
        long started = Stopwatch.GetTimestamp();
        int budget = Config.MaximumVisionUpdatesPerFrame;
        // Building bursts stay bounded without starving moving-player checks at the default budget.
        int reserved = _moving.Count > 0 && budget > 1 ? 1 : 0;
        while (budget > reserved && _pending.First != null)
        {
            Source source = _pending.First.Value; _pending.RemoveFirst(); source.Pending = null;
            UpdateSource(source); budget--;
        }
        int checks = Math.Min(budget, _moving.Count);
        double now = Time.GetTicksMsec() * .001;
        while (checks-- > 0)
        {
            _cursor ??= _moving.First;
            Source source = _cursor.Value; _cursor = _cursor.Next;
            if (source.Pending != null || !source.Enabled || now < source.NextCheck) continue;
            source.NextCheck = now + Config.VisibilityUpdateInterval;
            Vector2 position = source.Owner.GlobalPosition;
            bool changed = Config.UpdateOnSourceCellChange ? Grid.WorldToCell(position) != Grid.WorldToCell(source.Position)
                : position.DistanceSquaredTo(source.Position) > .01f;
            if (!source.Applied || changed) UpdateSource(source);
        }
        FlushChanges();
        LastUpdateMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        SetProcess(_pending.Count > 0 || _moving.Count > 0 || _dirty.Count > 0);
    }

    // Circular cell-center coverage within local bounds; sorted lists permit an allocation-free merge.
    private void UpdateSource(Source source)
    {
        source.Scratch.Clear();
        source.Position = source.Owner.GlobalPosition; source.Applied = true;
        if (source.Enabled && (source.Target == null || source.Target.TargetAlive))
        {
            Vector2I min = Grid.WorldToCell(source.Position - Vector2.One * source.Radius);
            Vector2I max = Grid.WorldToCell(source.Position + Vector2.One * source.Radius);
            for (int y = Math.Max(0, min.Y); y <= Math.Min(Dimensions.Y - 1, max.Y); y++)
                for (int x = Math.Max(0, min.X); x <= Math.Min(Dimensions.X - 1, max.X); x++)
                    if (Grid.CellCenter(new(x, y)).DistanceSquaredTo(source.Position) <= source.Radius * source.Radius)
                        source.Scratch.Add(y * Dimensions.X + x);
        }
        int a = 0, b = 0;
        while (a < source.Cells.Count || b < source.Scratch.Count)
        {
            int old = a < source.Cells.Count ? source.Cells[a] : int.MaxValue;
            int next = b < source.Scratch.Count ? source.Scratch[b] : int.MaxValue;
            if (old == next) { a++; b++; }
            else if (old < next) { RemoveCell(old); a++; }
            else { AddCell(next); b++; }
        }
        (source.Cells, source.Scratch) = (source.Scratch, source.Cells);
    }
    private void AddCell(int index)
    {
        if (_counts[index] == ushort.MaxValue) throw new InvalidOperationException("Too many overlapping vision sources.");
        if (_counts[index]++ != 0) return;
        if (_states[index] == FogCellState.Unexplored) ExploredCells++;
        _states[index] = FogCellState.Visible; VisibleCells++; Dirty(index);
    }
    private void RemoveCell(int index)
    {
        if (_counts[index] == 0) throw new InvalidOperationException("Unbalanced vision contribution.");
        if (--_counts[index] != 0) return;
        _states[index] = FogCellState.Explored; VisibleCells--; Dirty(index);
    }
    // Friendly construction is known, but never grants current vision.
    public void MarkExplored(Rect2I area)
    {
        area = area.Intersection(Grid.Bounds);
        for (int y = area.Position.Y; y < area.End.Y; y++)
            for (int x = area.Position.X; x < area.End.X; x++)
            {
                int index = y * Dimensions.X + x;
                if (_states[index] != FogCellState.Unexplored) continue;
                _states[index] = FogCellState.Explored; ExploredCells++; Dirty(index);
            }
        if (_dirty.Count > 0) SetProcess(true);
    }
    private void Dirty(int index)
    { if (!_dirtyFlags[index]) { _dirtyFlags[index] = true; _dirty.Add(index); } }
    private void FlushChanges()
    {
        LastUpdatedCells = _dirty.Count;
        if (_dirty.Count == 0) return;
        Vector2I min = Dimensions, max = Vector2I.Zero;
        foreach (int index in _dirty)
        { var cell = new Vector2I(index % Dimensions.X, index / Dimensions.X); min = new(Math.Min(min.X, cell.X), Math.Min(min.Y, cell.Y)); max = new(Math.Max(max.X, cell.X), Math.Max(max.Y, cell.Y)); }
        VisibilityChanged?.Invoke(new Rect2I(min, max - min + Vector2I.One));
        foreach (int index in _dirty) _dirtyFlags[index] = false;
        _dirty.Clear();
    }
    public void SetPresentationEnabled(bool enabled)
    { if (PresentationEnabled != enabled) { PresentationEnabled = enabled; PresentationChanged?.Invoke(); } }
    public override void _ExitTree()
    {
        _closing = true;
        foreach (ulong id in new List<ulong>(_sources.Keys)) UnregisterSource(id);
        _dirty.Clear(); _states = null; _counts = null; _dirtyFlags = null;
    }
}
