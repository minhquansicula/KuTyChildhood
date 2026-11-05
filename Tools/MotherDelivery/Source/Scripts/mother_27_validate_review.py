import bpy,bmesh,json,hashlib,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import intersect_ray_tri
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
manifest=json.loads((OUT/'Source/ao_ba_ba_preservation_manifest.json').read_text())
def action_hash(action):
 curves=[]
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for fc in bag.fcurves:curves.append((fc.data_path,fc.array_index,[(list(k.co),list(k.handle_left),list(k.handle_right),k.interpolation) for k in fc.keyframe_points]))
 return hashlib.sha256(json.dumps(curves,sort_keys=True).encode()).hexdigest()
report={'stage':'Ao ba ba artist review; not a full animation/export certification','errors':[],'warnings':[],'meshes':[],'preservation':{},'collision_checks':{},'limitations':['Triangle segment tests omit coplanar intersections and do not certify all animation frames.','Open garment hems, neckline, cuffs, side slits, skin cuts hidden by clothing, scalp rim and eyebrow strips are intentional boundaries.','Scalp and bun are intentionally overlapping weighted islands, not a destructively remeshed surface.']}
report['preservation']['actions_unchanged']={name:bool(bpy.data.actions.get(name) and action_hash(bpy.data.actions[name])==digest) for name,digest in manifest['actions'].items()}
report['preservation']['texture_files_unchanged']={path:Path(path).is_file() and hashlib.sha256(Path(path).read_bytes()).hexdigest()==digest for path,digest in manifest['image_files'].items()}
rest={}
for name,bones in manifest['rig_rest'].items():
 rig=bpy.data.objects.get(name);rest[name]=bool(rig and len(rig.data.bones)==len(bones) and all(bpy.data.objects[name].data.bones.get(n) and max(abs(rig.data.bones[n].matrix_local[i][j]-matrix[i][j]) for i in range(4) for j in range(4))<1e-7 for n,matrix in bones.items()))
report['preservation']['armature_rest_unchanged']=rest
report['preservation']['backup_collection']=bool(bpy.data.collections.get('BACKUP_Mother_Original'))
report['preservation']['backup_hidden']=bpy.data.collections['BACKUP_Mother_Original'].hide_render and bpy.data.collections['BACKUP_Mother_Original'].hide_viewport
report['preservation']['original_source_file_retained']=Path(OUT/'Source/Mother.blend').is_file()
for category in ['actions_unchanged','texture_files_unchanged','armature_rest_unchanged']:
 if not all(report['preservation'][category].values()):report['errors'].append('Preservation failure: '+category)
rig=bpy.data.objects['Mother_Deform'];scene=bpy.context.scene;scene.frame_set(1);bpy.context.view_layer.update();depsgraph=bpy.context.evaluated_depsgraph_get()
evaluated={}
for obj in bpy.data.collections['CHARACTER_Mother'].objects:
 if obj.type!='MESH':continue
 obj.data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(obj.data)
 bad=0
 for v in obj.data.vertices:
  weights=[g.weight for g in v.groups if obj.vertex_groups[g.group].name in rig.data.bones and g.weight>0]
  if not weights or len(weights)>4 or abs(sum(weights)-1)>.001:bad+=1
 islands=[];todo=set(bm.faces)
 while todo:
  stack=[todo.pop()];component=[]
  while stack:
   face=stack.pop();component.append(face)
   for edge in face.edges:
    for neighbor in edge.link_faces:
     if neighbor in todo:todo.remove(neighbor);stack.append(neighbor)
  closed=all(e.is_manifold for f in component for e in f.edges)
  volume=0.
  if closed:
   for f in component:
    points=[v.co for v in f.verts]
    for i in range(1,len(points)-1):volume+=points[0].dot(points[i].cross(points[i+1]))/6
  islands.append({'faces':len(component),'closed':closed,'signed_volume':volume if closed else None})
 entry={'object':obj.name,'vertices':len(obj.data.vertices),'triangles':len(obj.data.loop_triangles),'uv_layers':[u.name for u in obj.data.uv_layers],'bad_weights':bad,'boundary_edges':sum(e.is_boundary for e in bm.edges),'nonmanifold_edges_more_than_two_faces':sum(len(e.link_faces)>2 for e in bm.edges),'loose_vertices':sum(not v.link_faces for v in bm.verts),'wire_edges':sum(e.is_wire for e in bm.edges),'closed_inverted_components':sum(p['closed'] and p['signed_volume']<-.000000001 for p in islands),'components':islands,'armature_modifiers':[m.object.name if m.object else None for m in obj.modifiers if m.type=='ARMATURE'],'shape_keys':[(k.name,k.value) for k in obj.data.shape_keys.key_blocks] if obj.data.shape_keys else [],'materials':[m.name if m else None for m in obj.data.materials],'local_scale':list(obj.scale)}
 bm.free();report['meshes'].append(entry)
 if bad or entry['loose_vertices'] or entry['wire_edges'] or not entry['uv_layers'] or any(n is None for n in entry['materials']):report['errors'].append('Mesh validation issue: '+obj.name)
 if entry['nonmanifold_edges_more_than_two_faces'] or entry['closed_inverted_components']:report['warnings'].append('Inspect non-manifold/inverted components: '+obj.name)
 evaluated_obj=obj.evaluated_get(depsgraph);mesh=evaluated_obj.to_mesh();mesh.calc_loop_triangles();points=[obj.matrix_world@v.co for v in mesh.vertices];triangles=[tuple(t.vertices) for t in mesh.loop_triangles]
 evaluated[obj.name]=(points,triangles,BVHTree.FromPolygons(points,triangles,all_triangles=True,epsilon=0))
 evaluated_obj.to_mesh_clear()
