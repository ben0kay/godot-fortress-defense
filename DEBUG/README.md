# DEBUG — development sandbox and instrumentation

**Status:** `test.tscn` is an empty Godot 2D test-world scene shell. It is not yet a generated world or a functional battle.

**test.tscn** is the main developer-controlled battlefield/stress-map scene for adding the Citadel, wall placement, tower, Enemy001, edge spawner, procedural obstacles and profiling tools during ST0–ST5. It must remain separate from campaign/production level content in `LEVELS/`.

Later DEBUG logic can collect truthful FPS, frame timings, path queue, enemy counts and performance metrics; visual debug panels still belong under root `UI/DebugPanels/`. Avoid forcing developer tools to run in release gameplay.

**Planned controls:** F1 metrics and manual 10/50/100/200-enemy presets (not implemented). See `../NOTES/INITIAL_STRESS_NAVIGATION_PLAN.md`.
