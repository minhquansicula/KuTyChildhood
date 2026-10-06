"""Build five lightweight rural Vietnamese house models for Scene 3.

Run with Blender 5.x: blender -b -t 2 --python Tools/Scene3/generate_rural_houses.py
The FBX files go to Art/Models/Scene3/RuralHouses; the editable Blender
source stays beside this generator to avoid importing all houses twice.
"""

import math
import os
import random
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "_Project" / "Art" / "Models" / "Scene3" / "RuralHouses"
OUT.mkdir(parents=True, exist_ok=True)
TEXTURES = ROOT / "Assets" / "_Project" / "Art" / "Textures" / "Scene3" / "RuralHouses"
TEXTURES.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)


def weathered_texture(name, rgb):
    """A subtle, seamless colour map so the houses do not look newly painted."""
    def srgb_channel(linear):
        linear = min(1, max(0, linear))
        return 12.92 * linear if linear <= .0031308 else 1.055 * linear ** (1 / 2.4) - .055

    size = 256
    image = bpy.data.images.new(name + "_Weathered", width=size, height=size, alpha=True)
    values = [0.0] * (size * size * 4)
    rng = random.Random(sum((i + 1) * ord(char) for i, char in enumerate(name)))
    is_tile = "Tile" in name
    is_tin = "Tin" in name
    for y in range(size):
        v = y / size
        for x in range(size):
            u = x / size
            broad = math.sin(2 * math.pi * (u * 2 + v * 3)) * .045
            broad += math.sin(2 * math.pi * (u * 5 - v * 4)) * .024
            grain = (rng.random() - .5) * (.075 if is_tile else .045)
            shade = 1 + broad + grain
            if is_tile:
                course = (u * 8) % 1
                joint = (v * 8 + .5 * (int(u * 8) % 2)) % 1
                if course < .055 or joint < .025:
                    shade *= .72
                else:
                    shade *= .94 + .06 * math.sin(math.pi * joint)
                if math.sin(2 * math.pi * (u * 3 + v)) * math.sin(2 * math.pi * (v * 5 - u)) > .72:
                    shade *= .78  # moss/dirt on a few old tiles
            elif is_tin:
                shade *= .93  # faded paint and dulled galvanizing
            elif "Wall" in name or "wash" in name or "Lime" in name or "Earth" in name:
                if math.sin(2 * math.pi * (u * 4 + v)) * math.sin(2 * math.pi * (v * 3 - u)) > .67:
                    shade *= .83
            idx = 4 * (y * size + x)
            colour = [c * shade for c in rgb]
            if is_tin:
                # Uneven oxide patches, rather than a uniform brown roof.
                blotch = .55 * math.sin(2 * math.pi * (u * 3 + v))
                blotch += .45 * math.sin(2 * math.pi * (v * 4 - u * 2))
                blotch += .18 * math.sin(2 * math.pi * (u * 13 + v * 9))
                rust = min(.38, max(0, (blotch - .38) * .9))
                oxide = (.39, .16, .065) if "Galvanized" in name else (.28, .11, .045)
                colour = [(1 - rust) * c + rust * oxide[i] for i, c in enumerate(colour)]
            values[idx:idx + 4] = [srgb_channel(c) for c in colour] + [1.0]
    image.pixels.foreach_set(values)
    image.filepath_raw = str(TEXTURES / (name + "_Weathered.png"))
    image.file_format = "PNG"
    image.save()
    return image


def material(name, rgb, roughness=0.82, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = (*rgb, 1)
    node.inputs["Roughness"].default_value = roughness
    node.inputs["Metallic"].default_value = metallic
    image = weathered_texture(name, rgb)
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = image
    tex.interpolation = "Linear"
    mat.node_tree.links.new(tex.outputs["Color"], node.inputs["Base Color"])
    return mat


M = {
    "lime": material("Limewash_Cream", (0.59, 0.53, 0.35)),
    "white": material("Whitewash_Warm", (0.69, 0.66, 0.53)),
    "ochre": material("Ochre_Earth", (0.51, 0.36, 0.22)),
    "clay": material("Clay_Wall", (0.44, 0.31, 0.21)),
    "moss": material("Moss_Lime", (0.49, 0.53, 0.38)),
    "tile": material("Roof_Clay_Tile", (0.39, 0.14, 0.09)),
    "tile_dark": material("Roof_Old_Tile", (0.24, 0.20, 0.17)),
    "tile_moss": material("Roof_Mossy_Tile", (0.34, 0.29, 0.18)),
    "tin": material("Roof_Galvanized_Tin", (0.31, 0.35, 0.35), 0.59, 0.39),
    "tin_red": material("Roof_Faded_Red_Tin", (0.36, 0.20, 0.17), 0.65, 0.35),
    "timber": material("Dark_Timber", (0.22, 0.15, 0.10)),
    "bamboo": material("Bamboo", (0.44, 0.37, 0.20)),
    "brick": material("Old_Brick", (0.40, 0.22, 0.15)),
    "stone": material("Stone_Plaster", (0.44, 0.43, 0.36)),
    "glass": material("Window_Dark", (0.10, 0.16, 0.16), 0.23),
}


def cube(name, center, size, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mat)
    return ob


def cylinder(name, center, radius, depth, mat, vertices=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=center)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mat)
    return ob


