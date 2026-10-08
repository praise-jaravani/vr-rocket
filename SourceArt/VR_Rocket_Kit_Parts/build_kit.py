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
TUBE_R, TUBE_LEN, TUBE_WALL = 33.7 / 2, 300.0, 1.2      # body tube stand-in (treatment 2.2)
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
    ob.data.materials.clear(); ob.data.materials.append(mat)
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
nose = finish(ob, bm, "NoseCone", MAT["white"], target_tris=2500)

# ---------------------------------------------------------------- motor cap
# Grip ring from y=0 to the step at ~12 mm, bayonet lugs above it that go up into the tube.
ob, bm = load("24mm_motor_cap.STL", lambda p: Vector((p.x - 17.0, -(p.z - 17.0), p.y)))
seat = max(v.co.z for v in bm.verts if math.hypot(v.co.x, v.co.y) > 16.0)
bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -seat))
print(f"CAP grip ring depth {seat:.1f} mm, lugs reach {max(v.co.z for v in bm.verts):.1f} mm into the tube")
cap = finish(ob, bm, "MotorCap", MAT["black"], target_tris=3000)

# ---------------------------------------------------------------- body tube stand-in
# Plain hollow tube with no slots, only so the kit parts can be checked against it.
bm = bmesh.new()
segs = 48
ring = lambda r, z: [bm.verts.new((r * math.cos(2 * math.pi * i / segs), r * math.sin(2 * math.pi * i / segs), z)) for i in range(segs)]
o0, o1, i0, i1 = ring(TUBE_R, 0), ring(TUBE_R, TUBE_LEN), ring(TUBE_R - TUBE_WALL, 0), ring(TUBE_R - TUBE_WALL, TUBE_LEN)
for i in range(segs):
    j = (i + 1) % segs
    bm.faces.new((o0[i], o0[j], o1[j], o1[i])); bm.faces.new((i0[j], i0[i], i1[i], i1[j]))
    bm.faces.new((o0[j], o0[i], i0[i], i0[j])); bm.faces.new((o1[i], o1[j], i1[j], i1[i]))
me = bpy.data.meshes.new("BodyTube_StandIn"); ob = bpy.data.objects.new("BodyTube_StandIn", me)
bpy.context.scene.collection.objects.link(ob)
tube = finish(ob, bm, "BodyTube_StandIn", MAT["white"], smooth_deg=60)

# ---------------------------------------------------------------- export each part on its own
def export(ob):
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, ob.name + ".fbx"), use_selection=True,
                             apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='FACE',
                             use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False)
for ob in (fin, nose, cap, tube):
    export(ob)

# ---------------------------------------------------------------- assembled check scene
nose.location = (0, 0, TUBE_LEN * MM)
fins = [fin]
for k in (1, 2):
    f = fin.copy(); bpy.context.scene.collection.objects.link(f); fins.append(f)   # linked duplicates, one mesh
for k, f in enumerate(fins):
    a = math.radians(120 * k)
    f.name = f"TailFin_{k + 1}"
    f.rotation_euler = (0, 0, a)
    # fin's outward direction is local -Y
    f.location = Matrix.Rotation(a, 3, 'Z') @ Vector((0, -TUBE_R * MM, (FIN_ROOT_START + FIN_ROOT_LEN / 2) * MM))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, "rocket_kit_parts.blend"))
