# OffensiveTowers

**Status:** Basic Level1 Tower001 is Source Implemented / Awaiting Owner Validation. See Tower001/README.md for controls, authored values and limitations.

Tower001 and future attack towers. Stationary base sprite plus optional independently rotating turret/weapon/animated overlays. Targeting/weapons are optional additions to the inherited building scene.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
