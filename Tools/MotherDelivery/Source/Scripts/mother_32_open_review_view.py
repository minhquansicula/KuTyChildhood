import bpy,math,json
from pathlib import Path
from mathutils import Quaternion
assert bpy.data.filepath.endswith('Mother_AoBaBa_Review.blend'), 'Do not reopen or overwrite another file.'
scene=bpy.context.scene;rig=bpy.data.objects['Mother_Deform'];rig.animation_data.action=bpy.data.actions['Mother_AoBaBa_Relaxed_Idle'];scene.frame_start=1;scene.frame_end=151;scene.frame_set(1)
backup=bpy.data.collections['BACKUP_Mother_Original'];backup.hide_viewport=True;backup.hide_render=True
for obj in bpy.data.objects:
 obj.select_set(False)
 if obj.type in {'CAMERA','LIGHT','ARMATURE','EMPTY'} or obj.name=='Mother_StudioFloor':obj.hide_set(True)
for obj in bpy.data.collections['CHARACTER_Mother'].objects:obj.hide_set(False)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type!='VIEW_3D':continue
  space=area.spaces.active;space.shading.type='MATERIAL';space.shading.use_scene_world=False;space.shading.use_scene_lights=False
  space.shading.studiolight_rotate_z=.35;space.overlay.show_floor=False;space.overlay.show_axis_x=False;space.overlay.show_axis_y=False;space.overlay.show_extras=False
  space.clip_start=.005;space.clip_end=100;region=space.region_3d;region.view_perspective='ORTHO';region.view_location=(0,0,.80);region.view_distance=2.45;region.view_rotation=Quaternion((math.cos(math.pi/4),math.sin(math.pi/4),0,0));area.tag_redraw()
scene['mother_review_report']='Tools/MotherDelivery/AO-BA-BA-REVIEW.md. Preserved artist review; unresolved cloth contact is documented; no FBX export.'
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print(json.dumps({'open_file':bpy.data.filepath,'active_action':rig.animation_data.action.name,'backup_hidden':backup.hide_render and backup.hide_viewport,'visible_meshes':[o.name for o in bpy.data.collections['CHARACTER_Mother'].objects if not o.hide_get()],'viewport':'Material preview; front orthographic; rig/studio hidden','status':'Art review, not certified final delivery'}))
