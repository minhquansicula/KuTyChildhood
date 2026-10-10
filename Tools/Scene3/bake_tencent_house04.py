"""Bake independent UV atlases from the original; never modify LOD geometry."""
import bpy,json,math
import numpy as np
from pathlib import Path
from mathutils import Vector

project=Path(__file__).resolve().parents[2];source=project.parent/'BLENDER/Nhaque4'
report=project/'Tools/Scene3/TencentHouse04'
textures=report/'Staging/Textures'
model=report/'Staging/NhaQue_Tencent_04.fbx'
textures.mkdir(parents=True,exist_ok=True);model.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source/'NhaQue_04_Prepare.blend'))
high=bpy.data.objects['Nha04_Original_BakeOnly'];lods=[bpy.data.objects['Nha04_LOD'+str(i)] for i in range(3)]
source_mat=high.data.materials[0];nodes=source_mat.node_tree.nodes;links=source_mat.node_tree.links
# Keep the newly generated weathered colour atlas unchanged.
# Clamp fresh gloss using baked roughness only, with no runtime noise.
rough=nodes.new('ShaderNodeMath');rough.operation='MAXIMUM';rough.inputs[1].default_value=.72
shader=nodes.get('Principled BSDF')
links.new(shader.inputs['Roughness'].links[0].from_socket,rough.inputs[0]);links.new(rough.outputs[0],shader.inputs['Roughness'])
shader=nodes.get('Principled BSDF');surface=nodes.get('Material Output')
emission=nodes.new('ShaderNodeEmission');combine=nodes.new('ShaderNodeCombineXYZ')
invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1
links.new(shader.inputs['Metallic'].links[0].from_socket,combine.inputs['X'])
links.new(shader.inputs['Roughness'].links[0].from_socket,invert.inputs[1]);links.new(invert.outputs[0],combine.inputs['Y'])
links.new(combine.outputs[0],emission.inputs['Color'])
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1
scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.08;scene.render.bake.max_ray_distance=.25
scene.render.bake.margin=12;scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
for i,obj in enumerate(lods):
 # Flat split normals give each tile/wall a stable projection direction.
 # The tangent normal bake then restores original shading without moving vertices.
 for face in obj.data.polygons:face.use_smooth=False
 scene.render.bake.cage_extrusion=(.025,.05,.05)[i]
 scene.render.bake.max_ray_distance=.15
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
 bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.008,area_weight=.5)
 bpy.ops.object.mode_set(mode='OBJECT')
 material=bpy.data.materials.new(obj.name+'_PBR');material.use_nodes=True
 obj.data.materials.clear();obj.data.materials.append(material)
 texture=material.node_tree.nodes.new('ShaderNodeTexImage');material.node_tree.nodes.active=texture
 resolution=(2048,1024,512)[i];images={}
 for kind in ('BaseColor','Normal','MetallicSmoothness'):
  image=bpy.data.images.new(obj.name+'_'+kind,width=resolution,height=resolution,alpha=True)
  image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
  images[kind]=image;texture.image=image
  bpy.ops.object.select_all(action='DESELECT');high.select_set(True);obj.select_set(True);bpy.context.view_layer.objects.active=obj
  for other in lods:other.hide_render=other!=obj
  high.hide_render=False
  print('Bake',obj.name,kind,resolution,flush=True)
  if kind=='MetallicSmoothness':
   links.new(emission.outputs[0],surface.inputs['Surface']);bpy.ops.object.bake(type='EMIT')
   pixels=np.empty(resolution*resolution*4,dtype=np.float32);image.pixels.foreach_get(pixels);pixels=pixels.reshape(-1,4)
   pixels[:,3]=pixels[:,1];pixels[:,1:3]=0;image.pixels.foreach_set(pixels.ravel())
   links.new(shader.outputs[0],surface.inputs['Surface'])
  elif kind=='BaseColor':
   albedo=nodes.new('ShaderNodeEmission')
   links.new(shader.inputs['Base Color'].links[0].from_socket,albedo.inputs['Color'])
   links.new(albedo.outputs[0],surface.inputs['Surface']);bpy.ops.object.bake(type='EMIT')
   links.new(shader.outputs[0],surface.inputs['Surface']);nodes.remove(albedo)
  else:bpy.ops.object.bake(type='NORMAL')
  image.filepath_raw=str(textures/(image.name+'.png'));image.file_format='PNG';image.save()
 lnodes=material.node_tree.nodes;llinks=material.node_tree.links;lowshader=lnodes.get('Principled BSDF')
 texture.image=images['BaseColor'];llinks.new(texture.outputs['Color'],lowshader.inputs['Base Color'])
 ntex=lnodes.new('ShaderNodeTexImage');ntex.image=images['Normal'];normal=lnodes.new('ShaderNodeNormalMap')
 llinks.new(ntex.outputs['Color'],normal.inputs['Color']);llinks.new(normal.outputs['Normal'],lowshader.inputs['Normal'])
 packed=lnodes.new('ShaderNodeTexImage');packed.image=images['MetallicSmoothness'];split=lnodes.new('ShaderNodeSeparateColor');split.mode='RGB'
 llinks.new(packed.outputs['Color'],split.inputs[0]);llinks.new(split.outputs['Red'],lowshader.inputs['Metallic'])
 roughness=lnodes.new('ShaderNodeMath');roughness.operation='SUBTRACT';roughness.inputs[0].default_value=1
 llinks.new(packed.outputs['Alpha'],roughness.inputs[1]);llinks.new(roughness.outputs[0],lowshader.inputs['Roughness'])

bpy.ops.object.select_all(action='DESELECT');high.hide_render=True;high.hide_set(True)
for obj in lods:obj.hide_set(False);obj.hide_render=False;obj.select_set(True)
bpy.context.view_layer.objects.active=lods[0]
bpy.ops.export_scene.fbx(filepath=str(model),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
for obj in lods[1:]:obj.hide_set(True);obj.hide_render=True
# Keep the original hidden in this editable Blender file for future comparisons.
bpy.ops.wm.save_as_mainfile(filepath=str(source/'NhaQue_04_Game.blend'))

scene.cycles.samples=4;scene.cycles.use_denoising=True;scene.view_settings.view_transform='Standard'
scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.75
ld=bpy.data.lights.new('Softbox','AREA');ld.energy=2300;ld.size=8
light=bpy.data.objects.new('Softbox',ld);scene.collection.objects.link(light);light.location=(6,-6,9);light.rotation_euler=(Vector((0,0,2))-light.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Camera');camera=bpy.data.objects.new('Camera',cd);scene.collection.objects.link(camera);scene.camera=camera;cd.type='ORTHO';cd.ortho_scale=10
for name,position in [('Front',(10,-13,8)),('Back',(-10,13,8)),('Right',(14,0,4)),('Left',(-14,0,4))]:
 camera.location=position;camera.rotation_euler=(Vector((0,0,2.2))-camera.location).to_track_quat('-Z','Y').to_euler()
 for i,obj in enumerate(lods):
  for other in lods:other.hide_render=other!=obj
  scene.render.filepath=str(report/('LOD'+str(i)+'_'+name+'.png'));bpy.ops.render.render(write_still=True)
print('BAKED AND EXPORTED',str(model),flush=True)
