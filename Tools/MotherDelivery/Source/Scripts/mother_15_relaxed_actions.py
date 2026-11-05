import bpy
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
for name in ['Mother_Control','Mother_Deform']:
 obj=bpy.data.objects[name]
 if obj.animation_data:obj.animation_data.action=None
for act in list(bpy.data.actions):
 if act.name.startswith(('Mother_','CTRL_Mother_')):bpy.data.actions.remove(act)
source=Path('E:/BLENDER/mother_05_animation.py').read_text(encoding='utf8').split("render('05_IdleStanding'")[0]
source=source.replace("save('05_TenBakedAnimations')", "bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother.blend'))")
exec(compile(source,'mother_05_animation.py','exec'))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Progress/08_SlenderWarmRelaxed.blend'),copy=True)
exec(compile(Path('E:/BLENDER/mother_11_review_views.py').read_text(encoding='utf8'),'review','exec'))
print('Ten actions regenerated with a relaxed elbow pole and continuous forearm/hand direction.')
