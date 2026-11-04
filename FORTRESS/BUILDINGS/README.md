# BUILDINGS

**Status:** Category folders and the shared scene shell are scaffolded; health, footprints, construction, placement, collision, weapons and registration are **not implemented**.

All actual structures inherit `SHARED/Building.tscn` (Godot inherited scenes). Concrete scenes are organized by **purpose**, not by shared gameplay system:

- `SHARED/` — universal building scene and future Building.cs, definitions and registry.
- `Core/` — Citadel/CPU and unique base structures.
- `Defense/` — offensive towers, repair towers, shields and walls.
- `Energy/` — power generators, solar, wind, relays/pylons and batteries.
- `Extraction/` — ore-specific mines.
- `Economy/` — credit-production buildings.
- `Industry/` — refining, fabrication and ammunition production.
- `Infrastructure/` — storage, cargo hubs and construction support.

Building category is independent of capabilities. A tower can consume power; a miner may produce physical inventory; neither justifies duplicating global resources or navigation.

**Visuals:** Each inherited building can add a non-rotating `BaseSprite`, optional independently rotating `RotatingParts` (with muzzle marker if needed) and optional baked `AnimatedParts`. `ConstructionVisuals` (staged sprites/drones) is reserved for later. Do not attach unused processing nodes to every building. Rectangular multi-cell footprints must be authored as data when BF0 is implemented, not inferred from sprites.

**Next code passes:** BF0 creates the shared Building.cs/definition/registry, footprint/collision and lifecycle. BF1 creates inherited Citadel, Tower001 and Wall001 scenes. Root `DEBUG/test.tscn` hosts the future stress map. See `../../NOTES/INITIAL_STRESS_NAVIGATION_PLAN.md`.
