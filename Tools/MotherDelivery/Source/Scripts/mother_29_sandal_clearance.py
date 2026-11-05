import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
shoe=bpy.data.objects['Mother_Footwear'];body=bpy.data.objects['Mother_Body'];key=shoe.data.shape_keys.key_blocks['PeriodSandal_ContactFit']
bm=bmesh.new();bm.from_mesh(shoe.data);left=set(bm.verts);strap_ids=set()
while left:
 stack=[left.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if n in left:left.remove(n);stack.append(n)
 if max((shoe.matrix_world@v.co).z for v in part)<.03:
  for v in part:
   p=shoe.matrix_world@v.co;p.z=.001+(p.z-.001)*.52;key.data[v.index].co=shoe.matrix_world.inverted()@p
 else:strap_ids.update(v.index for v in part)
bm.free()
neighbors=[set() for _ in shoe.data.vertices]
for edge in shoe.data.edges:
 a,b=edge.vertices;neighbors[a].add(b);neighbors[b].add(a)
def evaluated(obj):
 dg=bpy.context.evaluated_depsgraph_get();ob=obj.evaluated_get(dg);me=ob.to_mesh();me.calc_loop_triangles();points=[obj.matrix_world@v.co for v in me.vertices];triangles=[tuple(t.vertices) for t in me.loop_triangles];tree=BVHTree.FromPolygons(points,triangles,all_triangles=True,epsilon=0);ob.to_mesh_clear();return tree,triangles
bpy.context.scene.frame_set(1);bpy.context.view_layer.update();body_tree,_=evaluated(body)
history=[];delta=shoe.matrix_world.inverted().to_3x3()@Vector((0,0,.0008))
for iteration in range(16):
 bpy.context.scene.frame_set(1);bpy.context.view_layer.update();tree,triangles=evaluated(shoe);pairs=tree.overlap(body_tree);history.append(len(pairs))
 if not pairs:break
 affected={v for a,b in pairs for v in triangles[a] if v in strap_ids}
 if not affected:break
 adjacent={n for v in affected for n in neighbors[v] if n in strap_ids}-affected
 for i in affected:key.data[i].co+=delta
 for i in adjacent:key.data[i].co+=delta*.40
bpy.context.scene.frame_set(1);bpy.context.view_layer.update();tree,_=evaluated(shoe);remaining=len(tree.overlap(body_tree))
report={'overlap_history':history,'remaining_neutral_pose_candidate_pairs':remaining,'method':'Existing corrective key only; low plain sole beneath foot; sub-mm local strap clearance increments. Topology/UVs/weights unchanged.'}
(OUT/'Source/ao_ba_ba_sandal_clearance.json').write_text(json.dumps(report,indent=2))
if remaining:print('UNRESOLVED_FOOTWEAR_CONTACT',remaining)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother_AoBaBa_Review.blend'))
print(json.dumps(report))
