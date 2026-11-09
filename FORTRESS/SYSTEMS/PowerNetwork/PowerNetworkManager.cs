// Level-owned range mesh: relay components, single-relay endpoint attachment and building lifecycle.
using Godot;
using System;
using System.Collections.Generic;

public enum PowerRole { Relay, Consumer, Generator, Battery }

public readonly record struct PowerMembership(int NetworkId, long RelayId)
{
    public static PowerMembership Disconnected => new(-1, 0);
}

public partial class PowerNetworkManager : Node
{
    [Export(PropertyHint.Range, "1,4096,1,or_greater")]
    public float CitadelRelayRadius { get; set; } = 400;

    private sealed class Entry
    {
        public PowerRole Role;
        public Vector2 Position;
        public float Radius;
        public bool Enabled;
        public bool Active;
        public Building Owner;
        public ulong AttachmentRevision;
        public PowerMembership Membership = PowerMembership.Disconnected;
    }

    private sealed class BuildingBinding
    {
        public readonly Dictionary<PowerRole, long> Ids = new();
        public Action Detach;
    }

    private readonly Dictionary<long, Entry> _entries = new();
    private readonly Dictionary<Building, BuildingBinding> _owners = new();
    private readonly SortedSet<long> _activeRelays = new();
    private readonly List<(long Id, Entry Entry)> _unvisited = new();
    private readonly List<long> _frontier = new();
    private readonly List<IReadOnlyList<long>> _components = new();
    private long _nextId;
    private bool _relayDirty;
    private ulong _relayRevision = 1;

    public int NodeCount => _activeRelays.Count;
    public int EndpointCount { get; private set; }
    public int NetworkCount { get { EnsureComponents(); return _components.Count; } }
    public ulong TopologyRevision { get; private set; }

    #region Registration and lifecycle
    // Synthetic or non-building participants use explicit position and eligibility updates.
    public long Register(PowerRole role, Vector2 position, float radius, bool enabled = true)
        => Add(role, position, radius, enabled, null);

    // Bind after successful placement. A building can register independent roles with one lifecycle subscription.
    public long BindBuilding(Building owner, PowerRole role, float radius, bool enabled = true)
    {
        if (!CombatTarget.IsAlive(owner) || !owner.IsInsideTree() || owner.GetTree() != GetTree())
            throw new ArgumentException("Power binding requires a live building placed in this world.", nameof(owner));
        if (_owners.TryGetValue(owner, out var binding) && binding.Ids.ContainsKey(role))
            throw new InvalidOperationException("Building already registered for this power role.");
        long id = Add(role, owner.GlobalPosition, radius, enabled, owner);
        if (binding == null)
        {
            binding = new BuildingBinding { Detach = () => RemoveBuilding(owner) };
            _owners.Add(owner, binding);
            owner.Detached += binding.Detach;
            if (!owner.IsOperational) owner.WorkCompleted += OnConstructionCompleted;
        }
        binding.Ids.Add(role, id);
        return id;
    }

