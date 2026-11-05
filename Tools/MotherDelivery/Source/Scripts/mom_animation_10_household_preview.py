import bpy,json,math
from pathlib import Path
from mathutils import Quaternion
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
scene=bpy.context.scene;rig=bpy.data.objects['Mom_User_Deform'];mesh=bpy.data.objects['Mom_User_Character']
assert bpy.data.filepath.endswith('Mom_User_Household.blend')
name='PREVIEW_MomUser_HouseholdSequence'
if bpy.data.actions.get(name):raise RuntimeError('Preview already exists; preserve it instead of overwriting.')
segments=[('MomUser_SitDown',85),('MomUser_Idle_Seated',121),('MomUser_StandUp',85),('MomUser_CallChild',109)]
samples=[];metadata=[]
for index,(source,end) in enumerate(segments):
 rig.animation_data.action=bpy.data.actions[source];first=len(samples)+1
 for frame in range(1 if index==0 else 2,end+1):
  scene.frame_set(frame);bpy.context.view_layer.update()
  samples.append({pb.name:(pb.location.copy(),pb.rotation_quaternion.copy(),pb.scale.copy()) for pb in rig.pose.bones})
 metadata.append({'source':source,'timeline_start':first,'timeline_end':len(samples)})
preview=bpy.data.actions.new(name);preview.use_fake_user=True;preview['purpose']='Artist-review sequence only: sit, seated idle, stand, call child; not an additional production clip.';rig.animation_data.action=preview
previous={}
for frame,sample in enumerate(samples,1):
 for bone,channels in sample.items():
  location,rotation,scale=channels
  if bone in previous and rotation.dot(previous[bone])<0:rotation.negate()
  previous[bone]=rotation.copy();pb=rig.pose.bones[bone];pb.location=location;pb.rotation_quaternion=rotation;pb.scale=scale
  for channel in ['location','rotation_quaternion','scale']:pb.keyframe_insert(channel,frame=frame,group=bone)
for layer in preview.layers:
 for strip in layer.strips:
  for bag in strip.channelbags:
   for curve in bag.fcurves:
    for point in curve.keyframe_points:point.interpolation='LINEAR'
scene.frame_start=1;scene.frame_end=len(samples);scene.frame_set(1);scene.render.fps=30
for obj in scene.objects:
 obj.select_set(False)
 if obj.type in {'ARMATURE','EMPTY','CAMERA','LIGHT'}:obj.hide_set(True)
mesh.hide_set(False);rig.hide_set(False);rig.show_in_front=False;rig.select_set(True);bpy.context.view_layer.objects.active=rig
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.shading.type='MATERIAL';space.shading.use_scene_world=False;space.shading.use_scene_lights=False;space.overlay.show_extras=False;space.overlay.show_floor=False;space.overlay.show_axis_x=False;space.overlay.show_axis_y=False
   if hasattr(space.overlay,'show_bones'):space.overlay.show_bones=False
   region=space.region_3d;region.view_location=(0,.03,.575);region.view_distance=1.75;region.view_perspective='ORTHO';region.view_rotation=Quaternion((math.cos(math.pi/4),math.sin(math.pi/4),0,0));area.tag_redraw()
  elif area.type=='DOPESHEET_EDITOR':area.spaces.active.mode='ACTION';area.tag_redraw()
(OUT/'Source/user_mom_household_preview.json').write_text(json.dumps({'action':name,'frames':len(samples),'fps':30,'segments':metadata,'production_action_count':7,'export_performed':False},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Household.blend'))
print(json.dumps({'file':bpy.data.filepath,'preview':name,'frames':len(samples),'seconds':(len(samples)-1)/30,'segments':metadata,'instruction':'Space to review the full sequence. Original and new production clips remain separately selectable.'}))
