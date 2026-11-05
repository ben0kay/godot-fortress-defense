# Wall001

**Status: BF1 Source Implemented / Awaiting Owner Validation.** `Wall001.tscn` inherits `SHARED/Building.tscn`; `Wall001Definition.tres` sets the default 1×1 solid footprint and 120 HP. Optional vector plate artwork/health/selection uses `BuildingVectorVisual.cs`, without idle root processing.

Placement, collision, grid reservation, damage and destruction use the universal lifecycle. Every footprint edit changes navigation once; accessible walls can be temporary enemy breach targets when the CPU is unreachable. Removing/destroying a wall frees all stored cells. DEBUG left-click/drag places, right-click removes. Enemy-body/clearance overlap is rejected; sealing the CPU is allowed. No costs or construction animation.
