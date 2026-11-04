# FORTRESS/SYSTEMS

**Status:** `BuildingPlacement/BuildingPlacement.cs` is Source Implemented / Awaiting Owner Validation (BF0). It composes the shared Building lifecycle with root WORLD occupancy and the per-world BuildingRegistry; it has no idle processing. Shared health currently lives in the small Building root, without a redundant health manager. Other systems remain planned.

Shared building placement and health, credits/material accounting, power distribution, production, and cargo logistics belong here **when needed**. Buildings and their scenes belong under `../BUILDINGS/`. The authoritative grid/navigation belongs to root `WORLD/`. Root `SYSTEMS/` (when introduced) owns application/session/persistence, not fortress mechanics.
