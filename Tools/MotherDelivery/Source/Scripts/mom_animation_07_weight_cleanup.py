import bpy,json
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
mesh=bpy.data.objects['Mom_User_Character'];changed=0
for vertex in mesh.data.vertices:
 p=mesh.matrix_world@vertex.co
 if p.z<.590 or abs(p.x+.004)>.140:continue
 weights={mesh.vertex_groups[g.group].name:g.weight for g in vertex.groups};fade=max(0,min(1,(p.z-.590)/.050));fade=fade*fade*(3-2*fade);removed=0
 for name in list(weights):
  if name.startswith(('UpperLeg','LowerLeg','Foot','Toes')):
   amount=weights[name]*fade;weights[name]-=amount;removed+=amount
 if removed<.00001:continue
 weights['Hips']=weights.get('Hips',0)+removed
 weights=sorted([(n,w) for n,w in weights.items() if w>.0001],key=lambda x:-x[1])[:4];total=sum(w for _,w in weights)
 for group in mesh.vertex_groups:group.remove([vertex.index])
 for name,weight in weights:mesh.vertex_groups[name].add([vertex.index],weight/total,'REPLACE')
 changed+=1
mesh.data.update();bpy.context.view_layer.update()
print('Removed automatic leg-weight leakage from the shirt/button placket:',changed,'vertices; source mesh/UV/texture unchanged.')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mom_User_Animated.blend'))
