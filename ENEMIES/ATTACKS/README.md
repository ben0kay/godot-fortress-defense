# ENEMIES/ATTACKS

**Status:** Structure only — no gameplay implementation in this folder yet.

## Ownership

Enemy-specific triggers/aiming for contact, projectile, sustained beam, missiles, ramming and siege; shared hit/damage/projectile collision remains in root COMBAT. Keep movement strategy distinct from attack type.

## Implementation guidance

- Read `ENEMIES/README.md`, `NOTES/FOUNDATION_PERFORMANCE_PLAN.md`, `NOTES/CodePreferences.md` and relevant root docs before editing.
- Add files only for an authorized feature pass; use focused C#, Inspector-editable definitions when appropriate, and avoid unused scaffolding.
- Preserve the performance goal of 100–200 active combatants; document measured costs or limitations honestly.
- Update this README with concrete files, data flow, settings and status when implementation begins.
