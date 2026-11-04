# ENEMIES/MODIFIERS

**Status:** Structure only — no gameplay implementation in this folder yet.

## Ownership

Optional enemy traits such as Shielded, Stealthed, Armored and Elite. Definitions/flags describe eligibility and presets; mutable shield level, reveal status, buffs and cooldowns remain in runtime state. Generic shield damage belongs in COMBAT.

## Implementation guidance

- Read `ENEMIES/README.md`, `NOTES/FOUNDATION_PERFORMANCE_PLAN.md`, `NOTES/CodePreferences.md` and relevant root docs before editing.
- Add files only for an authorized feature pass; use focused C#, Inspector-editable definitions when appropriate, and avoid unused scaffolding.
- Preserve the performance goal of 100–200 active combatants; document measured costs or limitations honestly.
- Update this README with concrete files, data flow, settings and status when implementation begins.
