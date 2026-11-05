import bpy,math,json
from mathutils import Quaternion
scene=bpy.context.scene;rig=bpy.data.objects['Mom_User_Deform'];mesh=bpy.data.objects['Mom_User_Character']
rig.animation_data.action=bpy.data.actions['MomUser_Idle'];scene.frame_start=1;scene.frame_end=121;scene.frame_set(1)
for obj in scene.objects:
 obj.select_set(False)
 if obj.type in {'CAMERA','LIGHT','ARMATURE','EMPTY'}:obj.hide_set(True)
mesh.hide_set(False)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.shading.type='MATERIAL';space.shading.use_scene_world=False;space.shading.use_scene_lights=False;space.overlay.show_extras=False;space.overlay.show_floor=False;space.overlay.show_axis_x=False;space.overlay.show_axis_y=False
   if hasattr(space.overlay,'show_bones'):space.overlay.show_bones=False
   space.region_3d.view_location=(0,0,.575);space.region_3d.view_distance=1.70;space.region_3d.view_perspective='ORTHO';space.region_3d.view_rotation=Quaternion((math.cos(math.pi/4),math.sin(math.pi/4),0,0));area.tag_redraw()
# Make the rig's action selector accessible without displaying bones over the mesh.
rig.hide_set(False);rig.select_set(True);bpy.context.view_layer.objects.active=rig;rig.show_in_front=False
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type in {'DOPESHEET_EDITOR','TIMELINE'}:
   area.type='DOPESHEET_EDITOR';area.spaces.active.mode='ACTION';area.tag_redraw()
bpy.ops.wm.save_as_mainfile(filepath='E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery/Source/Mom_User_Animated.blend')
print(json.dumps({'file':bpy.data.filepath,'active_action':rig.animation_data.action.name,'frame_range':[scene.frame_start,scene.frame_end],'available_actions':[a.name for a in bpy.data.actions if a.name.startswith('MomUser_')],'instruction':'Space: play/pause; switch action through Action Editor on Mom_User_Deform. No export performed.'}))
