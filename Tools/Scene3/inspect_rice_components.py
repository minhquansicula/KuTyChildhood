import bpy
import collections
import json
from pathlib import Path

source = Path(__file__).resolve().parents[3] / "BLENDER/caylua_nhe.blend"
bpy.ops.wm.open_mainfile(filepath=str(source))
mesh = bpy.data.objects["CayLuaNhe_1Cay"].data
adjacency = [[] for _ in mesh.vertices]
for edge in mesh.edges:
    a, b = edge.vertices
    adjacency[a].append(b)
    adjacency[b].append(a)
seen, components, vertex_component = set(), [], {}
for vertex in mesh.vertices:
    if vertex.index in seen:
        continue
    stack, ids = [vertex.index], []
    seen.add(vertex.index)
    while stack:
        index = stack.pop()
        ids.append(index)
        for neighbor in adjacency[index]:
            if neighbor not in seen:
                seen.add(neighbor)
                stack.append(neighbor)
    component = len(components)
    components.append(ids)
    for index in ids:
        vertex_component[index] = component
polygons = collections.defaultdict(list)
for polygon in mesh.polygons:
    polygons[vertex_component[polygon.vertices[0]]].append(polygon)
summary = []
for component, ids in enumerate(components):
    ps = polygons[component]
    summary.append(dict(component=component, first_vertex=min(ids), vertices=len(ids),
                        triangles=sum(len(p.vertices)-2 for p in ps), material=ps[0].material_index,
                        center=[round(sum(mesh.vertices[i].co[a] for i in ids)/len(ids), 3) for a in range(3)]))
print("FIRST_COMPONENTS", json.dumps(summary[:28]))
print("PANICLE_COMPONENTS", json.dumps([s for s in summary if s["material"] >= 3 and s["vertices"] != 8]))
print("GRAIN_SAMPLES", json.dumps([s for s in summary if s["vertices"] == 8][::50]))
