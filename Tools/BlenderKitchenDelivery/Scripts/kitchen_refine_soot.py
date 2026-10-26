exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
old=bpy.data.materials['M_Aluminum_Soot'];slots=[(o,i) for o in scene.objects if o.type=='MESH' for i,m in enumerate(o.data.materials) if m==old]
bpy.data.materials.remove(old,do_unlink=True)
for im in list(bpy.data.images):
 if im.name.startswith('M_Aluminum_Soot'):bpy.data.images.remove(im)
new=texmat('M_Aluminum_Soot',(.045,.041,.032),.84,.24,2048,'soot')
for o,i in slots:o.data.materials[i]=new
a=bpy.data.objects['SM_RiceWashingPot'];b=bpy.data.objects['SM_CookingPot'];w=bpy.data.objects['Water_Surface'];t=bpy.data.objects['Tilt_Pivot'];lid=bpy.data.objects['Lid_Pivot']
export(a);export(b);save('03_PotsSurfaceRefined')
render('01_WashingPot_Upright',[a]);t.rotation_euler.x=math.radians(23);render('02_WashingPot_Tilt23',[a],(.85,-1.05,.83));t.rotation_euler.x=0
render('03_WashingPot_Interior',[a],(.43,-.55,.94),(0,0,.12),62,exclude=[w]);w.hide_render=False
render('04_CookingPot_Closed',[b],(.88,-1.12,.88),(0,0,.20));lid.location.z=.55
render('05_CookingPot_Lifted',[b],(.88,-1.12,1.03),(0,0,.27),52)
# Place on floor well clear of the pot, with a slight natural tilt.
lid.location=(.57,.06,.083);lid.rotation_euler=(.12,-.16,.12)
render('06_CookingPot_LidAside',[b],(1.1,-1.44,1.14),(.2,0,.2),53)
render('07_CookedRice_Closeup',[b],(.41,-.53,.96),(0,0,.24),63)
lid.location=(0,0,.328);lid.rotation_euler=(0,0,0)
save('03_PotsSurfaceRefined')
