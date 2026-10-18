"""Make lightweight, regular spheres and rebake the existing marble colours."""
from pathlib import Path
import bpy, json, shutil
from mathutils import Vector

project = Path(__file__).resolve().parents[2]
out = project / 'Tools/Scene3/VienBi'
stage = out / 'Staging'
backup = out / 'BeforeRound'
backup.mkdir(exist_ok=True)
report = json.loads((out / 'game_assets_report.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 1
scene.render.bake.use_selected_to_active = True
scene.render.bake.cage_extrusion = .06
scene.render.bake.max_ray_distance = .12
scene.render.bake.margin = 8
for item in report['variants']:
    fbx, png = stage / item['fbx'], stage / item['texture']
    for path in (fbx, png):
        if not (backup / path.name).exists():
            shutil.copy2(path, backup / path.name)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    # Always start with the original exported colour/UV pair, so reruns do not degrade it.
    bpy.ops.import_scene.fbx(filepath=str(backup / fbx.name))
    source = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
    center = sum((Vector(c) for c in source.bound_box), Vector()) / 8
    for v in source.data.vertices:
        v.co = (v.co - center).normalized() * .5
    source.data.update()
    source.location = (0, 0, 0)
    source.rotation_euler = (0, 0, 0)
    source.scale = (1, 1, 1)
    mat = bpy.data.materials.new('SourceColour'); mat.use_nodes = True
    nodes = mat.node_tree.nodes; nodes.clear(); links = mat.node_tree.links
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(str(backup / png.name), check_existing=False)
    em = nodes.new('ShaderNodeEmission'); output = nodes.new('ShaderNodeOutputMaterial')
    links.new(tex.outputs['Color'], em.inputs['Color']); links.new(em.outputs[0], output.inputs['Surface'])
    source.data.materials.clear(); source.data.materials.append(mat)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=14, radius=.5)
    sphere = bpy.context.object; sphere.name = 'VienBi_' + item['name']
    for p in sphere.data.polygons: p.use_smooth = True
    target = bpy.data.images.new('RoundColour_' + item['name'], width=512, height=512, alpha=False)
    dst = bpy.data.materials.new('BakedColour'); dst.use_nodes = True
    dst_tex = dst.node_tree.nodes.new('ShaderNodeTexImage'); dst_tex.image = target
    dst.node_tree.nodes.active = dst_tex
    sphere.data.materials.append(dst)
    bpy.ops.object.select_all(action='DESELECT'); source.select_set(True); sphere.select_set(True)
    bpy.context.view_layer.objects.active = sphere
    bpy.ops.object.bake(type='EMIT')
    target.filepath_raw = str(png); target.file_format = 'PNG'; target.save()
    bpy.ops.object.select_all(action='DESELECT'); sphere.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={'MESH'},
        axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_anim=False, path_mode='STRIP', embed_textures=False)
    sphere.data.calc_loop_triangles()
    item['game_triangles'] = len(sphere.data.loop_triangles)
    item['game_dimensions'] = list(sphere.dimensions)
    item['radius_error'] = max(abs(v.co.length - .5) for v in sphere.data.vertices)
    assert item['radius_error'] < 1e-6
    assert max(item['game_dimensions']) - min(item['game_dimensions']) < 1e-6
    print('ROUND', item['name'], item['game_triangles'], item['radius_error'], flush=True)
report['shape'] = 'regular UV sphere, radius 0.5, scene diameter 0.016 metres'
(out / 'game_assets_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
