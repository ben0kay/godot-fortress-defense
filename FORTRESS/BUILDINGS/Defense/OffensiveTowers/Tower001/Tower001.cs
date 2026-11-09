// Battle Tower Level 1 opts into the shared initial construction clock and PWR1 capacitor.
using Godot;

public partial class Tower001 : ProjectileTower
{
    [ExportGroup("Construction")]
    [Export] public bool ConstructionEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0.1,120,0.1,or_greater")] public float ConstructionSeconds { get; set; } = 3;
    [ExportGroup("Power")]
    [Export] public TowerPowerConfig PowerConfig { get; set; }

    public PowerCapacitor Capacitor { get; private set; }
    private PowerNetworkManager _power;
    protected override float InitialConstructionSeconds => ConstructionEnabled ? ConstructionSeconds : 0;
    protected override bool WeaponPowered => Capacitor?.IsPowered == true;

    // Bind only after placement. The mesh waits for C1 completion before allocating power.
    internal void BindPower(PowerNetworkManager power)
    {
        if (Capacitor != null) throw new System.InvalidOperationException("Battle Tower power is already bound.");
        var config = PowerConfig ?? throw new System.InvalidOperationException("Battle Tower power settings are required.");
        var buffer = config.CreateCapacitor();
        power.BindConsumer(this, config.ConnectionRadius, buffer);
        _power = power; Capacitor = buffer;
        buffer.PowerStateChanged += RefreshWeaponActivity;
    }

    // Deduct charge once, immediately before spawning the validated projectile.
    protected override bool TryPayForShot()
    {
        if (Capacitor?.TrySpendShot() != true) return false;
        _power.WakeDelivery();
        return true;
    }

    // The mesh removes membership on Building.Detached and drops all references on world exit.
    public override void _ExitTree()
    {
        if (Capacitor != null) Capacitor.PowerStateChanged -= RefreshWeaponActivity;
        _power = null;
        base._ExitTree();
    }
}
