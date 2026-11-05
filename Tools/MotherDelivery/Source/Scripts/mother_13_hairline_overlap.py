import bpy,bmesh
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
hair=bpy.data.objects['Mother_HairMesh'];bm=bmesh.new();bm.from_mesh(hair.data)
left={v for v in bm.verts if v.is_boundary};parts=[]
while left:
 stack=[left.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if e.is_boundary and n in left:left.remove(n);stack.append(n)
 parts.append(part)
deform=bm.verts.layers.deform.verify();group=hair.vertex_groups['Head'].index
for part in parts:
 if len(part)<100:continue
 ids=set(part);edges=[e for e in bm.edges if e.is_boundary and all(v in ids for v in e.verts)];ring={}
 for v in part:
  p=v.co.copy();p.z-=.016;p.x*=1.008;p.y+=.0015 if p.y>-.04 else -.0015
  n=bm.verts.new(p);n[deform][group]=1;ring[v]=n
 for e in edges:
  a,b=e.verts;f=bm.faces.new((a,b,ring[b],ring[a]));f.smooth=True
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(hair.data);bm.free()
shirt=bpy.data.objects['Mother_BlouseMesh'];bm=bmesh.new();bm.from_mesh(shirt.data)
vs=[v for v in bm.verts if not v.is_boundary and v.co.z>1.04 and len(v.link_faces)>2]
for _ in range(3):bmesh.ops.smooth_vert(bm,verts=vs,factor=.22,use_axis_x=True,use_axis_y=True,use_axis_z=True)
bm.to_mesh(shirt.data);bm.free()
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother.blend'))
exec(compile(Path('E:/BLENDER/mother_11_review_views.py').read_text(encoding='utf8'),'review','exec'))
print('Scalp edge now overlaps the cut skin surface; softened upper blouse facets.')
