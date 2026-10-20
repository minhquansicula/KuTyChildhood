import bpy,json,math
import numpy as np
from pathlib import Path
from mathutils import Vector
p=Path(__file__).resolve().parents[2];root=p.parent/'BLENDER';out=p/'Tools/Scene3/TencentHouse01/Repair';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(root/'NhaQue_Tencent_01_ToiUu.blend'))
low=bpy.data.objects['Nha01_LOD0'];low.hide_set(False);low.hide_render=False
for o in list(bpy.context.scene.objects):
 if o!=low:bpy.data.objects.remove(o,do_unlink=True)
mat=low.data.materials[0]
def stats(obj):
 m=obj.data;pts=np.array([v.co[:] for v in m.vertices]);ns=np.array([f.normal[:] for f in m.polygons]);cs=np.array([f.center[:] for f in m.polygons]);area=np.array([f.area for f in m.polygons]);result={}
 for axis in (0,1):
  chosen=(np.abs(ns[:,axis])>.95)&(cs[:,2]>.8)&(cs[:,2]<3.7)
  hist,edges=np.histogram(cs[chosen,axis],bins=np.arange(-4,4.01,.025),weights=area[chosen]);ids=np.argsort(hist)[-8:][::-1]
  result[str(axis)]=[(round(float((edges[i]+edges[i+1])/2),3),round(float(hist[i]),2)) for i in ids]
 print(obj.name,json.dumps(result),flush=True)
stats(low)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=True
scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard';scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.7
ld=bpy.data.lights.new('Sun','SUN');ld.energy=2;lo=bpy.data.objects.new('Sun',ld);scene.collection.objects.link(lo);lo.rotation_euler=(.3,-.5,-.3)
cd=bpy.data.cameras.new('Camera');co=bpy.data.objects.new('Camera',cd);scene.collection.objects.link(co);scene.camera=co;cd.type='ORTHO';cd.ortho_scale=7.7
co.location=(13,-1,3.5);co.rotation_euler=(Vector((0,0,2.6))-co.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(out/'Before_Low_Wall.png');bpy.ops.render.render(write_still=True)
low.hide_render=True
bpy.ops.wm.obj_import(filepath=str(root/'531c2c5d7c5e6e14567ea9087e4e6b2b.obj'),forward_axis='NEGATIVE_Z',up_axis='Y')
high=bpy.context.view_layer.objects.active;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
pts=np.array([v.co[:] for v in high.data.vertices]);mi=pts.min(axis=0);ma=pts.max(axis=0);scale=7.2/(ma[0]-mi[0]);centre=np.array([(mi[0]+ma[0])/2,(mi[1]+ma[1])/2,mi[2]])
pts=(pts-centre)*scale;high.data.vertices.foreach_set('co',pts.ravel());high.data.update();high.data.materials.clear();high.data.materials.append(mat)
stats(high)
scene.render.filepath=str(out/'Source_High_Wall.png');bpy.ops.render.render(write_still=True)
print('DONE',flush=True)
