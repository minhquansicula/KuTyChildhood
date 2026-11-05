exec(compile(open('E:/BLENDER/mother_common.py',encoding='utf8').read(),'mother_common.py','exec'))
import bmesh,collections
data=json.loads((RES/'mother_geometry.json').read_text());CV=np.array(data['vertices']);J=data['joints'];WV={int(k):v for k,v in data['weights'].items()}
for im in bpy.data.images:
 if im.name.startswith('Mother_') and (OUT/'Textures'/(im.name+'.png')).is_file():im.filepath=str(OUT/'Textures'/(im.name+'.png'));im.reload()
# Replace long-hair fitting helper with a close-fitted scalp cap and a low, practical bun.
old=bpy.data.objects['Mother_HairMesh'];bpy.data.objects.remove(old,do_unlink=True)
body=bpy.data.objects['Mother_SkinMesh'];ids=list(body['source_indices']);capfaces=[];capuv=[];capids=set()
for p in body.data.polygons:
 c=p.center;threshold=1.505 if c.y<-.06 else (1.465 if abs(c.x)>.055 else 1.425)
 if c.z>threshold:
  capfaces.append([ids[i] for i in p.vertices]);capuv.append([list(body.data.uv_layers.active.data[li].uv) for li in p.loop_indices]);capids.update(ids[i] for i in p.vertices)
capids=sorted(capids);remap={i:j for j,i in enumerate(capids)};vs=[]
for i in capids:
 p=CV[i].copy();direction=p-np.array([0,-.023,1.472]);direction/=max(np.linalg.norm(direction),.001);vs.append(list(p+direction*.003))
hair=mesh('Mother_Scalp',vs,[[remap[i] for i in f] for f in capfaces],bpy.data.materials['Mother_Hair'],capuv)
active(hair);mod=hair.modifiers.new('Smooth hair silhouette','SUBSURF');mod.levels=1;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=hair.modifiers.new('Scalp LOD0','DECIMATE');mod.ratio=.58;bpy.ops.object.modifier_apply(modifier=mod.name)
bun=ellipsoid('LowBun',(0,.086,1.411),(.053,.041,.043),bpy.data.materials['Mother_Hair'],24,12)
hair=join([hair,bun],'Mother_HairMesh');vg=hair.vertex_groups.new(name='Head');vg.add(list(range(len(hair.data.vertices))),1,'REPLACE')
# Remove all clothing finger/tights remnants using hand/leg weights, then relax and smooth.
blouse=bpy.data.objects['Mother_BlouseMesh'];bm=bmesh.new();bm.from_mesh(blouse.data)
deform=bm.verts.layers.deform.active;gnames={g.index:g.name for g in blouse.vertex_groups}
remove=[]
for f in bm.faces:
 hand=sum(sum(w for gi,w in v[deform].items() if any(gnames.get(gi,'').startswith(n) for n in ['Hand','Thumb','Index','Middle','Ring','Little'])) for v in f.verts)/len(f.verts)
 leg=sum(sum(w for gi,w in v[deform].items() if gnames.get(gi,'').startswith(('UpperLeg','LowerLeg'))) for v in f.verts)/len(f.verts)
 if hand>.25 or (leg>.65 and f.calc_center_median().z<.88):remove.append(f)
bmesh.ops.delete(bm,geom=remove,context='FACES');bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
# Flatten hem boundary on the torso and remove jagged cage cutouts.
for v in bm.verts:
 if v.is_boundary and abs(v.co.x)<.20 and v.co.z<.96:v.co.z=.795
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(blouse.data);bm.free()
active(blouse);mod=blouse.modifiers.new('Soft cotton silhouette','SUBSURF');mod.levels=1;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=blouse.modifiers.new('Cloth LOD0','DECIMATE');mod.ratio=.38;bpy.ops.object.modifier_apply(modifier=mod.name)
# Normal smooth the fitting-cage trousers, broadening hems for straight-cut cloth.
tr=bpy.data.objects['Mother_TrousersMesh']
for v in tr.data.vertices:
 if v.co.z<.30:
  cx=math.copysign(.16,v.co.x);v.co.x=cx+(v.co.x-cx)*1.12
