import bpy,json,math
from pathlib import Path
from mathutils import Matrix
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
mesh=bpy.data.objects['Mom_User_Character'];rig=bpy.data.objects['Mom_User_Deform'];changed=0
for vertex in mesh.data.vertices:
 p=mesh.matrix_world@vertex.co;ax=abs(p.x+.004);weights={mesh.vertex_groups[g.group].name:g.weight for g in vertex.groups};updated=False
 if p.z>.56 and ax<.145:
  factor=max(0,min(1,(p.z-.56)/.03));factor=factor*factor*(3-2*factor);removed=0
  for name in list(weights):
   if name.startswith(('UpperLeg','LowerLeg','Foot','Toes')):
    amount=weights[name]*factor;weights[name]-=amount;removed+=amount
  if removed>.000001:weights['Hips']=weights.get('Hips',0)+removed;updated=True
 if .62<p.z<.83 and ax<.13:
  factor=max(0,min(1,(.13-ax)/.025));removed=0
  for name in list(weights):
   if name.startswith(('Shoulder','UpperArm','LowerArm','Hand')):
    amount=weights[name]*factor;weights[name]-=amount;removed+=amount
  if removed>.000001:
   name='Chest' if p.z>.73 else 'Spine';weights[name]=weights.get(name,0)+removed;updated=True
 if not updated:continue
 keep=sorted([(n,w) for n,w in weights.items() if w>.0001],key=lambda x:-x[1])[:4];total=sum(w for _,w in keep)
 for group in mesh.vertex_groups:group.remove([vertex.index])
 for name,w in keep:mesh.vertex_groups[name].add([vertex.index],w/total,'REPLACE')
 changed+=1
mesh.data.update()
# Re-evaluate only the four actions created in this continuation, retaining their IDs.
source=Path('E:/BLENDER/mom_animation_08_continue.py').read_text(encoding='utf8')
functions=source[source.index('# Load only existing solver definitions;'):source.index('manifest=json.loads')]
functions=functions.replace('sign*.095,-.005,.350','sign*.095,-.005,.385').replace('(sign*.1,-1,-.08)','(sign*.06,-1,-.35)')
functions=functions.replace('.18*seated,-.225*seated','.155*seated,-.185*seated')
exec(compile(functions,'household_pose_refinement','exec'))
manifest=json.loads((OUT/'Source/user_mom_household_manifest.json').read_text())
for clip in manifest:
 if clip['action'] not in ['MomUser_SitDown','MomUser_Idle_Seated','MomUser_StandUp','MomUser_CallChild']:continue
 action=bpy.data.actions[clip['action']];rig.animation_data.action=action;previous={}
 for frame in range(1,clip['frame_end']+1):
  matrices=household_pose(clip['action'],(frame-1)/(clip['frame_end']-1))
  for bone in order:
   name=bone.name;parent=bone.parent;reference=matrices[parent.name]@REST[parent.name].inverted()@REST[name] if parent else REST[name]
   location,rotation,scale=(reference.inverted()@matrices[name]).decompose()
   if name in previous and rotation.dot(previous[name])<0:rotation.negate()
   previous[name]=rotation.copy();pb=rig.pose.bones[name];pb.location=location;pb.rotation_quaternion=rotation;pb.scale=(1,1,1)
   for channel in ['location','rotation_quaternion','scale']:pb.keyframe_insert(channel,frame=frame,group=name)
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:
     for point in curve.keyframe_points:point.interpolation='LINEAR'
rig.animation_data.action=bpy.data.actions['MomUser_Idle'];bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Household.blend'))
print('Household cleanup: removed leg/arm weight leakage from torso, vertices:',changed,'; seated wrist/palm pitch refined. Original three actions untouched.')
