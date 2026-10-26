exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
import bmesh, shutil
# Final material response: smooth blend of metallic/roughness out of the soot band;
# planed wood is shallower than bark, retaining a tactile but handled surface.
for name,color,rough,metal,size,kind in [('M_Aluminum_Soot',(.045,.041,.032),.84,.24,2048,'soot'),('M_DryWood',(.43,.265,.115),.84,0,1024,'wood'),('M_DarkWood',(.17,.095,.038),.76,0,1024,'wood')]:
 old=bpy.data.materials[name];slots=[(o,i) for o in scene.objects if o.type=='MESH' for i,m in enumerate(o.data.materials) if m==old]
 bpy.data.materials.remove(old,do_unlink=True)
 for im in list(bpy.data.images):
  if im.name.startswith(name+'_'):bpy.data.images.remove(im)
 new=texmat(name,color,rough,metal,size,kind)
 for o,i in slots:o.data.materials[i]=new

roots=sorted([o for o in scene.objects if o.parent is None and o.name.startswith('SM_')],key=lambda o:o.name)
cleanup={}
for r in roots:
 r.location=(0,0,0)
 for o in descendants(r):
  if o.type!='MESH':continue
  before=len(o.data.vertices);bm=bmesh.new();bm.from_mesh(o.data)
  bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0000002)
  bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.0000001)
  bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
  if o.name=='Water_Surface':
   for f in bm.faces:
    if f.normal.z<0:f.normal_flip()
  bm.to_mesh(o.data);bm.free();o.data.update()
  cleanup[o.name]={'welded_vertices':before-len(o.data.vertices)}
 bpy.context.view_layer.update()
 coords=[o.matrix_world@v.co for o in descendants(r) if o.type=='MESH' for v in o.data.vertices]
 minimum=min(p.z for p in coords)
 for child in r.children:child.location.z-=minimum
 r['ground_correction_m']=minimum
 # Make asset-level collections while preserving parent relationships.
 collection=bpy.data.collections.get(r.name) or bpy.data.collections.new(r.name)
 if collection.name not in scene.collection.children:scene.collection.children.link(collection)
 for o in descendants(r):
  for col in list(o.users_collection):col.objects.unlink(o)
  collection.objects.link(o)
  o['export_name']=o.name.split('.')[0]
 bpy.context.view_layer.update()

stove=bpy.data.objects['SM_WoodStove'];pot=bpy.data.objects['SM_CookingPot'];wash=bpy.data.objects['SM_RiceWashingPot']
rest=bpy.data.objects['PotRest_Anchor'];top=bpy.data.objects['Stove_Top'];support_top=max((top.matrix_world@v.co).z for v in top.data.vertices)
rest.location.z=support_top-.0018
# Dark inside edge of crown, grounded and supported by the masonry below.
if bpy.data.materials['M_Charcoal'] not in list(top.data.materials):top.data.materials.append(bpy.data.materials['M_Charcoal'])
charindex=list(top.data.materials).index(bpy.data.materials['M_Charcoal'])
for p in top.data.polygons:
 c=p.center
 if .215<math.sqrt(c.x*c.x+(c.y/.91)**2)<.24 and c.z>.35:p.material_index=charindex

# Generate URP Lit's actual metallic/smoothness packing in addition to requested channels.
for m in bpy.data.materials:
 if not m.name.startswith('M_'):continue
 mi=bpy.data.images.get(m.name+'_Metallic');ro=bpy.data.images.get(m.name+'_Roughness')
 if not mi or not ro:continue
 size=mi.size[0];a=np.empty(size*size*4,dtype=np.float32);b=np.empty_like(a)
 mi.pixels.foreach_get(a);ro.pixels.foreach_get(b);a=a.reshape(size,size,4);b=b.reshape(size,size,4)
 a[:,:,3]=1-b[:,:,0]
 packed=bpy.data.images.new(m.name+'_MetallicSmoothness',width=size,height=size,alpha=True);packed.colorspace_settings.name='Non-Color';packed.pixels.foreach_set(a.ravel());packed.filepath_raw=str(OUT/'Textures'/(packed.name+'.png'));packed.file_format='PNG';packed.save()

# Record at zero, before making the presentation layout.
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
(OUT/'mesh_cleanup.json').write_text(json.dumps(cleanup,indent=2),encoding='utf8')
save('08_FinalMeshes')

