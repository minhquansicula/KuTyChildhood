exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
al=bpy.data.materials['M_Aluminum_Worn'];soot=bpy.data.materials['M_Aluminum_Soot']
rice_mat=texmat('M_CookedRice',(.85,.83,.71),.7,0,1024,'rice')
wood=texmat('M_DarkWood',(.17,.095,.038),.76,0,1024,'wood')
root=empty('SM_CookingPot')
profile=[(.001,.004),(.12,.004),(.175,.007),(.19,.015),(.202,.032),(.215,.07),(.226,.12),(.237,.19),(.246,.26),(.249,.313),(.253,.319),(.253,.325),(.247,.329),(.241,.325),(.241,.319),(.243,.312),(.240,.26),(.231,.19),(.220,.12),(.209,.07),(.196,.039),(.182,.025),(.15,.02),(.001,.02)]
body=lathe('Pot_Body',profile,al,root,n=96);body.data.materials.append(soot)
for p in body.data.polygons[:7*96]:p.material_index=1
hs=handles(root,.274,al,.245)
lid=empty('Lid_Pivot',root,(0,0,.328));lid['interaction']='Raise local Z >=0.12m before translating sideways >=0.40m; then tilt local X. Origin is lid center.'
lp=[(.001,-.001),(.08,-.001),(.16,-.001),(.225,-.002),(.242,-.006),(.247,-.006),(.25,-.001),(.25,.003),(.245,.007),(.23,.009),(.19,.021),(.13,.031),(.06,.037),(.001,.038)]
lmesh=lathe('Lid_Mesh',lp,al,lid,n=72,asym=.0005)
knob=lathe('Lid_Knob',[(.001,.038),(.013,.038),(.013,.045),(.022,.048),(.026,.055),(.026,.068),(.022,.075),(.001,.076)],wood,lid,n=28,asym=.0002)
origin(lmesh,(0,0,.328)) if False else None
cr=rice('CookedRice',root,.227,.239,220,rice_mat,True)
grip=empty('LidGrip_Anchor',root,(0,0,.399));grip['follow']='In Unity cache local offset from Lid_Pivot; anchor must follow lid.'
steam=empty('Steam_Anchor',root,(0,0,.322))
export(root);save('02_CookingPot')
render('04_CookingPot_Closed',[root],(.88,-1.12,.88),(0,0,.20))
lid.location.z=.55
render('05_CookingPot_Lifted',[root],(.88,-1.12,1.03),(0,0,.27),52)
lid.location=(.44,.07,.19);lid.rotation_euler=(.2,-.26,.12)
render('06_CookingPot_LidAside',[root],(.99,-1.36,1.15),(.12,0,.22),53)
render('07_CookedRice_Closeup',[root],(.41,-.53,.96),(0,0,.24),63)
lid.location=(0,0,.328);lid.rotation_euler=(0,0,0)
save('02_CookingPot')
