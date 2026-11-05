"""Build the remaining mother delivery stages without touching an open UI scene."""
import bpy, json, sys, traceback
from pathlib import Path

BASE = Path('E:/BLENDER')
OUT = Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
bpy.ops.wm.open_mainfile(filepath=str(OUT / 'Source/Mother.blend'))
if 'Mother_Deform' not in bpy.data.objects:
    exec(compile((BASE / 'mother_04_rig.py').read_text(encoding='utf8'), 'mother_04_rig.py', 'exec'))
if not (OUT / 'Source/animation_manifest.json').exists():
    exec(compile((BASE / 'mother_05_animation.py').read_text(encoding='utf8'), 'mother_05_animation.py', 'exec'))

rig = bpy.data.objects['Mother_Deform']
char = list(bpy.data.collections['CHARACTER_Mother'].objects)
sockets = [o for o in bpy.data.objects if o.name.startswith('Socket_')]
root = bpy.data.objects['SK_Mother']
clips = json.loads((OUT / 'Source/animation_manifest.json').read_text())
scene = bpy.context.scene
scene.render.fps = 30
export = OUT / 'Exports'
export.mkdir(exist_ok=True)

def select(objects):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig

def fbx(path, animated):
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True, object_types={'MESH', 'ARMATURE', 'EMPTY'},
        global_scale=1.0, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True,
        add_leaf_bones=False, use_armature_deform_only=True,
        bake_anim=animated, bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0, path_mode='AUTO')

rig.animation_data.action = None
for pb in rig.pose.bones:
    pb.location = (0, 0, 0)
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.scale = (1, 1, 1)
scene.frame_set(1)
select([root, rig] + char + sockets)
fbx(export / 'SK_Mother.fbx', False)
for clip in clips:
    rig.animation_data.action = bpy.data.actions[clip['name']]
    scene.frame_start = clip['frame_start']
    scene.frame_end = clip['frame_end']
    scene.frame_set(1)
    select([root, rig])
    fbx(export / 'Animations' / (clip['name'] + '.fbx'), True)

errors = []
stats = []
for obj in char:
    if obj.type != 'MESH':
        continue
    obj.data.calc_loop_triangles()
    missing = bad = 0
    for v in obj.data.vertices:
        weights = [g.weight for g in v.groups if obj.vertex_groups[g.group].name in rig.data.bones and g.weight > 0]
        missing += not weights
        bad += bool(weights) and (len(weights) > 4 or abs(sum(weights) - 1) > .001)
    if missing or bad:
        errors.append(f'{obj.name}: {missing} unweighted / {bad} invalid weights')
    stats.append({'mesh': obj.name, 'vertices': len(obj.data.vertices), 'triangles': len(obj.data.loop_triangles), 'materials': len(obj.data.materials)})
loop_errors = {}
for clip in clips:
    rig.animation_data.action = bpy.data.actions[clip['name']]
    scene.frame_set(1)
    first = {p.name: p.matrix.copy() for p in rig.pose.bones}
    scene.frame_set(clip['frame_end'])
    delta = max(max(abs(first[p.name][i][j] - p.matrix[i][j]) for i in range(4) for j in range(4)) for p in rig.pose.bones)
    if clip['loop']:
        loop_errors[clip['name']] = delta
        if delta > .001:
            errors.append(f"Loop seam {clip['name']}: {delta}")
report = {'errors': errors, 'meshes': stats, 'triangles': sum(x['triangles'] for x in stats), 'bones': len(rig.data.bones), 'clips': len(clips), 'loop_matrix_errors': loop_errors}
(OUT / 'Source/delivery_validation.json').write_text(json.dumps(report, indent=2))
rig.animation_data.action = bpy.data.actions['Mother_Idle_Standing']
scene.frame_start = 1
scene.frame_end = 151
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'Source/Mother.blend'))
print('MOTHER_DELIVERY_REPORT', json.dumps(report))
if errors:
    raise RuntimeError('; '.join(errors))
