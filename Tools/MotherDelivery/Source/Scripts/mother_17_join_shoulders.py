import bpy,bmesh,math
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
shirt=bpy.data.objects['Mother_BlouseMesh'];rig=bpy.data.objects['Mother_Deform']
bm=bmesh.new();bm.from_mesh(shirt.data)
left={v for v in bm.verts if v.is_boundary};parts=[]
while left:
 start=left.pop();part=[start];current=start
 while True:
  options=[e.other_vert(current) for e in current.link_edges if e.is_boundary and e.other_vert(current) in left]
  if not options:break
  current=options[0];left.remove(current);part.append(current)
 parts.append(part)
for sign in [-1,1]:
 candidates=[]
 for p in parts:
  c=sum((v.co for v in p),Vector())/len(p)
  if sign*c.x>.08 and 1.18<c.z<1.33 and len(p)<50:candidates.append(p)
 assert len(candidates)==2,[(len(p),sum(v.co.z for v in p)/len(p)) for p in candidates]
 a=next(p for p in candidates if len(p)!=24);b=next(p for p in candidates if len(p)==24)
 offset=min(range(len(b)),key=lambda j:(b[j].co-a[0].co).length)
 b=b[offset:]+b[:offset]
 if (a[1].co-b[-1].co).length<(a[1].co-b[1].co).length:b=[b[0]]+list(reversed(b[1:]))
 n,m=len(a),len(b);i=j=0
 while i<n or j<m:
  pa=(i+1)/n if i<n else 2;pb=(j+1)/m if j<m else 2
  if abs(pa-pb)<1e-8:
   f=bm.faces.new((a[i%n],a[(i+1)%n],b[(j+1)%m],b[j%m]));i+=1;j+=1
  elif pa<pb:
   f=bm.faces.new((a[i%n],a[(i+1)%n],b[j%m]));i+=1
  else:
   f=bm.faces.new((a[i%n],b[(j+1)%m],b[j%m]));j+=1
  f.smooth=True
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(shirt.data);bm.free()

# Keep the hidden forearm volume safely inside the opaque cloth while retaining
# continuous skin through the wrist. Apply the same delta to all facial key blocks.
skin=bpy.data.objects['Mother_SkinMesh']
for v in skin.data.vertices:
 if abs(v.co.x)<.23 or not .80<v.co.z<1.20:continue
 side='L' if v.co.x>0 else 'R';bone=rig.data.bones['LowerArm.'+side]
 d=bone.tail_local-bone.head_local;t=(v.co-bone.head_local).dot(d)/d.length_squared
 if not -.12<t<.88:continue
 center=bone.head_local+d*t;factor=.67+.33*max(0,min(1,(t-.65)/.23));delta=(v.co-center)*(factor-1)
 for key in skin.data.shape_keys.key_blocks:key.data[v.index].co+=delta
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother.blend'))
exec(compile(Path('E:/BLENDER/mother_15_relaxed_actions.py').read_text(encoding='utf8'),'relaxed_actions','exec'))
print('Shoulder loops stitched into a continuous garment; hidden forearm volume adjusted; relaxed hands clear the tunic.')
