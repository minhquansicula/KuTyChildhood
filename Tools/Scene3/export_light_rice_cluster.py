"""Export a nine-plant field cluster from the approved lightweight Blender model."""
import bpy
import random
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
source = root.parent / "BLENDER/caylua_nhe.blend"
target = root / "Assets/_Project/Art/Models/Scene3/CayLuaNhe_Cum3x3.fbx"

bpy.ops.wm.open_mainfile(filepath=str(source))
plant = bpy.data.objects.get("CayLuaNhe_1Cay")
if not plant or plant.get("triangle_count") != 5986:
    raise RuntimeError("Expected the 5,986-triangle approved rice plant")

for obj in list(bpy.data.objects):
    if obj != plant:
        bpy.data.objects.remove(obj, do_unlink=True)

rng = random.Random(9321)
copies = []
for row in range(3):
    for col in range(3):
        obj = plant.copy()
        obj.data = plant.data.copy()
        bpy.context.scene.collection.objects.link(obj)
        obj.name = f"CayLua_{row}_{col}"
        obj.location = Vector(((col-1)*1.02 + rng.uniform(-.11,.11),
                               (row-1)*1.02 + rng.uniform(-.11,.11), 0))
        obj.rotation_euler.z = rng.uniform(-.30,.30)
        scale = rng.uniform(.90, 1.08)
        obj.scale = (scale,scale,scale)
        copies.append(obj)

bpy.data.objects.remove(plant, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
for obj in copies:
    obj.select_set(True)
bpy.context.view_layer.objects.active = copies[0]
bpy.ops.object.join()
cluster = bpy.context.object
cluster.name = "CayLuaNhe_Cum3x3"
cluster.data.name = "CayLuaNhe_Cum3x3_Mesh"
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
cluster.location = (0,0,0)

target.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(target), use_selection=True,
                         object_types={"MESH"}, apply_unit_scale=True,
                         axis_forward="-Z", axis_up="Y", bake_anim=False)
triangles = sum(len(p.vertices)-2 for p in cluster.data.polygons)
print(f"[RICE CLUSTER] {triangles} triangles, {len(cluster.data.materials)} materials, saved {target}")
