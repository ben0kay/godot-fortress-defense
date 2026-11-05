# ENEMIES/TYPES

**Status:** ST1 `Basic/Enemy001/Enemy001.tscn` and its definition are Source Implemented / Awaiting Owner Validation. Other families remain planned.

## Ownership

Enemy-specific assets, presets and small exception handlers. Organize future content into Basic, Hunters, Shooters, Kamikaze, Heavy, Siege, Flying, Support, Transport, Formations and Bosses only when a real type needs the folder. Do not duplicate shared mechanics.

## Implementation guidance

- Read `ENEMIES/README.md`, `NOTES/FOUNDATION_PERFORMANCE_PLAN.md`, `NOTES/CodePreferences.md` and relevant root docs before editing.
- Add files only for an authorized feature pass; use focused C#, Inspector-editable definitions when appropriate, and avoid unused scaffolding.
- Preserve the performance goal of 100–200 active combatants; document measured costs or limitations honestly.
- Update this README with concrete files, data flow, settings and status when implementation begins.
