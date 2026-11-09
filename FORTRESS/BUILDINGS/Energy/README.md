# Energy buildings and Adaptive Power Mesh

**Status: PWR0 Source Implemented / Awaiting Owner Validation.** Operational power gameplay starts in PWR1.

Fortress Defense uses a **range-based relay backbone**. The existing Citadel is the initial relay node; future placeable metallic Power Relays bridge independent networks and allow automatic merge/split when built or destroyed. Producers, batteries and consumers attach as leaves to their nearest in-range relay, never extending the backbone themselves. Each connected network will later share finite generation and storage, while individual buildings keep private capacitors.

**PWR0 setup and checks:** [Power Network](../../SYSTEMS/PowerNetwork/README.md).
**Full staged roadmap:** [Adaptive Power Mesh](../../SYSTEMS/PowerNetwork/ADAPTIVE_POWER_MESH_ROADMAP.md).

Power topology/accounting belongs in `FORTRESS/SYSTEMS/PowerNetwork/`; actual generators, relays and batteries belong in this `FORTRESS/BUILDINGS/Energy/` folder. Each concrete placeable building must inherit `FORTRESS/BUILDINGS/SHARED/Building.tscn` and use the existing placement, health, credits and C1 construction mechanics. Building category is independent of opt-in power capability.

PWR0 implements graph membership only; PWR1 adds Citadel output and Tower001 power consumption, PWR2 adds a placeable relay, PWR3 dedicated generators/batteries, PWR4 network UI and PWR5 advanced disruption/AI. **None of these plans authorizes later passes automatically.**
