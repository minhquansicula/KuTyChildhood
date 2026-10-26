exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
al=texmat('M_Aluminum_Worn',(.47,.49,.48),.58,.78,2048,'metal')
soot=texmat('M_Aluminum_Soot',(.065,.058,.046),.84,.24,2048,'soot')
raw=texmat('M_RawRice',(.76,.73,.58),.74,0,1024,'rice')
water=simplemat('M_RiceWater',(.58,.64,.54),.15)
p=water.node_tree.nodes.get('Principled BSDF');p.inputs['Alpha'].default_value=.33;p.inputs['Transmission Weight'].default_value=.15;p.inputs['IOR'].default_value=1.333
water.surface_render_method='DITHERED';water.diffuse_color=(.58,.64,.54,.33)
root=empty('SM_RiceWashingPot');root['description']='Old aluminum washing pot; front/drain is -Y. Metres.'
pivot=empty('Tilt_Pivot',root,(0,-.176,.019));pivot['interaction']='Rotate local X +22 degrees to drain toward -Y. Root remains on floor.'
# Generate in root coordinates then preserve transforms under tilt pivot.
profile=[(.001,.004),(.11,.004),(.168,.006),(.184,.012),(.196,.026),(.205,.053),(.213,.095),(.22,.145),(.226,.205),(.231,.262),(.234,.270),(.234,.275),(.229,.278),(.224,.275),(.224,.269),(.226,.262),(.221,.205),(.215,.145),(.208,.095),(.200,.053),(.191,.03),(.179,.02),(.15,.017),(.001,.017)]
body=lathe('Pot_Body',profile,al,root,n=96);body.data.materials.append(soot)
for poly in body.data.polygons:
 if poly.center.z<0:pass
# Ring-index assignment gives a mottled soot band without an opaque decal shell.
for j in range(7):
 for p in body.data.polygons[j*96:(j+1)*96]:p.material_index=1
hs=handles(root,.225,al,.229)
grains=rice('Rice_Grains',root,.204,.103,220,raw)
watermesh=lathe('Water_Surface',[(.001,.124),(.209,.124)],water,root,n=96,asym=0)
# Horizontal surface normal faces upward; radial profile outside direction needs reversal.
for p in watermesh.data.polygons:p.flip()
stir=empty('Stir_Anchor',root,(0,0,.124));drain=empty('Drain_Anchor',root,(0,-.23,.274));grip=empty('Grip_Anchor',root,(-.28,0,.237))
bpy.context.view_layer.update()
for o in [body,*hs,grains,watermesh,stir,drain,grip]:
 world=o.matrix_world.copy();o.parent=pivot;o.matrix_world=world
origin(watermesh,(0,0,.124));watermesh['interaction']='Move local Z to lower water; shader alpha/transmission controlled in URP. Radius must reduce when lowering below z=.095.'
export(root);save('01_RiceWashingPot')
render('01_WashingPot_Upright',[root])
pivot.rotation_euler.x=math.radians(23);render('02_WashingPot_Tilt23',[root],(.85,-1.05,.83));pivot.rotation_euler.x=0
watermesh.hide_render=True
render('03_WashingPot_Interior',[root],(.43,-.55,.94),(0,0,.12),62,exclude=[watermesh])
save('01_RiceWashingPot')
