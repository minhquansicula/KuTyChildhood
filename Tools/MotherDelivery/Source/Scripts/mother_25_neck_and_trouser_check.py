import bpy,json,math,numpy as np
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery');RES=OUT/'Source/Resources'
if bpy.data.objects.get('Mother_NeckContinuity'):raise RuntimeError('Neck continuity patch already exists.')
body=bpy.data.objects['Mother_Body'];D=json.loads((RES/'mother_geometry.json').read_text());CV=D['vertices'];WV={int(k):v for k,v in D['weights'].items()}
def signature(points):
 n=len(points);c=[sum(p[i] for p in points)/n for i in range(3)]
 return (n,*(round(x,5) for x in c))
existing={signature([body.data.vertices[i].co for i in p.vertices]) for p in body.data.polygons}
faces=[];uvs=[];tex=[];group=''
for line in (RES/'base.obj').read_text().splitlines():
 t=line.split()
 if not t:continue
 if t[0]=='vt':tex.append(tuple(map(float,t[1:3])))
 elif t[0]=='g':group=t[1]
 elif t[0]=='f' and group=='body':
  ids=[int(x.split('/')[0])-1 for x in t[1:]];points=[CV[i] for i in ids];center=sum((Vector(p) for p in points),Vector())/len(ids)
  if not 1.265<center.z<1.375 or abs(center.x)>.078:continue
  weight=sum(sum(w for n,w in WV.get(i,{}).items() if n in ['Head','Neck']) for i in ids)/len(ids)
  if weight<.08 or signature(points) in existing:continue
  faces.append(ids);uvs.append([tex[int(x.split('/')[1])-1] for x in t[1:]])
if not faces:raise RuntimeError('No missing source neck faces identified; patch not created.')
ids=sorted({i for face in faces for i in face});index={i:j for j,i in enumerate(ids)}
mesh=bpy.data.meshes.new('Mother_NeckContinuity_Surface');mesh.from_pydata([CV[i] for i in ids],[],[[index[i] for i in f] for f in faces]);mesh.update()
obj=bpy.data.objects.new('Mother_NeckContinuity',mesh);bpy.data.collections['CHARACTER_Mother'].objects.link(obj);obj.parent=bpy.data.objects['SK_Mother']
mesh.materials.append(bpy.data.materials['Mother_Skin']);layer=mesh.uv_layers.new(name='UVMap')
for p,coords in zip(mesh.polygons,uvs):
 p.use_smooth=True
 for li,uv in zip(p.loop_indices,coords):layer.data[li].uv=uv
for j,i in enumerate(ids):
 weights=sorted(WV[i].items(),key=lambda pair:-pair[1])[:4];total=sum(w for _,w in weights)
 for name,w in weights:
  group=obj.vertex_groups.get(name) or obj.vertex_groups.new(name=name);group.add([j],w/total,'REPLACE')
modifier=obj.modifiers.new('Mother skinning','ARMATURE');modifier.object=bpy.data.objects['Mother_Deform'];modifier.use_deform_preserve_volume=False
obj['purpose']='Additive missing neck skin restored from original source topology/UV/weights. Existing Body and facial keys remain untouched.'

pants=bpy.data.objects['Mother_Pants'];key=pants.data.shape_keys.key_blocks['LooseStraight_FabricVolume'];rig=bpy.data.objects['Mother_Deform']
def center_at(z,side):
 points=[rig.data.bones[n+'.'+side].head_local for n in ['UpperLeg','LowerLeg','Foot']]
 for a,b in zip(points,points[1:]):
  if z>=b.z:return a.lerp(b,max(0,min(1,(a.z-z)/(a.z-b.z))))
 return points[-1].copy()
for v in pants.data.vertices:
 z=v.co.z
 if z>=.84:continue
 p=key.data[v.index].co;center=center_at(z,'L' if v.co.x>0 else 'R')
 oldx=float(np.interp(z,[.083,.28,.48,.66,.80],[.040,.043,.046,.057,.066]));newx=float(np.interp(z,[.083,.28,.48,.66,.80],[.048,.049,.052,.061,.068]))
 oldy=float(np.interp(z,[.083,.28,.48,.66,.80],[.052,.056,.059,.074,.080]));newy=float(np.interp(z,[.083,.28,.48,.66,.80],[.061,.064,.067,.078,.082]))
 t=max(0,min(1,(.84-z)/.12));t=t*t*(3-2*t)
 p.x=center.x+(p.x-center.x)*(1+(newx/oldx-1)*t)
 p.y=center.y-.005+(p.y-center.y+.005)*(1+(newy/oldy-1)*t)
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother_AoBaBa_Review.blend'))
print(json.dumps({'neck_patch_vertices':len(ids),'neck_patch_faces':len(faces),'reason':'Lowering the old raised collar exposed an already missing lower-neck surface; repaired additively, not by replacing the body.','trousers':'Additional straight-cut cloth ease at knees and ankle openings.'}))
