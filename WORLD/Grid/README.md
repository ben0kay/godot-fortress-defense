# WORLD/Grid — authoritative occupancy and projectile queries

WorldGrid owns the existing terrain/occupancy arrays, bounds, footprint reservations and topology revisions. See [WORLD contracts](../README.md) for placement, navigation and collision ownership.

## COMBAT1 query filtering

**Source Implemented / Awaiting Owner Validation.** FirstObstacleFraction overloads accept an optional `WorldProjectileFilter`, default **AllSolids**. FriendlyTower skips occupied building cells only when the placed building's `BlocksFriendlyTowerProjectiles` snapshot is false; terrain and world bounds always block. The source footprint exception remains explicit through `ignored`. The returned fraction/building refer to the earliest eligible obstacle; terrain/bounds win equal-entry building ties. PositiveInfinity means no contact.

The existing sweep checks the segment's local cell bounds, expands cell rectangles by projectile radius and uses the same slab intersections/ordering for both policies. It reads authoritative occupancy directly, with no registry enumeration, secondary collision map or per-shot physics node. Multi-cell buildings are filtered consistently across every occupied cell.

Friendly tower acquisition, final pre-fire validation and projectile movement explicitly request FriendlyTower. Default FirstObstacleFraction and **all SegmentClear/movement/navigation queries are unchanged**; hostile ground rounds continue using AllSolids, while airborne rounds retain the separate world-boundary query. Building obstruction settings never change occupancy, physical collision, topology or sight.

Default definitions block friendly tower rounds; Wall001 opts out. Definitions are snapshotted on building initialization, so author changes require new instances/reload. Owner checks: walls versus CPU/towers/dead terrain, multiple segments, radius/grazing/high-speed ordering, hostile wall hits and unchanged ground routing/AA/fog eligibility. No runtime validation was available here.
