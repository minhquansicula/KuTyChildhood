exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
import bmesh,re
from mathutils import Matrix,Euler
manifest=json.loads((OUT/'asset_manifest.json').read_text())
def norm(n):return re.sub(r'\.\d+$','',n)
original_scene=bpy.context.window.scene
report={'blender_version':bpy.app.version_string,'tolerance_m':.00002,'fbx_roundtrip':{},'mesh_audit':{},'texture_links':{},'failures':[]}
for rname,expected in manifest.items():
 r=bpy.data.objects[rname]
 for o in descendants(r):
  if any(s<=0 for s in o.scale):report['failures'].append(f'Non-positive scale: {o.name}')
  if o.type!='MESH':continue
  bm=bmesh.new();bm.from_mesh(o.data)
  boundary=sum(e.is_boundary for e in bm.edges);nonmanifold=sum(not e.is_manifold and not e.is_boundary for e in bm.edges)
  inconsistent=sum(e.is_manifold and not e.is_contiguous for e in bm.edges)
  degenerate=sum(f.calc_area()<1e-14 for f in bm.faces)
  duplicates=len(bm.faces)-len({tuple(sorted(v.index for v in f.verts)) for f in bm.faces})
  volume=bm.calc_volume(signed=True)
  report['mesh_audit'][rname+'/'+norm(o.name)]={'vertices':len(bm.verts),'boundary_edges':boundary,'nonmanifold_edges':nonmanifold,'inconsistent_normals':inconsistent,'degenerate_faces':degenerate,'duplicate_faces':duplicates,'signed_volume_m3':volume,'uv_layers':len(o.data.uv_layers)}
  if nonmanifold or inconsistent or degenerate or duplicates or not o.data.uv_layers:report['failures'].append(f'Mesh integrity: {o.name}')
  if boundary and norm(o.name)!='Water_Surface':report['failures'].append(f'Unexpected open mesh: {o.name}')
  if not boundary and volume<0:report['failures'].append(f'Inverted volume: {o.name}')
  bm.free()
for im in bpy.data.images:
 if im.source=='FILE' and im.name.startswith('M_'):
  path=bpy.path.abspath(im.filepath);exists=os.path.isfile(path);report['texture_links'][im.name]={'path':im.filepath,'exists':exists,'size':list(im.size)}
  if not exists:report['failures'].append('Missing image: '+im.name)

for name,expected in manifest.items():
 test=bpy.data.scenes.new('VALIDATE_'+name);test.unit_settings.system='METRIC';test.unit_settings.scale_length=1;bpy.context.window.scene=test
 before=set(bpy.data.objects);before_meshes=set(bpy.data.meshes);before_mats=set(bpy.data.materials);before_images=set(bpy.data.images)
 bpy.ops.import_scene.fbx(filepath=str(OUT/'Exports'/(name+'.fbx')),use_anim=False)
 imported=set(bpy.data.objects)-before;bpy.context.view_layer.update()
 rootlist=[o for o in imported if o.parent is None];meshes=[o for o in imported if o.type=='MESH'];vs=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 low=[min(v[k] for v in vs) for k in range(3)];high=[max(v[k] for v in vs) for k in range(3)];dims=[high[k]-low[k] for k in range(3)]
 wanted={(d['name'],d['parent']) for d in expected['hierarchy']};got={(norm(o.name),norm(o.parent.name) if o.parent else None) for o in imported}
 maxtransform=0;maxrotation=0;maxscale=0;details=[];expected_by_name={e['name']:e for e in expected['hierarchy']};expected_matrices={}
 def expected_matrix(name):
  if name in expected_matrices:return expected_matrices[name]
  e=expected_by_name[name];mat=Matrix.LocRotScale(Vector(e['local_position_m']),Euler(e['rotation_radians']).to_quaternion(),Vector(e['scale']))
  if e['parent']:mat=expected_matrix(e['parent'])@mat
  expected_matrices[name]=mat;return mat
 for e in expected['hierarchy']:
  match=next((o for o in imported if norm(o.name)==e['name']),None)
  if match:
   err=(match.matrix_world.translation-Vector(e['world_position_m'])).length;maxtransform=max(maxtransform,err)
   wanted_matrix=expected_matrix(e['name']);angle=match.matrix_world.to_quaternion().rotation_difference(wanted_matrix.to_quaternion()).angle
   angle=min(angle,abs(math.tau-angle));maxrotation=max(maxrotation,angle)
   maxscale=max(maxscale,(match.matrix_world.to_scale()-wanted_matrix.to_scale()).length)
   if err>report['tolerance_m']:details.append((e['name'],err))
 triangles=0
 for o in meshes:o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
 maxsize=max(abs(dims[k]-expected['dimensions_xyz_m'][k]) for k in range(3))
 rootzero=max((o.matrix_world.translation.length for o in rootlist),default=999)
 success=(wanted==got and maxsize<report['tolerance_m'] and maxtransform<report['tolerance_m'] and maxrotation<.0002 and maxscale<.00002 and rootzero<report['tolerance_m'] and triangles==expected['triangles'] and all(o.type in {'MESH','EMPTY'} for o in imported))
 report['fbx_roundtrip'][name]={'pass':success,'object_count':len(imported),'hierarchy_matches':wanted==got,'dimension_error_m':maxsize,'dimensions_m':dims,'max_origin_anchor_error_m':maxtransform,'max_orientation_error_radians':maxrotation,'max_scale_error':maxscale,'root_distance_from_zero_m':rootzero,'triangles':triangles,'triangle_count_matches':triangles==expected['triangles'],'expected_missing':list(wanted-got),'unexpected':list(got-wanted),'transform_mismatches':details,'no_camera_lights':all(o.type in {'MESH','EMPTY'} for o in imported)}
 if not success:report['failures'].append('FBX roundtrip: '+name)
 for o in imported:bpy.data.objects.remove(o,do_unlink=True)
 for meshdata in set(bpy.data.meshes)-before_meshes:bpy.data.meshes.remove(meshdata)
 for matdata in set(bpy.data.materials)-before_mats:bpy.data.materials.remove(matdata)
 for imdata in set(bpy.data.images)-before_images:bpy.data.images.remove(imdata)
 bpy.context.window.scene=original_scene;bpy.data.scenes.remove(test)
report['pass']=not report['failures']
(OUT/'Validation.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps({'pass':report['pass'],'failures':report['failures'],'roundtrip':report['fbx_roundtrip']},indent=2))
