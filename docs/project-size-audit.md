# KuTy — audit dung lượng và ứng viên dọn dẹp

Ngày 21/10/2026. Chỉ đọc; không xóa file, không đổi importer, scene hoặc lịch sử Git. Dung lượng báo bằng MiB/GiB (1024). Số liệu là dung lượng file nguồn trên ổ đĩa, không phải dung lượng build hoặc bộ nhớ GPU.

## Tổng quan

Toàn bộ thư mục làm việc: 16.22 GiB. Assets đã được Git theo dõi: 4.57 GiB.

| Thư mục | MiB | Vai trò |
|---|---:|---|
| `Library` | 4875.32 | Cache/import của Unity; tái tạo được, cần import lại khi dọn |
| `.git` | 4821.54 | Lịch sử Git; giữ nguyên, không xóa thủ công |
| `Assets` | 4722.84 | Nội dung dự án |
| `.unity-validation` | 1814.83 | Project kiểm thử và cache; xem lại nguồn/backup trước khi bỏ |
| `Builds` | 220.94 | Các bản build cũ; không phải source |
| `dumb` | 77.57 | Tải xuống/draft cục bộ, đang được ignore |
| `SourceAssets` | 48.61 | File Blender nguồn có giá trị chỉnh sửa; không coi là rác |
| `UnityChairValidation` | 11.53 | Project thử ghế cục bộ, đang được ignore |
| `Tools` | 9.03 | Công cụ, kiểm tra và báo cáo |
| `docs` | 3.83 | Tài liệu |

Art Scene 2: Models 2073,85 MiB (~2,03 GiB), Textures 1317,86 MiB (~1,29 GiB). Đây là nhóm lớn nhất trong source. Audio toàn dự án chỉ khoảng 28,2 MiB tại lần quét.

## Các asset lớn nhất

Reachability được đối chiếu với dependency đệ quy của 5 scene bật trong EditorBuildSettings và 4 root asset dưới _Project/Resources. Không coi việc không tìm thấy trong tập này là bằng chứng đủ để xóa; cần xét thêm prefab không nằm trong build, Addressables/StreamingAssets, công cụ Editor và source cần tái dựng.

| Asset | MiB | Scene/Resources tham chiếu |
|---|---:|---|
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_KitchenStructure_01.fbx` | 97.73 | Không tìm thấy trong tập root đã kiểm tra |
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_KitchenStructure_02.fbx` | 96.68 | home-dependencies |
| `Assets/_Project/Art/Textures/Scene3/HDRI/golf_course_sunrise_4k.exr` | 92.24 | Act3_MemoryWorld_OutSide-dependencies |
| `Assets/_Project/Art/Models/Scene3/caylua.fbx` | 91.73 | Không tìm thấy trong tập root đã kiểm tra |
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_RiceCookingStove.fbx` | 90.35 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Yard/SM_ToolSet.fbx` | 68.25 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_DishRack.fbx` | 65.60 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Architecture/SM_WallPanel_02.fbx` | 63.24 | Không tìm thấy trong tập root đã kiểm tra |
| `Assets/_Project/Art/Scene2/Models/Architecture/SM_WallPanel_01.fbx` | 63.24 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Architecture/SM_TimberColumn.fbx` | 61.45 | Không tìm thấy trong tập root đã kiểm tra |
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_KitchenCurtain.fbx` | 61.27 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Architecture/SM_Roof.fbx` | 61.06 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Bedroom/SM_Radio.fbx` | 60.81 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_DishCabinet.fbx` | 59.85 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Yard/SM_PottedPlant.fbx` | 58.83 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_WallBasin.fbx` | 54.54 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Yard/SM_GrainPile.fbx` | 54.05 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Architecture/SM_ImportedPanel_02.fbx` | 48.62 | Không tìm thấy trong tập root đã kiểm tra |
| `Assets/_Project/Art/Materials/Scene3/tiemtaphoa.fbx` | 48.10 | Act3_MemoryWorld_OutSide-dependencies |
| `Assets/_Project/Art/Scene2/Models/Architecture/SM_HouseFacade.fbx` | 47.66 | Không tìm thấy trong tập root đã kiểm tra |
| `Assets/_Project/Art/Scene2/Models/Yard/SM_GrainMill.fbx` | 46.08 | home-dependencies |
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_RiceJar.fbx` | 44.59 | home-dependencies |

