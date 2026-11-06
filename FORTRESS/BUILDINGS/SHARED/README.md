# SHARED — universal building foundation

**BF0 + BF1 status: Source Implemented / Awaiting Owner Validation.**

Every concrete building must inherit `Building.tscn`. It now has a passive `Building.cs` root, `Visuals` attachment, `FootprintBody/CollisionShape2D` and `SelectionAnchor`. No default processing, weapons, electricity, inventory or construction animation is imposed.

- `BuildingDefinition.cs`: Inspector resource with stable ID, display name, category, rectangular footprint and maximum structural health. These values are snapshotted per instance on spawn; live Resource edits do not change reservations or refill health.
- `Building.cs`: placement identity, occupied rectangle, health, selection/change events, destruction/removal and idempotent ExitTree cleanup. `ApplyDamage` is scalar structural damage for BF0, not a final shield/resistance framework.
- `BuildingRegistry.cs`: scene-owned live placed-building set with Added/Removed notifications; no global singleton or scene scans. Do not mutate the set while enumerating it.
- `../../SYSTEMS/BuildingPlacement/BuildingPlacement.cs`: explicit service instantiating an inherited scene, initializing its definition before AddChild, validating/claiming occupancy, sizing collision and registering it. Invalid placements free the instance and leave no reservation.

The grid anchor is the top-left occupied cell. Building root is centered on its complete rectangle; visuals never determine footprint. Place through the service, not raw AddChild; the bare base scene is not itself a gameplay structure. Roots/containers must remain unscaled/unrotated, and placed roots must not be manually moved. Quarter-turn placement is deferred.

Visuals may add `BaseSprite`, independent `RotatingParts`, baked `AnimatedParts` and future `ConstructionVisuals`. Only a concrete building that needs a capability should add it. DEBUG's inherited `Foundation/TestBuilding.tscn` demonstrates 2×3 health, selection and placement with event-drawn prototype art. BF1 now also has inherited `Core/Citadel/Citadel.tscn` (4×4 / 4000 HP) and `Defense/Walls/Wall001/Wall001.tscn` (1×1 / 120 HP). Tower001 now also inherits the shared scene, with a Building-derived script and opt-in Level1 combat capability; advanced systems remain deferred.

Destroy/Remove releases every reserved cell and registry membership immediately; ExitTree also handles direct deletion. Selection is cleared, static collision is disabled safely/deferred, and scene deletion is queued. The surviving grid terrain is not erased. Registry references and subscriptions are scoped to one world. `Placed`/`Detached` events and the snapshotted `FootprintPixels` let optional `BuildingVectorVisual.cs` draw stable art and temporary hit/destruction feedback without root processing. `BuildingPlacement.AdditionalValidation` integrates enemy-overlap checks; grid remains authoritative.

Manual acceptance: select every cell of the prototype; reject overlapping/out-of-map placements; damage four times at 25 HP; inspect collision matching the rectangle; confirm destruction/removal releases all six cells with one navigation revision. See [sandbox controls](../../../DEBUG/README.md).

Tower001 adds its own BaseSprite, independent TurretPivot, muzzle and optional simple flash under Visuals. BuildingPlacement.Placed publishes after a successful claim so scene composition can bind IBuildingCombat without adding a weapon loop/dependency to passive Building. Tower001's base._Ready/base._ExitTree calls preserve canonical lifecycle and cleanup.

## BM0 — catalogue and purchases

**Source Implemented / Awaiting Owner Validation.** `BuildingCatalogue/DefaultBuildingCatalogue.tres` pairs Wall001 and Tower001 scenes with their shared definitions. Definitions expose stable ID, name, description, icon, category, footprint, Purchasable and typed Cost entries. Citadel defaults to non-purchasable and is excluded. Catalogue validation runs once at level setup, rejects duplicate IDs/non-purchasable entries and verifies that each inherited scene uses its paired definition; menu metadata never requires per-frame instantiation. Restart after authoring changes.

`BuildingPlacement.Catalogue` exposes cached entries; `IsAvailable` checks the session allowlist. `SetAllowedBuildingIds` publishes CatalogueChanged; an empty allowlist permits nothing, and unknown IDs fail setup clearly. Shared purchase authority is documented in [Resources](../../SYSTEMS/Resources/README.md). BM1 now provides the shared root UI/BuildMenu, consuming this cached metadata and session availability.

EN2: BuildingRegistry owns a BuildingSpatialIndex with256px buckets covering WorldFootprint (the placed rectangle). Add/remove lifecycle maintains the cache; deduplicated nearby queries reuse scratch storage and exact distance filtering lives in BuildingHunterTargeting. The live HashSet remains authoritative. No building processing loop, competing occupancy or movable-building support is introduced.

AIR2: `Combat/ProjectileTower.cs` + ProjectileTowerConfig.cs own the reusable opt-in stationary-base/rotating-gun component. Tower001 retains compatibility scripts/resources at stable paths; Tower002 inherits Building.tscn directly with the shared component and its own AA resources. DefaultBuildingCatalogue now includes tower002; no placement/health/navigation owner changes.

## FW1 building vision provider

**Source Implemented / Awaiting Owner Validation.** Every friendly placed building registers one stationary source through `WORLD/FOG/BuildingVisionSources`, using registry Added/Removed and existing TargetLost/TreeExiting cleanup. Citadel keeps its dedicated master radius and is not registered twice. `Building.VisionRadius` is a nullable virtual provider: null uses FogMasterConfig.DefaultBuildingVisionRadius (64px); concrete providers may declare sight without implementing combat. `VisionRadiusOverride` is Inspector-editable (-1 automatic, >=0 explicit), with changes publishing VisionChanged. Custom providers call NotifyVisionChanged after range changes or a future grid-safe relocation. Current placed roots still must not be moved manually. Runtime combat-config edits do not modify existing weapon snapshots. See [fog configuration/lifecycle/owner checks](../../../WORLD/FOG/README.md).

Catalogue authoring: all included definitions must retain Purchasable=true and their intended Cost, Icon and Description when saved in the editor. Wall001 uses a 10-credit price; Citadel stays non-purchasable and outside the catalogue. Setup exceptions distinguish non-purchasable definitions from duplicate IDs and name the ID/resource path. Wall001 purchase/menu metadata was restored after a saved-resource regression blocked Level001 startup; runtime confirmation remains an owner check.

## COMBAT1 projectile obstruction

**Source Implemented / Awaiting Owner Validation.** BuildingDefinition → BlocksFriendlyTowerProjectiles defaults true; Building snapshots it at initialization. Wall001 authors false so friendly ground tower rounds/LOS pass over it. Other towers, CPU and all other existing definitions keep true. Future low barriers can author false without implementing combat or a type-name filter. This never changes physical collision, grid occupancy, navigation, vision or enemy projectile obstruction. Reload/place new instances after authoring changes. Shared filtered delivery is documented in [COMBAT](../../../COMBAT/README.md#combat1--friendly-tower-rounds-over-low-barriers).
