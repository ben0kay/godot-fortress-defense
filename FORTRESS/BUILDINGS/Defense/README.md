# Defense

**Status:** Organizational folder only; no building gameplay implemented.

Offensive and repair towers, static shields, walls and other static defenses; all inherit SHARED/Building.tscn.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
