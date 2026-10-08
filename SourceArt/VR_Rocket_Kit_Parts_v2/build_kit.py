"""Cut the Printables 'Model Rocket Kit' (ellipticaloptician, BSD 2-Clause) into
separate, Unity-ready parts for the VR Rocket project.

Run:  blender --background --python build_kit.py -- <kit_folder> <output_folder>

Conventions for every exported part
  * units are metres (the STLs are millimetres)
  * Blender +Z = rocket axis, pointing at the nose  -> Unity +Y
  * Blender -Y = radially outward (fin only)        -> Unity +Z
  * origin sits on the attachment point (see each part below)
"""
import bpy, bmesh, sys, os, math
from mathutils import Vector, Matrix

kit, out = sys.argv[-2], sys.argv[-1]
os.makedirs(out, exist_ok=True)
MM = 0.001
TUBE_R, TUBE_LEN = 33.7 / 2, 300.0                      # body tube (treatment 2.2)
TUBE_INNER_R = 16.4                                     # matches the nose cone shoulder, so it reads as a push fit
PLATE_T = 2.0                                           # thickness of fins and flaps (kit fin is 1.55, too thin to read in VR)
SLOT_W = 2.5                                            # slot width (treatment 2.2)
SLOT_CLEAR = 1.0                                        # slot is this much longer than the tab that goes in it
FLAP_ROOT, FLAP_SPAN, FLAP_TAB = 78.0, 42.0, 60.0       # wing flap (treatment 2.2); tab is centred so it fits either way up
FLAP_SWEEP_TOP, FLAP_SWEEP_BOTTOM = 30.0, 8.0           # leading edge sweeps more than trailing edge: "up" is readable but subtle
FLAP_LOW_START = 70.0                                   # lower flap slots start just above the tail fins
FLAP_MID_CENTRE = TUBE_LEN / 2                          # correct flap position
FIN_ANGLES, FLAP_MID_ANGLES, FLAP_LOW_ANGLES = (0, 120, 240), (0, 120, 240), (60, 180, 300)
NOSE_TIP = 12.0                                         # height of the orange nose tip
MOTOR_R, MOTOR_LEN = 12.0, 70.0                         # 24 x 70 mm motor (treatment 2.2)
MOTOR_SEAT_Z = -11.0                                    # nozzle face sits inside the cap, just above its retaining lip
KIT_R = 17.0                                            # outside radius of the kit's fin can
TAB_DEPTH = 2.0                                         # fin root tab that sits in the slot
FILLET = 1.5                                            # the kit blends each fin into the can with a small fillet
FIN_ROOT_START = 6.0                                    # height of the fin root above the tube base

bpy.ops.wm.read_factory_settings(use_empty=True)

def material(name, rgb, rough=0.6):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    return m

MAT = {"white": material("Rocket_White", (0.9, 0.9, 0.88)),
       "card": material("Rocket_Cardboard", (0.62, 0.47, 0.30), 0.85),
       "orange": material("Rocket_Orange", (0.9, 0.27, 0.08)),
       "black": material("Rocket_Black", (0.03, 0.03, 0.035), 0.45)}

def load(stl, to_frame):
    """Import an STL and move its vertices into the rocket frame (still mm)."""
    bpy.ops.wm.stl_import(filepath=os.path.join(kit, stl))
    ob = bpy.context.selected_objects[0]
    for v in ob.data.vertices:
        v.co = to_frame(v.co)
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    return ob, bm

def finish(ob, bm, name, mat, target_tris=None, smooth_deg=35):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(ob.data); bm.free()
    ob.name = ob.data.name = name
    bpy.context.view_layer.objects.active = ob
    before = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    if target_tris and before > target_tris:
        # CAD tessellation: merge coplanar facets first, then collapse what is left
        d = ob.modifiers.new("planar", 'DECIMATE'); d.decimate_type = 'DISSOLVE'; d.angle_limit = math.radians(0.5)
        bpy.ops.object.modifier_apply(modifier=d.name)
        t = ob.modifiers.new("tri", 'TRIANGULATE'); bpy.ops.object.modifier_apply(modifier=t.name)
        n = len(ob.data.polygons)
        if n > target_tris:
            c = ob.modifiers.new("collapse", 'DECIMATE'); c.ratio = target_tris / n
            bpy.ops.object.modifier_apply(modifier=c.name)
    t = ob.modifiers.new("tri", 'TRIANGULATE'); bpy.ops.object.modifier_apply(modifier=t.name)
    for v in ob.data.vertices:
        v.co *= MM
    ob.data.polygons.foreach_set("use_smooth", [True] * len(ob.data.polygons))
    ob.data.set_sharp_from_angle(angle=math.radians(smooth_deg))
    idx = [p.material_index for p in ob.data.polygons]      # clearing the slots resets these, so keep them
    ob.data.materials.clear()
    for m in (mat if isinstance(mat, (list, tuple)) else [mat]):
        ob.data.materials.append(m)
    ob.data.polygons.foreach_set("material_index", idx)
    ob.data.update()
    bpy.context.view_layer.update()
    dims = [round(d / MM, 1) for d in ob.dimensions]
    print(f"PART {name}: {before} -> {len(ob.data.polygons)} tris, size mm {dims}")
    return ob

