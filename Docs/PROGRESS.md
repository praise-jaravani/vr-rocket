# VR Rocket progress log

One dated entry per milestone (SPEC.md section 11): what works, what was verified and how, and what is still unverified in the headset. Headset checklist items (SPEC.md 12.2) are only ever ticked by a person wearing the headset.

## 2026-10-08: Housekeeping

- SPEC.md v1.0 adopted as source of truth (`Docs/SPEC.md`). Old conventions note reduced to a pointer.
- Kit v2 extracted to `SourceArt/VR_Rocket_Kit_Parts_v2/`; v1 folder removed. Six FBX files in `Assets/VRRocket/Models/` (BodyTube_StandIn deleted; TailFin, NoseCone, MotorCap overwritten keeping their .meta files; BodyTube, WingFlap, Motor added).
- LFS rules added for `.unitypackage` and `.cubemap` ahead of committing the Creepy Cat kit.
- Verified: file layout only. Nothing opened in Unity yet.

## 2026-10-08: M0 Import and verify (done)

**What works**
- Six FBX files in `Assets/VRRocket/Models`, import scale 1, no axis conversion needed. Shared URP Lit materials extracted to `Assets/VRRocket/Materials` (Rocket_White, Rocket_Orange, Rocket_Black, Rocket_Cardboard) and remapped on every importer.
- `VRRocket.Runtime` / `VRRocket.Editor` / `VRRocket.Tests.Editor` assemblies. Runtime: PartType, RocketPart, AttachPoint, BuildReport (section 7), AssemblyTuning (section 10.4). Editor: AttachPointGenerator reads `SourceArt/VR_Rocket_Kit_Parts_v2/attach_points.json` (menu VR Rocket > Generate Attach Points On Selected BodyTube).
- Prefabs: BodyTube (with 12 generated attach points), TailFin, WingFlap, NoseCone, Motor, MotorCap. FBX nested as a child named Mesh, RocketPart on the root.
- `Assets/VRRocket/Scenes/Dev_Interactions.unity` with a static assembled rocket (M0_StaticAssembledRocket), added to the build list.
- Interaction layer `RocketPart` added at index 1 of `Assets/XRI/Settings/Resources/InteractionLayerSettings.asset`.

**Verified, and how**
- Mesh bounds read in the Editor match SPEC.md 4.3 to 0.1 mm for all six parts; triangle counts match 4.2 (2100 / 425 / 28 / 2702 / 768 / 3000).
- Mirror and rotation check from vertex data: flap outer edge Y range -31..+9 mm (swept edge up); tube slot vertex clusters at yaw 0/120/240 (fins, mid flaps) and 60/180/300 (low flaps); thrust ring at Y=59; motor black submesh at Y 0..10; nose orange submesh at Y 37.4..49.4; cap lugs within r=15.2 mm.
- Edit-mode tests (6/6 green): generated transforms match the JSON, +Z points outward, generator is idempotent; all four outcome rows of 7.2 plus mixed failedComponents.
- Game-view render of the assembled rocket matches `preview.png`. NoseSeat at world y+0.300, CapSeat at +0.000.

**Not verified in the headset**: nothing to verify yet (no interaction).

**Notes**
- XRI 3.4 API confirmed from package source: `XRBaseGrabTransformer.Process(XRGrabInteractable, UpdatePhase, ref Pose, ref Vector3)`; haptics via `XRBaseInputInteractor.SendHapticImpulse(float amplitude, float duration)` (wraps HapticImpulsePlayer). The rig's hands are `NearFarInteractor` (sphere caster near, curve caster far) so a single interaction layer cannot block far grabs without blocking near ones; near-only will be a select/hover filter on the part.
- Creepy Cat kit: all 39 materials use the built-in Standard shader and need the URP converter before the dev room is built. Its Example_01 scene has 257 lights.

## 2026-10-08: M1 Parts, tray, grab, respawn (done)