    // IDs increase for the lifetime of the service and are never reassigned to replacements.
    private long Add(PowerRole role, Vector2 position, float radius, bool enabled, Building owner)
    {
        if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role));
        ValidateGeometry(position, radius);
        long id = checked(++_nextId);
        var entry = new Entry { Role = role, Owner = owner };
        _entries.Add(id, entry);
        SetEntry(id, entry, position, radius, enabled);
        return id;
    }

    // Call after a committed relocation, radius edit or enable/disable. Construction eligibility still applies.
    public bool UpdateParticipant(long id, Vector2 position, float radius, bool enabled)
    {
        if (!_entries.TryGetValue(id, out var entry)) return false;
        ValidateGeometry(position, radius);
        SetEntry(id, entry, position, radius, enabled);
        return true;
    }

    // Make inactive edits free of graph work, and skip invalidation for identical effective state.
    private void SetEntry(long id, Entry entry, Vector2 position, float radius, bool enabled)
    {
        bool wasActive = entry.Active;
        bool active = enabled && (entry.Owner == null || (CombatTarget.IsAlive(entry.Owner) && entry.Owner.IsOperational));
        bool changed = wasActive != active || (active && (entry.Position != position || entry.Radius != radius));
        entry.Position = position; entry.Radius = radius; entry.Enabled = enabled; entry.Active = active;
        if (changed) Invalidate(id, entry, wasActive);
    }

    // Completion promotes only opted-in and enabled roles.
    private void OnConstructionCompleted(Building owner)
    {
        owner.WorkCompleted -= OnConstructionCompleted;
        if (!_owners.TryGetValue(owner, out var binding)) return;
        foreach (long id in binding.Ids.Values)
        {
            Entry entry = _entries[id];
            SetEntry(id, entry, entry.Position, entry.Radius, entry.Enabled);
        }
    }

    // Explicit removal is idempotent and drops the owner subscription when its last role leaves.
    public bool Remove(long id)
    {
        if (!_entries.Remove(id, out var entry)) return false;
        if (entry.Active) { entry.Active = false; Invalidate(id, entry, true); }
        if (entry.Owner != null && _owners.TryGetValue(entry.Owner, out var binding))
        {
            binding.Ids.Remove(entry.Role);
            if (binding.Ids.Count == 0) RemoveBuilding(entry.Owner);
        }
        return true;
    }

    // Detach fires on destruction, ordinary removal and owner exit. Either subtree can exit first.
    private void RemoveBuilding(Building owner)
    {
        if (!_owners.Remove(owner, out var binding)) return;
        Unsubscribe(owner, binding);
        foreach (long id in binding.Ids.Values) Remove(id);
    }

    private void Unsubscribe(Building owner, BuildingBinding binding)
    {
        if (!GodotObject.IsInstanceValid(owner)) return;
        owner.Detached -= binding.Detach;
        owner.WorkCompleted -= OnConstructionCompleted;
    }

    // Reset without retaining actor references or cached component membership.
    public override void _ExitTree()
    {
        foreach (var pair in _owners) Unsubscribe(pair.Key, pair.Value);
        _owners.Clear(); _entries.Clear(); _activeRelays.Clear(); _components.Clear();
        _unvisited.Clear(); _frontier.Clear();
        EndpointCount = 0; _relayDirty = false; TopologyRevision++;
    }

    private static void ValidateGeometry(Vector2 position, float radius)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) throw new ArgumentOutOfRangeException(nameof(position));
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
    }
    #endregion

    #region Read-only queries and lazy cache
    // Relay edits invalidate the backbone. Endpoint edits invalidate only that endpoint's cached attachment.
    private void Invalidate(long id, Entry entry, bool wasActive)
    {
        TopologyRevision++;
        if (entry.Role == PowerRole.Relay)
        {
            if (entry.Active) _activeRelays.Add(id);
            else _activeRelays.Remove(id);
            _relayDirty = true;
        }
        else
        {
            if (entry.Active != wasActive) EndpointCount += entry.Active ? 1 : -1;
            entry.AttachmentRevision = 0;
        }
    }

    // O(1) on the clean path. An endpoint with no mutual-range relay is disconnected.
    public PowerMembership NetworkFor(long id)
    {
        if (!_entries.TryGetValue(id, out var entry) || !entry.Active) return PowerMembership.Disconnected;
        EnsureComponents();
        if (entry.Role != PowerRole.Relay && entry.AttachmentRevision != _relayRevision)
        {
            entry.Membership = FindNearest(entry);
            entry.AttachmentRevision = _relayRevision;
        }
        return entry.Membership;
    }

    // Lookup is role-specific because one building may be both a relay and a generator.
    public PowerMembership NetworkFor(Building owner, PowerRole role = PowerRole.Relay)
        => owner != null && _owners.TryGetValue(owner, out var binding) && binding.Ids.TryGetValue(role, out long id)
            ? NetworkFor(id) : PowerMembership.Disconnected;

    // This immutable snapshot remains readable after later mutations. Network IDs are revision-scoped.
    public IReadOnlyList<long> ConnectedNodes(int networkId)
    {
        EnsureComponents();
        return (uint)networkId < (uint)_components.Count ? _components[networkId] : Array.Empty<long>();
    }

    // Direct-link query for inspecting physical relay edges, distinct from component membership.
    public bool AreRelaysLinked(long firstId, long secondId)
        => firstId != secondId && _entries.TryGetValue(firstId, out var first)
            && _entries.TryGetValue(secondId, out var second)
            && first.Active && second.Active && first.Role == PowerRole.Relay && second.Role == PowerRole.Relay
            && InRange(first, second, out _);

    // Traverse implicit edges and remove discovered relays from candidate scans in O(1).
    // Stable input order makes traversal deterministic. Sparse layouts have an O(relays²) worst case.
    private void EnsureComponents()
    {
        if (!_relayDirty) return;
        _relayDirty = false; _relayRevision++;
        _components.Clear();
        foreach (long id in _activeRelays) _unvisited.Add((id, _entries[id]));

        while (_unvisited.Count > 0)
        {
            int network = _components.Count;
            int last = _unvisited.Count - 1;
            var root = _unvisited[last];
            _unvisited.RemoveAt(last);
            root.Entry.Membership = new(network, root.Id);
            _frontier.Add(root.Id);
            for (int i = 0; i < _frontier.Count && _unvisited.Count > 0; i++)
            {
                Entry current = _entries[_frontier[i]];
                int candidate = 0;
                while (candidate < _unvisited.Count)
                {
                    var other = _unvisited[candidate];
                    if (!InRange(current, other.Entry, out _)) { candidate++; continue; }
                    other.Entry.Membership = new(network, other.Id);
                    _frontier.Add(other.Id);
                    last = _unvisited.Count - 1;
                    _unvisited[candidate] = _unvisited[last];
                    _unvisited.RemoveAt(last);
                }
            }
            _components.Add(Array.AsReadOnly(_frontier.ToArray()));
            _frontier.Clear();
        }
    }

    // IDs are iterated in registration order, so equal distances keep the lowest live relay ID.
    private PowerMembership FindNearest(Entry endpoint)
    {
        double bestDistance = double.PositiveInfinity;
        PowerMembership best = PowerMembership.Disconnected;
        foreach (long id in _activeRelays)
        {
            Entry relay = _entries[id];
            if (!InRange(endpoint, relay, out double distance) || distance >= bestDistance) continue;
            bestDistance = distance; best = relay.Membership;
        }
        return best;
    }

    // Double intermediates keep squared range and distance finite for all valid float geometry.
    private static bool InRange(Entry a, Entry b, out double distanceSquared)
    {
        double dx = (double)a.Position.X - b.Position.X, dy = (double)a.Position.Y - b.Position.Y;
        double radius = Math.Min(a.Radius, b.Radius);
        distanceSquared = dx * dx + dy * dy;
        return distanceSquared <= radius * radius;
    }
    #endregion
}
