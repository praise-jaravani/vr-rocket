# VR Rocket progress log

One dated entry per milestone (SPEC.md section 11): what works, what was verified and how, and what is still unverified in the headset. Headset checklist items (SPEC.md 12.2) are only ever ticked by a person wearing the headset.

## 2026-10-08: Housekeeping

- SPEC.md v1.0 adopted as source of truth (`Docs/SPEC.md`). Old conventions note reduced to a pointer.
- Kit v2 extracted to `SourceArt/VR_Rocket_Kit_Parts_v2/`; v1 folder removed. Six FBX files in `Assets/VRRocket/Models/` (BodyTube_StandIn deleted; TailFin, NoseCone, MotorCap overwritten keeping their .meta files; BodyTube, WingFlap, Motor added).
- LFS rules added for `.unitypackage` and `.cubemap` ahead of committing the Creepy Cat kit.
- Verified: file layout only. Nothing opened in Unity yet.