**What works**
- Part prefabs now carry physics and grab: box colliders padded to 8 mm on fins and flaps, capsules on nose cone, motor and tube, convex mesh on the cap; dynamic rigidbodies with gravity, continuous collision, interpolation. `XRGrabInteractable` (Kinematic movement, dynamic attach, snap to collider volume, throw on detach), interaction layers Default|RocketPart, and `NearOnlyGrabFilter`.
- `Assets/VRRocket/Prefabs/RocketWorkstation.prefab`: root on the desk surface, `ScaledRoot` (scaled by `AssemblyTuning.rocketScale`), greybox `AssemblyStand` holding the tube kinematic with its bottom rim 0.20 m up, `PartsTray` with 3 fins, 3 flaps, nose cone, motor, cap laid out in groups, `RespawnPad`, `WorkstationBounds` trigger, `PartRespawner`.
- `Assets/VRRocket/Settings/AssemblyTuning.asset` with the section 10.4 starting values.
- Dev scene: greybox room (floor, workstation desk, mission control desk), `Complete XR Origin Set Up Variant` with continuous move, teleportation provider and both teleport interactors disabled, workstation on the back desk. The M0 static rocket is kept as `Prefabs/AssembledRocket_Static.prefab` for the launch-animation owner.
- XR Interaction Simulator set to auto-instantiate in the Editor only (`Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset`), so Play mode works on the laptop and the Quest build is unaffected.

**Verified, and how (Play mode, driven through the Editor link)**
- All nine parts start at rest on the desk, bodies dynamic, one collider each, one select filter and one hover filter registered. Tube clamped kinematic at world y 0.95 with no grab interactable.
- Respawn: a fin moved below the floor and a flap and nose cone parked out of bounds all reappeared on the pad within respawnDelay, upright in their tray orientation, zero velocity, collider resting on the desk, bodies dynamic.
- Near-only: `XRInteractionManager.IsSelectPossible/IsHoverPossible` for the right NearFarInteractor against a fin: false at 0.58 m and 0.20 m, true at 0.10 m and 0.03 m. A forced SelectEnter then SelectExit toggles the body kinematic and back. An existing grab is not cancelled when the hand moves far.
- Teleport ray inactive and cannot select parts.

**Not verified in the headset** (SPEC 12.2 items 1 and 10 remain for Praise): real controller grabs anywhere on a surface with either hand, throw and drop feel, respawn timing.

**Deviations and notes**
- SPEC 10.3 suggests a `RocketPart` interaction layer to stop far and ray interactors. The rig's hands are single `NearFarInteractor`s whose near and far casters share one layer mask, so a layer cannot separate near from far. The layer exists and parts carry it, but near-only is enforced by `NearOnlyGrabFilter` measuring from the interactor's own transform (its attach transform sits at the far-cast hit point and is useless for this). Parts also keep the Default layer so the prefab works in any scene without rig edits.
- Dev room is greybox primitives, not the Creepy Cat kit: all 39 kit materials use the built-in Standard shader and render magenta in URP until the Render Pipeline Converter is run, which edits the read-only kit folder and is Praise's call. The kit's demo scene has 257 lights.

## 2026-10-08: M2 Guide mechanic on the nose cone (done)

