# ENEMIES/CORE

**Status:** Structure only — no gameplay implementation in this folder yet.

## Ownership

Shared enemy runtime identity, lifecycle, spawn/death/despawn, state transitions and registration. Own minimal enemy data/state; do not implement combat, movement or every ability in a giant Enemy.cs.

## Implementation guidance

- Read `ENEMIES/README.md`, `NOTES/FOUNDATION_PERFORMANCE_PLAN.md`, `NOTES/CodePreferences.md` and relevant root docs before editing.
- Add files only for an authorized feature pass; use focused C#, Inspector-editable definitions when appropriate, and avoid unused scaffolding.
- Preserve the performance goal of 100–200 active combatants; document measured costs or limitations honestly.
- Update this README with concrete files, data flow, settings and status when implementation begins.
