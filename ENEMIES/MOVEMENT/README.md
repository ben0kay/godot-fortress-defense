# ENEMIES/MOVEMENT

**Status:** Structure only — no gameplay implementation in this folder yet.

## Ownership

Enemy movement modes and steering, pursuit, orbit, standoff, grounding/flying/phased traversal. Consume WORLD navigation services; don't create a competing terrain grid or repath all enemies each frame.

## Implementation guidance

- Read `ENEMIES/README.md`, `NOTES/FOUNDATION_PERFORMANCE_PLAN.md`, `NOTES/CodePreferences.md` and relevant root docs before editing.
- Add files only for an authorized feature pass; use focused C#, Inspector-editable definitions when appropriate, and avoid unused scaffolding.
- Preserve the performance goal of 100–200 active combatants; document measured costs or limitations honestly.
- Update this README with concrete files, data flow, settings and status when implementation begins.
