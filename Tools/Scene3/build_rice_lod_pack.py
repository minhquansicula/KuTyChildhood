"""Build reusable rice LOD assets from the existing approved Blender plant.

Run with Blender --background --factory-startup --python this_file.py.
Only NEW RiceOptimized assets and BLENDER/CayLua_ToiUu.blend are written.
No scene, old FBX, old material, or runtime graphics script is changed.
"""
from pathlib import Path
import json
import math
import hashlib

import bpy
import numpy as np
from mathutils import Vector


PROJECT = Path(__file__).resolve().parents[2]
WORKSPACE = PROJECT.parent
SOURCE = WORKSPACE / "BLENDER/caylua_nhe.blend"
MODELS = PROJECT / "Assets/_Project/Art/Models/Scene3/RiceOptimized"
TEXTURES = PROJECT / "Assets/_Project/Art/Textures/Scene3/RiceOptimized"
REPORTS = PROJECT / "Tools/Scene3/RiceOptimized"
BLEND = WORKSPACE / "BLENDER/CayLua_ToiUu.blend"
ATLAS_SIZE = 2048
TILE_W, TILE_H = 672, 640
ANGLES = (0, 60, 120)


def announce(message):
    print("[RICE_LOD] " + message, flush=True)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def count_triangles(obj):
    return sum(len(poly.vertices) - 2 for poly in obj.data.polygons)


def mesh_object(name, vertices, faces, material_indices=None, materials=None):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    for material in materials or []:
        mesh.materials.append(material)
    if material_indices:
        for polygon, index in zip(mesh.polygons, material_indices):
            polygon.material_index = index
    return obj


def subset(source, indices, name):
    polygons = [source.data.polygons[index] for index in indices]
    used = sorted({index for polygon in polygons for index in polygon.vertices})
    remap = {old: new for new, old in enumerate(used)}
    return mesh_object(name, [tuple(source.data.vertices[index].co) for index in used],
                       [tuple(remap[index] for index in polygon.vertices) for polygon in polygons],
                       [polygon.material_index for polygon in polygons],
                       list(source.data.materials))


def split_panicles(source):
    """Recover the original three separate panicles from mesh connectivity.

    Their primary five-sided axes have 30/35 vertices. Component order in
    this approved source follows axis, branches, grains, then the next axis.
    """
    mesh = source.data
    adjacency = [[] for _ in mesh.vertices]
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacency[a].append(b)
        adjacency[b].append(a)
    seen, components = set(), []
    for vertex in mesh.vertices:
        if vertex.index in seen:
            continue
        stack, ids = [vertex.index], []
        seen.add(vertex.index)
        while stack:
            index = stack.pop()
            ids.append(index)
            for neighbor in adjacency[index]:
                if neighbor not in seen:
                    seen.add(neighbor)
                    stack.append(neighbor)
        components.append(ids)
    axes = []
    panicle_vertices = {v for p in mesh.polygons if p.material_index >= 3 for v in p.vertices}
    for component in components:
        if len(component) in (30, 35) and component[0] in panicle_vertices:
            axes.append(min(component))
    axes.sort()
    if len(axes) != 3:
        raise RuntimeError("Could not identify the source plant's three panicles")
    limits = axes + [len(mesh.vertices)]
    return [subset(source, [p.index for p in mesh.polygons
                            if p.material_index >= 3 and limits[i] <= min(p.vertices) < limits[i + 1]],
                   "BAKE_Panicle_" + label)
            for i, label in enumerate(("Middle", "Left", "Right"))]


def emission_material(name, color):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (*color[:3], 1)
    material.node_tree.links.new(emission.outputs[0], output.inputs["Surface"])
    return material