**What works**
- `GuideGrabTransformer` (XRBaseGrabTransformer, registered after XRI's XRGeneralGrabTransformer so it runs last): approach glow on every accepting point within glowRadius (brightness by closeness), capture inside captureRadius when within orientation tolerance, rotation locked to the seated rotation, position projected onto the attach axis and clamped to guideLength, nose magnetism (displayed depth = hand depth x noseMagnetism), break away on sideways drift past breakRadius or past the end of the guide, seat on release (animated over seatDuration, then parented under the attach point, kinematic, collisions with the rocket ignored), re-grab of an attached part re-enters the guide at depth 0, pull past guideLength detaches and returns the part to its tray group.
- `RocketGrabInteractable`: XRGrabInteractable plus a pre-grab event, needed so an attached (kinematic) part is dynamic again when XRI records its state; otherwise XRI restores kinematic on release and refuses to throw.
- `AttachPoint` now owns its glow (unlit emissive ring mesh, MaterialPropertyBlock, per-frame max request) and a static registry of active points.
- Feedback: `AssemblyEvents` static hub, `FeedbackLibrary` asset (clips per event, haptic amplitude and duration per SPEC 6 table), `AssemblyFeedback` on the workstation root (pooled 3D AudioSources, haptics through `XRBaseInputInteractor.SendHapticImpulse` on the holding hand). Placeholder clips: HoverSound (engage tick), Button_22_Click (seat click), Button_14_Hover (nose soft click, cap seat, unseat), Button Pop (impacts, respawn).
- Only the nose cone carries the transformer so far; the other parts stay plain grabbables until their milestones.

**Verified, and how**
- Edit-mode tests 13/13 green (GuideMath: depth/sideways decomposition, clamp and magnetism, break rule, orientation tolerance, seated rotation for nose/fin/flap and flap orientation readout).
- Play-mode test `NoseGuideTests.NoseCone_ApproachCaptureSlideBreakSeatAndRemove` green (6.4 s): loads Dev_Interactions, takes over the right controller transform, forces the grab through XRInteractionManager and asserts every stage of 5.2 including events, parenting, kinematic state, collision ignore, glow on/off and that the nose seat clip was queued on a 3D source. Run it with Test Runner > PlayMode or `unity command run_tests --mode playmode --filter NoseGuide --async_tests true`.
- Console clean of errors and of warnings from VRRocket code during the runs.

**Not verified in the headset** (SPEC 12.2 items 2 and 3 for the nose, and all haptics): the feel of capture and break-away radii, the vibration values, whether the glow ring reads well on Quest.

**Deviations and notes**
- `noseMagnetism` is implemented literally as the displayed fraction of the hand's distance (0.5 shows half). If "pull strength" was meant, invert it in `GuideGrabTransformer.Process`.
- XRGrabInteractable unparents a held object and restores the grab-time parent on drop; the transformer re-parents a pulled-off part to its tray group after the drop.
- Part-side glow (the "should" in SPEC 6) not done yet; planned for M8 polish.
- Teleporting a controller through the tray in tests sweeps parts off the desk via the rig's Pusher bodies; harmless (they respawn) but tests park the hand away from the tray first.

## 2026-10-08: Decisions applied after M2

- Creepy Cat kit and both `.unitypackage` archives deleted; the dev room stays greybox. SPEC.md 3.2 and 10.1 and CLAUDE.md updated; no art kit is a dependency.
- Near-only distance filter accepted; SPEC.md 10.3 rewritten to describe it. It already blocks hover as well as select, so a far-pointed part shows no highlight, ray cursor or glow (verified: IsHoverPossible false at 0.20 m and 0.58 m).
- noseMagnetism confirmed in the literal sense. New requirement in SPEC.md 5.3: the pull fades in along the guide (exact follow at the outer end, full value at the seat) and engaging or breaking away never jumps. Implemented as `fraction = Lerp(noseMagnetism, 1, depth / guideLength)` plus a `guideBlendDuration` (0.1 s, tunable) ease between free and guided poses on engage, release and detach.
- `Dev_Interactions` is now first in the build scene list.

## 2026-10-08: M3 Motor cap twist (done)

**What works**
- `IAttachedGrabHandler`: a part can take over what happens when it is grabbed while attached, and offset where it rests when seated. `GuideGrabTransformer` delegates to it; everything else (guide to the seat, seat animation, pull-off detach) is shared with the other parts.
- `MotorCapTwist` on the MotorCap prefab (SPEC 5.4): seats unlocked `capUnlockedGap` proud of the tube with the soft seat sound and 0.2/0.04 haptic; gripping the seated cap does not move it; the cap turns about the tube axis following the hand, clockwise only viewed from below, as a ratchet (turning back does nothing, progress kept between grabs); one tick and 0.2/0.02 haptic per `capDetentAngle` of new progress; at `capLockAngle` it stops, plays the firm click, fires 1.0/0.20 and `capLocked` becomes true; the gap closes over 0.1 s; a locked cap cannot be turned or removed; letting go early is silent; an unlocked cap pulled more than `capPullOff` along the axis comes off and its progress resets.
- Twist reading borrows the XRKnob technique: the hand's position orbiting the axis when it grips more than 3 cm off-axis, otherwise its wrist roll (forward or up vector, whichever lies flattest), never both, with wrap-safe per-frame deltas.
- `CapTwistLogic` is plain C# (progress, detents, lock, reset) so SPEC 12.1's cap rule is unit-tested.
- Glow rings on CapSeat and MotorSeat at the bottom rim, sharing the nose ring mesh.
- New events `CapDetent` and `CapLocked` wired into `AssemblyFeedback` with the placeholder ratchet tick (HoverSound) and firm click (Button_22_Click).

**Verified, and how**
- Edit-mode 20/20 green. CapTwistLogic: accumulates across grabs, never decreases, detent ticks exactly on each 30 degree boundary of new progress, locks exactly at 180 without overshoot even from summed float deltas, lock reported once, nothing changes after lock, reset on pull-off.
- Play-mode 4/4 green (26 s), via `unity command run_tests --mode playmode --filter VRRocket.Tests --async_tests true`:
  - `CapTwistTests.Cap_SeatsUnlockedWithGap_RatchetsClockwise_LocksAtLockAngle`: 2 mm gap after seating, gripping leaves it, 100 degrees of clockwise orbit gives progress 100 and 3 ticks with the cap turned to match, 50 back changes nothing, forward again locks at exactly 180 with 5 ticks total and one lock, gap 0 afterwards, further turning and a 8 cm pull do nothing, release leaves it seated kinematic under CapSeat.
  - `CapTwistTests.Cap_EarlyLetGoKeepsSeat_ProgressKeptBetweenGrabs_PullOffResets`: early let-go keeps the seat and the gap with no events; progress continues across grabs; a 6 cm pull detaches, resets to 0, the cap follows the hand and drops dynamic into its tray group; re-seating starts from zero with the gap.
  - `NoseGuideTests` (2): the M2 walk-through now also checks the faded magnetism at three depths and that engage and break-away ease over the blend instead of jumping.
- Console clean of VRRocket errors and warnings during the runs. One teardown bug found and fixed on the way: XRI releases a held object from OnDisable during scene unload, and re-parenting there threw.

**Not verified in the headset**: the twist direction (clockwise viewed from below is implemented as negative Unity yaw about the tube's +Y; `MotorCapTwist` has a flip toggle if it feels reversed), the 3 cm position/wrist mode radius, how the ratchet reads with real wrist motion, whether the 2 mm gap is visible on Quest, all haptic values.

**Notes**
- Gating is not implemented yet (M6), so the cap can be seated on a bare tube, as M3 requires.
- Dynamic attach snaps to the collider surface, so the hand-to-origin offset of a held cap depends on where it was grabbed.

## 2026-10-08: Environment v1, control room and workstation (done)

Separate from the rocket milestones. Rocket prefabs and scripts untouched; `Dev_Interactions.unity` untouched.

**What was built**
- `Assets/VRRocket/Environment/Prefabs/ControlRoom.prefab` and `Assets/VRRocket/Scenes/Env_ControlRoom.unity` (second in the build list). Room per SPEC 3.1: workbench with the RocketWorkstation at the back, mission control console with bin, Submit, Launch, Abort and puzzle placeholders at the front, 2.4 m emissive launch screen on the front wall with a countdown clock and mission panel beside it, window with an outside view and an exit door on the left, help board and roller shutter door on the right, LED ceiling panels and cove strips, rubber standing mat. Both work surfaces at 0.95 m, checked against the workstation's stand and tray. No chairs. Seven anchors (PlayerSpawn, WorkstationAnchor, MissionDeskAnchor, BinAnchor, LaunchScreenAnchor, HelpWallAnchor, BlueprintAnchor). Full description in `Docs/ENVIRONMENT.md`, credits in `Docs/ASSET_CREDITS.md`, screenshots in `Docs/Screenshots/`.
- Extra: a concrete apron, launch pad, gantry and a full-size (36x) static copy of the assembled rocket 30 m outside the window, so the user sees what they are building.

**Style decision**: photoreal Poly Haven props on primitive-built architecture dressed with Poly Haven PBR surfaces. The rocket parts are photoreal and the mood is "real mission control plus clean workshop", so stylised kits would clash; and primitives keep the architecture cheap and trivially editable. The stylised kits (Quaternius Modular Sci-Fi MegaKit and Essentials, Kenney Space and Furniture kits) could not be fetched without a click-through on itch.io / kenney.nl, so no side-by-side scratch scene was made; if you want that comparison, drop their zips in `specs/` and I will stage it.

**Assets considered**
- Taken: steel_frame_shelves_03, worn_metal_rack, metal_tool_chest, metal_toolbox, bench_vice_01, industrial_pipe_lamp, Television_01 (as the blueprint CRT), fire_alarm, rollershutter_door (plain variant), mounted_fluorescent_lights (one tube variant, 2.5k tris instead of 17.8k); textures painted_plaster_wall, smooth_concrete_floor (albedo dropped for a neutral grey epoxy look, normal and roughness kept), metal_plate, rubber_tiles; HDRI qwantani_puresky 2K.
- Rejected: metal_office_desk (0.79 m sitting height, raising or scaling it looks wrong, so both desks are built from primitives at 0.95 m), steel_frame_shelves_01 (redundant with 03), tool_cart (29k tris for a background prop), industrial_storage_cart (19k tris, does not fit a clean room). Poly Haven's 14k-tri toolbox and 13k-tri tool chest were kept because they sit at the workbench where the user looks closely.
- Not found as free CC0/CC BY within the bounded search: a proper console desk with screens, a window frame, a door, a countdown clock. All built from primitives plus TMP text instead; they are the obvious things for the teammates to replace.
- Unity Asset Store items were not used (they need your account); none was needed for v1.

**Pipeline**: a Poly Haven material builder (scratch editor script, kept in `SourceArt/Environment/tools/`) converts the EXR roughness/metal maps into URP metallic-smoothness PNGs, converts normal EXRs to 8-bit PNG and remaps each FBX importer. Props import with file scale off and global scale 0.01 (Poly Haven FBX vertices are in centimetres).

**Measured at commit** (room hierarchy only, Editor figures)
- Triangles: 75,418 for the room including the outside pad and full-size rocket (about 10k of that is the big rocket); the workstation adds 12,485; XR rig controllers add a few thousand.
- Materials: 38 on room renderers, of which 5 are the rocket's (outside copy), 4 the XRI button's and 3 puzzle-wire dots; 26 are the room's own.
- Textures: 52, all 1K except the 2K HDRI cubemap at 1024. Editor memory 125 MB (uncompressed in the Editor); estimated 33 MB on Quest at ASTC 6x6. One 1024 lightmap, 2.7 MB.
- Added to the repo: about 58 MB under Assets/VRRocket/Environment, 82 MB of originals under SourceArt/Environment, 5 MB of scene and lightmaps. Total about 145 MB.
- Lighting baked (Progressive CPU, 10 texels/unit). One real-time directional fill at 0.35 with no shadows; everything else baked or emissive. Light probes and a baked reflection probe cover the dynamic parts.
- Headset frame rate not measured here; that is Praise's check on the Quest.

**Known rough edges for v1.1**: the CRT's screen face orientation has not been confirmed in a close-up; the puzzle panel and side screens are flat placeholders; the roller door and rack are a little rusty for the palette; the bin has no lid or animation (M7 and the teammates own that).
