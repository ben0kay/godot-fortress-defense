# Walls

**Status:** BF1 Wall001 is Source Implemented / Awaiting Owner Validation. Towers and other defenses remain planned.

Placeable solid walls that update WORLD grid occupancy and invalidate relevant enemy routes. Wall001 is the first prototype.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
