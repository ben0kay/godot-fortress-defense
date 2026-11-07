// Reusable drone pose and flight phase. The dispatcher advances it only during travel/deployment.
using Godot;

internal enum ConstructionDronePhase { Parked, Flying, Deploying, Building, Returning }

public partial class ConstructionDrone : Node2D
{
    internal ConstructionDronePhase Phase { get; private set; }
    internal Building Site { get; private set; }
    private readonly ConstructionDroneMotion _motion = new();
    private Sprite2D _body;
    private ConstructionBeam _beam;
    private float _deployRemaining;

    public override void _Ready()
    {
        _body = GetNode<Sprite2D>("Body");
        _beam = GetNode<ConstructionBeam>("Beam");
    }

    internal void BindFog(FogPresentation fog)
    {
        fog.RegisterDynamic(_body);
        fog.RegisterDynamic(_beam);
    }

    internal void Launch(Building site, Vector2 home, Vector2 destination, ConstructionDroneMasterConfig config)
    {
        Site = site;
        GlobalPosition = home;
        _body.GlobalRotation = (destination - home).Angle() + Mathf.Pi / 2;
        _motion.Begin(home, destination, config);
        Phase = ConstructionDronePhase.Flying;
        Visible = config.ShowVisuals;
    }

    internal void ReturnHome(Vector2 home, ConstructionDroneMasterConfig config)
    {
        Site = null;
        _beam.Hide();
        _motion.Begin(GlobalPosition, home, config);
        Phase = ConstructionDronePhase.Returning;
    }

    internal void Park()
    {
        Site = null;
        _beam.Hide();
        Hide();
        Phase = ConstructionDronePhase.Parked;
    }

    // Return true once the beam has deployed and the manager can release the work clock.
    internal bool Advance(float delta, ConstructionDroneMasterConfig config)
    {
        if (Phase == ConstructionDronePhase.Deploying)
        {
            _deployRemaining -= delta;
            if (_deployRemaining > 0) return false;
            Phase = ConstructionDronePhase.Building;
            return true;
        }
        if (Phase is not (ConstructionDronePhase.Flying or ConstructionDronePhase.Returning)) return false;

        bool arrived = _motion.Advance(delta, config.ArrivalRadius, out Vector2 position);
        Vector2 movement = position - GlobalPosition;
        GlobalPosition = position;
        if (!movement.IsZeroApprox())
        {
            float turn = Mathf.AngleDifference(_body.GlobalRotation, movement.Angle() + Mathf.Pi / 2);
            float step = Mathf.DegToRad(config.TurnDegreesPerSecond) * delta;
            _body.GlobalRotation += Mathf.Clamp(turn, -step, step);
        }
        if (!arrived) return false;
        if (Phase == ConstructionDronePhase.Returning) { Park(); return false; }

        _beam.Deploy(Site.GlobalPosition, Mathf.Min(Site.FootprintPixels.X * .3f, 24), config.BeamColor);
        _deployRemaining = config.BeamDeploymentSeconds;
        Phase = ConstructionDronePhase.Deploying;
        if (_deployRemaining > 0) return false;
        Phase = ConstructionDronePhase.Building;
        return true;
    }
}