active(tr);mod=tr.modifiers.new('Relax trouser cage','SMOOTH');mod.factor=.45;mod.iterations=2;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=tr.modifiers.new('Soft trouser silhouette','SUBSURF');mod.levels=1;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=tr.modifiers.new('Trouser LOD0','DECIMATE');mod.ratio=.36;bpy.ops.object.modifier_apply(modifier=mod.name)
# Remove scalp skin hidden under the hair, then reduce hidden/secondary skin topology.
bm=bmesh.new();bm.from_mesh(body.data);remove=[]
for f in bm.faces:
 c=f.calc_center_median();threshold=1.510 if c.y<-.06 else (1.473 if abs(c.x)>.055 else 1.436)
 if c.z>threshold:remove.append(f)
bmesh.ops.delete(bm,geom=remove,context='FACES');bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(body.data);bm.free()
active(body);mod=body.modifiers.new('Skin LOD0','DECIMATE');mod.ratio=.70;bpy.ops.object.modifier_apply(modifier=mod.name)
for name,ratio in [('Mother_EyesMesh',.48),('Mother_SandalsMesh',.46)]:
 o=bpy.data.objects[name];active(o);mod=o.modifiers.new('Secondary detail LOD0','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
# Eyebrow strips, each subtly different, with their own silhouette but shared hair material.
brows=[]
for side in ['l','r']:
 eye=np.array(J['joint-'+side+'-eye']);sign=1 if side=='l' else -1;vs=[];fs=[]
 for k in range(13):
  t=k/12;x=eye[0]+sign*(t-.5)*.036;z=eye[2]+.024+.004*math.sin(t*math.pi)+(0.0007 if side=='l' else 0)
  for dz in [-.0015,.0015]:vs.append((x,eye[1]-.003+.006*abs(t-.5),z+dz))
 for k in range(12):fs.append((k*2,k*2+1,k*2+3,k*2+2))
 b=mesh('Brow_'+side,vs,fs,bpy.data.materials['Mother_Hair']);vg=b.vertex_groups.new(name='Head');vg.add(list(range(len(vs))),1,'REPLACE');brows.append(b)
hair=join([hair]+brows,'Mother_HairMesh')
# A plain button placket, small matte buttons and a short collar sell the everyday blouse.
pieces=[]
pl=cube('Blouse_Placket',(0,-.116,1.105),(.014,.004,.365),bpy.data.materials['Mother_Blouse'],.002);pieces.append(pl)
for z in [1.27,1.20,1.13,1.06,.99]:
 b=ellipsoid('Cloth_Button',(0,-.120,z),(.0036,.0018,.0036),bpy.data.materials['Mother_Hair'],12,6);pieces.append(b)
for o in pieces:
 vg=o.vertex_groups.new(name='Chest' if o.location.z>1.12 else 'Spine');vg.add(list(range(len(o.data.vertices))),1,'REPLACE')
blouse=join([blouse]+pieces,'Mother_BlouseMesh')
char=bpy.data.collections['CHARACTER_Mother']
for o in [hair,blouse]:
 for c in list(o.users_collection):c.objects.unlink(o)
 char.objects.link(o)
save('02_ProportionRefined')
render('01_Model_Front',(0,-3.5,1.02),(0,0,.81),65)
render('04_Face_Closeup',(.23,-.78,1.52),(0,-.04,1.455),70)
print('STATS',[(o.name,sum(len(p.vertices)-2 for p in o.data.polygons)) for o in char.objects]);print('FACE_JOINTS',json.dumps({k:v for k,v in J.items() if k in ['joint-mouth','joint-jaw','head____tail']}))
