exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
bamboo=bpy.data.materials['M_AgedBamboo'];weave=texmat('M_BambooWeave',(.48,.325,.14),.79,0,1024,'weave')
root=empty('SM_BambooTray')
rim=lathe('Tray_Rim',[(.235,.017),(.241,.014),(.249,.019),(.254,.029),(.254,.037),(.248,.043),(.241,.042),(.236,.034),(.235,.017)],bamboo,root,n=80,asym=.001)
# A closed, gently concave woven shell. Fine over/under strands are baked in UV space.
profile=[(.001,.0015),(.08,.004),(.16,.012),(.21,.021),(.239,.029),(.24,.032),(.21,.024),(.16,.015),(.08,.007),(.001,.0045)]
tray=lathe('Tray_Weave',profile,weave,root,n=72,asym=.0003)
for loop in tray.data.loops:
 co=tray.data.vertices[loop.vertex_index].co;tray.data.uv_layers.active.data[loop.index].uv=(co.x/.5+.5,co.y/.5+.5)
bindings=[]
for k in range(24):
 a=TAU*k/24;pts=[]
 for i in range(8):
  t=TAU*i/8;r=.246+.010*math.cos(t);z=.029+.011*math.sin(t)
  pts.append((r*math.cos(a),r*math.sin(a),z))
 bindings.append(tube_path('Rim_Binding',pts,.0014,bamboo,root,3,True))
combine([rim]+bindings,'Tray_Rim',root)
export(root);save('07_BambooTray');render('14_BambooTray',[root],(.46,-.62,.66),(0,0,.02),55);save('07_BambooTray')
