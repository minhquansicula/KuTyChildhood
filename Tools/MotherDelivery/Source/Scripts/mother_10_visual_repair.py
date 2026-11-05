import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Progress/06_BeforeVisualRepair.blend'),copy=True)
rig=bpy.data.objects['Mother_Deform']

def boundaries(bm):
 remaining={v for v in bm.verts if v.is_boundary};parts=[]
 while remaining:
  todo=[remaining.pop()];part=[]
  while todo:
   v=todo.pop();part.append(v)
   for e in v.link_edges:
    n=e.other_vert(v)
    if e.is_boundary and n in remaining:remaining.remove(n);todo.append(n)
  parts.append(part)
 return parts

shirt=bpy.data.objects['Mother_BlouseMesh']
bm=bmesh.new();bm.from_mesh(shirt.data)
deform=bm.verts.layers.deform.verify()
def group(name):
 g=shirt.vertex_groups.get(name) or shirt.vertex_groups.new(name=name)
 return g.index
def weight(v,name):
 v[deform].clear();v[deform][group(name)]=1.

for part in boundaries(bm):
 center=sum((v.co for v in part),Vector())/len(part)
 ids={v for v in part}
 edges=[e for e in bm.edges if e.is_boundary and all(v in ids for v in e.verts)]
 if center.z>1.25:
  # Explicit oval neckline and a continuous short standing collar.
  lifted={}
  for v in part:
   a=math.atan2((v.co.y+.038)/.082,v.co.x/.079)
   v.co=Vector((.077*math.cos(a),-.038+.082*math.sin(a),1.335))
   weight(v,'UpperChest')
   n=bm.verts.new((.067*math.cos(a),-.038+.072*math.sin(a),1.358))
   weight(n,'UpperChest');lifted[v]=n
  for e in edges:
   a,b=e.verts;bm.faces.new((a,b,lifted[b],lifted[a]))
 elif abs(center.x)>.3:
  side='L' if center.x>0 else 'R';bone=rig.data.bones['LowerArm.'+side]
  direction=(bone.tail_local-bone.head_local).normalized()
  center=bone.tail_local-direction*.009
  inner={}
  for v in part:
   delta=v.co-center;delta-=direction*delta.dot(direction)
   if delta.length<.00001:delta=direction.cross(Vector((0,1,0)))
   radial=delta.normalized()
   v.co=center+radial*.028
   weight(v,'LowerArm.'+side)
   n=bm.verts.new(v.co-direction*.007-radial*.0015)
   weight(n,'LowerArm.'+side);inner[v]=n
  for e in edges:
   a,b=e.verts;bm.faces.new((a,b,inner[b],inner[a]))

# Ease the tunic over the full waistband, including the previously intersecting back.
for v in bm.verts:
 if .78<v.co.z<1.08 and abs(v.co.x)<.25:
  f=max(0,min(1,(1.08-v.co.z)/.15))
  if v.co.y>.015:v.co.y+=.040*f
  v.co.x*=1+.16*f
  if v.co.z<.94:
   mix=max(0,min(1,(v.co.z-.82)/.12))*.35
   v[deform].clear();v[deform][group('Hips')]=1-mix
   if mix:v[deform][group('Spine')]=mix
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(shirt.data);bm.free()

# Bring the bun into the scalp rather than leaving a floating sphere.
hair=bpy.data.objects['Mother_HairMesh'];bm=bmesh.new();bm.from_mesh(hair.data)
remaining=set(bm.verts);components=[]
while remaining:
 todo=[remaining.pop()];part=[]
 while todo:
  v=todo.pop();part.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if n in remaining:remaining.remove(n);todo.append(n)
 components.append(part)
for part in components:
 c=sum((v.co for v in part),Vector())/len(part)
 if c.y>.04 and c.z<1.49:
  for v in part:v.co.y-=.045;v.co.z+=.035
bm.to_mesh(hair.data);bm.free()

# Flatten trouser ankle openings instead of leaving a saw-tooth cut.
pants=bpy.data.objects['Mother_TrousersMesh'];bm=bmesh.new();bm.from_mesh(pants.data)
for part in boundaries(bm):
 if max(v.co.z for v in part)<.15:
  for v in part:v.co.z=.083
bm.to_mesh(pants.data);bm.free()

# Fit the sole to the actual heel-to-toe extent, accounting for the joined object's origin.
shoe=bpy.data.objects['Mother_SandalsMesh']
for v in shoe.data.vertices:v.co.y-=.065

for name in ['Mother_Blouse','Mother_Trousers']:
 mat=bpy.data.materials[name]
 for n in mat.node_tree.nodes:
  if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.18
mat=bpy.data.materials['Mother_Hair']
for n in mat.node_tree.nodes:
 if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.08

bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother.blend'))
print('Visual repair applied: connected bun, explicit collar and cuff rings, rear waist clearance, flat trouser hems, sandal alignment.')
