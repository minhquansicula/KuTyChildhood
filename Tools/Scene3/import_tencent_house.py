"""Create Unity LOD meshes from the downloaded house without changing its OBJ.

Run with Blender in background mode. Original maps stay in BLENDER; Unity limits
their imported resolution. The FBX contains only meshes, never preview lights.
"""
import bpy
import json
import math
import shutil
from pathlib import Path
from mathutils import Vector

project = Path(__file__).resolve().parents[2]
source = project.parent / 'BLENDER'
output = project / 'Assets/_Project/Art/Models/Scene3/TencentHouse01'
textures = project / 'Assets/_Project/Art/Textures/Scene3/TencentHouse01'
report = project / 'Tools/Scene3/TencentHouse01'
for folder in (output, textures, report):
    folder.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=str(source/'531c2c5d7c5e6e14567ea9087e4e6b2b.obj'), forward_axis='NEGATIVE_Z', up_axis='Y')
mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
if len(mesh_objects) != 1:
    raise RuntimeError('Expected one downloaded house mesh')
house = mesh_objects[0]
bpy.context.view_layer.objects.active = house
house.select_set(True)
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
original_triangles = len(house.data.polygons)
coords = [v.co.copy() for v in house.data.vertices]
minimum = Vector(tuple(min(v[i] for v in coords) for i in range(3)))
maximum = Vector(tuple(max(v[i] for v in coords) for i in range(3)))
factor = 7.2 / (maximum.x-minimum.x)
centre = Vector(((maximum.x+minimum.x)/2, (maximum.y+minimum.y)/2, minimum.z))
for vertex in house.data.vertices:
    vertex.co = (vertex.co-centre)*factor
house.location = (0,0,0)
house.data.update()
del coords

names = {'texture_pbr_20250901.png':'Nha01_BaseColor.png',
         'texture_pbr_20250901_normal.png':'Nha01_Normal.png',
         'texture_pbr_20250901_roughness.png':'Nha01_Roughness.png',
         'texture_pbr_20250901_metallic.png':'Nha01_Metallic.png'}
for original, name in names.items():
    shutil.copy2(source/original, textures/name)

material = bpy.data.materials.new('Nha01_Tencent_PBR')
material.use_nodes = True
nodes = material.node_tree.nodes
links = material.node_tree.links
bsdf = nodes.get('Principled BSDF')
for filename, socket, noncolor in [('Nha01_BaseColor.png','Base Color',False),
                                  ('Nha01_Roughness.png','Roughness',True),
                                  ('Nha01_Metallic.png','Metallic',True)]:
    image = bpy.data.images.load(str(textures/filename), check_existing=True)
    if noncolor: image.colorspace_settings.name = 'Non-Color'
    node = nodes.new('ShaderNodeTexImage');node.image=image
    links.new(node.outputs['Color'],bsdf.inputs[socket])
normal_image=bpy.data.images.load(str(textures/'Nha01_Normal.png'),check_existing=True)
normal_image.colorspace_settings.name='Non-Color'
normal_texture=nodes.new('ShaderNodeTexImage');normal_texture.image=normal_image
normal_map=nodes.new('ShaderNodeNormalMap');normal_map.inputs['Strength'].default_value=.65
links.new(normal_texture.outputs['Color'],normal_map.inputs['Color'])
links.new(normal_map.outputs['Normal'],bsdf.inputs['Normal'])
house.data.materials.clear();house.data.materials.append(material)

# Pack metallic R and inverse roughness A for the URP/Lit metallic workflow.
# These are numerical material channels, not colour images.
import numpy as np
metal_image=bpy.data.images.load(str(textures/'Nha01_Metallic.png'),check_existing=True)
rough_image=bpy.data.images.load(str(textures/'Nha01_Roughness.png'),check_existing=True)
width,height=metal_image.size
metal=np.empty(width*height*4,dtype=np.float32);metal_image.pixels.foreach_get(metal)
rough=np.empty(width*height*4,dtype=np.float32);rough_image.pixels.foreach_get(rough)
packed=np.zeros((width*height,4),dtype=np.float32)
packed[:,0]=metal.reshape(-1,4)[:,0]
packed[:,3]=1-rough.reshape(-1,4)[:,0]
packed_image=bpy.data.images.new('Nha01_MetallicSmoothness',width=width,height=height,alpha=True)
packed_image.colorspace_settings.name='Non-Color'
packed_image.pixels.foreach_set(packed.ravel())
packed_image.filepath_raw=str(textures/'Nha01_MetallicSmoothness.png');packed_image.file_format='PNG';packed_image.save()
del metal,rough,packed

def simplify(obj,target):
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
    mod=obj.modifiers.new('Game mesh reduction','DECIMATE')
    mod.ratio=min(1,target/len(obj.data.polygons));mod.use_collapse_triangulate=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in obj.data.polygons:poly.use_smooth=True
    obj.data.update()

print('Reducing original',original_triangles,'triangles to 30000',flush=True)
simplify(house,30000);house.name='Nha01_LOD0';house.data.name='Nha01_LOD0_Mesh'
lods=[house]
for i,target in enumerate((12000,3000),start=1):
    obj=house.copy();obj.data=house.data.copy();bpy.context.collection.objects.link(obj)
    obj.name='Nha01_LOD'+str(i);obj.data.name=obj.name+'_Mesh'
    simplify(obj,target);lods.append(obj)

bpy.ops.object.select_all(action='DESELECT')
for obj in lods:obj.select_set(True)
bpy.context.view_layer.objects.active=house
bpy.ops.export_scene.fbx(filepath=str(output/'NhaQue_Tencent_01.fbx'),use_selection=True,
                        object_types={'MESH'},axis_forward='-Z',axis_up='Y',
                        apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',
                        use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,
                        path_mode='STRIP',embed_textures=False)

stats={'source_triangles':original_triangles,'lod_triangles':[len(o.data.polygons) for o in lods],
       'dimensions_metres':list(house.dimensions),'width_metres':7.2,
       'fbx':str(output/'NhaQue_Tencent_01.fbx'),'source_unchanged':str(source/'531c2c5d7c5e6e14567ea9087e4e6b2b.obj')}
(report/'mesh_report.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
for obj in lods[1:]:obj.hide_render=True;obj.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(source/'NhaQue_Tencent_01_ToiUu.blend'))

# Four small previews establish which side has the entrance before scene placement.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
scene.cycles.use_denoising=True;scene.render.resolution_x=800;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral preview');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.65,.65,.65,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.6
scene.view_settings.view_transform='Standard'
light_data=bpy.data.lights.new('Preview softbox','AREA');light_data.energy=1800;light_data.shape='DISK';light_data.size=9
light=bpy.data.objects.new('Preview softbox',light_data);scene.collection.objects.link(light);light.location=(4,-6,11)
light.rotation_euler=(Vector((0,0,2.5))-light.location).to_track_quat('-Z','Y').to_euler()
camera_data=bpy.data.cameras.new('Preview camera');camera=bpy.data.objects.new('Preview camera',camera_data)
scene.collection.objects.link(camera);scene.camera=camera;camera_data.lens=40
for name,position in [('Front',(9,-13,8)),('Back',(-9,13,8))]:
    camera.location=position;camera.rotation_euler=(Vector((0,0,2.3))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(report/('Preview_'+name+'.png'));bpy.ops.render.render(write_still=True)
print(json.dumps(stats),flush=True)

# Collapse changes UV interpolation across wall triangles. Always rebuild the
# atlases from the original before this FBX is used in Unity.
import runpy
runpy.run_path(str(Path(__file__).with_name('repair_tencent_house.py')),run_name='__main__')
