# VR Rocket

UCT VR course assignment, team of three. The user hand-assembles a model rocket (body tube, 3 tail fins, 3 wing flaps, motor, motor cap, nose cone), submits it, and watches it launch. Wrong flap placement or an unlocked motor cap makes the launch fail.

- **Source of truth: `Docs/SPEC.md`.** Read it before any work. Where it disagrees with this file, the treatment, or older notes, SPEC.md wins. Ownership is in SPEC.md section 0: Praise owns the rocket model and every assembly interaction; the room, puzzle, launch screen and final audio belong to teammates, and only the stubs in section 8.5 are built here.
- Target: Meta Quest 3 standalone (Android, OpenXR). No PCVR. Keep meshes light.
- Engine: Unity **6000.3.11f1** (pinned in `ProjectSettings/ProjectVersion.txt`, everyone installs this exact version), XR Interaction Toolkit 3.4.0, URP.
- Layout: our work lives in `Assets/VRRocket` (see SPEC.md section 10.1). Read-only reference folders, copy from them and never edit them: `Assets/XRI_Examples`, `Assets/Samples`, and the Creepy Cat kit in `Assets/Creepy_Cat`. `SourceArt` holds the Blender sources and stays outside `Assets` so Unity never imports the .blend file.
- Progress log: `Docs/PROGRESS.md`, one dated entry per milestone.
- Scenes merge badly: one person edits a given scene at a time. Prefer prefabs and separate scenes per person.
- Binary assets go through Git LFS (see `.gitattributes`). Before committing an imported pack, check every binary type it contains has an LFS rule. Never commit `Library`, `Temp`, `Logs`, `UserSettings` or `Builds`.
- Every attachment needs visual, audio and controller vibration feedback, per the table in SPEC.md section 6. That is a marking requirement.
