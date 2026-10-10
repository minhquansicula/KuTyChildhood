"""Reproject the original appearance onto clean UVs for each game LOD.

Decimation changed the UV interpolation on large wall triangles. Every LOD gets
its own non-overlapping atlas baked from the untouched downloaded high mesh.
"""
import bpy
import numpy as np
import json,math,shutil
from pathlib import Path
from mathutils import Vector

p=Path(__file__).resolve().parents[2];root=p.parent/'BLENDER'
out=p/'Tools/Scene3/TencentHouse01/Repair';out.mkdir(parents=True,exist_ok=True)
textures=p/'Assets/_Project/Art/Textures/Scene3/TencentHouse01'
fbx=p/'Assets/_Project/Art/Models/Scene3/TencentHouse01/NhaQue_Tencent_01.fbx'
backup=out/'BeforeRepair';backup.mkdir(exist_ok=True)
if not (backup/fbx.name).exists():shutil.copy2(fbx,backup/fbx.name)
blend=root/'NhaQue_Tencent_01_ToiUu.blend'
if not (backup/blend.name).exists():shutil.copy2(blend,backup/blend.name)
bpy.ops.wm.open_mainfile(filepath=str(blend))
lods=[bpy.data.objects['Nha01_LOD'+str(i)] for i in range(3)]
source_mat=lods[0].data.materials[0]
for obj in list(bpy.context.scene.objects):
 if obj not in lods:bpy.data.objects.remove(obj,do_unlink=True)
for obj in lods:obj.hide_set(False);obj.hide_render=False

bpy.ops.wm.obj_import(filepath=str(root/'531c2c5d7c5e6e14567ea9087e4e6b2b.obj'),forward_axis='NEGATIVE_Z',up_axis='Y')
high=bpy.context.view_layer.objects.active;high.name='Original_High_BakeOnly'
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
pts=np.empty(len(high.data.vertices)*3,dtype=np.float32);high.data.vertices.foreach_get('co',pts);pts=pts.reshape(-1,3)
mi=pts.min(axis=0);ma=pts.max(axis=0);scale=7.2/(ma[0]-mi[0]);centre=np.array([(mi[0]+ma[0])/2,(mi[1]+ma[1])/2,mi[2]])
high.data.vertices.foreach_set('co',((pts-centre)*scale).ravel());high.data.update();high.data.materials.clear();high.data.materials.append(source_mat)
del pts

scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1
scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.12
scene.render.bake.max_ray_distance=.35;scene.render.bake.margin=12
scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True

nodes=source_mat.node_tree.nodes;links=source_mat.node_tree.links
bsdf=nodes.get('Principled BSDF');surface=nodes.get('Material Output')
emission=nodes.new('ShaderNodeEmission');combine=nodes.new('ShaderNodeCombineXYZ')
invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1
metal_socket=bsdf.inputs['Metallic'].links[0].from_socket
rough_socket=bsdf.inputs['Roughness'].links[0].from_socket
links.new(metal_socket,combine.inputs['X']);links.new(rough_socket,invert.inputs[1]);links.new(invert.outputs[0],combine.inputs['Y'])
links.new(combine.outputs[0],emission.inputs['Color'])

