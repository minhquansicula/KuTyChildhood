import bpy,json
from pathlib import Path
REVIEW_CAPTURE_BASELINE=False
exec(compile(Path('E:/BLENDER/mother_23_review_capture.py').read_text(encoding='utf8'),'review_capture','exec'))
# Neutral, restrained exposure rather than a bright beauty-light presentation.
s.view_settings.exposure=-.35
for name,power in [('Review_Key',250),('Review_Fill',150),('Review_Rim',150)]:bpy.data.objects[name].data.energy=power
s.world.node_tree.nodes['Background'].inputs[1].default_value=.40
active=bpy.data.collections['CHARACTER_Mother'];rigs=bpy.data.collections['RIGS_ControlAndDeform'];backup=bpy.data.collections['BACKUP_Mother_Original']
active.hide_render=True;rigs.hide_render=True;backup.hide_render=False;backup.hide_viewport=False
bpy.context.view_layer.update()
before=[capture('Before','Front','Solid'),capture('Before','Front','Material'),capture('Before','ThreeQuarter','Material')]
active.hide_render=False;rigs.hide_render=False;backup.hide_render=True;backup.hide_viewport=True
bpy.context.view_layer.update()
after=[capture('After',view,mode) for mode in ['Solid','Material'] for view in views]
# Extra close-ups for neckline, wrist, cloth hems and the bun transition.
details=[]
for name,eye,target,scale in [
 ('NeckFace',(0,-3,1.44),(0,-.035,1.435),.43),
 ('Bun',(2,2,1.49),(0,.025,1.47),.31),
 ('Wrist',(.9,-1.5,.87),(.21,-.09,.84),.31),
 ('Trousers',(0,-3,.43),(0,-.025,.43),.91)]:
 cam.location=eye;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=scale
 s.render.engine='BLENDER_EEVEE';s.render.filepath=str(PREVIEW/('Detail_'+name+'.png'));bpy.ops.render.render(write_still=True);details.append(s.render.filepath)
(OUT/'Source/ao_ba_ba_review_views.json').write_text(json.dumps({'before':before,'after':after,'details':details,'lighting':'Neutral white area lights; gray world; AgX; exposure -0.35','capture':'Generated Blender solid and material-lit inspection renders; not claimed as viewport screenshots.'},indent=2))
print(json.dumps({'before':before,'after':after,'details':details}))
