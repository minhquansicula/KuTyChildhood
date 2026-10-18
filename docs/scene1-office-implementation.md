# Scene 1 — báo cáo triển khai

Scene: `Assets/_Project/Scenes/Act1_Office_Blockout.unity`. Luồng đã triển khai: OpeningDialogue → ReturnToDesk → SeatingTransition → ReadyAtDesk. Play trực tiếp; WASD/chuột/E, Esc pause. Chưa tích hợp MainMenu hoặc mini-game.

## Thành phần

- `OfficeIntroController`: chủ sở hữu input/cursor, thoại dữ liệu, pause/focus, raycast có occlusion, chuyển ngồi và tín hiệu kết thúc. Dùng interaction cục bộ vì PlayerInteraction cũ phụ thuộc HUD/task của prototype. Raycast bỏ collider của player, vẫn xét vật cản.
- `OfficeDoorInteractable`: mở một lần, kiểm tra vùng quét với capsule người chơi, giữ collider đi theo cánh.
- `OfficeIntroHUD`: TMP/UGUI, phụ đề Việt, objective, prompt, pause/resume.
- Giữ motor dùng chung nguyên code; disable khi camera intro/ngồi tự điều khiển. Không sửa Build Settings.
- Giữ `_SLOT`, `Visual_REPLACE`, `Collision` và seat anchor để thay hình.
- Traffic, keyboard, slam lấy từ project. Cửa/ghế/điều hòa là WAV placeholder tạo cục bộ. Voice mới chưa có; các câu dùng thời lượng fallback khoảng 22 giây.

## Kiểm thử ngày 18/10/2026

Harness Editor gọi component thật, raycast thật và CharacterController.Move trong Play. Không mô phỏng bàn phím/chuột thật. Kết quả chi tiết: `Assets/Screenshots/OfficeIntro_Integration.txt` (17 assertion PASS ở lần chạy hoàn tất). Các lỗi RectTransform, teardown UI và raycast tự chạm player đã được sửa trước lần chạy đạt.

| Ca | Trạng thái | Bằng chứng / giới hạn |
| --- | --- | --- |
| T01 | Đạt tự động | Spawn, trạng thái mở đầu |
| T02 | Đạt logic | Motor tắt, interaction bị từ chối; chưa nhấn WASD thật |
| T03 | Chưa thử | Chưa có voice khớp lời; dùng fallback |
| T04 | Đạt | Toàn bộ thoại không voice hoàn tất |
| T05 | Đạt phần text | Thời gian phụ đề đóng băng khi pause; voice chưa thử |
| T06 | Đạt logic | Trả ReturnToDesk sau đủ sáu nhịp |
| T07 | Đạt | Cube chắn ray làm mất prompt và từ chối tương tác |
| T08 | Đạt | Ray mở cửa, lệnh thứ hai bị từ chối |
| T09 | Đạt | Capsule trong vùng quét chặn mở |
| T10 | Đạt một tuyến | CharacterController đi qua cửa đến bàn, phát exit; nhiều hướng chưa thử |
| T11 | Chưa thử thủ công | Chưa đi vòng/quay lại bằng WASD |
| T12 | Chưa thử thủ công | Code chỉ so ancestry của ghế Tý |
| T13 | Đạt | Raycast ghế và chống gọi ngồi lặp |
| T14 | Đạt | Pause giữ nguyên vị trí giữa transition |
| T15 | Đạt một phần | Đúng seat anchor/collider tắt, đã xem ảnh ngồi; chưa quét mọi góc chuột |
| T16 | Đạt | ReadyAtDesk đúng một lần, E không chạy thêm |
| T17 | Đã quan sát một phần | Mất focus qua công cụ khiến pause; chưa kiểm tra click Resume thực tế |
| T18 | Đạt phần lifecycle | Nhiều lượt Stop/Play; teardown trả spawn an toàn; chưa test reload additive |
| T19 | Đạt với ghế | Tắt visual ghế vẫn tương tác đúng; model cửa thay mới chưa thử |
| T20 | Đạt phụ đề | Font đủ glyph; render và xem PNG 1920×1080,1366×768, không tràn; pause/button chưa thử click |
| T21 | Kiểm tra mã | FirstPersonController không sửa; chưa Play lại các scene khác |
| T22 | Đạt | So toàn bộ local pose/active và byte scene trước/sau capture không đổi |

Ảnh `OfficeBlockout_Overview`, `OfficeBlockout_Boss`, `OfficeBlockout_Workspace`, `OfficeBlockout_Seated` trong `Assets/Screenshots`; thêm `OfficeIntro_Runtime.png` là Game View thật và `OfficeIntro_HUD_1920/1366.png` là render QA bố cục.

Sau khi ráp FBX, đã chạy lại 17 assertion tích hợp: tất cả PASS. Quan sát runtime bổ sung 6 giây, job `8ad58f4f`: completed, healthy=true, errorCount=0. Scene cuối xác nhận isDirty=false. Các ca thủ công chưa thử trong bảng vẫn giữ nguyên trạng thái, không coi lần quan sát ngắn là kiểm chứng toàn bộ input.

## Asset người dùng

Ngày 18/10 nhận lại `Office_Workstation.fbx` (10.797.212 byte), thay bản rỗng 4076 byte tại `Assets/_Project/Art/Office/RetroOffice/Models/Office_Workstation.fbx`, giữ meta/GUID. Unity import được, có 72 material dùng URP/Lit. File chứa nhiều nội thất, không chỉ một workstation.

Đã ráp bàn, màn hình CRT, bàn phím và ghế vào ba bàn phụ và bàn Tý. Mesh được đặt trong các nhóm `ImportedDesk`, `ImportedMonitor`, `ImportedKeyboard`, `ImportedChair` dưới `Visual_REPLACE`; khối mẫu tương ứng được tắt, không xóa. Chuẩn hóa trục và kích thước theo slot hiện có; collider và seat anchor được giữ. Công cụ `KuTy/Office Intro/Place Imported Furniture` là công cụ authoring có ghi đè các nhóm Imported nói trên, không dùng sau khi đã sửa tay nếu chưa lưu bản sao.

Texture gốc đã được khôi phục từ `90s_retro_office_pack.glb`: trích xuất nguyên 160 ảnh, đối chiếu 72 tên material với FBX, tạo material URP tại `Assets/_Project/Art/Office/RetroOffice/Materials`, remap importer và cập nhật 28 renderer nội thất trong scene. Các material màu tạm cũ được giữ nhưng không còn gán cho nhóm Imported.

Base color/emission đọc sRGB; normal đọc normal map; metallic/roughness chuyển từ kênh B/G của glTF thành R/A metallic/smoothness của URP. Occlusion chuyển R sang G. Giữ base factor, tiling/offset, alpha và double-sided theo dữ liệu gốc. Shader URP/Lit chưa tái tạo các extension specular/sheen, nên độ bóng có thể khác viewer gốc. Những đạo cụ còn dùng khối mẫu không được coi là đã thay bằng mesh/texture gốc.

Công cụ `KuTy/Office Intro/Restore Original Materials` tái tạo mapping từ manifest lưu kèm texture. Công cụ ráp bàn ưu tiên material đã khôi phục, tránh quay về màu tạm. Đã xem lại ảnh góc ngồi/khu làm việc: chữ bàn phím, họa tiết bàn và mặt màn hình hiển thị. Scene xác nhận đã lưu (isDirty=false). Báo cáo `Assets/Screenshots/OfficeOriginalMaterials_Report.txt` ghi từng material/map; ảnh bốn góc và kiểm tra capture đã cập nhật.