reports=[]
for i,obj in enumerate(lods):
 bpy.context.view_layer.objects.active=obj;bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
 # Flatten only nearly coplanar wall faces. Window frames, tiles and tin remain relief.
 planes=[(0,-3.038),(0,3.138),(1,-1.762),(1,2.938)]
 wall_report=[];wall_faces={}
 for axis,plane in planes:
  selected=set()
  for face in obj.data.polygons:
   if abs(face.normal[axis])>.90 and .65<face.center.z<4.8 and abs(face.center[axis]-plane)<.06 and all(abs(obj.data.vertices[v].co[axis]-plane)<.09 for v in face.vertices):
    selected.update(face.vertices)
    wall_faces[face.index]=(axis,1 if plane>0 else -1)
  for index in selected:obj.data.vertices[index].co[axis]=plane
  wall_report.append({'axis':axis,'plane':plane,'vertices':len(selected)})
 obj.data.update()
 # Old custom normals corresponded to the removed high geometry. Recompute them.
 if obj.data.has_custom_normals:
  obj.data.normals_split_custom_set([(0,0,0)]*len(obj.data.loops))
 for face in obj.data.polygons:face.use_smooth=True
 # Give the plaster planes exact face normals; smooth averaging across the roof
 # or window edges otherwise leaves visible triangles even with corrected UVs.
 normals=np.empty(len(obj.data.loops)*3,dtype=np.float32)
 obj.data.corner_normals.foreach_get('vector',normals);normals=normals.reshape(-1,3)
 for face_index,(axis,sign) in wall_faces.items():
  for loop_index in obj.data.polygons[face_index].loop_indices:
   normals[loop_index]=0;normals[loop_index,axis]=sign
 obj.data.normals_split_custom_set(normals.tolist())
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
 bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.008,area_weight=.5)
 bpy.ops.object.mode_set(mode='OBJECT')
 lowmat=bpy.data.materials.new('Nha01_LOD'+str(i)+'_PBR');lowmat.use_nodes=True
 obj.data.materials.clear();obj.data.materials.append(lowmat)
 texnode=lowmat.node_tree.nodes.new('ShaderNodeTexImage');lowmat.node_tree.nodes.active=texnode
 resolution=(2048,1024,512)[i];images={}
 for kind in ('BaseColor','Normal','MetallicSmoothness'):
  image=bpy.data.images.new('Nha01_LOD'+str(i)+'_'+kind,width=resolution,height=resolution,alpha=True)
  image.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
  texnode.image=image;images[kind]=image
  bpy.ops.object.select_all(action='DESELECT');high.select_set(True);obj.select_set(True);bpy.context.view_layer.objects.active=obj
  for other in lods:other.hide_render=other!=obj
  high.hide_render=False
  print('Bake',obj.name,kind,resolution,flush=True)
  if kind=='MetallicSmoothness':
   links.new(emission.outputs[0],surface.inputs['Surface']);bpy.ops.object.bake(type='EMIT')
   values=np.empty(resolution*resolution*4,dtype=np.float32);image.pixels.foreach_get(values);values=values.reshape(-1,4)
   values[:,3]=values[:,1];values[:,1:3]=0;image.pixels.foreach_set(values.ravel())
   links.new(bsdf.outputs[0],surface.inputs['Surface'])
  else:bpy.ops.object.bake(type='DIFFUSE' if kind=='BaseColor' else 'NORMAL')
  if kind=='Normal':
   # Walls must stay flat. Keep relief normals on tiles, corrugated tin and
   # window frames, while removing the source scan's bumps on plaster only.
   pixels=np.empty(resolution*resolution*4,dtype=np.float32);image.pixels.foreach_get(pixels);pixels=pixels.reshape(resolution,resolution,4)
   mask=np.zeros((resolution,resolution),dtype=bool)
   uv=obj.data.uv_layers.active.data
   for face_index in wall_faces:
    face=obj.data.polygons[face_index];tri=np.array([uv[l].uv[:] for l in face.loop_indices])*resolution-.5
    xmin=max(0,int(np.floor(tri[:,0].min())));xmax=min(resolution-1,int(np.ceil(tri[:,0].max())))
    ymin=max(0,int(np.floor(tri[:,1].min())));ymax=min(resolution-1,int(np.ceil(tri[:,1].max())))
    xx,yy=np.meshgrid(np.arange(xmin,xmax+1),np.arange(ymin,ymax+1));a,b,c=tri
    denominator=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(denominator)<1e-8:continue
    u=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/denominator
    v=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/denominator
    mask[ymin:ymax+1,xmin:xmax+1]|=(u>=-1e-5)&(v>=-1e-5)&(u+v<=1.00001)
   expanded=mask.copy()
   for dy,dx in ((0,1),(0,-1),(1,0),(-1,0),(1,1),(-1,-1),(1,-1),(-1,1)):
    expanded|=np.roll(np.roll(mask,dy,axis=0),dx,axis=1)
   pixels[expanded]=(.5,.5,1,1);image.pixels.foreach_set(pixels.ravel())
  image.filepath_raw=str(textures/(image.name+'.png'));image.file_format='PNG';image.save()
 # Set baked maps for the Blender repair preview, using the same tangent convention.
 lnodes=lowmat.node_tree.nodes;llinks=lowmat.node_tree.links;lshader=lnodes.get('Principled BSDF')
 texnode.image=images['BaseColor'];llinks.new(texnode.outputs['Color'],lshader.inputs['Base Color'])
 ntex=lnodes.new('ShaderNodeTexImage');ntex.image=images['Normal'];normal=lnodes.new('ShaderNodeNormalMap')
 llinks.new(ntex.outputs['Color'],normal.inputs['Color']);llinks.new(normal.outputs['Normal'],lshader.inputs['Normal'])
 packed=lnodes.new('ShaderNodeTexImage');packed.image=images['MetallicSmoothness'];separate=lnodes.new('ShaderNodeSeparateColor');separate.mode='RGB'
 llinks.new(packed.outputs['Color'],separate.inputs[0]);llinks.new(separate.outputs['Red'],lshader.inputs['Metallic'])
 roughness=lnodes.new('ShaderNodeMath');roughness.operation='SUBTRACT';roughness.inputs[0].default_value=1
 llinks.new(packed.outputs['Alpha'],roughness.inputs[1]);llinks.new(roughness.outputs[0],lshader.inputs['Roughness'])
 reports.append({'LOD':i,'triangles':len(obj.data.polygons),'atlas_size':resolution,'wall_planes':wall_report,'flat_wall_faces':len(wall_faces),'max_wall_planarity_error_m':max((abs(obj.data.vertices[v].co[axis]-plane) for info in wall_report for axis,plane in [(info['axis'],info['plane'])] for face_index,(fa,sign) in wall_faces.items() if fa==axis and sign==(1 if plane>0 else -1) for v in obj.data.polygons[face_index].vertices),default=0)})

