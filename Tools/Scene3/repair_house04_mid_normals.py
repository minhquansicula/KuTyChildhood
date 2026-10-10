"""Smooth curved mid-LOD shading and rebake normals through a fitted cage.

Only normal/edge-sharp attributes change. Vertex positions and triangle topology
are hashed before/after; a temporary projection cage is never exported.
"""
import bpy, math, hashlib, json
import numpy as np
from pathlib import Path
from mathutils.bvhtree import BVHTree
from mathutils import Vector

project=Path(__file__).resolve().parents[2]
folder=project/'Tools/Scene3/TencentHouse04'
blend=project.parent/'BLENDER/Nhaque4/NhaQue_04_Game.blend'
bpy.ops.wm.open_mainfile(filepath=str(blend))
high=bpy.data.objects['Nha04_Original_BakeOnly']
lods=[bpy.data.objects['Nha04_LOD'+str(i)] for i in range(3)]
target=lods[1];mesh=target.data

def geometry_hash(obj):
    co=np.empty(len(obj.data.vertices)*3,dtype=np.float32);obj.data.vertices.foreach_get('co',co)
    indices=np.empty(len(obj.data.loops),dtype=np.int32);obj.data.loops.foreach_get('vertex_index',indices)
    return hashlib.sha256(co.tobytes()+indices.tobytes()).hexdigest()

before=geometry_hash(target)
faces={}
for face in mesh.polygons:
    face.use_smooth=True
    for key in face.edge_keys:faces.setdefault(key,[]).append(face.index)
threshold=math.cos(math.radians(55))
for edge in mesh.edges:
    adjacent=faces.get(edge.key,[])
    edge.use_edge_sharp=(len(adjacent)!=2 or mesh.polygons[adjacent[0]].normal.dot(mesh.polygons[adjacent[1]].normal)<threshold)
mesh.update()
high.hide_set(False);high.hide_render=False
tree=BVHTree.FromObject(high,bpy.context.evaluated_depsgraph_get())
cage=target.copy();cage.data=mesh.copy();cage.name='Temporary_Normal_Cage'
bpy.context.collection.objects.link(cage);cage.hide_render=True
for vertex in cage.data.vertices:
    position,normal,index,distance=tree.find_nearest(vertex.co)
    vertex.co=position+normal*.015
cage.data.update()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1
scene.render.bake.use_selected_to_active=True;scene.render.bake.use_cage=True;scene.render.bake.cage_object=cage
scene.render.bake.max_ray_distance=.15;scene.render.bake.margin=16
for obj in lods:obj.hide_set(False);obj.hide_render=obj!=target
material=target.data.materials[0];nodes=material.node_tree.nodes;links=material.node_tree.links
shader=nodes.get('Principled BSDF')
normal_output=shader.inputs['Normal'].links[0].from_socket
links.remove(shader.inputs['Normal'].links[0])
image=bpy.data.images.new('Corrected_LOD1_Normal',width=1024,height=1024,alpha=True);image.colorspace_settings.name='Non-Color'
active=nodes.new('ShaderNodeTexImage');active.image=image;nodes.active=active
bpy.ops.object.select_all(action='DESELECT');high.select_set(True);target.select_set(True);bpy.context.view_layer.objects.active=target
bpy.ops.object.bake(type='NORMAL')
image.filepath_raw=str(folder/'Staging/Textures/Nha04_LOD1_Normal.png');image.file_format='PNG';image.save()
for node in nodes:
    if node!=active and node.type=='TEX_IMAGE' and node.image and node.image.name.endswith('_Normal'):node.image=image
nodes.remove(active);links.new(normal_output,shader.inputs['Normal'])
scene.render.bake.use_cage=False;scene.render.bake.cage_object=None
bpy.data.objects.remove(cage,do_unlink=True)
after=geometry_hash(target)
assert before==after,'Shading repair changed geometry'
(folder/'mid_shading_report.json').write_text(json.dumps({'geometry_hash_before':before,'geometry_hash_after':after,'unchanged_geometry':True,'smooth_angle_degrees':55,'cage_source_offset_m':.015},indent=2),encoding='utf-8')
high.hide_set(True);high.hide_render=True
bpy.ops.object.select_all(action='DESELECT')
for obj in lods:obj.hide_set(False);obj.hide_render=False;obj.select_set(True)
bpy.context.view_layer.objects.active=lods[0]
bpy.ops.export_scene.fbx(filepath=str(folder/'Staging/NhaQue_Tencent_04.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
for i,obj in enumerate(lods):obj.hide_set(i!=0);obj.hide_render=i!=0
bpy.ops.wm.save_as_mainfile(filepath=str(blend))

lods[0].hide_render=True;target.hide_set(False);target.hide_render=False
scene.cycles.samples=4;scene.cycles.use_denoising=True;scene.view_settings.view_transform='Standard'
scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.75
ld=bpy.data.lights.new('Softbox','AREA');ld.energy=2300;ld.size=8
light=bpy.data.objects.new('Softbox',ld);scene.collection.objects.link(light);light.location=(6,-6,9);light.rotation_euler=(Vector((0,0,2))-light.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=10
camera.location=(10,-13,8);camera.rotation_euler=(Vector((0,0,2.2))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(folder/'LOD1_Corrected_Front.png');bpy.ops.render.render(write_still=True)
print('MID NORMAL REPAIR PASS; GEOMETRY UNCHANGED',flush=True)
