"""Add household actions to the user's mom.fbx rig without rewriting existing clips."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
scene=bpy.context.scene;rig=bpy.data.objects['Mom_User_Deform'];mesh=bpy.data.objects['Mom_User_Character']
assert scene.name=='USER_Mom_FBX_Review'
new_clips=[('MomUser_SitDown',2.8,False),('MomUser_Idle_Seated',4,True),('MomUser_StandUp',2.8,False),('MomUser_CallChild',3.6,False)]
if any(bpy.data.actions.get(name) for name,_,_ in new_clips):raise RuntimeError('Household actions already exist; inspect before updating.')
def action_hash(action):
 values=[]
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:values.append((curve.data_path,curve.array_index,[(list(k.co),list(k.handle_left),list(k.handle_right),k.interpolation) for k in curve.keyframe_points]))
 return hashlib.sha256(json.dumps(values,sort_keys=True).encode()).hexdigest()
def geometry_hash(obj):
 return hashlib.sha256(json.dumps({'vertices':[list(v.co) for v in obj.data.vertices],'faces':[list(p.vertices) for p in obj.data.polygons],'uvs':[[list(p.uv) for p in layer.data] for layer in obj.data.uv_layers],'matrix':[list(row) for row in obj.matrix_world]},sort_keys=True).encode()).hexdigest()
preserved={a.name:action_hash(a) for a in bpy.data.actions if a.name.startswith('MomUser_')}
original_geometry=geometry_hash(mesh)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_BeforeHousehold.blend'),copy=True)

# Load only existing solver definitions; never execute the old clip-building loop.
code=Path('E:/BLENDER/mom_animation_03_actions.py').read_text(encoding='utf8').split('clips=')[0]
code=code.replace("assert not any(a.name.startswith('MomUser_') for a in bpy.data.actions),'Do not overwrite existing animation work.'",'')
code=code.replace('sign*.185,-.005,.590','sign*.185,-.005,.563')
exec(compile(code,'existing_mom_solver','exec'))
standing_pose=pose
def envelope(t):return smooth(t/.24)*(1-smooth((t-.76)/.24))
def basis(location,long,normal):
 y=Vector(long).normalized();z=Vector(normal);z-=y*z.dot(y);z.normalize();x=y.cross(z).normalized();z=x.cross(y).normalized();m=Matrix((x,y,z)).transposed().to_4x4();m.translation=location;return m
def household_pose(kind,t):
 breath=math.sin(math.tau*t)
 seated=1. if kind=='MomUser_Idle_Seated' else smooth(t) if kind=='MomUser_SitDown' else 1-smooth(t) if kind=='MomUser_StandUp' else 0.
 gesture=envelope(t) if kind=='MomUser_CallChild' else 0.
 hip_offset=Vector((.0015*breath,.18*seated,-.225*seated+.0008*(1-math.cos(math.tau*t))))
 lean=.09*seated+.004*breath
 if kind in ['MomUser_SitDown','MomUser_StandUp']:lean+=.20*math.sin(math.pi*t)**2
 matrices={'Root':REST['Root'].copy(),'Hips':REST['Hips'].copy()};matrices['Hips'].translation=HEAD['Hips']+hip_offset
 for name,factor in [('Spine',.3),('Chest',.7),('UpperChest',1)]:fk(name,matrices,Quaternion((1,0,0),lean*factor))
 fk('Neck',matrices,Quaternion((0,0,1),-.035*gesture)@Quaternion((1,0,0),.025*seated))
 fk('Head',matrices,Quaternion((0,0,1),-.10*gesture)@Quaternion((1,0,0),.04*seated))
 for side,sign in [('L',1),('R',-1)]:
  suffix='.'+side;fk('Shoulder'+suffix,matrices,Quaternion((1,0,0),lean))
  ankle=HEAD['Foot'+suffix].copy();hip=fkhead('UpperLeg'+suffix,matrices)
  knee,ankle=solve(hip,ankle,(TAIL['UpperLeg'+suffix]-HEAD['UpperLeg'+suffix]).length,(TAIL['LowerLeg'+suffix]-HEAD['LowerLeg'+suffix]).length,(sign*.11,-.6,.28))
  matrices['UpperLeg'+suffix]=orient(hip,knee,REST['UpperLeg'+suffix]);matrices['LowerLeg'+suffix]=orient(knee,ankle,REST['LowerLeg'+suffix])
  matrices['Foot'+suffix]=REST['Foot'+suffix].copy();matrices['Foot'+suffix].translation=ankle
  matrices['Toes'+suffix]=matrices['Foot'+suffix]@REST['Foot'+suffix].inverted()@REST['Toes'+suffix]
  wrist=Vector((-.004+sign*.185,-.005,.563)).lerp(Vector((-.004+sign*.095,-.005,.350)),seated)
  wrist.y-=.0015*breath;wrist.z+=.001*breath
  if side=='R' and gesture:
   wave=.011*math.sin(math.tau*t*2)
   wrist=wrist.lerp(Vector((-.245+wave,-.12,.895)),gesture)
  shoulder=fkhead('UpperArm'+suffix,matrices)
  pole=Vector((sign*.27,-.015,.73)).lerp(Vector((sign*.24,.07,.48)),seated)
  if side=='R':pole=pole.lerp(Vector((-.36,-.015,.75)),gesture)
  elbow,wrist=solve(shoulder,wrist,(TAIL['UpperArm'+suffix]-HEAD['UpperArm'+suffix]).length,(TAIL['LowerArm'+suffix]-HEAD['LowerArm'+suffix]).length,pole)
  matrices['UpperArm'+suffix]=orient(shoulder,elbow,REST['UpperArm'+suffix]);matrices['LowerArm'+suffix]=orient(elbow,wrist,REST['LowerArm'+suffix])
  natural=matrices['LowerArm'+suffix]@REST['LowerArm'+suffix].inverted()@REST['Hand'+suffix]
  target=basis(wrist,(sign*.1,-1,-.08),(0,0,-1));blend=seated
  if side=='R' and gesture:target=basis(wrist,(.10*math.sin(math.tau*t*2),0,1),(0,-1,0));blend=gesture
  rot=natural.to_quaternion().slerp(target.to_quaternion(),blend);matrices['Hand'+suffix]=rot.to_matrix().to_4x4();matrices['Hand'+suffix].translation=wrist
 return matrices

manifest=json.loads((OUT/'Source/user_mom_animation_manifest.json').read_text())
rig.animation_data_create();scene.render.fps=30
for name,duration,loop in new_clips:
 action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action;previous={};frames=round(duration*30)+1
 for frame in range(1,frames+1):
  matrices=household_pose(name,(frame-1)/(frames-1))
  for bone in order:
   n=bone.name;parent=bone.parent;reference=matrices[parent.name]@REST[parent.name].inverted()@REST[n] if parent else REST[n]
   location,rotation,scale=(reference.inverted()@matrices[n]).decompose()
   if n in previous and rotation.dot(previous[n])<0:rotation.negate()
   previous[n]=rotation.copy();pb=rig.pose.bones[n];pb.location=location;pb.rotation_quaternion=rotation;pb.scale=(1,1,1)
   for channel in ['location','rotation_quaternion','scale']:pb.keyframe_insert(channel,frame=frame,group=n)
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:
     for key in curve.keyframe_points:key.interpolation='LINEAR'
 manifest.append({'action':name,'fps':30,'frame_start':1,'frame_end':frames,'duration_seconds':duration,'loop':loop,'in_place':True})
report={'preserved_actions':{name:action_hash(bpy.data.actions[name])==digest for name,digest in preserved.items()},'mesh_uv_transform_unchanged':geometry_hash(mesh)==original_geometry,'new_actions':[x[0] for x in new_clips],'finger_controls':'Not added; existing open hand deforms with hand bone.','export_performed':False}
(OUT/'Source/user_mom_household_preservation.json').write_text(json.dumps(report,indent=2))
(OUT/'Source/user_mom_household_manifest.json').write_text(json.dumps(manifest,indent=2))
rig.animation_data.action=bpy.data.actions['MomUser_Idle'];scene.frame_start=1;scene.frame_end=121;scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Household.blend'))
print(json.dumps(report))
