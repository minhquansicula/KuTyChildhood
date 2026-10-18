import bpy,json
from pathlib import Path
from mathutils import Vector
project=Path(__file__).resolve().parents[2]
source=project.parent/'BLENDER/VienBi/1ca9a2dd15e97dae21ef7955ada009d2.obj'
out=project/'Tools/Scene3/VienBi'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=str(source),forward_axis='NEGATIVE_Z',up_axis='Y')
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
items=[]
for i,o in enumerate(sorted((o for o in bpy.context.scene.objects if o.type=='MESH'),key=lambda x:x.location.x)):
    world=[o.matrix_world@Vector(c) for c in o.bound_box]
    lo=Vector(tuple(min(v[k] for v in world) for k in range(3)))
    hi=Vector(tuple(max(v[k] for v in world) for k in range(3)))
    items.append({'name':o.name,'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'center':list((lo+hi)/2),'dimensions':list(hi-lo)})
(out/'parts_report.json').write_text(json.dumps(items,indent=2),encoding='utf-8')
print(json.dumps(items),flush=True)
