import bpy,bmesh,math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')

def loops(bm):
 todo={v for v in bm.verts if v.is_boundary};result=[]
 while todo:
  stack=[todo.pop()];part=[]
  while stack:
   v=stack.pop();part.append(v)
   for e in v.link_edges:
    n=e.other_vert(v)
    if e.is_boundary and n in todo:todo.remove(n);stack.append(n)
  result.append(part)
 return result

rig=bpy.data.objects['Mother_Deform'];shirt=bpy.data.objects['Mother_BlouseMesh']
bm=bmesh.new();bm.from_mesh(shirt.data)
for part in loops(bm):
 c=sum((v.co for v in part),Vector())/len(part)
 if abs(c.x)>.3:
  side='L' if c.x>0 else 'R';b=rig.data.bones['LowerArm.'+side];axis=(b.tail_local-b.head_local).normalized()
  center=b.tail_local+axis*.022
  for v in part:
   radial=v.co-center;radial-=axis*radial.dot(axis)
   v.co=center+radial.normalized()*.0275
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(shirt.data);bm.free()

hair=bpy.data.objects['Mother_HairMesh'];bm=bmesh.new();bm.from_mesh(hair.data)
for part in loops(bm):
 if len(part)>100:
  for _ in range(12):bmesh.ops.smooth_vert(bm,verts=part,factor=.4,use_axis_x=True,use_axis_y=True,use_axis_z=True)
bm.to_mesh(hair.data);bm.free()

shoe=bpy.data.objects['Mother_SandalsMesh'];bm=bmesh.new();bm.from_mesh(shoe.data)
todo=set(bm.verts)
while todo:
 stack=[todo.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if n in todo:todo.remove(n);stack.append(n)
 if min((shoe.matrix_world@v.co).z for v in part)>.025:
  bmesh.ops.delete(bm,geom=part,context='VERTS')
body=bpy.data.objects['Mother_SkinMesh']
bvh=BVHTree.FromPolygons([body.matrix_world@v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons])
inv=shoe.matrix_world.inverted();deform=bm.verts.layers.deform.verify()
for side,sign in [('L',1),('R',-1)]:
 x=rig.data.bones['Foot.'+side].head_local.x
 group=shoe.vertex_groups['Foot.'+side].index
 for branch in [-1,1]:
  rings=[]
  for j in range(17):
   t=j/16;px=x+branch*.035*t;py=-.157+.083*t
   hit=bvh.ray_cast(Vector((px,py,.19)),Vector((0,0,-1)))[0]
   z=hit.z+.0045 if hit else .038
   # A broad, flattened rubber strap sits on, not inside, the foot surface.
   ring=[]
   for k in range(8):
    a=math.tau*k/8;p=Vector((px+math.cos(a)*.005,py,z+math.sin(a)*.0025))
    v=bm.verts.new(inv@p);v[deform][group]=1;ring.append(v)
   rings.append(ring)
  for j in range(16):
   for k in range(8):bm.faces.new((rings[j][k],rings[j][(k+1)%8],rings[j+1][(k+1)%8],rings[j+1][k]))
  bm.faces.new(tuple(reversed(rings[0])));bm.faces.new(tuple(rings[-1]))
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(shoe.data);bm.free()
for o in [shoe,hair,shirt]:
 for p in o.data.polygons:p.use_smooth=True
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother.blend'))
bpy.context.scene.display.shading.show_specular_highlight=False
exec(compile(Path('E:/BLENDER/mother_11_review_views.py').read_text(encoding='utf8'),'review','exec'))
print('Extended cuffs over wrist, smoothed continuous hairline, rebuilt fitted sandal straps.')
