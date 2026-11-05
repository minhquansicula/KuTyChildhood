exec(compile(open('E:/BLENDER/mother_common.py',encoding='utf8').read(),'mother_common.py','exec'))
import bmesh,collections
D=json.loads((RES/'mother_geometry.json').read_text());J={k:Vector(v) for k,v in D['joints'].items()};CV=np.array(D['vertices']);WV={int(k):v for k,v in D['weights'].items()}
bones=[]
def add(name,head,tail,parent):bones.append((name,Vector(head),Vector(tail),parent))
add('Root',(0,0,0),(0,0,.12),None)
add('Hips',J['spine05____head'],J['spine04____head'],'Root')
add('Spine',J['spine04____head'],J['spine02____head'],'Hips')
add('Chest',J['spine02____head'],J['spine01____head'],'Spine')
add('UpperChest',J['spine01____head'],J['neck01____head'],'Chest')
add('Neck',J['neck01____head'],J['head____head'],'UpperChest')
add('Head',J['head____head'],J['head____tail'],'Neck')
palmnorm={}
for side in ['L','R']:
 suf='.'+side
 add('Shoulder'+suf,J['clavicle'+suf+'____head'],J['upperarm01'+suf+'____head'],'UpperChest')
 add('UpperArm'+suf,J['upperarm01'+suf+'____head'],J['lowerarm01'+suf+'____head'],'Shoulder'+suf)
 add('LowerArm'+suf,J['lowerarm01'+suf+'____head'],J['wrist'+suf+'____head'],'UpperArm'+suf)
 handtail=(J['finger2-1'+suf+'____head']+J['finger5-1'+suf+'____head'])/2
 add('Hand'+suf,J['wrist'+suf+'____head'],handtail,'LowerArm'+suf)
 across=J['finger2-1'+suf+'____head']-J['finger5-1'+suf+'____head'];length=J['finger3-3'+suf+'____tail']-J['finger3-1'+suf+'____head'];normal=across.cross(length).normalized()
 if normal.x*(1 if side=='L' else -1)>0:normal=-normal
 palmnorm[side]=normal
 for i,finger in enumerate(['Thumb','Index','Middle','Ring','Little'],1):
  for j,part in enumerate(['Proximal','Intermediate','Distal'],1):
   src=f'finger{i}-{j}{suf}';parent='Hand'+suf if j==1 else finger+['Proximal','Intermediate'][j-2]+suf
   add(finger+part+suf,J[src+'____head'],J[src+'____tail'],parent)
 add('UpperLeg'+suf,J['upperleg01'+suf+'____head'],J['lowerleg01'+suf+'____head'],'Hips')
 add('LowerLeg'+suf,J['lowerleg01'+suf+'____head'],J['foot'+suf+'____head'],'UpperLeg'+suf)
 add('Foot'+suf,J['foot'+suf+'____head'],J['foot'+suf+'____tail'],'LowerLeg'+suf)
 add('Toes'+suf,J['foot'+suf+'____tail'],J.get('toe3-2'+suf+'____tail',J['foot'+suf+'____tail']+Vector((0,-.06,-.015))),'Foot'+suf)

def armature(name):
 arm=bpy.data.armatures.new(name+'_Skeleton');ob=bpy.data.objects.new(name,arm);S.collection.objects.link(ob);active(ob);bpy.ops.object.mode_set(mode='EDIT')
 for name,h,t,parent in bones:
  b=arm.edit_bones.new(name);b.head=h;b.tail=t
  if parent:b.parent=arm.edit_bones[parent]
  side=name[-1] if name.endswith(('.L','.R')) else None
  normal=palmnorm[side] if side and name.startswith(('Hand','Thumb','Index','Middle','Ring','Little')) else Vector((0,-1,0))
  if abs((b.tail-b.head).normalized().dot(normal))>.95:normal=Vector((1,0,0))
  b.align_roll(normal)
 bpy.ops.object.mode_set(mode='OBJECT');ob.show_in_front=True;arm.display_type='OCTAHEDRAL'
 for p in ob.pose.bones:p.rotation_mode='QUATERNION'
 return ob
deform=armature('Mother_Deform');control=armature('Mother_Control')
root=em('SK_Mother');deform.parent=root;control['purpose']='Editable FK control rig; CTRL_ actions. Deform rig holds fully sampled animation for independent FBX export.'
for p in deform.pose.bones:
 con=p.constraints.new('COPY_TRANSFORMS');con.name='SourceControl_'+p.name;con.target=control;con.subtarget=p.name;con.target_space='WORLD';con.owner_space='WORLD';con.mute=True
char=bpy.data.collections['CHARACTER_Mother']
for o in list(char.objects):
 if o.type!='MESH':continue
 o.parent=root;mod=o.modifiers.new('Mother skinning','ARMATURE');mod.object=deform;mod.use_deform_preserve_volume=False
 # Normalize and limit weights exactly as the Unity LBS path (four influences).
 valid=set(deform.data.bones.keys());groups={g.index:g.name for g in o.vertex_groups}
 for v in o.data.vertices:
  ws=sorted([(groups[g.group],g.weight) for g in v.groups if groups[g.group] in valid and g.weight>.0001],key=lambda x:-x[1])[:4]
  if not ws:ws=[('Head' if v.co.z>1.34 else 'Hips',1)]
  total=sum(w for _,w in ws)
  for g in o.vertex_groups:
   try:g.remove([v.index])
   except RuntimeError:pass
  for name,w in ws:
   vg=o.vertex_groups.get(name) or o.vertex_groups.new(name=name);vg.add([v.index],w/total,'REPLACE')

# Four sockets are real bone-parented Empty objects; no Blender-only constraint dependency.
bpy.context.view_layer.update()
def socket(name,bone,position,forward,up):
 z=Vector(forward).normalized();x=Vector(up).cross(z).normalized();y=z.cross(x).normalized();m=Matrix((x,y,z)).transposed().to_4x4();m.translation=Vector(position)
 o=em(name);o.parent=deform;o.parent_type='BONE';o.parent_bone=bone;o.matrix_world=m;o['axes']='+Z forward/grip direction; +Y palm normal (hand) or head up; +X right.';return o
for side in ['R','L']:
 b=deform.data.bones['Hand.'+side];p=b.head_local+(b.tail_local-b.head_local)*.62+palmnorm[side]*.004
 socket('Socket_Hand_'+side,'Hand.'+side,p,b.tail_local-b.head_local,palmnorm[side])
socket('Socket_LookAt','Head',(J['joint-l-eye']+J['joint-r-eye'])/2+Vector((0,-.009,0)),(0,-1,0),(0,0,1))
socket('Socket_Voice','Head',(0,-.149,1.410),(0,-1,0),(0,0,1))
rigcol=bpy.data.collections.new('RIGS_ControlAndDeform');S.collection.children.link(rigcol)
for o in [root,deform,control]+[bpy.data.objects[n] for n in ['Socket_Hand_R','Socket_Hand_L','Socket_LookAt','Socket_Voice']]:
 for c in list(o.users_collection):c.objects.unlink(o)
 rigcol.objects.link(o)
control.hide_set(True)
(OUT/'Source/rig_manifest.json').write_text(json.dumps({'height_m':max(v.co.z for o in char.objects if o.type=='MESH' for v in o.data.vertices),'bones':[{'name':n,'head':list(h),'tail':list(t),'parent':p} for n,h,t,p in bones],'palm_normals':{s:list(n) for s,n in palmnorm.items()}},indent=2),encoding='utf8')
save('04_RigWeightsSockets')
print('RIG',len(bones),'bones; 4 sockets; normalized <=4 weights. A-pose is rest pose.')
