// Stable source identity that does not keep removed scene actors alive.
using Godot;
using System;

public enum DamageSourceKind { Unknown = 0, Player, FriendlyBuilding, Enemy, Environment, Debug }

public sealed class DamageSource
{
    private readonly WeakReference<Node> _owner;
    public DamageSourceKind Kind { get; }
    public ulong OwnerId { get; }
    public static DamageSource Debug { get; } = new(DamageSourceKind.Debug);

    // Cache this identity on the emitter and share it with its hits.
    public DamageSource(DamageSourceKind kind, Node owner = null)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
        if (GodotObject.IsInstanceValid(owner))
        {
            OwnerId = owner.GetInstanceId();
            _owner = new(owner);
        }
    }

    // A removed owner is unavailable, but its original ID and kind remain readable.
    public bool TryGetOwner(out Node owner)
    {
        if (_owner != null && _owner.TryGetTarget(out owner) && GodotObject.IsInstanceValid(owner)
            && owner.IsInsideTree() && !owner.IsQueuedForDeletion()) return true;
        owner = null;
        return false;
    }
}
