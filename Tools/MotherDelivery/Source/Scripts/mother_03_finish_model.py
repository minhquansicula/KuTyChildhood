exec(compile(open('E:/BLENDER/mother_common.py',encoding='utf8').read(),'mother_common.py','exec'))
import bmesh,collections
from mathutils.bvhtree import BVHTree
D=json.loads((RES/'mother_geometry.json').read_text());CV=np.array(D['vertices']);WV={int(k):v for k,v in D['weights'].items()};J=D['joints']
body=bpy.data.objects['Mother_SkinMesh'];old=bpy.data.objects['Mother_BlouseMesh'];bpy.data.objects.remove(old,do_unlink=True)
# Rebuild a complete loose blouse from the fitting cage, keeping a continuous hem.
fs=[];fu=[];g='';uv=[]
for l in (RES/'base.obj').read_text().splitlines():
 t=l.split()
 if not t:continue
 if t[0]=='vt':uv.append(list(map(float,t[1:3])))
 elif t[0]=='g':g=t[1]
 elif t[0]=='f' and g=='helper-tights':fs.append([int(x.split('/')[0])-1 for x in t[1:]]);fu.append([uv[int(x.split('/')[1])-1] for x in t[1:]])
kd=KDTree(len(WV))
for i in WV:kd.insert(Vector(CV[i]),i)
kd.balance();sources={};selected=[]
for k,f in enumerate(fs):
 c=np.mean(CV[f],axis=0);src=kd.find(Vector(c))[1];w=WV[src];hand=sum(a for bn,a in w.items() if bn.startswith(('Hand','Thumb','Index','Middle','Ring','Little')))
 if .815<c[2]<1.345 and hand<.20:selected.append(k)
ids=sorted({i for k in selected for i in fs[k]});index={i:j for j,i in enumerate(ids)};vs=[]
for i in ids:
 p=CV[i].copy();src=kd.find(Vector(p))[1];sources[i]=src;w=WV[src];arm=sum(a for bn,a in w.items() if bn.startswith(('UpperArm','LowerArm')))
 if arm>.35:
  p[0]+=math.copysign(.006,p[0]);p[1]*=1.16;p[2]+=.006
 else:
  p[0]*=1.07;p[1]*=1.05
  if p[1]<-.035:p[1]=min(p[1]-.01,-.122+.013*(p[0]/.18)**2)
  else:p[1]+=.009
  p[1]+=.0018*math.sin(p[2]*80+p[0]*15)
 vs.append(list(p))
bl=mesh('Mother_BlouseMesh',vs,[[index[i] for i in fs[k]] for k in selected],bpy.data.materials['Mother_Blouse'],[fu[k] for k in selected])
for j,i in enumerate(ids):
 for bn,w in WV[sources[i]].items():
  vg=bl.vertex_groups.get(bn) or bl.vertex_groups.new(name=bn);vg.add([j],w,'REPLACE')
bm=bmesh.new();bm.from_mesh(bl.data)
for v in bm.verts:
 if v.is_boundary and abs(v.co.x)<.22 and v.co.z<.98:v.co.z=.81
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(bl.data);bm.free()
active(bl);mod=bl.modifiers.new('Soft cotton','SUBSURF');mod.levels=1;bpy.ops.object.modifier_apply(modifier=mod.name)
mod=bl.modifiers.new('Blouse LOD0','DECIMATE');mod.ratio=.43;bpy.ops.object.modifier_apply(modifier=mod.name)
pieces=[]
# Placket tracks the torso curvature; individual buttons remain tiny and matte.
for j,z in enumerate([1.285,1.22,1.15,1.08,1.01,.94,.87]):
 p=ellipsoid('Small cloth button',(0,-.134,z),(.0031,.0016,.0031),bpy.data.materials['Mother_Hair'],10,6)
 vg=p.vertex_groups.new(name='UpperChest' if z>1.24 else ('Chest' if z>1.08 else 'Spine'));vg.add(list(range(len(p.data.vertices))),1,'REPLACE');pieces.append(p)
bl=join([bl]+pieces,'Mother_BlouseMesh')
col=bpy.data.collections['CHARACTER_Mother']
for c in list(bl.users_collection):c.objects.unlink(bl)
col.objects.link(bl)
# Shallow, dark combed hair; reduce the oversized procedural grooves.
hm=bpy.data.materials['Mother_Hair'];p=hm.node_tree.nodes.get('Principled BSDF');p.inputs['Specular IOR Level'].default_value=.18
for n in hm.node_tree.nodes:
 if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.18