def normal_material(right, up, facing):
    material = bpy.data.materials.new("Bake_ViewSpace_Normal")
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    nodes.clear()
    geometry = nodes.new("ShaderNodeNewGeometry")
    negate = nodes.new("ShaderNodeVectorMath")
    negate.operation = "SCALE"
    negate.inputs[3].default_value = -1
    links.new(geometry.outputs["Normal"], negate.inputs[0])
    # A visible back face must have a normal facing the card's front side.
    orient = nodes.new("ShaderNodeMixRGB")
    orient.blend_type = "MIX"
    links.new(geometry.outputs["Backfacing"], orient.inputs[0])
    links.new(geometry.outputs["Normal"], orient.inputs[1])
    links.new(negate.outputs["Vector"], orient.inputs[2])
    combine = nodes.new("ShaderNodeCombineXYZ")
    for index, axis in enumerate((right, up, facing)):
        dot = nodes.new("ShaderNodeVectorMath")
        dot.operation = "DOT_PRODUCT"
        dot.inputs[1].default_value = axis
        links.new(orient.outputs[0], dot.inputs[0])
        encode = nodes.new("ShaderNodeMath")
        encode.operation = "MULTIPLY_ADD"
        encode.inputs[1].default_value = .5
        encode.inputs[2].default_value = .5
        links.new(dot.outputs["Value"], encode.inputs[0])
        links.new(encode.outputs[0], combine.inputs[index])
    emission = nodes.new("ShaderNodeEmission")
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(combine.outputs[0], emission.inputs["Color"])
    links.new(emission.outputs[0], output.inputs["Surface"])
    return material


def image_array(path):
    image = bpy.data.images.load(str(path), check_existing=False)
    pixels = np.empty(len(image.pixels), dtype=np.float32)
    image.pixels.foreach_get(pixels)
    result = pixels.reshape(image.size[1], image.size[0], 4).copy()
    bpy.data.images.remove(image)
    return result


def save_array(name, array, path, color_space):
    image = bpy.data.images.new(name, width=array.shape[1], height=array.shape[0], alpha=True)
    image.colorspace_settings.name = color_space
    image.alpha_mode = "STRAIGHT"
    image.pixels.foreach_set(array.ravel())
    image.file_format = "PNG"
    image.filepath_raw = str(path)
    image.save()
    return image


def dilate_rgb(array, steps=12):
    """Fill transparent texels with nearby colors, retaining the original alpha."""
    result = array.copy()
    known = result[:, :, 3] > .02
    for _ in range(steps):
        sums = np.zeros_like(result[:, :, :3])
        neighbors = np.zeros_like(result[:, :, 3])
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            mask = np.roll(known, (dy, dx), axis=(0, 1))
            values = np.roll(result[:, :, :3], (dy, dx), axis=(0, 1))
            if dy == -1:
                mask[-1, :] = False
            elif dy == 1:
                mask[0, :] = False
            if dx == -1:
                mask[:, -1] = False
            elif dx == 1:
                mask[:, 0] = False
            sums += values * mask[:, :, None]
            neighbors += mask
        fill = ~known & (neighbors > 0)
        result[fill, :3] = sums[fill] / neighbors[fill, None]
        known |= fill
    return result


