"""Incremental edits on the existing skinned meshes. Originals are independently backed up."""
import bpy,bmesh,math,json,numpy as np
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
assert bpy.data.collections.get('BACKUP_Mother_Original'), 'A preservation backup is required.'
assert bpy.data.filepath.endswith('Mother_AoBaBa_Review.blend')
mapping={'Mother_SkinMesh':'Mother_Body','Mother_BlouseMesh':'Mother_Shirt','Mother_TrousersMesh':'Mother_Pants','Mother_HairMesh':'Mother_Hair','Mother_EyesMesh':'Mother_Eyes','Mother_SandalsMesh':'Mother_Footwear'}
for old,new in mapping.items():
 if bpy.data.objects.get(new):raise RuntimeError('Revision already appears applied: '+new)
 bpy.data.objects[old].name=new
shirt=bpy.data.objects['Mother_Shirt'];pants=bpy.data.objects['Mother_Pants'];body=bpy.data.objects['Mother_Body'];hair=bpy.data.objects['Mother_Hair'];rig=bpy.data.objects['Mother_Deform']

# Safe local seam construction before adding corrective shape keys. BMesh carries
# existing UV/deform layers through the bisects and edge splits.
bm=bmesh.new();bm.from_mesh(shirt.data)
side_faces=[f for f in bm.faces if .79<f.calc_center_median().z<.91 and abs(f.calc_center_median().x)>.135]
if side_faces:
 geometry=set(side_faces)
 for f in side_faces:geometry.update(f.edges);geometry.update(f.verts)
 bmesh.ops.bisect_plane(bm,geom=list(geometry),dist=.000001,plane_co=(0,0,.885),plane_no=(0,0,1),clear_inner=False,clear_outer=False)
 side_faces=[f for f in bm.faces if f.calc_center_median().z<.88499 and abs(f.calc_center_median().x)>.135]
 geometry=set(side_faces)
 for f in side_faces:geometry.update(f.edges);geometry.update(f.verts)
 bmesh.ops.bisect_plane(bm,geom=list(geometry),dist=.000001,plane_co=(0,-.02,0),plane_no=(0,1,0),clear_inner=False,clear_outer=False)
 slit_edges=[e for e in bm.edges if all(abs(v.co.y+.02)<.00001 and abs(v.co.x)>.135 and v.co.z<.88501 for v in e.verts) and min(v.co.z for v in e.verts)<.884]
 if not slit_edges:raise RuntimeError('No side-slit edges found; stopped instead of claiming success.')
 bmesh.ops.split_edges(bm,edges=slit_edges)
 for v in bm.verts:
  if abs(v.co.y+.02)<.00001 and abs(v.co.x)>.135 and v.co.z<.885:
   mean=sum(f.calc_center_median().y for f in v.link_faces)/max(1,len(v.link_faces))
   v.co.y+=(-1 if mean<-.02 else 1)*.0035*max(0,min(1,(.885-v.co.z)/.075))
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(shirt.data);bm.free()

def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def new_key(obj,name):
 if not obj.data.shape_keys:obj.shape_key_add(name='Basis')
 key=obj.shape_key_add(name=name);key.value=1;return key

shirt_key=new_key(shirt,'AoBaBa_LooseCut_MinimalNeck')
names={g.index:g.name for g in shirt.vertex_groups}
for v in shirt.data.vertices:
 p=v.co.copy();x,y,z=p
 arm=sum(g.weight for g in v.groups if names.get(g.group,'').startswith(('UpperArm','LowerArm')))
 if arm<.25 and abs(x)<.245:
  # Modest shaping with more ease at the waist and a less flared hip-length hem.
  waist=math.exp(-((z-1.065)/.095)**2)
  p.x*=1+.095*waist
  if z<.94:p.x*=1-.045*smooth((.94-z)/.12)
  if z<.89:p.z-=.009*smooth((.89-z)/.08)
  if y<-.04:
   p.y-=.0035*waist
  else:p.y+=.0025*waist
  if z>1.312 and abs(x)<.10:
   t=smooth((z-1.312)/.016);target=1.322+(z-1.318)*.08
   p.z=p.z*(1-t)+target*t
   if y<-.065:p.z-=.015*math.exp(-(x/.024)**2)*t
  # Very small cloth ripples, not anatomical skin detail.
  fold=.0012*math.sin(z*76+x*21)*math.exp(-((z-1.015)/.075)**2)
  if abs(y)>.04:p.y+=(-1 if y<0 else 1)*fold
  if z<.93:
   a=math.atan2(y+.02,x);p.y+=.0010*math.sin(a*7)*math.exp(-((z-.84)/.06)**2)
 elif arm>.25:
  side='L' if x>0 else 'R';bone=rig.data.bones['LowerArm.'+side]
  direction=bone.tail_local-bone.head_local;t=(p-bone.head_local).dot(direction)/direction.length_squared
  c=bone.head_local+direction*t;radial=p-c
  if radial.length:
   ripple=.0014*math.sin(t*math.tau*5)*math.exp(-((t-.06)/.22)**2)
   p+=radial.normalized()*ripple
 shirt_key.data[v.index].co=p
