# WORLD/Terrain — WG0 classification and presentation

**Status: Source Implemented / Awaiting Owner Validation.**

`TerrainType.cs` initially defines Ground and DeadCell only. `TerrainDefinition.cs` exposes type/tint with matching fixed WG0 buildability/traversal semantics; Ground/DeadCell `.tres` assets provide level appearance. Appearance definitions cannot turn a logical dead cell into traversable ground. Future terrain identities/rules should extend these owners; deposits remain separate overlay/occupancy features rather than being introduced now.

`TerrainRenderer.cs` draws one ground rectangle and the grid's merged solid rectangles through one passive CanvasItem. Level001 dead tint is solid black; no textures, Sprite2D per cell, processing scripts or per-frame terrain generation. Optional grid/route/placement visualization shares the renderer. Static drawing refreshes only after explicit state/input/topology changes, not the HUD timer. The existing DEBUG GridDebugView path is a compatibility subclass of this renderer.

`TerrainRectangles.cs` merges identical horizontal solid runs vertically. Every output rectangle covers only actual dead cells; holes stay open. WorldGrid owns the same rectangle list and creates one passive CollisionShape2D per rectangle on one StaticBody2D. There is no body/script per cell and no simplification that permits enemies across dead terrain. Structural rectangle counts are visible in F1; no measured FPS improvement is claimed.

Startup bulk terrain creates shapes once. Rare DEBUG terrain edits rebuild the small terrain rectangle partition; **building/wall** edits do not rebuild terrain collision and still update navigation only in the dirty footprint. Old shapes are safely disabled/deleted before replacement. Grid state and swept movement remain authoritative, independent of render geometry. No chunk streaming or physics threading.
