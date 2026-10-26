import bpy, math, random, json, os, numpy as np
from mathutils import Vector
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/BlenderKitchenDelivery')
for d in ['', 'Exports','Textures','Previews','Progress','Scripts']:(OUT/d).mkdir(parents=True,exist_ok=True)
random.seed(213)
TAU=math.tau
scene=bpy.context.scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.render.resolution_x=1100;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=scene.world or bpy.data.worlds.new('Kitchen_StudioWorld')
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.19,.22,.26,1)
scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.35
scene.view_settings.view_transform='AgX'

def empty(name,parent=None,loc=(0,0,0),size=.035):
 o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);o.empty_display_type='PLAIN_AXES';o.empty_display_size=size
 o.parent=parent;o.location=loc;return o

def mesh(name,verts,faces,mat=None,parent=None,uv=None):
 m=bpy.data.meshes.new(name+'_Geometry');m.from_pydata(verts,[],faces);m.update()
 o=bpy.data.objects.new(name,m);scene.collection.objects.link(o);o.parent=parent
 if mat:m.materials.append(mat)
 if uv:
  layer=m.uv_layers.new(name='UVMap')
  for poly in m.polygons:
   for li in poly.loop_indices:layer.data[li].uv=uv[m.loops[li].vertex_index]
 else:
  layer=m.uv_layers.new(name='UVMap')
  for p in m.polygons:
   n=p.normal; ax=max(range(3),key=lambda k:abs(n[k])); axes=[k for k in range(3) if k!=ax]
   for li in p.loop_indices:
    co=m.vertices[m.loops[li].vertex_index].co;layer.data[li].uv=(co[axes[0]]*2.5,co[axes[1]]*2.5)
 for p in m.polygons:p.use_smooth=True
 return o

def lathe(name,profile,mat,parent=None,n=96,asym=.0012):
 vs=[];uv=[]
 for j,(r,z) in enumerate(profile):
  for i in range(n+1):
   a=TAU*i/n;rr=r+asym*(math.sin(a*3+.4)+.4*math.cos(a*7))*min(1,r/.1)
   vs.append((rr*math.cos(a),rr*math.sin(a),z+asym*.3*math.sin(5*a)*min(1,r/.1)))
   uv.append((i/n,j/(len(profile)-1)))
 fs=[]
 for j in range(len(profile)-1):
  for i in range(n):
   a=j*(n+1)+i;fs.append((a,a+1,a+n+2,a+n+1))
 if profile[0][0]<.01:fs.append(tuple(reversed(range(n))))
 if profile[-1][0]<.01:fs.append(tuple((len(profile)-1)*(n+1)+i for i in range(n)))
 return mesh(name,vs,fs,mat,parent,uv)

def tube_path(name,pts,radius,mat,parent=None,sides=10,closed=False):
 pts=[Vector(p) for p in pts];vs=[];uv=[];N=len(pts)
 for j,p in enumerate(pts):
  tangent=pts[(j+1)%N]-pts[(j-1)%N] if closed else pts[min(N-1,j+1)]-pts[max(0,j-1)]
  tangent.normalize();ref=Vector((0,0,1)) if abs(tangent.z)<.95 else Vector((1,0,0))
  u=tangent.cross(ref).normalized();v=tangent.cross(u).normalized()
  for i in range(sides):
   a=TAU*i/sides;rr=radius if isinstance(radius,(float,int)) else radius[j]
   vs.append(tuple(p+rr*(math.cos(a)*u+math.sin(a)*v)));uv.append((i/sides,j/max(1,N-1)))
 fs=[]
 for j in range(N if closed else N-1):
  for i in range(sides):fs.append((j*sides+i,j*sides+(i+1)%sides,((j+1)%N)*sides+(i+1)%sides,((j+1)%N)*sides+i))
 if not closed:fs.extend([tuple(reversed(range(sides))),tuple((N-1)*sides+i for i in range(sides))])
 return mesh(name,vs,fs,mat,parent,uv)

