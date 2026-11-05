exec(compile(open('E:/BLENDER/mother_common.py',encoding='utf8').read(),'mother_common.py','exec'))
import collections
ctrl=bpy.data.objects['Mother_Control'];rig=bpy.data.objects['Mother_Deform'];ctrl.hide_set(False)
order=list(ctrl.data.bones);REST={b.name:b.matrix_local.copy() for b in order};HEAD={b.name:b.head_local.copy() for b in order};TAIL={b.name:b.tail_local.copy() for b in order}
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def envelope(t,a=.18,b=.37,c=.68,d=.91):return smooth((t-a)/(b-a))*(1-smooth((t-c)/(d-c)))
def orient(head,tail,rest):
 delta=(rest.to_3x3()@Vector((0,1,0))).rotation_difference((tail-head).normalized());m=(delta.to_matrix()@rest.to_3x3()).to_4x4();m.translation=head;return m
def basis(head,long,normal):
 y=Vector(long).normalized();z=Vector(normal)-y*Vector(normal).dot(y);z.normalize();x=y.cross(z).normalized();z=x.cross(y).normalized();m=Matrix((x,y,z)).transposed().to_4x4();m.translation=head;return m
def solve(a,c,l1,l2,pole):
 delta=c-a;dist=max(.0001,min(delta.length,l1+l2-.00005));direction=delta.normalized();c=a+direction*dist
 x=(l1*l1-l2*l2+dist*dist)/(2*dist);h=math.sqrt(max(.000001,l1*l1-x*x));p=Vector(pole)-a;p-=direction*p.dot(direction)
 if p.length<.0001:p=direction.cross(Vector((1,0,0)))
 b=a+direction*x+p.normalized()*h;return b,c
def fkhead(name,M):
 p=ctrl.data.bones[name].parent
 return (M[p.name]@REST[p.name].inverted()@HEAD[name]) if p else HEAD[name].copy()
def fk(name,M,rot=None):
 head=fkhead(name,M);m=REST[name].copy();m.translation=head
 if rot:m=rot.to_matrix().to_4x4()@REST[name];m.translation=head
 M[name]=m
