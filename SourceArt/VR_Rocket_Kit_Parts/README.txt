VR ROCKET - KIT PARTS, FIRST PASS (8 Oct 2026)

Source: "Model Rocket Kit" by ellipticaloptician, printables.com/model/157400-model-rocket-kit
Licence: BSD 2-Clause. Credit the author wherever these meshes are redistributed.

FILES
  TailFin.fbx           425 tris   one fin cut from base_swept.STL (the kit has five; use this mesh three times)
  NoseCone.fbx         2500 tris   nosecone_snub.STL, outer skin only (was 211k)
  MotorCap.fbx         3000 tris   24mm_motor_cap.STL (was 54k)
  BodyTube_StandIn.fbx  384 tris   plain 33.7 x 300 mm tube, NO slots, placeholder only
  rocket_kit_parts.blend           all of the above assembled (saved from Blender 5.2)
  build_kit.py                     the script that produced everything, rerun with
                                   blender --background --python build_kit.py -- <kit_folder> <output_folder>

CONVENTIONS (same for every part)
  Units          metres, real size (import into Unity at scale factor 1)
  Up             Unity +Y is the rocket axis, pointing at the nose
  Fin outward    Unity +Z points away from the tube, so the slide-in axis is local Z

ORIGINS (put the socket's attach transform here)
  TailFin    on the tube surface, at the middle of the root edge. A 2 mm tab continues into the tube.
  NoseCone   centre of the step where the shoulder meets the base, i.e. the top rim of the tube.
             The shoulder hangs 17.6 mm below the origin.
  MotorCap   centre of the face that meets the bottom rim of the tube.
             The grip ring is 12 mm below the origin, the two lugs reach 6.4 mm above it.
  BodyTube   centre of the bottom rim.

MEASURED SIZES
  Fin        root 58.6 mm, 55.9 mm out from the tube, 1.5 mm thick
  Nose cone  34.0 mm wide, 49.4 mm above the tube rim
  Motor cap  34.0 mm wide, bore 24.4 mm
  Assembled  about 361 mm from the bottom of the cap to the nose tip

NOT DONE YET
  Body tube with slots, wing flaps and motor (waiting on the flap slot layout).
  The FBX files were re-imported into Blender to check size and origin. They have not been opened in Unity.
