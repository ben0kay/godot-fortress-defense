# SHARED — universal building foundation

**BF0 + BF1 status: Source Implemented / Awaiting Owner Validation.**

Every concrete building must inherit `Building.tscn`. It now has a passive `Building.cs` root, `Visuals` attachment, `FootprintBody/CollisionShape2D` and `SelectionAnchor`. No default processing, weapons, electricity, inventory or construction animation is imposed.

- `BuildingDefinition.cs`: Inspector resource with stable ID, display name, category, rectangular footprint and maximum structural health. These values are snapshotted per instance on spawn; live Resource edits do not change reservations or refill health.
- `Building.cs`: placement identity, occupied rectangle, health, selection/change events, destruction/removal and idempotent ExitTree cleanup. `ApplyDamage` is scalar structural damage for BF0, not a final shield/resistance framework.
- `BuildingRegistry.cs`: scene-owned live placed-building set with Added/Removed notifications; no global singleton or scene scans. Do not mutate the set while enumerating it.
- `../../SYSTEMS/BuildingPlacement/BuildingPlacement.cs`: explicit service instantiating an inherited scene, initializing its definition before AddChild, validating/claiming occupancy, sizing collision and registering it. Invalid placements free the instance and leave no reservation.

The grid anchor is the top-left occupied cell. Building root is centered on its complete rectangle; visuals never determine footprint. Place through the service, not raw AddChild; the bare base scene is not itself a gameplay structure. Roots/containers must remain unscaled/unrotated, and placed roots must not be manually moved. Quarter-turn placement is deferred.

Visuals may add `BaseSprite`, independent `RotatingParts`, baked `AnimatedParts` and future `ConstructionVisuals`. Only a concrete building that needs a capability should add it. DEBUG's inherited `Foundation/TestBuilding.tscn` demonstrates 2×3 health, selection and placement with event-drawn prototype art. BF1 now also has inherited `Core/Citadel/Citadel.tscn` (4×4 / 4000 HP) and `Defense/Walls/Wall001/Wall001.tscn` (1×1 / 120 HP). Tower001 remains ST3.

Destroy/Remove releases every reserved cell and registry membership immediately; ExitTree also handles direct deletion. Selection is cleared, static collision is disabled safely/deferred, and scene deletion is queued. The surviving grid terrain is not erased. Registry references and subscriptions are scoped to one world. `Placed`/`Detached` events and the snapshotted `FootprintPixels` let optional `BuildingVectorVisual.cs` draw stable art and temporary hit/destruction feedback without root processing. `BuildingPlacement.AdditionalValidation` integrates enemy-overlap checks; grid remains authoritative.

Manual acceptance: select every cell of the prototype; reject overlapping/out-of-map placements; damage four times at 25 HP; inspect collision matching the rectangle; confirm destruction/removal releases all six cells with one navigation revision. See [sandbox controls](../../../DEBUG/README.md).
