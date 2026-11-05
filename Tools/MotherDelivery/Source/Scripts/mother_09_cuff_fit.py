import bpy
from pathlib import Path
OUT = Path('E:/FPT/FALL26/PRU213/KuTy/Tools/MotherDelivery')
bpy.ops.wm.open_mainfile(filepath=str(OUT / 'Source/Mother.blend'))
shirt = bpy.data.objects['Mother_BlouseMesh']
if not shirt.get('final_cuff_fit'):
    with bpy.data.libraries.load(str(OUT / 'Source/Progress/05_TenBakedAnimations.blend'), link=False) as (source, target):
        target.objects = ['Mother_BlouseMesh']
    original = target.objects[0]
    assert len(shirt.data.vertices) == len(original.data.vertices)
    for v, ref in zip(shirt.data.vertices, original.data.vertices):
        if abs(ref.co.x) > .25:
            v.co = ref.co.copy()
        # Keep the lower shirt in front of the trouser waistband, rather than intersecting it.
        if .79 < ref.co.z < 1.08 and ref.co.y < -.035:
            fade = min(1, (1.08-ref.co.z)/.08)
            v.co.y -= .035 * fade
    bpy.data.objects.remove(original, do_unlink=True)
    shirt['final_cuff_fit'] = True
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'Source/Mother.blend'))
exec(compile(Path('E:/BLENDER/mother_06_delivery.py').read_text(encoding='utf8'), 'mother_06_delivery.py', 'exec'))
exec(compile(Path('E:/BLENDER/mother_common.py').read_text(encoding='utf8'), 'mother_common.py', 'exec'))
S.cycles.samples=12
S.render.resolution_percentage=65
render('08_Final_Fit', (1.8,-3.6,1.6), (0,0,.82), 65)
