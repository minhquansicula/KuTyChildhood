"""Artist review revision: continuous wrists, slender proportions, quiet rural clothing, facial keys."""
import bpy, bmesh, json, math, numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
RES=OUT/'Source/Resources'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Progress/07_BeforeSlenderWarmReview.blend'),copy=True)
rig=bpy.data.objects['Mother_Deform'];skin=bpy.data.objects['Mother_SkinMesh']
D=json.loads((RES/'mother_geometry.json').read_text());CV=D['vertices'];WV={int(k):v for k,v in D['weights'].items()}

# The original skin extraction deleted nearly the whole forearm. Rebuild a continuous
# elbow-to-fingertip skin surface from the source body, not a cosmetic sleeve cover.
bm=bmesh.new();bm.from_mesh(skin.data)
remove=[v for v in bm.verts if abs(v.co.x)>.25 and .60<v.co.z<1.24]
bmesh.ops.delete(bm,geom=remove,context='VERTS');bm.to_mesh(skin.data);bm.free()
faces=[];uvs=[];tex=[];group=''
prefix=('LowerArm','Hand','Thumb','Index','Middle','Ring','Little')
for line in (RES/'base.obj').read_text().splitlines():
 t=line.split()
 if not t:continue
 if t[0]=='vt':tex.append(tuple(map(float,t[1:3])))
 elif t[0]=='g':group=t[1]
 elif t[0]=='f' and group=='body':
  ids=[int(x.split('/')[0])-1 for x in t[1:]]
  w=sum(sum(w for n,w in WV.get(i,{}).items() if n.startswith(prefix)) for i in ids)/len(ids)
  if w>.50:
   faces.append(ids);uvs.append([tex[int(x.split('/')[1])-1] for x in t[1:]])
ids=sorted({i for f in faces for i in f});index={i:j for j,i in enumerate(ids)}
me=bpy.data.meshes.new('Mother_ContinuousForearmsHands');me.from_pydata([CV[i] for i in ids],[],[[index[i] for i in f] for f in faces]);me.update()
arms=bpy.data.objects.new('ContinuousForearmsHands',me);bpy.context.scene.collection.objects.link(arms)
me.materials.append(bpy.data.materials['Mother_Skin']);uv=me.uv_layers.new(name='UVMap')
for p,coords in zip(me.polygons,uvs):
 p.use_smooth=True
 for li,co in zip(p.loop_indices,coords):uv.data[li].uv=co
for j,i in enumerate(ids):
 weights=sorted(WV[i].items(),key=lambda x:-x[1])[:4];total=sum(w for _,w in weights)
 for name,w in weights:
  vg=arms.vertex_groups.get(name) or arms.vertex_groups.new(name=name);vg.add([j],w/total,'REPLACE')
bpy.ops.object.select_all(action='DESELECT');skin.hide_set(False);skin.select_set(True);arms.select_set(True);bpy.context.view_layer.objects.active=skin;bpy.ops.object.join()
skin=bpy.context.object

def leg_center(z,side):
 names=['UpperLeg.'+side,'LowerLeg.'+side,'Foot.'+side]
 pts=[rig.data.bones[n].head_local for n in names]
 for a,b in zip(pts,pts[1:]):
  if z>=b.z:
   t=max(0,min(1,(a.z-z)/max(.001,a.z-b.z)));return a.lerp(b,t)
 return pts[-1].copy()

pants=bpy.data.objects['Mother_TrousersMesh']
for v in pants.data.vertices:
 p=v.co;side='L' if p.x>0 else 'R'
 if p.z>.79:
  p.x*=.91;p.y=-.035+(p.y+.035)*.83
 else:
  c=leg_center(p.z,side);p.x=c.x+(p.x-c.x)*.77;p.y=c.y+(p.y-c.y)*.81

# Smaller, lighter feet and fitted sandals share exactly the same proportional change.
def small_foot(p):
 sign=1 if p.x>0 else -1;c=Vector((sign*.165,-.095,.014))
 return Vector((c.x+(p.x-c.x)*.85,c.y+(p.y-c.y)*.86,c.z+(p.z-c.z)*.96))
for v in skin.data.vertices:
 if v.co.z<.105:
  blend=max(0,min(1,(.105-v.co.z)/.045));v.co=v.co.lerp(small_foot(v.co),blend)
shoe=bpy.data.objects['Mother_SandalsMesh'];inv=shoe.matrix_world.inverted()
for v in shoe.data.vertices:v.co=inv@small_foot(shoe.matrix_world@v.co)

shirt=bpy.data.objects['Mother_BlouseMesh']
for v in shirt.data.vertices:
 p=v.co
 if abs(p.x)<.245 and p.z<1.32:
  p.x*=.91
  # Reduce the exaggerated bust/hip depth; keep a loose, straighter cotton blouse.
  p.y=-.027+(p.y+.027)*.86
  if .96<p.z<1.14:p.x*=1+.045*math.sin(math.pi*(p.z-.96)/.18)
 elif abs(p.x)>.23:
  side='L' if p.x>0 else 'R';best=None
  for name in ['UpperArm.'+side,'LowerArm.'+side]:
   b=rig.data.bones[name];d=b.tail_local-b.head_local;t=max(0,min(1,(p-b.head_local).dot(d)/d.length_squared));c=b.head_local+d*t
   if best is None or (p-c).length<best[0]:best=((p-c).length,c)
  p[:]=best[1]+(p-best[1])*.90

