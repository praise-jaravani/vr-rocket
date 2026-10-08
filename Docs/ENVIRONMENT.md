# Environment v1: control room and workstation

First version of the room from SPEC.md section 3.1, built 8 Oct 2026 from free assets so the rocket can be tested in context. It is self-contained and meant to be replaced by the teammates' final room.

## Where things are

```
Assets/VRRocket/Environment/
  Models/        Poly Haven FBX props (1K), file scale off, global scale 0.01
  Textures/      one folder per asset: albedo (jpg), normal (png), metalsmooth (png, R = metallic, A = smoothness), ao
                 LaunchScreen_Placeholder.png, Skybox/qwantani_puresky_2k.hdr (imported as a cubemap)
  Materials/     Poly Haven prop materials (named after the asset), Surface_* tiling materials, Mat_* plain materials, Mat_Skybox
  Prefabs/       ControlRoom.prefab, PushButton.prefab (copy of the XRI example button)
  ControlRoomLighting.lighting
Assets/VRRocket/Scenes/Env_ControlRoom.unity     the scene: ControlRoom + XR rig at PlayerSpawn + RocketWorkstation at WorkstationAnchor
SourceArt/Environment/                            original downloads (polyhaven, polyhaven_textures, polyhaven_hdri)
Docs/ASSET_CREDITS.md                             every asset with author, source and licence
```

The room is a prefab built from Unity primitives (walls, desks, window, door, screen, panels) dressed with Poly Haven PBR surfaces and ten Poly Haven props. All placeholder text is TextMeshPro.

## Layout (metres, scene origin on the floor, +Z toward the workstation)

- Room 7.2 wide (x) by 5.2 deep (z) by 3.0 high. The user stands on the rubber mat at the origin, facing +Z.
- **Workbench** (back, z 1.1 to 1.9): 3.0 m steel-framed bench, top at 0.95. Carries the RocketWorkstation, a toolbox, a vice, a desk lamp; tool chest below; shelving in the back-right corner, rack in the back-left; CRT on a wall bracket as the blueprint display; a fluorescent fixture above.
- **Mission control console** (front, z -1.85 to -1.05): 3.4 m console, top at 0.95, with a raised 20 degree panel along its back edge holding the Submit button, two small telemetry screens, the wire-match puzzle placeholder, and the green Launch and red Abort buttons. The submission bin is a 0.46 m square opening in the console top at x -1.05 with a dark shaft below it and a green rim.
- **Launch screen** (front wall): 2.4 by 1.35 m emissive display with a placeholder pad graphic, title and countdown text, a row of status LEDs beneath, a countdown clock panel to its left and a mission panel to its right.
- **Left wall**: 3.2 m window (sill 1.1, head 2.3) looking out on a concrete apron with a launch pad, gantry and a full-size (36x) static copy of the assembled rocket 30 m away; exit door with an EXIT sign at the front corner; fire alarm.
- **Right wall**: help board (build guide text) at the front half, roller shutter door at the back half.
- **Ceiling**: six emissive LED panels (each with a baked rectangular area light) and cove strips along the front and back walls.

## Anchors (empty transforms under ControlRoom/Anchors)

| Anchor | Position | Facing | Meaning |
|---|---|---|---|
| PlayerSpawn | (0, 0, -0.2) | +Z | put the XR rig here |
| WorkstationAnchor | (-0.3, 0.95, 1.45) | +Z | RocketWorkstation prefab root (its origin sits on the desk surface) |
| MissionDeskAnchor | (0, 0.95, -1.45) | -Z | mission control console surface |
| BinAnchor | (-1.05, 0.95, -1.45) | -Z | centre of the submission opening, at desk height |
| LaunchScreenAnchor | (0, 1.95, -2.55) | +Z | centre of the launch screen surface |
| HelpWallAnchor | (3.55, 1.65, -0.3) | -X | centre of the help board |
| BlueprintAnchor | (1.0, 1.55, 2.3) | -Z | blueprint display |

`PlayerSpawn.forward` points at the workbench, so the user spawns facing the workstation as the treatment asks.

## Lighting and budget

- Everything that does not move is static. Lighting is baked with the Progressive CPU lightmapper (10 texels per unit, one 1024 lightmap, AO on, non-directional). Baked lights: one directional sun through the window, six rectangular area lights at the ceiling panels, emissive panels and strips. One real-time directional fill at 0.35 intensity with no shadows lights the dynamic rocket parts, together with a 5 x 3 x 5 light probe grid and one baked reflection probe.
- Rebake after moving anything: Window > Rendering > Lighting > Generate Lighting (settings asset `ControlRoomLighting`).
- Materials are URP Lit only. Textures are 1K. See Docs/PROGRESS.md for the triangle, material and memory figures measured at commit time.

## Replacing the room

1. Build the final room as its own prefab with the same seven anchors (names and facing as above). Nothing in the rocket code references the environment; only the scene does.
2. In a new or the existing scene: drop the new room prefab, place the XR rig at PlayerSpawn, place `Assets/VRRocket/Prefabs/RocketWorkstation.prefab` at WorkstationAnchor (its origin must sit on a 0.95 m desk surface with about 1.6 by 0.9 m of clear top around it). The bin, Submit, Launch and Abort placeholders will be replaced by the real systems in M7 and by the teammates' work; keep their anchors.
3. Delete `Assets/VRRocket/Environment` and the entries in Docs/ASSET_CREDITS.md that are no longer used. `Dev_Interactions.unity` does not depend on any of it.
