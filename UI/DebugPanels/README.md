# Reusable debug overlay

**Status: Source Implemented / Awaiting Owner Validation.** Instance `Debug.tscn` under a plain `RootDebug` node in any scene. Edit this one packed scene/script to update all linked instances; avoid making instances local or overriding their internals unnecessarily.

- Always-visible top-right readout: actual `Engine.GetFramesPerSecond()`, live `EnemyRegistry.Count` and pending debug spawns. Refreshed four times/second, with no scene-wide scans or per-frame text rebuilds.
- **F2** or the button expands/collapses spawn controls. Inspector: ToggleKey and StartExpanded.
- **Add 10 / 50 / 100 / 200**, or custom amount: add that many units, not a target total. Active + pending is capped at 200. The spawner raises its active cap enough to admit the request, then creates at most 2 debug enemies/frame by default using the same valid-edge/clearance/body checks as normal spawning. Accepted amount is reported.
- Automatic spawning checkbox controls the existing interval-driven spawner independently. Manual requests still work while automatic spawning is off. When a manual batch is pending, it is processed before natural spawning; after it drains, automatic spawning resumes if enabled. The raised active cap remains until changed/restarted.
- Clear cancels pending work, clears enemies and pauses automatic spawning. C is reserved for optional player-follow camera toggling; use this Clear button. CPU loss and scene exit cancel pending work. A blocked edge leaves accepted requests pending; clear cancels them.

In LevelController scenes (including Level001 and DEBUG), the overlay finds its nearest level ancestor once and binds after the level's Ready initialization. No per-frame discovery or global singleton. For another kind of scene, assign the **Registry** and **Spawner** node references in the overlay Inspector. Spawner must already be initialized by that scene. Without bindings, FPS still displays, enemies show N/A and unavailable controls are disabled. Multiple separate scenes can reuse this same packed scene without sharing counts or queues.

UI lives here; gameplay spawning stays in `ENEMIES/SPAWNING/EdgeEnemySpawner.cs`. This adds basic debug stats/manual spawn controls, not advanced ST2 AI priorities, CPU profiling or benchmark claims. F1 retains Level001 world-generation details; F2 is dedicated to reusable debug controls.

Owner check: run both Level001 and DEBUG, open F2, turn automatic spawning off, Add10 and verify live/pending counts, then add a larger batch and clear while it is pending. Try CPU loss/restart and confirm no old requests survive. Instance Debug.tscn in a plain scene to check FPS-only mode, then assign initialized services. Build/runtime were not run here (Godot/.NET unavailable); source/scene checks only.