## Bản sao giống byte

Hash SHA-256 trên các asset từ 256 KiB trở lên có cùng kích thước: 7 nhóm, khoảng 110.04 MiB dữ liệu lặp tiềm năng. File có GUID khác nhau vẫn có thể đang được nhiều material/prefab sử dụng; phải chuyển tham chiếu trước khi hợp nhất.

- 63.24 MiB mỗi bản; phần lặp 63.24 MiB:
  - `Assets/_Project/Art/Scene2/Models/Architecture/SM_WallPanel_01.fbx`
  - `Assets/_Project/Art/Scene2/Models/Architecture/SM_WallPanel_02.fbx`

- 42.10 MiB mỗi bản; phần lặp 42.10 MiB:
  - `Assets/_Project/Art/Scene2/Models/Kitchen/SM_FirewoodStove_01.fbx`
  - `Assets/_Project/Art/Scene2/Models/Kitchen/SM_FirewoodStove_03.fbx`

- 2.16 MiB mỗi bản; phần lặp 2.16 MiB:
  - `Assets/_Project/UI/bacground.png`
  - `Assets/_Project/UI/Sprites/bacground.png`

- 0.57 MiB mỗi bản; phần lặp 1.13 MiB:
  - `Assets/_Project/Art/Office/RetroOffice/Textures/UnityPacked/Printer_Base_MetalSmooth.png`
  - `Assets/_Project/Art/Office/RetroOffice/Textures/UnityPacked/Printer_Base_White_MetalSmooth.png`
  - `Assets/_Project/Art/Office/RetroOffice/Textures/UnityPacked/Printer_Screen_MetalSmooth.png`

- 0.54 MiB mỗi bản; phần lặp 0.54 MiB:
  - `Assets/_Project/Art/Office/RetroOffice/Textures/UnityPacked/Creme_Lamp_MetalSmooth.png`
  - `Assets/_Project/Art/Office/RetroOffice/Textures/UnityPacked/White_Lamp_MetalSmooth.png`

- 0.51 MiB mỗi bản; phần lặp 0.51 MiB:
  - `Assets/_Project/Art/Office/RetroOffice/Textures/UnityPacked/Printer_Top_MetalSmooth.png`
  - `Assets/_Project/Art/Office/RetroOffice/Textures/UnityPacked/Printer_Top_White_MetalSmooth.png`

- 0.35 MiB mỗi bản; phần lặp 0.35 MiB:
  - `Assets/_Project/Art/Office/RetroOffice/Textures/image_21.png`
  - `Assets/_Project/Art/Office/RetroOffice/Textures/image_26.png`

Đã kiểm tra usage toàn AssetDatabase: SM_KitchenStructure_01.fbx, SM_WallPanel_02.fbx, SM_FirewoodStove_03.fbx, caylua.fbx và UI/Sprites/bacground.png đều có usedByCount = 0. Tuy nhiên caylua.fbx vẫn được Editor/Scene3VillageRebuild.cs dùng theo đường dẫn; đó là source cho công cụ tái dựng, không nên xóa chỉ dựa vào dependency build. Ba ứng viên KitchenStructure_01 + WallPanel_02 + FirewoodStove_03 có tổng khoảng 203,07 MiB; WallPanel_02 và FirewoodStove_03 là bản sao byte của bản đang dùng.

## Hình học đang nặng

Đây là thống kê mesh asset từ Unity, không phải số tam giác đang render mỗi frame của toàn scene.