def box(name,loc,scale,mat,parent=None,bevel=.01):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if mat:o.data.materials.append(mat)
 o.parent=parent
 if bevel:
  mod=o.modifiers.new('Soft hand-worn edges','BEVEL');mod.width=bevel;mod.segments=3
  bpy.ops.object.modifier_apply(modifier=mod.name)
  mod=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=mod.name)
 if not o.data.uv_layers:
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
 return o

def combine(objects,name,parent):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 world=o.matrix_world.copy();o.parent=parent;o.matrix_world=world;return o

def origin(o,p):
 world=Vector(p);local=o.matrix_world.inverted()@world
 for v in o.data.vertices:v.co-=local
 o.location+=o.matrix_basis.to_3x3()@local

def texmat(name,color,rough=.7,metal=0,size=1024,kind='noise'):
 if name in bpy.data.materials:return bpy.data.materials[name]
 # Analytic surface bake: periodic noise, grain/scratches and height gradients rasterized to UV images.
 rng=np.random.default_rng(sum(map(ord,name))); y,x=np.mgrid[0:size,0:size].astype(np.float32)/size
 fine=rng.random((size,size),dtype=np.float32)-.5
 noise=np.zeros_like(x)
 for f,a in [(3,.46),(9,.23),(27,.12),(81,.06)]:
  noise+=a*np.sin(TAU*(x*f+.14*np.sin(TAU*y*(f*.63)))+rng.random()*TAU)*np.cos(TAU*y*f*.83)
 h=noise*.35+fine*.035; variation=noise*.16+fine*.025
 if kind=='metal':
  scratches=np.maximum(0,np.sin(TAU*(y*630+x*3))-.94)*np.maximum(0,np.sin(TAU*x*11))
  h+=scratches*.16;variation+=scratches*.2
 elif kind=='wood':
  grain=np.sin(TAU*(x*42+noise*1.2+np.sin(TAU*y*2)*.25));h+=grain*.13;variation+=grain*.12
 elif kind=='endgrain':
  radial=np.sqrt((x-.5)**2+(y-.5)**2);rings=np.sin(TAU*(radial*19+noise*.12))
  h=rings*.075+fine*.025;variation=rings*.12+noise*.07
 elif kind=='bamboo':
  grain=np.sin(TAU*(x*71+noise*.7));h+=grain*.07;variation+=grain*.07
 elif kind=='weave':
  parity=(np.floor(x*36)+np.floor(y*36))%2
  a=np.cos(TAU*x*36);b=np.cos(TAU*y*36)
  h=np.where(parity==0,a,b)*.14+np.minimum(a,b)*.04
  variation=np.where(parity==0,a,b)*.12+noise*.12
 elif kind=='soot':variation-=np.maximum(0,noise)*.5;h+=fine*.06
 elif kind=='clay':h+=fine*.14;variation+=fine*.08
 elif kind=='rice':
  gx=x*54;gy=y*54;ix=np.floor(gx);iy=np.floor(gy)
  jitter=np.sin(ix*127.1+iy*311.7)*43758.5453;jitter-=np.floor(jitter)
  angle=jitter*TAU;xx=gx-ix-.5;yy=gy-iy-.5
  qx=xx*np.cos(angle)+yy*np.sin(angle);qy=-xx*np.sin(angle)+yy*np.cos(angle)
  q=(qx/.48)**2+(qy/.19)**2;grain=np.sqrt(np.maximum(0,1-q))
  h=grain*.35+noise*.025;variation=grain*.15-.07
 col=np.clip(np.array(color)[None,None,:]*(1+variation[:,:,None]),.003,.95)
 if kind=='soot' and 'Aluminum' in name:
  t=np.clip((y-.025+noise*.035)/.23,0,1);t=t*t*(3-2*t)
  col=col*(1-t[:,:,None])+np.array([.47,.49,.48])[None,None,:]*(1+variation[:,:,None]) * t[:,:,None]
 rr=np.clip(rough+noise*.09+fine*.035,.05,1)
 mm=np.full_like(x,metal)
 if kind=='soot' and 'Aluminum' in name:rr=rr*(1-t)+(.58+noise*.07)*t
 if kind=='soot' and 'Aluminum' in name:mm=metal*(1-t)+.78*t
 dy,dx=np.gradient(h); strength=18 if kind in ('metal','bamboo','endgrain') or name in ['M_DryWood','M_DarkWood'] else (size*.12 if kind=='rice' else (7 if kind=='soot' else 65))
 nx=-dx*strength;ny=-dy*strength;nz=np.ones_like(x);ln=np.sqrt(nx*nx+ny*ny+1)
 normal=np.stack((nx/ln*.5+.5,ny/ln*.5+.5,nz/ln*.5+.5),axis=-1)
 m=bpy.data.materials.new(name);m.use_nodes=True;nodes=m.node_tree.nodes;links=m.node_tree.links;p=nodes.get('Principled BSDF')
 for suffix,data,space in [('BaseColor',col,'sRGB'),('Roughness',rr,'Non-Color'),('Metallic',mm,'Non-Color'),('Normal',normal,'Non-Color')]:
  image=bpy.data.images.new(name+'_'+suffix,width=size,height=size,alpha=False)
  image.colorspace_settings.name=space
  rgba=np.ones((size,size,4),dtype=np.float32)
  if data.ndim==2:rgba[:,:,:3]=data[:,:,None]
  else:rgba[:,:,:3]=data
  image.pixels.foreach_set(rgba.ravel());image.filepath_raw=str(OUT/'Textures'/f'{name}_{suffix}.png');image.file_format='PNG';image.save()
  node=nodes.new('ShaderNodeTexImage');node.image=image;node.label=suffix;node.location=(-650,300-180*len(nodes))
  if suffix=='Normal':
   normalnode=nodes.new('ShaderNodeNormalMap');normalnode.inputs['Strength'].default_value=.7;links.new(node.outputs['Color'],normalnode.inputs['Color']);links.new(normalnode.outputs['Normal'],p.inputs['Normal'])
  else:links.new(node.outputs['Color'],p.inputs[{'BaseColor':'Base Color','Roughness':'Roughness','Metallic':'Metallic'}[suffix]])
 m.diffuse_color=(*color,1);return m

