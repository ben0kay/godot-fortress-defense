# Resources — BM0

**Source Implemented / Awaiting Owner Validation.** `ResourceWallet.cs` owns mutable balances once per level under RootFortress/Resources; no autoload, UI-owned balance, processing loop, kill rewards, income, persistence or refunds. Root status caches credits via BalanceChanged, alongside its existing health timer. UI may subscribe to wallet BalanceChanged and placement CatalogueChanged; gameplay never depends on a particular panel.

## Authoring

- `LevelConfig`: Catalogue, StartingCredits, AllowedBuildingIds and FreeDebugPlacement. Level001 starts at1,000; wall001/tower001 are allowed. Empty allowlist locks everything. Reset/regenerate discards the wallet and starts fresh.
- BuildingDefinition: Purchasable, description/icon and Cost array of ResourceCost entries. Wall001 costs10 Credits; Tower001 costs100. These are editable starter prices, not final balance. Citadel is non-purchasable and absent from the default catalogue.
- ResourceCost: typed Id + positive integer Quantity. Duplicate IDs aggregate with overflow checks. Empty list explicitly means free; null/zero/negative/undefined-ID/overflow recipes are rejected. Credits is the only currently funded resource. Copper/Iron/Silicon are reserved schema IDs with zero balances, not implemented inventory or mining. Add later physical IDs to the enum without changing purchase transactions. Electricity is not a spendable material.

## Purchase contract

Call `BuildingPlacement.TryPurchaseAndPlace(id, anchor, out building, out failure)`. It validates catalogue availability, definition, full footprint, terrain/reservations/actor overlap and affordability. Synchronous claim runs under reentrancy guards; failed claims free the candidate through canonical occupancy/registry cleanup. Debit occurs only after a successful claim, leaving no post-debit fallible placement operation to refund. Success/balance notifications occur after occupancy and all debits are final. The same held cell is ignored by drag input; a second attempt on occupied cells cannot charge again.

`TryPlaceFree` is the named bypass for initial CPU setup and explicitly configured technical testing. DEBUG uses it; Level001 uses purchases by default. `FreeDebugPlacement=true` restores unlimited Level001 benchmark construction without bypassing terrain/actor checks. No refunds on removal/destruction. Author prices/definitions before starting a session; live editing shared resources is unsupported. Only supported gameplay availability changes go through SetAllowedBuildingIds.

## Brief owner checks

Run Level001: see1,000 credits; place W wall (990), T tower (890). Try occupied/terrain/reserved/enemy cells: balance stays unchanged. Set a low starting balance or remove tower001 from AllowedBuildingIds and confirm failed attempts leave world/balance unchanged. R creates a fresh session. DEBUG and explicitly enabled free Level001 placement never charge. Confirm inherited combat/navigation still operate. BM1 menu, purchasing materials, income and later player/camera/fog passes remain planned. No runtime or FPS validation claimed.
