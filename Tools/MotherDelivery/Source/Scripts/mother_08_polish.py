import bpy, bmesh
from pathlib import Path

OUT = Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
bpy.ops.wm.open_mainfile(filepath=str(OUT / 'Source/Mother.blend'))
shirt = bpy.data.objects['Mother_BlouseMesh']
if not shirt.get('delivery_edge_polish'):
    bm = bmesh.new()
    bm.from_mesh(shirt.data)
    hem = [v for v in bm.verts if v.is_boundary and .70 < v.co.z < 1.035 and abs(v.co.x) < .235]
    for v in hem:
        v.co.z = .81
    collar = [v for v in bm.verts if v.is_boundary and v.co.z > 1.24 and abs(v.co.x) < .13]
    cuffs = [v for v in bm.verts if v.is_boundary and abs(v.co.x) > .25]
    for _ in range(6):
        bmesh.ops.smooth_vert(bm, verts=collar + cuffs, factor=.45, use_axis_x=True, use_axis_y=True, use_axis_z=True)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(shirt.data)
    bm.free()
    shirt['delivery_edge_polish'] = True
hair = bpy.data.objects['Mother_HairMesh']
if not hair.get('delivery_edge_polish'):
    bm = bmesh.new()
    bm.from_mesh(hair.data)
    fringe = [v for v in bm.verts if v.is_boundary and v.co.z > 1.50 and v.co.y < -.04]
    for _ in range(10):
        bmesh.ops.smooth_vert(bm, verts=fringe, factor=.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
    bm.to_mesh(hair.data)
    bm.free()
    hair['delivery_edge_polish'] = True
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'Source/Mother.blend'))
exec(compile(Path('E:/BLENDER/mother_06_delivery.py').read_text(encoding='utf8'), 'mother_06_delivery.py', 'exec'))
exec(compile(Path('E:/BLENDER/mother_common.py').read_text(encoding='utf8'), 'mother_common.py', 'exec'))
S.render.engine = 'CYCLES'
S.cycles.samples = 16
S.render.resolution_percentage = 65
render('07_Final_Standing', (1.8,-3.6,1.6), (0,0,.82), 65)
