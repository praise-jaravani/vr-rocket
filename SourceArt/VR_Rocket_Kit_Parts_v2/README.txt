VR ROCKET - KIT PARTS v2 (8 Oct 2026)

Complete set: all six component types. Replaces the first-pass zip.
Docs/SPEC.md sections 4 and 13 are the reference for conventions, sizes, attach points and design decisions.

Source of the kit-derived meshes: "Model Rocket Kit" by ellipticaloptician,
printables.com/model/157400-model-rocket-kit, BSD 2-Clause. Credit the author.

FILES                 TRIS   NOTES
  BodyTube.fbx        2100   33.7 x 300 mm, nine slots, internal motor mount
  TailFin.fbx          425   cut from the kit (which has five fins); use three times
  WingFlap.fbx          28   78 mm root, 42 mm out; use three times
  NoseCone.fbx        2702   kit part, orange tip
  Motor.fbx            768   24 x 70 mm, black nozzle end
  MotorCap.fbx        3000   kit part
  attach_points.json         every attach point in the BodyTube's Unity local space
  rocket_kit_parts.blend     the correct build, assembled (saved from Blender 5.2)
  build_kit.py               regenerates everything from the original STLs:
                             blender --background --python build_kit.py -- <kit_folder> <output_folder>
  preview.png                check renders

CHANGED SINCE v1
  TailFin is now 2.0 mm thick (was 1.55). NoseCone has an orange tip. BodyTube_StandIn is gone.

STATUS
  Each FBX was re-imported into Blender to confirm size, origin and materials.
  None of them has been opened in Unity yet. SPEC.md section 4.3 lists the bounds to check on import.
