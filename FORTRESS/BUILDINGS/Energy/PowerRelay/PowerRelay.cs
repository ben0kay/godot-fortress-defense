// Constructible relay: shared C1 timing and static artwork, with mesh ownership delegated to the level.
using Godot;
using System;

public partial class PowerRelay : Building
{
    private Sprite2D _body;
    private Node2D _visuals;
    private PowerRelayDefinition Settings => Definition as PowerRelayDefinition
        ?? throw new InvalidOperationException("Power Relay requires a PowerRelayDefinition.");
    protected override float InitialConstructionSeconds => Settings.CheckedConstructionSeconds();

    // Place through the normal transaction. PowerNetworkManager activates the relay on WorkCompleted.
    internal void BindPower(PowerNetworkManager mesh) => mesh.BindBuilding(this, PowerRole.Relay, Settings.LinkRadius);

    public override void _Ready()
    {
        base._Ready();
        _visuals = GetNode<Node2D>("Visuals"); _body = _visuals.GetNode<Sprite2D>("RelayBody");
        Placed += ResizeArtwork;
        WorkCompleted += RevealBody;
        PresentationChanged += QueueRedraw;
        SelectionChanged += QueueRedraw;
        _body.Visible = IsOperational;
    }

    // Match the grid footprint at both Level001 and DEBUG cell sizes.
    private void ResizeArtwork()
    {
        _visuals.Scale = Vector2.One * (Mathf.Min(FootprintPixels.X, FootprintPixels.Y) / 64);
        QueueRedraw();
    }

    private void RevealBody(Building site) => _body.Show();

    // Selection is world-space and never reveals the relay through the fog.
    public override void _Draw()
    {
        if (!IsPlaced || !IsSelected || !HasCurrentVision) return;
        DrawRect(new Rect2(-FootprintPixels * .5f, FootprintPixels).Grow(2), Colors.Yellow, false, 2);
    }

    public override void _ExitTree()
    {
        Placed -= ResizeArtwork;
        WorkCompleted -= RevealBody;
        PresentationChanged -= QueueRedraw;
        SelectionChanged -= QueueRedraw;
        base._ExitTree();
    }
}
