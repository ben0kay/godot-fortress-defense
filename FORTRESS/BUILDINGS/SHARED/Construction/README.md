# C1 initial building construction

**Source Implemented / Awaiting Owner Validation.** Tower001 Level 1 opts in through its Construction settings. A positive duration initializes the building at AwaitingDrone before it enters the scene. Occupancy and HP belong to the normal placed building throughout its lifetime.

BuildingPlacement publishes ConstructionRequested after a successful paid or explicitly free claim. ConstructionDroneManager queues the site and releases it to ConstructionWorkManager after beam deployment. The work manager advances only active jobs in unpaused physics time. Progress notifications run at roughly 10 Hz with extra notifications at artwork thresholds. Completion changes the same building to Operational, notifies the existing fog provider and wakes staggered weapon acquisition. Detachment cancels construction immediately.

The optional BuildingConstructionVisuals sprite uses authored BuildingConstructionStage resources. The highest matching progress threshold supplies its texture. Tower001 uses 0%, 35% and 70%, with its original base and gun taking over at completion. The presenter listens to events and swaps a cached texture only when the stage changes.

In Level001, select a site to see its work state in the existing battlefield HUD. Exercise cancellation during travel and work, pause during the beam, and verify HP persists through completion. The shared clock and drone dispatcher are level-scoped and are recreated on reload or regeneration.
