exec(compile(open('E:/BLENDER/mother_common.py',encoding='utf8').read(),'mother_common.py','exec'))
import collections,bmesh
# Start a separate source file; previously saved kitchen files remain untouched on disk.
old=S;S=bpy.data.scenes.new('Mother_Source');bpy.context.window.scene=S
for sc in list(bpy.data.scenes):
 if sc!=S:bpy.data.scenes.remove(sc)
for ob in list(bpy.data.objects):
 if ob.name not in S.objects:bpy.data.objects.remove(ob,do_unlink=True)
S.unit_settings.system='METRIC';S.unit_settings.scale_length=1;S.render.fps=30
# Read CC0 MakeHuman production topology, morph with Asian female young/age blend.
verts=[];tex=[];faces=[];fuv=[];groups=[];g='body'
for l in (RES/'base.obj').read_text().splitlines():
 t=l.split()
 if not t:continue
 if t[0]=='v':verts.append(list(map(float,t[1:4])))
 elif t[0]=='vt':tex.append(list(map(float,t[1:3])))
 elif t[0]=='g':g=t[1]
 elif t[0]=='f':
  faces.append([int(x.split('/')[0])-1 for x in t[1:]]);fuv.append([tex[int(x.split('/')[1])-1] for x in t[1:]]);groups.append(g)
V=np.array(verts,dtype=np.float64)
for filename,weight in [('asian-female-young.target',.83),('asian-female-old.target',.17),('universal-female-young-averagemuscle-averageweight.target',.83),('universal-female-old-averagemuscle-averageweight.target',.17)]:
 for l in (RES/filename).read_text().splitlines():
  t=l.split()
  if t and not t[0].startswith('#') and len(t)==4:V[int(t[0])]+=np.array(list(map(float,t[1:])))*weight
bodyids=set(i for f,g in zip(faces,groups) if g=='body' for i in f)
lo=min(V[i,1] for i in bodyids);hi=max(V[i,1] for i in bodyids);scale=1.562/(hi-lo)
CV=np.stack((V[:,0]*scale,-V[:,2]*scale,(V[:,1]-lo)*scale+.014),axis=1)
skel=json.loads((RES/'default.mhskel').read_text());J={name:np.mean(CV[ids],axis=0).tolist() for name,ids in skel['joints'].items()}
G={}
for f,g in zip(faces,groups):G.setdefault(g,set()).update(f)
for name,ids in G.items():
 if name.startswith('joint-'):J[name]=np.mean(CV[list(ids)],axis=0).tolist()
weights=json.loads((RES/'default_weights.mhw').read_text())['weights'];WV=collections.defaultdict(dict)
def bone_map(name):
 side=name[-2:] if name.endswith(('.L','.R')) else ''
 if name.startswith('finger'):
  a,b=name[6:-2].split('-');return ['Thumb','Index','Middle','Ring','Little'][int(a)-1]+['Proximal','Intermediate','Distal'][int(b)-1]+side
 if name.startswith('metacarpal') or name.startswith('wrist'):return 'Hand'+side
 if name.startswith('upperarm'):return 'UpperArm'+side
 if name.startswith('lowerarm'):return 'LowerArm'+side
 if name.startswith('upperleg'):return 'UpperLeg'+side
 if name.startswith('lowerleg'):return 'LowerLeg'+side
 if name.startswith('foot'):return 'Foot'+side
 if name.startswith('toe'):return 'Toes'+side
 if name.startswith(('clavicle','shoulder')):return 'Shoulder'+side
 if name in ['root','pelvis.L','pelvis.R']:return 'Hips'
 if name in ['spine05','spine04']:return 'Spine'
 if name in ['spine03','spine02'] or name.startswith('breast'):return 'Chest'
 if name=='spine01':return 'UpperChest'
 if name.startswith('neck'):return 'Neck'
 return 'Head'
for name,items in weights.items():
 bn=bone_map(name)
 for i,w in items:WV[i][bn]=WV[i].get(bn,0)+w
for i,ws in WV.items():
 total=sum(ws.values())
 for k in ws:ws[k]/=total
