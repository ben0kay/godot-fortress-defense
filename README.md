# Godot Fortress Defense

A top-down, sci-fi **fortress-defense, action, and base-management** game built in **Godot 4 with C#/.NET**.

The goal is to recreate and then expand the gameplay of my earlier **VECTOR TD 2026** GameMaker project, while using the cleaner, modular, documented architecture established in **godot-tower-defense**.

> **Project status:** BF0/ST0 and BF1/ST1 are **Source Implemented / Awaiting Owner Validation**: shared inherited buildings, centered Citadel, placeable Wall001, gradual Enemy001 edge spawning, safe continuous movement, contact damage and reachable wall-breach fallback in `DEBUG/test.tscn`. See [controls and manual checks](DEBUG/README.md) and [WORLD contracts](WORLD/README.md). ST2 advanced priorities/profiling/stress presets, advanced tower systems, player combat, power and logistics remain planned. **WG0** now adds [Level001 procedural world](LEVELS/Level001/README.md): 4032×4032px, seeded black terrain clusters, central clearing, marker reservation and regeneration. Run that scene with F6; F5 keeps the fixed DEBUG sandbox. All new WG0 work is Source Implemented / Awaiting Owner Validation.

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

**All player-constructible structures are buildings** and live in `FORTRESS/BUILDINGS/`, grouped by player-facing purpose. The shared gameplay services used by those buildings live in `FORTRESS/SYSTEMS/`. Root `UI/` owns **all interfaces** including fortress build menus and inspectors. Other gameplay domains remain separate at root.

```text
AUDIO/                        Shared audio
CAMERA/                       Player camera
COMBAT/                       Shared damage / shields / projectiles
DEBUG/                        Runtime metrics / stress tools (UI views in root UI)
ENEMIES/                      Enemy data, AI, movement and spawning
FEEDBACK/                     World visual effects and feedback
FORTRESS/
  BUILDINGS/                  Every player-built structure
	Core/                     Citadel / CPU and unique central structures
	Defense/                  Offensive towers, repair towers, shields, walls
	Energy/                   Generators, solar, wind, pylons/relays, batteries
	Extraction/               Ore-specific mines and extractors
	Economy/                  Credit-producing structures
	Industry/                 Refineries, fabricators, ammunition plants
	Infrastructure/           Storage, cargo hubs, construction support
  SYSTEMS/                    Shared mechanics, NOT additional buildings
	BuildingPlacement/        Placement, construction rules and occupancy calls
	BuildingHealth/           Structure lifecycle and durability
	Resources/                Credits, physical materials, accounting
	PowerNetwork/             Connected supply, consumption and buffers
	Production/               Mining/refining/fabrication processing
	Logistics/                Drone dispatch and material transfers
LEVELS/                       Scenarios and waves
NOTES/                        Code preferences and plans
PLAYER/                       Chassis and weapons
SYSTEMS/                      Game session, profiles, persistence (not fortress systems)
TESTS/                        Focused checks
UI/                           ALL menus, HUD, build menu, inspectors, debug panels
WORLD/                        Procedural map, grid, navigation, ore deposits
```

**Categories are player/build-menu labels, not exclusive gameplay capabilities.** A Copper Mine is in `BUILDINGS/Extraction/` but uses power, inventories and logistics. A Repair Tower is in `BUILDINGS/Defense/` and can consume electricity. The central CPU is in `BUILDINGS/Core/`, not a separate root class. A universal building definition can identify category, footprint, unlock requirements, health, build cost and optional capabilities without a separate manager for every building.

**Resource semantics:** Credits are currency/account balances, electricity is networked power generation/demand with optional batteries, and ores are physical inventory (e.g., copper/iron/silicon) mined from deposits, stored and moved by logistics. Power is **not** treated as physical cargo. The `BUILDINGS/Economy/` category means credit-generation buildings only; the common `FORTRESS/SYSTEMS/Resources/` owns resource accounting for all categories.

### Ownership rules

