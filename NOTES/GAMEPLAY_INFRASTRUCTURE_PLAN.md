# Fortress Defense — Gameplay Infrastructure Plan (BM0–BM1, PL0, CAM0, FW0)

**Status:** BM0 **Source Implemented / Awaiting Owner Validation**. BM1, PL0, CAM0 and FW0 remain **Planned**. No later-pass implementation authorized by this note alone.
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

**Source Implemented / Awaiting Owner Validation.** See [resource/transaction settings and owner checks](../FORTRESS/SYSTEMS/Resources/README.md). Level001 starts with 1,000 credits and permits wall001 (10) / tower001 (100); CPU setup is free and non-purchasable. DEBUG explicitly bypasses purchases. No BM1 menu, earning, refunds or material acquisition implemented.

**Responsibilities:** `FORTRESS/BUILDINGS/SHARED/` owns authored building identity/definition; a small catalogue of available placeable scenes/definitions (suggest `FORTRESS/BUILDINGS/SHARED/BuildingCatalogue` or similarly focused path) is reusable by menus and level configs. `FORTRESS/SYSTEMS/Resources/` owns mutable **per-level/session balances and purchases**. `FORTRESS/SYSTEMS/BuildingPlacement/` remains authoritative for placement. Avoid parallel wallets in UI/buildings and avoid one manager per resource.

- Represent prices as a flexible list/map of typed **resource ID + positive quantity** entries; currently **Credits** only. Future copper/iron/silicon/material cost combinations should require no redesign. Never model electricity as a spendable material: it is a future network flow.
- Per-building editable default cost and display/icon/category/scene/footprint identity must be accessible without instantiating every building each frame. Prefer existing definition Resource extended judiciously, with a catalogue of authored entries and **stable IDs**.
- Initially register **Wall001** and **Tower001** as purchasable; **Citadel is non-purchasable**. Keep costs Inspector editable, and use authored level-specific starting credit balance and allowed/unlocked building IDs (the latter only needs a basic allowlist now).
- One single purchase-and-place operation: validate unlock/availability, resource balance and full grid/actor/terrain/footprint first; claim placement and debit as one atomic logical transaction. Invalid/blocked/aborted placement **does not charge**; avoid double-charging on repeated clicks. Guard malformed/negative prices and insufficient funds; if failure happens after a debit, rollback safely. No refunds for deletion unless defined later.
- Expose balance and catalogue-change notifications; UI subscribes instead of polling. Maintain existing direct DEBUG/test controls with explicit **free debug-placement bypass** where needed, not accidental credit charges.
- Suggested acceptance: starting balance visible via a lightweight diagnostic; place a valid wall/tower and balance falls exactly once; blocked/overlapping/locked/unaffordable attempts leave balance and world unchanged; scene reset creates a fresh level session; debug sandbox still functions.
- Performance: static catalogue cached, no global scans or per-frame wallet updates.

## BM1 — Godot Tower Defense-inspired category → building menu

**Location:** root `UI/BuildMenu/` and optionally small reusable controls under `UI/Shared/`. Integrate via `LEVELS/SHARED/`, not a `Level005`-only UI fork.

- **Ctrl** toggles a build menu. Show **categories first**: Defense, Energy, Extraction, Economy, Industry, Infrastructure (Core displayed only if useful, but Citadel locked/not purchasable). Clicking a category opens a **sub-menu** of available buildings; clicking one selects the placement preview. Start with Defense → Wall001 and Tower001. Future categories can show a tidy empty/locked state, not fake working buildings.
- Match the reference game's **clean vector/sci-fi panels, button treatment, typography, spacing, highlights, selection and placement feedback** as closely as sensible; copy/adapt artwork/layout snippets from `godot-tower-defense` locally where useful. This is for style/interaction rather than fixed-path tower gameplay.
- Show building name, icon/preview, cost(s), footprint, short description, available credits, affordance for locked/unaffordable entries. Menu is driven from the catalogue and current level's allowlist; **no switch statement with one hardcoded button per building**.
- Maintain one placement mode/state owner; valid preview green/invalid red or equivalent established reference styling, grid snapped for real footprint, preserve user selection and existing wall/tower behavior. Escape/right-click cancel placement, return to world interaction normally, and don't block UI clicks with world-placement input. Clarify whether Ctrl closes/toggles menu while placement is active. Keep keyboard/mouse interactions discoverable.
- Avoid colliding with the current `Ctrl` or other bindings in Godot (review `project.godot` and existing `LevelController` logic). F1 remains generation/world info, F2 remains debug spawns; avoid stealing F2 for fog.
- Initially keep Godot Tower Defense-like UI presentation but do **not** copy upgrade/heat/ammo/power Inspector panels or introduce a full tower-upgrade system.
- Acceptance: Ctrl → Defense → Tower001/Wall001 → preview → valid purchase placement; cancel doesn't charge; blocked/unaffordable visibly communicated; repeated placement is deliberate; Level001 and Level005 can reuse the same UI scene without duplicated implementation.

## PL0 — player foundation

**Ownership:** root `PLAYER/` player scene/controller/movement/health foundation; `LEVELS/SHARED/` spawns; future combat in root `COMBAT/` and `PLAYER/Weapons/` when authorized.

