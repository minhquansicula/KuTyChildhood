# Scene 1 — báo cáo triển khai

Scene: `Assets/_Project/Scenes/Act1_Office_Blockout.unity`.

## Đợt A — onboarding dễ hiểu và ít áp lực (20/10/2026)

Rà soát tiếp: marker dùng hình thoi vẽ bằng UI Image để tránh thiếu glyph; tự ẩn khi pause/input bị khóa và hiện lại khi tiếp tục. Khi đóng Excel rồi đứng dậy, hint và target hướng về ghế trước khi mở báo cáo. Smoke test bổ sung pause/resume đạt **51 assertions**, console không có error/exception; ảnh HUD đã kiểm tra lại.

- HUD chỉ hiển thị **một nhiệm vụ hiện tại**, cập nhật bằng event khi phase/ngồi/làm việc thay đổi; objective cũ trong `OfficeSceneController` được ẩn để không trùng nội dung.
- Hướng dẫn có hai tầng: sau 20 giây hiện gợi ý bằng chữ, sau 40 giây hiện marker tới đúng đối tượng. Người chơi có thể nhấn `H` để gọi gợi ý thủ công; tiến độ thật sẽ reset timer và dọn gợi ý cũ.
- `PlayerInteraction` phát `FocusChanged` và lưu `CurrentTarget`, tạo điểm móc cho viền sáng/âm thanh focus ở các đợt sau mà không phải polling.
- Lần đầu mở Excel có tutorial giải thích hai phép tính và cho chọn `60 giây` hoặc `Thư thả — không giới hạn`. Tutorial không làm timer chạy ngầm; đổi mode/retry/cancel đều dừng coroutine cũ để tránh timer hoặc popup bị nhân đôi.
- `OfficeGuidanceState` tách logic thời gian khỏi MonoBehaviour để kiểm thử được các mốc hint/marker/reset mà không phụ thuộc frame rate.

Kiểm thử cuối bằng `KuTy > Tests > Run Act1 office task flow + edge cases`: `Assets/Screenshots/OfficeBlockoutGameplay_Integration.txt` đạt **49 assertions**, gồm thứ tự hint → marker, resolve đúng workstation, reset sau tiến độ, tutorial chặn attempt/timer, Relaxed đóng băng thời gian, Timed bắt đầu đúng, cùng toàn bộ luồng Excel → in → lấy giấy → giao sếp → uống nước → nghỉ → Act 2. Validator Scene 1 đạt và Unity Console có **0 error/exception**. Ảnh QA mới: `OfficeExcel_Tutorial_1366.png`, `OfficeExcel_UI_1366.png`, `OfficeShift_HUD_1366.png`.

Giới hạn còn lại: smoke test gọi hook/component thật nhưng không thay thế một lượt chơi thủ công toàn tuyến bằng WASD/chuột từ MainMenu; nên làm một usability playtest ngắn với người chưa biết nhiệm vụ trước khi chốt đợt B.

## Cập nhật gameplay/UI — 19/10/2026

Luồng hiện tại: nghe sếp → mở cửa → ghế Tý → báo cáo Excel hai sheet → gửi báo cáo → 15 giây nghỉ mắt/đi lại → ngồi và nghỉ → gục xuống → `Act2_MemoryWorld_Home`. MainMenu và Build Settings đã trỏ tới Blockout. Bản code nhận đầu phiên đã dùng **Excel-only**; email/phân loại giấy trong kế hoạch parity cũ không còn là nhiệm vụ bắt buộc.

- Thêm bảng tiến trình ca làm và giờ 17:42/18:17; ẩn khi mở công việc, pause và chuyển cảnh. Cập nhật mục tiêu khi ngồi/đứng ở bước nghỉ.
- Excel có hướng dẫn công thức theo sheet, tiến độ 0–2 ô đúng, nút đóng giữ dữ liệu, nút gửi với Ctrl+Enter. Đóng/mở giữ cả thời gian còn lại; hết giờ cho thử lại với dữ liệu hiện tại; cấu hình thời gian không dương dùng 60 giây.
- Tin nhắn có nền che và chặn tương tác bảng tính phía sau; phím tắt không gửi bài khi pause hoặc popup đang mở. Nội dung tin nhắn phù hợp cuối giờ làm.
- Chuẩn hóa font tiếng Việt trong Office, nạp các dấu còn thiếu. Cải thiện bố cục HUD và tương phản prompt; không dựng lại map/nội thất/ánh sáng.
- Khóa thao tác đứng dậy khi còn chuyển ghế, đang làm việc hoặc đang gục xuống.
- Menu `KuTy > Office > Apply shift overview` cập nhật HUD/font tại scene Office hiện có; không chạy builder nội thất.

