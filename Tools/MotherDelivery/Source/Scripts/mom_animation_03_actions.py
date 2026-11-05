import bpy,math,json
from pathlib import Path
from mathutils import Vector,Quaternion
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
scene=bpy.context.scene;rig=bpy.data.objects['Mom_User_Deform'];mesh=bpy.data.objects['Mom_User_Character']
assert not any(a.name.startswith('MomUser_') for a in bpy.data.actions),'Do not overwrite existing animation work.'
order=list(rig.data.bones);REST={b.name:b.matrix_local.copy() for b in order};HEAD={b.name:b.head_local.copy() for b in order};TAIL={b.name:b.tail_local.copy() for b in order}
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def fkhead(name,matrices):
 parent=rig.data.bones[name].parent
 return matrices[parent.name]@REST[parent.name].inverted()@HEAD[name] if parent else HEAD[name].copy()
def fk(name,matrices,rotation=None):
 head=fkhead(name,matrices);matrix=REST[name].copy()
 if rotation:matrix=rotation.to_matrix().to_4x4()@REST[name]
 matrix.translation=head;matrices[name]=matrix
def orient(head,tail,rest):
 axis=rest.to_3x3()@Vector((0,1,0));q=axis.rotation_difference((tail-head).normalized());matrix=(q.to_matrix()@rest.to_3x3()).to_4x4();matrix.translation=head;return matrix
def solve(a,c,l1,l2,pole):
 delta=c-a;distance=max(.00001,min(delta.length,l1+l2-.00001));direction=delta.normalized();c=a+direction*distance
 x=(l1*l1-l2*l2+distance*distance)/(2*distance);height=math.sqrt(max(.000000001,l1*l1-x*x));p=Vector(pole)-a;p-=direction*p.dot(direction)
 if p.length<.00001:p=direction.cross(Vector((1,0,0)))
 return a+direction*x+p.normalized()*height,c
def pose(kind,t):
 breath=math.sin(math.tau*t);walk=kind=='MomUser_Walk_InPlace';nod=kind=='MomUser_Nod'
 hips=Vector((.0015*breath,0,.0008*(1-math.cos(math.tau*t))))
 if walk:hips=Vector((.0025*breath,0,.003*(1-math.cos(4*math.pi*t))))
 matrices={'Root':REST['Root'].copy(),'Hips':REST['Hips'].copy()};matrices['Hips'].translation=HEAD['Hips']+hips
 lean=.004*breath if not walk else .007*breath
 for name,factor in [('Spine',.3),('Chest',.7),('UpperChest',1)]:fk(name,matrices,Quaternion((1,0,0),lean*factor))
 pitch=0
 if nod:
  envelope=smooth(t/.20)*(1-smooth((t-.76)/.24));pitch=.13*math.sin(math.pi*min(1,max(0,(t-.18)/.52)))*envelope
 fk('Neck',matrices,Quaternion((1,0,0),pitch*.25))
 fk('Head',matrices,Quaternion((1,0,0),pitch))
 for side,sign in [('L',1),('R',-1)]:
  suffix='.'+side;fk('Shoulder'+suffix,matrices,Quaternion((1,0,0),lean))
  ankle=HEAD['Foot'+suffix].copy();footrot=Quaternion((1,0,0),0)
  if walk:
   phase=math.tau*t+(0 if side=='L' else math.pi)
   ankle.y+=.070*math.cos(phase);ankle.z+=.025*max(0,math.sin(phase))**1.4
   footrot=Quaternion((1,0,0),.045*max(0,math.sin(phase)))
  hip=fkhead('UpperLeg'+suffix,matrices);knee,ankle=solve(hip,ankle,(TAIL['UpperLeg'+suffix]-HEAD['UpperLeg'+suffix]).length,(TAIL['LowerLeg'+suffix]-HEAD['LowerLeg'+suffix]).length,(sign*.11,-.6,.28))
  matrices['UpperLeg'+suffix]=orient(hip,knee,REST['UpperLeg'+suffix]);matrices['LowerLeg'+suffix]=orient(knee,ankle,REST['LowerLeg'+suffix])
  matrices['Foot'+suffix]=footrot.to_matrix().to_4x4()@REST['Foot'+suffix];matrices['Foot'+suffix].translation=ankle
  matrices['Toes'+suffix]=matrices['Foot'+suffix]@REST['Foot'+suffix].inverted()@REST['Toes'+suffix]
  wrist=Vector((-.004+sign*.185,-.005,.590));wrist.y-=.0015*breath;wrist.z+=.001*breath
  if walk:wrist.y+=.027*math.cos(math.tau*t+(math.pi if side=='L' else 0))
  shoulder=fkhead('UpperArm'+suffix,matrices);elbow,wrist=solve(shoulder,wrist,(TAIL['UpperArm'+suffix]-HEAD['UpperArm'+suffix]).length,(TAIL['LowerArm'+suffix]-HEAD['LowerArm'+suffix]).length,(sign*.27,-.015,.73))
  matrices['UpperArm'+suffix]=orient(shoulder,elbow,REST['UpperArm'+suffix]);matrices['LowerArm'+suffix]=orient(elbow,wrist,REST['LowerArm'+suffix])
  matrices['Hand'+suffix]=matrices['LowerArm'+suffix]@REST['LowerArm'+suffix].inverted()@REST['Hand'+suffix];matrices['Hand'+suffix].translation=wrist
 return matrices
clips=[('MomUser_Idle',4,True),('MomUser_Walk_InPlace',1.2,True),('MomUser_Nod',2.4,False)];report=[]
rig.animation_data_create();scene.render.fps=30
for name,duration,loop in clips:
 action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action;frames=round(duration*30)+1;previous={}
 for frame in range(1,frames+1):
  matrices=pose(name,(frame-1)/(frames-1))
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
 report.append({'action':name,'fps':30,'frame_start':1,'frame_end':frames,'duration_seconds':duration,'loop':loop,'in_place':True})
rig.animation_data.action=bpy.data.actions['MomUser_Idle'];scene.frame_start=1;scene.frame_end=121;scene.frame_set(1)
for obj in scene.objects:
 if obj.type in {'ARMATURE','EMPTY','CAMERA','LIGHT'}:obj.hide_set(True)
mesh.hide_set(False)
(OUT/'Source/user_mom_animation_manifest.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Animated.blend'))
print(json.dumps(report))