def crossings(a,b):
 av,at,ab=evaluated[a];bv,bt,bb=evaluated[b];candidates=ab.overlap(bb);hits=set();pairs=0
 for ai,bi in candidates:
  pa=[av[i] for i in at[ai]];pb=[bv[i] for i in bt[bi]];found=False
  for triangle,other in [(pa,pb),(pb,pa)]:
   for i in range(3):
    start=triangle[i];ray=triangle[(i+1)%3]-start
    if ray.length_squared<1e-14:continue
    point=intersect_ray_tri(other[0],other[1],other[2],ray,start,True)
    if point is not None:
     t=(point-start).dot(ray)/ray.length_squared
     if 1e-5<t<1-1e-5:
      hits.add(tuple(round(x,5) for x in point));found=True
  pairs+=found
 return {'candidate_triangle_pairs':len(candidates),'crossing_triangle_pairs':pairs,'sample_points_m':[list(p) for p in sorted(hits)[:40]],'note':'Surface crossings, not a solid-volume containment test.'}
for a,b in [('Mother_Pants','Mother_Shirt'),('Mother_Pants','Mother_Body'),('Mother_Pants','Mother_Footwear'),('Mother_Body','Mother_Footwear'),('Mother_Body','Mother_Shirt'),('Mother_NeckContinuity','Mother_Shirt')]:
 report['collision_checks'][a+' / '+b]=crossings(a,b)
report['garment_self_intersections']={}
for name in ['Mother_Shirt','Mother_Pants']:
 points,triangles,tree=evaluated[name];pairs=[];samples=[]
 for a,b in tree.overlap(tree):
  if a>=b or set(triangles[a]) & set(triangles[b]):continue
  pa=[points[i] for i in triangles[a]];pb=[points[i] for i in triangles[b]];found=False
  for triangle,other in [(pa,pb),(pb,pa)]:
   for i in range(3):
    start=triangle[i];ray=triangle[(i+1)%3]-start
    if ray.length_squared<1e-14:continue
    hit=intersect_ray_tri(other[0],other[1],other[2],ray,start,True)
    if hit is not None:
     t=(hit-start).dot(ray)/ray.length_squared
     if 1e-5<t<1-1e-5:found=True;samples.append([round(v,5) for v in hit]);break
   if found:break
  if found:pairs.append((a,b))
 report['garment_self_intersections'][name]={'nonadjacent_crossing_triangle_pairs':len(pairs),'sample_points_m':samples[:20]}
 if pairs:report['warnings'].append('Unresolved nonadjacent triangle crossings in '+name+': '+str(len(pairs))+' pairs; includes possible intentional button/placket overlap. Not certified intersection-free.')
for label,check in report['collision_checks'].items():
 if check['crossing_triangle_pairs']:report['warnings'].append('Surface contact/crossings requiring review: '+label+' ('+str(check['crossing_triangle_pairs'])+' triangle pairs).')
report['triangles_total']=sum(x['triangles'] for x in report['meshes'])
report['root']={'name':'SK_Mother','location':list(bpy.data.objects['SK_Mother'].location),'rotation':list(bpy.data.objects['SK_Mother'].rotation_euler),'scale':list(bpy.data.objects['SK_Mother'].scale)}
report['active_action']=rig.animation_data.action.name
report['wrist_joint_error_m']={side:(rig.pose.bones['LowerArm.'+side].tail-rig.pose.bones['Hand.'+side].head).length for side in ['L','R']}
(OUT/'Source/ao_ba_ba_validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'triangles':report['triangles_total'],'errors':report['errors'],'warnings':report['warnings'],'original_actions_unchanged':all(report['preservation']['actions_unchanged'].values()),'texture_files_unchanged':all(report['preservation']['texture_files_unchanged'].values()),'rest_rigs_unchanged':rest,'mesh_summary':[{k:x[k] for k in ['object','triangles','bad_weights','boundary_edges','nonmanifold_edges_more_than_two_faces','closed_inverted_components']} for x in report['meshes']],'collision_checks':report['collision_checks'],'garment_self_intersections':report['garment_self_intersections']}))
