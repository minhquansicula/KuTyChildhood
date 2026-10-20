# Scene 2 — kiểm kê asset nặng, 21/10/2026

Scene được kiểm tra: `Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity`.

## Kết quả chính

Nhiều model có mật độ hình học rất cao so với vai trò đạo cụ trong nhà. Hai model đứng đầu gần một triệu tam giác mỗi model; số liệu hai model này đã được đối chiếu với mesh Unity import và khớp hoàn toàn với phép đọc FBX.

| Asset | Tam giác/model nguồn | FBX nguồn (MiB) | Số prefab instance trực tiếp trong scene lưu |
| --- | ---: | ---: | ---: |
| `Kitchen/SM_RiceCookingStove.fbx` | 999.956 | 90,35 | 1 |
| `Kitchen/SM_KitchenStructure_02.fbx` | 999.644 | 96,68 | 1 |
| `Kitchen/SM_DishRack.fbx` | 500.000 | 65,60 | 1 |
| `Yard/SM_ToolSet.fbx` | 500.000 | 68,25 | 2 |
| `Architecture/SM_Roof.fbx` | 500.000 | 61,06 | 3 |
| `Kitchen/SM_WallBasin.fbx` | 500.000 | 54,54 | 2 |
| `Architecture/SM_WallPanel_01.fbx` | 499.930 | 63,24 | 3 |
| `Yard/SM_PottedPlant.fbx` | 499.732 | 58,83 | 1 |
| `Kitchen/SM_KitchenCurtain.fbx` | 498.779 | 61,27 | 2 |
| `Kitchen/SM_DishCabinet.fbx` | 488.202 | 59,85 | 1 |

Các đường dẫn trong bảng tương đối với `Assets/_Project/Art/Scene2/Models/`.

Mười file này cộng lại có 5.986.243 tam giác nguồn, chưa nhân số instance. Con số này không phải số tam giác GPU vẽ mỗi frame. Instance có thể bị tắt, được thay mesh hoặc bị culling; báo cáo dependency bao gồm cả các tham chiếu tới object ẩn và các material thuộc prefab.

`Architecture/SM_TimberColumn_Single.asset` còn có **169.278 tam giác cho một mesh cột**, đã xác nhận bằng Unity. Mesh đang bật Read/Write; cần xem có mã runtime thật sự cần truy cập mesh trước khi tắt để giảm bộ nhớ CPU.

## Texture

- Phát hiện 121 file ảnh trong nhánh dependency được duyệt, trong đó 85 ảnh nguồn 4096 × 4096.
- Cả 121 file đều có `maxTextureSize: 2048` ở thiết lập mặc định. Ảnh nguồn 4K không đồng nghĩa game đang tải texture 4K.
- Kiểm tra trực tiếp `T_StovePot_BaseColor.png`: Unity import 2048 × 2048, DXT1, mipmap, Read/Write tắt; số đo bộ nhớ đối tượng texture khoảng **2,67 MiB**.
- Kiểm tra trực tiếp `T_DishRack_Normal.png`: Unity import 2048 × 2048, DXT5, mipmap, Read/Write tắt; số đo bộ nhớ đối tượng texture khoảng **5,33 MiB**.
- Đây là số đo cho từng texture mẫu, không phải tổng VRAM Scene 2. Không dùng dung lượng PNG/FBX trên đĩa để suy ra RAM hoặc VRAM.

## Đèn và bóng

Scene lưu 10 component Light được bật, thiết lập realtime; 6 component bật soft shadows. Cần kiểm tra parent đang active, tầm ảnh hưởng, pipeline và frame thực tế trước khi kết luận số đèn/bóng được render. Các model dày tam giác kết hợp nhiều lượt bóng là điểm cần đo tiếp.

## Thứ tự xử lý đề xuất

1. Giảm hình học bếp nấu cơm và khối bếp. Ưu tiên giữ silhouette, UV và texture, bake chi tiết sang normal map khi cần; thử model thay thế riêng trước khi áp dụng.
2. Kiểm tra mái, vách, chậu rửa và bộ dụng cụ: vừa dày hình học vừa có nhiều prefab instance trong scene lưu. Xác nhận instance nào đang dùng mesh gốc trước khi tối ưu hàng loạt.
3. Giảm giá chén, tủ chén, chậu cây, rèm và cột. Thêm LOD khi khoảng cách/góc nhìn có lợi; với đồ nhỏ, giảm mesh nền tảng trước.
4. Đo lại đèn/bóng từ các vị trí chơi đại diện. Giữ nguồn sáng quan trọng, cân nhắc bake phần tĩnh nếu phù hợp cách thay đổi ánh sáng hiện tại.
5. Sau đó mới cân nhắc texture 1024 cho đồ nhỏ nhìn xa, dựa vào ảnh so sánh và bộ nhớ đo được.

Mesh Compression trong importer không thay thế việc giảm số tam giác. Số tam giác giảm được cũng không tự động tương ứng cùng tỷ lệ tăng FPS.

## Phạm vi và khả năng tái kiểm tra

- Trong lúc kiểm tra, người dùng đang chơi `Act1_Office_Blockout`. Không dừng Play Mode, không điều hướng sang Scene 2, không sửa scene/importer/gameplay/checkpoint.
- `audit_scene2.py` đọc dependency GUID từ scene/prefab/material/asset và material remap của FBX; đọc polygon index FBX nhị phân để tính tam giác tương đương khi triangulate. Bộ đếm không mô phỏng prefab override, trạng thái active hoặc camera culling.
- `source-audit.json`: toàn bộ kết quả nguồn, bao gồm 37 FBX đọc thành công, các mesh nhúng, texture và thiết lập đèn lưu trong scene.
- `unity-import-check.json`: kết quả API Unity xác minh hai FBX lớn nhất, mesh cột và hai texture mẫu. API model này đọc mesh đầu tiên; hai FBX xác minh có số tam giác trùng tổng FBX nguồn.
- Chưa có phép đo CPU/GPU frame time hay tổng VRAM của Scene 2 trong lượt này. Vì vậy đây là danh sách ứng viên tối ưu đã có bằng chứng về độ nặng asset, chưa phải kết luận bottleneck runtime.

Chạy lại bằng Python có Pillow: `python Tools/Scene2/AssetAudit/audit_scene2.py`. Việc đọc file không yêu cầu Unity mở và không sửa file nguồn.
