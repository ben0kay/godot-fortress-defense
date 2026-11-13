# BASEMENT1 — Temporary Basement Floor Planner

## Purpose
This is a **standalone, disposable side project** inside the Fortress Defense Godot 4 C# repository. It is NOT Fortress gameplay, a level editor plugin, or a feature to integrate into the game. Keep every new scene, script, asset and documentation file inside `BASEMENT/` (user calls it basement). Do not modify existing Fortress scenes, project startup settings, shared gameplay systems, or input mappings. The entire tool must be removable by deleting this folder.

## Launch
Create `BASEMENT/BasementPlanner.tscn` as an independent runnable scene. In the Godot editor, open that scene and press **F6 (Run Current Scene)**. It opens a top-down room with draggable labelled furniture. Do not override F6 in gameplay or set this as the project's main scene.

## Geometry
Use a consistent tile grid (e.g. 64 px/tile) with visible subtle gridlines. Main room: **11 tiles wide × 13 tiles long**. Attached room: **4 × 5 tiles**, adjoining the **top-right corner** and extending outside the main room. The two rooms form one accessible floor area; provide an opening along their shared wall (position can be adjusted later). Clearly render perimeter walls. Treat geometry as tile units, including fractions. Avoid assumptions about real-world dimensions.

## Items (all initially available, none omitted)
| Object | Width × Length in tiles | Count |
| --- | --- | --- |
| Fridge | 2 × 2 | 1 |
| Sofa | 5 × 2 | 1 |
| TV shelf / stand | 1 × 4 | 1 |
| Sports apparatus | 3.5 × 2.5 | 1 |
| Small shelf | 2.25 × 1 | 1 |
| Long kitchen countertop | 1.5 × 7 | 1 |
| Short kitchen countertop | 1.5 × 2 | 1 |
| Door | 2.5 wide, wall-mounted | 3 |

Represent each furniture item as a simple colored rectangle with its name centered. Doors are distinct wall openings, not floor furniture. Put initially unplaced items in a simple side tray to avoid invalid default overlaps. All objects can be moved into either room.

## Interaction
- Left-click/drag a furniture rectangle to move; release to place.
- Snap furniture to **quarter-tile (0.25)** increments, supporting every listed dimension.
- Press **R** to rotate selected or dragged furniture 90 degrees; validate the rotated footprint.
- While dragging, show a translucent **green valid / red invalid** preview.
- On invalid release, restore the last valid position; an unplaced item returns to the tray.
- No furniture overlap with other furniture, exterior walls or outside floor area. Validate entire axis-aligned footprint, not merely its center. The two-room union determines allowed floor area.
- Drag each of the three doors independently along a wall; doors snap to **half-tile (0.5)** increments and align automatically with horizontal/vertical wall segments. Reject overlapping doors, door openings extending past a segment, and invalid wall placements. Door cutouts should visually interrupt wall rendering. Consider the connecting opening in wall geometry.
- Pan and zoom the top-down camera; allow a grid visibility toggle. Show selected item's dimensions.
- Keep labels legible and basic controls discoverable.

## Persistence
Save/load layout JSON within `user://` under a basement-specific filename, including object positions, orientations and door locations. A Reset button restores initial unplaced items. No need for sophisticated editor UI, sprites, physics bodies or simulation.

## Architecture
Prefer a few small self-contained Godot 4 C# scripts/scenes under `BASEMENT/`, e.g. BasementPlanner, FloorPlan, FurnitureItem, DoorItem, PlacementValidator and LayoutSerializer. Avoid new global autoloads, dependencies on Fortress types, or edits outside the folder. If project-level configuration would otherwise be needed, implement local scene input handling instead.

## Acceptance
1. Opening `BASEMENT/BasementPlanner.tscn` and pressing F6 runs the planner without entering Fortress gameplay.
2. 11×13 main room and attached outward 4×5 room are visible.
3. Seven furniture rectangles and three doors exist with correct sizes.
4. Furniture moves, rotates, snaps at 0.25 tile; doors snap at 0.5 tile on walls.
5. Invalid overlaps and out-of-bounds placement are rejected with rollback.
6. Pan/zoom, save/load, reset work.
7. Deleting `BASEMENT/` removes the entire feature without breaking Fortress.

## Scope of this document
**Planning only. Do not implement BASEMENT1 yet.** This document is intended as instructions for a later Codex implementation. Confirm the exact orientation/opening of the attached room during implementation if needed.
