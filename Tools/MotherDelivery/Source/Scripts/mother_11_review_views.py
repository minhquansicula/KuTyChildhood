import bpy, math
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
s=bpy.context.scene;cam=bpy.data.objects['Mother_Camera'];s.camera=cam
old=(s.render.engine,s.render.resolution_x,s.render.resolution_y,s.render.resolution_percentage)
floor=bpy.data.objects['Mother_StudioFloor'];old_floor=floor.hide_render;floor.hide_render=True
s.render.engine='BLENDER_WORKBENCH'
s.render.resolution_x=800;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.display.shading.light='STUDIO';s.display.shading.color_type='MATERIAL'
s.display.shading.show_shadows=True;s.display.shading.show_cavity=True
s.display.shading.cavity_type='BOTH';s.display.shading.background_type='WORLD'
cam.data.type='ORTHO';cam.data.ortho_scale=1.85
for name,eye in [('09_Repair_Front',(0,-4,.84)),('10_Repair_Side',(4,0,.84)),('11_Repair_Back',(0,4,.84))]:
 cam.location=eye;cam.rotation_euler=(Vector((0,0,.80))-cam.location).to_track_quat('-Z','Y').to_euler()
 s.render.filepath=str(OUT/'Preview'/(name+'.png'));bpy.ops.render.render(write_still=True)
s.render.engine,s.render.resolution_x,s.render.resolution_y,s.render.resolution_percentage=old
floor.hide_render=old_floor;cam.data.type='PERSP'
print('Three orthographic geometry review images rendered.')
