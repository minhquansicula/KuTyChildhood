import bpy,json,math
from pathlib import Path
from mathutils import Matrix
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
mesh=bpy.data.objects['Mom_User_Character']
if mesh.data.shape_keys:
 names=list(mesh.data.shape_keys.key_blocks.keys())
 assert names==['Basis','MomUser_Sit_ClothSupport'],'Unfamiliar facial/shape keys found; do not clear.'
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_ExperimentalClothSupport.blend'),copy=True)
 key=mesh.data.shape_keys.key_blocks['MomUser_Sit_ClothSupport'];key.driver_remove('value')
 mesh.shape_key_clear()
 print('Experimental cloth corrective archived and removed from active rig because it increased local stretch.')
changed=0
source=Path('E:/BLENDER/mom_animation_11_household_cleanup.py').read_text(encoding='utf8')
tail=source[source.index('# Re-evaluate only the four actions'):]
exec(compile(tail,'seat_height_pose_fit','exec'))
print('Seated pose fitted to a moderately higher seat, with planted feet; source model and original three action curves retained.')