shirt['design']='Ao ba ba inspired: minimal round/V front neck, small central buttons, eased waist, hip-length hem, real side slits.'

# Re-cut the current trouser silhouette with a reversible key, retaining the original
# mesh, UVs, groups and Armature modifier. The cross sections stay loose and straight.
pants_key=new_key(pants,'LooseStraight_FabricVolume')
coords=np.array([v.co[:] for v in pants.data.vertices]);grid=np.linspace(.08,.82,38);stats={}
for sign in [-1,1]:
 array=coords[coords[:,0]*sign>0];rows=[]
 for z in grid:
  chosen=array[np.abs(array[:,2]-z)<.035]
  if len(chosen)<8:chosen=array[np.argsort(np.abs(array[:,2]-z))[:16]]
  mn=chosen[:,:2].min(axis=0);mx=chosen[:,:2].max(axis=0)
  rows.append(((mn+mx)/2,np.maximum((mx-mn)/2,[.012,.016])))
 stats[sign]=rows
def leg_center(z,side):
 points=[rig.data.bones[n+'.'+side].head_local for n in ['UpperLeg','LowerLeg','Foot']]
 for a,b in zip(points,points[1:]):
  if z>=b.z:return a.lerp(b,max(0,min(1,(a.z-z)/(a.z-b.z))))
 return points[-1].copy()
for v in pants.data.vertices:
 p=v.co.copy();sign=1 if p.x>0 else -1;z=p.z
 if z<.84:
  index=max(0,min(len(grid)-2,int(np.searchsorted(grid,z))-1));blend=max(0,min(1,(z-grid[index])/(grid[index+1]-grid[index])))
  ca,ra=stats[sign][index];cb,rb=stats[sign][index+1];c=ca*(1-blend)+cb*blend;r=ra*(1-blend)+rb*blend
  nx=(p.x-c[0])/r[0];ny=(p.y-c[1])/r[1]
  rx=float(np.interp(z,[.083,.28,.48,.66,.80],[.040,.043,.046,.057,.066]))
  ry=float(np.interp(z,[.083,.28,.48,.66,.80],[.052,.056,.059,.074,.080]))
  center=leg_center(z,'L' if sign>0 else 'R')
  target=Vector((center.x+nx*rx,center.y-.005+ny*ry,z))
  angle=math.atan2(ny,nx)
  fold=.0014*math.sin(z*83+angle*1.8)*(math.exp(-((z-.48)/.07)**2)+.7*math.exp(-((z-.16)/.045)**2))
  target.x+=math.cos(angle)*fold;target.y+=math.sin(angle)*fold
  ease=smooth((.84-z)/.12);p=p.lerp(target,ease)
  if z<.115:p.z+=.004*smooth((.115-z)/.032)
 pants_key.data[v.index].co=p
pants['design']='Loose straight black fabric trousers; eased thigh/knee/ankle sections; ankle-length cloth hems.'

