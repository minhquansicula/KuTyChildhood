"""Build conservative LODs; reject candidates exceeding measured surface error.

No remeshing, scaling per axis, flattening or smoothing vertex positions.
Textures are subsequently reprojected from the untouched original geometry.
"""
import bpy,json,math,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

project=Path(__file__).resolve().parents[2]
source=project.parent/'BLENDER/Nhaque4'
report=project/'Tools/Scene3/TencentHouse04';report.mkdir(parents=True,exist_ok=True)
source_file=source/'2f79e2974f1488051a3e23b79cf6144f.obj'
source_hash=hashlib.sha256(source_file.read_bytes()).hexdigest()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=str(source_file),forward_axis='NEGATIVE_Z',up_axis='Y')
high=bpy.context.view_layer.objects.active;high.name='Nha04_Original_BakeOnly'
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
coords=np.empty(len(high.data.vertices)*3,dtype=np.float32);high.data.vertices.foreach_get('co',coords);coords=coords.reshape(-1,3)
mi=coords.min(axis=0);ma=coords.max(axis=0);scale=min(7.8/max(ma[0]-mi[0],ma[1]-mi[1]),5.7/(ma[2]-mi[2]))
center=np.array([(mi[0]+ma[0])/2,(mi[1]+ma[1])/2,mi[2]])
coords=(coords-center)*scale;high.data.vertices.foreach_set('co',coords.ravel());high.data.update()
material=bpy.data.materials.new('Nha04_Original_PBR');material.use_nodes=True
nodes=material.node_tree.nodes;links=material.node_tree.links;shader=nodes.get('Principled BSDF')
for suffix,socket,noncolor in [('', 'Base Color',False),('_roughness','Roughness',True),('_metallic','Metallic',True)]:
 image=bpy.data.images.load(str(source/('texture_pbr_20250901'+suffix+'.png')))
 if noncolor:image.colorspace_settings.name='Non-Color'
 node=nodes.new('ShaderNodeTexImage');node.image=image;links.new(node.outputs['Color'],shader.inputs[socket])
image=bpy.data.images.load(str(source/'texture_pbr_20250901_normal.png'));image.colorspace_settings.name='Non-Color'
node=nodes.new('ShaderNodeTexImage');node.image=image;normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.65
links.new(node.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],shader.inputs['Normal'])
high.data.materials.clear();high.data.materials.append(material)
original_triangles=len(high.data.polygons)
source_assets={path.name:hashlib.sha256(path.read_bytes()).hexdigest() for path in source.iterdir() if path.suffix.lower() in ('.obj','.mtl','.png')}
tree=BVHTree.FromObject(high,bpy.context.evaluated_depsgraph_get())
# Check both directions: reduced vertices against the source, and uniform source
# surface samples plus all six extrema against the reduced surface.
ids=np.unique(np.concatenate((np.linspace(0,len(coords)-1,18000,dtype=int),coords.argmin(axis=0),coords.argmax(axis=0))))
samples=[Vector(tuple(coords[i])) for i in ids]
samples.extend(high.data.polygons[i].center.copy() for i in np.linspace(0,original_triangles-1,12000,dtype=int))
lods=[];measurements=[]
for lod,(budgets,max_error,p99_error) in enumerate([((40000,65000,90000),.035,.008),((16000,24000,36000),.05,.02),((5000,8000,12000),.09,.04)]):
 for budget in budgets:
  obj=high.copy();obj.data=high.data.copy();bpy.context.collection.objects.link(obj);obj.name='Nha04_LOD'+str(lod)
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
  modifier=obj.modifiers.new('Conservative LOD','DECIMATE');modifier.ratio=budget/original_triangles;modifier.use_collapse_triangulate=True
  bpy.ops.object.modifier_apply(modifier=modifier.name)
  reduced_tree=BVHTree.FromObject(obj,bpy.context.evaluated_depsgraph_get())
  errors=[reduced_tree.find_nearest(point)[3] for point in samples]
  errors.extend(tree.find_nearest(v.co)[3] for v in obj.data.vertices)
  errors=np.array(errors);lowcoords=np.array([v.co[:] for v in obj.data.vertices])
  bounds_error=float(max(np.max(np.abs(lowcoords.min(axis=0)-coords.min(axis=0))),np.max(np.abs(lowcoords.max(axis=0)-coords.max(axis=0)))))
  stats={'LOD':lod,'triangles':len(obj.data.polygons),'samples':len(errors),'max_surface_error_m':float(errors.max()),'p99_surface_error_m':float(np.percentile(errors,99)),'mean_surface_error_m':float(errors.mean()),'bounds_error_m':bounds_error}
  print('Candidate',json.dumps(stats),flush=True)
  if stats['max_surface_error_m']<=max_error and stats['p99_surface_error_m']<=p99_error and bounds_error<=max_error:
   if obj.data.has_custom_normals:obj.data.normals_split_custom_set([(0,0,0)]*len(obj.data.loops))
   for face in obj.data.polygons:face.use_smooth=True
   obj.data.name=obj.name+'_Mesh';lods.append(obj);measurements.append(stats);break
  bpy.data.objects.remove(obj,do_unlink=True)
 else:raise RuntimeError('No LOD '+str(lod)+' candidate preserves the house within the conservative tolerance; no FBX exported')

summary={'source_sha256':source_hash,'source_assets_sha256':source_assets,'source_triangles':original_triangles,'dimensions_metres':list(high.dimensions),'uniform_scale':float(scale),'lods':measurements,'geometry_policy':'Only conservative edge collapse; no remeshing, vertex smoothing or wall flattening. Bidirectional sampled surface comparison.'}
(report/'geometry_report.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(source/'NhaQue_04_Prepare.blend'))

scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=4;scene.cycles.use_denoising=True
scene.view_settings.view_transform='Standard';scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.75
ld=bpy.data.lights.new('Softbox','AREA');ld.energy=2300;ld.size=8
light=bpy.data.objects.new('Softbox',ld);scene.collection.objects.link(light);light.location=(6,-6,9);light.rotation_euler=(Vector((0,0,2))-light.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Camera');camera=bpy.data.objects.new('Camera',cd);scene.collection.objects.link(camera);scene.camera=camera;cd.type='ORTHO';cd.ortho_scale=10
for obj in lods:obj.hide_render=True
for name,position in [('Front',(10,-13,8)),('Back',(-10,13,8)),('Right',(14,0,4)),('Left',(-14,0,4))]:
 camera.location=position;camera.rotation_euler=(Vector((0,0,2.2))-camera.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(report/('Source_'+name+'.png'));bpy.ops.render.render(write_still=True)
print('PREPARED',json.dumps(summary),flush=True)
