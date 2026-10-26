exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
import shutil
for m in list(bpy.data.meshes):
 if m.users==0:bpy.data.meshes.remove(m)
for m in list(bpy.data.materials):
 if m.users==0:bpy.data.materials.remove(m)
for im in list(bpy.data.images):
 if im.name.startswith('M_') and im.users==0:bpy.data.images.remove(im)
save('12_FinalDeliveryClean')
for file in Path('E:/BLENDER').glob('kitchen_*.py'):shutil.copy2(file,OUT/'Scripts'/file.name)