kd=KDTree(len(bodyids))
for i in bodyids:kd.insert(Vector(CV[i]),i)
kd.balance()
def groupmesh(name,selected,mat,adjust=None):
 ids=sorted({i for k in selected for i in faces[k]});index={v:i for i,v in enumerate(ids)};vs=[CV[i].copy() for i in ids]
 if adjust:vs=[adjust(i,v.copy()) for i,v in zip(ids,vs)]
 ob=mesh(name,[list(p) for p in vs],[[index[i] for i in faces[k]] for k in selected],mat,[fuv[k] for k in selected])
 sources=[]
 for i in ids:sources.append(i if i in WV else kd.find(Vector(CV[i]))[1])
 for vi,src in enumerate(sources):
  for bn,w in WV[src].items():
   vg=ob.vertex_groups.get(bn) or ob.vertex_groups.new(name=bn);vg.add([vi],w,'REPLACE')
 ob['source_indices']=ids;return ob,ids
skin=material('Mother_Skin',(.50,.305,.185),.52,'skin')
shirtmat=material('Mother_Blouse',(.135,.061,.047),.88,'cloth')
pantsmat=material('Mother_Trousers',(.028,.026,.031),.91,'cloth')
hairmat=material('Mother_Hair',(.016,.012,.010),.62,'hair')
eyemat=material('Mother_Eyes',(.72,.68,.58),.32,'eye',1024)
# Skin only where visible: head/neck, hands and lower ankles/feet.
selected=[]
for k,(f,g) in enumerate(zip(faces,groups)):
 if g!='body':continue
 ws=collections.defaultdict(float)
 for i in f:
  for bn,w in WV[i].items():ws[bn]+=w/len(f)
 if ws['Head']+ws['Neck']>.55 or sum(w for bn,w in ws.items() if any(bn.startswith(x) for x in ['Hand','Thumb','Index','Middle','Ring','Little']))>.67 or (np.mean(CV[f,2])<.12):selected.append(k)
body,body_src=groupmesh('Mother_SkinMesh',selected,skin)
# Clothing from the low-polygon full-body fitting cage, with original skin weights transferred.
def region(k):
 f=faces[k];c=np.mean(CV[f],axis=0);src=kd.find(Vector(c))[1];return c,WV[src]
up=[];down=[]
for k,g in enumerate(groups):
 if g!='helper-tights':continue
 c,w=region(k)
 if .76<c[2]<1.365 and abs(c[0])<.61 and not any(w.get(n+s,0)>.45 for n in ['Hand','ThumbProximal','IndexProximal','MiddleProximal'] for s in ['.L','.R']):up.append(k)
 if .085<c[2]<.92:down.append(k)
def shirt_adjust(i,p):
 src=kd.find(Vector(CV[i]))[1];w=WV[src];arm=sum(w.get(n+s,0) for n in ['UpperArm','LowerArm'] for s in ['.L','.R'])
 if arm>.3:
  p[0]+=math.copysign(.006,p[0]);p[1]+=.008*math.cos(p[2]*60);p[2]+=.008
 else:
  # Relax bust/waist into a simple everyday buttoned blouse, rather than tight bodywear.
  p[0]*=1.06
  center=-.004;dist=p[1]-center;p[1]=center+math.copysign(max(abs(dist)+.012,.085 if p[2]>.93 else .10),dist)
  p[1]+=.0025*math.sin(p[2]*90+p[0]*18);p[0]+=.0015*math.sin(p[2]*120)
 return p
blouse,shirt_src=groupmesh('Mother_BlouseMesh',up,shirtmat,shirt_adjust)
def pants_adjust(i,p):
 s=1 if p[0]>0 else -1;cx=.087*s;p[0]=cx+(p[0]-cx)*1.16;p[1]*=1.18;p[1]+=.003*math.sin(p[2]*85+p[0]*25);return p
trousers,pant_src=groupmesh('Mother_TrousersMesh',down,pantsmat,pants_adjust)
# Real collar/cuff edge thickness, limited to boundaries rather than a hidden full lining.
for ob in [blouse,trousers]:
 active(ob);mod=ob.modifiers.new('Garment edge thickness','SOLIDIFY');mod.thickness=.002;mod.offset=0;mod.use_rim_only=True;bpy.ops.object.modifier_apply(modifier=mod.name)
