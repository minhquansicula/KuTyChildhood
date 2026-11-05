import bpy,bmesh,math
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
shirt=bpy.data.objects['Mother_BlouseMesh'];rig=bpy.data.objects['Mother_Deform']
bm=bmesh.new();bm.from_mesh(shirt.data);deform=bm.verts.layers.deform.verify()
names={g.index:g.name for g in shirt.vertex_groups}
remove=[]
for f in bm.faces:
 arm=sum(sum(w for gi,w in v[deform].items() if names.get(gi,'').startswith(('UpperArm','LowerArm','Hand'))) for v in f.verts)/len(f.verts)
 if arm>.32 or (abs(f.calc_center_median().x)>.24 and f.calc_center_median().z>.95):remove.append(f)
bmesh.ops.delete(bm,geom=remove,context='FACES')
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
uv=bm.loops.layers.uv.verify()
for side in ['L','R']:
 upper=rig.data.bones['UpperArm.'+side];lower=rig.data.bones['LowerArm.'+side]
 a=upper.head_local;b=lower.head_local;c=lower.tail_local
 l1=(b-a).length;l2=(c-b).length;total=l1+l2;split=l1/total
 rings=[]
 def vg(name):
  g=shirt.vertex_groups.get(name) or shirt.vertex_groups.new(name=name);return g.index
 for j in range(33):
  t=j/32
  if t<=split:
   u=t/split;center=a.lerp(b,u);axis=(b-a).normalized();radius=.058*(1-u)+.041*u
  else:
   u=(t-split)/(1-split);center=b.lerp(c,u);axis=(c-b).normalized();radius=.041*(1-u)+.026*u
  if j==0:center-=axis*.012
  if j==32:center+=axis*.025
  front=Vector((0,-1,0));front-=axis*front.dot(axis);front.normalize();across=axis.cross(front).normalized()
  blend=max(0,min(1,(t-split+.07)/.14));row=[]
  for k in range(24):
   angle=math.tau*k/24;p=center+front*(math.cos(angle)*radius)+across*(math.sin(angle)*radius)
   v=bm.verts.new(p)
   if blend<1:v[deform][vg('UpperArm.'+side)]=1-blend
   if blend>0:v[deform][vg('LowerArm.'+side)]=blend
   row.append(v)
  rings.append(row)
 for j in range(32):
  for k in range(24):
   f=bm.faces.new((rings[j][k],rings[j][(k+1)%24],rings[j+1][(k+1)%24],rings[j+1][k]));f.smooth=True
   for loop,co in zip(f.loops,[(k/24,j/32),((k+1)/24,j/32),((k+1)/24,(j+1)/32),(k/24,(j+1)/32)]):loop[uv].uv=co
 # A short turned-in rim has the same bone weights as the sleeve opening.
 inner=[]
 for v in rings[-1]:
  p=v.co.copy();d=p-center;p=center+d*.94-axis*.006;n=bm.verts.new(p)
  n[deform][vg('LowerArm.'+side)]=1;inner.append(n)
 for k in range(24):
  f=bm.faces.new((rings[-1][k],inner[k],inner[(k+1)%24],rings[-1][(k+1)%24]));f.smooth=True
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(shirt.data);bm.free()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother.blend'))
exec(compile(Path('E:/BLENDER/mother_15_relaxed_actions.py').read_text(encoding='utf8'),'relaxed_actions','exec'))
print('Both sleeves rebuilt around the anatomical arm chain; relaxed wrists inherit forearm rotation.')