- Replace/consume Level001's current reserved `PlayerSpawn` `Marker2D` with actual spawned lightweight, visually identifiable top-down mobile chassis, initially beside the Citadel. Keep start position legal, preserve world cell reservation lifecycle, and do not overlap Citadel, terrain or other solids.
- Basic movement with configurable acceleration/top speed/collision radius (simple first pass), facing/aim driven by pointer if helpful; input mapping Inspector-configurable; no weapon fire, ammo, chassis class/progression, stats-heavy UI, AI player targeting or complex animation yet.
- Player must respect terrain/building collisions and not bypass world boundaries; walls should not be placed on an occupied player. Keep building interface usable without accidentally steering/shooting while it has focus.
- Do not mix player state, camera code, and world generation in one large controller.
- Acceptance: player spawns next to CPU, moves visibly and collides correctly, can build and cannot occupy a newly placed wall.

## CAM0 — follow/free-roam camera modes

**Ownership:** root `CAMERA/` camera scripts/settings; level scene contains/configures a reusable camera instance, not separate custom camera algorithms per level.

- **Default camera follows player**, with smoothness, offset, zoom and bounds exposed in Inspector.
- **Tab** toggles to free-roam camera (mouse pan/middle-drag and/or configured keyboard movement) and back; zoom with wheel. Switching should be deliberate and ideally preserve view smoothly.
- Define camera-vs-player input ownership and priority so moving the free camera does not issue player movement by accident. Menu/UI input must not leak into map placement.
- Camera position **must not change fog visibility**. The player can inspect parts of the world without revealing them just by moving the free camera.
- Level001 full-map/debug inspection remains achievable (debug override where needed). Keep current world regeneration/debug controls, scrolling/zoom, and preserve an option to test without the player cam.

## FW0 — three-state fog of war (later)

**Ownership:** `WORLD/FogOfWar/` authoritative visibility/exploration state and sight-source updates; root `UI/DebugPanels/` debug toggle; renderer can be under WORLD while UI consumes state.

- **Never explored:** opaque black; **explored but not currently visible:** dim; **currently visible:** full color. Per-world cell visibility should persist during a level, not globally between levels unless saves are later implemented.
- Sight comes from **player, Citadel and eventually towers/scouts/buildings**, not from camera position. Expose source-specific reveal radii; decide LOS/terrain occlusion rules explicitly in FW0 before implementing complex masks. Use the existing grid/terrain as source of truth.
- Separate simulated AI/combat from rendering visibility: fog must **not disable offscreen enemy simulation/attacks/paths**. Avoid accidentally leaking invisible enemies in the explored-but-not-visible area; remembered terrain/buildings and current actor visibility require explicit semantics.
- Add a clear **Fog Enabled** checkbox/toggle to **F2 debug menu**, bypassing fog presentation for testing but leaving gameplay fog/exploration state intact. Existing F2 spawn controls remain; F1 generation controls remain. Show disabled state visually so FPS comparisons aren't misleading.
- Cache/stagger expensive vision recomputation, favor visibility masks/shared cell state over an LOS check for every entity each tick. Handle regeneration, scene reset and source deletion without stale reveal.
- Acceptance: move player to reveal, leave area dimmed, unopened areas black; free camera does not reveal; F2 hide/show works; enemies still fight under fog; removing a sight source updates visibility; no unwanted scanning per frame.

## Multi-level compatibility and later systems

- **Level001** stays the owner's technical benchmark: 63×63 grid / 64px cells / seeded terrain, plus existing F1/F2 stress utilities and free DEBUG setup as intentionally configured.
- **Level005** may become the generic baseline gameplay starter with costs, unlocks, player and eventual level-driven baseline/cluster/wave/milestone spawning. Implement its own `Level005Config.tres`, not by making other levels inherit Level005. `LEVELS/SHARED/LevelBase.tscn` and reusable systems stay shared.
- Consider later per-level starting credits, allowed building catalogues, wave pressure, win/lose requirements, but don't accidentally implement these full systems during BM0.
- Simplified future power: Citadel likely provides initial generation/distribution so players don't have to build a node, solar input and relay before their first tower; defer all network mechanics and power UI until explicitly requested.
- The user eventually wants buildings costing **multiple types of physical materials**. Only extend the cost schema now; actual inventory/extraction/transport and supply checks come in their respective later passes.

## Codex implementation handoff rule

At the start of **each** approved pass, refresh `AGENTS.md`, `NOTES/CodePreferences.md`, root `README.md`, this plan, relevant own-code and reference repo owners; inspect source for changes since this note. Use Godot Tower Defense as the **preferred initial structural and visual UI reference** and Vector TD for intended gameplay. Copy local styles/artwork where sensible, but retain `Building.tscn` inheritance and Fortress Defense world/navigation owners. Keep detailed work proportional: no speculative frameworks or unrelated rewrites.

Describe edited paths, Inspector properties, manual owner checks and known limitations; update status **Planned → Source Implemented / Awaiting Owner Validation** only for actually finished passes. Validate via brief source/diff review and at most a build when practical, no extensive test suites. Commit and push to `main` by default. Do not proceed from one pass to the next without owner authorization.
