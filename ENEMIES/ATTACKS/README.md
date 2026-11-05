# ENEMIES/ATTACKS — ST1 contact damage

**Status: Source Implemented / Awaiting Owner Validation.** `EnemyMeleeAttack.cs` owns a cooldown independent of path search. An arrived enemy attacks only a live building within center-to-footprint range and with a clear contact line; it calls the existing shared `Building.ApplyDamage` scalar receiver. Attack intervals stay at physics cadence even offscreen. No catch-up hit bursts after long frames.

Damage/range/cooldown are authored in EnemyDefinition and snapshotted at spawn. Defaults: 8 damage, 24-pixel range, 1.2 seconds. Larger configured clearance requires a correspondingly larger range. Shields, generic resistance pipelines, ranged attacks and projectiles remain future work.
