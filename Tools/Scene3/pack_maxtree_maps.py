"""Pack original albedo + opacity for the selected species; keep source maps intact."""
from pathlib import Path
from PIL import Image
import hashlib,json
root=Path(__file__).resolve().parents[2]
source=root.parent/'BLENDER/MT_PM_V060_FBX/MT_PM_V060_Maps'
dest=root/'Assets/_Project/Art/Textures/Scene3/MaxtreeOptimized'
dest.mkdir(parents=True,exist_ok=True)
species=('Alnus_cremastogyne','Cyperus_eragrostis','Digitaria_sanguinalis','Lolium_perenne','Setaria_viridis_beauv')
report=[]
for f in source.glob('*_albedo.jpg'):
    if not any(s in f.name for s in species):continue
    stem=f.stem.removesuffix('_albedo');image=Image.open(f).convert('RGBA');limit=2048 if '_Trunk_' in stem else 1024
    image.thumbnail((limit,limit),Image.Resampling.LANCZOS)
    opacity=source/(stem+'_opacity.jpg')
    if opacity.exists():image.putalpha(Image.open(opacity).convert('L').resize(image.size,Image.Resampling.LANCZOS))
    image.save(dest/(stem+'.png'),optimize=True)
    report.append(dict(name=stem,resolution=list(image.size),alpha=opacity.exists(),source_sha256=hashlib.sha256(f.read_bytes()).hexdigest()))
(root/'Tools/Scene3/Maxtree/texture_report.json').write_text(json.dumps(report,indent=2))
print('Packed',len(report),'textures')