def mesh(name, vertices, faces, mat):
    me = bpy.data.meshes.new(name)
    me.from_pydata(vertices, [], faces)
    me.update()
    uv = me.uv_layers.new(name="UVMap")
    for polygon in me.polygons:
        for loop_index in polygon.loop_indices:
            co = me.vertices[me.loops[loop_index].vertex_index].co
            if name == "Main_clay_tile_roof":
                uv.data[loop_index].uv = (co.x / 3.0, co.y / 3.0)
            elif name == "Upper_gable":
                uv.data[loop_index].uv = (co.x / 1.4, co.z / 1.4)
            else:
                uv.data[loop_index].uv = (co.x / 1.25, co.y / 1.25)
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    return ob


def gable(name, y, width, wall_top, rise, mat):
    vertices = [(-width / 2, y, wall_top), (width / 2, y, wall_top),
                (0, y, wall_top + rise)]
    return mesh(name, vertices, [(0, 1, 2), (2, 1, 0)], mat)


def roof(name, width, depth, wall_top, rise, overhang, mat, waves=18, amplitude=0.018):
    """Real corrugated geometry; ridges follow the roof slope."""
    half_w = width / 2 + overhang
    half_d = depth / 2 + overhang
    z_eave = wall_top - 0.02
    z_ridge = wall_top + rise
    verts = []
    faces = []
    count_y = waves * 6
    count_x = 8
    for side in (-1, 1):
        base = len(verts)
        for layer in (0, 1):
            for ix in range(count_x + 1):
                t = ix / count_x
                x = side * half_w * t
                slope_z = z_ridge + (z_eave - z_ridge) * t
                for iy in range(count_y + 1):
                    y = -half_d + depth * (1 + 2 * overhang / depth) * iy / count_y
                    ripple = amplitude * math.cos(2 * math.pi * waves * iy / count_y)
                    verts.append((x, y, slope_z + ripple - layer * 0.035))
        row = count_y + 1
        plane = (count_x + 1) * row
        for ix in range(count_x):
            for iy in range(count_y):
                a = base + ix * row + iy
                b = a + row
                faces.extend(((a, a + 1, b + 1, b),
                              (a + plane, b + plane, b + plane + 1, a + plane + 1)))
        for ix in range(count_x):
            for iy in (0, count_y):
                a = base + ix * row + iy
                b = a + row
                faces.append((a, b, b + plane, a + plane))
        for iy in range(count_y):
            for ix in (0, count_x):
                a = base + ix * row + iy
                faces.append((a, a + plane, a + plane + 1, a + 1))
    return mesh(name, verts, faces, mat)


def sloped_sheet(name, width, front_y, back_y, front_z, back_z, mat):
    """Porch sheet made from joined, extruded semicircular corrugations."""
    verts = []
    faces = []
    arch_count = max(12, round(width / .18))
    radius = width / (2 * arch_count)
    segments_per_arch = 8
    profile = []
    for arch in range(arch_count):
        left = -width / 2 + 2 * radius * arch
        for segment in range(segments_per_arch + 1):
            if arch and segment == 0:
                continue  # neighboring half circles share their valley vertex
            angle = math.pi * segment / segments_per_arch
            profile.append((left + radius * (1 - math.cos(angle)),
                            radius * math.sin(angle)))
    count = len(profile) - 1
    thickness = .006
    for layer in (0, 1):
        for j in range(2):
            y = front_y if j == 0 else back_y
            z = front_z if j == 0 else back_z
            for x, wave_z in profile:
                verts.append((x, y, z + wave_z - thickness * layer))
    row = count + 1
    for i in range(count):
        faces.extend(((i, i + 1, row + i + 1, row + i),
                      (2 * row + i, 3 * row + i, 3 * row + i + 1, 2 * row + i + 1)))
    for i in range(count):
        faces.extend(((i, 2 * row + i, 2 * row + i + 1, i + 1),
                      (row + i, row + i + 1, 3 * row + i + 1, 3 * row + i)))
    faces.extend(((0, row, 3 * row, 2 * row),
                  (count, 2 * row + count, 3 * row + count, row + count)))
    return mesh(name, verts, faces, mat)