def capture(obj, angle, tile_index, color_materials):
    material_indices = [polygon.material_index for polygon in obj.data.polygons]
    theta = math.radians(angle)
    right = Vector((math.cos(theta), math.sin(theta), 0))
    up = Vector((0, 0, 1))
    facing = right.cross(up)
    points = [vertex.co for vertex in obj.data.vertices]
    xs = [point.dot(right) for point in points]
    zs = [point.z for point in points]
    ds = [point.dot(facing) for point in points]
    center_x, center_z, depth = (min(xs) + max(xs)) / 2, (min(zs) + max(zs)) / 2, (min(ds) + max(ds)) / 2
    width = max((max(xs) - min(xs)) * 1.10, (max(zs) - min(zs)) * 1.10 * TILE_W / TILE_H)
    height = width * TILE_H / TILE_W
    center = right * center_x + up * center_z + facing * depth
    camera.location = center + facing * 8
    camera.rotation_euler = (-facing).to_track_quat("-Z", "Y").to_euler()
    camera.data.ortho_scale = width
    for candidate in bpy.context.scene.objects:
        if candidate.type == "MESH":
            candidate.hide_render = candidate != obj
    scene.render.resolution_x, scene.render.resolution_y = TILE_W, TILE_H
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    obj.data.materials.clear()
    for material in color_materials:
        obj.data.materials.append(material)
    for polygon, material_index in zip(obj.data.polygons, material_indices):
        polygon.material_index = material_index
    color_path = REPORTS / f"bake_{tile_index:02d}_color.png"
    scene.render.filepath = str(color_path)
    announce(f"Baking {obj.name}, view {angle}, color")
    bpy.ops.render.render(write_still=True)
    color = image_array(color_path)
    normal = normal_material(right, up, facing)
    obj.data.materials.clear()
    for _ in color_materials:
        obj.data.materials.append(normal)
    for polygon, material_index in zip(obj.data.polygons, material_indices):
        polygon.material_index = material_index
    scene.view_settings.view_transform = "Raw"
    normal_path = REPORTS / f"bake_{tile_index:02d}_normal.png"
    scene.render.filepath = str(normal_path)
    announce(f"Baking {obj.name}, view {angle}, normal")
    bpy.ops.render.render(write_still=True)
    # Raw PNG: do not apply the sRGB decoder when reading the encoded normal.
    normal_image = bpy.data.images.load(str(normal_path), check_existing=False)
    normal_image.colorspace_settings.name = "Non-Color"
    normal_pixels = np.empty(len(normal_image.pixels), dtype=np.float32)
    normal_image.pixels.foreach_get(normal_pixels)
    normal_array = normal_pixels.reshape(TILE_H, TILE_W, 4).copy()
    bpy.data.images.remove(normal_image)
    announce(f"Finished tile {tile_index}, alpha pixels {int((color[:, :, 3] > .05).sum())}")
    return {"angle": angle, "tile": tile_index, "right": list(right), "facing": list(facing),
            "center_x": center_x, "center_z": center_z, "depth": depth,
            "width": width, "height": height, "color": color, "normal": normal_array}