def simplemat(name,color,rough=.65,metal=0):
 if name in bpy.data.materials:return bpy.data.materials[name]
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal;m.diffuse_color=(*color,1);return m

def rice(name,parent,radius,z,count,mat,cooked=False):
 # Closed low mound, with a single joined mesh of elongated grains.
 base=lathe(name+'_Base',[(.001,z-.013),(radius*.9,z-.013),(radius,z-.004),(radius*.92,z+.002),(radius*.5,z+.006),(.001,z+.008)],mat,parent,n=64,asym=.0005)
 for loop in base.data.loops:
  co=base.data.vertices[loop.vertex_index].co;base.data.uv_layers.active.data[loop.index].uv=(co.x/(radius*2)+.5,co.y/(radius*2)+.5)
 vs=[];fs=[];uv=[]
 for k in range(count):
  a=random.random()*TAU;r=radius*math.sqrt(random.random())*.96;px=r*math.cos(a);py=r*math.sin(a);pz=z+.008*(1-(r/radius)**2)
  angle=random.random()*TAU;length=random.uniform(.0048,.0075)*(1.12 if cooked else 1);width=length*(.34 if cooked else .28);start=len(vs)
  local=[(-length/2,0,0),(length/2,0,0)]+[(0,width*.5*math.cos(TAU*i/4),width*.43*math.sin(TAU*i/4)) for i in range(4)]
  for xx,yy,zz in local:
   vs.append((px+math.cos(angle)*xx-math.sin(angle)*yy,py+math.sin(angle)*xx+math.cos(angle)*yy,pz+zz));uv.append((random.random(),random.random()))
  for i in range(4):
   fs.append((start,start+2+(i+1)%4,start+2+i));fs.append((start+1,start+2+i,start+2+(i+1)%4))
 grains=mesh(name+'_Surface',vs,fs,mat,parent,uv)
 return combine([base,grains],name,parent)