| Model | Vertices | Triangles |
|---|---:|---:|
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_KitchenStructure_01.fbx` | 577,821 | 999,978 |
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_KitchenStructure_02.fbx` | 559,864 | 999,644 |
| `Assets/_Project/Art/Scene2/Models/Kitchen/SM_RiceCookingStove.fbx` | 551,517 | 999,956 |
| `Assets/_Project/Art/Scene2/Models/Yard/SM_ToolSet.fbx` | 294,969 | 500,000 |

caylua.fbx có 2.088.558 vertices và 4.124.358 triangles theo Unity, nhưng không nằm trong dependency scene build hiện tại; không được cộng nó vào chi phí render hiện tại.

Các FBX Scene 2 còn kèm payload ảnh lớn:
- `SM_KitchenStructure_01.fbx`: 42.90 MiB ảnh nhúng trong file 97.73 MiB.
- `SM_KitchenStructure_02.fbx`: 42.77 MiB ảnh nhúng trong file 96.68 MiB.
- `caylua.fbx`: 0.00 MiB ảnh nhúng trong file 91.73 MiB.
- `SM_RiceCookingStove.fbx`: 35.14 MiB ảnh nhúng trong file 90.35 MiB.
- `SM_ToolSet.fbx`: 39.23 MiB ảnh nhúng trong file 68.25 MiB.
- `SM_DishRack.fbx`: 36.49 MiB ảnh nhúng trong file 65.60 MiB.
- `SM_WallPanel_02.fbx`: 36.65 MiB ảnh nhúng trong file 63.24 MiB.
- `SM_WallPanel_01.fbx`: 36.65 MiB ảnh nhúng trong file 63.24 MiB.
- `SM_TimberColumn.fbx`: 34.18 MiB ảnh nhúng trong file 61.45 MiB.

Hướng tối ưu: tạo bản low-poly/LOD cho các asset đang dùng; bake normal; xuất FBX không nhúng lại texture khi đã dùng texture ngoài. Lưu bản nguồn chất lượng cao ngoài Assets. Không giảm chất lượng hay xóa tự động trong lượt audit này.

HDRI golf_course_sunrise_4k.exr: file nguồn 92,24 MiB nhưng Unity import hiện tại 2048×1024 BC6H, báo khoảng 5,33 MiB bộ nhớ. Radio PNG nguồn khoảng 25,11 MiB nhưng import 2048×2048 DXT1 cũng báo khoảng 5,33 MiB. Không suy dung lượng RAM/GPU từ kích thước EXR/PNG nguồn.

## Ứng viên cần rà soát thêm

Có 1125.70 MiB file Art không nằm trong tập dependency build/Resources đã kiểm tra; đây là danh sách rà soát, không phải dung lượng được xác nhận xóa an toàn.

- Scene2 Models: 7 file, ~403,99 MiB chưa nằm trong dependency root đã xét.
- Scene2 Textures: 63 file, ~292,08 MiB chưa nằm trong dependency root đã xét.
- Assets/Screenshots: ~31,11 MiB ảnh kiểm tra, hiện được Git ignore; nên lưu ngoài Assets để giảm import.
- SourceAssets/RetroOfficePack/RetroOfficePack_Unity.blend được Git theo dõi: cần giữ nếu còn chỉnh sửa asset.

## Git

git count-objects -vH: loose objects ~1,05 GiB, packs ~3,39 GiB, 19 mục garbage/tmp khoảng 260,44 MiB. Toàn .git khoảng 4,71 GiB. Không chạy prune/gc, không xóa tmp hoặc viết lại lịch sử trong lượt kiểm tra. Dọn file source hiện tại không làm các blob cũ biến mất khỏi lịch sử Git.

Ưu tiên: (1) rà/xóa hoặc lưu trữ project kiểm thử và build cũ; (2) kiểm tra ba ứng viên asset đã nêu; (3) làm nhẹ mesh Scene 2 đang dùng; (4) hợp nhất các bản sao texture bằng cách chuyển tham chiếu; (5) xử lý Git maintenance riêng khi chắc không có tác vụ Git đang chạy.

Dữ liệu chi tiết nằm trong Tools/ProjectAudit/*.json.