def srgb_channel(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
def palette(material_name,hex_color):
 mat=bpy.data.materials[material_name];color=tuple(srgb_channel(int(hex_color[i:i+2],16)/255) for i in (0,2,4))
 mat.diffuse_color=(*color,1);tree=mat.node_tree;p=tree.nodes.get('Principled BSDF')
 existing=list(p.inputs['Base Color'].links);source=existing[0].from_socket if existing else None
 mix=tree.nodes.new('ShaderNodeMixRGB');mix.name='AoBaBa_FadedPalette';mix.label='#'+hex_color+' / original texture retained';mix.blend_type='MIX';mix.inputs[0].default_value=.035;mix.inputs[1].default_value=(*color,1);mix.location=(-220,320)
 if source:tree.links.new(source,mix.inputs[2])
 else:mix.inputs[2].default_value=(*color,1)
 tree.links.new(mix.outputs[0],p.inputs['Base Color'])
 old=list(p.inputs['Roughness'].links);source=old[0].from_socket if old else None
 rough=tree.nodes.new('ShaderNodeMath');rough.operation='MAXIMUM';rough.name='AoBaBa_MatteCloth';rough.inputs[1].default_value=.88;rough.location=(-200,-30)
 if source:tree.links.new(source,rough.inputs[0])
 else:rough.inputs[0].default_value=.90
 tree.links.new(rough.outputs[0],p.inputs['Roughness'])
 p.inputs['Specular IOR Level'].default_value=.18
 for node in tree.nodes:
  if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.08
 mat['review_palette_srgb']='#'+hex_color
palette('Mother_Blouse','795A48');palette('Mother_Trousers','252525')

# Preserve the existing facial identity, UV skin and expression keys. New age cues
# are a low-amplitude corrective layer (sub-millimetre), not a texture replacement.
age=new_key(body,'MiddleAge_GentleRest')
for v in body.data.vertices:
 x,y,z=v.co;ax=abs(x)
 if not 1.375<z<1.52 or y>-.08:continue
 front=smooth((-y-.08)/.04)
 line_x=.019+.42*max(0,1.452-z)
 smile_fold=math.exp(-((ax-line_x)/.0035)**2-((z-1.430)/.023)**2)*front
 eye_fold=math.exp(-((ax-.046)/.009)**2-((z-1.466)/.006)**2)*front
 age.data[v.index].co.y+=.00042*smile_fold+.00030*eye_fold
 age.data[v.index].co.z-=.00045*math.exp(-((ax-.036)/.02)**2-((z-1.455)/.016)**2)*front
body.data.shape_keys.key_blocks['WarmSmile'].value=.55
body.data.shape_keys.key_blocks['Concern'].value=0
body['review_face']='Preserved face and skin tone; calm WarmSmile 0.55; reversible gentle age folds, no high-frequency skin replacement.'

# Flatten/relax only the existing bun; keep the fitted scalp and eyebrow topology.
hair_key=new_key(hair,'LowBun_OrganicNape')
bm=bmesh.new();bm.from_mesh(hair.data);left=set(bm.verts);bun_ids=[]
while left:
 stack=[left.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if n in left:left.remove(n);stack.append(n)
 c=sum((v.co for v in part),Vector())/len(part)
 if 150<len(part)<600 and c.y>.025 and 1.40<c.z<1.49:
  for v in part:
   p=v.co-c;angle=math.atan2(p.z,p.x);organic=1+.025*math.sin(3*angle)+.015*math.cos(5*angle)
   hair_key.data[v.index].co=Vector((c.x+p.x*.98*organic,c.y-.006+p.y*.72,c.z-.003+p.z*.86*organic));bun_ids.append(v.index)
bm.free()
if not bun_ids:raise RuntimeError('Existing bun island was not identified; no bun change claimed.')
hair['design']='Existing low bun flattened in depth and height; restrained asymmetry; gathered close to nape. Scalp/bun overlap is intentional.'

# New, independently keyed review action. No original Action or bone rest transform
# is edited, replaced, deleted or renamed.
source=Path('E:/BLENDER/mother_05_animation.py').read_text(encoding='utf8').split('clips=')[0]
source=source.replace('sign*.225,-.100,.865','sign*.210,-.085,.835')
source=source.replace('curl=.10+.38*s','curl=.16+.32*s')
source=source.replace('angle=curl*',"angle=curl*({'Index':.8,'Middle':1.,'Ring':1.15,'Little':1.25}.get(finger,1.))*")
exec(compile(source,'review_pose_functions','exec'))
rig=bpy.data.objects['Mother_Deform'];rig.animation_data_create()
action=bpy.data.actions.new('Mother_AoBaBa_Relaxed_Idle');action.use_fake_user=True;rig.animation_data.action=action
previous={}
for frame in range(1,152):
 matrices=pose('Mother_Idle_Standing',(frame-1)/150)
 for bone in order:
  name=bone.name;parent=bone.parent
  reference=matrices[parent.name]@REST[parent.name].inverted()@REST[name] if parent else REST[name]
  location,rotation,scale=(reference.inverted()@matrices[name]).decompose()
  if name in previous and rotation.dot(previous[name])<0:rotation.negate()
  previous[name]=rotation.copy();pb=rig.pose.bones[name];pb.location=location;pb.rotation_quaternion=rotation;pb.scale=(1,1,1)
  for channel in ['location','rotation_quaternion','scale']:pb.keyframe_insert(channel,frame=frame,group=name)
for layer in action.layers:
 for strip in layer.strips:
  for bag in strip.channelbags:
   for fc in bag.fcurves:
    for point in fc.keyframe_points:point.interpolation='LINEAR'
bpy.data.objects['Mother_Control'].hide_set(True)
bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=151;bpy.context.scene.render.fps=30;bpy.context.scene.frame_set(1)
for obj in [body,shirt,pants,hair,bpy.data.objects['Mother_Eyes'],bpy.data.objects['Mother_Footwear']]:obj.hide_set(False)
rig.hide_set(True)
bpy.context.scene['mother_review_action']=action.name
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother_AoBaBa_Review.blend'))
print(json.dumps({'changed_meshes':list(mapping.values()),'new_action':action.name,'side_slit_edges_split':len(slit_edges),'bun_vertices_adjusted':len(bun_ids),'palette':['#795A48','#252525'],'method':'Local slit seam edits plus reversible shape keys and shader nodes; original actions and UV layers preserved; no FBX export.'}))
