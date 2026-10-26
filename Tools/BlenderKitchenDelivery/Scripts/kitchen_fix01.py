import bpy
r=bpy.data.objects['SM_RiceWashingPot']
for o in list(r.children_recursive)+[r]:bpy.data.objects.remove(o,do_unlink=True)
for n in ['M_RawRice','M_Aluminum_Soot']:
 bpy.data.materials.remove(bpy.data.materials[n],do_unlink=True)
 for im in list(bpy.data.images):
  if im.name.startswith(n):bpy.data.images.remove(im)
exec(compile(open('E:/BLENDER/kitchen_01_washing.py',encoding='utf8').read(),'kitchen_01_washing.py','exec'))