def window(prefix, x, y, z, width, height, shutter=False):
    cube(prefix + "_recess", (x, y, z), (width, 0.035, height), M["glass"])
    for s in (-1, 1):
        cube(prefix + "_vertical", (x + s * width / 2, y - 0.04, z),
             (0.075, 0.1, height + 0.11), M["timber"])
    for s in (-1, 1):
        cube(prefix + "_horizontal", (x, y - 0.04, z + s * height / 2),
             (width + 0.12, 0.1, 0.075), M["timber"])
    cube(prefix + "_mullion", (x, y - 0.055, z), (0.06, 0.07, height), M["timber"])
    if shutter:
        for s in (-1, 1):
            cube(prefix + "_shutter", (x + s * (width * 0.78), y - 0.025, z),
                 (width * 0.46, 0.06, height * 0.93), M["timber"])


def front_door(y, z, width, height, split=False):
    cube("Door_recess", (0, y, z), (width, 0.04, height), M["timber"])
    for x in (-width / 2, width / 2):
        cube("Door_jamb", (x, y - 0.055, z), (0.08, 0.11, height + 0.1), M["brick"])
    cube("Door_lintel", (0, y - 0.055, z + height / 2),
         (width + 0.16, 0.11, 0.08), M["brick"])
    if split:
        cube("Door_divider", (0, y - 0.065, z), (0.055, 0.06, height), M["bamboo"])
    else:
        cylinder("Door_handle", (width * 0.33, y - 0.11, z), 0.035, 0.05, M["bamboo"])


SPECS = [
    dict(name="Nha01_NgoiDo_HienTon", w=5.8, d=5.4, h=3.1, rise=1.48,
         wall="lime", roof="tile", porch=1.65, trim="brick", shutter=True,
         side=False, split=False, piers=False, porch_roof="tin_red"),
    dict(name="Nha02_NgoiNau_HienTon", w=4.9, d=6.2, h=2.8, rise=1.17,
         wall="white", roof="tile_dark", porch=1.3, trim="stone", shutter=False,
         side=True, split=False, piers=False, porch_roof="tin"),
    dict(name="Nha03_NgoiReu_HienTon", w=4.5, d=5.1, h=2.75, rise=1.8,
         wall="clay", roof="tile_moss", porch=1.2, trim="bamboo", shutter=True,
         side=False, split=True, piers=True, porch_roof="tin"),
    dict(name="Nha04_NgoiDoSam_HienTon", w=6.7, d=5.1, h=3.05, rise=1.08,
         wall="ochre", roof="tile", porch=1.85, trim="brick", shutter=False,
         side=True, split=True, piers=False, porch_roof="tin_red"),
    dict(name="Nha05_NgoiCu_HienTon", w=6.1, d=6.0, h=2.9, rise=1.35,
         wall="moss", roof="tile_dark", porch=1.45, trim="stone", shutter=True,
         side=False, split=True, piers=False, porch_roof="tin"),
]


