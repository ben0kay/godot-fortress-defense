# WORLD/Generation — WG0

**Status: Source Implemented / Awaiting Owner Validation.** Reusable algorithms operate on plain bounded cell arrays, then commit one accepted result to WORLD/Grid. No generation loop runs during gameplay.

- `WorldGenerationConfig.cs`: Inspector seed, coverage, cluster budgets/shape, cleanup, clearing, protected edge band and validation limits.
- `TerrainClusterGenerator.cs`: multiple connected directional frontiers, local seeded RNG, smooth 5-cell lattice-value noise, elongation/branching variation, simultaneous cardinal smoothing and component-size filtering. No independent random dead-cell scatter.
- `SpawnAreaValidator.cs`: verifies protected cells and nearby buildable ground, validates marker, and floods from legal cardinal CPU approaches while treating the full CPU footprint as solid. Cardinal connectivity matches corner-safe diagonal A*. Requires enough reachable edge cells on **all four sides**, respecting clearance.
- `WorldGenerator.cs`: constructs protected CPU/edge masks before growth, derives a deterministic candidate seed per attempt, retries a bounded number, then optionally carves/validates cardinal connections once. Result retains original/derived seed, attempts/rejections, repair status, CPU/player cells, actual coverage and initial reachable cells.

Validation and graph traversal are startup work over 3969 cells by default; no native A* is called for every generation cell or every edge. Initial accepted terrain is copied once into `WorldGrid.TryApplyTerrain` before the shared AStar view is initialized. Buildings/enemies are created afterward. The spawner uses initial connectivity as a candidate filter, but checks current solids/body overlap at actual spawn time. Dynamic wall sealing/breaching remains normal gameplay, not a reason to regenerate.

MaxGenerationAttempts bounds random candidates (default 6), with at most one further repaired-candidate validation. If impossible authored settings survive input checks and still fail repair, generation throws a clear error. Coverage/count/size are approximate after protected masks, merges, smoothing, fragment cleanup and repair; there is no promise of exact 22% or disjoint clusters. Very large size/count settings can make synchronous startup more expensive. Determinism is scoped to the same generator/settings/runtime version, not a future cross-version save format.

References reviewed: Vector TD `scr_world`/`scr_world_data` (blueprint-before-realization, bounded connected growth, safe radius, border band and independent RNG); Godot Tower Defense `WORLD/GRID/WorldGrid.cs`, navigation README and enemy performance notes (terrain/occupancy separation, batch notifications and avoiding optional expensive per-route refinement). We do not port individual dead-cell object instances, per-cell navigation rebuilds, fixed portal placement restrictions or the future four pressure modes.

Level integration/settings and owner checks: `../../LEVELS/Level001/README.md`.
