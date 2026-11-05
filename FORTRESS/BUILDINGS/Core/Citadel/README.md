# Citadel / CPU

**Status: BF1 Source Implemented / Awaiting Owner Validation.** `Citadel.tscn` inherits `SHARED/Building.tscn`. `CitadelDefinition.tres` configures identity/category, 4×4 footprint and 4000 HP. `BuildingVectorVisual` is an optional child providing a recognizable CPU/core outline, health bar, selection and brief hit feedback. Root lifecycle/collision/occupancy are shared, never duplicated.

DEBUG centers the scene and gives its reference directly to the enemy spawner. Destruction releases all 16 cells, stops spawning and surviving enemies, and emits cosmetic feedback. No generation, resources, power or separate CPU controller. Restart R to reset the encounter. Enlarged footprints must fit the map's authored clearing.
