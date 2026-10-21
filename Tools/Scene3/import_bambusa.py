"""Read-only FBX extraction and game derivatives of the user's Bambusa library.

The source FBX is never copied wholesale into Assets. UVs and source polygon
normals are retained; foliage reduction selects complete connected leaf cards.
"""
from pathlib import Path
import argparse, json, struct, zlib, sys
import numpy as np
from scipy.spatial import cKDTree
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components
from scipy.spatial.transform import Rotation
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT.parent / 'as01-bambusa-vulgaris-golden-bamboo/source/primary upload/primary upload'
WORK = ROOT / 'Tools/Scene3/BambusaImported'
TEXTURES = ROOT / 'Assets/_Project/Art/Textures/Scene3/BambooImported'
sys.path.insert(0, str(ROOT / '.unity-validation/bamboo-python'))


class Fbx:
    def __init__(self, path):
        self.data = path.read_bytes()
        if not self.data.startswith(b'Kaydara FBX Binary'):
            raise ValueError('Expected binary FBX')
        self.version = struct.unpack_from('<I', self.data, 23)[0]
        self.wide = self.version >= 7500
        self.roots = []
        pos = 27
        while pos < len(self.data):
            node, pos = self.node(pos)
            if node is None:
                break
            self.roots.append(node)
        objects = self.child(self.roots, 'Objects')[2]
        self.models = {n[1][0]: n for n in objects if n[0] == 'Model'}
        self.geometries = {n[1][0]: n for n in objects if n[0] == 'Geometry'}
        self.materials = {n[1][0]: n for n in objects if n[0] == 'Material'}
        connections = self.child(self.roots, 'Connections')[2]
        self.parents = {n[1][1]: n[1][2] for n in connections if n[0] == 'C' and n[1][0] == 'OO'}
        self.settings = self.properties(self.child(self.roots, 'GlobalSettings'))

    @staticmethod
    def child(nodes, name):
        return next((n for n in nodes if n[0] == name), (name, [], []))

    @staticmethod
    def properties(node):
        props = Fbx.child(node[2], 'Properties70')[2]
        return {p[1][0]: p[1][4:] for p in props if p[0] == 'P'}

    def prop(self, pos):
        t = chr(self.data[pos]); pos += 1
        scalar = {'Y': 'h', 'C': '?', 'I': 'i', 'F': 'f', 'D': 'd', 'L': 'q'}
        if t in scalar:
            fmt = '<' + scalar[t]
            return struct.unpack_from(fmt, self.data, pos)[0], pos + struct.calcsize(fmt)
        if t in 'SR':
            size = struct.unpack_from('<I', self.data, pos)[0]; pos += 4
            raw = self.data[pos:pos + size]
            return raw.decode('utf8', 'replace') if t == 'S' else None, pos + size
        if t in 'fdilbc':
            count, encoding, size = struct.unpack_from('<III', self.data, pos); pos += 12
            return ('array', t, count, encoding, pos, size), pos + size
        raise ValueError((t, pos))

    def node(self, pos):
        end, count, _ = struct.unpack_from('<QQQ' if self.wide else '<III', self.data, pos)
        pos += 24 if self.wide else 12
        length = self.data[pos]; pos += 1
        if not end:
            return None, pos
        name = self.data[pos:pos + length].decode(); pos += length
        props = []
        for _ in range(count):
            value, pos = self.prop(pos); props.append(value)
        children = []
        while pos < end - (25 if self.wide else 13):
            child, pos = self.node(pos)
            if child is None:
                break
            children.append(child)
        return (name, props, children), end

    def array(self, descriptor):
        _, typ, count, encoding, offset, size = descriptor
        raw = self.data[offset:offset + size]
        if encoding:
            raw = zlib.decompress(raw)
        return np.frombuffer(raw, dtype={'d': '<f8', 'f': '<f4', 'i': '<i4', 'l': '<i8', 'b': 'u1', 'c': 'u1'}[typ], count=count)

    def values(self, node, name):
        return self.array(self.child(node[2], name)[1][0])

    def matrix(self, model_id):
        node = self.models[model_id]; p = self.properties(node)
        def translation(value):
            m = np.eye(4); m[:3, 3] = value; return m
        def rotation(value):
            m = np.eye(4); m[:3, :3] = Rotation.from_euler('xyz', value, degrees=True).as_matrix(); return m
        # Fail explicitly for uncommon pivots/order rather than silently moving geometry.
        for key in ['RotationPivot', 'ScalingPivot', 'RotationOffset', 'ScalingOffset']:
            if np.linalg.norm(p.get(key, [0, 0, 0])) > 1e-5:
                raise ValueError(f'Unsupported nonzero {key} on {node[1][1]}')
        if p.get('RotationOrder', [0])[0] != 0:
            raise ValueError('Unsupported FBX Euler rotation order')
        scale = np.eye(4); scale[:3, :3] = np.diag(p.get('Lcl Scaling', [1, 1, 1]))
        local = translation(p.get('Lcl Translation', [0, 0, 0])) @ rotation(p.get('PreRotation', [0, 0, 0])) @ rotation(p.get('Lcl Rotation', [0, 0, 0])) @ np.linalg.inv(rotation(p.get('PostRotation', [0, 0, 0]))) @ scale
        parent = self.parents.get(model_id)
        world = self.matrix(parent) @ local if parent in self.models else local
        geom_scale = np.eye(4); geom_scale[:3, :3] = np.diag(p.get('GeometricScaling', [1, 1, 1]))
        return world @ translation(p.get('GeometricTranslation', [0, 0, 0])) @ rotation(p.get('GeometricRotation', [0, 0, 0])) @ geom_scale

    def group(self, index):
        group_id = next(i for i, n in self.models.items() if n[1][1].split('\x00')[0] == f'ABC_AS01_BambusaVulgaris_{index}.001')
        result = []
        for gid, geo in self.geometries.items():
            mid = self.parents[gid]
            if self.parents.get(mid) != group_id:
                continue
            result.append((self.models[mid], geo, self.matrix(mid)))
        return result


