import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
scene=bpy.context.scene
assert scene.name=='USER_Mom_FBX_Review'
if bpy.data.objects.get('Mom_User_Deform'):raise RuntimeError('Rig already exists; inspect before repeating.')
mesh=next(o for o in scene.objects if o.type=='MESH')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_PreRig.blend'),copy=True)
backup=bpy.data.collections.new('BACKUP_UserMom_BeforeRig');scene.collection.children.link(backup)
original=mesh.copy();original.data=mesh.data.copy();original.name='ORIGINAL_UserMom_Mesh';backup.objects.link(original);backup.hide_viewport=True;backup.hide_render=True
def mesh_hash(obj):
 return hashlib.sha256(json.dumps({'vertices':[list(v.co) for v in obj.data.vertices],'polygons':[list(p.vertices) for p in obj.data.polygons],'uv':[[list(p.uv) for p in layer.data] for layer in obj.data.uv_layers],'matrix':[list(row) for row in obj.matrix_world]},sort_keys=True).encode()).hexdigest()
fingerprint=mesh_hash(mesh)
mesh.name='Mom_User_Character'
collection=bpy.data.collections.new('USER_Mom_Rig');scene.collection.children.link(collection)
root=bpy.data.objects.new('Mom_User_Root',None);collection.objects.link(root);root.empty_display_size=.06
data=bpy.data.armatures.new('Mom_User_Skeleton');rig=bpy.data.objects.new('Mom_User_Deform',data);collection.objects.link(rig);rig.parent=root;rig.show_in_front=True;data.display_type='OCTAHEDRAL'
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
bones=[]
def add(name,head,tail,parent=None):
 bone=data.edit_bones.new(name);bone.head=head;bone.tail=tail
 if parent:bone.parent=data.edit_bones[parent]
 normal=Vector((0,-1,0));axis=(bone.tail-bone.head).normalized()
 if abs(axis.dot(normal))>.97:normal=Vector((1,0,0))
 bone.align_roll(normal);bones.append(name)
add('Root',(0,0,0),(0,0,.075))
add('Hips',(-.004,.028,.565),(-.004,.027,.64),'Root')
add('Spine',(-.004,.027,.64),(-.004,.018,.735),'Hips')
add('Chest',(-.004,.018,.735),(-.004,.020,.825),'Spine')
add('UpperChest',(-.004,.020,.825),(-.004,.023,.895),'Chest')
add('Neck',(-.004,.023,.895),(-.004,.022,.964),'UpperChest')
add('Head',(-.004,.022,.964),(-.004,.025,1.132),'Neck')
for side,sign in [('L',1),('R',-1)]:
 cx=-.004
 shoulder=(cx+sign*.095,.023,.875);upper=(cx+sign*.130,.020,.856);elbow=(cx+sign*.215,.006,.727);wrist=(cx+sign*.292,-.012,.608);hand=(cx+sign*.331,-.020,.551)
 add('Shoulder.'+side,shoulder,upper,'UpperChest')
 add('UpperArm.'+side,upper,elbow,'Shoulder.'+side)
 add('LowerArm.'+side,elbow,wrist,'UpperArm.'+side)
 add('Hand.'+side,wrist,hand,'LowerArm.'+side)
 hip=(cx+sign*.077,.033,.575);knee=(cx+sign*.104,.019,.310);ankle=(cx+sign*.116,.035,.063);toe=(cx+sign*.116,-.049,.027)
 add('UpperLeg.'+side,hip,knee,'Hips')
 add('LowerLeg.'+side,knee,ankle,'UpperLeg.'+side)
 add('Foot.'+side,ankle,toe,'LowerLeg.'+side)
 add('Toes.'+side,toe,(cx+sign*.116,-.078,.020),'Foot.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
data.bones['Root'].use_deform=False
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION'
bpy.ops.object.select_all(action='DESELECT');mesh.hide_set(False);mesh.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
errors=[]
try:bpy.ops.object.parent_set(type='ARMATURE_AUTO')
except Exception as exc:errors.append(str(exc))
valid=set(data.bones.keys());names={g.index:g.name for g in mesh.vertex_groups};unweighted=[];maximum=0
for vertex in mesh.data.vertices:
 weights=sorted([(names[g.group],g.weight) for g in vertex.groups if names.get(g.group) in valid and g.weight>.0001],key=lambda p:-p[1])[:4]
 if not weights:unweighted.append(vertex.index);continue
 total=sum(w for _,w in weights)
 for group in mesh.vertex_groups:
  try:group.remove([vertex.index])
  except RuntimeError:pass
 for name,w in weights:mesh.vertex_groups[name].add([vertex.index],w/total,'REPLACE')
 maximum=max(maximum,len(weights))
for modifier in mesh.modifiers:
 if modifier.type=='ARMATURE':modifier.use_deform_preserve_volume=False
scene.render.fps=30
report={'source':'C:/Users/LENOVO/Downloads/mom.fbx','rig':rig.name,'bones':bones,'weight_method':'Blender automatic bone heat; normalized, pruned to 4 influences','unweighted_vertices':unweighted,'max_influences':maximum,'errors':errors,'original_mesh_uv_transform_hash':fingerprint,'mesh_uv_transform_unchanged':mesh_hash(mesh)==fingerprint,'backup_collection':backup.name,'limitations':['Initial body rig; no independently animated finger or facial controls yet.','Automatic weights require visual deformation validation before game delivery.']}
(OUT/'Source/user_mom_rig_validation.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Animated.blend'))
print(json.dumps({k:v for k,v in report.items() if k!='unweighted_vertices'}));print('UNWEIGHTED_VERTEX_COUNT',len(unweighted))
if errors or unweighted:raise RuntimeError('Rig needs weight repair; inspect the report.')
