# ENEMIES — feature ownership

**Status:** Folder foundation only; gameplay is not implemented.

This is the authoritative home for fortress-specific enemy identities, simulation, target selection, movement, attacks, special abilities, modifiers, spawning, visual design and performance coordination. Inspired by `VECTOR_TD_2026`; follow this repository's README, CodePreferences, AGENTS and FD0–FD5 plan.

## Structure

| Folder | Responsibility |
| --- | --- |
| `CORE/` | Shared enemy lifecycle, state, runtime identity and registry |
| `DATA/` | Godot Resource definitions, tunable enemy stats, profiles and presets |
| `TYPES/` | Concrete vector enemy variants and family-specific content |
| `AI/` | Target selection, decisions, tactical orders and behavior coordination |
| `MOVEMENT/` | Steering, standoff/orbit, traversal and navigation requests |
| `ATTACKS/` | Enemy-specific contact, projectile, beam, missile and ram logic |
| `ABILITIES/` | Phasing, stealth actions, splitting, shielding allies, transport, regeneration |
| `MODIFIERS/` | Optional shielded/stealthed/elite/etc. attributes and variant rules |
| `SPAWNING/` | Spawn queues, waves, pressure, formations and enemy grouping |
| `VISUALS/` | Vector shapes, animations and enemy-specific visual adapters |
| `PERFORMANCE/` | Staggered scheduling, target indexing, entity counters and budgets |

**Composition principle:** Enemy *type* is identity/content; AI, movement, attacks, abilities and modifiers are reusable configurable behavior. Avoid a different giant controller per type or a broad subclass tree. Not all abstract capabilities require Godot child nodes; lightweight C# state and shared services are preferred in hot paths.

**Cross-feature boundaries:** root `WORLD/` owns navigation/grid and deposits; root `COMBAT/` owns generic damage, projectile collision/hit resolution and shields; root `DEBUG/` owns instrumentation; root `UI/` owns panels. Enemies can target the player, Citadel/CPU or buildings under `FORTRESS/`. Enemy projectiles can use shared combat facilities rather than duplicating a projectile engine.

**Performance target:** Design to scale toward 100–200 fully active enemies at 60 FPS in a representative fight, without claiming this is measured. Never pathfind or globally scan targets every frame. Schedule AI checks and preserve offscreen gameplay correctness.

**Future content from VECTOR TD 2026:** seekers, hunters, single/triple shooters, sniper, kamikaze, brute/berserker, siege beam/rocket, phaser, flyers/gunship, splitters, transporters, shield support, centipede formations. Old GameMaker behavior is a reference, not a required direct port.

Add actual code and data only in the authorized FD4 or later pass. Keep README statuses factual.
