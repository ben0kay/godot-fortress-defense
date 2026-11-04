# Extraction

**Status:** Organizational folder only; no building gameplay implemented.

Ore-specific miners (copper, iron, silicon, etc.). World deposits belong in WORLD; production/inventory and cargo flow live in shared fortress systems.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
