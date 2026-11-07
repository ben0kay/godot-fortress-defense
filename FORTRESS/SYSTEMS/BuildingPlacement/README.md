# Building placement

BuildingPlacement validates the catalogue and footprint before claiming a site. Paid placement uses ResourceWallet to debit exactly once after a successful claim. TryPlaceFree is the explicit setup and debug bypass.

C1 initializes any opted-in site's construction state before it enters the tree. After a successful placement, the service publishes ConstructionRequested separately from Placed. The level dispatcher consumes that explicit construction request for both paid and intentionally free sites. Failed claims publish neither event. The initial Citadel has zero construction duration.
