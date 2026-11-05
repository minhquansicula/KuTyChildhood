import bpy
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
s=bpy.context.scene;cam=bpy.data.objects['Mother_Camera'];s.camera=cam
old=(s.render.engine,s.render.resolution_x,s.render.resolution_y,s.render.resolution_percentage,s.cycles.samples)
floor=bpy.data.objects['Mother_StudioFloor'];floor.hide_render=True
s.render.engine='CYCLES';s.cycles.samples=12;s.cycles.use_denoising=True
s.render.resolution_x=700;s.render.resolution_y=800;s.render.resolution_percentage=100
cam.data.type='ORTHO'
for name,eye,target,scale in [
 ('12_WarmFace_Material',(.07,-2,1.48),(0,-.035,1.47),.32),
 ('13_Wrist_Material',(.9,-1.5,.89),(.225,-.1,.87),.34)]:
 cam.location=eye;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 s.render.filepath=str(OUT/'Preview'/(name+'.png'));bpy.ops.render.render(write_still=True)
s.render.engine,s.render.resolution_x,s.render.resolution_y,s.render.resolution_percentage,s.cycles.samples=old
floor.hide_render=False;cam.data.type='PERSP'
print('Material-lit closeups rendered for facial expression and wrist alignment.')
