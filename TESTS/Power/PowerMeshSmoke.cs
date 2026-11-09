// Focused PWR0 editor smoke checks for topology, cached queries and real building lifecycle events.
using Godot;
using System;

public partial class PowerMeshSmoke : Node
{
    // Run this scene directly with F6.
    public override void _Ready()
    {
        CheckTopology();
        CheckBuildingLifecycle();
        GD.Print("PWR0/PWR1 power mesh smoke passed.");
        GetTree().Quit();
    }

    // Exercise synthetic layouts without a battlefield, terrain or a simulation loop.
    private void CheckTopology()
    {
        using var mesh = new PowerNetworkManager();
        long left = mesh.Register(PowerRole.Relay, Vector2.Zero, 100);
        Require(mesh.NetworkCount == 1, "isolated relay");
        long right = mesh.Register(PowerRole.Relay, new(180, 0), 150);
        long leaf = mesh.Register(PowerRole.Consumer, new(90, 0), 100);
        Require(mesh.NetworkCount == 2 && mesh.NetworkFor(leaf).RelayId == left, "leaf does not bridge and ties use lowest ID");

        long bridge = mesh.Register(PowerRole.Relay, new(90, 0), 100);
        long redundant = mesh.Register(PowerRole.Relay, new(90, 30), 100);
        int joinedNetwork = mesh.NetworkFor(left).NetworkId;
        var joinedSnapshot = mesh.ConnectedNodes(joinedNetwork);
        Require(mesh.NetworkCount == 1 && joinedSnapshot.Count == 4, "bridge merges islands");
        mesh.Remove(bridge);
        Require(mesh.NetworkCount == 1 && mesh.NetworkFor(leaf).RelayId == redundant, "redundant route and nearest reattachment");
        mesh.UpdateParticipant(redundant, new(90, 30), 100, false);
        Require(mesh.NetworkCount == 2 && joinedSnapshot.Count == 4, "disable splits and prior snapshot is immutable");
        Require(mesh.Remove(left) && !mesh.Remove(left) && mesh.NetworkFor(leaf).RelayId == right, "idempotent removal and fallback");

        long boundary = mesh.Register(PowerRole.Relay, new(240, 80), 100);
        Require(mesh.AreRelaysLinked(right, boundary), "diagonal exact mutual boundary");
        mesh.UpdateParticipant(boundary, new(240, 80.01f), 100, true);
        Require(!mesh.AreRelaysLinked(right, boundary), "smaller radius rejects just outside");
        mesh.UpdateParticipant(leaf, new(90, 0), 89, true);
        ulong revision = mesh.TopologyRevision;
        Require(mesh.NetworkFor(leaf) == PowerMembership.Disconnected && mesh.EndpointCount == 1, "disconnected leaf still registered");
        Require(mesh.UpdateParticipant(leaf, new(90, 0), 89, true) && mesh.TopologyRevision == revision, "identical update remains clean");
    }

    // Exercise real C1 completion and Building.Detached for distinct roles on the same owner.
    private void CheckBuildingLifecycle()
    {
        var world = new WorldGrid();
        var buildings = new BuildingRegistry();
        var placement = new BuildingPlacement();
        var container = new Node2D();
        var mesh = new PowerNetworkManager();
        AddChild(world); AddChild(buildings); AddChild(placement); AddChild(container); AddChild(mesh);
        placement.Initialize(world, buildings, container);
        var cpuScene = GD.Load<PackedScene>("res://FORTRESS/BUILDINGS/Core/Citadel/Citadel.tscn");
        var towerScene = GD.Load<PackedScene>("res://FORTRESS/BUILDINGS/Defense/OffensiveTowers/Tower001/Tower001.tscn");
        Require(placement.TryPlaceFree(cpuScene, new(1, 1), out Building cpu), "fixture Citadel placement");
        Require(placement.TryPlaceFree(towerScene, new(6, 1), out Building site), "fixture C1 placement");
        long relay = mesh.BindBuilding(cpu, PowerRole.Relay, mesh.CitadelRelayRadius);
        mesh.BindGenerator(cpu, 400, 10);
        var tower = (Tower001)site;
        tower.BindPower(mesh);
        mesh.Distribute(1, 1);
        Require(mesh.NetworkCount == 1 && mesh.EndpointCount == 1
            && mesh.NetworkFor(tower, PowerRole.Consumer) == PowerMembership.Disconnected
            && tower.Capacitor.Charge == 0, "pending site is absent");
        site.StartConstruction();
        site.AdvanceConstruction(site.ConstructionDuration);
        Require(mesh.EndpointCount == 2 && mesh.NetworkFor(site, PowerRole.Consumer).RelayId == relay, "completed site joins");
        Require(placement.TryPlaceFree(towerScene, new(8, 1), out Building secondSite), "second tower placement");
        var second = (Tower001)secondSite;
        second.BindPower(mesh);
        second.StartConstruction();
        second.AdvanceConstruction(second.ConstructionDuration);
        mesh.Distribute(1, 1);
        Require(tower.Capacitor.Charge == 3 && second.Capacitor.Charge == 3 && !tower.Capacitor.IsPowered, "finite shared budget and idle drain");
        mesh.Distribute(2, 2);
        Require(tower.Capacitor.IsPowered && tower.Capacitor.TrySpendShot() && tower.Capacitor.Charge == 5, "restart threshold and prepaid shot");
        cpu.Destroy();
        mesh.Distribute(1, 1);
        Require(mesh.NetworkCount == 0 && mesh.EndpointCount == 2
            && mesh.NetworkFor(tower, PowerRole.Consumer) == PowerMembership.Disconnected
            && tower.Capacitor.Charge == 3 && !tower.Capacitor.IsPowered, "Citadel death cuts supply and stored power drains");
        site.Remove();
        second.Remove();
        Require(mesh.EndpointCount == 0, "site removal clears registration");
    }

    // Surface mismatches in the editor output with the individual contract name.
    private static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException($"PWR0 smoke failed: {name}");
    }
}
