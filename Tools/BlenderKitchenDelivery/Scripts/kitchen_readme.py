import pathlib,json
OUT=pathlib.Path('E:/FPT/FALL26/PRU213/KuTy/Tools/BlenderKitchenDelivery')
m=json.loads((OUT/'asset_manifest.json').read_text(encoding='utf8'))
rows=[]
for name,v in m.items():
 dims=' × '.join(f'{x:.3f}' for x in v['dimensions_xyz_m']);rows.append(f"| `{name}` | {dims} | {v['triangles']:,} | {len(v['materials'])} |")
hier=[]
for name,v in m.items():
 entries={e['name']:e for e in v['hierarchy']};children={}
 for e in entries.values():children.setdefault(e['parent'],[]).append(e['name'])
 def tree(n,depth=0):
  lines=['  '*depth+n]
  for c in sorted(children.get(n,[])):lines+=tree(c,depth+1)
  return lines
 hier+=['### '+name,'```text','\n'.join(tree(name)),'```','']
textures=sorted(p.name for p in (OUT/'Textures').glob('*.png'))
text='''# Bộ asset bếp quê — Chìa Khóa Ký Ức

Đã dựng trong Blender 5.2.2 LTS qua máy chủ MCP cục bộ. Tham chiếu: `Scene2Kitchen/wash-reference.png` và `fire-reference.png` của project. Đơn vị tác giả: **mét**, Blender Z-up. Các FBX có Empty root, mesh rời và anchor, không cần animation để dùng.

## Tệp bàn giao

- `KitchenAssets.blend`: bộ model, vật liệu ảnh, các collection theo asset; camera/đèn/phông trưng bày.
- `Exports/`: 11 FBX độc lập. Mỗi asset được đưa về gốc tọa độ khi xuất; không xuất camera, đèn hoặc phông.
- `Textures/`: PNG BaseColor / Normal / Roughness / Metallic và bản **MetallicSmoothness** đóng kênh cho URP.
- `Previews/`: ảnh render kiểm tra thật, gồm overview, các trạng thái tương tác và cận cảnh.
- `Progress/`: các bản lưu sau từng asset và bước hiệu chỉnh. Bản cuối ở thư mục gốc là bản chính thức.
- `asset_manifest.json`: kích thước, hierarchy, transform/pivot/anchor ở trạng thái nghỉ và số triangles thực tế.
- `Validation.json`: kết quả kiểm tra mesh và **xuất FBX rồi import lại** bằng Blender, từng asset.
- `RelocationValidation.json`: kiểm tra mở bản sao `.blend` + `Textures` ở thư mục khác.
- `Scripts/`: script dựng, xuất và kiểm tra để truy vết công việc; không cần để chạy asset trong Unity.

## Kích thước và hiệu năng

Bao hình đo ở trạng thái nghỉ, X × Y × Z, đơn vị mét; nồi tính cả quai, nồi cơm tính cả vung/núm. Số triangles tính toàn bộ mesh gameplay, không tính Empty.

| Asset | Bao hình X × Y × Z (m) | Triangles | Material |
|---|---|---:|---:|
'''+ '\n'.join(rows)+'''

Mỗi mesh đã có UV0. Các bộ phận cùng chất liệu dùng chung texture/material. UV quay tròn dùng seam ở phía khuất; mesh gạo gộp giữ các cụm hạt trong cùng object. Một số UV của các chi tiết nhỏ dùng tile/overlap có chủ đích để chia sẻ texture; **không phải UV lightmap**. Trong Unity bật Generate Lightmap UVs nếu cần bake GI, hoặc dùng ánh sáng realtime. Nia + ống thổi nằm trong ngân sách tổng 6.000 triangles. Không có scale âm; scale mesh/pivot được kiểm tra trong manifest/Validation.

## Pivot, anchor và điều khiển

### Quy ước chung

- Root của từng asset nằm tại tâm chân đế trên sàn; hình học được hiệu chỉnh để đáy chạm Z=0. Root trong file `.blend` có offset trưng bày; trong FBX root ở (0,0,0).
- Các vật cần cầm ở giữa/thân có `Grip_Pivot` con. **Di chuyển/xoay `Grip_Pivot` khi cầm**, dùng root làm mốc đặt xuống sàn. Cách này giữ đồng thời root chân đế và pivot tương tác.
- Các trục dưới đây mô tả trong **Blender**. FBX chuyển trục thành Y-up; trong Unity ưu tiên `TransformDirection`, anchor và `slot.forward`, không hard-code Blender Z thành Unity Z.
- Blender yêu cầu tên object toàn cục duy nhất nên có thể có `.001` trong file chung; exporter bỏ hậu tố số và giữ đúng tên con như cây FBX ở cuối tài liệu. Dùng `Find()` tương đối theo root, không tìm tên toàn scene.

### Nồi vo gạo

- `Tilt_Pivot`: gần mép đáy phía -Y (phía chắt nước), xoay **local X +20–25°** để nghiêng về -Y. Ảnh thử dùng +23°. Thân, hai quai, gạo, nước và ba anchor cùng nằm dưới pivot.
- `Drain_Anchor` ở mép miệng -Y; `Stir_Anchor` giữa mực nước mặc định; `Grip_Anchor` trên quai trái gần người chơi.
- `Rice_Grains` là nền gạo kín + các hạt thuôn gộp một mesh; không có hàng nghìn object hạt.
- `Water_Surface` là mặt đơn riêng, normal hướng lên; origin ở tâm mặt nước. Đổi local position/scale và material trong Unity để hạ mực nước/đổi màu/alpha. Mực mặc định khoảng 0,12m; nếu hạ thấp dưới khoảng 0,10m, đồng thời thu bán kính cho phù hợp lòng nồi. Khi thay mực nước, cập nhật `Stir_Anchor` tương ứng.
- Nước hiện là **mặt phẳng tĩnh**, không phải mô phỏng chất lỏng. Khi nghiêng, code/shader Unity cần giữ mặt nước ngang, clip theo lòng nồi và giảm thể tích nếu muốn hiệu ứng chắt chân thực. Cấu trúc mesh/anchor đã tách sẵn cho việc này.

### Nồi cơm

- `Lid_Pivot` ở tâm vung; vung và núm cùng là con của pivot. Nâng ít nhất **0,12m**, sau đó dịch ngang ít nhất **0,40m**; kiểm tra mẫu dùng nâng khoảng 0,22m và dịch sang phải 0,57m. Với tư thế đặt sát sàn, tính điểm thấp nhất sau xoay để vung chạm sàn.
- `CookedRice` là mesh độc lập; mở vung nhìn thấy cơm. `Steam_Anchor` gần miệng nồi.
- `LidGrip_Anchor` là con trực tiếp của root theo cây yêu cầu: đánh dấu vị trí núm ở trạng thái đóng. Khi tương tác, cache `lid.InverseTransformPoint(anchor.position)` rồi lấy `lid.TransformPoint(offset)` để điểm cầm đi theo vung, hoặc reparent anchor vào vung trong prefab.
- Nồi khớp ba gối đỡ của bếp. Đặt root nồi tại `PotRest_Anchor` của bếp; đây là vị trí đặt đã hiệu chỉnh theo độ cong đáy.

### Bếp và củi

- Mặt trước bếp là -Y Blender; khoang lò mở thật, không có mặt bịt phía trước. Lửa/khói/nồi không nằm trong FBX bếp.
- `Hearth_Anchor`: tâm tương tác trong khoang. `Fire_Anchor`, `Smoke_Anchor`: chỗ gắn VFX, chỉ là Empty.
- `WoodSlot_01..03`: local **+Z hướng vào lò (+Y Blender)**. Trong Unity `slot.forward` là hướng đẩy; dịch ngược hướng đó để rút. Ba slot có khoảng cách ngang để củi không chồng nhau.
- Cả năm thanh củi có trục dọc **+Y Blender**, đầu nạp là **+Y**. `Feed_Anchor` tại đầu này, local +Z cùng hướng nạp. Align hướng của Feed_Anchor với `WoodSlot.forward`, rồi đẩy/rút bằng offset dọc slot. `Grip_Pivot` ở gần giữa thanh để cầm, root là mốc sàn.
- 01 thanh nguyên; 02 bổ đôi, lộ mặt chẻ; 03 hơi cong; 04 đầu gãy nham nhở; 05 thanh chẻ tiết diện khác. Vỏ, gỗ chẻ và mặt cắt có material riêng.
- `Ember_Bed` có material `M_Ember`, emission tách riêng, thay `_EmissionColor`/intensity bằng MaterialPropertyBlock trong Unity. `Ash_Bed` gồm nền tro + các cục than nguội, không chứa hiệu ứng lửa.

### Vá, ống thổi, nia

- Vá: dài khoảng 0,25m; `Grip_Pivot` gần cuối cán, đầu vá có thể tích và cạnh tròn.
- Ống thổi: dài 0,40m, đường kính xấp xỉ 0,036–0,040m tính đốt; hai đầu rỗng xuyên suốt. `Grip_Pivot` gần đầu cầm; `Mouth_Anchor` ở -Y, `AirOut_Anchor` ở +Y, local +Z chỉ hướng luồng khí.
- Nia: vỏ kín có chiều dày và lòng lõm; vành + dây buộc dùng geometry, phần đan nhỏ dùng BaseColor/Normal. Không dựa vào shader procedural khi chạy game.

## Texture và Unity URP Lit

- Nhôm và gạch/trát bếp: **2048²**. Gạo, tro/than, gỗ, tre, nia: **1024²**. Các map được raster/bake từ hàm bề mặt procedural sang ảnh UV, không có node Noise/Bump procedural bắt buộc ở runtime.
- BaseColor không bake ánh sáng studio, bóng đổ hoặc highlight cố định. Roughness và normal mang vết xước, thớ, đan tre và độ nhám. Đây là bộ texture procedural hoàn chỉnh ở mức asset được bàn giao; chưa có lớp vẽ tay riêng từng vết bẩn hay ảnh scan vật liệu.
- Normal: **tangent-space OpenGL (+Y/green up)**. Unity: Texture Type = Normal map, không Flip Green mặc định. Normals và UV được xuất, tangent FBX bật; có thể chọn Tangents = Calculate Mikktspace trong Unity.
- BaseColor: sRGB ON. Normal/Roughness/Metallic/MetallicSmoothness: sRGB OFF. Max Size 2048 hoặc 1024 theo file; bật mipmaps, anisotropic 4 cho nia/nền.
- URP Lit: BaseColor → Base Map; Normal → Normal Map; `_MetallicSmoothness` → Metallic Map. Map này có **RGB = Metallic, A = 1 − Roughness**; Smoothness Source = Metallic Alpha, hệ số Smoothness = 1. Map Roughness riêng dành cho shader khác/điều chỉnh, URP Lit không có slot roughness trực tiếp.
- Khi import FBX, tạo/extract material URP Lit rồi remap theo tên `M_*`; FBX không đóng gói shader URP tự động. Màu vật liệu trong Blender chỉ là preview; Unity sẽ tự chiếu sáng.
- `M_RiceWater`: URP Lit Transparent, Alpha blend, alpha ban đầu 0,33, màu hơi đục, roughness khoảng 0,15 (smoothness 0,85), IOR tham chiếu 1,333. URP Lit chuẩn không có khúc xạ thể tích: bổ sung shader nước nếu gameplay cần.
- `M_Ember`: bật Emission màu cam đỏ, điều khiển cường độ theo gameplay; không bake ánh sáng từ than vào BaseColor.
- Nước dùng thông số hằng, không cần ảnh; emission cũng là thông số material. Không có texture giả/ảnh bị thiếu bắt buộc. Các map Metallic=0 cho phi kim được giữ để thống nhất bộ map.
- Duy trì cấu trúc `Exports/` cạnh `Textures/` khi chuyển thư mục. `.blend` dùng đường dẫn tương đối `//Textures/...`; FBX dùng đường dẫn tương đối tới `../Textures/...`.

## Thiết lập export thực tế

Blender FBX binary, Selected Objects, Object Types Mesh + Empty; Global Scale **1.0**; Apply Unit Scale ON; Apply Scalings **FBX Units Scale** (`FBX_SCALE_UNITS`); Forward **-Z**, Up **Y**; scene unit scale 1m. Apply Transform/bake_space_transform OFF; dùng transform của hierarchy để giữ origin. Mesh modifiers ON, triangulate ON, smoothing FACE, tangent space ON; animation OFF; leaf bones OFF; Path Mode RELATIVE, Embed Textures OFF.

Trong Unity: Scale Factor 1, Convert Units bật (Use File Scale nếu phiên bản Unity hiển thị), Bake Axis Conversion tùy phiên bản nhưng không thêm xoay thủ công lên các pivot; kiểm tra bounds bằng manifest. Không bật Optimize Game Objects làm mất anchor. Đây là bàn giao model/texture, không chứa animation, particle, gameplay script, collider hay prefab bắt buộc.

## Kiểm tra đã thực hiện

- Render và xem ảnh sau từng asset; hiệu chỉnh hình học, vật liệu, đường dẫn và mặt cắt củi qua các bản lưu tiến độ.
- Ảnh thẳng/nghiêng 23° của nồi vo; nồi cơm đóng vung/nhấc vung/đặt vung lệch bên; cận lòng nồi, vành/quai, gạo sống/cơm.
- Bếp góc trước/góc người chơi, lắp nồi trên gối đỡ; củi ngoài bếp/trong lò/rút một phần.
- Hàn seam hình học, loại mặt suy biến, tính lại normals; kiểm tra UV, mặt trùng, scale, lỗ hở ngoài dự kiến. Mặt nước mở có chủ đích, phần còn lại được kiểm tra topology kín theo từng mesh component.
- Mỗi FBX đã import lại để so hierarchy, kích thước, root, origin/anchor và số triangles; xem số liệu chính xác cùng trạng thái pass/fail trong `Validation.json`.
- Kiểm tra chuyển `.blend` và thư mục texture sang vị trí khác rồi mở lại bằng Blender headless; xem `RelocationValidation.json`.
- **Chưa chạy playtest/kiểm tra shader bên trong Unity Editor**. Phần chất lỏng, hơi nước, lửa/khói và điều khiển vật thể do Unity xử lý theo thiết kế gameplay.

## Cấu trúc object trong từng FBX

'''+ '\n'.join(hier)+ '\n## Danh sách texture\n\n```text\n'+'\n'.join(textures)+'\n```\n'
(OUT/'README.md').write_text(text,encoding='utf8')
print('README written:',len(text),'characters;',len(textures),'texture PNGs')
