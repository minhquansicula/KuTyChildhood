import bpy,math,bmesh,json
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
pants=bpy.data.objects['Mother_Pants'];key=pants.data.shape_keys.key_blocks['LooseStraight_FabricVolume']
# Preserve the original crotch topology and anatomical side. The earlier radial
# expansion is only appropriate below the crotch, not across its shared saddle.
for v in pants.data.vertices:
 x,y,z=v.co
 if .60<z<.90:
  blend=max(0,min(1,(.14-abs(x))/.075))*max(0,min(1,(z-.60)/.075))
  p=key.data[v.index].co.copy();p=p.lerp(v.co,blend)
  if x*p.x<0:p.x=math.copysign(max(.0015,abs(x)*.70),x)
  key.data[v.index].co=p

shirt=bpy.data.objects['Mother_Shirt'];shape=shirt.data.shape_keys.key_blocks['AoBaBa_LooseCut_MinimalNeck']
bm=bmesh.new();bm.from_mesh(shirt.data);left=set(bm.verts);parts=[]
while left:
 stack=[left.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for e in v.link_edges:
   other=e.other_vert(v)
   if other in left:left.remove(other);stack.append(other)
 parts.append(part)
# Keep buttons/placket deliberately outside the cloth instead of coplanar.
for part in parts:
 center=sum((v.co for v in part),Vector())/len(part)
 if len(part)<180 and abs(center.x)<.015:
  is_button=len(part)<80
  for v in part:shape.data[v.index].co.y-=.0035 if is_button else .002
bm.free()

# Slightly increase clearance at the sleeve, but not at the exposed hand. Original
# bone assignments and every existing key remain unchanged.
rig=bpy.data.objects['Mother_Deform'];names={g.index:g.name for g in shirt.vertex_groups}
for v in shirt.data.vertices:
 arm=sum(g.weight for g in v.groups if names.get(g.group,'').startswith(('UpperArm','LowerArm')))
 if arm<.8:continue
 p=shape.data[v.index].co;side='L' if v.co.x>0 else 'R';best=None
 for bn in ['UpperArm.'+side,'LowerArm.'+side]:
  bone=rig.data.bones[bn];d=bone.tail_local-bone.head_local;t=max(0,min(1,(p-bone.head_local).dot(d)/d.length_squared));c=bone.head_local+d*t
  if best is None or (p-c).length<best[0]:best=((p-c).length,c)
 if best[0]>.001:p[:]=best[1]+(p-best[1])*1.065
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother_AoBaBa_Review.blend'))
exec(compile(Path('E:/BLENDER/mother_27_validate_review.py').read_text(encoding='utf8'),'validate','exec'))
