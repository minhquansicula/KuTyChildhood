import bpy,json,hashlib
from pathlib import Path
OUT=Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
if bpy.data.collections.get('BACKUP_Mother_Original'):
 raise RuntimeError('Backup already exists; do not duplicate or overwrite it blindly.')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother_PreAoBaBa.blend'),copy=True)
def action_hash(action):
 curves=[]
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for fc in bag.fcurves:
     curves.append((fc.data_path,fc.array_index,[(list(k.co),list(k.handle_left),list(k.handle_right),k.interpolation) for k in fc.keyframe_points]))
 return hashlib.sha256(json.dumps(curves,sort_keys=True).encode()).hexdigest()
manifest={'source_file':bpy.data.filepath,'objects':{},'actions':{a.name:action_hash(a) for a in bpy.data.actions},'image_files':{},'rig_rest':{}}
for im in bpy.data.images:
 if im.name.startswith('Mother_') and im.filepath:
  path=Path(bpy.path.abspath(im.filepath))
  if path.is_file():manifest['image_files'][str(path)]=hashlib.sha256(path.read_bytes()).hexdigest()
collection=bpy.data.collections.new('BACKUP_Mother_Original');bpy.context.scene.collection.children.link(collection)
originals=list(bpy.data.collections['CHARACTER_Mother'].objects)+list(bpy.data.collections['RIGS_ControlAndDeform'].objects)
originals=list(dict.fromkeys(originals));copies={};materials={}
for obj in originals:
 clone=obj.copy()
 if obj.data is not None:clone.data=obj.data.copy()
 clone.name='ORIGINAL__'+obj.name;collection.objects.link(clone);copies[obj]=clone
 clone['backup_of']=obj.name
 if obj.type=='MESH':
  for slot in clone.material_slots:
   mat=slot.material
   if mat:
    if mat not in materials:
     saved=mat.copy();saved.name='ORIGINAL__'+mat.name;materials[mat]=saved
    slot.material=materials[mat]
 manifest['objects'][obj.name]={'backup':clone.name,'vertices':len(obj.data.vertices) if obj.type=='MESH' else None,'uv_layers':[u.name for u in obj.data.uv_layers] if obj.type=='MESH' else None,'matrix_world':[list(row) for row in obj.matrix_world]}
 if obj.type=='ARMATURE':manifest['rig_rest'][obj.name]={b.name:[list(row) for row in b.matrix_local] for b in obj.data.bones}
for original,clone in copies.items():
 if original.parent in copies:clone.parent=copies[original.parent];clone.matrix_parent_inverse=original.matrix_parent_inverse.copy()
 for m in clone.modifiers:
  if getattr(m,'object',None) in copies:m.object=copies[m.object]
 for c in clone.constraints:
  if getattr(c,'target',None) in copies:c.target=copies[c.target]
 if clone.type=='ARMATURE':
  for pb in clone.pose.bones:
   for c in pb.constraints:
    if getattr(c,'target',None) in copies:c.target=copies[c.target]
 clone.hide_render=original.hide_render
collection.hide_render=True;collection.hide_viewport=True
collection['purpose']='Untouched independent object/mesh/armature/material backup. Original actions and image files are shared read-only and fingerprinted.'
(OUT/'Source/ao_ba_ba_preservation_manifest.json').write_text(json.dumps(manifest,indent=2))
bpy.context.scene['mother_review_status']='Artist review only; no FBX export authorized.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Source/Mother_AoBaBa_Review.blend'))
print(json.dumps({'backup_collection':collection.name,'objects':len(copies),'independent_materials':len(materials),'preserved_actions':len(manifest['actions']),'fingerprinted_texture_files':len(manifest['image_files']),'review_file':bpy.data.filepath}))