# ---------------------------------------------------------------- tail fin
# The fin can is one fused body with FIVE fins. Its axis is the STL's Y axis through
# (69.52, 59.38) in XZ and one fin points along +Z, so that is the one we keep.
CX, CZ = 69.52, 59.38
ob, bm = load("base_swept.STL", lambda p: Vector((p.x - CX, -(p.z - CZ - KIT_R), p.y)))
# Cut just above the root fillet, where the fin is a clean flat plate (y = 0 is the tube surface,
# -y is outward), then rebuild the last stretch down to the surface plus the tab as a straight extrusion.
geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(0, -FILLET, 0), plane_no=(0, 1, 0), clear_outer=True)
cut_edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
bmesh.ops.holes_fill(bm, edges=cut_edges)
# drop every island except the fin on the +Z side (|x| < 2 mm, reaches past 40 mm)
seen, keep = set(), None
for v in bm.verts:
    if v in seen: continue
    stack, isl = [v], []
    seen.add(v)
    while stack:
        a = stack.pop(); isl.append(a)
        for e in a.link_edges:
            b = e.other_vert(a)
            if b not in seen: seen.add(b); stack.append(b)
    if max(abs(a.co.x) for a in isl) < 2 and min(a.co.y for a in isl) < -40:
        keep = set(isl)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v not in keep], context='VERTS')
# root face = the cap on the cut plane; centre the origin on it and grow the tab from it
root = [f for f in bm.faces if all(abs(v.co.y + FILLET) < 1e-4 for v in f.verts)]
zs = [v.co.z for f in root for v in f.verts]
zmid = (min(zs) + max(zs)) / 2
FIN_ROOT_LEN = max(zs) - min(zs)
FIN_THICK = max(v.co.x for v in bm.verts) - min(v.co.x for v in bm.verts)
bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -zmid))
ext = bmesh.ops.extrude_face_region(bm, geom=root)
bmesh.ops.translate(bm, verts=[g for g in ext["geom"] if isinstance(g, bmesh.types.BMVert)], vec=(0, FILLET + TAB_DEPTH, 0))
bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.5), verts=bm.verts, edges=bm.edges)
for v in bm.verts:                                      # thicken the plate from the kit's 1.55 mm to PLATE_T
    v.co.x *= PLATE_T / FIN_THICK
print(f"FIN root length {FIN_ROOT_LEN:.1f} mm, thickness {FIN_THICK:.2f} mm, span {-min(v.co.y for v in bm.verts):.1f} mm, root faces {len(root)}")
fin = finish(ob, bm, "TailFin", MAT["orange"])

# ---------------------------------------------------------------- nose cone
# Two closed surfaces: the outer skin and a sealed internal cavity nobody can see. Keep the skin.
ob, bm = load("nosecone_snub.STL", lambda p: Vector((p.x - 17.277, -(p.z - 17.277), p.y)))
seen, islands = set(), []
for v in bm.verts:
    if v in seen: continue
    stack, isl = [v], []
    seen.add(v)
    while stack:
        a = stack.pop(); isl.append(a)
        for e in a.link_edges:
            b = e.other_vert(a)
            if b not in seen: seen.add(b); stack.append(b)
    islands.append(isl)
islands.sort(key=lambda i: max(math.hypot(a.co.x, a.co.y) for a in i), reverse=True)
for isl in islands[1:]:
    bmesh.ops.delete(bm, geom=isl, context='VERTS')
# seat = the step where the 16.4 mm shoulder widens to the 17 mm base
seat = min(v.co.z for v in bm.verts if math.hypot(v.co.x, v.co.y) > 16.9)
bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -seat))
print(f"NOSE shoulder length {seat:.1f} mm")
nose = finish(ob, bm, "NoseCone", [MAT["white"], MAT["orange"]], target_tris=2500)
# orange tip (treatment 2.2): cut a clean edge loop 12 mm below the tip and recolour everything above it
bm = bmesh.new(); bm.from_mesh(nose.data)
tip_z = max(v.co.z for v in bm.verts) - NOSE_TIP * MM
bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(0, 0, tip_z), plane_no=(0, 0, 1))
bmesh.ops.triangulate(bm, faces=bm.faces)
for f in bm.faces:
    f.material_index = 1 if f.calc_center_median().z > tip_z else 0
    f.smooth = True
