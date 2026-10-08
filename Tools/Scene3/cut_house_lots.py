"""Create five reversible rice-mesh variants with space for the scene-3 homes.

Run with Python before Scene3RuralHousePlacement.Place in Unity. The original
rice OBJ assets remain untouched, so existing field art can be restored.
"""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "Assets/_Project/Art/Models/Scene3"
OUTPUT_DIR = MODEL_DIR / "RuralHouseClearings"

# (source rice mesh, house world X, house world Z). OBJ X is flipped by Unity.
LOTS = [
    ("LuaNuoc_-1_-9", -18.0, -9.0),
    ("LuaNuoc_1_8", 18.0, 8.0),
    ("LuaNuoc_-1_27", -18.0, 27.0),
    ("LuaNuoc_1_65", 18.0, 65.0),
    ("LuaNuoc_-1_84", -18.0, 84.0),
]
LOT_HALF_X = 6.8
LOT_HALF_Z = 5.8
PATH_HALF_Z = 0.95


def cut(source_name: str, house_x: float, house_z: float) -> None:
    source = MODEL_DIR / f"{source_name}.obj"
    lines = source.read_text(encoding="utf-8").splitlines()
    positions = [tuple(map(float, line.split()[1:4])) for line in lines if line.startswith("v ")]
    vertices = [line for line in lines if line.startswith("v ")]
    normals = [line for line in lines if line.startswith("vn ")]
    faces = [line for line in lines if line.startswith("f ")]
    if len(positions) % 4 or len(faces) % 4:
        raise ValueError(f"Unexpected rice topology in {source}")

    keep = []
    removed = 0
    for start in range(0, len(faces), 4):
        group = faces[start:start + 4]
        indexes = {int(token.split("/")[0]) for face in group for token in face.split()[1:]}
        if len(indexes) != 4:
            raise ValueError(f"Unexpected rice-leaf group at face {start} in {source}")
        world_x = -sum(positions[i - 1][0] for i in indexes) / 4
        world_z = sum(positions[i - 1][2] for i in indexes) / 4
        in_lot = abs(world_x - house_x) < LOT_HALF_X and abs(world_z - house_z) < LOT_HALF_Z
        if house_x < 0:
            in_path = house_x + LOT_HALF_X <= world_x <= -4.25
        else:
            in_path = 4.25 <= world_x <= house_x - LOT_HALF_X
        if in_lot or (in_path and abs(world_z - house_z) < PATH_HALF_Z):
            removed += 1
        else:
            keep.extend(group)

    used_v = sorted({int(token.split("/")[0]) for face in keep for token in face.split()[1:]})
    used_n = sorted({int(token.split("//")[1]) for face in keep for token in face.split()[1:]})
    v_map = {old: new for new, old in enumerate(used_v, 1)}
    n_map = {old: new for new, old in enumerate(used_n, 1)}
    output = [f"o {source_name}_NhaDan"]
    output.extend(vertices[i - 1] for i in used_v)
    output.extend(normals[i - 1] for i in used_n)
    for face in keep:
        tokens = face.split()[1:]
        output.append("f " + " ".join(
            f"{v_map[int(t.split('//')[0])]}//{n_map[int(t.split('//')[1])]}" for t in tokens
        ))
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    target = OUTPUT_DIR / f"{source_name}_NhaDan.obj"
    target.write_text("\n".join(output) + "\n", encoding="utf-8")
    print(f"{source_name}: removed {removed} rice-leaf groups -> {target.name}")


if __name__ == "__main__":
    for lot in LOTS:
        cut(*lot)
