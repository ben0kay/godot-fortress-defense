# Energy

**Status:** Organizational folder only; no building gameplay implemented.

Generators, solar panels, wind turbines, batteries and pylons/relays. Network calculation belongs in FORTRESS/SYSTEMS/PowerNetwork.

Every concrete placeable building scene here must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn`. Building category is not the same as its optional capabilities. Do not create duplicated placement, health, grid or resource systems per type.
