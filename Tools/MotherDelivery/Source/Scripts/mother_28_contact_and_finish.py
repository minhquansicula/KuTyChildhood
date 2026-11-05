import bpy,bmesh,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
body=bpy.data.objects['Mother_Body'];shoe=bpy.data.objects['Mother_Footwear'];patch=bpy.data.objects['Mother_NeckContinuity']

# An additive normal-transfer modifier softens the seam; the original body mesh,
# existing facial keys and normals data are not overwritten.
kd=KDTree(len(body.data.vertices))
for v in body.data.vertices:kd.insert(v.co,v.index)
kd.balance();group=patch.vertex_groups.new(name='NeckBoundary_NormalBlend')
for v in patch.data.vertices:
 distance=kd.find(v.co)[2];weight=max(0,min(1,1-distance/.009))
 if weight:group.add([v.index],weight,'REPLACE')
modifier=patch.modifiers.new('Soft shared-neck normals','DATA_TRANSFER');modifier.object=body;modifier.use_loop_data=True;modifier.data_types_loops={'CUSTOM_NORMAL'};modifier.loop_mapping='POLYINTERP_NEAREST';modifier.mix_mode='REPLACE';modifier.mix_factor=1;modifier.vertex_group=group.name

# Relax the pre-existing cage's tight crotch folds within the reversible clothing
# key only. Do not modify topology, UVs, vertex groups or original Basis data.
pants=bpy.data.objects['Mother_Pants'];key=pants.data.shape_keys.key_blocks['LooseStraight_FabricVolume']
neighbors=[set() for _ in pants.data.vertices]
for edge in pants.data.edges:
 a,b=edge.vertices;neighbors[a].add(b);neighbors[b].add(a)
for _ in range(9):
 old=[p.co.copy() for p in key.data]
 for v in pants.data.vertices:
  x,y,z=v.co
  if .60<z<.88 and abs(x)<.125 and neighbors[v.index]:
   influence=math.exp(-(x/.095)**4)*math.exp(-((z-.75)/.105)**4)*.34
   average=sum((old[i] for i in neighbors[v.index]),Vector())/len(neighbors[v.index])
   key.data[v.index].co=old[v.index].lerp(average,influence)

# Fit existing sole and strap vertices to the existing foot, retaining the mesh,
# UVs, materials, four-weight skinning and bone assignment. No remesh/subdivision.
shoe.shape_key_add(name='Basis')
fit=shoe.shape_key_add(name='PeriodSandal_ContactFit');fit.value=1
bvh=BVHTree.FromPolygons([body.matrix_world@v.co for v in body.data.vertices],[tuple(p.vertices) for p in body.data.polygons])
bm=bmesh.new();bm.from_mesh(shoe.data);left=set(bm.verts);components=[]
while left:
 stack=[left.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if n in left:left.remove(n);stack.append(n)
 components.append(part)
inverse=shoe.matrix_world.inverted();counts={'sole':0,'strap':0}
for part in components:
 world={v.index:shoe.matrix_world@v.co for v in part};zs=[p.z for p in world.values()]
 if max(zs)<.03:
  middle=(min(zs)+max(zs))/2
  for v in part:
   p=world[v.index].copy()
   if p.z>=middle-.0001:
    hit=bvh.ray_cast(Vector((p.x,p.y,-.05)),Vector((0,0,1)))[0]
    if hit and hit.z<.040:p.z=hit.z-.0016
    else:p.z=min(p.z,.0115)
    fit.data[v.index].co=inverse@p;counts['sole']+=1
 else:
  rings={}
  for v in part:rings.setdefault(round(world[v.index].y,6),[]).append(v)
  for ring in rings.values():
   center_z=sum(world[v.index].z for v in ring)/len(ring)
   for v in ring:
    p=world[v.index].copy();hit=bvh.ray_cast(Vector((p.x,p.y,.18)),Vector((0,0,-1)))[0]
    if hit:
     p.z=hit.z+.0038+(p.z-center_z);fit.data[v.index].co=inverse@p;counts['strap']+=1
bm.free()
shoe['review_contact']='Reversible sole/strap fit against the current foot surface; no decorative additions. Exact contact checked separately.'
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother_AoBaBa_Review.blend'))
print('FINISH_PASS',counts,'Neck seam normal transfer added; original cage crotch folds relaxed in corrective key.')