def pose(kind,t):
 s=0.;lean=0.;hips=Vector((0,0,0));yaw=0.;headpitch=0.;headyaw=0.;gesture=envelope(t);breath=math.sin(t*TAU)
 if kind in ['Mother_Idle_Seated','Mother_PrepVegetables','Mother_PointToStove','Mother_NodApprove']:s=1
 if kind=='Mother_SitDown':s=smooth(t)
 if kind=='Mother_StandUp':s=1-smooth(t)
 lean=.13*s
 if kind in ['Mother_SitDown','Mother_StandUp']:lean+=.26*math.sin(t*math.pi)**2
 hips.y=.38*s;hips.z=-.39*s
 if kind in ['Mother_Idle_Standing','Mother_Idle_Seated']:
  lean+=.007*breath;hips.x=.003*math.sin(t*TAU);hips.z+=.0012*(1-math.cos(t*TAU));headyaw=.018*math.sin(t*TAU)
 if kind=='Mother_PrepVegetables':lean=.18+.015*math.sin(t*TAU*2);headpitch=.13
 if kind=='Mother_CallChild':headyaw=-.24*gesture;headpitch=-.035*gesture
 if kind=='Mother_PointToStove':lean+=.06*gesture;headyaw=-.15*gesture;headpitch=-.06*gesture
 if kind=='Mother_NodApprove':
  headpitch=(-.055*envelope(t,.05,.2,.36,.48)+.14*envelope(t,.32,.47,.59,.77));headyaw=-.07*gesture
 if kind=='Mother_CheckPot':lean=.32*gesture;hips.y=.035*gesture;headpitch=.27*gesture;headyaw=-.06*gesture
 if kind=='Mother_Walk':hips.z=.006*(1-math.cos(4*math.pi*t));hips.x=.006*math.sin(t*TAU);yaw=.018*math.sin(t*TAU);headpitch=.008*math.sin(t*TAU)
 # Exactly shared transition end states; feet are solved against fixed floor targets.
 M={};M['Root']=REST['Root'].copy();hr=Quaternion((0,0,1),yaw)@Quaternion((1,0,0),lean*.16);M['Hips']=hr.to_matrix().to_4x4()@REST['Hips'];M['Hips'].translation=HEAD['Hips']+hips
 for n,portion in [('Spine',.45),('Chest',.8),('UpperChest',1.)]:fk(n,M,Quaternion((0,0,1),yaw*.6)@Quaternion((1,0,0),lean*portion))
 fk('Neck',M,Quaternion((0,0,1),headyaw*.35)@Quaternion((1,0,0),lean*.40+headpitch*.35))
 fk('Head',M,Quaternion((0,0,1),headyaw)@Quaternion((1,0,0),lean*.25+headpitch))
 for side,sign in [('L',1),('R',-1)]:
  suf='.'+side;fk('Shoulder'+suf,M,Quaternion((0,0,1),yaw*.6)@Quaternion((1,0,0),lean))
  ankle=HEAD['Foot'+suf].copy();fr=Quaternion((1,0,0),0)
  ankle.x*=.82+.18*s
  if kind=='Mother_Walk':
   phase=t*TAU+(0 if side=='L' else math.pi);ankle.y+=.095*math.cos(phase);ankle.z+=.045*max(0,math.sin(phase))**1.3;fr=Quaternion((1,0,0),.08*max(0,math.sin(phase)))
  hip=fkhead('UpperLeg'+suf,M);knee,ankle=solve(hip,ankle,(TAIL['UpperLeg'+suf]-HEAD['UpperLeg'+suf]).length,(TAIL['LowerLeg'+suf]-HEAD['LowerLeg'+suf]).length,(sign*.15,-1,.5))
  M['UpperLeg'+suf]=orient(hip,knee,REST['UpperLeg'+suf]);M['LowerLeg'+suf]=orient(knee,ankle,REST['LowerLeg'+suf]);M['Foot'+suf]=fr.to_matrix().to_4x4()@REST['Foot'+suf];M['Foot'+suf].translation=ankle
  M['Toes'+suf]=M['Foot'+suf]@REST['Foot'+suf].inverted()@REST['Toes'+suf]
  standing=Vector((sign*.225,-.100,.865));seated=Vector((sign*.13,.035,.548));wrist=standing.lerp(seated,s);long=Vector((sign*.04,-.12,-1)).lerp(Vector((sign*.08,-1,-.15)),s);normal=Vector((-sign,0,-.1)).lerp(Vector((0,0,-1)),s);curl=.10+.38*s
  if kind in ['Mother_Idle_Standing','Mother_Idle_Seated']:wrist.y-=.002*breath;wrist.z+=.0015*breath
  if kind=='Mother_PrepVegetables':
   # Asymmetric pick/strip/return beats, with held intervals between reaches.
   beat=math.sin(t*TAU*3+(0 if side=='R' else 1.15));work=max(0,beat)**2;pause=math.sin(t*TAU*2)**2
   if side=='R':wrist+=Vector((.035*math.sin(t*TAU*3),-.035*work,.028*work));curl=.45+.35*work
   else:wrist+=Vector((-.012*pause,.010*math.sin(t*TAU*2),.007*pause));curl=.55
  if kind=='Mother_CallChild' and side=='R':
   wrist=wrist.lerp(Vector((-.29,-.235,1.30)),gesture);long=long.lerp(Vector((-.08,0,1)),gesture);normal=normal.lerp(Vector((0,-1,0)),gesture);curl=.22+gesture*(.14+.25*math.sin((t-.3)*TAU*2))
  if kind=='Mother_PointToStove' and side=='R':wrist=wrist.lerp(Vector((-.28,-.12,.83)),gesture);long=long.lerp(Vector((.38,-.85,-.35)),gesture);normal=normal.lerp(Vector((0,0,-1)),gesture);curl=.48+.5*gesture
  if kind=='Mother_CheckPot' and side=='R':wrist=wrist.lerp(Vector((-.18,-.465,.965)),gesture);long=long.lerp(Vector((.10,-1,-.12)),gesture);normal=normal.lerp(Vector((0,0,-1)),gesture);curl=.22+.18*gesture
  if kind=='Mother_Walk':wrist.y+=.050*math.cos(t*TAU+(math.pi if side=='L' else 0));wrist.z+=.008*math.sin(t*TAU)
  shoulder=fkhead('UpperArm'+suf,M);pole=Vector((sign*.24,-.09,1.08)).lerp(Vector((sign*.52,.015,.69)),s);elbow,wrist=solve(shoulder,wrist,(TAIL['UpperArm'+suf]-HEAD['UpperArm'+suf]).length,(TAIL['LowerArm'+suf]-HEAD['LowerArm'+suf]).length,pole)
  # Keep the relaxed wrist in line with the forearm; blend into the deliberate
  # gesture orientation instead of imposing a hard bend at the hand joint.
  hand_blend=s
  if kind in ['Mother_CallChild','Mother_CheckPot']:hand_blend=max(s,gesture)
  long=(wrist-elbow).normalized().lerp(long.normalized(),hand_blend)
  M['UpperArm'+suf]=orient(shoulder,elbow,REST['UpperArm'+suf]);M['LowerArm'+suf]=orient(elbow,wrist,REST['LowerArm'+suf]);M['Hand'+suf]=basis(wrist,long,normal)
  natural=M['LowerArm'+suf]@REST['LowerArm'+suf].inverted()@REST['Hand'+suf]
  rotation=natural.to_quaternion().slerp(M['Hand'+suf].to_quaternion(),hand_blend)
  M['Hand'+suf]=rotation.to_matrix().to_4x4();M['Hand'+suf].translation=wrist
  for finger in ['Thumb','Index','Middle','Ring','Little']:
   for j,part in enumerate(['Proximal','Intermediate','Distal']):
    name=finger+part+suf;p=ctrl.data.bones[name].parent.name;angle=curl*(.65 if finger=='Thumb' else 1)*(1.0 if j<2 else .7)
    if kind=='Mother_PointToStove' and side=='R' and finger=='Index':angle*=1-gesture
    # Ease thumb opposition independently from the four longer fingers.
    local=Quaternion((1,0,0),angle)
    if finger=='Thumb' and j==0:local=Quaternion((0,1,0),.22+.2*s)@local
    M[name]=M[p]@REST[p].inverted()@REST[name]@local.to_matrix().to_4x4()
 return M