# Interaction evidence: states use the actual asset meshes, not demonstration duplicates.
render('01_WashingPot_Upright',[wash]);tilt=bpy.data.objects['Tilt_Pivot'];tilt.rotation_euler.x=math.radians(23)
render('02_WashingPot_Tilt23',[wash],(.85,-1.05,.83));tilt.rotation_euler.x=0
water=bpy.data.objects['Water_Surface'];render('03_WashingPot_Interior',[wash],(.43,-.55,.94),(0,0,.12),62,exclude=[water]);water.hide_render=False
render('04_CookingPot_Closed',[pot],(.88,-1.12,.88),(0,0,.20))
lid=bpy.data.objects['Lid_Pivot'];closed=lid.location.copy();lid.location.z=closed.z+.222
render('05_CookingPot_Lifted',[pot],(.88,-1.12,1.03),(0,0,.27),52)
lid.location=(.57,.06,.08);lid.rotation_euler=(.12,-.16,.12);bpy.context.view_layer.update()
minimum=min((o.matrix_world@v.co).z for o in lid.children for v in o.data.vertices);lid.location.z-=minimum
render('06_CookingPot_LidAside',[pot],(1.1,-1.44,1.14),(.2,0,.2),53)
render('07_CookedRice_Closeup',[pot],(.41,-.53,.96),(0,0,.24),63)
lid.location=closed;lid.rotation_euler=(0,0,0)
render('08_Stove_Front',[stove],(1.05,-1.65,1.0),(0,0,.23),56)
render('09_Stove_PlayerView',[stove],(.42,-1.28,1.33),(0,0,.23),47)
pot.location.z=rest.location.z;render('10_Stove_PotFit',[stove,pot],(1.12,-1.65,1.25),(0,0,.4),52)
woods=[bpy.data.objects[f'SM_Firewood_{i:02d}'] for i in range(1,6)]
for i,w in enumerate(woods):w.location=((i-2)*.115,-.69,.0)
render('15_Firewood_Outside',[stove,pot,*woods],(1.0,-1.6,1.35),(0,-.16,.3),50)
for i,w in enumerate(woods):
 if i<3:w.location=((i-1)*.085,-.205,.095)
 else:w.location=((i-3.5)*.10,-.70,0)
render('16_Firewood_InHearth',[stove,pot,*woods],(.79,-1.5,.88),(0,-.14,.30),55)
woods[1].location.y-=.18
render('17_Firewood_PartWithdrawn',[stove,pot,*woods],(.72,-1.38,.72),(0,-.20,.24),57)
for w in woods:w.location=(0,0,0)
pot.location=(0,0,0)
render('12_RicePaddle',[bpy.data.objects['SM_RicePaddle']],(.24,-.36,.29),(0,0,.008),55)
render('18_Rim_Handles_Closeup',[wash],(-.54,-.61,.59),(-.13,0,.22),72,exclude=[water]);water.hide_render=False
save('08_FinalInteractionChecks')

# A readable final scene; export() always resets each root to zero temporarily.
stove.location=(-.44,.23,0);pot.location=(-.44,.23,rest.location.z)
wash.location=(.48,-.02,0);bpy.data.objects['SM_BambooTray'].location=(.38,.60,0)
bpy.data.objects['SM_RicePaddle'].location=(.79,-.12,0)
bpy.data.objects['SM_BlowTube'].location=(-.92,-.08,0)
for i,w in enumerate(woods):w.location=(-.57+i*.12,-.50,0);w.rotation_euler.z=(i-2)*.09
render('00_KitchenAssets_Overview',roots,(2.05,-2.9,2.5),(0,.1,.3),55)
for o in scene.objects:o.hide_render=False
bpy.ops.object.select_all(action='DESELECT');stove.select_set(True);bpy.context.view_layer.objects.active=stove
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  area.spaces.active.region_3d.view_distance=3.1;area.spaces.active.region_3d.view_location=(0,.1,.3)
save('09_AllAssetsComplete')
for file in Path('E:/BLENDER').glob('kitchen_*.py'):shutil.copy2(file,OUT/'Scripts'/file.name)
print('FINAL_MANIFEST',json.dumps({k:(v['triangles'],v['dimensions_xyz_m']) for k,v in manifest.items()}))
