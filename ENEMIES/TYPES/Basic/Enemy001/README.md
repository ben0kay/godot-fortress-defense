# Enemy001 — basic ground CPU seeker

**Status: Source Implemented / Awaiting Owner Validation.** `Enemy001.tscn` uses shared Enemy runtime + a single vector visual; `Enemy001Definition.tres` supplies defaults (30 HP, 7px radius, 70px/s, 8 damage / 1.2s, 24px range). It is not a duplicate enemy engine.

Spawner assigns a cached Citadel reference. It navigates through shared WORLD ground services, temporarily attacks a reachable breach building if necessary, and returns to the original CPU after topology changes. No steering, player targeting, shields or extra variants yet.
