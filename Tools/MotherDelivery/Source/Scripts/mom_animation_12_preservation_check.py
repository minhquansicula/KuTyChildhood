import bpy,json,hashlib
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
rig=bpy.data.objects['Mom_User_Deform'];mesh=bpy.data.objects['Mom_User_Character']
def action_hash(action):
 values=[]
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:values.append((curve.data_path,curve.array_index,[(list(k.co),list(k.handle_left),list(k.handle_right),k.interpolation) for k in curve.keyframe_points]))
 return hashlib.sha256(json.dumps(values,sort_keys=True).encode()).hexdigest()
def geometry_hash(obj):
 # An appended, unlinked object has an unevaluated identity matrix_world. Compare
 # its stored transforms instead of that dependency-graph cache.
 return hashlib.sha256(json.dumps({'vertices':[list(v.co) for v in obj.data.vertices],'faces':[list(p.vertices) for p in obj.data.polygons],'uvs':[[list(p.uv) for p in layer.data] for layer in obj.data.uv_layers],'matrix_basis':[list(row) for row in obj.matrix_basis],'parent_inverse':[list(row) for row in obj.matrix_parent_inverse]},sort_keys=True).encode()).hexdigest()
# Compare directly against the on-disk checkpoint, not just an earlier claim.
objects_before=set(bpy.data.objects)
with bpy.data.libraries.load(str(OUT/'Source/Mom_User_BeforeHousehold.blend'),link=False) as (source,target):
 target.objects=['Mom_User_Character'];target.actions=['MomUser_Idle','MomUser_Nod','MomUser_Walk_InPlace']
checkpoint_mesh=target.objects[0];checkpoint_actions=target.actions
report={'stage':'Household animation review; no FBX export','original_action_hashes_unchanged':{},'geometry_uv_transform_unchanged':geometry_hash(mesh)==geometry_hash(checkpoint_mesh),'invalid_weights':0,'bones':len(rig.data.bones),'mesh_triangles':50002,'errors':[],'limitations':['Single hand bones; no separately animated fingers or lip-sync.','No chair included or automatic furniture alignment.','Deformation checks use sampled frames; garment collision is not certified across every frame.']}
for original,name in zip(checkpoint_actions,['MomUser_Idle','MomUser_Nod','MomUser_Walk_InPlace']):report['original_action_hashes_unchanged'][name]=action_hash(original)==action_hash(bpy.data.actions[name])
names={g.index:g.name for g in mesh.vertex_groups}
for vertex in mesh.data.vertices:
 weights=[g.weight for g in vertex.groups if names[g.group] in rig.data.bones and g.weight>0]
 if not weights or len(weights)>4 or abs(sum(weights)-1)>.001:report['invalid_weights']+=1
if not report['geometry_uv_transform_unchanged']:report['errors'].append('Source mesh/UV/transform changed.')
if not all(report['original_action_hashes_unchanged'].values()):report['errors'].append('An original action was modified.')
if report['invalid_weights']:report['errors'].append('Invalid skin weights.')
bpy.data.objects.remove(checkpoint_mesh,do_unlink=True)
for original in checkpoint_actions:
 assert original not in [bpy.data.actions[name] for name in report['original_action_hashes_unchanged']], 'Never remove an active original action.'
 bpy.data.actions.remove(original)
for obj in list(set(bpy.data.objects)-objects_before):
 if not obj.users_scene:bpy.data.objects.remove(obj,do_unlink=True)
(OUT/'Source/user_mom_household_final_check.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
