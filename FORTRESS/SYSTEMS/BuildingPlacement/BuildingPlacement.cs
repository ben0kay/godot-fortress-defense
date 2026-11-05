// Sole placement boundary: cached availability, validated purchase/claim, and explicit free setup/debug.
using Godot;
using System;
using System.Collections.Generic;

public partial class BuildingPlacement : Node
{
    public Func<Rect2I, bool> AdditionalValidation { get; set; }
    public event Action<Building> Placed;
    public event Action CatalogueChanged;
    public IReadOnlyDictionary<string, BuildingCatalogueEntry> Catalogue { get; private set; }
    private readonly HashSet<string> _allowed = new(StringComparer.Ordinal);
    private WorldGrid _grid;
    private BuildingRegistry _registry;
    private Node2D _container;
    private ResourceWallet _wallet;
    private bool _busy;

    // World composition supplies dependencies; this service has no global scene searches.
    public void Initialize(WorldGrid grid, BuildingRegistry registry, Node2D container)
    { _grid = grid; _registry = registry; _container = container; }

    // Cache static metadata once per level; menus can subscribe without polling or scene instantiation.
    public void ConfigurePurchases(BuildingCatalogue catalogue, ResourceWallet wallet, string[] allowedIds)
    {
        if (_busy || wallet == null || catalogue == null) throw new InvalidOperationException("Invalid purchase setup.");
        Catalogue = catalogue.CreateLookup(); _wallet = wallet;
        SetAllowedBuildingIds(allowedIds);
    }

    // Empty means nothing unlocked; unknown IDs indicate an authoring typo rather than a free unlock.
    public void SetAllowedBuildingIds(string[] ids)
    {
        if (_busy || Catalogue == null || ids == null) throw new InvalidOperationException("Invalid building allowlist.");
        foreach (string id in ids)
            if (id == null || !Catalogue.ContainsKey(id)) throw new InvalidOperationException("Unknown allowed building ID: " + id);
        _allowed.Clear(); foreach (string id in ids) _allowed.Add(id);
        CatalogueChanged?.Invoke();
    }
    public bool IsAvailable(string id) => id != null && Catalogue != null && _allowed.Contains(id)
        && Catalogue.TryGetValue(id, out var entry) && entry.Definition.Purchasable;

    // Actor overlap and complete footprints are checked alongside authoritative grid occupancy.
    public bool CanPlace(Rect2I area) => _grid != null && _grid.CanOccupy(area) && (AdditionalValidation?.Invoke(area) ?? true);

    // Menu/debug hotkeys share this transaction: validate all inputs, claim, debit exactly once, then notify.
    public bool TryPurchaseAndPlace(string id, Vector2I anchor, out Building building, out string failure)
    {
        building = null; failure = "Building unavailable or locked";
        if (_busy || _wallet == null || !IsAvailable(id)) return false;
        var entry = Catalogue[id];
        try { entry.Definition.Validate(); }
        catch (Exception) { failure = "Invalid building definition or price"; return false; }
        failure = "Placement blocked by terrain, buildings, reserved spawn, enemies or map edge";
        if (!CanPlace(new Rect2I(anchor, entry.Definition.Footprint))) return false;
        failure = "Insufficient resources";
        if (!_wallet.CanAfford(entry.Definition.Cost)) return false;
        Building placed = null; _busy = true;
        try
        {
            failure = "Placement failed; no resources charged";
            if (!_wallet.TryPurchase(entry.Definition.Cost, () => TryClaim(entry.Scene, anchor, entry.Definition, out placed))) return false;
            building = placed; failure = "";
            _wallet.PublishBalanceChanged(); Placed?.Invoke(placed);
            return true;
        }
        finally { _busy = false; }
    }

    // Explicit bypass for the initial CPU and freely configured technical sandboxes; never refunds/debits.
    public bool TryPlaceFree(PackedScene scene, Vector2I anchor, out Building building)
    {
        building = null;
        if (_busy) return false;
        _busy = true;
        try
        {
            if (!TryClaim(scene, anchor, null, out building)) return false;
            Placed?.Invoke(building); return true;
        }
        finally { _busy = false; }
    }

    // Any failed claim frees the candidate, invoking canonical occupancy/registry cleanup even after AddChild.
    private bool TryClaim(PackedScene scene, Vector2I anchor, BuildingDefinition expected, out Building building)
    {
        building = null; Node instance = null;
        if (scene == null || _grid == null || !GodotObject.IsInstanceValid(_container)) return false;
        try
        {
            instance = scene.Instantiate();
            if (instance is not Building candidate || (expected != null && candidate.Definition != expected)) return false;
            candidate.Initialize();
            var area = new Rect2I(anchor, candidate.Footprint);
            if (!CanPlace(area)) return false;
            _container.AddChild(candidate);
            if (!CanPlace(area) || !candidate.TryPlace(_grid, _registry, anchor) || !candidate.IsPlaced) return false;
            building = candidate; return true;
        }
        catch (Exception error) { GD.PushError("Building placement failed: " + error.Message); return false; }
        finally { if (building == null && GodotObject.IsInstanceValid(instance)) instance.Free(); }
    }
}
