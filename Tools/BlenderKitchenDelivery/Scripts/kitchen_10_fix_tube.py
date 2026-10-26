exec(compile(open('E:/BLENDER/kitchen_common.py',encoding='utf8').read(),'kitchen_common.py','exec'))
import bmesh,shutil
r=bpy.data.objects['SM_BlowTube'];oldloc=r.location.copy();r.location=(0,0,0);piv=next(o for o in r.children if o.name.startswith('Grip_Pivot'));o=bpy.data.objects['BlowTube_Mesh']
bm=bmesh.new();bm.from_mesh(o.data)
badnodes=[f for f in bm.faces if abs(f.calc_center_median().y-.072)<.002 or abs(f.calc_center_median().y-.240)<.002]
print('Replacing node faces',len(badnodes));bmesh.ops.delete(bm,geom=badnodes,context='FACES')
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(o.data);bm.free()
parts=[o];mat=bpy.data.materials['M_AgedBamboo']
for cy in [.072,.240]:
 vs=[];uv=[];faces=[];n=32;k=6
 for j in range(n):
  a=TAU*j/n
  for i in range(k):
   b=TAU*i/k;rad=.0182+.0016*math.cos(b);vs.append((rad*math.cos(a),cy+.0016*math.sin(b),rad*math.sin(a)));uv.append((j/n,i/k))
 for j in range(n):
  for i in range(k):faces.append((j*k+i,((j+1)%n)*k+i,((j+1)%n)*k+(i+1)%k,j*k+(i+1)%k))
 parts.append(mesh('Bamboo_Node_Fixed',vs,faces,mat,piv,uv))
o=combine(parts,'BlowTube_Mesh',piv);o['export_name']='BlowTube_Mesh'
bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update();bpy.context.view_layer.update()
minimum=min((o.matrix_world@v.co).z for v in o.data.vertices)
for child in r.children:child.location.z-=minimum
bpy.context.view_layer.update();objects=descendants(r);vs=[o.matrix_world@v.co for o in objects if o.type=='MESH' for v in o.data.vertices]
low=[min(v[k] for v in vs) for k in range(3)];high=[max(v[k] for v in vs) for k in range(3)]
data={'bounds_min_m':low,'bounds_max_m':high,'dimensions_xyz_m':[high[k]-low[k] for k in range(3)],'triangles':0,'materials':sorted({m.name for o in objects if o.type=='MESH' for m in o.data.materials}),'hierarchy':[]}
for ob in objects:
 if ob.type=='MESH':ob.data.calc_loop_triangles();data['triangles']+=len(ob.data.loop_triangles)
 data['hierarchy'].append({'name':ob['export_name'],'parent':ob.parent.get('export_name',ob.parent.name) if ob.parent else None,'type':ob.type,'world_position_m':list(ob.matrix_world.translation),'local_position_m':list(ob.location),'rotation_radians':list(ob.rotation_euler),'scale':list(ob.scale)})
manifest=json.loads((OUT/'asset_manifest.json').read_text());manifest[r.name]=data;(OUT/'asset_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
export(r);render('13_BlowTube',[r],(.19,-.42,.23),(0,0,.019),55);r.location=oldloc
roots=[ob for ob in scene.objects if ob.parent is None and ob.name.startswith('SM_')]
render('00_KitchenAssets_Overview',roots,(2.05,-2.9,2.5),(0,.1,.3),55)
for ob in scene.objects:ob.hide_render=False
save('11_TopologyValidatedTube')
for file in Path('E:/BLENDER').glob('kitchen_*.py'):shutil.copy2(file,OUT/'Scripts'/file.name)
