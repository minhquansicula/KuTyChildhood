"""Reversible pose-only cloth support; neutral source model remains unchanged."""
import bpy,bmesh,json,math,numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
scene=bpy.context.scene;rig=bpy.data.objects['Mom_User_Deform'];mesh=bpy.data.objects['Mom_User_Character']
if mesh.data.shape_keys:raise RuntimeError('Inspect existing shape keys before adding seated cloth support.')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Household_BeforeClothSupport.blend'),copy=True)
rig.animation_data.action=bpy.data.actions['MomUser_Idle_Seated'];scene.frame_set(1);bpy.context.view_layer.update()
depsgraph=bpy.context.evaluated_depsgraph_get();evaluated=mesh.evaluated_get(depsgraph);data=evaluated.to_mesh();posed=[mesh.matrix_world@v.co for v in data.vertices];evaluated.to_mesh_clear()
rest=[mesh.matrix_world@v.co for v in mesh.data.vertices]
image=mesh.data.materials[0].node_tree.nodes.get('Principled BSDF').inputs['Base Color'].links[0].from_node.image
w,h=image.size;pixels=np.empty(w*h*4,dtype=np.float32);image.pixels.foreach_get(pixels);pixels=pixels.reshape(h,w,4)
colors={};uv=mesh.data.uv_layers.active
for polygon in mesh.data.polygons:
 for li in polygon.loop_indices:
  vi=mesh.data.loops[li].vertex_index
  if vi not in colors:
   u,v=uv.data[li].uv;colors[vi]=pixels[min(h-1,int((v%1)*h)),min(w-1,int((u%1)*w)),:3].copy()
del pixels
brown={i for i,c in colors.items() if c[0]>c[1]*1.27 and c[1]>c[2]*1.12}
pants_faces=[]
for polygon in mesh.data.polygons:
 ids=list(polygon.vertices);z=sum(rest[i].z for i in ids)/len(ids)
 if .38<z<.595 and sum(i in brown for i in ids)<len(ids)/2:pants_faces.append(ids)
pants_tree=BVHTree.FromPolygons(posed,pants_faces)
deltas=[Vector() for _ in mesh.data.vertices]
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
shirt_count=pants_count=0
for vertex in mesh.data.vertices:
 i=vertex.index;p=rest[i];target=posed[i].copy()
 if i in brown and .565<p.z<.65 and abs(p.x+.004)<.165:
  hit=pants_tree.ray_cast(Vector((target.x,target.y,.85)),Vector((0,0,-1)))[0]
  if hit and .20<hit.z<.45 and target.z<hit.z+.007:
   target.z=hit.z+.007;shirt_count+=1
 elif i not in brown and .40<p.z<.59 and p.y>.020:
  side='L' if p.x>-.004 else 'R';bone=rig.data.bones['UpperLeg.'+side]
  ideal=rig.matrix_world@rig.pose.bones[bone.name].matrix@bone.matrix_local.inverted()@rig.matrix_world.inverted()@p
  factor=smooth((p.z-.40)/.07)*smooth((.59-p.z)/.065)*smooth((p.y-.020)/.035)*smooth((abs(p.x+.004)-.025)/.035)*.70
  target=target.lerp(ideal,factor)
  if factor:pants_count+=1
 delta=target-posed[i]
 if delta.length>.035:delta=delta.normalized()*.035
 deltas[i]=delta
neighbors=[set() for _ in mesh.data.vertices]
for edge in mesh.data.edges:
 a,b=edge.vertices;neighbors[a].add(b);neighbors[b].add(a)
region={i for i,d in enumerate(deltas) if d.length>.000001}
for _ in range(2):region.update(j for i in list(region) for j in neighbors[i])
for _ in range(5):
 previous=[d.copy() for d in deltas]
 for i in region:
  if neighbors[i]:deltas[i]=previous[i].lerp(sum((previous[j] for j in neighbors[i]),Vector())/len(neighbors[i]),.30)
mesh.shape_key_add(name='Basis');corrective=mesh.shape_key_add(name='MomUser_Sit_ClothSupport');corrective.value=0
bone_matrices={b.name:rig.pose.bones[b.name].matrix@b.matrix_local.inverted() for b in rig.data.bones};names={g.index:g.name for g in mesh.vertex_groups}
for i in region:
 if deltas[i].length<.000001:continue
 skin=Matrix(((0,0,0,0),)*4)
 for group in mesh.data.vertices[i].groups:
  matrix=bone_matrices.get(names[group.group])
  if matrix:
   for a in range(4):
    for b in range(4):skin[a][b]+=matrix[a][b]*group.weight
 world_skin=rig.matrix_world@skin@rig.matrix_world.inverted()@mesh.matrix_world
 corrective.data[i].co=world_skin.inverted_safe()@(posed[i]+deltas[i])
driver=corrective.driver_add('value').driver;driver.type='SCRIPTED';variable=driver.variables.new();variable.name='hip_y';variable.type='SINGLE_PROP';variable.targets[0].id=rig;variable.targets[0].data_path='pose.bones["Hips"].location[1]'
driver.expression='max(0.0,min(1.0,(-hip_y-0.03)/0.195))'
mesh['seated_cloth_note']='Reversible pose corrective; original neutral coordinates/UVs intact. Driver must be baked to blendshape animation for a later Unity export.'
mesh.data.update();scene.frame_set(2);scene.frame_set(1);bpy.context.view_layer.update()
seated_value=corrective.value
rig.animation_data.action=bpy.data.actions['MomUser_Idle'];scene.frame_set(1);bpy.context.view_layer.update();standing_value=corrective.value
report={'corrective':corrective.name,'shirt_support_vertices':shirt_count,'upper_trouser_support_vertices':pants_count,'affected_vertices':len(region),'maximum_pose_offset_m':max(d.length for d in deltas),'seated_value':seated_value,'standing_value':standing_value,'neutral_basis_edited':False,'export_requirement':'Bake the shape driver to blendshape keyframes if exporting to Unity later.'}
(OUT/'Source/user_mom_seated_cloth_support.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Household.blend'))
print(json.dumps(report))
