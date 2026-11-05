import bpy,bmesh,json,math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery');RES=OUT/'Source/Resources'
D=json.loads((RES/'mother_geometry.json').read_text());CV=D['vertices'];WV={int(k):v for k,v in D['weights'].items()}
skin=bpy.data.objects['Mother_SkinMesh']
skin.shape_key_clear()
bm=bmesh.new();bm.from_mesh(skin.data)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z>1.28],context='VERTS');bm.to_mesh(skin.data);bm.free()
faces=[];fuv=[];tex=[];group=''
for line in (RES/'base.obj').read_text().splitlines():
 t=line.split()
 if not t:continue
 if t[0]=='vt':tex.append(tuple(map(float,t[1:3])))
 elif t[0]=='g':group=t[1]
 elif t[0]=='f' and group=='body':
  ids=[int(x.split('/')[0])-1 for x in t[1:]]
  if min(CV[i][2] for i in ids)<1.25:continue
  if sum(sum(w for n,w in WV.get(i,{}).items() if n in ['Head','Neck']) for i in ids)/len(ids)>.55:
   faces.append(ids);fuv.append([tex[int(x.split('/')[1])-1] for x in t[1:]])
ids=sorted({i for f in faces for i in f});index={i:j for j,i in enumerate(ids)}
me=bpy.data.meshes.new('Mother_CompleteHeadSkin');me.from_pydata([CV[i] for i in ids],[],[[index[i] for i in f] for f in faces]);me.update()
head=bpy.data.objects.new('CompleteHeadSkin',me);bpy.context.scene.collection.objects.link(head);me.materials.append(bpy.data.materials['Mother_Skin']);uv=me.uv_layers.new(name='UVMap')
for p,coords in zip(me.polygons,fuv):
 p.use_smooth=True
 for li,co in zip(p.loop_indices,coords):uv.data[li].uv=co
for j,i in enumerate(ids):
 weights=sorted(WV[i].items(),key=lambda x:-x[1])[:4];total=sum(w for _,w in weights)
 for name,w in weights:
  g=head.vertex_groups.get(name) or head.vertex_groups.new(name=name);g.add([j],w/total,'REPLACE')
bvh=BVHTree.FromPolygons([Vector(CV[i]) for i in ids],[[index[i] for i in f] for f in faces])
bpy.ops.object.select_all(action='DESELECT');skin.hide_set(False);skin.select_set(True);head.select_set(True);bpy.context.view_layer.objects.active=skin;bpy.ops.object.join();skin=bpy.context.object
expression=Path('E:/BLENDER/mother_14_slender_warm.py').read_text(encoding='utf8').split('# Facial expression controls.')[1].split('# Softer curved eyebrow strips')[0]
exec(compile('# Facial expression controls.'+expression,'facial_controls','exec'))

hair=bpy.data.objects['Mother_HairMesh'];bm=bmesh.new();bm.from_mesh(hair.data);left=set(bm.verts);remove=[]
while left:
 stack=[left.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if n in left:left.remove(n);stack.append(n)
 if len(part)>500 or len(part)==75:remove.extend(part)
bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(hair.data);bm.free()

center=Vector((0,-.045,1.49));N=64;R=20
def surface(phi,theta):
 direction=Vector((math.sin(theta)*math.sin(phi),-math.sin(theta)*math.cos(phi),math.cos(theta)))
 hit,normal,_,_=bvh.ray_cast(center,direction)
 if hit is None:raise RuntimeError('Scalp ray missed head')
 return hit+normal*.0025
limits=[]
for k in range(N):
 phi=math.tau*k/N;c=math.cos(phi)
 target=1.482+.038*c if c>=0 else 1.482+.044*c
 lo=.5;hi=2.7
 for _ in range(22):
  mid=(lo+hi)/2;p=surface(phi,mid)
  if p.z>target:lo=mid
  else:hi=mid
 limits.append((lo+hi)/2)
verts=[surface(0,0)];fs=[]
for j in range(1,R+1):
 for k in range(N):verts.append(surface(math.tau*k/N,limits[k]*j/R))
for k in range(N):fs.append((0,1+k,1+(k+1)%N))
for j in range(R-1):
 for k in range(N):
  a=1+j*N+k;b=1+j*N+(k+1)%N;c=1+(j+1)*N+(k+1)%N;d=1+(j+1)*N+k;fs.append((a,b,c,d))
me=bpy.data.meshes.new('Mother_ContinuousFittedScalp');me.from_pydata(verts,[],fs);me.update()
cap=bpy.data.objects.new('FittedScalp',me);bpy.context.scene.collection.objects.link(cap);me.materials.append(bpy.data.materials['Mother_Hair']);uv=me.uv_layers.new(name='UVMap')
for p in me.polygons:
 p.use_smooth=True
 for li in p.loop_indices:
  vi=me.loops[li].vertex_index;uv.data[li].uv=((vi-1)%N/N,max(0,(vi-1)//N)/R)
g=cap.vertex_groups.new(name='Head');g.add(list(range(len(verts))),1,'REPLACE')
bpy.ops.object.select_all(action='DESELECT');hair.hide_set(False);hair.select_set(True);cap.select_set(True);bpy.context.view_layer.objects.active=hair;bpy.ops.object.join()
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother.blend'))
exec(compile(Path('E:/BLENDER/mother_11_review_views.py').read_text(encoding='utf8'),'review','exec'))
exec(compile(Path('E:/BLENDER/mother_19_detail_review.py').read_text(encoding='utf8'),'details','exec'))
print('Complete forehead skin restored; fitted scalp rebuilt with a smooth hairline above the brows; warm facial controls retained.')
