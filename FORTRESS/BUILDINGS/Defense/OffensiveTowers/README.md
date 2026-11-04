# OffensiveTowers

**Status:** Organizational folder only; no building gameplay implemented.

Tower001 and future attack towers. Stationary base sprite plus optional independently rotating turret/weapon/animated overlays. Targeting/weapons are optional additions to the inherited building scene.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
