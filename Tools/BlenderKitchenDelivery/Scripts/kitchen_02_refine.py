import bpy
root=bpy.data.objects['SM_CookingPot']
for o in list(root.children_recursive)+[root]:bpy.data.objects.remove(o,do_unlink=True)
exec(compile(open('E:/BLENDER/kitchen_02_cooking.py',encoding='utf8').read(),'kitchen_02_cooking.py','exec'))
