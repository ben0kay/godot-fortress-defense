# Core

**Status:** BF1 Citadel is Source Implemented / Awaiting Owner Validation.

Citadel/CPU, headquarters and unique central structures. `Citadel/Citadel.tscn` inherits SHARED/Building.tscn and is the ST1 strategic objective. Its .tres definition supplies footprint/HP; geometric core visuals and shared health/occupancy/destruction are active.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
