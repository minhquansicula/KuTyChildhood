import bpy,json
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
source=Path('E:/BLENDER/mom_animation_03_actions.py').read_text(encoding='utf8').split('clips=')[0]
source=source.replace("assert not any(a.name.startswith('MomUser_') for a in bpy.data.actions),'Do not overwrite existing animation work.'",'')
source=source.replace("sign*.185,-.005,.590","sign*.185,-.005,.563")
source=source.replace(".070*math.cos(phase)",".055*math.cos(phase)")
source=source.replace(".003*(1-math.cos(4*math.pi*t))","-.006+.0015*(1-math.cos(4*math.pi*t))")
exec(compile(source,'user_mom_polished_pose','exec'))
# These are only the three actions created for the imported model in this task.
# Updating key values preserves their action identity and leaves all other actions alone.
manifest=json.loads((OUT/'Source/user_mom_animation_manifest.json').read_text())
for clip in manifest:
 action=bpy.data.actions[clip['action']];rig.animation_data.action=action;previous={}
 for frame in range(1,clip['frame_end']+1):
  matrices=pose(clip['action'],(frame-1)/(clip['frame_end']-1))
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
     for key in curve.keyframe_points:key.interpolation='LINEAR'
mesh=bpy.data.objects['Mom_User_Character'];changed=0
for vertex in mesh.data.vertices:
 world=mesh.matrix_world@vertex.co
 if world.z<.046:
  for group in mesh.vertex_groups:group.remove([vertex.index])
  mesh.vertex_groups['Foot.'+('L' if world.x>-.004 else 'R')].add([vertex.index],1,'REPLACE');changed+=1
rig.animation_data.action=bpy.data.actions['MomUser_Idle'];scene.frame_set(1);scene.frame_start=1;scene.frame_end=121
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Animated.blend'))
print('Polished relaxed elbows, shortened walk stride, lowered pelvis for planted stance, foot/sole weights:',changed)