# Move the existing small cloth buttons onto the actual front surface.
bm=bmesh.new();bm.from_mesh(shirt.data);remaining=set(bm.verts);buttons=[]
while remaining:
 stack=[remaining.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if n in remaining:remaining.remove(n);stack.append(n)
 if len(part)<150 and all(f.material_index==1 for v in part for f in v.link_faces):buttons.append(part)
bvh=BVHTree.FromPolygons([v.co for v in shirt.data.vertices],[list(p.vertices) for p in shirt.data.polygons if p.material_index==0])
for part in buttons:
 c=sum((v.co for v in part),Vector())/len(part);hit=bvh.ray_cast(Vector((0,-.6,c.z)),Vector((0,1,0)))[0]
 if hit:
  delta=Vector((-c.x,hit.y-.002-c.y,0))
  for v in part:v.co+=delta
bm.to_mesh(shirt.data);bm.free()

# Add an understated front placket, weighted continuously down the torso.
bm=bmesh.new();bm.from_mesh(shirt.data);deform=bm.verts.layers.deform.verify();rows=[]
uvlayer=bm.loops.layers.uv.active
for j in range(41):
 z=.85+(1.316-.85)*j/40;hit=bvh.ray_cast(Vector((0,-.6,z)),Vector((0,1,0)))[0]
 y=hit.y-.001 if hit else -.14;row=[]
 for x in [-.005,.005]:
  v=bm.verts.new((x,y,z));bn='Hips' if z<.94 else 'Spine' if z<1.08 else 'Chest' if z<1.24 else 'UpperChest'
  g=shirt.vertex_groups.get(bn) or shirt.vertex_groups.new(name=bn);v[deform][g.index]=1;row.append(v)
 rows.append(row)
for j in range(40):
 f=bm.faces.new((rows[j][0],rows[j][1],rows[j+1][1],rows[j+1][0]));f.smooth=True
 if uvlayer:
  for loop,uvco in zip(f.loops,[(.49,j/40),(.51,j/40),(.51,(j+1)/40),(.49,(j+1)/40)]):loop[uvlayer].uv=uvco
bm.to_mesh(shirt.data);bm.free()

# Facial expression controls. Default is a restrained, warm smile, not a fixed blank stare.
skin.shape_key_add(name='Basis')
warm=skin.shape_key_add(name='WarmSmile');concern=skin.shape_key_add(name='Concern')
for v in skin.data.vertices:
 x,y,z=v.co;ax=abs(x)
 if y>-.075 or not 1.37<z<1.53:continue
 front=max(0,min(1,(-y-.075)/.035))
 corner=math.exp(-((ax-.023)/.011)**2-((z-1.411)/.011)**2)*front
 cheek=math.exp(-((ax-.039)/.022)**2-((z-1.448)/.018)**2)*front
 warm.data[v.index].co.z+=.0040*corner+.0014*cheek
 warm.data[v.index].co.y-=.0010*corner
 lowerlip=math.exp(-(x/.023)**4-((z-1.402)/.006)**2)*front
 warm.data[v.index].co.z+=.0011*lowerlip
 # Ease the upper eyelids slightly downward to remove the startled gaze.
 lid=math.exp(-((ax-.0295)/.012)**4-((z-1.482)/.0045)**2)*front
 warm.data[v.index].co.z-=.0012*lid
 brow=math.exp(-((ax-.019)/.015)**2-((z-1.499)/.009)**2)*front
 concern.data[v.index].co.z+=.0028*brow-.0018*corner
warm.value=.8;concern.value=0
skin['facial_controls']='Shape Keys: WarmSmile (default 0.8), Concern. Artist-review controls; not yet facial-animation validated.'

# Softer curved eyebrow strips accompany the warm expression.
hair=bpy.data.objects['Mother_HairMesh'];bm=bmesh.new();bm.from_mesh(hair.data);left=set(bm.verts)
while left:
 stack=[left.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if n in left:left.remove(n);stack.append(n)
 if len(part)==34 and 1.49<sum(v.co.z for v in part)/len(part)<1.51:
  for v in part:
   t=max(0,min(1,(abs(v.co.x)-.014)/.037));v.co.z+=.0015*math.sin(math.pi*t)+.0006*(1-t)
bm.to_mesh(hair.data);bm.free()

# Plain, subtly worn cotton rather than the strong procedural grid.
im=bpy.data.images['Mother_Blouse_BaseColor'];w,h=im.size;rng=np.random.default_rng(213)
yy,xx=np.mgrid[0:h,0:w].astype(np.float32)
noise=rng.random((h,w),dtype=np.float32)-.5
variation=.014*noise+.012*np.sin(xx/w*math.tau*3)*np.sin(yy/h*math.tau*2)
rgba=np.ones((h,w,4),dtype=np.float32);rgba[:,:,:3]=np.array([.135,.067,.048])[None,None,:]*(1+variation[:,:,None])
im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/'Textures/Mother_Blouse_BaseColor.png');im.save()
for name in ['Mother_Blouse','Mother_Trousers']:
 for n in bpy.data.materials[name].node_tree.nodes:
  if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.10

bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother.blend'))
print('SLENDER_WARM_REVIEW: continuous forearm skin rebuilt; legs/feet reduced; rural blouse/placket/buttons; WarmSmile and Concern shape keys.')
