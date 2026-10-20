"""Publish validated staged assets with correct importer settings before refresh.

Normal maps already have their import type; FBX material auto-creation is off.
Existing GUIDs are retained when rebaking so scene references stay valid.
"""
from pathlib import Path
import shutil
import re
import uuid
import json

project = Path(__file__).resolve().parents[2]
report = project/'Tools/Scene3/TencentHouse03'
geometry = json.loads((report/'geometry_report.json').read_text(encoding='utf-8'))
silhouette = json.loads((report/'silhouette_report.json').read_text(encoding='utf-8'))
assert len(silhouette) == 12
assert min(r['silhouette_intersection_over_union'] for r in silhouette if r['LOD'] == 0) >= .995
assert len(geometry['lods']) == 3
assert all(r['triangles'] <= cap for r, cap in zip(geometry['lods'], (90000, 36000, 12000)))

def publish(staged, output, template):
    assert staged.is_file(), staged
    output.parent.mkdir(parents=True, exist_ok=True)
    meta = Path(str(output)+'.meta')
    if not meta.exists():
        settings = template.read_text(encoding='utf-8')
        settings = re.sub(r'(?m)^guid: \w+', 'guid: '+uuid.uuid4().hex, settings)
        meta.write_text(settings, encoding='utf-8')
    shutil.copy2(staged, output)
    print(output.relative_to(project))

for i in range(3):
    for kind in ('BaseColor', 'Normal', 'MetallicSmoothness'):
        filename = f'Nha03_LOD{i}_{kind}.png'
        publish(report/'Staging/Textures'/filename,
                project/'Assets/_Project/Art/Textures/Scene3/TencentHouse03'/filename,
                project/f'Assets/_Project/Art/Textures/Scene3/TencentHouse02/Nha02_LOD{i}_{kind}.png.meta')
publish(report/'Staging/NhaQue_Tencent_03.fbx',
        project/'Assets/_Project/Art/Models/Scene3/TencentHouse03/NhaQue_Tencent_03.fbx',
        project/'Assets/_Project/Art/Models/Scene3/TencentHouse02/NhaQue_Tencent_02.fbx.meta')