Kiểm thử Play Mode: `Assets/Screenshots/OfficeBlockoutGameplay_Integration.txt` đạt **37 assertions**: bàn giao intro (bỏ qua thoại bằng hook), ngồi/đứng, callback lặp, pause timer, popup modal, nhập sai, retry, giữ dữ liệu/thời gian khi đóng, cấu hình 0 giây, nhập qua TMP_InputField thật, gửi thành công và tải Act 2. Chạy lại bằng `KuTy > Tests > Run Act1 Excel gameplay + edge cases`. Timeout chờ tải cảnh của harness là 45 giây để không báo lỗi giả khi Editor tải chậm; không thay thời lượng cinematic.

Ảnh QA: `OfficeExcel_UI_1920.png`, `OfficeExcel_UI_1366.png`, `OfficeExcel_Chat_1366.png`, `OfficeShift_HUD_1366.png`, `OfficeShift_Complete_1366.png` trong `Assets/Screenshots`. Ảnh render camera + UI ở hai độ phân giải; không phải mô phỏng bàn phím/chuột người dùng. Kiểm thử này không chứng nhận toàn bộ đường đi bằng WASD hay lượt chơi từ MainMenu; kiểm thử intro cũ bên dưới là bằng chứng lịch sử. Voice mới/NPC sếp hoàn chỉnh vẫn chưa có.

Scene Act 2 đang mở và có chỉnh sửa chưa lưu ở đầu phiên; đã lưu trạng thái đó trước khi chuyển sang Office, không chạy builder hay thay nội dung Act 2.

## Triển khai intro ban đầu (lịch sử)

Luồng ban đầu dừng ở ReadyAtDesk; các ghi chú chưa tích hợp mini-game bên dưới mô tả giai đoạn đó.

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

## TTS pilot đợt A — 20/10/2026

- Intro Scene 1 dùng `DialogueSequence` tại `Assets/_Project/Data/Dialogue/Act1OfficeIntro.asset`, nhưng vẫn giữ fallback sang `OfficeIntroController.Line[]` cho scene cũ.
- `DialogueVoicePlayer` nằm trên object `DialogueVoice`, phát 2D, một clip mỗi lần và được intro điều khiển pause/resume/stop.
- Có 4 stable voice ID: `act1_intro_boss_001`, `act1_intro_boss_002`, `act1_intro_boss_003`, `act1_intro_ty_001`; hai beat im lặng giữ nhịp đập bàn/khoảng dừng.
- Manifest ở `Assets/_Project/Audio/Voice/vi/Act1/act1_tts_manifest.json`. Hiện 4 clip tiếng Việt thật chưa được tạo/gán vì chưa chọn provider và máy không có voice Việt.
- Smoke đạt 65 assertions; ngoài task flow còn kiểm tra missing clip fallback, mute không mất timing, pause/resume, stop/replace token và intro tự hoàn tất không dùng skip.

## SFX đợt A — 20/10/2026

- `SfxCatalog` có 14 typed cue cho UI, task, nước/chén, bi, shop và memory; `AudioManager` dùng pool one-shot, cooldown và tránh lặp biến thể liên tiếp.
- Tất cả call site gameplay hiện tại đã bỏ string key. `ui_select` trước đây không có mapping và phát im lặng nay dùng `SfxId.UIConfirm`.
- UI source vẫn phát được khi game pause; gameplay SFX/ambience/voice chịu global pause. Transient one-shot được dừng khi đổi scene.
- Scene 1 thêm chair shift và nối feedback cho Excel invalid/success, printer, paper và water. Tương tác sai phase/too early không phát âm thành công.
- Catalog/prefab/manifest được cài bằng `KuTy/SFX/Install SFX-A foundation`; validator nằm tại `KuTy/Validation/Validate SFX-A foundation`.
- Smoke Act 1 đạt **75 assertions**, Console 0 error/0 warning; missing script và missing reference đều bằng 0.
- Các clip trong manifest hiện vẫn là generated fallback, chưa phải production SFX. Đợt B cần thay clip thật, ghi nguồn/license và listening review cùng TTS/ambience.
