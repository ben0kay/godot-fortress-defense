# Code Preferences — Godot Fortress Defense

> **Applies only to:** `ben0kay/godot-fortress-defense` (Godot 4 .NET / C#, top-down 2D).
> **Default branch:** `main`, unless the owner explicitly requests another branch.
> **Read first:** [Project README](../README.md), this file, relevant feature README/notes and existing code. Read `AGENTS.md` if one exists.

These preferences guide AI-assisted and manual development. This is a **separate sister game**, not a second mode inside `godot-tower-defense`. Its gameplay reference is [VECTOR TD 2026](https://github.com/ben0kay/VECTOR_TD_2026); its Godot architecture/style reference is [godot-tower-defense](https://github.com/ben0kay/godot-tower-defense). Adapt useful designs, **do not copy unrelated implementation or modify the reference repositories**.

The goal is a maintainable, performant, playable **vector-style fortress-defense/action/base-management** game: a mobile combat chassis, central CPU, freely placed structures, procedural battlefields, geometric enemies, multi-target enemy AI, and eventually mining, energy, production and logistics. **The README's future feature list is not a claim that these systems are implemented.**

## Before every AI task

1. Confirm the **repository, branch, scope and actual files** before editing. Read the root README, this file, any `AGENTS.md`, the relevant feature docs and current source. Trust implemented code over old plans.
2. Find the existing feature owner and shared systems before adding code. Prefer enhancing them over building a second competing implementation.
3. Keep changes **focused and proportional**. Don't add speculative systems, broad refactors or changes to unrelated visuals, gameplay or balance.
4. Prefer **one commit per authorized job**. For tasks authorizing repository edits, **push by default unless the owner says not to**, report changed files and commit SHA. Do not force-push, force-reset, discard or overwrite unrelated work. **If the owner explicitly requests chat-only snippets, provide copy/paste-ready complete methods/files instead of committing.**
5. Use the minimal validation policy below; don't turn small implementation requests into lengthy test campaigns.

## Validation and testing — IMPORTANT OWNER PREFERENCE

**The owner performs extensive gameplay, visual, balance and FPS testing in the Godot editor. AI agents should not spend substantial time running tests unless explicitly requested.**

- **Default:** inspect the diff and perform a brief source sanity check. For substantive C# changes, **one `dotnet build` when practical** is generally enough; add only a directly relevant small smoke check for a high-risk change.
- **Do not** run repeated builds, broad regression suites, lengthy headless Godot sessions, prolonged simulations, visual acceptance passes or performance benchmarks by default.
- **Documentation-only or small configuration edits:** reviewing the relevant file/diff is enough; no game build required.
- When tools are unavailable or validation would be disproportionate, stop at source review. Don't install a toolchain merely to validate.
- Never say a feature was tested, working or accepted unless the corresponding check actually occurred. State what was checked and what remains for the owner.
- These instructions override older generic notes demanding exhaustive AI tests, unless the owner explicitly requests deeper validation for the task.

## Coding style and readability

- Begin every new script with a brief comment explaining its responsibility.
- Prefer concise comments **above methods/functions**, including a `// =========================================================` separator where useful for quick navigation.
- Use C# `#region` / `#endregion` for meaningful groups; not for every trivial method.
- Prefer **moderately vertically compact, readable C#**. Simple, understandable code is more valuable than clever abstractions or extreme compression.
- Choose descriptive names, small focused methods, shared helpers and reusable components. Centralize recurring logic like staggered checks or target filters when it genuinely reduces duplication.
- Group related settings with typed classes/resources, `[ExportGroup]` and appropriate Inspector exports. Expose frequently tuned parameters rather than burying magic numbers.
- Keep copy/paste guidance practical: when the owner asks for manual changes, supply **complete methods or complete short files**, their destination paths and clear placement instructions. For many scene/Inspector adjustments, provide a concise `.tscn` snippet if requested and safe to paste; avoid overwriting authored scene content.
- Follow this repository's conventions and existing C# Godot APIs. Reference repos are examples, not mandatory templates.

## Folder ownership, scenes and configuration

- Organize by **feature ownership** with recognizable uppercase umbrella folders as described in the root README; don't make one giant Scripts/Scenes directory. **Never put `BUILDINGS/`, `TOWERS/`, `ECONOMY/`, `POWER/`, `PRODUCTION/` or `LOGISTICS/` individually at repository root: all six belong inside `FORTRESS/`.**
- Each substantial feature folder should have a short `README.md`: purpose, important files, dependencies, configurable values, current implementation status and any known limitations.
- Prefer one **feature-level master config** with exported settings or typed `Resource` / `.tres` defaults when there are enough related knobs to justify it. Do not create empty config frameworks just to satisfy a naming convention.
- Keep scene trees readable. Plain container `Node` helpers are welcome; container-only nodes **do not require processing scripts**. Avoid unnecessary autoloads, giant controllers and deep hard-coded `GetNode` paths.
- Prefer explicit signals, interfaces, callbacks and clear ownership boundaries. Keep authored definitions separate from per-instance mutable state and keep cosmetic feedback separate from gameplay resolution.
- Intended owners (see README for details):
  - `WORLD/`: procedural map generation, authoritative grid/occupancy, terrain, navigation and deposits.
  - `LEVELS/`: scenarios, difficulty and attack-wave composition.
  - `ENEMIES/`: fortress-specific enemy catalog, behaviors, targeting, spawners and movement integration.
  - `PLAYER/`: combat chassis, movement, health, weapons and abilities.
  - `FORTRESS/`: single top-level umbrella for **all base-owned systems**; it is an organizational folder, not a monolithic manager.
    - `FORTRESS/BUILDINGS/`: construction, CPU, walls, durability, repair and destruction.
    - `FORTRESS/TOWERS/`: definitions, targeting, heat, weapons, upgrades and rank.
    - `FORTRESS/ECONOMY/`: resource identities, balances, costs, inventory and transfers.
    - `FORTRESS/POWER/`: generators, relays, networks, consumers, buffers and outages.
    - `FORTRESS/PRODUCTION/`: extraction, mining, refining, fabrication and recipes.
    - `FORTRESS/LOGISTICS/`: cargo drones, jobs, collection, delivery and reservations.
  - `COMBAT/`: shared hits, damage types, shields and resistance resolution (also used by enemies and player).
  - `UI/` and `FEEDBACK/`: menus, HUD, inspectors and shared audiovisual feedback.
  - `SYSTEMS/`, `CAMERA/`, `AUDIO/`, `TESTS/`: cross-scene services/persistence, camera, sound and lightweight checks.
- These are **target owners, not proof these folders exist**. Do not relocate working subsystems solely to match an aspirational diagram.
- Don't couple shared mechanics to a single test map, player chassis, geometric enemy or building instance.

## Performance-first implementation

**For each new feature ask:** What activates it? What runs while idle? How does it stop? How does work scale with hundreds of enemies, bullets, drones, buildings or resource nodes?

- **Inactive should mean dormant.** Avoid `_Process`/`_PhysicsProcess` polling when an event, signal, timer or explicit update can do the job. Disable processing and visuals when inactive; ensure reliable wake-up transitions.
- Stagger target acquisition, enemy decisions and expensive queries; use bounded work, caching, spatial indexing, pooling or batching where they actually help. Do not scan every enemy, building or entire world every frame.
- Keep navigational state and occupancy authoritative in `WORLD/`. Recompute paths when meaningful geometry changes or on controlled intervals, **not per enemy per frame**. Differentiate flying, ground and obstacle-ignoring movement appropriately.
- For procedural generation, use **reproducible seeds**, separated generation stages, valid buildable areas and reachable objectives/resources. Avoid huge synchronous spikes where feasible; don't introduce obvious pop-in or delayed response for trivial savings.
- High-count vector enemies, projectiles and visual feedback should minimize needless draw calls, allocations and per-object updates. Consider shared renderers/buffers when warranted, but preserve readable gameplay logic and collisions.
- UI/inspectors should update from relevant state changes rather than rebuilding layouts every frame.
- Separate authoritative combat, economy, energy and navigation data from purely cosmetic effects.
- Clean up timers, target references, reservations, navigation requests and deferred work on death, despawn, building removal and level transitions.
- Do not claim FPS, CPU, memory or load-time improvements without measurements. Let the owner conduct extended profiling by default.

## Fortress-specific gameplay boundaries

- **Not conventional path-only tower defense:** enemies may choose the **CPU/Citadel, player or individual buildings** as targets, and some attack from range, chase, support others or fly into structures and explode. Support role-driven, data-configured behaviors without baking every enemy into one path-following model.
- Keep **target choice**, **movement/navigation**, and **attack execution** separable so geometric enemy variants can combine behaviors without duplicating code.
- The battlefield uses **procedural open-map generation** and free building placement; do not import fixed portal-path assumptions from `godot-tower-defense`.
- Preserve the game's distinct **minimal geometric/vector aesthetic**: arrows, rotating circles, unusual shapes, legible silhouettes and efficient 2D effects. Avoid importing Classic TD art or styling by default.
- Keep tower data/inspector, damage, power and feedback concepts reusable, but **port selectively** and adapt to Fortress Defense's own rules; don't create a live cross-repository dependency without explicit authorization.
- For mining and manufacturing, keep everything under `FORTRESS/`: `ECONOMY` owns quantities/transfers, `PRODUCTION` owns recipes, `LOGISTICS` owns physical movement and `POWER` owns supply. World deposits and terrain remain in `WORLD/`. Avoid parallel inventories or hidden resource mutation across subsystems.
- Ship features in **small playable milestones**: first CPU + player + tower/wall + one attacker, then behavior variety, then power/resources/logistics. Do not build the entire production chain before the core loop works.

## Godot 4 C# reliability

- Use real Godot 4/.NET APIs that match the project; avoid invented signals, properties and unsafe background scene-tree mutations.
- Use conventional C# loops and clear null/validity handling. Keep references, exported settings, scene paths and stable IDs compatible when iterating.
- Treat physics updates, pause, scene transitions, entity death/destruction and cleanup as deliberate lifecycle boundaries.
- Threaded procedural work, if later required, must have bounded/cancellable jobs and may only apply safe, current results to the scene tree.

## Documentation, phased plans and reporting

- Read feature Markdown before editing: some notes contain crucial design intent but may be outdated. Distinguish **source-implemented**, planned and unverified behavior.
- For substantial features, propose small passes with **scope, affected files/owners, Inspector knobs, performance implications and owner acceptance checks**; don't implement future phases without authorization.
- Use accurate status labels: **Planned / In Progress / Source Implemented / Awaiting Owner Validation / Accepted**. A commit or build is not gameplay acceptance.
- Update relevant feature README(s) and work/backlog notes when implementing changes. Keep documentation aligned without creating unnecessary process bureaucracy.
- If a recurring bug or meaningful performance lesson emerges, capture it in the appropriate existing notes.
- Report concisely: **what changed, touched paths, commit SHA, lightweight checks (if any), what the owner should inspect**.

**Bottom line:** Preserve a small, clear, fast, distinctly vector-style fortress-defense project. Reuse well-chosen Godot Tower Defense patterns and Vector TD 2026 gameplay ideas; avoid accidental cross-game coupling. Implement only the requested pass, validate lightly, commit and push authorized repo work by default, and let the owner handle extensive in-editor testing.
