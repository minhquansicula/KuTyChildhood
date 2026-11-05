import bpy,json,math
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery');PREVIEW=OUT/'Preview/UserMomAnimation'
scene=bpy.context.scene;rig=bpy.data.objects['Mom_User_Deform'];mesh=bpy.data.objects['Mom_User_Character'];camera=bpy.data.objects['UserMom_ReviewCamera'];scene.camera=camera
scene.render.resolution_x=560;scene.render.resolution_y=720;scene.render.resolution_percentage=100
camera.data.type='ORTHO';camera.data.ortho_scale=1.36
captures=[]
for action,frame,view,eye in [
 ('MomUser_Idle',1,'Front',(0,-3,.575)),('MomUser_Idle',1,'Side',(3,0,.575)),('MomUser_Idle',1,'Back',(0,3,.575)),
 ('MomUser_Walk_InPlace',10,'Front',(0,-3,.575)),('MomUser_Walk_InPlace',10,'Side',(3,0,.575)),
 ('MomUser_Walk_InPlace',28,'Side',(3,0,.575)),('MomUser_Nod',38,'Front',(0,-3,.575))]:
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(frame);bpy.context.view_layer.update()
 camera.location=eye;camera.rotation_euler=(Vector((0,0,.575))-camera.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(PREVIEW/(action+'_'+str(frame)+'_'+view+'.png'));bpy.ops.render.render(write_still=True);captures.append(scene.render.filepath)
report={'captures':captures,'clips':[],'errors':[]}
manifest=json.loads((OUT/'Source/user_mom_animation_manifest.json').read_text())
mesh.data.calc_loop_triangles();triangles=[tuple(t.vertices) for t in mesh.data.loop_triangles];rest_points=[mesh.matrix_world@v.co for v in mesh.data.vertices]
rest_edges={tuple(sorted((triangle[i],triangle[(i+1)%3]))) for triangle in triangles for i in range(3)}
rest_lengths={edge:(rest_points[edge[0]]-rest_points[edge[1]]).length for edge in rest_edges}
for clip in manifest:
 rig.animation_data.action=bpy.data.actions[clip['action']];scene.frame_set(1)
 start={p.name:p.matrix.copy() for p in rig.pose.bones};scene.frame_set(clip['frame_end'])
 seam=max(abs(start[p.name][i][j]-p.matrix[i][j]) for p in rig.pose.bones for i in range(4) for j in range(4))
 entry={'action':clip['action'],'loop_matrix_error':seam if clip['loop'] else None,'samples':[]}
 for frame in sorted({1,round(clip['frame_end']*.25),round(clip['frame_end']*.5),round(clip['frame_end']*.75),clip['frame_end']}):
  scene.frame_set(frame);dg=bpy.context.evaluated_depsgraph_get();evaluated=mesh.evaluated_get(dg);data=evaluated.to_mesh();points=[mesh.matrix_world@v.co for v in data.vertices]
  stretches=[(points[a]-points[b]).length/rest_lengths[(a,b)] for a,b in rest_edges if rest_lengths[(a,b)]>.0005]
  foots={side:list(rig.pose.bones['Foot.'+side].head) for side in ['L','R']}
  entry['samples'].append({'frame':frame,'min_z':min(p.z for p in points),'max_edge_stretch':max(stretches),'edges_stretched_over_2x':sum(ratio>2 for ratio in stretches),'ankles':foots});evaluated.to_mesh_clear()
 if clip['loop'] and seam>.001:report['errors'].append('Loop seam: '+clip['action'])
 report['clips'].append(entry)
rig.animation_data.action=bpy.data.actions['MomUser_Idle'];scene.frame_start=1;scene.frame_end=121;scene.frame_set(1)
(OUT/'Source/user_mom_animation_review.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Animated.blend'))
print(json.dumps(report))
