# AIR2 — Tower002 Anti-Air

**Source Implemented / Awaiting Owner Validation.** Tower002 inherits the canonical `FORTRESS/BUILDINGS/SHARED/Building.tscn` directly. It is an Air-only offensive building using existing layer-filtered spatial targeting, rotating-gun aiming and shared airborne projectiles. No Tower001 scene inheritance, duplicated weapon loop, new projectile registry or gunship.

## Use

In Level001 or DEBUG, **Ctrl → Defense → Anti-Air Tower / Tower002**, then LMB/drag for repeated placement. Esc/RMB cancels. Tower002 costs **100 Credits** in Level001; DEBUG remains explicitly free. It uses the same actual-footprint green/red preview, actor/terrain/reservation checks, atomic purchases and inherited placement/health/destruction. It blocks ground navigation as a normal building; flyers pass over it.

Open **F2 → enemy003 → Add** to send flyers. Tower002 rotates and shoots them over terrain/buildings; its rounds cannot hit ground enemies. Tower001 remains Ground-only, using the same shared index/cadence and its original balance. With only ground enemies, Tower002 cannot acquire/fire at them. No extra placement shortcut is needed: the catalogue drives the menu.

## Inspector defaults

| Resource | Values |
| --- | --- |
| Tower002Definition.tres | tower002; Defense; 1×1; 1000HP; Credits100 |
| Tower002CombatConfig.tres / TargetLayers | **Air** |
| Same config / weapon | Damage30; 1.68shots/s; range323.4px; projectile2300px/s |
| Same config / aiming | Turn216°/s; tolerance12°; search0.15s; spread±2.5° |
| Same config / presentation | Projectile radius1.5px; tracer18px |

These provisional Level1 numbers intentionally match the current basic Battle Tower; no separate AA balance was specified. All remain Inspector-editable. Author changes then place a new tower/restart: runtime stats and fired shots are snapshots. Health/price/footprint stay on BuildingDefinition. TargetLayers supports None/Ground/Air/Both for reusable tower capabilities, with this scene's authored default Air. No heat/ammo/power/reload/upgrades/ranks/XP/research/construction.

`Artwork/aa_base.svg` and `aa_gun.svg` are local adaptations of Tower001's copied original Battle Tower art: fixed base with AA chevrons, independently rotating winged gun cradle and original64px registration. One functional muzzle stays `(0,-28.5)`; fins are cosmetic, not burst/multiple-barrel mechanics. The same footprint-dependent uniform art scaling supports Level001/DEBUG cells.

## Shared ownership and performance

`SHARED/Combat/ProjectileTower.cs` and ProjectileTowerConfig.cs now own the common opt-in weapon behavior previously implemented on Tower001. Tower001.cs and Tower001CombatConfig.cs remain compatibility subclasses at their original paths, preserving scene/resource references and settings. Tower002.tscn uses the shared component/config directly with its own authored definition/art. All building lifecycle and root ownership stay canonical; COMBAT continues to own targeting/projectile delivery.

Acquisition uses nearby enemy buckets and the same staggered0.15s cadence. Cached-target validation includes layer eligibility. Per-shot Air eligibility and Airborne collision profile are snapshotted from the selected enemy layer; ground terrain/buildings cannot block the shot, and Ground bodies cannot intercept it. World bounds/range and swept target hits still apply. No per-frame global searches, new path work, detection areas or pooling. Empty enemy registries sleep; removal/destruction unbinds firing. No performance measurement is claimed.

## Brief owner checks

1. Ctrl-place Tower002 in Level001; verify100-credit purchase once, footprint/collision, HP and AA identification. Repeat in free DEBUG.
2. F2-spawn enemy003: inspect turret rotation, real straight rounds, hits/deaths and shooting across ground obstacles.
3. Mix enemy001/002/003: Tower001 should attack ground and Tower002 air. Ground bodies/buildings must not intercept AA rounds.
4. Check no targets in range, source/target destruction, Clear, reset/regeneration and removal freeing occupancy.
5. Inspect larger mixed groups when convenient. Only source/resource checks were possible without Godot/.NET; build/gameplay/FPS acceptance remains with the owner.

AIR2 stops here. AIR3 orbiting gunship, later aircraft, power, ammo and tower progression remain unimplemented.
