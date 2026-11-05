import bpy, math
from mathutils import Quaternion

bpy.ops.wm.open_mainfile(filepath='E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery/Source/Mother.blend', load_ui=False)
scene = bpy.context.scene
rig = bpy.data.objects.get('Mother_Deform')
if rig and rig.animation_data:
    rig.animation_data.action = bpy.data.actions.get('Mother_Idle_Standing')
scene.frame_start = 1
scene.frame_end = 151
scene.frame_set(1)
for obj in bpy.data.objects:
    obj.select_set(False)
    if obj.type in {'CAMERA', 'LIGHT', 'ARMATURE', 'EMPTY'} or obj.name == 'Mother_StudioFloor':
        obj.hide_set(True)
    elif obj.name.startswith('Mother_') and obj.type == 'MESH':
        obj.hide_set(False)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type != 'VIEW_3D':
            continue
        space = area.spaces.active
        space.shading.type = 'MATERIAL'
        space.shading.use_scene_world = False
        space.shading.use_scene_lights = False
        space.overlay.show_floor = False
        space.overlay.show_axis_x = False
        space.overlay.show_axis_y = False
        space.overlay.show_extras = False
        space.clip_start = .01
        space.clip_end = 100
        space.region_3d.view_perspective = 'ORTHO'
        space.region_3d.view_location = (0, 0, .81)
        space.region_3d.view_distance = 2.5
        space.region_3d.view_rotation = Quaternion((math.cos(math.pi/4), math.sin(math.pi/4), 0, 0))
        area.tag_redraw()
print('Opened latest Mother model for visual review; material preview, front view, rigs hidden. File not overwritten.')
