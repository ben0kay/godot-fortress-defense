# Godot Fortress Defense

A top-down, sci-fi **fortress-defense, action, and base-management** game built in **Godot 4 with C#/.NET**.

The goal is to recreate and then expand the gameplay of my earlier **VECTOR TD 2026** GameMaker project, while using the cleaner, modular, documented architecture established in **godot-tower-defense**.

> **Project status:** New Godot project / foundation stage. The mechanics below describe the intended game and architecture, **not features already implemented** here.

## Reference repositories

| Repository | Purpose |
| --- | --- |
| [VECTOR TD 2026](https://github.com/ben0kay/VECTOR_TD_2026) | **Gameplay and content reference.** Consult its GML scripts, objects, data definitions and notes for the original mechanics, enemy and tower rosters, world generation, resource flow, energy, logistics, player chassis, UI and combat. |
| [godot-tower-defense](https://github.com/ben0kay/godot-tower-defense) | **Godot architecture and coding reference.** Follow its C# style, uppercase feature umbrellas, per-feature READMEs, Inspector-editable resources, master configurations, reusable scenes, explicit ownership, performance budgets and regression tests where they fit this game. |

**Precedence:** This repository's actual code and feature READMEs are authoritative for what exists here. Use VECTOR TD 2026 as the gameplay specification and godot-tower-defense as the structural reference. Neither reference repository should be modified as part of work on this project. Avoid copying GML designs literally when Godot has a simpler native approach.

## Game concept

The player controls a mobile combat chassis and defends a central **CPU** by constructing a growing fortress. Enemy forces enter the map, approach from different directions and may attack the player, the CPU, towers, walls, production equipment or other valuable infrastructure.

Unlike a conventional tower-defense game, the player participates directly in combat and must also maintain the base's economy and defenses.

The intended core gameplay includes:

- **Active player combat:** movement, weapons, health/shields and selectable Assault, Heavy, Engineer and Support chassis, each eventually with distinct abilities.
- **Fortress construction:** place walls, towers, foundations and functional buildings on a shared grid, respecting footprints and navigation rules.
- **Destructible defenses:** buildings have health, can take enemy damage, and may be repaired, disabled or destroyed.
- **Diverse enemies:** ground and flying types, shooters, siege attackers, shield support, splitters, transports and special abilities with different tactical targets.
- **Enemy pressure:** continuous threats, clusters, major waves, milestone attacks and configurable spawn compositions.
- **Resources and production:** mine raw materials, store and refine them, fabricate supplies and ammunition, then deliver them to consumers.
- **Local power networks:** generators, relays/nodes, batteries, consumers, demand, buffers and outages.
- **Logistics:** cargo drones and material delivery between extraction, storage, production and defense.
- **Progression and feedback:** tower upgrades/ranks, player improvements, profile/campaign progression, readable combat feedback and a useful management HUD.

Implement these incrementally. The first milestone should be a playable **player + CPU + buildable tower/wall + attacking enemy** loop, not an entire production chain.

## Project structure

Organize by **feature ownership**, not by one huge Scripts/Scenes folder. Prefer uppercase top-level umbrella folders, focused subfolders, a `README.md` within each major feature and a conveniently located `*MasterConfig` with an Inspector-editable default `.tres` when useful.

The following is the **target structure**, not a claim that these folders already exist:

```text
AUDIO/           Shared playback, mixing and sound policies
CAMERA/          Player-follow, panning, zoom and camera behavior
COMBAT/          Shared damage types, hits, shields and resolution
ECONOMY/         Resource identities, balances, costs and transactions
ENEMIES/         Enemy catalog, types, AI, movement, spawning and abilities
FEEDBACK/        Hit effects, health bars, particles, alerts and world feedback
LEVELS/          Level definitions, scenarios, wave/pressure authoring
NOTES/           Coding conventions, plans, migration notes and decisions
PLAYER/          Chassis catalog, player movement, stats, weapons and abilities
BUILDINGS/       Shared non-tower structures, walls, foundations, utilities
TOWERS/          Tower catalog, placement, targeting, weapons and upgrades
POWER/           Connected local energy networks, batteries and distribution
PRODUCTION/      Mining processes, refining, fabrication and recipes
LOGISTICS/       Cargo drones, delivery jobs, storage transfers and reservations
SYSTEMS/         App-level profiles, saving and cross-scene services
TESTS/           Headless regression and performance checks
UI/              Boot/menu screens, gameplay HUD, construction and inspectors
WORLD/           Terrain, grid, navigation, resource nodes and world objects
```

### Ownership rules

- **WORLD** owns the authoritative terrain/grid and occupancy state. **ENEMIES** uses navigation services; it should not maintain a competing world grid.
- **BUILDINGS** owns generic construction, durability and non-tower structure behavior. **TOWERS** owns tower-specific targeting, weapons, upgrades and combat behavior.
- **COMBAT** owns reusable damage/shield calculations. The attacker delivers the hit; the target owns its lifecycle and death consequences.
- **ECONOMY** owns resource identities and transfers. **PRODUCTION** transforms materials; **LOGISTICS** physically moves them; **POWER** manages electrical supply. Keep responsibilities distinct.
- **PLAYER** owns chassis-specific state and abilities. **UI** displays state and sends actions but does not own the gameplay models.
- **LEVELS** provides level-specific content and configuration; **SYSTEMS** owns persistence across scenes and sessions.
- Prefer data-driven definitions and reusable components over duplicate scripts for every enemy, tower or building. Keep feature-specific files near their owning feature.

## Coding and configuration conventions

Read **[NOTES/CodePreferences.md](NOTES/CodePreferences.md)** before proposing or changing code. It is copied from the Godot Tower Defense project to preserve my preferred working style.

In particular:

- Prefer clear, compact, maintainable **C#** over unnecessarily abstract frameworks; optimize where it matters.
- Put explanatory comments at the top of scripts and above methods, using clear comment separators and regions where appropriate.
- Use exported settings, resource definitions, grouped Inspector properties and per-umbrella master configs for frequently tuned values.
- Keep scene trees readable by grouping related nodes; avoid unnecessary node clutter.
- Centralize shared systems (navigation, economy, damage, audio) without putting unrelated behavior into one controller.
- Consider hundreds of enemies and towers early: avoid repeated full-scene searches, pathfinding every frame, wasteful allocations and expensive per-entity work.
- Add or extend headless regression tests for major systems; preserve working gameplay.
- Record **implemented vs planned** functionality accurately in feature READMEs. Large future-design documents are not proof that a feature exists.

## Suggested development order

1. **Foundation:** project folders/READMEs, C# setup, boot/menu/profile flow, camera, shared grid and test scene.
2. **First playable loop:** mobile player, central CPU, one buildable tower/wall, one attacker, basic health/damage and loss condition.
3. **Combat depth:** enemy/tower catalogs, targeting, projectiles and beams, building repairs/destruction, varied enemy objectives and waves.
4. **Infrastructure:** resource deposits, storage, energy generation and connected networks.
5. **Production loop:** mining, refining, fabrication, ammunition resupply and cargo-drone delivery.
6. **Content and progression:** more towers/enemies, chassis abilities, veterancy, minimap/fog, campaign persistence and balance.

Work in small, independently testable passes. Keep the project playable after each major addition. Port **behavior and design intent**, not GameMaker-specific object/event patterns.

## Development notes

This is a **separate sister project**, not a replacement for either reference repository. The target is to preserve what made Vector TD 2026 distinctive—direct player combat, destructible defenses, enemy counterattacks, power and physical logistics—while benefiting from the modular structure and maintainability of Godot Tower Defense.

Before making substantial changes, inspect the current repository state, the appropriate feature README, [the coding preferences](NOTES/CodePreferences.md), and only the relevant source features in the two reference projects.
