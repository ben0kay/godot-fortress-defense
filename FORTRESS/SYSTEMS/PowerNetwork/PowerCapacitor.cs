// Tower-owned charge and hysteresis. Generation and delivery are owned by the battlefield.
using System;

public sealed class PowerCapacitor
{
    public double Charge { get; private set; }
    public double Capacity { get; }
    public bool IsPowered { get; private set; }
    internal event Action PowerStateChanged;
    private readonly double _idle, _shot, _recharge, _restart;

    // New capacitors start empty. Reconnection preserves existing charge.
    internal PowerCapacitor(double capacity, double idle, double shot, double recharge, double restart)
    { Capacity = capacity; _idle = idle; _shot = shot; _recharge = recharge; _restart = restart; }

    // Request only the usable portion of this interval's generation, including idle consumption.
    internal double Request(double seconds, double supplySeconds)
        => Math.Min(_recharge * supplySeconds, Capacity - Charge + _idle * seconds);

    // Apply allocation and idle drain once, then publish only a real power-state transition.
    internal void Advance(double seconds, double grant)
    {
        Charge = Math.Clamp(Charge + grant - _idle * seconds, 0, Capacity);
        SetPowered(Charge >= (IsPowered ? _shot : _restart));
    }

    // A dormant buffer needs a wake only when topology changes or a shot spends stored energy.
    internal bool NeedsService(bool hasSupply) => (_idle > 0 && Charge > 0)
        || (hasSupply && _recharge > 0 && (Charge < Capacity || _idle > 0));

    // Called only after aim, cooldown and line-of-sight have accepted a shot.
    internal bool TrySpendShot()
    {
        if (!IsPowered) return false;
        if (Charge < _shot) { SetPowered(false); return false; }
        Charge -= _shot;
        return true;
    }

    private void SetPowered(bool powered)
    {
        if (IsPowered == powered) return;
        IsPowered = powered;
        PowerStateChanged?.Invoke();
    }
}