clips=[('Mother_Idle_Standing',5,True),('Mother_SitDown',2.6,False),('Mother_Idle_Seated',5,True),('Mother_PrepVegetables',7.2,True),('Mother_StandUp',2.6,False),('Mother_CallChild',3.2,False),('Mother_PointToStove',2.8,False),('Mother_NodApprove',2,False),('Mother_Walk',1.2,True),('Mother_CheckPot',3.6,False)]
ctrl.animation_data_create();rig.animation_data_create();metadata=[]
for name,duration,loop in clips:
 ca=bpy.data.actions.new('CTRL_'+name);ca.use_fake_user=True;ctrl.animation_data.action=ca
 da=bpy.data.actions.new(name);da.use_fake_user=True;rig.animation_data.action=da
 frames=round(duration*30)+1;prev={};maxdelta=0
 for frame in range(1,frames+1):
  t=(frame-1)/(frames-1);M=pose(name,t)
  for bone in order:
   n=bone.name;p=bone.parent;basis0=(M[p.name]@REST[p.name].inverted()@REST[n]) if p else REST[n]
   local=basis0.inverted()@M[n];loc,rot,scale=local.decompose()
   if n in prev and rot.dot(prev[n])<0:rot.negate()
   prev[n]=rot.copy()
   for ob in [ctrl,rig]:
    pb=ob.pose.bones[n];pb.location=loc;pb.rotation_quaternion=rot;pb.scale=(1,1,1)
    pb.keyframe_insert('location',frame=frame,group=n);pb.keyframe_insert('rotation_quaternion',frame=frame,group=n);pb.keyframe_insert('scale',frame=frame,group=n)
 # Linear sampled curves preserve the solved contact poses without Bezier overshoot.
 for act in [ca,da]:
  for layer in act.layers:
   for strip in layer.strips:
    for bag in strip.channelbags:
     for fc in bag.fcurves:
      for k in fc.keyframe_points:k.interpolation='LINEAR'
 metadata.append({'name':name,'fps':30,'frame_start':1,'frame_end':frames,'duration_seconds':duration,'loop':loop,'in_place':True,'sample_step':1})
 print('BAKED',name,frames,'frames')
for p in rig.pose.bones:
 for c in p.constraints:c.mute=True
ctrl.hide_set(True);ctrl.animation_data.action=None
rig.animation_data.action=bpy.data.actions['Mother_Idle_Standing'];S.frame_start=1;S.frame_end=151;S.frame_set(1)
(OUT/'Source/animation_manifest.json').write_text(json.dumps(metadata,indent=2),encoding='utf8')
save('05_TenBakedAnimations')
render('05_IdleStanding',(1.8,-3.6,1.6),(0,0,.82),65)
rig.animation_data.action=bpy.data.actions['Mother_PrepVegetables'];S.frame_set(50)
render('06_Seated_FirstRigCheck',(1.8,-3.4,1.5),(0,0,.65),65)
save('05_TenBakedAnimations')
