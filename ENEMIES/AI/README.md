# ENEMIES/AI — ST1 objective and breach targeting

**Status: Source Implemented / Awaiting Owner Validation.** `EnemyTargeting.cs` contains shared target validity, center-to-footprint attack range/solid line checks and direct cardinal cell-to-building boundary queries. `Enemy` caches the Citadel at spawn; `ActiveTarget` is either that Citadel or a temporary accessible breach building. No weighted retargeting, player aggro or scene-tree searches.

Reachability/selection runs inside the cooperative request queue described in `../MOVEMENT/README.md`, never in ordinary movement/attack ticks. Every chosen attack position is walkable, reachable from the job origin and outside the target footprint. Another wall/terrain blocks the contact line. After an opening, breachers reconsider the CPU. If no reachable damageable building exists, stop until topology changes instead of requesting endlessly.
