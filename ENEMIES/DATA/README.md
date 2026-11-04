# ENEMIES/DATA

**Status:** Structure only — no gameplay implementation in this folder yet.

## Ownership

Inspector-friendly C# Resource definitions and .tres presets for identity, vitals, target profile, movement, attacks, abilities, modifiers, rewards and visuals. Keep immutable definition data separate from mutable runtime state.

## Implementation guidance

- Read `ENEMIES/README.md`, `NOTES/FOUNDATION_PERFORMANCE_PLAN.md`, `NOTES/CodePreferences.md` and relevant root docs before editing.
- Add files only for an authorized feature pass; use focused C#, Inspector-editable definitions when appropriate, and avoid unused scaffolding.
- Preserve the performance goal of 100–200 active combatants; document measured costs or limitations honestly.
- Update this README with concrete files, data flow, settings and status when implementation begins.