def decode_geometry(fbx, model, geo, matrix):
    cp = fbx.values(geo, 'Vertices').reshape(-1, 3)
    cp = cp @ matrix[:3, :3].T + matrix[:3, 3]
    raw = fbx.values(geo, 'PolygonVertexIndex')
    indices = np.where(raw < 0, -raw - 1, raw)
    ends = np.flatnonzero(raw < 0)
    starts = np.r_[0, ends[:-1] + 1]
    corner_tris = np.array([(s, k, k + 1) for s, e in zip(starts, ends) for k in range(s + 1, e)], dtype=np.int32)
    corners = corner_tris.reshape(-1)
    cp_indices = indices[corners]
    def layer(name, data, item_size, index_name):
        node = fbx.child(geo[2], name)
        mapping = fbx.child(node[2], 'MappingInformationType')[1][0]
        reference = fbx.child(node[2], 'ReferenceInformationType')[1][0]
        address = corners if mapping == 'ByPolygonVertex' else cp_indices if mapping in ['ByVertice', 'ByVertex'] else None
        if address is None:
            raise ValueError(f'Unsupported {name} mapping {mapping}')
        if reference == 'IndexToDirect':
            address = fbx.values(node, index_name)[address]
        elif reference != 'Direct':
            raise ValueError(reference)
        return fbx.values(node, data).reshape(-1, item_size)[address].copy()
    uv = layer('LayerElementUV', 'UV', 2, 'UVIndex')
    normal = layer('LayerElementNormal', 'Normals', 3, 'NormalsIndex')
    normal = normal @ np.linalg.inv(matrix[:3, :3])
    normal /= np.maximum(np.linalg.norm(normal, axis=1, keepdims=True), 1e-12)
    # FBX scene basis to Unity: choose the declared up axis and keep a left-handed basis.
    up = int(fbx.settings['UpAxis'][0]); sign = int(fbx.settings['UpAxisSign'][0])
    if up == 1:
        order = [0, 1, 2]; signs = np.array([1, sign, -1])
    elif up == 2:
        order = [0, 2, 1]; signs = np.array([1, sign, 1])
    else:
        raise ValueError(f'Unexpected source up axis: {up}')
    vertices = cp[cp_indices][:, order] * signs
    normal = normal[:, order] * signs
    faces = np.arange(len(corners)).reshape(-1, 3)
    faces = faces[:, [0, 2, 1]]
    name = model[1][1].split('\x00')[0]
    material = 0 if name.startswith('Bark') else 1 if name.startswith('Branch') else int(name[4:6]) + 1
    return dict(name=name, vertices=vertices, normal=normal, uv=uv, faces=faces, material=material)


def leaf_components(part):
    _, inverse = np.unique(np.round(part['vertices'], 6), axis=0, return_inverse=True)
    faces = inverse[part['faces']]
    edges = np.concatenate([faces[:, [0, 1]], faces[:, [1, 2]], faces[:, [2, 0]]])
    size = int(inverse.max()) + 1
    graph = coo_matrix((np.ones(len(edges)), (edges[:, 0], edges[:, 1])), shape=(size, size)).tocsr()
    _, labels = connected_components(graph, directed=False)
    face_labels = labels[faces[:, 0]]
    ids = np.unique(face_labels)
    groups = [np.flatnonzero(face_labels == i) for i in ids]
    rng = np.random.default_rng(7919 + part['material'] * 101)
    order = rng.permutation(len(groups)).tolist()
    centers = np.array([part['vertices'][part['faces'][g]].mean(axis=(0, 1)) for g in groups])
    extremes = set(np.argmin(centers, axis=0).tolist() + np.argmax(centers, axis=0).tolist())
    part['components'] = [groups[i] for i in order if i in extremes] + [groups[i] for i in order if i not in extremes]


