# ENEMIES/DATA — ST1 definition

**Status: Source Implemented / Awaiting Owner Validation.** `EnemyDefinition.cs` is a GlobalClass Resource with stable EnemyId, HP, radius, movement/turn speed and contact damage/range/cooldown. Default content lives in `../TYPES/Basic/Enemy001/Enemy001Definition.tres`.

All numeric stats must be finite and positive, radius below half a cell. Instances snapshot these at spawn; changing the shared resource does not mutate existing enemies. For whole-cell clearance N, cardinal attack range must reach at least `(N + 0.5) × CellSize`. Advanced movement roles, rewards, modifiers and abilities remain future data, not unused fields in this first definition.

EN2 adds TargetPriority.BuildingHunter plus optional BuildingHunterConfig (local radius/cadence/hysteresis) and EnemyRangedConfig (speed/lifetime/radius/tracer/aim). Enemy002Definition.tres is separate from Enemy001. Resources validate finite positive settings; projectile lifetime must cover the attack range. Runtime captures relevant settings on spawn. No shields/rewards/tiers.
