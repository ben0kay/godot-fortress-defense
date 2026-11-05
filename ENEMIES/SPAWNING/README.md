# ENEMIES/SPAWNING — ST1 edge spawner

**Status: Source Implemented / Awaiting Owner Validation.** `EdgeEnemySpawner.cs` receives explicit scene/world/registry/request/container/CPU references. One authored definition is validated once; created enemies receive snapshots and their edge position before entering the tree.

Inspector: EnemyScene, SpawningEnabled, SpawnInterval (0.8s), MaxActiveEnemies (20), MaxSpawnAttempts (32), MinCitadelDistance (160px), Seed (1). At most one enemy is created per interval/frame; a hitch never causes catch-up spawning. Each interval tests a bounded sequence around a seeded perimeter start, checks shared walkability/radius clearance, occupied cells, CPU separation and existing spawn bodies. Clearance moves the edge band inward. Failed attempts wait for the next interval; a fully blocked edge cannot hang the scene.

Maximum active count is clamped to 0–200 as an input guard, not an ST2 stress preset or benchmark claim. Disabling spawning does not disable existing enemies. C clears and pauses; P toggles; CPU loss stops the spawner. R reloads the world. Waves, role mixtures and 10/50/100/200 presets remain planned.

## Future level-driven enemy pressure — planned, not implemented

The game will have **multiple levels with independently configured difficulty**, inspired by `ben0kay/VECTOR_TD_2026` (`scripts/scr_enemy_spawner/scr_enemy_spawner.gml` and `scripts/scr_world_data/scr_world_data.gml`). Preserve these four distinct sources of pressure:

1. **Baseline:** occasional individual/small trickle spawns; interval, weighted enemy pool and difficulty scaling per level.
2. **Clusters:** periodic localized groups (count range, composition, side/region, intra-group stagger); can coexist with baseline.
3. **Waves:** major scheduled attacks with warning time, multi-group composition, ordering/staggering and level-defined win/progression rules.
4. **Milestones:** exceptional one-shot or explicitly repeatable reinforcements triggered by level events/progress (e.g., kill count, elapsed time, wave completion). Track consumed triggers; don't fire the same milestone every update.

**Ownership:** `LEVELS/` should own future individual level/scenario data (enemy pools, allowed types, difficulty, event triggers, victory, scheduling); `ENEMIES/SPAWNING/` should own reusable spawn coordination, edge validation and the bounded execution queue. Do not put level-specific scripts/configuration in `DEBUG/` or duplicate four spawner engines. Keep `DEBUG/test.tscn` as a manual stress-test sandbox independent from campaign settings.

**Performance:** All four pressure sources enqueue spawn intents into **one bounded, fair per-frame executor** shared with debug hotkey spawns. Track both pending count and active cap; spread formation members over frames, never bypass the cap for a large event, and define deferred/expired/overflow behavior rather than dropping events silently. Use deterministic seeds for reproducible level tests. Pause, victory, CPU loss, scene unload and reset must cancel or retain queued requests intentionally. Spawn budgets and gameplay scheduling should be separate from path-request budgets. A wave warning is UI/presentation, not a requirement to instantiate the whole wave at once.

**Current scope:** `EdgeEnemySpawner.cs` only does gradual Enemy001 edge spawning. Do **not** implement level progression, four pressure modes, weighted enemy selection or milestone tracking during the initial navigation/tower combat stress pass; keep it extensible for this future level system.

## WG0 Level001 integration

`DefinitionOverride` optionally supplies level-local Enemy001 tuning while reusing the existing scene/runtime. Level001 uses 40px range for its 64px cells; the 32px debug world keeps its original stats. Spawner initialization rejects range below a legal cardinal approach distance. `SpawnCellFilter` optionally restricts attempts to edge cells in the generated world's initial CPU-connected region, then current walkability, radius and body-overlap checks still run. Sealing the CPU with later walls is allowed and does not rebuild generation connectivity per frame. These hooks add no new spawn mode or campaign progression; the four pressure modes above remain future work.

## Reusable debug spawn controls

`QueueDebugSpawns(count)` accepts bounded manual add requests; `PendingDebugSpawns`, `CancelDebugSpawns()` and `CanSpawn` expose the state to the reusable UI. Active+pending cannot exceed200, and accepted requests raise MaxActiveEnemies enough to fit. MaxDebugSpawnsPerFrame defaults2 (clamped1–8). One executor runs the same TrySpawnOne validity path for manual and automatic units; manual batches take precedence, while natural spawning keeps its existing interval/no-catch-up behavior afterward. Automatic spawning can be paused while manual requests continue. Invalid edge attempts remain pending, without bypassing collision or active caps; clear, CPU loss and scene exit cancel them. Full queues/caps sleep and registry changes wake the executor. The four future pressure modes above are still unimplemented.