def create_house(s):
    before = set(bpy.data.objects)
    w, d, h, rise = s["w"], s["d"], s["h"], s["rise"]
    front = -d / 2
    raised = 0.48 if s["piers"] else 0.18
    wall = M[s["wall"]]
    roof_mat = M[s["roof"]]
    trim = M[s["trim"]]
    cube("Plinth", (0, 0, raised / 2), (w + 0.2, d + 0.2, raised), trim)
    if s["piers"]:
        for x in (-w * 0.42, w * 0.42):
            for y in (-d * 0.42, d * 0.42):
                cube("Raised_pier", (x, y, raised / 2), (0.25, 0.25, raised), M["stone"])
        cube("Raised_floor", (0, 0, raised), (w + 0.28, d + 0.25, 0.13), M["timber"])
    wall_base = raised + 0.07
    wall_center = wall_base + h / 2
    for y in (front, d / 2):
        cube("Gable_wall", (0, y, wall_center), (w, 0.19, h), wall)
        gable("Upper_gable", y, w, wall_base + h, rise, wall)
    for x in (-w / 2, w / 2):
        cube("Side_wall", (x, 0, wall_center), (0.19, d, h), wall)
        cube("Bottom_band", (x, 0, wall_base + 0.24), (0.22, d + 0.03, 0.4), trim)
    cube("Front_bottom_band", (0, front - 0.08, wall_base + 0.24),
         (w + 0.08, 0.09, 0.4), trim)

    door_h = min(2.13, h - 0.25)
    front_door(front - 0.12, wall_base + door_h / 2, 1.1 if w < 5 else 1.35,
               door_h, s["split"])
    for x in (-w * 0.32, w * 0.32):
        window("Front_window", x, front - 0.12, wall_base + 1.72, 0.82, 0.87, s["shutter"])
    for x, sign in ((-w / 2, -1), (w / 2, 1)):
        cube("Side_window", (x + sign * 0.13, d * 0.12, wall_base + 1.7),
             (0.04, 0.94, 0.77), M["glass"])
        for y in (d * 0.12 - 0.48, d * 0.12 + 0.48):
            cube("Side_window_frame", (x + sign * 0.15, y, wall_base + 1.7),
                 (0.07, 0.07, 0.85), M["timber"])

    roof("Main_clay_tile_roof", w, d, wall_base + h, rise, 0.34,
         roof_mat, 22, 0.013)
    cube("Ridge_cap", (0, 0, wall_base + h + rise + 0.01),
         (0.14, d + 0.72, 0.09), roof_mat)
    cube("Front_fascia", (0, front - 0.34, wall_base + h - 0.015),
         (w + 0.73, 0.055, 0.11), M["timber"])

    porch = s["porch"]
    porch_center = front - porch / 2 - 0.06
    cube("Porch_floor", (0, porch_center, raised + 0.02),
         (w + 0.34, porch + 0.32, 0.16), trim)
    cube("Front_step", (0, front - porch - 0.38, raised * 0.45),
         (w * 0.48, 0.36, max(0.12, raised * 0.55)), M["stone"])
    post_y = front - porch + 0.02
    porch_top = wall_base + h * 0.79
    for x in (-w * 0.44, w * 0.44):
        if s["piers"]:
            cylinder("Bamboo_porch_post", (x, post_y, (raised + porch_top) / 2),
                     0.075, porch_top - raised, M["bamboo"], 7)
        else:
            cube("Porch_post", (x, post_y, (raised + porch_top) / 2),
                 (0.13, 0.13, porch_top - raised), M["timber"])
    cube("Porch_beam", (0, post_y, porch_top),
         (w * 0.96, 0.12, 0.14), M["timber"])
    sloped_sheet("Porch_roof", w + 0.5, post_y - 0.2, front + 0.26,
                 porch_top + 0.09, porch_top + 0.43, M[s["porch_roof"]])

    if s["side"]:
        side_x = w / 2 + 0.9
        cube("Side_shed_floor", (side_x, d * 0.18, raised / 2),
             (1.9, d * 0.55, raised), trim)
        for y in (-d * 0.08, d * 0.44):
            cube("Side_shed_post", (w / 2 + 1.65, y, (raised + h * 0.68) / 2),
                 (0.1, 0.1, h * 0.68 - raised), M["timber"])
        cube("Side_shed_roof", (side_x, d * 0.18, raised + h * 0.73),
             (2.2, d * 0.63, 0.07), M[s["porch_roof"]])

    if s["piers"]:
        for x in (-w * 0.43, w * 0.43):
            for i in range(6):
                cube("Bamboo_wall_batten", (x, front - 0.15,
                     wall_base + 0.5 + i * 0.36), (0.045, 0.05, 0.25), M["bamboo"])
    if s["name"].startswith("Nha05"):
        for x in (-w * 0.45, w * 0.45):
            cube("Brick_front_planter", (x, post_y - 0.18, raised + 0.22),
                 (0.5, 0.5, 0.34), M["brick"])

    created = [o for o in bpy.data.objects if o not in before]
    root = bpy.data.objects.new(s["name"], None)
    bpy.context.collection.objects.link(root)
    for ob in created:
        ob.parent = root
        ob.matrix_parent_inverse = root.matrix_world.inverted()
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for ob in created:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=str(OUT / (s["name"] + ".fbx")),
                             use_selection=True, object_types={"EMPTY", "MESH"},
                             axis_forward="-Z", axis_up="Y", apply_unit_scale=True,
                             use_mesh_modifiers=True, mesh_smooth_type="OFF",
                             add_leaf_bones=False, bake_anim=False, path_mode="AUTO")
    for ob in created:
        ob.hide_render = True
        ob.hide_set(True)
    root.hide_render = True
    root.hide_set(True)
    return root


for spec in SPECS:
    create_house(spec)

for ob in bpy.data.objects:
    ob.hide_render = False
    ob.hide_set(False)

bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).resolve().parent / "NamNhaQue_Source.blend"))
print("Created 5 rural houses in", OUT)