bm.to_mesh(nose.data); bm.free(); nose.data.set_sharp_from_angle(angle=math.radians(35)); nose.data.update()

# ---------------------------------------------------------------- motor cap
# Grip ring from y=0 to the step at ~12 mm, bayonet lugs above it that go up into the tube.
ob, bm = load("24mm_motor_cap.STL", lambda p: Vector((p.x - 17.0, -(p.z - 17.0), p.y)))
seat = max(v.co.z for v in bm.verts if math.hypot(v.co.x, v.co.y) > 16.0)
bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -seat))
print(f"CAP grip ring depth {seat:.1f} mm, lugs reach {max(v.co.z for v in bm.verts):.1f} mm into the tube")
cap = finish(ob, bm, "MotorCap", MAT["black"], target_tris=3000)

# ---------------------------------------------------------------- helpers for the modelled parts
def new_object(name):
    me = bpy.data.meshes.new(name); ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob

def lathe(bm, profile, mats, segs=64):
    """Revolve a closed (r, z) profile around Z. mats[i] is the material index of the band from point i to i+1."""
    rings = []
    for r, z in profile:
        rings.append(None if r < 1e-6 else [bm.verts.new((r * math.cos(2 * math.pi * i / segs), r * math.sin(2 * math.pi * i / segs), z)) for i in range(segs)])
    axis = {i: bm.verts.new((0, 0, z)) for i, (r, z) in enumerate(profile) if r < 1e-6}
    n = len(profile)
    for k in range(n):
        a, b = k, (k + 1) % n
        if rings[a] is None and rings[b] is None:
            continue
        for i in range(segs):
            j = (i + 1) % segs
            if rings[a] is None:   f = bm.faces.new((axis[a], rings[b][i], rings[b][j]))
            elif rings[b] is None: f = bm.faces.new((rings[a][j], rings[a][i], axis[b]))
            else:                  f = bm.faces.new((rings[a][i], rings[a][j], rings[b][j], rings[b][i]))
            f.material_index = mats[k]

def slot_centre(kind, k):
    """(angle in degrees, height of slot centre above the tube base) for slot k of a kind."""
    if kind == "fin":      return FIN_ANGLES[k], FIN_ROOT_START + FIN_ROOT_LEN / 2
    if kind == "flap_mid": return FLAP_MID_ANGLES[k], FLAP_MID_CENTRE
    if kind == "flap_low": return FLAP_LOW_ANGLES[k], FLAP_LOW_START + FLAP_ROOT / 2

# ---------------------------------------------------------------- body tube with nine slots and a motor mount
bm = bmesh.new()
lathe(bm, [(TUBE_R, 0), (TUBE_R, TUBE_LEN), (TUBE_INNER_R, TUBE_LEN), (TUBE_INNER_R, 0)], [0, 0, 1, 0])
ob = new_object("BodyTube"); bm.to_mesh(ob.data); bm.free()
ob.data.materials.append(MAT["white"]); ob.data.materials.append(MAT["card"])
cut_bm = bmesh.new()
for kind, length in (("fin", FIN_ROOT_LEN + SLOT_CLEAR), ("flap_mid", FLAP_TAB + SLOT_CLEAR), ("flap_low", FLAP_TAB + SLOT_CLEAR)):
    for k in range(3):
        ang, zc = slot_centre(kind, k)
        geom = bmesh.ops.create_cube(cut_bm, size=1.0)["verts"]
        bmesh.ops.scale(cut_bm, verts=geom, vec=(SLOT_W, 6.0, length))
        bmesh.ops.translate(cut_bm, verts=geom, vec=(0, -TUBE_R, zc))          # outward is -Y, like the fin
        bmesh.ops.rotate(cut_bm, verts=geom, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(ang), 3, 'Z'))
cutter = new_object("cutter"); cut_bm.to_mesh(cutter.data); cut_bm.free()
bpy.context.view_layer.objects.active = ob
mod = ob.modifiers.new("slots", 'BOOLEAN'); mod.operation = 'DIFFERENCE'; mod.object = cutter; mod.solver = 'EXACT'
bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.data.objects.remove(cutter)
bm = bmesh.new(); bm.from_mesh(ob.data)
# motor mount: lower centring ring, mount tube, and a thrust ring that stops the motor
MOUNT_R_IN, MOUNT_R_OUT, RING_R = MOTOR_R + 0.1, MOTOR_R + 0.6, TUBE_INNER_R - 0.02
STOP_Z = MOTOR_SEAT_Z + MOTOR_LEN
lathe(bm, [(MOUNT_R_IN, 8), (RING_R, 8), (RING_R, 9.5), (MOUNT_R_OUT, 9.5), (MOUNT_R_OUT, STOP_Z), (RING_R, STOP_Z),
           (RING_R, STOP_Z + 1.5), (10.0, STOP_Z + 1.5), (10.0, STOP_Z), (MOUNT_R_IN, STOP_Z)], [1] * 10)
