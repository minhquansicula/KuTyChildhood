"""Build a single lightweight rice plant inspired by the supplied caylua.fbx.

Run with Blender: blender --background --python build_light_rice_blender.py
The original FBX is read only for its colour/scale reference and is not modified.
"""
import bpy
import math
import random
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[3]
output = root / "BLENDER/caylua_nhe.blend"
preview = root / "BLENDER/caylua_nhe_preview.png"

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

def material(name, rgba, roughness=0.82):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgba, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgba, 1)
    shader.inputs["Roughness"].default_value = roughness
    return mat

stem_mat = material("Than_lua_xanh_oliu", (0.28, 0.34, 0.055))
leaf_mat = material("La_lua_vang_xanh", (0.45, 0.40, 0.085))
leaf_alt = material("La_lua_vang_sam", (0.36, 0.34, 0.075))
grain_mat = material("Bong_lua_vang_goldenrod", (0.7083, 0.3838, 0.0104))
grain_light = material("Hat_lua_sang", (0.78, 0.50, 0.07))

collection = bpy.data.collections.new("CayLuaNhe_MotCay")
bpy.context.scene.collection.children.link(collection)
root_obj = bpy.data.objects.new("CayLuaNhe_ROOT", None)
collection.objects.link(root_obj)

def mesh_object(name, vertices, faces, mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.parent = root_obj
    obj.data.materials.append(mat)
    return obj

def tube(name, points, radii, sides, mat):
    verts, faces = [], []
    for idx, point in enumerate(points):
        tangent = Vector(points[min(idx + 1, len(points) - 1)]) - Vector(points[max(0, idx - 1)])
        tangent.normalize()
        tangent = tangent if tangent.length > 0 else Vector((0, 0, 1))
        sideways = tangent.cross(Vector((0, 1, 0)))
        if sideways.length < .01:
            sideways = tangent.cross(Vector((1, 0, 0)))
        sideways.normalize()
        other = tangent.cross(sideways).normalized()
        for side in range(sides):
            angle = side * math.tau / sides
            verts.append(Vector(point) + radii[idx] * (math.cos(angle)*sideways + math.sin(angle)*other))
        if idx:
            before = (idx - 1) * sides
            current = idx * sides
            for side in range(sides):
                nxt = (side + 1) % sides
                faces.append((before + side, before + nxt, current + nxt, current + side))
    faces.extend((tuple(reversed(tuple(range(sides)))), tuple((len(points)-1)*sides + i for i in range(sides))))
    return mesh_object(name, verts, faces, mat)

def leaf(name, angle, height, length, width, droop, mat):
    direction = Vector((math.cos(angle), math.sin(angle), 0))
    side = Vector((-direction.y, direction.x, 0))
    vertices, faces = [], []
    segments = 8
    for i in range(segments + 1):
        t = i / segments
        spread = length * t
        z = height + .19*math.sin(math.pi*t) - droop*t*t
        center = direction*spread + Vector((0, 0, z))
        half = width * (math.sin(math.pi*t) ** .65) * .5 + .002
        vertices.extend((center - side*half, center + side*half))
        if i:
            a = 2*(i-1)
            faces.append((a, a+1, a+3, a+2))
    obj = mesh_object(name, vertices, faces, mat)
    obj.data.materials[0].use_nodes = True
    return obj

# Three tillers from one root and ten curved leaves. The upper stems lean with
# the weight of their panicles; all dimensions are metres, with the root at Z=0.
tube("Than_lua", [(.0,0,0),(.005,0,.35),(.015,0,.80),(.028,0,1.22),(.045,0,1.53),(.07,0,1.78)],
     [.023,.022,.019,.016,.013,.009], 6, stem_mat)
tube("Nhanh_lua_trai", [(-.015,0,0),(-.07,-.035,.31),(-.14,-.06,.73),(-.22,-.08,1.14),(-.30,-.10,1.57)],
     [.016,.015,.013,.010,.007], 5, stem_mat)
tube("Nhanh_lua_phai", [(.015,0,0),(.075,.03,.35),(.16,.07,.79),(.25,.10,1.18),(.32,.12,1.55)],
     [.016,.015,.013,.010,.007], 5, stem_mat)
for i, (height, length, width, droop) in enumerate([
    (.12, 1.08, .070, .52), (.23, 1.00, .067, .43), (.35, .94, .064, .41),
    (.48, 1.07, .062, .54), (.62, .91, .058, .39), (.77, .82, .054, .36),
    (.91, .85, .052, .35), (1.05, .73, .049, .31), (1.17, .65, .045, .27),
    (1.29, .55, .041, .24)]):
    leaf(f"La_lua_{i+1:02d}", i*2.39996 + .28, height, length, width, droop,
         leaf_mat if i % 2 == 0 else leaf_alt)

# Three bending panicles with slender side branches and individual low-poly grains.
rng = random.Random(41)
grain_count = 0
def add_panicle(label, points, grains_per_branch):
    global grain_count
    panicle = [Vector(p) for p in points]
    tube(f"Truc_bong_{label}", panicle,
         [.008 - i*.0008 for i in range(len(panicle))], 5, grain_mat)
    for branch in range(1, len(panicle)):
        center = panicle[branch]
        for side in (-1, 1):
            offset = Vector((.02, side*(.12 + branch*.019), -.06 - branch*.012))
            end = center + offset
            tube(f"Nhanh_bong_{label}_{branch}_{side:+d}",
                 [center, center + offset*.48, end], [.0032,.0027,.0018], 4, grain_mat)
            for n in range(grains_per_branch):
                t = .13 + n * (.82 / max(1, grains_per_branch - 1))
                location = center.lerp(end, t) + Vector((rng.uniform(-.020,.020),
                            side*rng.uniform(.004,.036), rng.uniform(-.031,.031)))
                # Six-sided tapered grain: 12 triangular faces and a fuller silhouette.
                radius = .014 + rng.uniform(-.002,.002)
                height = .031 + rng.uniform(-.004,.004)
                vs = [tuple(location + Vector((0,0,height)))]
                for edge in range(6):
                    angle = edge * math.tau / 6
                    vs.append(tuple(location + Vector((radius*math.cos(angle),
                                                        radius*.72*math.sin(angle), 0))))
                vs.append(tuple(location + Vector((0,0,-height))))
                fs = []
                for edge in range(6):
                    nxt = 1 + (edge + 1) % 6
                    fs.extend(((0,1+edge,nxt),(7,nxt,1+edge)))
                mesh_object(f"Hat_lua_{grain_count:03d}", vs, fs,
                            grain_mat if grain_count % 3 else grain_light)
                grain_count += 1

add_panicle("giua", [(.07,0,1.78),(.14,0,1.90),(.24,0,1.96),(.36,0,1.94),
                     (.48,0,1.86),(.57,0,1.75),(.64,0,1.61)], 17)
add_panicle("trai", [(-.30,-.10,1.57),(-.37,-.10,1.66),(-.47,-.10,1.68),
                     (-.56,-.10,1.62),(-.65,-.10,1.50),(-.72,-.10,1.37)], 10)
add_panicle("phai", [(.32,.12,1.55),(.40,.12,1.65),(.50,.12,1.67),
                     (.60,.12,1.61),(.69,.12,1.49),(.75,.12,1.37)], 10)

# Join components so the file has one lightweight mesh and one simple object.
meshes = [obj for obj in collection.objects if obj.type == "MESH"]
bpy.ops.object.select_all(action="DESELECT")
for obj in meshes:
    obj.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.join()
plant = bpy.context.object
plant.name = "CayLuaNhe_1Cay"
plant.data.name = "CayLuaNhe_Mesh"
plant.parent = None
bpy.data.objects.remove(root_obj, do_unlink=True)
for polygon in plant.data.polygons:
    polygon.use_smooth = False

triangle_count = sum(len(poly.vertices)-2 for poly in plant.data.polygons)
plant["source_reference"] = "caylua.fbx - artist's original, not modified"
plant["triangle_count"] = triangle_count
plant["grain_count"] = grain_count
print(f"[LIGHT RICE] {triangle_count} triangles, {grain_count} grains, {len(plant.data.materials)} materials")

# Set up a ready-to-inspect viewport, camera and neutral preview render.
bpy.context.scene.unit_settings.system = "METRIC"
bpy.context.scene.render.engine = "BLENDER_EEVEE"
bpy.context.scene.render.resolution_x = 900
bpy.context.scene.render.resolution_y = 1000
bpy.context.scene.render.resolution_percentage = 100
bpy.context.scene.world.use_nodes = True
background = bpy.context.scene.world.node_tree.nodes.get("Background")
background.inputs["Color"].default_value = (.42,.45,.43,1)
background.inputs["Strength"].default_value = .8
bpy.context.scene.view_settings.view_transform = "Standard"
camera_data = bpy.data.cameras.new("Preview_Camera")
camera = bpy.data.objects.new("Preview_Camera", camera_data)
collection.objects.link(camera)
camera.location = (3.2,-4.2,2.4)
target = Vector((0,0,1.0))
camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
camera_data.type = "ORTHO"
camera_data.ortho_scale = 2.9
bpy.context.scene.camera = camera
light_data = bpy.data.lights.new("Preview_Sun", "SUN")
light = bpy.data.objects.new("Preview_Sun", light_data)
collection.objects.link(light)
light.rotation_euler = (.35,-.5,-.5)
light_data.energy = 2.2

bpy.ops.object.select_all(action="DESELECT")
plant.select_set(True)
bpy.context.view_layer.objects.active = plant
for area in bpy.context.screen.areas:
    if area.type == "VIEW_3D":
        area.spaces.active.region_3d.view_location = (0,0,1)
        area.spaces.active.region_3d.view_distance = 3.1
        area.spaces.active.shading.type = "MATERIAL"

output.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(output))
bpy.context.scene.render.filepath = str(preview)
bpy.ops.render.render(write_still=True)
print(f"[LIGHT RICE] saved {output} and {preview}")
