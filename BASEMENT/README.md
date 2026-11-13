# Basement floor planner

**Source Implemented / Awaiting Owner Validation.** In the Fortress Defense Godot .NET editor, open `BASEMENT/basement.tscn` and press **F6**. The shortcut scene instances `BasementPlanner.tscn`, which also runs directly with F6.

- Left-drag any of the seven furniture pieces from the tray or floor. Press **R** to rotate a selected or dragged piece by 90°. Furniture snaps every quarter tile. Green previews can be placed, and red previews revert on release. **Esc** cancels the drag.
- Left-drag a cyan door opening to move it along a wall or onto another wall. Each door is 2.5 tiles wide and its start snaps every half tile. The fixed opening between the rooms spans from tile 1 to tile 4 along their shared edge.
- Scroll to zoom toward the pointer, middle-drag to pan, and click **Fit view** to frame both rooms and the tray. **Grid** toggles the floor grid.
- **Save** and **Load** use one slot at `user://basement_layout.json`. **Reset** returns all furniture to the tray and all doors to their original positions. Click **Save** after a reset to replace the saved arrangement.

`BasementLayout.cs` holds tile dimensions, tray slots, wall segments, collision rules and JSON persistence. `BasementPlanner.cs` draws and edits the layout. Both are independent of Fortress game systems and project input mappings. Deleting `BASEMENT/` removes the planner from the project. The saved JSON is stored in Godot's user data directory.
