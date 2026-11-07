# C1 Citadel construction drone service

**Source Implemented / Awaiting Owner Validation.** LevelBase and DEBUG each compose one dispatcher and one construction clock. A committed site joins a FIFO queue. A builder launches from the Citadel's footprint center and follows a curved direct flight using a retained distance table, acceleration, braking and smooth turning. At arrival it hovers at a fixed offset, deploys a cyan beam, waits for deployment, then releases the site to the shared work clock. Completion or cancellation switches the beam off and sends the builder home. Parked instances are pooled. A returning drone occupies its slot until it parks.

The Inspector resource DefaultConstructionDroneConfig sets service and visuals enabled, four simultaneous drones and two dispatches per physics frame. Travel speed is 280 px/s with 800 px/s² acceleration and deceleration, 240°/s turn speed, 2 px arrival tolerance, a (0, −38) px hover offset clamped within world bounds, and 0.25 seconds of beam deployment. Beam color alpha controls opacity. Author settings before running a level.

With RequireDroneToBuild off, disabling the service or losing the Citadel lets held jobs start locally under the same per-frame budget. A deployed builder already constructing finishes its job and parks if its home is lost. Strict mode holds jobs awaiting deployment while the source is unavailable. Removing a site cancels its queue or assignment and stops its work clock. The dispatcher sleeps when its flights and deployment timers are idle.

The drone and beam use FogPresentation's existing dynamic material. A distant site can wait for flight and return slots on the larger map. Level reload resets all construction state.

The 36×28 drone SVG is copied from `ben0kay/godot-tower-defense` on `dev`, at `TOWERS/ConstructionDrones/Visuals/ConstructionDrone.svg` (blob `484d0d7c6a2a55c08c5b84d6976327edeb3ec84f`). Preserve centered registration and an upward-facing nose in replacement artwork. The beam adapts the reference's tapered polygon to the fixed hover pose.
