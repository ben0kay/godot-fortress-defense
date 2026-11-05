# ENEMIES/VISUALS — ST1 arrow

**Status: Source Implemented / Awaiting Owner Validation.** `EnemyVectorVisual.cs` draws one lightweight arrow from the snapshotted enemy radius. Red = CPU seeker; orange = breach target; a brief white highlight accompanies hit/attack/state notifications. A damaged health arc and root FEEDBACK destruction ring provide basic lifecycle feedback.

Facing follows movement/attack direction. Drawing is event-driven; only the brief highlight enables visual processing. No visibility-based simulation suppression, crowds, fancy materials or unused visual child nodes. Artwork can slightly exceed the logical radius but never defines navigation/collision.
