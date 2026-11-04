# Tower001

**Status:** Organizational folder only; no building gameplay implemented.

Planned first stress-test tower. Later Tower001.tscn will inherit SHARED/Building.tscn; put its authored sprite assets and type-specific weapon logic here.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
