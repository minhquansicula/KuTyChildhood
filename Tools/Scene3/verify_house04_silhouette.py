"""Compare original and exported-LOD silhouettes with identical cameras."""
import bpy,json
import numpy as np
from pathlib import Path
from mathutils import Vector

project=Path(__file__).resolve().parents[2];report=project/'Tools/Scene3/TencentHouse04'
bpy.ops.wm.open_mainfile(filepath=str(project.parent/'BLENDER/Nhaque4/NhaQue_04_Game.blend'))
high=bpy.data.objects['Nha04_Original_BakeOnly'];lods=[bpy.data.objects['Nha04_LOD'+str(i)] for i in range(3)]
for obj in list(bpy.context.scene.objects):
 if obj not in [high]+lods:bpy.data.objects.remove(obj,do_unlink=True)
material=bpy.data.materials.new('Silhouette');material.use_nodes=True
nodes=material.node_tree.nodes;nodes.clear();emission=nodes.new('ShaderNodeEmission');emission.inputs['Color'].default_value=(1,1,1,1)
surface=nodes.new('ShaderNodeOutputMaterial');material.node_tree.links.new(emission.outputs[0],surface.inputs[0])
for obj in [high]+lods:obj.hide_set(False);obj.data.materials.clear();obj.data.materials.append(material)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1;scene.cycles.use_denoising=False
scene.render.film_transparent=True;scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
camera=bpy.data.objects.new('SilhouetteCamera',bpy.data.cameras.new('SilhouetteCamera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=10
results=[];folder=report/'Silhouettes';folder.mkdir(exist_ok=True)
for name,position in [('Front',(10,-13,8)),('Back',(-10,13,8)),('Right',(14,0,4)),('Left',(-14,0,4))]:
 camera.location=position;camera.rotation_euler=(Vector((0,0,2.2))-camera.location).to_track_quat('-Z','Y').to_euler()
 masks=[]
 for label,obj in [('Original',high)]+[('LOD'+str(i),obj) for i,obj in enumerate(lods)]:
  for other in [high]+lods:other.hide_render=other!=obj
  scene.render.filepath=str(folder/(label+'_'+name+'.png'));bpy.ops.render.render(write_still=True)
  image=bpy.data.images.load(scene.render.filepath,check_existing=False)
  pixels=np.empty(1000*850*4,dtype=np.float32);image.pixels.foreach_get(pixels)
  masks.append(pixels.reshape(-1,4)[:,3]>.5);bpy.data.images.remove(image)
 for i,mask in enumerate(masks[1:]):
  union=np.count_nonzero(masks[0]|mask);intersection=np.count_nonzero(masks[0]&mask)
  results.append({'view':name,'LOD':i,'silhouette_intersection_over_union':intersection/union,'changed_pixels':int(union-intersection)})
(report/'silhouette_report.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
near=[r['silhouette_intersection_over_union'] for r in results if r['LOD']==0]
if min(near)<.995:raise RuntimeError('Near LOD silhouette changed by more than 0.5%; inspect before completion')
print('SILHOUETTE PASS',json.dumps(results),flush=True)
