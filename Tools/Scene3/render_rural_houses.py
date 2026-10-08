"""Render a contact sheet from the generated editable Blender source."""

from pathlib import Path

import bpy
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
tools_dir = Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(tools_dir / "NamNhaQue_Source.blend"))

houses = sorted((o for o in bpy.data.objects if o.name.startswith("Nha0")), key=lambda o: o.name)
for i, house in enumerate(houses):
    house.location.x = (i - 2) * 8.4

bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, -0.09))
ground = bpy.context.object
ground.name = "Preview_ground"
ground.dimensions = (44, 13, 0.1)
ground.data.materials.append(bpy.data.materials["Stone_Plaster"])

bpy.ops.object.light_add(type="AREA", location=(0, -14, 22))
light = bpy.context.object
light.data.energy = 6500
light.data.shape = "DISK"
light.data.size = 35

bpy.ops.object.camera_add(location=(0, -38, 18))
camera = bpy.context.object
target = Vector((0, 0, 1.7))
camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.type = "ORTHO"
camera.data.ortho_scale = 43
bpy.context.scene.camera = camera

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1800
scene.render.resolution_y = 620
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = str(tools_dir / "NamNhaQue_Preview.png")
scene.world.color = (0.48, 0.55, 0.62)
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "Medium High Contrast"
bpy.ops.render.render(write_still=True)
print("Rendered", scene.render.filepath)
