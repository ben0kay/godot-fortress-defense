# Infrastructure

**Status:** Organizational folder only; no building gameplay implemented.

Physical inventory storage, cargo hubs, delivery and construction support. Shared logistics belong in FORTRESS/SYSTEMS/Logistics.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
