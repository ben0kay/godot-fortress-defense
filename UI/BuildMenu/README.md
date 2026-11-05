# BM1 — Category-based building menu

**Source Implemented / Awaiting Owner Validation.** `BuildMenu.tscn` is instanced under RootUI in LevelBase (including Level001 and future inheriting levels) and DEBUG/test. Change this shared scene to update all instances. The level explicitly binds it after BM0 services initialize; no global singleton or per-frame scene searches.

## Controls

- **Ctrl** opens a fresh category-first browser. Defense contains catalogue-driven Wall001/Tower001; other non-Core categories show an honest empty state. Ctrl closes the browser and resumes any existing placement selection. While browsing, world building/removal/panning is paused.
- Hover/focus a building to see its local icon, name, category, description, complete footprint and aggregated resource costs. Locked and unaffordable tiles remain visible, labelled and disabled. Credits and availability update from BM0 events, not polling/layout rebuilds.
- Click an available, affordable tile to close the menu and start placement. **W/T** remain quick-select development controls using the same selection owner. Free DEBUG mode ignores price but retains physical placement checks and menu availability rules.
- **Left-click/drag** repeatedly places the chosen building through BM0 TryPurchaseAndPlace (or explicit TryPlaceFree in DEBUG). One attempt per held cell; each successful building is a separate purchase. Failed attempts never charge. Selection persists after a purchase, including when credits become insufficient (red preview).
- **Esc/right-click** cancels placement and closes the menu even over UI, with no purchase/deletion. When idle, left-click selects and right-click/Delete removes non-CPU buildings as before. F1/F2 and existing development controls remain available.

The green/red ghost snaps its top-left footprint anchor to the authoritative grid. It uses a translucent local definition icon and the actual full rectangular footprint; validity includes terrain, reservations, buildings, actor overlap, unlocks and funds. It hides while browsing or over GUI controls. Only an active ghost updates each frame, to reflect moving enemies under a stationary cursor; unchanged ghosts do not redraw. Affordability is cached on relevant events. Cancelled/idle placement disables the level's preview processing. Terrain rendering no longer owns a competing placement outline.

## Files and reference

- `BuildMenu.cs/.tscn`: bottom category/build tray plus adjacent details panel, adapted from `godot-tower-defense/UI/GameplayHUD/TowerBar/TowerBar.tscn` and its HUD arrangement. Buttons come from cached catalogue metadata, with no building-specific switch/buttons.
- `../Shared/BuildMenuStyle.cs`: local compact adaptation of that repo's UIButtonFactory, UIMasterConfig cyan/dark palette, borders, corners and spacing. No external runtime dependency, upgrade inspector or imported TowerBase hierarchy.
- `PlacementPreview.cs`: one world-space icon/footprint renderer under this UI owner, separate from the CanvasLayer panel.
- `LEVELS/SHARED/LevelController.Building.cs`: shared input/selection owner; BM0 BuildingPlacement and ResourceWallet remain the sole purchase/occupancy/balance authorities.

Configure panel layout/minimum sizes in BuildMenu.tscn. Buildings, icons, prices and footprints remain in BuildingDefinition/catalogue; starting credits and allowlist stay in LevelConfig. No additional gameplay tuning resource or wallet was introduced. Restart after authoring definitions. Runtime unlock updates use SetAllowedBuildingIds.

## Brief owner checks

Run Level001: Ctrl → Defense → inspect Wall001/Tower001 → select and place repeatedly. Verify 10/100-credit debits and full green/red footprint, including terrain/building/enemy overlaps and map edges. Exhaust credits and verify disabled tiles/red selected ghost without charges on failure. Remove tower001 from the level allowlist to inspect LOCKED. Ctrl pauses/resumes selection; Esc/RMB cancels without placing/removing. Click UI or use F2 spawning and confirm no world placement leaks. Reset and check fresh credits/subscriptions. Run DEBUG for free building/menu placement and F1/F2 controls. Godot/.NET runtime and visual acceptance remain with the owner; no FPS claims.

Scope stops at BM1: no upgrades, power, heat, ammo, construction, player, camera redesign or fog. The tower icon is the local base-art thumbnail, not a live animated turret preview. Unknown future categories have no fake buildable content.

AIR2: Defense automatically includes Anti-Air Tower / Tower002 through the shared catalogue. Level001 and the free DEBUG allowlist include tower002. Existing prices/details/preview/purchase/cancellation logic is reused with no new hard-coded UI button or shortcut.
