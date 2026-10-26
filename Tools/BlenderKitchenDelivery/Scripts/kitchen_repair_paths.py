exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
for im in bpy.data.images:
 if im.name.startswith('M_'):
  im.filepath=str(OUT/'Textures'/(im.name+'.png'));im.reload()
r=bpy.data.objects['SM_RiceWashingPot'];w=bpy.data.objects['Water_Surface'];t=bpy.data.objects['Tilt_Pivot']
export(r);save('01_RiceWashingPot')
render('01_WashingPot_Upright',[r]);t.rotation_euler.x=math.radians(23)
render('02_WashingPot_Tilt23',[r],(.85,-1.05,.83));t.rotation_euler.x=0
render('03_WashingPot_Interior',[r],(.43,-.55,.94),(0,0,.12),62,exclude=[w])
w.hide_render=False
save('01_RiceWashingPot')
