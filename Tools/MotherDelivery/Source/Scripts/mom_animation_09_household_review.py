import bpy,json,math
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery');PREVIEW=OUT/'Preview/UserMomAnimation/Household';PREVIEW.mkdir(exist_ok=True)
scene=bpy.context.scene;rig=bpy.data.objects['Mom_User_Deform'];mesh=bpy.data.objects['Mom_User_Character'];camera=bpy.data.objects['UserMom_ReviewCamera'];scene.camera=camera
scene.render.resolution_x=640;scene.render.resolution_y=820;scene.render.resolution_percentage=100;scene.render.engine='BLENDER_EEVEE';scene.render.image_settings.file_format='PNG'
camera.data.type='ORTHO';camera.data.ortho_scale=1.36
captures=[]
for action,frame,label,eye,target in [
 ('MomUser_SitDown',43,'Mid_Front',(0,-3,.575),(0,0,.575)),
 ('MomUser_Idle_Seated',1,'Front',(0,-3,.44),(0,.04,.44)),
 ('MomUser_Idle_Seated',1,'Side',(3,0,.44),(0,.04,.44)),
 ('MomUser_Idle_Seated',1,'Back',(0,3,.44),(0,.04,.44)),
 ('MomUser_StandUp',43,'Mid_Side',(3,0,.575),(0,0,.575)),
 ('MomUser_CallChild',55,'Peak_Front',(0,-3,.575),(0,0,.575)),
 ('MomUser_CallChild',55,'Peak_Side',(3,0,.575),(0,0,.575))]:
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(frame);bpy.context.view_layer.update()
 camera.location=eye;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(PREVIEW/(action+'_'+label+'.png'));bpy.ops.render.render(write_still=True);captures.append(scene.render.filepath)
manifest=json.loads((OUT/'Source/user_mom_household_manifest.json').read_text());new=[c for c in manifest if c['action'] in ['MomUser_SitDown','MomUser_Idle_Seated','MomUser_StandUp','MomUser_CallChild']]
rest_points=[mesh.matrix_world@v.co for v in mesh.data.vertices];edges=[tuple(edge.vertices) for edge in mesh.data.edges];lengths={(a,b):(rest_points[a]-rest_points[b]).length for a,b in edges}
report={'captures':captures,'clips':[],'errors':[],'warnings':[],'transition_matrix_errors':{}}
def matrices(action,frame):
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(frame);bpy.context.view_layer.update();return {p.name:p.matrix.copy() for p in rig.pose.bones}
def difference(a,b):return max(abs(a[n][i][j]-b[n][i][j]) for n in a for i in range(4) for j in range(4))
for clip in new:
 begin=matrices(clip['action'],1);end=matrices(clip['action'],clip['frame_end']);entry={'action':clip['action'],'loop_matrix_error':difference(begin,end) if clip['loop'] else None,'samples':[]}
 for frame in sorted({1,round(clip['frame_end']*.25),round(clip['frame_end']*.5),round(clip['frame_end']*.75),clip['frame_end']}):
  matrices(clip['action'],frame);dg=bpy.context.evaluated_depsgraph_get();ob=mesh.evaluated_get(dg);data=ob.to_mesh();points=[mesh.matrix_world@v.co for v in data.vertices]
  stretches=[((points[a]-points[b]).length/lengths[(a,b)],a,b) for a,b in edges if lengths[(a,b)]>.0005]
  worst=sorted(stretches,reverse=True)[:8]
  entry['samples'].append({'frame':frame,'min_z':min(p.z for p in points),'max_edge_stretch':worst[0][0],'edges_stretched_over_2x':sum(ratio>2 for ratio,_,_ in stretches),'worst_edges':[{'ratio':ratio,'vertices':[a,b],'rest_position':list(rest_points[a])} for ratio,a,b in worst if ratio>2]});ob.to_mesh_clear()
 if clip['loop'] and entry['loop_matrix_error']>.001:report['errors'].append('Loop seam: '+clip['action'])
 if any(sample['edges_stretched_over_2x'] for sample in entry['samples']):report['warnings'].append('Cloth/skin stretch over 2x remains in sampled frames: '+clip['action'])
 if any(sample['min_z']<-.002 for sample in entry['samples']):report['warnings'].append('Floor penetration over 2 mm: '+clip['action'])
 report['clips'].append(entry)
for label,a,af,b,bf in [
 ('Standing_to_SitDown','MomUser_Idle',1,'MomUser_SitDown',1),
 ('SitDown_to_Seated','MomUser_SitDown',85,'MomUser_Idle_Seated',1),
 ('Seated_to_StandUp','MomUser_Idle_Seated',1,'MomUser_StandUp',1),
 ('StandUp_to_Standing','MomUser_StandUp',85,'MomUser_Idle',1),
 ('Standing_to_CallChild','MomUser_Idle',1,'MomUser_CallChild',1),
 ('CallChild_to_Standing','MomUser_CallChild',109,'MomUser_Idle',1)]:
 error=difference(matrices(a,af),matrices(b,bf));report['transition_matrix_errors'][label]=error
 if error>.001:report['errors'].append('Transition mismatch: '+label)
rig.animation_data.action=bpy.data.actions['MomUser_Idle'];scene.frame_start=1;scene.frame_end=121;scene.frame_set(1)
(OUT/'Source/user_mom_household_review.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Household.blend'))
print(json.dumps(report))