high.hide_render=True;high.hide_set(True)
bpy.ops.object.select_all(action='DESELECT')
for obj in lods:obj.hide_set(False);obj.hide_render=False;obj.select_set(True)
bpy.context.view_layer.objects.active=lods[0]
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False)
bpy.data.objects.remove(high,do_unlink=True)
for obj in lods[1:]:obj.hide_render=True;obj.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'NhaQue_Tencent_01_SuaTuong.blend'))
(out/'repair_report.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')

scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.view_settings.view_transform='Standard'
scene.render.resolution_x=1100;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Preview');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.8
ld=bpy.data.lights.new('Softbox','AREA');ld.energy=2500;ld.size=7
light=bpy.data.objects.new('Softbox',ld);scene.collection.objects.link(light);light.location=(10,-4,7);light.rotation_euler=(Vector((0,0,2.5))-light.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Camera');camera=bpy.data.objects.new('Camera',cd);scene.collection.objects.link(camera);scene.camera=camera;cd.type='ORTHO';cd.ortho_scale=7.7
camera.location=(13,-1,3.5);camera.rotation_euler=(Vector((0,0,2.6))-camera.location).to_track_quat('-Z','Y').to_euler()
for i,obj in enumerate(lods):
 for other in lods:other.hide_render=other!=obj
 scene.render.filepath=str(out/('After_Wall_LOD'+str(i)+'.png'));bpy.ops.render.render(write_still=True)
print('Repair completed',json.dumps(reports),flush=True)
