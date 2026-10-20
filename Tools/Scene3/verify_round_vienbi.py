"""Verify exported FBX geometry, not just pre-export Blender objects."""
from pathlib import Path
from mathutils import Vector
import bpy, json, hashlib

project=Path(__file__).resolve().parents[2]
out=project/'Tools/Scene3/VienBi'
bpy.ops.wm.read_factory_settings(use_empty=True)
results=[]
for index,label in enumerate(('Player','Target0','Target1','Target2','Target3','Target4')):
    bpy.ops.import_scene.fbx(filepath=str(project/'Assets/_Project/Art/Models/Scene3/VienBi'/('VienBi_'+label+'.fbx')))
    obj=bpy.context.selected_objects[0];mesh=obj.data
    mesh.calc_loop_triangles()
    points=[obj.matrix_world@v.co for v in mesh.vertices]
    error=max(abs(v.length-.5) for v in points)
    assert error<1e-5,(label,error)
    assert len(mesh.loop_triangles)==832
    assert len(mesh.materials)==1
    assert all(p.use_smooth for p in mesh.polygons)
    results.append({'name':label,'exported_triangles':len(mesh.loop_triangles),'radius_error':error,'diameter_metres_in_scene':.016})
    mat=bpy.data.materials.new('Preview_'+label);mat.use_nodes=True
    nodes=mat.node_tree.nodes;nodes.clear();links=mat.node_tree.links
    tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(out/'Staging'/('VienBi_'+label+'_Color.png')))
    em=nodes.new('ShaderNodeEmission');output=nodes.new('ShaderNodeOutputMaterial')
    links.new(tex.outputs['Color'],em.inputs['Color']);links.new(em.outputs[0],output.inputs['Surface'])
    mesh.materials.clear();mesh.materials.append(mat)
    obj.location=((index%3-1)*1.25,0,(1-index//3)*1.25)
    print('VERIFIED',label,error,flush=True)
source=project.parent/'BLENDER/VienBi/1ca9a2dd15e97dae21ef7955ada009d2.obj'
sha=hashlib.sha256(source.read_bytes()).hexdigest()
assert sha=='ca7ad5afd5d1fe19d2cb8a021fbdc461d4780992a71be9a3350e58c67aee05ad'
(out/'round_fbx_verification.json').write_text(json.dumps({'passed':True,'source_unchanged':sha,'variants':results},indent=2))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
scene.view_settings.view_transform='Standard'
camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=4.6
camera.location=(0,-5,.625);camera.rotation_euler=(Vector((0,0,.625))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.07,.07,.07,1)
scene.render.resolution_x=1000;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.render.filepath=str(out/'Six_Game_Marbles.png');bpy.ops.render.render(write_still=True)