def handles(parent,height,mat,r=.23):
 result=[]
 for label,sgn in [('Left',-1),('Right',1)]:
  pts=[]
  for i in range(33):
   a=math.pi*i/32;pts.append((sgn*(r+.058*math.sin(a)),.055*math.cos(a),height+.012*math.sin(a)))
  h=tube_path('Handle_'+label,pts,.009,mat,parent,12)
  pads=[]
  for yy in [-.055,.055]:
   pads.append(box('Riveted socket',(sgn*(r-.002),yy,height),(.015,.029,.039),mat,parent,.006))
  result.append(combine([h]+pads,'Handle_'+label,parent))
 return result

def descendants(root):return [root]+list(root.children_recursive)
def show_only(roots):
 allowed={o for r in roots for o in descendants(r)}
 for o in scene.objects:
  if o.type=='MESH' and not o.name.startswith('Studio_'):o.hide_render=o not in allowed

def studio():
 if bpy.data.objects.get('Studio_Camera'):return
 floor=simplemat('Studio_Backdrop',(.09,.073,.056),.85)
 box('Studio_Floor',(0,0,-.035),(200,200,.05),floor,None,.005)
 bpy.ops.object.camera_add(location=(.82,-1.05,.88));cam=bpy.context.object;cam.name='Studio_Camera';scene.camera=cam;cam.data.lens=55;cam.data.clip_start=.005
 for name,loc,power,size,color in [('Key',(.1,-1.6,2.3),160,2,(1,.86,.69)),('Fill',(-1.6,-.2,1.2),95,1.7,(.74,.84,1)),('Rim',(.4,1.2,1.7),210,1.5,(1,.92,.78))]:
  bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name='Studio_'+name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.data.color=color;o.rotation_euler=(Vector((0,0,.2))-o.location).to_track_quat('-Z','Y').to_euler()
def render(name,roots,eye=(.8,-1.02,.86),target=(0,0,.15),lens=55,exclude=()):
 studio();show_only(roots);cam=scene.camera;cam.location=eye;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=lens
 for o in exclude:o.hide_render=True
 scene.render.filepath=str(OUT/'Previews'/f'{name}.png');bpy.ops.render.render(write_still=True)

def export(root):
 old=root.location.copy();oldrot=root.rotation_euler.copy();root.location=(0,0,0);root.rotation_euler=(0,0,0);bpy.context.view_layer.update()
 bpy.ops.object.select_all(action='DESELECT')
 for o in descendants(root):o.hide_set(False);o.select_set(True)
 bpy.context.view_layer.objects.active=root
 names={o:o.name for o in bpy.data.objects}
 for o in descendants(root):
  desired=o.get('export_name',o.name.split('.')[0]);conflict=bpy.data.objects.get(desired)
  if conflict and conflict!=o:conflict.name='__export_hold__'+conflict.name
  o.name=desired
 bpy.ops.export_scene.fbx(filepath=str(OUT/'Exports'/f'{root.name}.fbx'),use_selection=True,object_types={'MESH','EMPTY'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,mesh_smooth_type='FACE',use_triangles=True,use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE',embed_textures=False)
 for o in names:o.name='__restore__'+str(o.as_pointer())
 for o,name in names.items():o.name=name
 root.location=old;root.rotation_euler=oldrot;bpy.context.view_layer.update()

def save(stage):
 for image in bpy.data.images:
  if image.source=='FILE' and image.filepath and 'Textures' in image.filepath:image.filepath='//Textures/'+image.filepath.replace('\\','/').rsplit('/',1)[-1]
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'KitchenAssets.blend'))
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Progress'/f'{stage}.blend'),copy=True)
 stats={}
 for root in scene.objects:
  if root.parent is None and root.name.startswith('SM_'):
   obs=descendants(root);tri=0
   for o in obs:
    if o.type=='MESH':o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles)
   stats[root.name]={'triangles':tri,'objects':len(obs),'materials':sorted({m.name for o in obs if o.type=='MESH' for m in o.data.materials})}
 (OUT/'asset_stats.json').write_text(json.dumps(stats,indent=2),encoding='utf8');print(json.dumps(stats))
