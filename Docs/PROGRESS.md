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
