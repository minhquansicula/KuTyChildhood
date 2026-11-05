"""Verify game-facing FBX files by importing into a clean Blender scene."""
import bpy, json
from pathlib import Path
from mathutils import Vector

OUT = Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT / 'Exports/SK_Mother.fbx'))
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
rigs = [o for o in bpy.data.objects if o.type == 'ARMATURE']
points = [o.matrix_world @ Vector(p) for o in meshes for p in o.bound_box]
height = max(p.z for p in points) - min(p.z for p in points)
errors = []
if len(meshes) != 6 or len(rigs) != 1:
    errors.append('Unexpected model mesh/rig count')
if not 1.45 < height < 1.85:
    errors.append('Unexpected scale: ' + str(height))
if any(not any(m.type == 'ARMATURE' for m in o.modifiers) for o in meshes):
    errors.append('Missing skin binding')
sockets = [o.name for o in bpy.data.objects if o.name.startswith('Socket_')]
if len(sockets) != 4:
    errors.append('Missing sockets')
report = {'height_m': height, 'mesh_count': len(meshes), 'armatures': len(rigs), 'bones': len(rigs[0].data.bones), 'sockets': sockets, 'clips': [], 'errors': errors}
for clip in json.loads((OUT / 'Source/animation_manifest.json').read_text()):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(OUT / 'Exports/Animations' / (clip['name'] + '.fbx')))
    rigs = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    action = rigs[0].animation_data.action if rigs and rigs[0].animation_data else None
    if action is None:
        errors.append('No animation in ' + clip['name'])
        continue
    frames = list(action.frame_range)
    report['clips'].append({'name': clip['name'], 'frame_range': frames, 'bones': len(rigs[0].data.bones)})
    if abs((frames[1] - frames[0]) - (clip['frame_end'] - 1)) > 1:
        errors.append('Wrong animation length: ' + clip['name'])
(OUT / 'Source/fbx_roundtrip_validation.json').write_text(json.dumps(report, indent=2))
print('MOTHER_ROUNDTRIP', json.dumps(report))
if errors:
    raise RuntimeError('; '.join(errors))
