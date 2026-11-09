# Citadel / CPU

**Status: BF1 Source Implemented / Awaiting Owner Validation.** `Citadel.tscn` inherits `SHARED/Building.tscn`. `CitadelDefinition.tres` configures identity/category, 4×4 footprint and 4000 HP. `BuildingVectorVisual` is an optional child providing a recognizable CPU/core outline, health bar, selection and brief hit feedback. Root lifecycle/collision/occupancy are shared, never duplicated.

DEBUG centers the scene and gives its reference directly to the enemy spawner. Destruction releases all 16 cells, stops spawning and surviving enemies, and emits cosmetic feedback. No generation, resources, power or separate CPU controller. Restart R to reset the encounter. Enlarged footprints must fit the map's authored clearing.

WG0 Level001 places this same inherited scene in a protected central clearing; its shared placement/health remain unchanged. The player marker is a separate reserved traversable ground cell, not part of the CPU's blocked footprint. CPU art/collision size follows the level's grid cell size automatically.

PWR0 binds the placed Citadel as the initial relay. Tune **Citadel Relay Radius** on `RootFortress/PowerNetwork` (default 400 world pixels). Destruction removes its membership. Electrical generation starts in PWR1. [Power Network](../../../SYSTEMS/PowerNetwork/README.md).
