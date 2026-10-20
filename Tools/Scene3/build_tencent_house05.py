"""Reuse the validated house-04 pipeline with independent house-05 inputs/outputs.

Blender stages: prepare, bake, refine, shading, verify.
Plain Python stage: publish.
The original OBJ and PBR textures are never modified.
"""
from pathlib import Path
import sys

folder=Path(__file__).resolve().parent
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
stage=args[0]
scripts={'prepare':'prepare_tencent_house04.py','bake':'bake_tencent_house04.py',
    'refine':'refine_house04_far.py','shading':'finalize_house04_shading.py',
    'verify':'verify_house04_silhouette.py','publish':'install_house04_assets.py'}
script=(folder/scripts[stage]).read_text(encoding='utf-8')
for before,after in [('TencentHouse04','TencentHouse05'),('Nhaque4','NhaQue5'),
    ('NhaQue_04','NhaQue_05'),('NhaQue_Tencent_04','NhaQue_Tencent_05'),('Nha04','Nha05'),
    ('2f79e2974f1488051a3e23b79cf6144f.obj','2d4563d05713d86596e74567977ff461.obj')]:
    script=script.replace(before,after)
if stage=='shading':
    # Smooth roof curves without changing a vertex; keep sharp wall/roof edges.
    anchor='for obj in lods[1:]:'
    addition='''import math, hashlib, json
import numpy as np
target=lods[1];mesh=target.data
def mesh_hash(obj):
    co=np.empty(len(obj.data.vertices)*3,dtype=np.float32);obj.data.vertices.foreach_get('co',co)
    indices=np.empty(len(obj.data.loops),dtype=np.int32);obj.data.loops.foreach_get('vertex_index',indices)
    return hashlib.sha256(co.tobytes()+indices.tobytes()).hexdigest()
before=mesh_hash(target)
faces={}
for face in mesh.polygons:
    face.use_smooth=True
    for key in face.edge_keys:faces.setdefault(key,[]).append(face.index)
threshold=math.cos(math.radians(55))
for edge in mesh.edges:
    adjacent=faces.get(edge.key,[])
    edge.use_edge_sharp=(len(adjacent)!=2 or mesh.polygons[adjacent[0]].normal.dot(mesh.polygons[adjacent[1]].normal)<threshold)
mesh.update()
assert mesh_hash(target)==before
(folder/'mid_shading_report.json').write_text(json.dumps({'positions_and_topology_unchanged':True,'sha256':before,'sharp_angle_degrees':55},indent=2))
'''
    script=script.replace(anchor,addition+'\n'+anchor)
    # Export the final shading attributes; only the three low meshes go to Unity.
    anchor='bpy.ops.wm.save_as_mainfile(filepath=str(blend))'
    addition='''bpy.ops.object.select_all(action='DESELECT')
for obj in lods:obj.hide_set(False);obj.select_set(True)
bpy.context.view_layer.objects.active=lods[0]
bpy.ops.export_scene.fbx(filepath=str(folder/'Staging/NhaQue_Tencent_05.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
for obj in lods[1:]:obj.hide_set(True)
'''
    script=script.replace(anchor,addition+'\n'+anchor)
if stage=='publish':
    # Publishing is allowed only while all original source files still match.
    script=script.replace('assert len(silhouette) == 12', '''import hashlib
source=project.parent/'BLENDER/NhaQue5'
assert all(hashlib.sha256((source/name).read_bytes()).hexdigest()==sha for name,sha in geometry['source_assets_sha256'].items())
assert len(silhouette) == 12''')
exec(compile(script,str(folder/scripts[stage]),'exec'),{'__file__':str(folder/scripts[stage]),'__name__':'__main__'})
