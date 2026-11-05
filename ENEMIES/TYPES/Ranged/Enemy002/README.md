# EN2 — Enemy002, Building-Hunter Shooter

**Source Implemented / Awaiting Owner Validation.** Enemy002 combines VECTOR TD's Building Hunter strategic lock/local hysteresis with its Single Shooter's ranged concept. It uses the same Enemy root, health/death, registry, waypoint movement, shared ground grid and path queue as Enemy001. It never selects the player. References are read-only; no live asset dependencies, copied shields/rewards or additional enemy variants.

## Spawn and settings

Run Level001 or DEBUG, open **F2**, select **enemy002** and use Add10/50/100/200/custom. Select enemy001 and add another batch to mix them. Each queued batch keeps its selected type; changing the dropdown only affects new requests. Shared active+pending cap200, gradual default2/frame, edge/body/terrain checks and Clear remain intact. Automatic spawning still uses EnemyScene (Enemy001 by default); changing the manual selector does not create an automatic mixed pool. For an explicitly authored automatic Enemy002 setup, assign EnemyScene accordingly. Level DefinitionOverride applies only to the same stable EnemyId, so Level001's Enemy001 stats never overwrite Enemy002's definition.

Edit `Enemy002Definition.tres`; restart after changes:

| Group | Defaults |
| --- | --- |
| Identity/vitals | enemy002; 35HP; radius12px |
| Movement | 90px/s; turn response12 |
| Targeting | BuildingHunter; DetectionRadius400px |
| BuildingHunting | scan interval0.75–1.5s; SwitchImprovement0.15; MinimumSwitchInterval1s |
| Attack | damage6; range250px; cooldown1.5s |
| RangedAttack | projectile speed650px/s; lifetime2s; radius2px; tracer14px; aim tolerance12° |

The amber angular body, forward barrel and orange-red rounds distinguish it from Enemy001. Hit/shot flashes and HP arcs use the existing event-driven visual. The muzzle is the barrel-tip world position. No complex effects, detection Area2D, pooling, armor, heat, ammunition, power, loot/rewards or upgrades.

## Target choice and recovery

Initial acquisition requests a **scheduled global nearest-building search** regardless of detection range. All living registered buildings, including the CPU, are valid. Distances use the nearest point on the full rectangle, not centers. The reference remains locked while travelling, even when outside local detection range.

Routine scans query nearby building buckets, then exact footprint distance. A replacement must be more than15% closer and at least1s after the last strategic switch. Equal/similar distances retain the old target. No changed target means no new route request. New construction is discovered on the next scheduled local scan, not an immediate global broadcast.

Destroyed/removed goals immediately clear cached movement/attack references and coalesce a priority recovery request. Recovery first searches locally; if none is found, a separate bounded global job follows. This bypasses normal switch cooldowns. No remaining buildings means dormant waiting until BuildingRegistry.Added; only sleepers wake on additions. Enemy002 continues hunting remaining structures after CPU destruction, while Enemy001 and the normal Enemy001 automatic spawner retain their CPU-loss behavior.

## Shared scheduler/index/navigation

`EnemyTargetRoster` handles both existing optional-player decisions and hunters. Defaults: **MaxDecisionsPerFrame4**, **MaxGlobalSearchesPerFrame1**. At most two consecutive priority decisions precede a due routine decision, preventing permanent starvation. Requests coalesce to one per hunter; lifecycle cleanup removes them. Routine intervals use deterministic per-instance/scan phases; actual latency can lengthen under queue pressure. No-player/no-hunter sessions leave decision processing dormant.

`BuildingRegistry` remains authoritative. Its stationary `BuildingSpatialIndex` covers every256px bucket touched by each complete footprint. Placement adds, removal deletes; deduplicated queries reuse scratch storage. Buildings spanning buckets appear once. Local scans perform exact circular-distance filtering after broadphase. Only scheduled initial/recovery jobs enumerate all registered buildings; no scene-tree/group searches. Placed buildings must stay stationary/unscaled as the existing building contract requires.

`EnemyPathRequests` still uses the same512 cell-visits/frame, one A*/frame and1.5ms soft budget. Cooperative reachability finds a walkable firing cell within range and with a projectile-radius-clear sight line; it does not assume adjacent/straight-line positions are reachable. Static-target failures sleep until topology changes or target replacement. Existing temporary building-breach fallback remains a heuristic and does not replace the strategic lock. Ranged execution can stop sooner at a valid firing point, cancelling an unnecessary queued route. Walls placed during travel are still checked by every movement sweep. Obstructed firing queues a bounded reposition; no identical failure retry every frame.

`EnemyRangedAttack` owns cooldown/aim/launch only. Shared `CombatProjectile` supports hostile straight rounds; WORLD reports the earliest solid footprint/terrain/boundary. A round damages the first living building hit once, including an intervening newly placed wall, and expires by impact or lifetime. Shots hold no target/source enemy reference, do not search, and survive source/target destruction independently. They do not hit players or other enemies in EN2. Tower bullets retain existing enemy-hit behavior.

F2 optionally shows lightweight target-job/global/local scan counts while expanded. Enemy exposes AcquisitionState, StrategicTarget and LocalTargetScans for debugging; no target-line overlays run by default. These counters are not a measured FPS guarantee.

## Brief owner acceptance checks

1. With only the CPU, spawn Enemy002 and inspect global lock, approach and ranged damage.
2. Build a substantially closer wall/tower within400px of a travelling shooter; inspect the next local switch. A distant lock must persist when the local query is empty.
3. Destroy/remove its selected target; inspect local recovery, then global fallback. Remove all buildings to observe sleeping, then place a new structure to wake hunters.
4. Place similarly distant buildings; check the15%/1s hysteresis prevents oscillation.
5. Block firing/path access with terrain/walls; inspect safe reposition/breach and dormant unreachable failures. Add a wall in flight and check first-solid projectile impact.
6. Mix Enemy001/Enemy002, with player disabled and enabled. Only Enemy001's permitted player policy should chase the chassis. Test CPU loss, Clear, reset and regeneration.
7. Add100–200 units and inspect FPS, target-job latency and route stability in-editor. No large-scale performance result is claimed.

Validation here is source/diff and scene/resource review only when Godot/.NET is unavailable. Visual, gameplay and large-population acceptance belong to the owner. Remaining limitations: enemies overlap; queued replacements can pause movement; breach selection is heuristic; very dense targets can increase broadphase work; no optimized interception/steering or advanced ST2 path priorities were added.
