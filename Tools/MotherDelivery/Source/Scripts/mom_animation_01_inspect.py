import bpy,json,math,bmesh
from pathlib import Path
from mathutils import Vector
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
PREVIEW=OUT/'Preview/UserMomAnimation';PREVIEW.mkdir(exist_ok=True)
scene=bpy.context.scene
assert scene.name.startswith('USER_Mom')
mesh=next(o for o in scene.objects if o.type=='MESH')
collection=bpy.data.collections.new('USER_Mom_AnimationStudio');scene.collection.children.link(collection)
data=bpy.data.cameras.new('UserMom_ReviewCamera');camera=bpy.data.objects.new('UserMom_ReviewCamera',data);collection.objects.link(camera);scene.camera=camera;data.type='ORTHO';data.ortho_scale=1.36
world=bpy.data.worlds.new('UserMom_NeutralWorld');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.14,.14,.14,1);world.node_tree.nodes['Background'].inputs[1].default_value=.5;scene.world=world
for name,loc,power in [('Key',(1.3,-2.0,2.0),150),('Fill',(-1.5,-1.0,1.3),90),('Rim',(0,1.4,1.8),100)]:
 light=bpy.data.lights.new('UserMom_'+name,'AREA');light.energy=power;light.shape='DISK';light.size=1.8;obj=bpy.data.objects.new('UserMom_'+name,light);collection.objects.link(obj);obj.location=loc;obj.rotation_euler=(Vector((0,0,.6))-obj.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=700;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
for name,loc in [('Front',(0,-3,.58)),('Side',(3,0,.58)),('Back',(0,3,.58))]:
 camera.location=loc;camera.rotation_euler=(Vector((0,0,.575))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(PREVIEW/('01_Original_'+name+'.png'));bpy.ops.render.render(write_still=True)
bm=bmesh.new();bm.from_mesh(mesh.data);left=set(bm.verts);parts=[]
while left:
 stack=[left.pop()];part=[]
 while stack:
  v=stack.pop();part.append(v)
  for edge in v.link_edges:
   other=edge.other_vert(v)
   if other in left:left.remove(other);stack.append(other)
 ps=[mesh.matrix_world@v.co for v in part]
 parts.append({'vertices':len(part),'min':[min(p[i] for p in ps) for i in range(3)],'max':[max(p[i] for p in ps) for i in range(3)]})
bm.free()
points=[mesh.matrix_world@v.co for v in mesh.data.vertices];slices=[]
for z in [.03,.08,.14,.24,.35,.46,.55,.64,.72,.80,.88,.96,1.04,1.1]:
 ps=[p for p in points if abs(p.z-z)<.012]
 slices.append({'z':z,'count':len(ps),'min':[min(p[i] for p in ps) for i in range(3)] if ps else None,'max':[max(p[i] for p in ps) for i in range(3)] if ps else None})
report={'mesh':mesh.name,'components':sorted(parts,key=lambda p:-p['vertices']),'slices':slices,'matrix':[list(r) for r in mesh.matrix_world]}
(OUT/'Source/user_mom_anatomy_inspection.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
