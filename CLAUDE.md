# VR Rocket

UCT VR course assignment, team of three. The user hand-assembles a model rocket (body tube, 3 tail fins, 3 wing flaps, motor, motor cap, nose cone), submits it, and watches it launch. Wrong flap placement or an unlocked motor cap makes the launch fail.

- Target: Meta Quest 3 standalone (Android, OpenXR). No PCVR. Keep meshes light.
- Engine: Unity 6000.3 LTS, XR Interaction Toolkit 3.4, URP. Everyone uses the identical Editor version listed in `ProjectSettings/ProjectVersion.txt`.
- Layout: our work lives in `Assets/VRRocket`. `Assets/XRI_Examples` and `Assets/Samples` are read-only reference: copy from them, never edit them. `SourceArt` holds the Blender sources and stays outside `Assets` so Unity never imports the .blend file.
- Asset conventions (units, axes, origins, measured sizes) are in `Docs/rocket-asset-conventions.md`. Read it before touching rocket parts or sockets.
- Scenes merge badly: one person edits a given scene at a time. Prefer prefabs and separate scenes per person.
- Binary assets go through Git LFS (see `.gitattributes`). Never commit `Library`, `Temp`, `Logs`, `UserSettings` or `Builds`.
- Every attachment needs visual, audio and controller vibration feedback. That is a marking requirement.