- **WORLD** owns authoritative terrain, resource deposits and grid/occupancy state. Buildings query or request occupancy changes through its services; **ENEMIES** uses navigation services, not a competing grid.
- **FORTRESS/BUILDINGS** owns the constructible catalog, its scenes/data and category organization. **All towers are buildings**. Place offensive and repair towers under `Defense/`; put ore-specific mines in `Extraction/`, generators and relays in `Energy/`, credit earners in `Economy/`.
- **FORTRESS/SYSTEMS** owns shared building mechanics: placement, building health, resource accounting, power topology, processing and logistics. Don't copy these mechanics into each building. Systems can serve more than one building category.
- **COMBAT** owns reusable damage/shield resolution for buildings, player and enemies. **PLAYER** owns player state. **ENEMIES** owns its own AI, targets, roles and spawning.
- **Root UI/** owns all user interfaces — gameplay HUD, menus, building placement UI, building inspectors and F1 panels. `DEBUG/` owns collection/instrumentation, not the panel views.
- **LEVELS** defines scenarios; root **SYSTEMS** owns session/save services (distinct from `FORTRESS/SYSTEMS`).
- Keep everything modular and data-driven; categories are not a forced one-to-one mapping to systems or capabilities.

## Coding and configuration conventions

Read **[NOTES/CodePreferences.md](NOTES/CodePreferences.md)** before proposing or changing code. It is copied from the Godot Tower Defense project to preserve my preferred working style.

In particular:

- Prefer clear, compact, maintainable **C#** over unnecessarily abstract frameworks; optimize where it matters.
- Put explanatory comments at the top of scripts and above methods, using clear comment separators and regions where appropriate.
- Use exported settings, resource definitions, grouped Inspector properties and per-umbrella master configs for frequently tuned values.
- Keep scene trees readable by grouping related nodes; avoid unnecessary node clutter.
- Centralize shared systems (navigation, economy, damage, audio) without putting unrelated behavior into one controller.
- Consider hundreds of enemies and towers early: avoid repeated full-scene searches, pathfinding every frame, wasteful allocations and expensive per-entity work.
- Follow the minimal AI validation policy in CodePreferences; the owner performs extensive Godot tests.
- Record **implemented vs planned** functionality accurately in feature READMEs. Large future-design documents are not proof that a feature exists.

## Suggested development order

1. **Foundation:** project folders/READMEs, C# setup, boot/menu/profile flow, camera, shared grid and test scene.
2. **First playable loop:** mobile player, central CPU, one buildable tower/wall, one attacker, basic health/damage and loss condition.
3. **Combat depth:** enemy/tower catalogs, targeting, projectiles and beams, building repairs/destruction, varied enemy objectives and waves.
4. **Infrastructure:** resource deposits, storage, energy generation and connected networks.
5. **Production loop:** mining, refining, fabrication, ammunition resupply and cargo-drone delivery.
6. **Content and progression:** more towers/enemies, chassis abilities, veterancy, minimap/fog, campaign persistence and balance.

Work in small, independently testable passes. Keep the project playable after each major addition. Port **behavior and design intent**, not GameMaker-specific object/event patterns.

## PWR0 — Adaptive Power Mesh

**Source Implemented / Awaiting Owner Validation.** The Citadel anchors a range-based relay graph in Level001 and DEBUG. Components can merge or split, and endpoints attach to a single nearest eligible relay. F2 shows counts in the expanded debug panel. [Setup, API and owner checks](FORTRESS/SYSTEMS/PowerNetwork/README.md) · [PWR1–PWR5 roadmap](FORTRESS/SYSTEMS/PowerNetwork/ADAPTIVE_POWER_MESH_ROADMAP.md).

## Development notes

This is a **separate sister project**, not a replacement for either reference repository. The target is to preserve what made Vector TD 2026 distinctive—direct player combat, destructible defenses, enemy counterattacks, power and physical logistics—while benefiting from the modular structure and maintainability of Godot Tower Defense.

Before making substantial changes, inspect the current repository state, the appropriate feature README, [the coding preferences](NOTES/CodePreferences.md), and only the relevant source features in the two reference projects.

## Reusable development overlay

`UI/DebugPanels/Debug.tscn` is linked under RootDebug in LevelBase/Level001 and DEBUG/test. It shows FPS/live enemies and has **F2** manual spawn controls. Edit the packed scene once to update all instances. See [debug reuse and settings](UI/DebugPanels/README.md). RootWorld, RootFortress, RootEnemies, RootCamera and RootUI keep the rest of the tree readable. Only this basic ST2 debug slice is source implemented; advanced scheduling/profiling remains planned, and editor validation remains with the owner.

## Tower001 — basic Battle Tower

**Source Implemented / Awaiting Owner Validation.** In Level001 or DEBUG, **T** selects Tower001 placement and **W** restores walls. F2 spawns enemies. The inherited building uses local original Battle Tower SVG layers, a rotating gun, Inspector-editable Level1 stats and shared swept projectiles/spatial targeting. RootCombat/Projectiles owns fired shots. See [Tower001 settings/owner checks](FORTRESS/BUILDINGS/Defense/OffensiveTowers/Tower001/README.md). Heat, ammo, power, upgrades/ranks/XP, construction, research and shields remain excluded; no combat FPS result is claimed.

## BM0 — construction catalogue and credits

**Source Implemented / Awaiting Owner Validation.** Level001 uses a per-level wallet (1,000 starting credits), an allowed-building catalogue and Inspector-editable prices: walls 10, Tower001 100. Existing W/T placement now purchases once per valid placement; blocked, locked or unaffordable attempts charge nothing. Status shows credits. CPU setup and DEBUG placement are explicitly free; deletion gives no refund. Enable Config → FreeDebugPlacement to restore unlimited Level001 benchmark construction. [Settings and owner checks](FORTRESS/SYSTEMS/Resources/README.md). BM1 supplies the Ctrl menu; revised optional player/camera are now source implemented; FW0 fog is now source implemented.

## BM1 — category building menu

**Source Implemented / Awaiting Owner Validation.** **Ctrl → Defense → Wall001/Tower001** opens the shared build tray/details UI, styled after Godot Tower Defense. Tiles show credits, prices, footprints and locked/unaffordable states. Selecting closes the menu; **LMB/drag** purchases repeatedly with a green/red full-footprint icon ghost. **Esc/RMB** cancels; Ctrl browsing pauses/resumes the selection. W/T remain quick selects, F1/F2 stay intact and DEBUG remains explicitly free. All menu/preview code lives in root UI and is linked into LevelBase and DEBUG. [Controls, reference and owner checks](UI/BuildMenu/README.md). No later gameplay systems added.

## Revised PL0 + CAM0 — optional chassis and camera

**Source Implemented / Awaiting Owner Validation.** Level001 stays player-free by default. Enable Config → SpawnPlayer (PlayerScene already points to PLAYER/Player.tscn) to spawn an arrow-key chassis beside the CPU. Configurable enemy rules can attack it using existing queued navigation and typed damage; removal/death returns enemies to Citadel behavior. Placement rejects the player body. **C** toggles free/follow only for a living player; **Home** frames the map. Free Roam is always the default. Enemy clearing is now exclusively in F2's existing Clear control. F1/F2/Ctrl building and tower combat remain available. See [player/settings/objective events](PLAYER/README.md) and [camera/settings](CAMERA/README.md). No player weapons or full defeat/results flow.

## EN2 — Enemy002 Building-Hunter Shooter

**Source Implemented / Awaiting Owner Validation.** F2 now has an enemy001/enemy002 manual type selector; queued batches preserve type and may be mixed under the existing200 combined cap. Automatic spawning remains the authored Enemy001 scene by default. Enemy002 globally acquires the nearest living building, keeps a strategic lock, locally switches with15%/1s hysteresis, navigates to reachable firing positions and shoots damage6 rounds. The shared roster bounds global/recovery/routine decisions; BuildingRegistry now indexes full stationary footprints. Enemy001's CPU/player/melee behavior remains. [Enemy002 settings, architecture and owner checks](ENEMIES/TYPES/Ranged/Enemy002/README.md). No extra variants, player targeting for Enemy002, rewards or measured FPS guarantee.

## AIR0 + AIR1 — air/ground foundation and Enemy003

**Source Implemented / Awaiting Owner Validation.** **F2 → enemy003 → Add** spawns Basic Flyers: 25HP,145px/s,12px radius,15HP once-only CPU impact. They use the shared enemy lifecycle/index but fly directly over ground terrain/buildings without A*. Enemy001/Enemy002 remain Ground; Tower001's new TargetLayers defaults Ground and ignores flyers. Shared projectile profiles prepare airborne enemy hits and intended-ground-target overflight without new projectile registries. [Enemy003 settings/contracts/owner checks](ENEMIES/TYPES/Flying/Enemy003/README.md). Existing cap/batches, automatic Enemy001 spawning, optional player and debug controls remain. AIR2 now adds Tower002 (below); AIR3 gunship remains **Planned**; no mixed-air FPS result claimed.

## AIR2 — Tower002 Anti-Air

**Source Implemented / Awaiting Owner Validation.** **Ctrl → Defense → Anti-Air Tower / Tower002** in Level001/DEBUG. Default100 Credits (free in DEBUG),1×1/1000HP, Air-only targeting and airborne rounds that ignore ground obstacles/bodies. It uses Battle Tower-derived local AA art, a fixed base/rotating gun and current Level1 combat values. Common tower behavior/config now lives in `FORTRESS/BUILDINGS/SHARED/Combat/`, with stable Tower001 compatibility scripts and unchanged ground defaults. [Tower002 settings and owner checks](FORTRESS/BUILDINGS/Defense/OffensiveTowers/Tower002/README.md). F2 enemy003 provides targets. AIR3 and advanced tower systems remain planned; no FPS result claimed.

## FW0 fog of war

**Source Implemented / Awaiting Owner Validation.** Shared levels and DEBUG now start with CPU vision, optional player exploration, black unknown terrain and dim remembered terrain. Hidden enemy/combat visuals are masked while simulation continues. F2 → **Fog of War Enabled** reveals the full simulation for testing without resetting exploration. Master settings: `WORLD/FOG/FogMasterConfig.tres`; per-level Fog/FogEnabled overrides. [Architecture, limits and owner checks](WORLD/FOG/README.md). No minimap or fog-based AI culling.

## FW1 building vision and tower visibility

**Source Implemented / Awaiting Owner Validation.** Every friendly building now reveals terrain: CPU retains its dedicated radius, Tower001/Tower002 default to authored weapon range, and walls/ordinary structures use FogMasterConfig.DefaultBuildingVisionRadius (64px). Building.VisionRadiusOverride supports independent sight. Offensive towers only acquire/track/fire at currently visible eligible enemies, including with F2 presentation disabled; fired shots continue normally. [Provider/lifecycle/settings and owner checks](WORLD/FOG/README.md). Existing fog renderer and enemy simulation are preserved.

## COMBAT1 friendly fire over walls

**Source Implemented / Awaiting Owner Validation.** Friendly ground towers can acquire/shoot visible enemies over Wall001. BuildingDefinition.BlocksFriendlyTowerProjectiles defaults true; only explicitly opted-out barriers are skipped. Other buildings, CPU, dead terrain and bounds still block. Shared acquisition/pre-fire/projectile sweeps agree; ground movement, hostile wall hits, AA and FW1 logical sight remain unchanged. [Collision policy and owner checks](COMBAT/README.md#combat1--friendly-tower-rounds-over-low-barriers).

## FOG-PERF1A hidden enemy cosmetics

**Source Implemented / Awaiting Owner Validation.** Hidden shared enemy visuals can now skip flash ticks and cosmetic redraw work while gameplay simulation continues unchanged. F2 adds **Optimize Hidden Enemy Visuals** plus logical visible/hidden counts; turning fog presentation off restores normal cosmetics for inspection. Initial setting: FogMasterConfig.OptimizeHiddenEnemyVisuals=true. [Lifecycle, settings and owner checks](ENEMIES/PERFORMANCE/README.md). No measured FPS gain or AI throttling is claimed.

## C1 Battle Tower construction

**Source Implemented / Awaiting Owner Validation.** Tower001 Level 1 places its foundation immediately, waits for a Citadel drone, and begins three seconds of staged work after beam deployment. The original turret and full vision activate at completion. The builder returns for reuse. See the [construction lifecycle](FORTRESS/BUILDINGS/SHARED/Construction/README.md) and [drone settings](FORTRESS/SYSTEMS/ConstructionDrones/README.md).

## DMG0 shared damage

**Source Implemented / Awaiting Owner Validation.** Existing attacks share a typed damage packet and resolver. Enemy, Player and Building still own their health and lifecycle. See [damage authoring and owner checks](COMBAT/README.md#dmg0-typed-damage).

PWR1 adds finite Citadel generation and a private capacitor for Tower001. Select a completed Battle Tower to see power status. [Power settings and owner checks](FORTRESS/SYSTEMS/PowerNetwork/README.md#pwr1--citadel-generation-and-battle-tower-power).