def append_cards(vertices, faces, face_uvs, face_roles, capture_data, segments):
    alpha = capture_data["color"][:, :, 3]
    ys, xs = np.where(alpha > .04)
    if len(xs) == 0:
        raise RuntimeError("Empty card bake")
    low = max(1, int(ys.min()) - 3)
    high = min(TILE_H - 2, int(ys.max()) + 3)
    rows = np.linspace(low, high, segments + 1)
    right, facing = Vector(capture_data["right"]), Vector(capture_data["facing"])
    tile_x = 16 + (capture_data["tile"] % 3) * TILE_W
    tile_y = 128 + (capture_data["tile"] // 3) * TILE_H
    for y0, y1 in zip(rows[:-1], rows[1:]):
        sample = alpha[max(0, int(y0) - 3):min(TILE_H, int(math.ceil(y1)) + 4), :]
        _, columns = np.where(sample > .02)
        if not len(columns):
            continue
        x0, x1 = max(1, int(columns.min()) - 4), min(TILE_W - 2, int(columns.max()) + 4)
        index = len(vertices)
        uvs = []
        for x, y in ((x0, y0), (x1, y0), (x1, y1), (x0, y1)):
            horizontal = capture_data["center_x"] + (x / TILE_W - .5) * capture_data["width"]
            z = capture_data["center_z"] + (y / TILE_H - .5) * capture_data["height"]
            point = right * horizontal + facing * capture_data["depth"] + Vector((0, 0, max(0., z)))
            vertices.append(tuple(point))
            uvs.append(((tile_x + x) / ATLAS_SIZE, (tile_y + y) / ATLAS_SIZE))
        faces.append((index, index + 1, index + 2, index + 3))
        face_uvs.append(uvs)
        face_roles.append(1.0)


def add_wind_attributes(obj, roles):
    colors = obj.data.color_attributes.new(name="WindMask", type="FLOAT_COLOR", domain="CORNER")
    obj.data.color_attributes.active_color = colors
    uv = obj.data.uv_layers.new(name="WindData")
    height = max(vertex.co.z for vertex in lod0.data.vertices)
    for polygon, role in zip(obj.data.polygons, roles):
        for loop_index in polygon.loop_indices:
            point = obj.data.vertices[obj.data.loops[loop_index].vertex_index].co
            mask = min(1., max(0., point.z / height))
            colors.data[loop_index].color = (mask, role, .5, 1.)
            uv.data[loop_index].uv = (mask, role)
    obj["wind_channels"] = "COLOR.r: height 0..1, COLOR.g: flutter, COLOR.b: reserved 0.5; UV1 duplicates R/G"


def final_mesh(name, vertices, faces, uvs, roles, material):
    obj = mesh_object(name, vertices, faces, materials=[material])
    layer = obj.data.uv_layers.new(name="UVMap")
    for polygon, polygon_uvs in zip(obj.data.polygons, uvs):
        for loop_index, uv in zip(polygon.loop_indices, polygon_uvs):
            layer.data[loop_index].uv = uv
        polygon.use_smooth = False
    add_wind_attributes(obj, roles)
    return obj


def build_geometry(source, keep_materials):
    polygons = [polygon for polygon in source.data.polygons if polygon.material_index in keep_materials]
    used = sorted({index for polygon in polygons for index in polygon.vertices})
    remap = {old: new for new, old in enumerate(used)}
    vertices = [tuple(source.data.vertices[index].co) for index in used]
    faces, uvs, roles = [], [], []
    for polygon in polygons:
        faces.append(tuple(remap[index] for index in polygon.vertices))
        # Constant-UV color palette; all geometry shares the same atlas material.
        uv = ((polygon.material_index + .5) * 128 / ATLAS_SIZE, 64 / ATLAS_SIZE)
        uvs.append([uv] * len(polygon.vertices))
        roles.append(.12 if polygon.material_index == 0 else (1. if polygon.material_index > 2 else .65))
    return vertices, faces, uvs, roles


def atlas_material(color_image, normal_image):
    material = bpy.data.materials.new("CayLua_Atlas")
    material.use_nodes = True
    material.surface_render_method = "DITHERED"
    material.use_backface_culling = False
    nodes, links = material.node_tree.nodes, material.node_tree.links
    principled = nodes.get("Principled BSDF")
    principled.inputs["Roughness"].default_value = .8
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = color_image
    links.new(texture.outputs["Color"], principled.inputs["Base Color"])
    threshold = nodes.new("ShaderNodeMath")
    threshold.operation = "GREATER_THAN"
    threshold.inputs[1].default_value = .30
    links.new(texture.outputs["Alpha"], threshold.inputs[0])
    links.new(threshold.outputs[0], principled.inputs["Alpha"])
    normal_texture = nodes.new("ShaderNodeTexImage")
    normal_texture.image = normal_image
    normal_map = nodes.new("ShaderNodeNormalMap")
    normal_map.inputs["Strength"].default_value = .7
    links.new(normal_texture.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], principled.inputs["Normal"])
    material["optional_normal_atlas"] = str(normal_image.filepath)
    return material


