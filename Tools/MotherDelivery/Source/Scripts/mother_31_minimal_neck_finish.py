import bpy,math
from mathutils import Vector
from pathlib import Path
shirt=bpy.data.objects['Mother_Shirt'];shape=shirt.data.shape_keys.key_blocks['AoBaBa_LooseCut_MinimalNeck']
# Keep the two existing collar rows ordered: an extremely thin, still positive
# separation is preferable to collapsing/reversing them against the neckline.
for v in shirt.data.vertices:
 x,y,z=v.co
 if z>1.312 and abs(x)<.105:
  p=shape.data[v.index].co
  p.z=1.312+(z-1.312)*.53
  if y<-.06:p.z-=.008*math.exp(-(x/.024)**2)
shirt.data.update();bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath='E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery/Source/Mother_AoBaBa_Review.blend')
print('Minimal neck rows retain positive separation; short central neckline dip preserved.')
