from pathlib import Path
import json
source=Path('E:/BLENDER/mom_animation_09_household_review.py').read_text(encoding='utf8')
source=source.replace('bpy.ops.render.render(write_still=True)','None').replace('print(json.dumps(report))','')
exec(compile(source,'household_numeric_review','exec'))
print(json.dumps({'errors':report['errors'],'warnings':report['warnings'],'clips':[{'name':clip['action'],'max_stretch':max(s['max_edge_stretch'] for s in clip['samples']),'over2x':max(s['edges_stretched_over_2x'] for s in clip['samples'])} for clip in report['clips']]}))
