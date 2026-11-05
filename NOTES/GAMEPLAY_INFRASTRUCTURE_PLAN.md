# Fortress Defense — Gameplay Infrastructure Plan (BM0–BM1, PL0, CAM0, FW0)

**Status:** BM0, BM1, revised PL0 and CAM0 **Source Implemented / Awaiting Owner Validation**. FW0 is also **Source Implemented / Awaiting Owner Validation**. No later-pass implementation authorized by this note alone.
**Repository / branch:** `ben0kay/godot-fortress-defense` / `main`  
**Sequence:** BM0 (catalogue/resources) → BM1 (category-based build menu) → PL0 (player) → CAM0 (camera) → FW0 (fog of war). Each pass requires explicit approval.  
**First:** read `AGENTS.md`, `NOTES/CodePreferences.md`, root `README.md`, this plan, owning READMEs, and current source/scenes. Preserve `LEVELS/Level001` combat-stress baseline and debug tools.

## Goals and visual reference

Transition from direct DEBUG wall/tower hotkeys to the beginning of a proper player-facing game, eventually starting with `LEVELS/Level005` as a configurable baseline gameplay level. Preserve `Level001` as the owner's practical technical benchmark. The owner has observed ~60 FPS with 200 basic enemies and a few shooting towers on an entirely visible map; **this is owner-reported for that scenario, not a measured guarantee for expanded systems**.

**Preferred initial look/feel:** closely follow `ben0kay/godot-tower-defense` for **UI layout, visual styling, reusable Godot scenes, feedback, placement previews, controls and code organization**. Copy/adapt suitable assets and UI patterns locally **where practical and appropriate**, rather than inventing a different visual language. This is intentionally a stronger style preference than the earlier generic 'selectively reference' wording. Still preserve Fortress Defense's own distinctive vector enemies/world, mobile player, dynamic free-placement and existing shared `Building.tscn` foundation; do **not** port the reference project's separate `TowerBase` hierarchy, fixed-lane path assumptions, persistence/research/campaign complexity, or live asset dependencies. Any copied files must be local to this repo and paths/references fixed. Inspect the original files rather than copying blindly.

Reference files worth inspecting before implementing:
- Godot Tower Defense `UI/GameplayHUD/GameplayHUD.tscn`, `GameplayHUD.cs`, `UI/GameplayHUD/TowerBar/TowerBar.tscn`, `UI/GameplayHUD/TopBar/TopBar.tscn`, `UI/GameplayHUD/Inspector/InspectorPanel.tscn`, `UI/Shared/MenuLayout.cs` and their READMEs; also inspect any relevant placement, catalogue, wallet, costs, input and camera owners found in the reference repository.
- `ben0kay/VECTOR_TD_2026` gameplay/build menu/player/fog/credit systems as design references, especially where its earlier onboarding and power hookup added too many early steps.
- Current Fortress Defense `FORTRESS/BUILDINGS/SHARED/BuildingDefinition.cs`, `FORTRESS/SYSTEMS/BuildingPlacement/BuildingPlacement.cs`, `LEVELS/SHARED/LevelController.cs`, `LEVELS/SHARED/LevelBase.tscn`, `LEVELS/Level001/`, `UI/DebugPanels/`, `WORLD/Grid/`, `WORLD/Navigation/`, `ENEMIES/` and `Tower001`/Wall001 scenes.

**Design principle:** user-facing operations should be simple, while the resource model and building/camera/fog boundaries permit later depth. Keep all UI in root `UI/`, not under `FORTRESS/` or `LEVELS/`. Keep per-scene gameplay owners reusable and event-driven. Build menus are presentation, not inventory/placement authority.

## Existing baseline / scope boundary

Source already has shared `Building.tscn` + `BuildingDefinition` with `Core/Defense/Energy/Extraction/Economy/Industry/Infrastructure` categories, grid-validated placement, level-owned Citadel, Wall001, Tower001 with basic firing, Enemy001, procedural Level001 and `F2` debug spawning. **BM0 adds authored purchase prices and cached catalogue metadata to building definitions.** Level controller currently handles direct wall/tower placement; these are test controls, not the finished menu/economy. Actual runtime status must be rechecked before each pass.

Do **not** implement power, ammo, heat, upgrades, research, production, logistics, mining, player weapons, full campaign progression, complex player AI interactions, or completed fog inside BM0/BM1. Future ore/material purchase costs are a data contract, not permission to make ores obtainable yet.

## BM0 — data-driven building catalogue + credits (first implementation pass)

**Source Implemented / Awaiting Owner Validation.** See [resource/transaction settings and owner checks](../FORTRESS/SYSTEMS/Resources/README.md). Level001 starts with 1,000 credits and permits wall001 (10) / tower001 (100); CPU setup is free and non-purchasable. DEBUG explicitly bypasses purchases. BM1 now supplies the shared menu; earning, refunds and material acquisition remain excluded.