tube = finish(ob, bm, "BodyTube", [MAT["white"], MAT["card"]], smooth_deg=50)

# ---------------------------------------------------------------- wing flap
bm = bmesh.new()
h, t = FLAP_ROOT / 2, PLATE_T / 2
outline = [(-h, 0), (-h + FLAP_SWEEP_BOTTOM, FLAP_SPAN), (h - FLAP_SWEEP_TOP, FLAP_SPAN), (h, 0),
           (FLAP_TAB / 2, 0), (FLAP_TAB / 2, -TAB_DEPTH), (-FLAP_TAB / 2, -TAB_DEPTH), (-FLAP_TAB / 2, 0)]   # (height, distance out)
front = [bm.verts.new((t, -r, z)) for z, r in outline]
back = [bm.verts.new((-t, -r, z)) for z, r in outline]
bm.faces.new(front); bm.faces.new(list(reversed(back)))
for i in range(len(outline)):
    j = (i + 1) % len(outline)
    bm.faces.new((front[j], front[i], back[i], back[j]))
flap = finish(new_object("WingFlap"), bm, "WingFlap", MAT["orange"])

# ---------------------------------------------------------------- motor
bm = bmesh.new()
lathe(bm, [(0, 10), (2.5, 10), (2.5, 3), (9.5, 3), (9.5, 0), (MOTOR_R, 0), (MOTOR_R, MOTOR_LEN),
           (10, MOTOR_LEN), (10, MOTOR_LEN - 2), (0, MOTOR_LEN - 2)], [1, 1, 1, 1, 0, 0, 0, 0, 2, 0], segs=48)
motor = finish(new_object("Motor"), bm, "Motor", [MAT["card"], MAT["black"], MAT["white"]], smooth_deg=50)

# ---------------------------------------------------------------- export each part on its own
def export(ob):
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, ob.name + ".fbx"), use_selection=True,
                             apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE',
                             use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False)
for ob in (tube, fin, flap, nose, motor, cap):
    export(ob)

# ---------------------------------------------------------------- assembled check scene (the correct build)
def place(src, kind, k, name, flipped=False):
    o = src if name == src.name else src.copy()
    if o is not src: bpy.context.scene.collection.objects.link(o)
    ang, zc = slot_centre(kind, k); a = math.radians(ang)
    o.name = name
    o.rotation_euler = (0, math.pi if flipped else 0, a)        # flipped = turned over about the outward axis
    o.location = Matrix.Rotation(a, 3, 'Z') @ Vector((0, -TUBE_R * MM, zc * MM))
    return o
for k in range(3):
    place(fin, "fin", k, "TailFin" if k == 0 else f"TailFin_{k + 1}")
    place(flap, "flap_mid", k, "WingFlap" if k == 0 else f"WingFlap_{k + 1}")
nose.location = (0, 0, TUBE_LEN * MM)
motor.location = (0, 0, MOTOR_SEAT_Z * MM)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, "rocket_kit_parts.blend"))

# ---------------------------------------------------------------- attach points, in the BodyTube's Unity local space
# Unity position = (R sin(yaw), height, R cos(yaw)), rotation = (0, yaw, 0); a part's local +Z then points outward.
import json
points = []
for kind, label in (("fin", "FinSlot"), ("flap_mid", "FlapSlot_Mid"), ("flap_low", "FlapSlot_Low")):
    slots = sorted(((-slot_centre(kind, k)[0]) % 360, slot_centre(kind, k)[1]) for k in range(3))
    for n, (yaw, zc) in enumerate(slots):
        y = math.radians(yaw)
        points.append({"name": f"{label}_{n + 1}", "yaw_deg": yaw, "pos_m": [round(TUBE_R * MM * math.sin(y), 5), round(zc * MM, 5), round(TUBE_R * MM * math.cos(y), 5)]})
points += [{"name": "NoseSeat", "yaw_deg": 0, "pos_m": [0, TUBE_LEN * MM, 0]},
           {"name": "MotorSeat", "yaw_deg": 0, "pos_m": [0, MOTOR_SEAT_Z * MM, 0]},
           {"name": "CapSeat", "yaw_deg": 0, "pos_m": [0, 0, 0]}]
json.dump({"space": "BodyTube local space in Unity: metres, +Y to the nose. A part placed with rotation (0, yaw, 0) has its local +Z pointing outward.", "points": points}, open(os.path.join(out, "attach_points.json"), "w"), indent=1)
for p in points: print("ATTACH", p)
