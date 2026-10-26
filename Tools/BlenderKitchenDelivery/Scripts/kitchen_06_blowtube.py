exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
bamboo=texmat('M_AgedBamboo',(.46,.31,.105),.69,0,1024,'bamboo')
inner=texmat('M_BambooInside',(.225,.15,.062),.85,0,1024,'bamboo')
root=empty('SM_BlowTube');pivot=empty('Grip_Pivot',root,(0,-.145,.019))
# A single closed wall profile leaves the bore open along its entire length.
profile=[(.014,-.2),(.018,-.2),(.0185,-.196),(.018,-.14),(.0182,-.08),(.018,.0),(.0177,.08),(.0175,.15),(.017,.196),(.0165,.2),(.0135,.2),(.0132,.196),(.0136,.1),(.014,.0),(.014,-.1),(.014,-.196),(.014,-.2)]
o=lathe('Bamboo_Tube',profile,bamboo,None,n=32,asym=.0003)
# Lathe Z to longitudinal +Y; preserve 0.40m length, grip near player end.
for v in o.data.vertices:
 x,y,z=v.co;v.co=(x,z+.145,y)
o.parent=pivot;o.data.materials.append(inner)
for p in o.data.polygons[10*32:]:p.material_index=1
parts=[o]
for y in [-.073,.095]:
 pts=[(.0182*math.cos(TAU*i/32),y+.145,.0182*math.sin(TAU*i/32)) for i in range(32)]
 parts.append(tube_path('Bamboo_Node',pts,.0016,bamboo,pivot,6,True))
o=combine(parts,'BlowTube_Mesh',pivot)
empty('Mouth_Anchor',root,(0,-.2,.019));air=empty('AirOut_Anchor',root,(0,.2,.019));air.rotation_euler.x=-math.pi/2
root['interaction']='Rotate Grip_Pivot near mouth end; AirOut_Anchor local +Z points along tube into stove. Both ends have true open bore.'
export(root);save('06_BlowTube');render('13_BlowTube',[root],(.19,-.42,.23),(0,0,.019),55);save('06_BlowTube')
