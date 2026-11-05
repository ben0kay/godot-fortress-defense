# LEVELS — WG0 world foundation

**Status: WG0 Source Implemented / Awaiting Owner Validation.**

`SHARED/LevelBase.tscn` composes existing WORLD, FORTRESS and ENEMIES services. `LevelController.cs` is the shared setup/input owner for both proper levels and the fixed DEBUG sandbox; it does not own enemy AI, pathfinding or building lifecycle. `LevelConfig.cs` carries per-level grid, generation and initial Enemy001 settings. Future campaign objectives/difficulty/pressure belong here, but WG0 does not implement them.

Start with [Level001](Level001/README.md): a finite 4032×4032-pixel seeded world. Open `LEVELS/Level001/Level001.tscn` and run F6. Project F5 continues to launch the separate `DEBUG/test.tscn` stress sandbox. No root launch-path change was made.

The shared base now groups nodes under RootWorld, RootFortress, RootEnemies, RootCamera, RootUI and RootDebug. Level001 inherits `RootDebug/Debug`, a linked instance of `UI/DebugPanels/Debug.tscn`; editing that packed scene updates every level using it. F2 opens its manual spawn controls; FPS/counts stay visible. Other scenes can instance it and assign registry/spawner references.

RootCombat/Projectiles now hosts shared basic projectiles. LevelController binds optional IBuildingCombat after placement and supports T(Tower001)/W(walls) in both Level001 and the fixed sandbox. No campaign progression or tower upgrades are implied.
