import bpy, json, hashlib, math
from pathlib import Path
from mathutils import Vector

project=Path(__file__).resolve().parents[2]
source=project.parent/'BLENDER/VienBi'
out=project/'Tools/Scene3/VienBi'
out.mkdir(parents=True,exist_ok=True)
obj=source/'1ca9a2dd15e97dae21ef7955ada009d2.obj'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=str(obj),forward_axis='NEGATIVE_Z',up_axis='Y')
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
info=[]
for mesh in meshes:
    b=[mesh.matrix_world@Vector(c) for c in mesh.bound_box]
    minv=Vector(tuple(min(v[i] for v in b) for i in range(3)))
    maxv=Vector(tuple(max(v[i] for v in b) for i in range(3)))
    info.append({'name':mesh.name,'vertices':len(mesh.data.vertices),'triangles':sum(len(p.vertices)-2 for p in mesh.data.polygons),'bounds_min':list(minv),'bounds_max':list(maxv),'dimensions':list(maxv-minv),'materials':[m.name for m in mesh.data.materials]})
(out/'source_report.json').write_text(json.dumps({'objects':info,'sha256':hashlib.sha256(obj.read_bytes()).hexdigest()},indent=2),encoding='utf-8')
print(json.dumps(info),flush=True)
mesh=meshes[0]
for material in mesh.data.materials:
    material.diffuse_color=(.2,.6,.5,1)
    material.use_nodes=True
    shader=material.node_tree.nodes.get('Principled BSDF')
    if shader:
        shader.inputs['Roughness'].default_value=.28
        color=bpy.data.images.load(str(source/'texture_pbr_20250901.png'),check_existing=True)
        tex=material.node_tree.nodes.new('ShaderNodeTexImage');tex.image=color
        material.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
scene.view_settings.view_transform='Standard';scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('World');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.8
ld=bpy.data.lights.new('Key','AREA');ld.energy=500;ld.size=2
light=bpy.data.objects.new('Key',ld);scene.collection.objects.link(light);light.location=(2,-3,3)
cam=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=1.5
target=Vector((-.014,.03,.189))
for name,position in [('Top',(0,0,3)),('Side',(0,-3,.2)),('Angle',(1,-2,2))]:
    cam.location=position;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(out/('Source_'+name+'.png'));bpy.ops.render.render(write_still=True)