**Responsibilities:** `FORTRESS/BUILDINGS/SHARED/` owns authored building identity/definition; a small catalogue of available placeable scenes/definitions (suggest `FORTRESS/BUILDINGS/SHARED/BuildingCatalogue` or similarly focused path) is reusable by menus and level configs. `FORTRESS/SYSTEMS/Resources/` owns mutable **per-level/session balances and purchases**. `FORTRESS/SYSTEMS/BuildingPlacement/` remains authoritative for placement. Avoid parallel wallets in UI/buildings and avoid one manager per resource.

- Represent prices as a flexible list/map of typed **resource ID + positive quantity** entries; currently **Credits** only. Future copper/iron/silicon/material cost combinations should require no redesign. Never model electricity as a spendable material: it is a future network flow.
- Per-building editable default cost and display/icon/category/scene/footprint identity must be accessible without instantiating every building each frame. Prefer existing definition Resource extended judiciously, with a catalogue of authored entries and **stable IDs**.
- Initially register **Wall001** and **Tower001** as purchasable; **Citadel is non-purchasable**. Keep costs Inspector editable, and use authored level-specific starting credit balance and allowed/unlocked building IDs (the latter only needs a basic allowlist now).
- One single purchase-and-place operation: validate unlock/availability, resource balance and full grid/actor/terrain/footprint first; claim placement and debit as one atomic logical transaction. Invalid/blocked/aborted placement **does not charge**; avoid double-charging on repeated clicks. Guard malformed/negative prices and insufficient funds; if failure happens after a debit, rollback safely. No refunds for deletion unless defined later.
- Expose balance and catalogue-change notifications; UI subscribes instead of polling. Maintain existing direct DEBUG/test controls with explicit **free debug-placement bypass** where needed, not accidental credit charges.
- Suggested acceptance: starting balance visible via a lightweight diagnostic; place a valid wall/tower and balance falls exactly once; blocked/overlapping/locked/unaffordable attempts leave balance and world unchanged; scene reset creates a fresh level session; debug sandbox still functions.
- Performance: static catalogue cached, no global scans or per-frame wallet updates.

## BM1 — Godot Tower Defense-inspired category → building menu

**Source Implemented / Awaiting Owner Validation.** See [UI/BuildMenu](../UI/BuildMenu/README.md). Ctrl opens category-first browsing; choosing closes it and arms repeated placement. Ctrl close resumes selection, Esc/RMB cancels. Definition/allowlist/wallet events drive tiles and details; the full-footprint icon ghost checks terrain, occupancy, actors, locks and funds. No later passes implemented.

**Location:** root `UI/BuildMenu/` and optionally small reusable controls under `UI/Shared/`. Integrate via `LEVELS/SHARED/`, not a `Level005`-only UI fork.

- **Ctrl** toggles a build menu. Show **categories first**: Defense, Energy, Extraction, Economy, Industry, Infrastructure (Core displayed only if useful, but Citadel locked/not purchasable). Clicking a category opens a **sub-menu** of available buildings; clicking one selects the placement preview. Start with Defense → Wall001 and Tower001. Future categories can show a tidy empty/locked state, not fake working buildings.
- Match the reference game's **clean vector/sci-fi panels, button treatment, typography, spacing, highlights, selection and placement feedback** as closely as sensible; copy/adapt artwork/layout snippets from `godot-tower-defense` locally where useful. This is for style/interaction rather than fixed-path tower gameplay.
- Show building name, icon/preview, cost(s), footprint, short description, available credits, affordance for locked/unaffordable entries. Menu is driven from the catalogue and current level's allowlist; **no switch statement with one hardcoded button per building**.
- Maintain one placement mode/state owner; valid preview green/invalid red or equivalent established reference styling, grid snapped for real footprint, preserve user selection and existing wall/tower behavior. Escape/right-click cancel placement, return to world interaction normally, and don't block UI clicks with world-placement input. Clarify whether Ctrl closes/toggles menu while placement is active. Keep keyboard/mouse interactions discoverable.
- Avoid colliding with the current `Ctrl` or other bindings in Godot (review `project.godot` and existing `LevelController` logic). F1 remains generation/world info, F2 remains debug spawns; avoid stealing F2 for fog.
- Initially keep Godot Tower Defense-like UI presentation but do **not** copy upgrade/heat/ammo/power Inspector panels or introduce a full tower-upgrade system.
- Acceptance: Ctrl → Defense → Tower001/Wall001 → preview → valid purchase placement; cancel doesn't charge; blocked/unaffordable visibly communicated; repeated placement is deliberate; Level001 and Level005 can reuse the same UI scene without duplicated implementation.

## PL0 — revised optional modular player

**Source Implemented / Awaiting Owner Validation.** This revision supersedes the original mandatory-player/AI-excluded foundation. See [PLAYER](../PLAYER/README.md).

