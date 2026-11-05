# BUILDINGS

**Status:** BF0 shared scene/runtime, definitions, health, selection, rectangular footprints, collision, placement and registry are Source Implemented / Awaiting Owner Validation. BF1 Citadel/Wall001 scenes are also Source Implemented / Awaiting Owner Validation. Basic Level1 Tower001/projectiles are now Source Implemented / Awaiting Owner Validation. Construction and advanced weapon/progression systems remain planned.

All actual structures inherit `SHARED/Building.tscn` (Godot inherited scenes). Concrete scenes are organized by **purpose**, not by shared gameplay system:

- `SHARED/` — universal building scene, Building.cs, definitions and per-world registry.
- `Core/` — Citadel/CPU and unique base structures.
- `Defense/` — offensive towers, repair towers, shields and walls.
- `Energy/` — power generators, solar, wind, relays/pylons and batteries.
- `Extraction/` — ore-specific mines.
- `Economy/` — credit-production buildings.
- `Industry/` — refining, fabrication and ammunition production.
- `Infrastructure/` — storage, cargo hubs and construction support.

Building category is independent of capabilities. A tower can consume power; a miner may produce physical inventory; neither justifies duplicating global resources or navigation.

**Visuals:** Each inherited building can add a non-rotating `BaseSprite`, optional independently rotating `RotatingParts` (with muzzle marker if needed) and optional baked `AnimatedParts`. `ConstructionVisuals` (staged sprites/drones) is reserved for later. Do not attach unused processing nodes to every building. Rectangular multi-cell footprints are authored in BuildingDefinition, not inferred from sprites.

**Current pass:** BF0 foundation plus BF1 Citadel/Wall001 are source implemented; see `SHARED/README.md`. Root `DEBUG/test.tscn` now hosts an ST1 enemy encounter with dynamic wall placement. Tower001/projectiles are source implemented for the owner-authorized basic ST3 combat slice. See `../../NOTES/INITIAL_STRESS_NAVIGATION_PLAN.md`.

## BM0 — catalogue and purchases

**Source Implemented / Awaiting Owner Validation.** `SHARED/BuildingCatalogue/DefaultBuildingCatalogue.tres` pairs Wall001 and Tower001 scenes with their shared definitions. Definitions expose stable ID, name, description, icon, category, footprint, Purchasable and typed Cost entries. Citadel defaults to non-purchasable and is excluded. Catalogue validation runs once at level setup, rejects duplicate IDs/non-purchasable entries and verifies that each inherited scene uses its paired definition; menu metadata never requires per-frame instantiation. Restart after authoring changes.

`BuildingPlacement.Catalogue` exposes cached entries; `IsAvailable` checks the session allowlist. `SetAllowedBuildingIds` publishes CatalogueChanged; an empty allowlist permits nothing, and unknown IDs fail setup clearly. Shared purchase authority is documented in [Resources](../SYSTEMS/Resources/README.md). No Ctrl build menu until BM1.
