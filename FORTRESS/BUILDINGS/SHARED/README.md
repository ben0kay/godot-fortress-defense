# SHARED — building foundation

**Status:** `Building.tscn` scene shell exists. Runtime scripts and gameplay are planned (BF0).

`Building.tscn` is the canonical inherited base scene for **every** building. It currently contains a `Node2D` root and a `Visuals` attachment point only; nothing processes per frame. Future `Building.cs`, `BuildingDefinition.cs` and `BuildingRegistry.cs` will provide common identity, rectangular multi-cell footprints, health, selection, placement, destruction and registration.

Inherited concrete scenes add optional static `BaseSprite`, independently rotating `RotatingParts`, baked `AnimatedParts`, and future `ConstructionVisuals`. These are not mandatory base children. The building's collision/occupancy rectangle comes from authored footprint data, not artwork dimensions.

Do not instantiate this bare shell as a gameplay building or assume health/placement currently works. See `../../../NOTES/INITIAL_STRESS_NAVIGATION_PLAN.md`.
