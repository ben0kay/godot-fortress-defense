# AI Agent Instructions — Godot Fortress Defense

**Repo:** `ben0kay/godot-fortress-defense` · **default branch:** `main`, unless the owner specifies otherwise. This is the agent entry point; it complements the documents below rather than replacing them.

## Before implementation — including ongoing conversations

At the start of each new substantial task, or whenever asked to read `AGENTS.md`, refresh the current repository files even if this conversation read them earlier. Keep this focused; do not launch broad analysis or tests merely to refresh instructions.

1. **[`NOTES/CodePreferences.md`](NOTES/CodePreferences.md) — read fully.** Authoritative coding, ownership, performance, Git and minimal-validation preferences.
2. **Root [`README.md`](README.md)** — project vision, reference repositories, intended architecture and implemented versus planned status.
3. **Relevant feature README, plan/backlog notes and current code, scenes and resources.** Find the existing owner before adding another implementation.
4. **Existing lessons notes, if present and relevant.** Capture meaningful reusable findings in the appropriate existing notes; do not create a lessons-file hierarchy for routine changes.

If documents and source disagree, inspect the discrepancy and flag material differences. A planned folder, feature or milestone is not evidence that it exists.

## Project identity and references

This is a standalone **Godot 4 .NET / C#, top-down 2D vector-style fortress-defense, action and base-management game**. Preserve its mobile combat chassis, central CPU, procedural open battlefields, free construction and role-driven enemies.

- **`ben0kay/VECTOR_TD_2026`: gameplay/content reference.** Read only the relevant GML objects, scripts, data and notes to understand intended behavior; adapt the intent to Godot.
- **`ben0kay/godot-tower-defense`: architecture/style reference.** Reuse suitable ownership, configuration and reusable-scene patterns selectively.
- **Do not modify either reference repository as part of this project.** Do not introduce live cross-repository dependencies or inherit another repo's branch rules, visuals, fixed portal/path assumptions or unrelated systems.
- Other Godot repositories may inform a specific task, but their terrain, addons and project-specific instructions do not apply here automatically.

## Working rules

- Implement only the authorized pass. Preserve working behavior, authored assets, stable paths, optimizations and unrelated changes. Avoid speculative frameworks, broad rewrites and gratuitous managers.
- Follow existing feature ownership and uppercase umbrella folders. The README's structure is a target, not a reason to relocate working code or create empty scaffolding.
- Keep `WORLD/` authoritative for grid, occupancy and navigation; `COMBAT/` for shared hit resolution; and targets responsible for their lifecycle. Separate enemy target choice, movement and attacks.
- Keep resource quantities/transfers in `ECONOMY/`, transformations in `PRODUCTION/`, deliveries in `LOGISTICS/` and electrical supply in `POWER/`. UI displays state and sends actions.
- Use readable, compact C#, responsibility comments, comments above methods and meaningful regions. Expose useful tuning through Inspector settings/resources without unnecessary configuration frameworks.
- Design for hundreds of entities: dormant idle systems, event-driven updates, staggered/bounded queries, deliberate cleanup and no repeated full-world scans or per-frame pathfinding. Do not claim performance gains without measurements.
- Build in small playable milestones. For substantial features, describe scope, affected owners/files, Inspector knobs, performance implications and brief owner acceptance checks. Do not implement later phases without authorization.
- Keep feature docs and existing work/backlog notes accurate when behavior or enduring usage changes. Distinguish **Planned / In Progress / Source Implemented / Awaiting Owner Validation / Accepted**; a commit or build does not establish gameplay acceptance.

## Validation and Git

- **Keep validation brief.** Default to diff/source review; for substantive C# changes, one `dotnet build` when practical. Add only a directly relevant small smoke check for a high-risk change.
- **No extensive or repeated test suites, lengthy headless sessions, visual automation, simulations or FPS benchmarks unless requested.** The owner performs extensive in-editor gameplay, visual, balance and performance testing. Do not install a toolchain merely to validate.
- Documentation-only changes need only file/diff review. Report only checks actually performed and identify any remaining owner checks.
- For authorized repository edits, **commit and push by default unless the owner says not to**, normally as one logical commit. Respect a requested branch. Never force-push, force-reset or discard unrelated work.
- If the owner requests chat-only snippets, provide complete copy/paste-ready methods or short files and destination paths instead of committing.
- Keep handoffs short: what changed, touched paths, commit SHA, checks performed and any useful owner inspection.

**Reminder:** Re-reading this entry point means refreshing its linked project instructions before implementation, not relying on an earlier conversation summary.
