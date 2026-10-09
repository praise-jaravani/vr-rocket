# Integrating the rocket (M9)

For Yi-Xuan and Richard. How to drop the rocket into your scene, what it tells you, and what to delete when your systems arrive. Source of truth for behaviour is `Docs/SPEC.md`.

## 1. Drop in the prefab

1. Place `Assets/VRRocket/Prefabs/RocketWorkstation.prefab` with its root **on the workstation desk surface** (its origin is the desk top). It needs about 1.6 by 0.9 m of clear desk around the origin: the parts tray is to the left (-x), the stand and respawn pad to the right (+x). Desk height around 0.95 m.
2. Your scene needs an XR rig with the XRI `NearFarInteractor` hands. `Assets/XRI_Examples/Global/Prefabs/Complete XR Origin Set Up Variant.prefab` works as is. Rocket parts are near-grab only. One recommended change on the rig instance: untick Play Hover Entered and Play Select Entered on the `SimpleHapticFeedback` components of the Near-Far and Poke interactors (both hands), otherwise the rig's generic buzz plays on top of the rocket's own designed haptics (SPEC 6). Both shipped scenes already do this.
3. Nothing in `Assets/VRRocket/Scripts` references the environment. The rocket does not care what room it is in.
4. `Env_ControlRoom.unity` shows a working placement: the prefab sits on `ControlRoom/Anchors/WorkstationAnchor`.

The whole rocket is scaled by `rocketScale` in `Assets/VRRocket/Settings/AssemblyTuning.asset` (1.0 = real size, 34 mm parts). Every other tunable of SPEC 10.4 is in that asset; every sound and vibration value is in `Settings/FeedbackLibrary.asset`. Swap your final clips in there, no code changes needed.

## 2. The interface: `RocketAssembly` on the prefab root

```csharp
public enum AssemblyState { BuildingAirframe, AirframeComplete, MotorFitted, PrototypeComplete, Submitted }
public enum ReturnMode { Editable, InspectOnly }

AssemblyState State { get; }
event Action<AssemblyState> StateChanged;      // also UnityEvent onStateChanged
event Action PartAttached;                     // also onPartAttached
event Action PartRemoved;                      // also onPartRemoved
event Action<BuildReport> Submitted;           // also onSubmitted
                                               // plus onPrototypeComplete (stand opens)
BuildReport GetReport();                       // valid from PrototypeComplete onward
bool TrySubmit();                              // the bin calls this: freezes the rocket, raises Submitted
void ReturnPrototype(ReturnMode mode, Transform at);
void BeginNewBuild();                          // fresh tube in the stand, fresh parts on the tray
void SetRocketVisible(bool visible);           // helper for your bin animation
RocketPart tube;                               // the working rocket's body tube (carries all attached parts as children)
RocketPart inspectPrototype;                   // the frozen rocket returned for inspection, if any
```

`BuildReport` (SPEC section 7): `flapPlacement[3]` with slot name, `Base`/`Midpoint` and `Up`/`Down`, `capLocked`, `FlapError`, `outcome` (`Success`, `MotorRetentionLoss`, `UnstableFlight`), `failedComponents` such as `{ "MotorCap", "WingFlap_2" }`, `FailedCount`.

## 3. Expected flow

- **Assembly.** The user builds in the stand. At `PrototypeComplete` the stand releases (you get `onPrototypeComplete`); the user lifts the rocket out and carries it as one object. Parts cannot be pulled off while it is out of the stand; the cap can still be twisted.
- **Submit.** Detect the loose complete rocket in your bin (its root has `RocketPart` with `partType == BodyTube`; `assembly.State == PrototypeComplete`). On your Submit button call `TrySubmit()`. It returns false if the rocket is still held or not complete. Then run your bin animation; `SetRocketVisible(false)` hides it when the animation needs that.
- **Launch.** Read `outcome` from the report in `Submitted` and play the matching animation on the screen. `failedComponents` feeds the failure screen.
- **Failed launch.** `ReturnPrototype(InspectOnly, yourBinReturnAnchor)` then `BeginNewBuild()`. The old rocket comes back as a frozen object the user can pick up and turn over but not alter; a new kit appears. Only one inspection prototype exists at a time.
- **Abort.** `ReturnPrototype(Editable, null)`. The same rocket goes back into the stand and can be corrected.
- A dropped loose part respawns on the pad; a dropped rocket returns to the stand; a dropped inspection prototype returns to its anchor.

