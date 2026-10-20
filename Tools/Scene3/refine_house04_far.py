"""Project mid/far atlases from the clean near LOD to avoid tiny source crevice hits.

Geometry and UVs are unchanged. Far shading uses mesh normals, reducing high
frequency normal aliasing on a house that occupies under 9% of screen height.
"""
import bpy
import numpy as np
from pathlib import Path
from mathutils import Vector

project=Path(__file__).resolve().parents[2]
source=project.parent/'BLENDER/Nhaque4/NhaQue_04_Game.blend'
folder=project/'Tools/Scene3/TencentHouse04'
bpy.ops.wm.open_mainfile(filepath=str(source))
near=bpy.data.objects['Nha04_LOD0']
for level,resolution in ((1,1024),(2,512)):
 far=bpy.data.objects['Nha04_LOD'+str(level)]
 for obj in bpy.context.scene.objects:
  if obj.type=='MESH':obj.hide_render=obj not in (near,far)
 near.hide_set(False);far.hide_set(False)
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1
 scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=(.02 if level==1 else .035);scene.render.bake.max_ray_distance=.15
 scene.render.bake.margin=16
 material=near.data.materials[0];nodes=material.node_tree.nodes;links=material.node_tree.links
 shader=nodes.get('Principled BSDF');surface=nodes.get('Material Output')
 emission=nodes.new('ShaderNodeEmission')
 combine=nodes.new('ShaderNodeCombineXYZ');invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1
 links.new(shader.inputs['Metallic'].links[0].from_socket,combine.inputs[0])
 links.new(shader.inputs['Roughness'].links[0].from_socket,invert.inputs[1]);links.new(invert.outputs[0],combine.inputs[1])
 far_mat=far.data.materials[0];far_nodes=far_mat.node_tree.nodes;far_links=far_mat.node_tree.links
 target=far_nodes.new('ShaderNodeTexImage');far_nodes.active=target
 images={}
 for kind in ('BaseColor','Normal','MetallicSmoothness'):
  image=bpy.data.images.new('Resolved_LOD'+str(level)+'_'+kind,width=resolution,height=resolution,alpha=True)
  image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
  target.image=image
  bpy.ops.object.select_all(action='DESELECT');near.select_set(True);far.select_set(True);bpy.context.view_layer.objects.active=far
  if kind=='Normal':
   links.new(shader.outputs[0],surface.inputs['Surface']);bpy.ops.object.bake(type='NORMAL')
  else:
   links.new(shader.inputs['Base Color'].links[0].from_socket if kind=='BaseColor' else combine.outputs[0],emission.inputs['Color'])
   links.new(emission.outputs[0],surface.inputs['Surface']);bpy.ops.object.bake(type='EMIT')
   if kind=='MetallicSmoothness':
    pixels=np.empty(resolution*resolution*4,dtype=np.float32);image.pixels.foreach_get(pixels);pixels=pixels.reshape(-1,4)
    pixels[:,3]=pixels[:,1];pixels[:,1:3]=0;image.pixels.foreach_set(pixels.ravel())
  image.filepath_raw=str(folder/'Staging/Textures'/('Nha04_LOD'+str(level)+'_'+kind+'.png'));image.file_format='PNG';image.save();images[kind]=image
 links.new(shader.outputs[0],surface.inputs['Surface']);nodes.remove(emission);nodes.remove(combine);nodes.remove(invert)
 for texture in far_nodes:
  if texture.type=='TEX_IMAGE' and texture!=target and texture.image:
   for kind,image in images.items():
    if texture.image.name.endswith('_'+kind):texture.image=image
 far_nodes.remove(target)
 far_shader=far_nodes.get('Principled BSDF')
 if level==2:
  for link in list(far_shader.inputs['Normal'].links):far_links.remove(link)
 near.hide_render=False;near.hide_set(False);far.hide_render=True;far.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(source))

near.hide_render=True;far.hide_set(False);far.hide_render=False;scene.cycles.samples=4;scene.cycles.use_denoising=True
scene.view_settings.view_transform='Standard';scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.75
ld=bpy.data.lights.new('Softbox','AREA');ld.energy=2300;ld.size=8
light=bpy.data.objects.new('Softbox',ld);scene.collection.objects.link(light);light.location=(6,-6,9);light.rotation_euler=(Vector((0,0,2))-light.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=10
for level in (1,2):
 far.hide_render=True
 far=bpy.data.objects['Nha04_LOD'+str(level)];far.hide_set(False);far.hide_render=False
 for name,position in [('Front',(10,-13,8)),('Back',(-10,13,8))]:
  camera.location=position;camera.rotation_euler=(Vector((0,0,2.2))-camera.location).to_track_quat('-Z','Y').to_euler()
  scene.render.filepath=str(folder/('LOD'+str(level)+'_Resolved_'+name+'.png'));bpy.ops.render.render(write_still=True)
print('MID AND FAR ATLASES REFINED; GEOMETRY UNCHANGED',flush=True)
