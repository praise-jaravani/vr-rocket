# Rocket asset conventions and open design issues

Written 8 Oct 2026, after the first-pass cut of the Printables kit (VR_Rocket_Kit_Parts.zip, delivered in chat).

## Kit facts (measured from the STLs)

- Source: "Model Rocket Kit" by ellipticaloptician, printables.com/model/157400-model-rocket-kit, BSD 2-Clause, credit required.
- STLs are in millimetres, tube axis along Y, very dense (base_swept 325k tris, nosecone_snub 211k, 24mm_motor_cap 54k).
- `base_swept.STL` is one fused body with FIVE fins, each blended into the can with a small root fillet. One fin was cut out above the fillet and is reused three times.
- `nosecone_snub.STL` is two closed surfaces: the outer skin and a sealed inner cavity. Only the skin is kept.
- The motor cap's bore is 24.4 mm, its grip ring is 12 mm deep and its two lugs reach 6.4 mm up into the tube.

## Part conventions (all exported FBX files)

- Units are metres at real size. Unity +Y is the rocket axis pointing at the nose.
- Tail fin: Unity +Z points away from the tube, so the slide axis is local Z. Origin on the tube surface at the middle of the root edge. A 2 mm tab continues into the tube.
- Nose cone: origin at the centre of the shoulder step (the tube's top rim). Shoulder hangs 17.6 mm below.
- Motor cap: origin at the centre of the face that meets the tube's bottom rim.
- Body tube: origin at the centre of the bottom rim.
- Sizes: fin root 58.6 mm, 55.9 mm out, 1.5 mm thick. Nose cone 49.4 mm above the rim. Assembled height about 361 mm.
- Triangle counts after clean-up: fin 425, nose cone 2500, motor cap 3000.

## Open design issues found while measuring

1. Flap slots overlap. Fin slots end about 66 mm up the tube. A 78 mm flap directly above them runs to about 144 mm, but a flap centred on the 150 mm midpoint starts at 111 mm. At the same angle the lower and midpoint positions collide over about 33 mm. Fix options: shorten the flaps, rotate the lower set 60 degrees, or move the upper set higher.
2. Motor "wider base" (treatment 2.3) cannot sit under the kit cap, whose bore is only 0.4 mm wider than the motor. Wrong-way insertion is better blocked in the socket logic.
3. Fin thickness is 1.5 mm against a 2.5 mm slot in the treatment.

## Still to model

Body tube with slots, wing flaps, motor. Blocked on issue 1.

## Status

FBX files were re-imported into Blender to confirm size and origin. Not yet opened in Unity.
