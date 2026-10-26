exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
import re,shutil
roots=sorted([o for o in scene.objects if o.parent is None and o.name.startswith('SM_')],key=lambda o:o.name)
layout={r:(r.location.copy(),r.rotation_euler.copy()) for r in roots}
for r in roots:r.location=(0,0,0);r.rotation_euler=(0,0,0)
bpy.context.view_layer.update()
pot=bpy.data.objects['SM_CookingPot'];stove=bpy.data.objects['SM_WoodStove'];lid=bpy.data.objects['Lid_Pivot'];grip=bpy.data.objects['LidGrip_Anchor']
body=next(o for o in pot.children if re.sub(r'\.\d+$','',o.name)=='Pot_Body')
rimtop=max((body.matrix_world@v.co).z for v in body.data.vertices)
lidlow=min((o.matrix_world@v.co).z for o in lid.children if o.type=='MESH' for v in o.data.vertices)
correction=rimtop-lidlow+.00015;lid.location.z+=correction;grip.location.z+=correction
print('LID_SEATING_CORRECTION_M',correction)
manifest={}
for r in roots:
 bpy.context.view_layer.update();objects=descendants(r);vs=[o.matrix_world@v.co for o in objects if o.type=='MESH' for v in o.data.vertices]
 low=[min(v[k] for v in vs) for k in range(3)];high=[max(v[k] for v in vs) for k in range(3)]
 data={'bounds_min_m':low,'bounds_max_m':high,'dimensions_xyz_m':[high[k]-low[k] for k in range(3)],'triangles':0,'materials':sorted({m.name for o in objects if o.type=='MESH' for m in o.data.materials}),'hierarchy':[]}
 for o in objects:
  if o.type=='MESH':o.data.calc_loop_triangles();data['triangles']+=len(o.data.loop_triangles)
  data['hierarchy'].append({'name':o['export_name'],'parent':o.parent.get('export_name',o.parent.name) if o.parent else None,'type':o.type,'world_position_m':list(o.matrix_world.translation),'local_position_m':list(o.location),'rotation_radians':list(o.rotation_euler),'scale':list(o.scale)})
 manifest[r.name]=data;export(r)
(OUT/'asset_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
rest=bpy.data.objects['PotRest_Anchor']
render('04_CookingPot_Closed',[pot],(.88,-1.12,.88),(0,0,.20))
closed=lid.location.copy();lid.location.z=closed.z+.222
render('05_CookingPot_Lifted',[pot],(.88,-1.12,1.03),(0,0,.27),52)
lid.location=(.57,.06,.08);lid.rotation_euler=(.12,-.16,.12);bpy.context.view_layer.update()
minimum=min((o.matrix_world@v.co).z for o in lid.children for v in o.data.vertices);lid.location.z-=minimum
render('06_CookingPot_LidAside',[pot],(1.1,-1.44,1.14),(.2,0,.2),53)
render('07_CookedRice_Closeup',[pot],(.41,-.53,.96),(0,0,.24),63)
lid.location=closed;lid.rotation_euler=(0,0,0)
pot.location.z=rest.location.z;render('10_Stove_PotFit',[stove,pot],(1.12,-1.65,1.25),(0,0,.4),52)
woods=[bpy.data.objects[f'SM_Firewood_{i:02d}'] for i in range(1,6)]
for i,w in enumerate(woods):w.location=((i-2)*.115,-.69,0)
render('15_Firewood_Outside',[stove,pot,*woods],(1.0,-1.6,1.35),(0,-.16,.3),50)
for i,w in enumerate(woods):w.location=(((i-1)*.085,-.32,.095) if i<3 else ((i-3.5)*.10,-.70,0))
render('16_Firewood_InHearth',[stove,pot,*woods],(.79,-1.5,.88),(0,-.14,.30),55)
woods[1].location.y-=.18
render('17_Firewood_PartWithdrawn',[stove,pot,*woods],(.72,-1.38,.72),(0,-.20,.24),57)
for r,(loc,rot) in layout.items():r.location=loc;r.rotation_euler=rot
bpy.data.objects['SM_BlowTube'].location=(-1.02,-.15,0)
render('00_KitchenAssets_Overview',roots,(2.05,-2.9,2.5),(0,.1,.3),55)
for o in scene.objects:o.hide_render=False
save('10_SeatingAndInteractionReviewed')
for file in Path('E:/BLENDER').glob('kitchen_*.py'):shutil.copy2(file,OUT/'Scripts'/file.name)
