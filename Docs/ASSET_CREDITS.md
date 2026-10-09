# Asset credits

Every third-party asset in the project. Licence texts: CC0 https://creativecommons.org/publicdomain/zero/1.0/ , BSD 2-Clause as noted.

## Rocket

| Asset | Author | Source | Licence | Downloaded | Used for |
|---|---|---|---|---|---|
| Model Rocket Kit (STL) | ellipticaloptician | https://www.printables.com/model/157400-model-rocket-kit | BSD 2-Clause (credit required) | 2026-10-08 | TailFin, NoseCone, MotorCap meshes (cleaned up in Blender, see `SourceArt/VR_Rocket_Kit_Parts_v2/build_kit.py`) |

## Environment (Poly Haven, all CC0)

All downloaded 2026-10-08 from https://polyhaven.com via the public API, 1K FBX with textures. Authors are as listed by the Poly Haven API for each asset.

| Asset | Author | Source | Used for |
|---|---|---|---|
| Steel Frame Shelves 03 | Ulan Cabanilla | https://polyhaven.com/a/steel_frame_shelves_03 | shelving in the back-right corner |
| Worn Metal Rack | Luca B | https://polyhaven.com/a/worn_metal_rack | rack in the back-left corner |
| Metal Tool Chest | Yann Kervran, John Hutcheson | https://polyhaven.com/a/metal_tool_chest | under the workbench |
| Metal Toolbox | Mateusz Sadek | https://polyhaven.com/a/metal_toolbox | on the workbench |
| Bench Vice 01 | Yann Kervran, Antanas Kep | https://polyhaven.com/a/bench_vice_01 | on the workbench |
| Industrial Pipe Lamp | Mateusz Sadek | https://polyhaven.com/a/industrial_pipe_lamp | desk lamp on the workbench |
| Television 01 | Gabriel Radić | https://polyhaven.com/a/Television_01 | blueprint display placeholder |
| Fire Alarm | Slinc | https://polyhaven.com/a/fire_alarm | by the exit door |
| Rollershutter Door | MP | https://polyhaven.com/a/rollershutter_door | hangar door on the right wall (plain variant only) |
| Mounted Fluorescent Lights | Ulan Cabanilla | https://polyhaven.com/a/mounted_fluorescent_lights | fixture over the workbench (one tube variant only) |
| Painted Plaster Wall (texture) | Amal Kumar | https://polyhaven.com/a/painted_plaster_wall | walls, help board |
| Smooth Concrete Floor (texture) | Dimitrios Savva | https://polyhaven.com/a/smooth_concrete_floor | floor |
| Metal Plate (texture) | Rob Tuytel | https://polyhaven.com/a/metal_plate | desk tops, wainscot, window sill, door |
| Rubber Tiles (texture) | Amal Kumar | https://polyhaven.com/a/rubber_tiles | standing mat |
| Qwantani (Pure Sky) HDRI, 2K | Greg Zaal, Jarod Guest | https://polyhaven.com/a/qwantani_puresky | skybox, ambient light, view through the window |

Downloaded but not used (kept in `SourceArt/Environment/polyhaven`, CC0): metal_office_desk, steel_frame_shelves_01, tool_cart, industrial_storage_cart. See Docs/PROGRESS.md for why.

## Already in the project

| Asset | Author | Source | Licence | Used for |
|---|---|---|---|---|
| XR Interaction Toolkit Examples 3.4.0 | Unity Technologies | https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples | Unity Companion License | XR rig, push button prefab (copied to Environment/Prefabs), placeholder audio clips |
| TextMesh Pro (LiberationSans SDF) | Unity Technologies / Red Hat (font) | Unity package | Unity Companion License / SIL OFL (font) | all placeholder text |

## Menu and launch sequence (added 9 Oct 2026)

| Asset | Author | Source | Licence | Downloaded | Used for |
|---|---|---|---|---|---|
| Particle Pack 1.1 | Kenney (kenney.nl) | https://kenney.nl/assets/particle-pack | CC0 | 2026-10-09 | engine flame, smoke, sparks, explosion sprites |
| UI Pack - Sci-Fi (space expansion) | Kenney | https://kenney.nl/assets/ui-pack-sci-fi | CC0 | 2026-10-09 | menu button and panel graphics |
| Sci-Fi Sounds | Kenney | https://kenney.nl/assets/sci-fi-sounds | CC0 | 2026-10-09 | engine loop, ignition, explosions, computer noise |
| Impact Sounds | Kenney | https://kenney.nl/assets/impact-sounds | CC0 | 2026-10-09 | part impacts on the desk and on each other |
| Orbitron (variable) | Matt McInerney | https://github.com/google/fonts/tree/main/ofl/orbitron | SIL Open Font License 1.1 | 2026-10-09 | menu and screen titles |
| Share Tech Mono | Carrois Apostrophe | https://github.com/google/fonts/tree/main/ofl/sharetechmono | SIL Open Font License 1.1 | 2026-10-09 | telemetry and body text |

Originals under `SourceArt/Kenney/` and `SourceArt/Menu/fonts/` with their licence texts. Only the files actually used go into `Assets`. Imported subsets: `Assets/VRRocket/Textures/Particles` (11 sprites), `Assets/VRRocket/Audio/Kenney` (9 clips), `Assets/VRRocket/UI/Sprites` (2 button graphics), `Assets/VRRocket/UI/Fonts` (the two fonts and their TMP font assets).
