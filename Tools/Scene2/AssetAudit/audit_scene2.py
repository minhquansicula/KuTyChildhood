"""Read-only source audit; does not import assets, change scenes or enter/exit Play Mode."""
from pathlib import Path
from collections import Counter
import json, re, struct, zlib

ROOT = Path(__file__).resolve().parents[3]
SCENE = ROOT / 'Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity'
OUT = Path(__file__).parent
GUID = re.compile(r'guid: ([a-f0-9]{32})')
index = {}
for meta in (ROOT / 'Assets').rglob('*.meta'):
    match = GUID.search(meta.read_text(errors='ignore')[:300])
    if match:
        index[match[1]] = Path(str(meta)[:-5])

visited, sources, references = set(), {}, Counter()
def visit(path):
    if path in visited or not path.is_file(): return
    visited.add(path)
    if path.suffix.lower() not in ('.unity', '.prefab', '.mat', '.asset'): return
    text = path.read_text(errors='ignore')
    sources[path] = text
    for guid in GUID.findall(text):
        if guid in index:
            target = index[guid]
            references[target] += 1
            visit(target)
    # Model importer remaps can refer to extracted materials/textures.
    for guid in set(GUID.findall(text)):
        target = index.get(guid)
        if target and target.suffix.lower() == '.fbx':
            meta = Path(str(target) + '.meta')
            for nested in GUID.findall(meta.read_text(errors='ignore')):
                if nested in index and index[nested] != target: visit(index[nested])
visit(SCENE)

def fbx_geometry(path):
    rows = []
    with path.open('rb') as f:
        if f.read(23) != b'Kaydara FBX Binary  \x00\x1a\x00':
            raise ValueError('not a binary FBX')
        version = struct.unpack('<I', f.read(4))[0]
        large = version >= 7500
        size, fmt = (25, '<QQQB') if large else (13, '<IIIB')
        def nodes(end):
            while f.tell() + size <= end:
                offset, count, length, namesize = struct.unpack(fmt, f.read(size))
                if offset == 0: return
                name = f.read(namesize).decode(errors='replace')
                propstart = f.tell()
                if name == 'Vertices':
                    kind = f.read(1)
                    if kind in (b'd', b'f'):
                        n, encoding, compressed = struct.unpack('<III', f.read(12))
                        rows.append({'vertices': n // 3})
                elif name == 'PolygonVertexIndex':
                    if f.read(1) == b'i':
                        n, encoding, length_bytes = struct.unpack('<III', f.read(12))
                        data = f.read(length_bytes)
                        if encoding: data = zlib.decompress(data)
                        faces = sum(1 for (v,) in struct.iter_unpack('<i', data) if v < 0)
                        rows.append({'triangles': n - 2 * faces, 'polygons': faces})
                f.seek(propstart + length)
                nodes(offset)
                f.seek(offset)
        nodes(path.stat().st_size)
    return {key: sum(row.get(key, 0) for row in rows) for key in ('vertices', 'triangles', 'polygons')}

models, errors = [], []
for path in visited:
    if path.suffix.lower() != '.fbx': continue
    try:
        info = fbx_geometry(path)
        meta = Path(str(path) + '.meta').read_text(errors='ignore')
        info.update(path=path.relative_to(ROOT).as_posix(), sourceMB=round(path.stat().st_size/1048576, 2),
                    referenceOccurrences=references[path], readable=bool(re.search(r'isReadable: 1', meta)))
        models.append(info)
    except Exception as exc: errors.append({'path': str(path), 'error': str(exc)})
models.sort(key=lambda x:x['triangles'], reverse=True)

embedded = []
for path, text in sources.items():
    for block in re.split(r'(?m)(?=^--- !u!)', text):
        if not block.startswith('--- !u!43 '): continue
        counts = [int(x) for x in re.findall(r'\bindexCount: (\d+)', block)]
        name = re.search(r'(?m)^  m_Name: (.*)', block)
        embedded.append({'container':path.relative_to(ROOT).as_posix(), 'name':name[1] if name else '',
                         'triangles':sum(counts)//3})
embedded.sort(key=lambda x:x['triangles'], reverse=True)

textures = []
for path in visited:
    if path.suffix.lower() not in ('.png','.jpg','.jpeg','.tga','.tif','.tiff','.exr','.hdr','.psd'): continue
    width = height = None
    try:
        from PIL import Image
        with Image.open(path) as im: width,height = im.size
    except Exception: pass
    meta = Path(str(path)+'.meta').read_text(errors='ignore')
    def setting(key):
        match = re.search(r'(?m)^  '+key+r': ([^\r\n]+)', meta)
        return match[1] if match else None
    textures.append({'path':path.relative_to(ROOT).as_posix(), 'width':width, 'height':height,
                     'sourceMB':round(path.stat().st_size/1048576,2),'maxTextureSize':setting('maxTextureSize'),
                     'readable':setting('isReadable'), 'settings':meta})
textures.sort(key=lambda x:(x['width'] or 0)*(x['height'] or 0), reverse=True)
scene_text=sources[SCENE]
lights=[]
for block in re.split(r'(?m)(?=^--- !u!)',scene_text):
    if not block.startswith('--- !u!108 '): continue
    def number(pattern):
        match=re.search(pattern,block)
        return match[1] if match else None
    lights.append({'gameObject':number(r'm_GameObject: \{fileID: (\d+)'), 'enabled':number(r'm_Enabled: (\d+)'),
                   'type':number(r'(?m)^  m_Type: (\d+)'), 'shadows':number(r'm_Shadows:\s+?m_Type: (\d+)'),
                   'lightmapping':number(r'm_Lightmapping: (\d+)')})
report={'scope':'Saved scene dependencies including inactive objects/prefab dependencies; not runtime visible counts. FBX triangles are source triangulation equivalents, not a frame GPU measurement.',
        'modelCount':len(models),'models':models,'embeddedMeshes':embedded,'textures':textures,'serializedSceneLights':lights,'errors':errors}
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'source-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'modelCount':len(models),'topModels':models[:18],'embeddedMeshes':embedded[:8],
                  'textureCount':len(textures),'topTextures':[{k:v for k,v in t.items() if k!='settings'} for t in textures[:12]],
                  'lights':lights,'errors':errors},ensure_ascii=False,indent=2))
