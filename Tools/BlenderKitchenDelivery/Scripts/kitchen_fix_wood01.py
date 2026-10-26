exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
old=bpy.data.materials['M_WoodEnd'];bpy.data.materials.remove(old,do_unlink=True)
for im in list(bpy.data.images):
 if im.name.startswith('M_WoodEnd'):bpy.data.images.remove(im)
r=bpy.data.objects['SM_Firewood_01']
for o in list(r.children_recursive)+[r]:bpy.data.objects.remove(o,do_unlink=True)
INDEX=1
exec(compile(open('E:/BLENDER/kitchen_04_firewood.py',encoding='utf8').read(),'kitchen_04_firewood.py','exec'))