## 4. Stubs to delete

`Assets/VRRocket/Prefabs/Stubs/MissionControlStubs.prefab` is placed in `Dev_Interactions.unity` only (the working flow of section 6 replaced it in `Env_ControlRoom.unity`), named `MissionControlStubs [STUB, delete when real systems arrive]`. It holds:

- `SubmissionZoneStub`: a trigger box plus a Submit button that calls `TrySubmit()` and hides the rocket. Your bin replaces it.
- `OutcomeReadoutStub`: a panel printing the report, with "Failed launch" and "Abort" buttons that call the two sequences above. Your launch screen and buttons replace it.
- `StubButton.prefab` is a copy of the XRI example push button; `PushButton.onPress` is wired in the Inspector, which is also how you can wire your own buttons without writing code.

Delete the stub object from your scene and the prefab folder when your systems are in.

## 5. Things to know

- All parts, the tube and the stand share one guided-attach mechanic (`GuideGrabTransformer`). The attach points are generated from `SourceArt/VR_Rocket_Kit_Parts_v2/attach_points.json` by the menu item VR Rocket > Generate Attach Points On Selected BodyTube; do not hand-edit them.
- `Assets/VRRocket/Prefabs/AssembledRocket_Static.prefab` is a plain assembled rocket (no physics or scripts) for your launch animation. `Prefabs/Launch/LaunchVehicle.prefab` is a 36x copy of it stripped to meshes, with the engine flame and sparks at the nozzle, used by the launch sequence below.
- Tests: Window > General > Test Runner. EditMode covers the outcome rule, state machine, gating, cap ratchet and attach transforms; PlayMode drives every interaction in `Dev_Interactions.unity`. Run them after changing anything under `Assets/VRRocket`.
- Headset-only checks (feel, haptics, frame rate) are listed in SPEC 12.2 and are Praise's.

## 6. Working version of the menu, flow and launch (yours to replace)

`Env_ControlRoom.unity` runs the whole loop today so the rocket can be played end to end. It lives in two scene roots that are not part of the room prefab or the rocket prefab, so deleting them removes it cleanly:

- `GameFlow`: `GameFlowController` (Menu, Assembly, PreLaunch, Countdown, Launch, Result), `LaunchScreenController` (drives the big screen's texts, swaps its material to the pad camera feed, paints the `Diagram` quads red for failed components), `BlueprintDisplay` (step text on the CRT), `SubmissionBin` (trigger over the console opening; `Submit()` calls `TrySubmit()` and sinks the rocket; `returnSpot` is where a failed prototype comes back), `EventSystem` and `MenuCanvas` (world-space UGUI with `TrackedDeviceGraphicRaycaster`; START calls `GameFlowController.StartGame`).
- `LaunchSite`: `LaunchSequence` with the 36x vehicle, effects, pad camera and audio; `Liftoff(outcome, onFinished)` plays one of the three outcomes from `RocketFlightModel`.

Entry points, all wired to the console on the room instance: `SubmissionBin.Submit`, `GameFlowController.Launch`, `GameFlowController.Abort`. `GameFlowController.Configure(...)` and the other `Configure` methods take every reference, so the objects can be rebuilt by script; the Inspector works too.

To replace a piece: keep the `RocketAssembly` interface of section 2 and the button names on the console. Your bin can keep calling `TrySubmit()` and the two return sequences; your launch screen can read `GameFlowController.lastReport` or subscribe to `RocketAssembly.Submitted`. The wire puzzle of SPEC 9 is not built; `Launch()` is accepted as soon as the vehicle is on the pad. The flow locks the parts outside the Assembly phase through `RocketAssembly.interactionEnabled`; if you drop the flow, leave that true (its default).
