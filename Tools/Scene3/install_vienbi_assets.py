"""Publish six validated, low-poly marble meshes and colour textures to Unity."""
from pathlib import Path
import json,re,shutil,uuid

project=Path(__file__).resolve().parents[2]
report=json.loads((project/'Tools/Scene3/VienBi/game_assets_report.json').read_text(encoding='utf-8'))
assert len(report['variants'])==6
assert all(600<=v['game_triangles']<=900 and min(v['game_dimensions'])>.82 and max(v['game_dimensions'])<=1.01 for v in report['variants'])
source=project.parent/'BLENDER/VienBi/1ca9a2dd15e97dae21ef7955ada009d2.obj'
import hashlib
assert hashlib.sha256(source.read_bytes()).hexdigest()==report['source_sha256']
staged=project/'Tools/Scene3/VienBi/Staging'
texture_template=project/'Assets/_Project/Art/Textures/Scene3/TencentHouse04/Nha04_LOD2_BaseColor.png.meta'
model_template=project/'Assets/_Project/Art/Models/Scene3/TencentHouse04/NhaQue_Tencent_04.fbx.meta'

def publish(src,dst,template):
    assert src.is_file(),src
    dst.parent.mkdir(parents=True,exist_ok=True)
    meta=Path(str(dst)+'.meta')
    if not meta.exists():
        settings=template.read_text(encoding='utf-8')
        settings=re.sub(r'(?m)^guid: \w+','guid: '+uuid.uuid4().hex,settings)
        meta.write_text(settings,encoding='utf-8')
    shutil.copy2(src,dst)
    print(dst.relative_to(project))

for item in report['variants']:
    publish(staged/item['fbx'],project/'Assets/_Project/Art/Models/Scene3/VienBi'/item['fbx'],model_template)
    publish(staged/item['texture'],project/'Assets/_Project/Art/Textures/Scene3/VienBi'/item['texture'],texture_template)
