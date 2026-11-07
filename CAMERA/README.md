# CAM0 — optional follow / independent free camera

**Source Implemented / Awaiting Owner Validation.** `BattlefieldCamera.tscn` is a reusable Camera2D, instanced at RootCamera/Camera2D in shared LevelBase and DEBUG. Initialize(bounds, optionalTarget) supplies world-space bounds and an optional ICombatTarget; BindTarget replaces/removes it explicitly. No player discovery, mandatory player or global autoload. Cursor-centred wheel zoom, immediate drag and viewport-aware clamping are adapted locally from godot-tower-defense's CAMERA/CameraController, retaining this game's optional actor model.

- Default **Free Roam**, with or without a player. Level001 starts zoomed in.
- **C** toggles Player Follow only when a living player exists. Without one, it safely reports that Free Roam remains active. C never clears enemies.
- **Arrow keys** or **MMB drag** pan in Free Roam; wheel zooms. Follow mode uses configurable smoothing/offset and allows zooming. Manual C return restores the previous free position/zoom. Losing/deleting the followed actor automatically restores that free view.
- **Home** returns to Free Roam/full-map framing, convenient for the 200-enemy benchmark. Resize reframes only while in full-map mode; manual views retain their zoom and are clamped.
- Ctrl build browsing and GUI-hovered mouse input block camera navigation. F1/F2 remain unchanged. DEBUG retains B + MMB terrain editing; leave the grid overlay off to pan there. Arrow panning pauses while the build menu is open or a GUI control has keyboard focus. In shared levels, arrows drive the player in Follow mode.

Camera Inspector: StartFullMap, StartingZoom(.5), MinimumZoom(.05), MaximumZoom(2), ZoomStep(1.15), FollowZoom(.8), FramePadding(64,120), PanButton(Middle), PanSensitivity(1), KeyboardPanSpeed(900 viewport px/s), FollowSmoothing(10;0=instant), FollowOffset, KeepInsideWorld, ToggleKey(C), FullMapKey(Home). C is the project default reserved toggle; don't reuse it for developer enemy clearing. F2's existing Clear button cancels pending spawns, clears enemies and pauses automatic spawning.

Free camera processes held arrows only while keyboard panning is active and otherwise updates on mouse input/resize; target death/removal is event-driven. Viewport-sized bounds center an axis when zoomed wider than the map, avoiding invalid clamp limits. No camera action changes gameplay simulation, navigation or future fog visibility.

Owner check: no-player C, wheel/pan/Home, then enable player, C follow/move/zoom/C free, remove or kill while followed, and reset/regenerate. Confirm F1/F2/Ctrl remain usable. Runtime/visual validation remains with the owner; no FPS claim.
