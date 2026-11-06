# Wall001

**Status: BF1 Source Implemented / Awaiting Owner Validation.** `Wall001.tscn` inherits `SHARED/Building.tscn`; `Wall001Definition.tres` sets the default 1×1 solid footprint and 120 HP. Optional vector plate artwork/health/selection uses `BuildingVectorVisual.cs`, without idle root processing.

Placement, collision, grid reservation, damage and destruction use the universal lifecycle. Every footprint edit changes navigation once; accessible walls can be temporary enemy breach targets when the CPU is unreachable. Removing/destroying a wall frees all stored cells. DEBUG left-click/drag places, right-click removes. Enemy-body/clearance overlap is rejected; sealing the CPU is allowed. BM0 adds a 10-credit price; DEBUG remains free. No construction animation.

COMBAT1 (**Source Implemented / Awaiting Owner Validation**): Wall001Definition.BlocksFriendlyTowerProjectiles=false lets friendly ground tower LOS/rounds cross the wall. This is a placed-definition snapshot, independent of its solid ground occupancy/collision and 64px fallback sight. Enemy002 rounds still collide with and damage it. Other building definitions default true; wall purchase, health, breach behavior and navigation remain unchanged.
