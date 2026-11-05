# Defense

**Status:** BF1 Wall001 is Source Implemented / Awaiting Owner Validation. Basic Tower001 is also Source Implemented / Awaiting Owner Validation; other defenses remain planned.

Offensive and repair towers, static shields, walls and other static defenses; all inherit SHARED/Building.tscn.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