def reduce_part(part, budget, lod):
    # Keep the same leaf cards and sizes at every LOD. Reduce only their
    # surface tessellation; removing/enlarging whole cards changes the canopy.
    if part['material'] >= 2:
        return part
    if len(part['faces']) <= budget:
        return part
    import fast_simplification
    points, unique_indices, inverse = np.unique(np.round(part['vertices'], 6), axis=0, return_index=True, return_inverse=True)
    faces = inverse[part['faces']].astype(np.int32)
    faces = faces[(faces[:, 0] != faces[:, 1]) & (faces[:, 1] != faces[:, 2]) & (faces[:, 2] != faces[:, 0])]
    points, faces = fast_simplification.simplify(points, faces, target_count=max(20, int(budget)), agg=5)
    if len(faces) > budget * 1.15:
        # Source fine branches include non-manifold joins that bound QEM reduction.
        # Cluster only these rigid details; leaf UV cards never pass through this step.
        original_points, original_faces = points, faces
        lo, hi = 1e-6, np.ptp(points, axis=0).max() * .04
        best = None
        for _ in range(22):
            cell = (lo + hi) * .5
            _, inverse = np.unique(np.floor(original_points / cell).astype(np.int32), axis=0, return_inverse=True)
            counts = np.bincount(inverse)
            merged = np.array([np.bincount(inverse, weights=original_points[:, axis]) / counts for axis in range(3)]).T
            triangles = inverse[original_faces]
            valid = (triangles[:, 0] != triangles[:, 1]) & (triangles[:, 1] != triangles[:, 2]) & (triangles[:, 2] != triangles[:, 0])
            triangles = triangles[valid]
            _, unique = np.unique(np.sort(triangles, axis=1), axis=0, return_index=True)
            triangles = triangles[unique]
            if len(triangles) <= budget and len(triangles) > 0:
                best = merged, triangles; hi = cell
            else:
                lo = cell
        if best is not None:
            points, faces = best
    _, closest = cKDTree(part['vertices']).query(points)
    normals = part['normal'][closest]
    return dict(part, vertices=points, faces=faces, normal=normals, uv=part['uv'][closest])


def resample_leaf_cards(part, segments):
    """Retain whole textured leaves, reducing the source's 3x9 control grid first."""
    vertices = []; normals = []; coords = []; faces = []
    for group in part['components']:
        ids = np.unique(part['faces'][group])
        uv = part['uv'][ids]; pos = part['vertices'][ids]; normal = part['normal'][ids]
        u0, u1 = uv[:, 0].min(), uv[:, 0].max()
        v0, v1 = uv[:, 1].min(), uv[:, 1].max()
        offset = len(vertices)
        for v in np.linspace(v0, v1, segments + 1):
            for u in [u0, u1]:
                column = np.flatnonzero(np.abs(uv[:, 0] - u) < 1e-5)
                order = column[np.argsort(uv[column, 1])]
                point = np.array([np.interp(v, uv[order, 1], pos[order, axis]) for axis in range(3)])
                n = np.array([np.interp(v, uv[order, 1], normal[order, axis]) for axis in range(3)])
                n /= max(np.linalg.norm(n), 1e-12)
                vertices.append(point); normals.append(n); coords.append([u, v])
        for row in range(segments):
            a = offset + row * 2
            for tri in [[a, a + 2, a + 1], [a + 1, a + 2, a + 3]]:
                x, y, z = (vertices[i] for i in tri)
                if np.dot(np.cross(y - x, z - x), normals[tri[0]]) < 0:
                    tri[1], tri[2] = tri[2], tri[1]
                faces.append(tri)
    result = dict(part, vertices=np.array(vertices), normal=np.array(normals), uv=np.array(coords), faces=np.array(faces, dtype=np.int32))
    leaf_components(result)
    return result


