"""Use geometry normals beyond the close LOD to avoid projection artifacts."""
import bpy
from pathlib import Path
from mathutils import Vector
project=Path(__file__).resolve().parents[2]
folder=project/'Tools/Scene3/TencentHouse04'
blend=project.parent/'BLENDER/Nhaque4/NhaQue_04_Game.blend'
bpy.ops.wm.open_mainfile(filepath=str(blend))
lods=[bpy.data.objects['Nha04_LOD'+str(i)] for i in range(3)]
for obj in lods[1:]:
    material=obj.data.materials[0]
    for link in list(material.node_tree.nodes.get('Principled BSDF').inputs['Normal'].links):
        material.node_tree.links.remove(link)
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
for obj in lods:obj.hide_set(False);obj.hide_render=obj!=lods[1]
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=4;scene.cycles.use_denoising=True
scene.view_settings.view_transform='Standard';scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.75
ld=bpy.data.lights.new('Softbox','AREA');ld.energy=2300;ld.size=8
light=bpy.data.objects.new('Softbox',ld);scene.collection.objects.link(light);light.location=(6,-6,9);light.rotation_euler=(Vector((0,0,2))-light.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=10
camera.location=(10,-13,8);camera.rotation_euler=(Vector((0,0,2.2))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(folder/'LOD1_Final_Front.png');bpy.ops.render.render(write_still=True)
print('FINAL SHADING; NO GEOMETRY MODIFICATION',flush=True)
