import bpy,bmesh,math
from mathutils import Vector
from pathlib import Path
shirt=bpy.data.objects['Mother_BlouseMesh'];bm=bmesh.new();bm.from_mesh(shirt.data)
deform=bm.verts.layers.deform.verify();names={g.index:g.name for g in shirt.vertex_groups}
def pure(v):return sum(w for i,w in v[deform].items() if names.get(i,'').startswith(('UpperArm','LowerArm')))>.999
faces=[f for f in bm.faces if any(pure(v) for v in f.verts) and not all(pure(v) for v in f.verts)]
bmesh.ops.delete(bm,geom=faces,context='FACES_ONLY')
left={v for v in bm.verts if v.is_boundary};parts=[]
while left:
 start=left.pop();part=[start];current=start
 while True:
  choices=[e.other_vert(current) for e in current.link_edges if e.is_boundary and e.other_vert(current) in left]
  if not choices:break
  current=choices[0];left.remove(current);part.append(current)
 parts.append(part)
near=[]
for sign,side in [(-1,'R'),(1,'L')]:
 candidates=[]
 for p in parts:
  c=sum((v.co for v in p),Vector())/len(p)
  if sign*c.x>.08 and 1.18<c.z<1.33 and len(p)<50:candidates.append(p)
 a=next(p for p in candidates if len(p)!=24);original=next(p for p in candidates if len(p)==24)
 n,m=len(a),len(original);best=None
 for orientation in [original,list(reversed(original))]:
  for offset in range(m):
   b=orientation[offset:]+orientation[:offset]
   cost=sum((a[i].co-b[round(i*m/n)%m].co).length_squared for i in range(n))
   if best is None or cost<best[0]:best=(cost,b)
 b=best[1];i=j=0
 while i<n or j<m:
  pa=(i+1)/n if i<n else 2;pb=(j+1)/m if j<m else 2
  if abs(pa-pb)<1e-8:f=bm.faces.new((a[i%n],a[(i+1)%n],b[(j+1)%m],b[j%m]));i+=1;j+=1
  elif pa<pb:f=bm.faces.new((a[i%n],a[(i+1)%n],b[j%m]));i+=1
  else:f=bm.faces.new((a[i%n],b[(j+1)%m],b[j%m]));j+=1
  f.smooth=True
 def assign(v,weights):
  v[deform].clear()
  for name,w in weights.items():
   group=shirt.vertex_groups.get(name) or shirt.vertex_groups.new(name=name);v[deform][group.index]=w
 for v in b:assign(v,{'UpperArm.'+side:.35,'Shoulder.'+side:.35,'UpperChest':.30})
 for v in a:assign(v,{'UpperArm.'+side:.15,'Shoulder.'+side:.25,'UpperChest':.60})
 region=set(a+b)
 for _ in range(2):region.update(e.other_vert(v) for v in list(region) for e in v.link_edges)
 near.extend(region)
wire=[e for e in bm.edges if e.is_wire]
if wire:bmesh.ops.delete(bm,geom=wire,context='EDGES')
for _ in range(6):bmesh.ops.smooth_vert(bm,verts=list(set(near)),factor=.35,use_axis_x=True,use_axis_y=True,use_axis_z=True)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(shirt.data);bm.free()
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath='E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery/Source/Mother.blend')
exec(compile(Path('E:/BLENDER/mother_11_review_views.py').read_text(encoding='utf8'),'review','exec'))
print('Shoulder seam correspondence minimized across both loop directions; shoulder weights blended and transitions relaxed.')