im=bpy.data.images['Mother_Hair_BaseColor'];sz=im.size[0];rgba=np.ones((sz,sz,4),dtype=np.float32);rgba[:,:,:3]=(.0035,.0025,.0020);im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/'Textures'/f'{im.name}.png');im.save()
im=bpy.data.images['Mother_Hair_Roughness'];rgba[:,:,:3]=.78;im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/'Textures'/f'{im.name}.png');im.save()
im=bpy.data.images['Mother_Hair_MetallicSmoothness'];rgba[:,:,:3]=0;rgba[:,:,3]=.22;im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/'Textures'/f'{im.name}.png');im.save()
hair=bpy.data.objects['Mother_HairMesh'];bm=bmesh.new();bm.from_mesh(hair.data)
# Remove old buried eyebrows and small fitting-cage lobes around the ears.
remove=[f for f in bm.faces if (abs(f.calc_center_median().x)<.058 and f.calc_center_median().y<-.11 and f.calc_center_median().z<1.51) or (abs(f.calc_center_median().x)>.063 and f.calc_center_median().z<1.483 and f.calc_center_median().y<.035)]
bmesh.ops.delete(bm,geom=remove,context='FACES');bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
edgeverts=[v for v in bm.verts if v.is_boundary and v.co.z>1.50 and v.co.y<-.04]
for _ in range(4):bmesh.ops.smooth_vert(bm,verts=edgeverts,factor=.5,use_axis_x=True,use_axis_y=True,use_axis_z=True)
bm.to_mesh(hair.data);bm.free()
bvh=BVHTree.FromPolygons([v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons])
brows=[]
for sign in [-1,1]:
 vs=[];faces=[]
 for k in range(17):
  t=k/16;x=sign*(.014+t*.037);z=1.498+.0032*math.sin(t*math.pi)-.0015*t+(.0007 if sign<0 else 0)
  for dz in [-.0012,.0012]:
   hit=bvh.ray_cast(Vector((x,-.5,z+dz)),Vector((0,1,0)))[0];vs.append((x,hit.y-.001 if hit else -.143,z+dz))
 for k in range(16):faces.append((k*2,k*2+2,k*2+3,k*2+1))
 o=mesh('Soft eyebrow',vs,faces,hm);vg=o.vertex_groups.new(name='Head');vg.add(list(range(len(vs))),1,'REPLACE');brows.append(o)
hair=join([hair]+brows,'Mother_HairMesh')
# Bake subtle lip, cheek and age coloration in the actual skin UVs (no lighting baked).
N=2048;base=np.zeros((N,N,3),dtype=np.float32)+np.array([.47,.286,.176]);rough=np.full((N,N),.59,dtype=np.float32)
body.data.calc_loop_triangles();uv=body.data.uv_layers.active
for tri in body.data.loop_triangles:
 pos=np.array([body.data.vertices[i].co[:] for i in tri.vertices]);tc=np.array([uv.data[i].uv[:] for i in tri.loops])*(N-1)
 xmin=max(0,int(np.floor(tc[:,0].min())));xmax=min(N-1,int(np.ceil(tc[:,0].max())));ymin=max(0,int(np.floor(tc[:,1].min())));ymax=min(N-1,int(np.ceil(tc[:,1].max())))
 if xmax<xmin or ymax<ymin:continue
 den=(tc[1,1]-tc[2,1])*(tc[0,0]-tc[2,0])+(tc[2,0]-tc[1,0])*(tc[0,1]-tc[2,1])
 if abs(den)<1e-8:continue
 yy,xx=np.mgrid[ymin:ymax+1,xmin:xmax+1]
 a=((tc[1,1]-tc[2,1])*(xx-tc[2,0])+(tc[2,0]-tc[1,0])*(yy-tc[2,1]))/den
 b=((tc[2,1]-tc[0,1])*(xx-tc[2,0])+(tc[0,0]-tc[2,0])*(yy-tc[2,1]))/den;c=1-a-b;inside=(a>=-.01)&(b>=-.01)&(c>=-.01)
 xyz=a[:,:,None]*pos[0]+b[:,:,None]*pos[1]+c[:,:,None]*pos[2];x=xyz[:,:,0];y=xyz[:,:,1];z=xyz[:,:,2]
 lip=np.exp(-((x/.024)**4+((z-1.411)/.0065)**4))*(y<-.129)
 cheeks=np.exp(-((np.abs(x)-.045)/.024)**2-((z-1.448)/.025)**2)*(y<-.08)
 ears=np.exp(-((np.abs(x)-.071)/.012)**2-((z-1.454)/.038)**2)
 clr=np.zeros((*a.shape,3))+np.array([.47,.286,.176]);clr+=cheeks[:,:,None]*np.array([.017,-.008,-.005])+ears[:,:,None]*np.array([.012,-.009,-.005]);clr=clr*(1-lip[:,:,None]*.62)+np.array([.36,.135,.10])*lip[:,:,None]*.62
 patch=base[ymin:ymax+1,xmin:xmax+1];patch[inside]=clr[inside]
 rp=rough[ymin:ymax+1,xmin:xmax+1];r=.59-.08*np.exp(-(x/.024)**2-((z-1.46)/.055)**2)*(y<-.12);rp[inside]=r[inside]
for suffix,arr in [('BaseColor',base),('Roughness',np.repeat(rough[:,:,None],3,axis=2)),('MetallicSmoothness',np.concatenate((np.zeros((N,N,3)),(1-rough)[:,:,None]),axis=2))]:
 im=bpy.data.images['Mother_Skin_'+suffix];rgba=np.ones((N,N,4),dtype=np.float32);rgba[:,:,:arr.shape[2]]=arr;im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/'Textures'/(im.name+'.png'));im.save()
bpy.data.materials['Mother_Skin'].node_tree.nodes.get('Principled BSDF').inputs['Subsurface Weight'].default_value=.025
# Keep total triangle count inside the requested LOD0 budget.
for name,ratio in [('Mother_TrousersMesh',.88),('Mother_BlouseMesh',.80)]:
 o=bpy.data.objects[name];active(o);m=o.modifiers.new('Final LOD0','DECIMATE');m.ratio=ratio;bpy.ops.object.modifier_apply(modifier=m.name)
for o in col.objects:
 for p in o.data.polygons:p.use_smooth=True
save('03_ModelFinal')
render('01_Model_Front',(0,-3.5,1.02),(0,0,.81),65)
render('02_Model_Side',(3.4,0,1.02),(0,0,.81),65)
render('03_Model_Back',(0,3.5,1.02),(0,0,.81),65)
render('04_Face_Closeup',(.23,-.78,1.52),(0,-.04,1.455),70)
print('TRIANGLES',sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in col.objects))
