# Core

**Status:** Organizational folder only; no building gameplay implemented.

Citadel/CPU, headquarters and unique central structures. First scene (BF1): Citadel.tscn inherits SHARED/Building.tscn.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
