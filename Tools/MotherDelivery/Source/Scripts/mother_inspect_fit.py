import bpy, bmesh, json
for name in ['Mother_BlouseMesh','Mother_HairMesh','Mother_TrousersMesh','Mother_SandalsMesh']:
 o=bpy.data.objects[name];bm=bmesh.new();bm.from_mesh(o.data)
 remaining=set(v for v in bm.verts if v.is_boundary);loops=[]
 while remaining:
  todo=[remaining.pop()];part=[]
  while todo:
   v=todo.pop();part.append(v)
   for e in v.link_edges:
    n=e.other_vert(v)
    if e.is_boundary and n in remaining:remaining.remove(n);todo.append(n)
  loops.append({'n':len(part),'min':[min(v.co[i] for v in part) for i in range(3)],'max':[max(v.co[i] for v in part) for i in range(3)],'ids':[v.index for v in part]})
 print(name,json.dumps(loops))
 bm.free()
print('JOINTS',[(b.name,list(b.head_local),list(b.tail_local)) for b in bpy.data.objects['Mother_Deform'].data.bones if b.name.startswith(('Foot','Hand','Head','LowerArm'))])
