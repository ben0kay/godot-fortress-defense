# Wall001

**Status:** Organizational folder only; no building gameplay implemented.

Planned first test wall. Later Wall001.tscn inherits SHARED/Building.tscn; even 1x1 walls use the same multi-cell footprint contract.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
