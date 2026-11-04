# Shields

**Status:** Organizational folder only; no building gameplay implemented.

Future static defense shields; shared damage/shield processing lives in COMBAT and optional fortress systems.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
