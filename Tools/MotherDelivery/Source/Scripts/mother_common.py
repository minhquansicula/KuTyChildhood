import bpy, math, json, random, os, numpy as np
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion,Euler
from mathutils.kdtree import KDTree
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery');RES=OUT/'Source/Resources'
for d in ['Source','Textures','Preview','Exports/Animations','Source/Progress','Source/Scripts']:(OUT/d).mkdir(parents=True,exist_ok=True)
TAU=math.tau
S=bpy.context.scene
def active(o):
 bpy.ops.object.select_all(action='DESELECT');o.hide_set(False);o.select_set(True);bpy.context.view_layer.objects.active=o
def em(name,loc=(0,0,0),parent=None):
 o=bpy.data.objects.new(name,None);S.collection.objects.link(o);o.location=loc;o.parent=parent;o.empty_display_size=.025;return o
def mesh(name,vs,fs,mat=None,uvs=None):
 m=bpy.data.meshes.new(name);m.from_pydata(vs,[],fs);m.update();o=bpy.data.objects.new(name,m);S.collection.objects.link(o)
 if mat:m.materials.append(mat)
 layer=m.uv_layers.new(name='UVMap')
 for p in m.polygons:
  p.use_smooth=True
  for li in p.loop_indices:
   if uvs is not None:layer.data[li].uv=uvs[p.index][list(p.loop_indices).index(li)]
   else:
    v=m.vertices[m.loops[li].vertex_index].co;layer.data[li].uv=(v.x*3+.5,v.z*3)
 return o
def imagefile(name,pixels,space='sRGB'):
 h,w=pixels.shape[:2];im=bpy.data.images.new(name,width=w,height=h,alpha=pixels.shape[2]==4);im.colorspace_settings.name=space
 rgba=np.ones((h,w,4),dtype=np.float32);rgba[:,:,:pixels.shape[2]]=pixels
 im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/'Textures'/(name+'.png'));im.file_format='PNG';im.save();return im
def material(name,color,rough,kind='cloth',size=2048):
 if name in bpy.data.materials:return bpy.data.materials[name]
 rng=np.random.default_rng(sum(map(ord,name)));yy,xx=np.mgrid[0:size,0:size].astype(np.float32)/size
 fine=rng.random((size,size),dtype=np.float32)-.5
 noise=np.sin(xx*TAU*7+.7*np.sin(yy*TAU*3))*np.cos(yy*TAU*5)*.45+np.sin(xx*TAU*21+yy*TAU*11)*.15
 if kind=='cloth':
  h=(np.sin(xx*TAU*500)+np.sin(yy*TAU*500))*.018+fine*.012
  var=noise*.07+fine*.025;rr=rough+noise*.035
 elif kind=='hair':
  h=np.sin(xx*TAU*170+np.sin(yy*TAU*2)*2)*.025;var=noise*.1+fine*.015;rr=rough+noise*.05
 elif kind=='skin':h=fine*.008;var=noise*.025+fine*.01;rr=rough+noise*.025
 else:h=np.zeros_like(xx);var=noise*.02;rr=np.full_like(xx,rough)
 col=np.clip(np.array(color)[None,None,:]*(1+var[:,:,None]),.002,.95)
 dy,dx=np.gradient(h);strength=3 if kind=='skin' else 8;nn=np.stack((-dx*strength,-dy*strength,np.ones_like(xx)),axis=-1);nn/=np.linalg.norm(nn,axis=-1)[:,:,None];nn=nn*.5+.5
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1);p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=rough
 if kind=='skin':p.inputs['Subsurface Weight'].default_value=.045;p.inputs['Subsurface Radius'].default_value=(1,.4,.22)
 maps={'BaseColor':col,'Normal':nn,'Roughness':np.repeat(np.clip(rr,.1,1)[:,:,None],3,axis=2),'Metallic':np.zeros((size,size,3),dtype=np.float32),'MetallicSmoothness':np.concatenate((np.zeros((size,size,3)),(1-np.clip(rr,.1,1))[:,:,None]),axis=2)}
 for i,(suffix,data) in enumerate(maps.items()):
  im=imagefile(name+'_'+suffix,data,'sRGB' if suffix=='BaseColor' else 'Non-Color')
  if suffix=='MetallicSmoothness':continue
  n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;n.location=(-600,300-200*i)
  if suffix=='Normal':
   normal=m.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.65;m.node_tree.links.new(n.outputs['Color'],normal.inputs['Color']);m.node_tree.links.new(normal.outputs['Normal'],p.inputs['Normal'])
  else:m.node_tree.links.new(n.outputs['Color'],p.inputs[{'BaseColor':'Base Color','Roughness':'Roughness','Metallic':'Metallic'}[suffix]])
 return m
def solidmat(name,color,rough=.7):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1);p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;return m
def ellipsoid(name,loc,scale,mat,n=32,rings=16):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=n,ring_count=rings,radius=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
 for p in o.data.polygons:p.use_smooth=True
 return o
def cube(name,loc,dims,mat,bevel=.01):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=dims;active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
 if bevel:
  mod=o.modifiers.new('Rounded edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
 return o
def join(obs,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;return o
def save(stage):
 for im in bpy.data.images:
  if im.source=='FILE' and im.filepath:
   p=Path(bpy.path.abspath(im.filepath))
   if p.is_file():im.filepath=os.path.relpath(str(p),str(OUT/'Source')).replace('\\','/');im.filepath='//'+im.filepath
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother.blend'),relative_remap=False)
 paths={im:im.filepath for im in bpy.data.images if im.source=='FILE'}
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Progress'/(stage+'.blend')),copy=True)
 for im,path in paths.items():im.filepath=path
def render(name,eye=(2,-3.9,1.9),target=(0,0,.83),lens=65):
 cam=bpy.data.objects['Mother_Camera'];S.camera=cam;cam.location=eye;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=lens
 S.render.filepath=str(OUT/'Preview'/(name+'.png'));bpy.ops.render.render(write_still=True)
def studio():
 S.render.engine='CYCLES';S.cycles.samples=32;S.cycles.use_denoising=True;S.render.resolution_x=1000;S.render.resolution_y=1200;S.render.resolution_percentage=100;S.render.image_settings.file_format='PNG';S.view_settings.view_transform='AgX'
 S.world=bpy.data.worlds.new('Mother_StudioWorld');S.world.use_nodes=True;S.world.node_tree.nodes.get('Background').inputs[0].default_value=(.19,.22,.27,1);S.world.node_tree.nodes.get('Background').inputs[1].default_value=.35
 mat=solidmat('Preview_Background',(.095,.083,.069),.9);cube('Mother_StudioFloor',(0,0,-.027),(200,200,.05),mat,.001)
 bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='Mother_Camera';cam.data.clip_start=.005;cam.data.clip_end=300;S.camera=cam
 for name,loc,power,size,color in [('Key',(1,-2.3,3.1),260,2.1,(1,.86,.72)),('Fill',(-2,-1,2.0),170,2.0,(.79,.86,1)),('Rim',(.2,1.8,2.7),300,1.6,(1,.91,.76))]:
  bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name='Mother_'+name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.data.color=color;o.rotation_euler=(Vector((0,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()
