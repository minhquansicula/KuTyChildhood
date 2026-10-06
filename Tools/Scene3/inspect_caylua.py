"""Print bounds and material usage of the artist-provided rice FBX."""
import bpy
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = root / "Assets/_Project/Art/Models/Scene3/caylua.fbx"
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(source))
for obj in bpy.context.scene.objects:
    if obj.type != "MESH":
        continue
    mesh = obj.data
    print(f"[RICE] {obj.name}: verts={len(mesh.vertices)} faces={len(mesh.polygons)} tris={sum(len(p.vertices)-2 for p in mesh.polygons)}")
    print(f"[RICE] dimensions={tuple(obj.dimensions)} location={tuple(obj.location)}")
    for slot in obj.material_slots:
        mat = slot.material
        print(f"[RICE] material={mat.name if mat else 'None'} diffuse={tuple(mat.diffuse_color) if mat else 'None'}")