def build_group(index, parts, entry):
    units = .01
    bark = np.concatenate([p['vertices'] for p in parts if p['material'] == 0])
    low = bark[:, 1].min(); height = entry['bounds_max'][1] - low
    foot = bark[bark[:, 1] <= low + height * .02].mean(axis=0); foot[1] = low
    for part in parts:
        part['vertices'] = (part['vertices'] - foot) * units
        if part['material'] >= 2:
            leaf_components(part)
    source_height = max(p['vertices'][:, 1].max() for p in parts)
    source_points = np.concatenate([p['vertices'] for p in parts])
    bounds = np.r_[source_points.min(axis=0), source_points.max(axis=0)].astype('<f4')
    totals = {kind: sum(len(p['faces']) for p in parts if (p['material'] >= 2) == (kind == 'leaf')) for kind in ['leaf', 'wood']}
    entry['native_height_metres'] = float(source_height)
    entry['lods'] = []
    for lod, target in enumerate([25000, 10000, 3500]):
        full = sum(totals.values()) <= target
        records = []
        for part in parts:
            kind = 'leaf' if part['material'] >= 2 else 'wood'
            original_count = len(part['faces'])
            if kind == 'leaf' and not full:
                part = resample_leaf_cards(part, (3, 2, 1)[lod])
            allowance = len(part['faces']) if full else max(12, round(target * (.75 if kind == 'leaf' else .25) * original_count / totals[kind]))
            # Keep the modest main culms intact; simplify fine branching separately.
            if part['material'] == 0:
                allowance = len(part['faces'])
            reduced = reduce_part(part, allowance, lod)
            indices = reduced['faces'].reshape(-1)
            v = reduced['vertices'][indices]
            colors = np.ones((len(indices), 4))
            colors[:, 0] = np.clip(v[:, 1] / source_height, 0, 1) ** 1.5 * (1 if kind == 'leaf' else .12)
            colors[:, 1] = 1 if kind == 'leaf' else 0
            colors[:, 2] = part['material']
            records.append(np.concatenate([v, reduced['normal'][indices], reduced['uv'][indices], colors], axis=1))
        all_records = np.concatenate(records).astype('<f4')
        if not np.isfinite(all_records).all():
            raise ValueError('Non-finite mesh data')
        vertices, indices = np.unique(all_records, axis=0, return_inverse=True)
        faces = indices.reshape(-1, 3).astype('<i4')
        path = WORK / f'Bambusa_{index:02d}_LOD{lod}.asset'
        with path.open('wb') as stream:
            stream.write(b'BAM1' + struct.pack('<IIII', index, lod, len(vertices), len(faces)))
            stream.write(bounds.tobytes()); stream.write(vertices.tobytes()); stream.write(faces.tobytes())
        entry['lods'].append(dict(lod=lod, triangles=len(faces), vertices=len(vertices), file=path.name))
        print('EXPORTED', path.name, len(faces), 'triangles', flush=True)


def pack_textures():
    TEXTURES.mkdir(parents=True, exist_ok=True)
    names = ['Bark01', 'Branch01', 'Leaf01', 'Leaf02', 'Leaf03', 'Leaf04']
    for slot, name in enumerate(names):
        for side in ['Front', 'Back']:
            filename = f'AS01_{name}{side if slot >= 2 else ""}_dif_su.png'
            color = Image.open(SOURCE / filename).convert('RGBA')
            if slot >= 2:
                alpha = Image.open(SOURCE / f'AS01_{name}_alp_su.png').convert('L')
                color.putalpha(alpha)
            color.save(TEXTURES / f'{slot}_{side}.png')
        Image.open(SOURCE / f'AS01_{name}_nml_su.png').convert('RGBA').save(TEXTURES / f'{slot}_Normal.png')
    print('Packed diffuse+alpha for six front/back layers; copied source normals. Originals untouched.', flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--stage', choices=['inspect', 'build', 'textures'], default='inspect')
    parser.add_argument('--groups', default='1')
    args = parser.parse_args()
    WORK.mkdir(parents=True, exist_ok=True)
    if args.stage == 'textures':
        pack_textures(); return
    fbx = Fbx(SOURCE / 'BambusaVulgaris-Master.fbx')
    if abs(float(fbx.settings['UnitScaleFactor'][0]) - 1) > 1e-6:
        raise ValueError('This extraction recipe expects the inspected centimetre-based source')
    report = {'fbx_version': fbx.version, 'settings': fbx.settings, 'groups': []}
    for index in map(int, args.groups.split(',')):
        parts = [decode_geometry(fbx, *part) for part in fbx.group(index)]
        points = np.concatenate([p['vertices'] for p in parts]); lo = points.min(axis=0); hi = points.max(axis=0)
        entry = dict(group=index, bounds_min=lo.tolist(), bounds_max=hi.tolist(), parts=[dict(name=p['name'], triangles=len(p['faces']), material=p['material']) for p in parts])
        report['groups'].append(entry)
        print(json.dumps(entry), flush=True)
        if args.stage == 'build':
            build_group(index, parts, entry)
    (WORK / (args.stage + '.json')).write_text(json.dumps(report, indent=2), encoding='utf8')


if __name__ == '__main__':
    main()
