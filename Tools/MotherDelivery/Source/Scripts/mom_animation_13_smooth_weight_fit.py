"""Restore the checkpoint weights, then apply tapered, continuous local corrections."""
import bpy,json,math
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
mesh=bpy.data.objects['Mom_User_Character'];names={g.index:g.name for g in mesh.vertex_groups}
with bpy.data.libraries.load(str(OUT/'Source/Mom_User_BeforeHousehold.blend'),link=False) as (source,target):
 assert mesh.data.name in source.meshes, 'Original mesh datablock name changed; inspect before restoring weights.'
 target.meshes=[mesh.data.name]
original=target.meshes[0]
assert len(original.vertices)==len(mesh.data.vertices)
weights=[{names[g.group]:g.weight for g in v.groups} for v in original.vertices]
bpy.data.meshes.remove(original)
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
changed=set()
for vertex in mesh.data.vertices:
 p=mesh.matrix_world@vertex.co;ax=abs(p.x+.004);w=weights[vertex.index]
 leg_factor=smooth((p.z-.48)/.10)*(1-smooth((ax-.12)/.06))
 if leg_factor:
  removed=0.
  for name in list(w):
   if name.startswith(('UpperLeg','LowerLeg','Foot','Toes')):
    amount=w[name]*leg_factor;w[name]-=amount;removed+=amount
  if removed>.000001:w['Hips']=w.get('Hips',0)+removed;changed.add(vertex.index)
 arm_factor=smooth((p.z-.61)/.07)*smooth((.89-p.z)/.12)*(1-smooth((ax-.085)/.070))
 if arm_factor:
  removed=0.
  for name in list(w):
   if name.startswith(('UpperArm','LowerArm','Shoulder','Hand')):
    amount=w[name]*arm_factor;w[name]-=amount;removed+=amount
  if removed>.000001:
   blend=smooth((p.z-.66)/.12);w['Chest']=w.get('Chest',0)+removed*blend;w['Spine']=w.get('Spine',0)+removed*(1-blend);changed.add(vertex.index)
 # Free an influence slot around the chest for both shoulder and arm weights,
 # avoiding abrupt fourth-influence swaps on tiny triangles near the armpit.
 merge=smooth((p.z-.71)/.06)*smooth((.90-p.z)/.05)*(1-smooth((ax-.10)/.08))
 if merge and w.get('UpperChest',0):
  amount=w['UpperChest']*merge;w['UpperChest']-=amount;w['Chest']=w.get('Chest',0)+amount
neighbors=[set() for _ in mesh.data.vertices]
for edge in mesh.data.edges:
 a,b=edge.vertices;neighbors[a].add(b);neighbors[b].add(a)
region=[]
for vertex in mesh.data.vertices:
 p=mesh.matrix_world@vertex.co;ax=abs(p.x+.004)
 strength=.22*smooth((p.z-.52)/.08)*smooth((.92-p.z)/.08)*(1-smooth((ax-.125)/.06))
 if strength>0:region.append((vertex.index,strength))
for _ in range(5):
 old=[w.copy() for w in weights]
 for i,strength in region:
  ids=neighbors[i]
  if not ids:continue
  average={}
  for j in ids:
   for name,value in old[j].items():average[name]=average.get(name,0)+value/len(ids)
  weights[i]={name:old[i].get(name,0)*(1-strength)+average.get(name,0)*strength for name in old[i].keys()|average.keys()}
for vertex in mesh.data.vertices:
 keep=sorted([(name,w) for name,w in weights[vertex.index].items() if w>.0001],key=lambda x:-x[1])[:4];total=sum(w for _,w in keep)
 assert total>0
 for group in mesh.vertex_groups:group.remove([vertex.index])
 for name,w in keep:mesh.vertex_groups[name].add([vertex.index],w/total,'REPLACE')
mesh.data.update();bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Household.blend'))
print('Checkpoint weights restored; continuous falloff and local smoothing applied. No hard z-boundary; <=4 normalized influences. Corrected vertices:',len(changed))
