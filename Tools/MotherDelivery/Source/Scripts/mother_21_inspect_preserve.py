import bpy, bmesh, json, os
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
def bounds(o):
 from mathutils import Vector
 ps=[o.matrix_world@Vector(p) for p in o.bound_box]
 return {'min':[min(p[i] for p in ps) for i in range(3)],'max':[max(p[i] for p in ps) for i in range(3)]}
report={'file':bpy.data.filepath,'dirty':bpy.data.is_dirty,'blender_version':bpy.app.version_string,'collections':{},'objects':[],'actions':[],'images':[]}
for c in bpy.data.collections:report['collections'][c.name]=[o.name for o in c.objects]
for o in bpy.data.objects:
 if o.name.startswith('Mother_') or o.name.startswith(('SK_Mother','Socket_')):
  entry={'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,'location':list(o.location),'rotation':list(o.rotation_euler),'scale':list(o.scale),'bounds':bounds(o),'hidden':o.hide_get(),'modifiers':[],'constraints':[]}
  for m in o.modifiers:entry['modifiers'].append({'name':m.name,'type':m.type,'target':getattr(getattr(m,'object',None),'name',None),'viewport':m.show_viewport,'render':m.show_render})
  for c in o.constraints:entry['constraints'].append({'name':c.name,'type':c.type,'target':getattr(getattr(c,'target',None),'name',None)})
  if o.type=='MESH':
   o.data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(o.data)
   entry.update({'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'uv_layers':[l.name for l in o.data.uv_layers],'boundary_edges':sum(e.is_boundary for e in bm.edges),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'loose_vertices':sum(not v.link_faces for v in bm.verts),'materials':[m.name if m else None for m in o.data.materials],'groups':[g.name for g in o.vertex_groups],'shape_keys':[(k.name,k.value) for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else []})
   bm.free()
  if o.type=='ARMATURE':entry['bones']=[{'name':b.name,'parent':b.parent.name if b.parent else None,'head':list(b.head_local),'tail':list(b.tail_local)} for b in o.data.bones]
  report['objects'].append(entry)
for a in bpy.data.actions:report['actions'].append({'name':a.name,'range':list(a.frame_range),'users':a.users,'fake_user':a.use_fake_user})
for im in bpy.data.images:
 if im.name.startswith('Mother_'):report['images'].append({'name':im.name,'size':list(im.size),'path':im.filepath,'exists':Path(bpy.path.abspath(im.filepath)).is_file() if im.filepath else False,'packed':bool(im.packed_file)})
report['materials']=[]
for m in bpy.data.materials:
 if m.name.startswith('Mother_'):
  entry={'name':m.name,'use_nodes':m.use_nodes,'diffuse':list(m.diffuse_color),'textures':[]}
  if m.use_nodes:
   for n in m.node_tree.nodes:
    if n.type=='TEX_IMAGE':entry['textures'].append({'node':n.name,'image':n.image.name if n.image else None,'outputs':[(l.from_socket.name,l.to_node.name,l.to_socket.name) for l in n.outputs[0].links]})
  report['materials'].append(entry)
(OUT/'Source/ao_ba_ba_inspection_before.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'file':report['file'],'dirty':report['dirty'],'objects':[{k:v for k,v in x.items() if k in ['name','type','parent','location','scale','vertices','triangles','uv_layers','boundary_edges','loose_vertices','materials','shape_keys','modifiers']} for x in report['objects']],'actions':report['actions'],'missing_images':[x for x in report['images'] if not x['exists'] and not x['packed']]}))
