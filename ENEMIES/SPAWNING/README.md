# ENEMIES/SPAWNING — ST1 edge spawner

**Status: Source Implemented / Awaiting Owner Validation.** `EdgeEnemySpawner.cs` receives explicit scene/world/registry/request/container/CPU references. One authored definition is validated once; created enemies receive snapshots and their edge position before entering the tree.

Inspector: EnemyScene, SpawningEnabled, SpawnInterval (0.8s), MaxActiveEnemies (20), MaxSpawnAttempts (32), MinCitadelDistance (160px), Seed (1). At most one enemy is created per interval/frame; a hitch never causes catch-up spawning. Each interval tests a bounded sequence around a seeded perimeter start, checks shared walkability/radius clearance, occupied cells, CPU separation and existing spawn bodies. Clearance moves the edge band inward. Failed attempts wait for the next interval; a fully blocked edge cannot hang the scene.

Maximum active count is clamped to 0–200 as an input guard, not an ST2 stress preset or benchmark claim. Disabling spawning does not disable existing enemies. C clears and pauses; P toggles; CPU loss stops the spawner. R reloads the world. Waves, role mixtures and 10/50/100/200 presets remain planned.
