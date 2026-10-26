exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
clay=texmat('M_Clay_Plaster',(.285,.183,.112),.9,0,2048,'clay')
brick=texmat('M_FiredBrick',(.32,.105,.052),.88,0,2048,'clay')
char=texmat('M_Charcoal',(.025,.021,.017),.96,0,1024,'soot')
ash=texmat('M_Ash',(.29,.28,.255),.98,0,1024,'clay')
ember=texmat('M_Ember',(.12,.035,.009),.91,0,1024,'soot');p=ember.node_tree.nodes.get('Principled BSDF');p.inputs['Emission Color'].default_value=(.45,.025,.001,1);p.inputs['Emission Strength'].default_value=.28
root=empty('SM_WoodStove');root['front']='-Y; +Y is feed direction. All slots local +Z points into hearth (Unity forward +Z).'
# Thick horseshoe shell: genuinely open firebox; cross section includes inside wall and beveled feet.
cross=[(.325,.065),(.422,.015),(.459,.015),(.477,.03),(.48,.055),(.465,.08),(.452,.12),(.442,.29),(.435,.365),(.405,.393),(.332,.382),(.321,.355),(.321,.11)]
verts=[];uv=[];segments=72;start=-1.10;end=math.pi+1.10
for j,(r,z) in enumerate(cross):
 for i in range(segments+1):
  a=start+(end-start)*i/segments;rr=r+.0025*math.sin(9*a+z*7)
  verts.append((rr*math.cos(a),rr*math.sin(a)*.88,z+.001*math.cos(a*5)));uv.append((i/segments,j/(len(cross)-1)))
faces=[]
for j in range(len(cross)):
 for i in range(segments):
  a=j*(segments+1)+i;b=((j+1)%len(cross))*(segments+1)+i;faces.append((a,a+1,b+1,b))
for i in [0,segments]:faces.append(tuple(j*(segments+1)+i for j in (range(len(cross)) if i==segments else reversed(range(len(cross))))))
wall=mesh('Stove_Shell',verts,faces,clay,root,uv)
wall.data.materials.append(char)
for poly in wall.data.polygons:
 if poly.index//segments>=10:poly.material_index=1
base=lathe('Stove_Base',[(.001,.005),(.45,.005),(.475,.018),(.475,.04),(.452,.058),(.001,.058)],clay,root,96,0)
for v in base.data.vertices:v.co.y*=.88
# Lower exposed bricks and two jambs give visible history under worn plaster.
parts=[wall,base]
for side in [-1,1]:
 for j in range(4):
  b=box('Mouth_Jamb',(side*(.201+.005*j),-.323,.105+j*.069),(.095,.165,.066),brick,root,.011)
  b.rotation_euler.z=side*-.13;parts.append(b)
 # Soot-dark inner lip sits on the inside face, not a coincident decal.
 b=box('Soot_Jamb',(side*.162,-.32,.215),(.018,.125,.25),char,root,.008);parts.append(b)
body=combine(parts,'Stove_Body',root)
# Thick annular crown, no disk across the fire opening.
top=lathe('Stove_Crown',[(.233,.355),(.394,.355),(.426,.38),(.432,.411),(.42,.438),(.39,.448),(.25,.448),(.231,.425),(.233,.355)],clay,root,96,.002)
for v in top.data.vertices:v.co.y*=.91
rests=[top]
for a in [0,math.pi,math.pi/2]:
 b=box('Pot_Support',(math.cos(a)*.213,math.sin(a)*.213,.454),(.13,.092,.072),char,root,.012);b.rotation_euler.z=a;rests.append(b)
st=combine(rests,'Stove_Top',root)
ashbed=lathe('Ash_Bed',[(.001,.059),(.286,.06),(.296,.068),(.28,.079),(.15,.087),(.001,.083)],ash,root,64,.003)
chunks=[];glowing=[]
for k in range(34):
 a=random.random()*TAU;r=random.random()**.5*.22
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(r*math.cos(a),r*math.sin(a),.087+random.uniform(.002,.012)))
 o=bpy.context.object;o.name='Coal_Lump';o.scale=(random.uniform(.017,.033),random.uniform(.019,.037),random.uniform(.01,.021));bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(ember if k%4==0 else char);o.parent=root
 (glowing if k%4==0 else chunks).append(o)
ashbed=combine([ashbed]+chunks,'Ash_Bed',root);embers=combine(glowing,'Ember_Bed',root)
# Recessed hairline cracks as narrow geometry ribbons conforming to exterior.
cracks=[]
for a,z in [(-.55,.23),(.23,.28),(1.4,.19),(2.7,.31),(-2.8,.18)]:
 pts=[]
 for j in range(6):
  zz=z+j*.014;aa=a+.014*math.sin(j*2);rr=.449-(zz-.15)*.065
  pts.append((rr*math.cos(aa),rr*math.sin(aa)*.88,zz))
 cracks.append(tube_path('Hairline_Crack',pts,.0011,char,root,4))
body=combine([body]+cracks,'Stove_Body',root)
for name,loc in [('PotRest_Anchor',(0,0,.486)),('Hearth_Anchor',(0,-.04,.105)),('Fire_Anchor',(0,-.02,.19)),('Smoke_Anchor',(0,0,.46))]:empty(name,root,loc)
for i,x in enumerate([-.085,0,.085],1):
 o=empty(f'WoodSlot_{i:02d}',root,(x,-.16,.126));o.rotation_euler.x=-math.pi/2;o['axis']='Local +Z = feed into stove (+Y Blender); move -Z to withdraw.'
export(root);save('03_WoodStove')
render('08_Stove_Front',[root],(1.05,-1.65,1.0),(0,0,.23),56)
render('09_Stove_PlayerView',[root],(.42,-1.28,1.33),(0,0,.23),47)
# Real fit: underside of pot is .004m above root, supports top .490m.
pot=bpy.data.objects['SM_CookingPot'];pot.location=(0,0,.486)
render('10_Stove_PotFit',[root,pot],(1.12,-1.65,1.25),(0,0,.4),52)
pot.location=(0,0,0);save('03_WoodStove')
