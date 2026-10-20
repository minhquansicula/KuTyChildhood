import bpy
from pathlib import Path
from mathutils import Vector

project=Path(__file__).resolve().parents[2]
folder=project/'Tools/Scene3/TencentHouse03/Diagnostics';folder.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(project.parent/'BLENDER/Nhaque3/NhaQue_03_Game.blend'))
high=bpy.data.objects['Nha03_Original_BakeOnly']
lods=[bpy.data.objects['Nha03_LOD'+str(i)] for i in range(3)]
for obj in [high]+lods:obj.hide_set(False);obj.hide_render=obj!=lods[0]
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=4;scene.cycles.use_denoising=True
scene.view_settings.view_transform='Standard';scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.75
ld=bpy.data.lights.new('Softbox','AREA');ld.energy=2300;ld.size=8
light=bpy.data.objects.new('Softbox',ld);scene.collection.objects.link(light);light.location=(6,-6,9);light.rotation_euler=(Vector((0,0,2))-light.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=10
camera.location=(10,-13,8);camera.rotation_euler=(Vector((0,0,2.2))-camera.location).to_track_quat('-Z','Y').to_euler()
material=lods[0].data.materials[0];nodes=material.node_tree.nodes;links=material.node_tree.links
shader=nodes.get('Principled BSDF');base=shader.inputs['Base Color'].links[0].from_socket
for link in list(shader.inputs['Normal'].links):links.remove(link)
scene.render.filepath=str(folder/'NoNormal.png');bpy.ops.render.render(write_still=True)
emit=nodes.new('ShaderNodeEmission');links.new(base,emit.inputs['Color']);links.new(emit.outputs[0],nodes.get('Material Output').inputs[0])
scene.render.filepath=str(folder/'Albedo.png');bpy.ops.render.render(write_still=True)