def export(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.hide_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = MODELS / (obj.name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_UNITS", bake_space_transform=True,
                             bake_anim=False, use_triangles=True, colors_type="LINEAR",
                             use_custom_props=True, path_mode="RELATIVE")
    announce(f"Exported {obj.name}: {count_triangles(obj)} triangles")
    return path


def render_comparison(objects):
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1100
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.world.use_nodes = True
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (.20, .24, .28, 1)
    scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = .6
    for index, obj in enumerate(objects):
        obj.hide_render = False
        obj.hide_set(False)
        obj.location.x = (index - 1) * 2.7
    source.hide_render = True
    for panicle in panicles:
        panicle.hide_render = True
    ground = mesh_object("Preview_Ground", [(-7, -4, -.015), (7, -4, -.015), (7, 4, -.015), (-7, 4, -.015)], [(0, 1, 2, 3)])
    ground_material = bpy.data.materials.new("Preview_Soil")
    ground_material.diffuse_color = (.19, .16, .12, 1)
    ground.data.materials.append(ground_material)
    light_data = bpy.data.lights.new("Preview_Sun", "SUN")
    light_data.energy = 2.3
    light = bpy.data.objects.new("Preview_Sun", light_data)
    scene.collection.objects.link(light)
    light.rotation_euler = (math.radians(28), math.radians(-25), math.radians(-35))
    camera.location = (1.8, -9.5, 3.4)
    camera.rotation_euler = (Vector((0, 0, .95)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.ortho_scale = 9.1
    scene.render.filepath = str(REPORTS / "CayLua_LOD_Comparison.png")
    announce("Rendering front comparison: LOD0, LOD1, LOD2")
    bpy.ops.render.render(write_still=True)
    camera.location = (6, -7.3, 4.7)
    camera.rotation_euler = (Vector((0, 0, .95)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(REPORTS / "CayLua_LOD_Comparison_Angle.png")
    announce("Rendering angled comparison")
    bpy.ops.render.render(write_still=True)
    for obj in objects:
        obj.location = (0, 0, 0)
    bpy.data.objects.remove(ground, do_unlink=True)
    bpy.data.objects.remove(light, do_unlink=True)


for directory in (MODELS, TEXTURES, REPORTS):
    directory.mkdir(parents=True, exist_ok=True)
source_hash = digest(SOURCE)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0
source = bpy.data.objects.get("CayLuaNhe_1Cay")
if source is None or count_triangles(source) != 5986:
    raise RuntimeError("Expected the existing approved 5,986-triangle source plant")
original_materials = list(source.data.materials)
original_polygon_materials = [polygon.material_index for polygon in source.data.polygons]
source_colors = [tuple(material.diffuse_color) for material in original_materials]
source.name = "REFERENCE_ApprovedPlant"
# The old plant has drooping leaf tips below z=0. Lift only those tips to
# the ground plane; the actual stem roots stay at the origin.
for vertex in source.data.vertices:
    if vertex.co.z < 0:
        vertex.co.z = .015
    elif vertex.co.z < .001:
        vertex.co.z = 0
source.data.update()
for obj in list(scene.objects):
    if obj != source:
        bpy.data.objects.remove(obj, do_unlink=True)
panicles = split_panicles(source)
color_materials = [emission_material("Bake_Color_" + str(i), color) for i, color in enumerate(source_colors)]
camera_data = bpy.data.cameras.new("Preview_Camera")
camera = bpy.data.objects.new("Preview_Camera", camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.data.type = "ORTHO"
scene.render.engine = "BLENDER_EEVEE"
scene.eevee.taa_render_samples = 32
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.render.image_settings.color_depth = "8"
captures_full, captures_heads = [], []
for index, angle in enumerate(ANGLES):
    captures_full.append(capture(source, angle, index * 3, color_materials))
for index, panicle in enumerate(panicles):
    for view, angle in enumerate((0, 90)):
        captures_heads.append(capture(panicle, angle, index * 3 + view + 1, color_materials))

color_atlas = np.zeros((ATLAS_SIZE, ATLAS_SIZE, 4), dtype=np.float32)
normal_atlas = np.zeros_like(color_atlas)
normal_atlas[:, :, :3] = (.5, .5, 1)
for capture_data in captures_full + captures_heads:
    tile = capture_data["tile"]
    x, y = 16 + (tile % 3) * TILE_W, 128 + (tile // 3) * TILE_H
    color_atlas[y:y + TILE_H, x:x + TILE_W, :] = dilate_rgb(capture_data["color"])
    normal_atlas[y:y + TILE_H, x:x + TILE_W, :] = dilate_rgb(capture_data["normal"])
for index, color in enumerate(source_colors):
    color_atlas[:128, index * 128:(index + 1) * 128, :] = (*color[:3], 1)
    normal_atlas[:128, index * 128:(index + 1) * 128, :] = (.5, .5, 1, 1)
color_image = save_array("CayLua_Atlas_BaseColor", color_atlas, TEXTURES / "CayLua_Atlas_BaseColor.png", "sRGB")
normal_image = save_array("CayLua_Atlas_Normal", normal_atlas, TEXTURES / "CayLua_Atlas_Normal.png", "Non-Color")
material = atlas_material(color_image, normal_image)

data0 = build_geometry(source, {0, 1, 2, 3, 4})
# add_wind_attributes uses lod0 for the common normalized height.
lod0 = mesh_object("CayLua_LOD0", data0[0], data0[1], materials=[material])
uv0 = lod0.data.uv_layers.new(name="UVMap")
for polygon, uvs in zip(lod0.data.polygons, data0[2]):
    for loop_index, uv in zip(polygon.loop_indices, uvs):
        uv0.data[loop_index].uv = uv
add_wind_attributes(lod0, data0[3])

data1 = build_geometry(source, {0, 1, 2})
for capture_data in captures_heads:
    append_cards(*data1, capture_data, segments=5)
lod1 = final_mesh("CayLua_LOD1", *data1, material)
data2 = ([], [], [], [])
for capture_data in captures_full:
    append_cards(*data2, capture_data, segments=8)
lod2 = final_mesh("CayLua_LOD2", *data2, material)

objects = (lod0, lod1, lod2)
for index, obj in enumerate(objects):
    obj["lod_level"] = index
    obj["triangle_count"] = count_triangles(obj)
    obj["root_pivot"] = "Blender (0,0,0), +Z up; exported FBX +Y up in metres"
    obj["source_asset"] = str(SOURCE)
    obj["atlas"] = color_image.name
    export(obj)

source.data.materials.clear()
for old_material in original_materials:
    source.data.materials.append(old_material)
for polygon, material_index in zip(source.data.polygons, original_polygon_materials):
    polygon.material_index = material_index
source.hide_render = True
for panicle in panicles:
    panicle.hide_render = True
render_comparison(objects)

# Separate collections permit editing one LOD at a time without overlapping.
for index, obj in enumerate(objects):
    collection = bpy.data.collections.new("LOD" + str(index))
    scene.collection.children.link(collection)
    for old_collection in list(obj.users_collection):
        old_collection.objects.unlink(obj)
    collection.objects.link(obj)
    collection.hide_render = index != 0
    collection.hide_viewport = index != 0
reference_collection = bpy.data.collections.new("REFERENCE_DoNotExport")
scene.collection.children.link(reference_collection)
for obj in (source, *panicles):
    for old_collection in list(obj.users_collection):
        old_collection.objects.unlink(obj)
    reference_collection.objects.link(obj)
reference_collection.hide_render = True
reference_collection.hide_viewport = True
camera.location = (3.2, -4.2, 2.4)
camera.rotation_euler = (Vector((0, 0, 1)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.ortho_scale = 3
for area in bpy.context.screen.areas:
    if area.type == "VIEW_3D":
        area.spaces.active.region_3d.view_location = (0, 0, 1)
        area.spaces.active.region_3d.view_distance = 3.2
        area.spaces.active.shading.type = "MATERIAL"
color_image.pack()
normal_image.pack()
scene.render.filepath = str(REPORTS / "CayLua_LOD_Comparison.png")
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))

report = {"source": str(SOURCE), "source_sha256": source_hash,
          "source_unchanged": source_hash == digest(SOURCE),
          "blender_version": bpy.app.version_string,
          "atlas_size": [ATLAS_SIZE, ATLAS_SIZE],
          "assets": [{"name": obj.name, "triangles": count_triangles(obj),
                      "vertices": len(obj.data.vertices), "materials": len(obj.data.materials),
                      "bounds": [list(corner) for corner in obj.bound_box],
                      "uv_layers": [uv.name for uv in obj.data.uv_layers],
                      "color_attributes": [color.name for color in obj.data.color_attributes]}
                     for obj in objects],
          "wind": {"R": "normalized height", "G": "flutter strength", "B": "reserved 0.5", "A": 1},
          "unity_setup": "Opaque + Alpha Clipping, Render Face Both; not Transparent alpha blending",
          "note": "No scene or existing rice asset changed. Separate Unity import audit: unity_import_report.json; rerun audit after regenerating FBX."}
(REPORTS / "model_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
announce("Finished pack; source unchanged: " + str(report["source_unchanged"]))
