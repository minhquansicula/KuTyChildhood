import bpy
import os
import re
import sys


def clean_name(value):
    value = re.sub(r"[^A-Za-z0-9_ .-]+", "_", value or "Object")
    value = re.sub(r"\s+", "_", value).strip("_.")
    return value[:96] or "Object"


args = sys.argv[sys.argv.index("--") + 1:]
if len(args) != 2:
    raise RuntimeError("Expected input GLB and output FBX")

source = os.path.abspath(args[0])
target = os.path.abspath(args[1])
output_dir = os.path.dirname(target)
blend_path = os.path.splitext(target)[0] + ".blend"
os.makedirs(output_dir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=source, import_pack_images=True, merge_vertices=False)

# Preserve the pack hierarchy but give Unity stable, readable names.
seen = {}
for obj in bpy.data.objects:
    base = clean_name(obj.name)
    count = seen.get(base, 0)
    seen[base] = count + 1
    obj.name = base if count == 0 else f"{base}_{count:03d}"
    if obj.type == "MESH":
        obj.data.name = clean_name(obj.name + "_Mesh")

for material in bpy.data.materials:
    material.name = clean_name(material.name)

# A single root makes placement and scale predictable in Unity.
roots = [obj for obj in bpy.context.scene.objects if obj.parent is None]
unity_root = bpy.data.objects.new("RetroOfficePack_ROOT", None)
bpy.context.collection.objects.link(unity_root)
for obj in roots:
    if obj != unity_root:
        obj.parent = unity_root

bpy.ops.wm.save_as_mainfile(filepath=blend_path, compress=True)

bpy.ops.object.select_all(action="SELECT")
bpy.context.view_layer.objects.active = unity_root
bpy.ops.export_scene.fbx(
    filepath=target,
    use_selection=True,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_UNITS",
    axis_forward="-Z",
    axis_up="Y",
    object_types={"EMPTY", "MESH"},
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    use_tspace=True,
    add_leaf_bones=False,
    bake_anim=False,
    path_mode="COPY",
    embed_textures=True,
)

mesh_count = len([o for o in bpy.data.objects if o.type == "MESH"])
vertex_count = sum(len(o.data.vertices) for o in bpy.data.objects if o.type == "MESH")
triangle_count = sum(len(o.data.loop_triangles) for o in bpy.data.objects if o.type == "MESH")
print(f"KUTY_CONVERT_OK meshes={mesh_count} vertices={vertex_count} triangles={triangle_count}")
print(f"KUTY_FBX={target}")
print(f"KUTY_BLEND={blend_path}")
