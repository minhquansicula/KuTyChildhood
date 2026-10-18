"""Check saved hierarchy and retained house transforms, without launching Unity."""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[2]
BEFORE = ROOT / "Tools/Scene3/VillageComposition/before-memory-landscape.unity"
AFTER = ROOT / "Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity"


def scan(path):
    text = path.read_text(encoding="utf-8-sig")
    blocks = re.split(r"(?m)^--- !u!(\d+) &(\d+)[^\n]*\n", text)
    docs = {int(blocks[i + 1]): (int(blocks[i]), blocks[i + 2]) for i in range(1, len(blocks), 3)}
    objects, transforms, renderers = {}, {}, []
    for file_id, (kind, body) in docs.items():
        if kind == 1:
            name = re.search(r"(?m)^  m_Name: (.*)$", body)
            if not name:
                continue  # stripped prefab records resolve through their instance
            objects[file_id] = {
                "name": name[1],
                "active": "  m_IsActive: 1" in body,
            }
        elif kind in (4, 224):
            if not re.search(r"m_GameObject: \{fileID: (\d+)\}", body):
                continue
            go = int(re.search(r"m_GameObject: \{fileID: (\d+)\}", body)[1])
            parent = int(re.search(r"m_Father: \{fileID: (\d+)\}", body)[1])
            transforms[file_id] = (go, parent)
        elif kind in (23, 137, 199, 120):
            if not re.search(r"m_GameObject: \{fileID: (\d+)\}", body):
                continue
            go = int(re.search(r"m_GameObject: \{fileID: (\d+)\}", body)[1])
            if "  m_Enabled: 1" in body:
                renderers.append(go)
    go_to_transform = {go: key for key, (go, _) in transforms.items()}

    def active(go):
        if go not in objects or go not in go_to_transform:
            return False
        if not objects[go]["active"]:
            return False
        parent = transforms[go_to_transform[go]][1]
        return not parent or active(transforms[parent][0])

    return docs, objects, transforms, sum(active(go) for go in renderers)


def main():
    old, old_objects, old_transforms, old_count = scan(BEFORE)
    new, new_objects, new_transforms, new_count = scan(AFTER)
    houses = []
    for key, obj in old_objects.items():
        if not re.fullmatch(r"Nha0[1-5]_Ngoi\w+_HienTon", obj["name"]) or not obj["active"]:
            continue
        # Exact file identities and uniform scales must survive the composition.
        assert key in new_objects and new_objects[key]["name"] == obj["name"]
        for t_id, (go, _) in old_transforms.items():
            if go != key:
                continue
            scale = re.search(r"m_LocalScale: (.*)", old[t_id][1])[1]
            assert re.search(r"m_LocalScale: (.*)", new[t_id][1])[1] == scale
        houses.append(obj["name"])
    for key, (kind, body) in old.items():
        if kind != 1001:
            continue
        match = re.search(r"propertyPath: m_Name\n\s+value: (Nha0[1-5]_Ngoi[^\n]+)", body)
        if not match or not re.fullmatch(r"Nha0[1-5]_Ngoi\w+_HienTon", match[1]):
            continue
        assert key in new
        for axis in "xyz":
            pattern = rf"propertyPath: m_LocalScale\.{axis}\n\s+value: ([^\n]+)"
            old_scale, new_scale = re.search(pattern, body), re.search(pattern, new[key][1])
            assert bool(old_scale) == bool(new_scale)
            if old_scale:
                assert old_scale[1] == new_scale[1]
        houses.append(match[1])
    assert len(set(houses)) == 5
    assert any(obj["name"] == "LangQue_HoiUc" and obj["active"] for obj in new_objects.values())
    result = {
        "passed": True,
        "house_root_ids_and_scales_preserved": sorted(set(houses)),
        "enabled_renderers_under_active_hierarchy_before": old_count,
        "enabled_renderers_under_active_hierarchy_after": new_count,
        "note": "Counts cover explicit scene renderers, excluding stripped prefab records. They are not draw calls or FPS; LOD/frustum culling still applies.",
    }
    output = ROOT / "Tools/Scene3/VillageComposition/memory_saved_scene_report.json"
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
