# BUILDINGS

**Status:** BF0 shared scene/runtime, definitions, health, selection, rectangular footprints, collision, placement and registry are Source Implemented / Awaiting Owner Validation. BF1 Citadel/Wall001 scenes are also Source Implemented / Awaiting Owner Validation. Construction and weapons remain planned.

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

**Current pass:** BF0 foundation plus BF1 Citadel/Wall001 are source implemented; see `SHARED/README.md`. Root `DEBUG/test.tscn` now hosts an ST1 enemy encounter with dynamic wall placement. Tower001/projectiles belong to ST3. See `../../NOTES/INITIAL_STRESS_NAVIGATION_PLAN.md`.