- Reusable PLAYER/Player.tscn supplies Inspector-configurable arrow movement, speed/acceleration/radius, health and simple aqua vector chassis visuals. No weapons/upgrades/abilities.
- LevelConfig exposes SpawnPlayer, PlayerScene and PlayerRequiredForDefeat. Player spawning defaults **off** in Level001; an optional absence is valid and preserves the benchmark. Enabled players spawn at PlayerSpawn beside the CPU. Plain RootPlayer owns the instance.
- WORLD grid sweeps enforce terrain/buildings/bounds. Shared placement and spawning reject overlap with a living player; no actor-specific pathfinding grid or mandatory player reference in WORLD/buildings.
- Shared ICombatTarget geometry/damage/loss events let Enemy001 consider an explicitly injected mobile target. EnemyDefinition selects CitadelOnly, PreferNearbyPlayer or NearestWithinRange and a detection radius. A bounded round-robin roster staggers decisions; every route uses the existing queue/budgets. Dead/removed/unreachable player targets safely fall back to the Citadel; CPU wall-breach behavior remains.
- LevelObjectiveSignals publishes PlayerDied/CitadelDestroyed and conditional one-shot DefeatRequested. Player-required levels request defeat on death/removal; optional/no-player levels do not. CitadelRequiredForDefeat defaults true. No results screen, gameplay stop or mission-end flow implemented.

## CAM0 — revised optional follow / default Free Roam

**Source Implemented / Awaiting Owner Validation.** This revision supersedes the earlier default-follow/Tab plan. See [CAMERA](../CAMERA/README.md).

- Reusable CAMERA/BattlefieldCamera.tscn starts **Free Roam**, full-map framed, independent of player presence. MMB pans; wheel zooms; Home frames the full map.
- **C** toggles follow for a living injected player. With none it safely stays free; death/removal returns to free automatically. Inspector configures zoom, follow smoothing/offset, pan sensitivity/button and world bounds.
- C is reserved for camera toggling in every level. Enemy clearing/cancelling/pausing stays in the existing **F2 Clear** control; the old C clear shortcut is removed.
- Ctrl/GUI input does not leak into camera/player movement or building placement. F1/F2 and W/T quick building controls remain intact. Arrow keys belong to the player, not the free camera.
- Only follow runs continuously; free motion/zoom/resize is input/event-driven. No camera-driven world reveal; fog is owned independently by FW0.

## FW0 — three-state fog of war

**Source Implemented / Awaiting Owner Validation.** Owner: `WORLD/FOG/`; configuration and detailed limits/checks: [Fog README](../WORLD/FOG/README.md).

- Existing grid-backed compact states retain exploration per level. Explicit CPU/optional-player circles use overlap counts, bounded phased source updates and dirty batches. Static sources do not poll; walls/terrain do not occlude sight.
- One retained low-resolution mask drives a soft world overlay and shared dynamic visual clipping. Enemy movement, targeting, shots, flight and path/spawn budgets are unchanged. No AI culling or minimap implemented.
- Friendly bases stay known/dim; unseen live HP, selection and gun feedback are hidden. This is remembered live base artwork, not a snapshot system. Dynamic enemy/projectile/effect visuals use the documented center-cell plus fragment rule.
- LevelConfig references a master config and can start presentation disabled. F2's **Fog of War Enabled** bypasses presentation only; state keeps tracking and re-enabling restores it. Existing controls remain.
- Source death/detach/removal cleans up contributions; scene regeneration resets exploration. Read-only states/queries/change notifications support a future minimap.
- Pending owner checks: player on/off; moving/overlapping/source-loss vision; unexplored black and explored dim; camera independence; all enemy types remain active while hidden; no live visual leaks; F2 bypass/state preservation; regeneration; FPS on/off comparison. No runtime or performance acceptance claimed.

## Multi-level compatibility and later systems

- **Level001** stays the owner's technical benchmark: 63×63 grid / 64px cells / seeded terrain, plus existing F1/F2 stress utilities and free DEBUG setup as intentionally configured.
- **Level005** may become the generic baseline gameplay starter with costs, unlocks, player and eventual level-driven baseline/cluster/wave/milestone spawning. Implement its own `Level005Config.tres`, not by making other levels inherit Level005. `LEVELS/SHARED/LevelBase.tscn` and reusable systems stay shared.
- Consider later per-level starting credits, allowed building catalogues, wave pressure, win/lose requirements, but don't accidentally implement these full systems during BM0.
- Simplified future power: Citadel likely provides initial generation/distribution so players don't have to build a node, solar input and relay before their first tower; defer all network mechanics and power UI until explicitly requested.
- The user eventually wants buildings costing **multiple types of physical materials**. Only extend the cost schema now; actual inventory/extraction/transport and supply checks come in their respective later passes.

## Codex implementation handoff rule

At the start of **each** approved pass, refresh `AGENTS.md`, `NOTES/CodePreferences.md`, root `README.md`, this plan, relevant own-code and reference repo owners; inspect source for changes since this note. Use Godot Tower Defense as the **preferred initial structural and visual UI reference** and Vector TD for intended gameplay. Copy local styles/artwork where sensible, but retain `Building.tscn` inheritance and Fortress Defense world/navigation owners. Keep detailed work proportional: no speculative frameworks or unrelated rewrites.

Describe edited paths, Inspector properties, manual owner checks and known limitations; update status **Planned → Source Implemented / Awaiting Owner Validation** only for actually finished passes. Validate via brief source/diff review and at most a build when practical, no extensive test suites. Commit and push to `main` by default. Do not proceed from one pass to the next without owner authorization.