# Low bun + fitted scalp, built from head fitting cage, subdivided once for silhouette.
hairsel=[k for k,g in enumerate(groups) if g=='helper-hair']
hair,hair_src=groupmesh('Mother_HairMesh',hairsel,hairmat,lambda i,p:p+np.array([0,.001,.002]))
active(hair);sub=hair.modifiers.new('Scalp smoothing','SUBSURF');sub.levels=1;bpy.ops.object.modifier_apply(modifier=sub.name)
for vg in list(hair.vertex_groups):hair.vertex_groups.remove(vg)
vg=hair.vertex_groups.new(name='Head');vg.add(list(range(len(hair.data.vertices))),1,'REPLACE')
headcenter=np.array(J['head____head']);bun=ellipsoid('Mother_LowBun',(0,.094,1.392),(.060,.043,.047),hairmat,32,16)
vg=bun.vertex_groups.new(name='Head');vg.add(list(range(len(bun.data.vertices))),1,'REPLACE')
hair=join([hair,bun],'Mother_HairMesh')
# Eyeballs: radial iris texture uses the front pole, not oversized cartoon eyes.
size=1024;yy,xx=np.mgrid[0:size,0:size].astype(np.float32)/size
base=np.zeros((size,size,3),dtype=np.float32)+np.array([.68,.66,.57])
iris=yy<.18;pupil=yy<.075;fibers=np.sin(xx*TAU*120+np.sin(yy*TAU*12)*2)*.018
base[iris]=np.stack((.095+fibers,.045+fibers*.5,.018+fibers*.3),axis=-1)[iris];base[pupil]=(.004,.003,.002)
base[xx>.85]=(.009,.005,.004)
image=bpy.data.images['Mother_Eyes_BaseColor'];rgba=np.ones((size,size,4),dtype=np.float32);rgba[:,:,:3]=base;image.pixels.foreach_set(rgba.ravel());image.filepath_raw=str(OUT/'Textures/Mother_Eyes_BaseColor.png');image.save()
eyeparts=[]
for side in ['l','r']:
 center=np.array(J['joint-'+side+'-eye']);r=.0110;vs=[];uv=[];fs=[];n=32;nr=16
 for j in range(nr+1):
  th=math.pi*j/nr
  for i in range(n+1):
   a=TAU*i/n;vs.append(list(center+np.array([r*math.sin(th)*math.cos(a),-r*math.cos(th),r*math.sin(th)*math.sin(a)])));uv.append((i/n*.8,j/nr))
 for j in range(nr):
  for i in range(n):
   a=j*(n+1)+i;fs.append((a,a+1,a+n+2,a+n+1))
 e=mesh('Mother_Eye_'+side,vs,fs,eyemat,[[uv[i] for i in f] for f in fs]);vg=e.vertex_groups.new(name='Head');vg.add(list(range(len(vs))),1,'REPLACE');eyeparts.append(e)
eyes=join(eyeparts,'Mother_EyesMesh')
# Sandals are independent, skin-weighted to each foot; pants material keeps 5 materials total.
shoes=[]
for side in ['L','R']:
 x=J['foot.'+side+'____head'][0];foot=np.array(J['foot.'+side+'____head']);toe=np.array(J['foot.'+side+'____tail'])
 shoe=ellipsoid('Sandal_'+side,(x,-.025,.013),(.046,.112,.012),pantsmat,32,12);vg=shoe.vertex_groups.new(name='Foot.'+side);vg.add(list(range(len(shoe.data.vertices))),1,'REPLACE');shoes.append(shoe)
 for sign in [-1,1]:
  ob=cube('Sandal_Strap',(x+sign*.018,-.053,.049),(.018,.067,.008),pantsmat,.004);ob.rotation_euler.y=sign*.45;vg=ob.vertex_groups.new(name='Foot.'+side);vg.add(list(range(len(ob.data.vertices))),1,'REPLACE');shoes.append(ob)
sandals=join(shoes,'Mother_SandalsMesh')
# UV-aware face tint and fine age detail will be baked after the first proportion review.
data={'scale':scale,'original_low':lo,'vertices':CV.tolist(),'joints':J,'weights':{str(i):w for i,w in WV.items()},'body_indices':body_src,'shirt_indices':shirt_src,'pants_indices':pant_src}
(RES/'mother_geometry.json').write_text(json.dumps(data),encoding='utf8')
character=bpy.data.collections.new('CHARACTER_Mother');S.collection.children.link(character)
for ob in [body,blouse,trousers,hair,eyes,sandals]:
 for c in list(ob.users_collection):c.objects.unlink(ob)
 character.objects.link(ob)
studio();save('01_ModelMaterials')
render('01_Model_Front',(0,-3.5,1.02),(0,0,.81),65)
render('04_Face_Closeup',(.26,-.82,1.53),(0,-.015,1.43),70)
print('JOINTS',json.dumps({k:v for k,v in J.items() if k in ['head____head','joint-l-eye','joint-r-eye','root____head','upperarm01.L____head','lowerarm01.L____head','wrist.L____head','upperleg01.L____head','lowerleg01.L____head','foot.L____head']}))
print('MESH_STATS',[(o.name,sum(len(p.vertices)-2 for p in o.data.polygons)) for o in character.objects])
