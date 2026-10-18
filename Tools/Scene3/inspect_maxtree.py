from pathlib import Path
import bpy, json, math, shutil
from mathutils import Vector

root=Path(__file__).resolve().parents[2]
source=root.parent/'BLENDER/MT_PM_V060_FBX'
output=root/'Tools/Scene3/Maxtree'
output.mkdir(parents=True,exist_ok=True)
species=['Cynodon_dactylon','Cyperus_eragrostis','Digitaria_sanguinalis','Lolium_perenne','Setaria_viridis_beauv','Zoysia_japonica_steud','Alnus_cremastogyne','Acer_buergerianum']
report=[]
for species_name in species:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    original=next(source.glob('*_'+species_name+'_01.fbx'))
    staged=output/original.name
    if not staged.exists():shutil.copy2(original,staged)
    bpy.ops.import_scene.fbx(filepath=str(staged),use_image_search=False,use_anim=False)
    objects=[]
    for obj in bpy.data.objects:
        if obj.type!='MESH':continue
        obj.data.calc_loop_triangles()
        points=[obj.matrix_world@Vector(v) for v in obj.bound_box]
        low=[min(p[i] for p in points) for i in range(3)]
        high=[max(p[i] for p in points) for i in range(3)]
        objects.append(dict(name=obj.name,verts=len(obj.data.vertices),triangles=len(obj.data.loop_triangles),bounds_min=low,bounds_max=high,materials=[m.name if m else None for m in obj.data.materials]))
    materials=[]
    for mat in bpy.data.materials:
        nodes=[]
        if mat.use_nodes:
            for n in mat.node_tree.nodes:
                if n.type=='TEX_IMAGE':nodes.append(dict(name=n.name,image=n.image.name if n.image else None,links=[(l.from_socket.name,l.to_node.name,l.to_socket.name) for s in n.outputs for l in s.links]))
        materials.append(dict(name=mat.name,nodes=nodes,diffuse=list(mat.diffuse_color)))
    images=[dict(name=i.name,path=i.filepath,size=list(i.size),packed=bool(i.packed_file),exists=Path(bpy.path.abspath(i.filepath)).exists()) for i in bpy.data.images]
    entry=dict(species=species_name,objects=objects,materials=materials,images=images)
    report.append(entry)
    (output/'inspection.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('INSPECTED',species_name,'meshes',len(objects),'tris',sum(o['triangles'] for o in objects),'images',len(images),flush=True)
