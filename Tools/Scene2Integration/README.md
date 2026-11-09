# Scene 2 — tích hợp dat-dev + Thuận, 09/11/2026

## Kết quả và phạm vi

- Local gameplay được giữ ở `0739a2d` trước khi merge.
- Merge Đạt: `5b288e9`, parent incoming `b64c1c5` (`origin/dat-dev/feature/scene2`).
- Merge Thuận: `7dd6d8a`, parent incoming `30190e9` (`origin/ThuanND_Scene_1`).
- Chỉ commit local, **chưa push**.
- Giữ khu rửa chén, đồ Downloads, chồng chén, nắp nồi và policy bốn minigame. Các việc khác vẫn cần xác nhận UI; không tự hoàn thành nhiệm vụ.
- Scene Đạt được giải quyết có chọn lọc qua Unity Editor: nhận `BoBanGhe_TiepKhach`, pose bàn ăn/ghế còn lại và hai ghế thêm. Ba ghế cũ bị thay được giữ dạng inactive để không mất object/reference. Không nhận các thay đổi bố trí/xóa đồ bếp, kết cấu nhà ngoài phạm vi phòng khách đã chốt.
- Nhận toàn bộ 37 đường dẫn ngoài Home scene của Đạt, gồm model/material/texture, video và helper. **Không chạy helper xóa/rebuild furniture của Đạt.** Thêm bốn static MeshCollider cho bộ mới và bake lại navigation mẹ theo bố trí tích hợp.
- Nhận ending/voice/code của Thuận; giữ policy local và kiểm tra khoảng cách/hướng nhìn/occlusion bảo thủ của nồi thay vì bỏ qua vật cản theo tên hoặc chấp nhận truy vấn không thấy nồi.

## QA sau tích hợp

**480 assertions PASS**, tính trên lần chạy cuối của bảy bộ:

- `four-activities.txt`: 129 — bốn minigame, UI đơn giản, pause/cancel/reward/checkpoint, thư đọc và nắp có mặt đỡ 3mm.
- `dishwashing.txt`: 77 — ray tương tác, ba món/hai mặt, làm ướt/xà phòng/chà/tráng/giá, occlusion, pause/focus/save và giữ pose nguồn.
- `mother-navigation.txt`: 42 — di chuyển thật trong nhà, đường dài tới khu bàn ăn mới, chặn vật cản/Tý, pause, trò chuyện và không tự thưởng.
- `merged-journey.txt`: 84 — cinematic thức dậy đầy đủ, UI các việc đơn giản, xác nhận thư thật, 130 mảnh, allowance 2.000đ một lần, checkpoint ra sân, `EndingSummary` giữ kết quả sau unload. Bốn minigame trong **fixture handoff** được advance trực tiếp; thao tác vật lý do hai suite riêng phía trên kiểm.
- `scene2-precise-targeting.txt`: 127 — vo gạo UI, nhóm lửa, nấu cơm, dọn mâm, rửa chén, đọc thư; có ít nhất hai vị trí đứng không overlap; nhìn đi nơi khác, tường và khoảng cách 5m chặn tương tác; mở/hủy giữ input/progress đúng.
- Routing/checkpoint: 8; guidance logic: 13. Bằng chứng log Editor được trích trong `routing-guidance.txt`.
- `build-scenes.txt`: 6 scene trong Build Settings PASS; không missing GUID/script/serialized reference; 42 sprite minigame hợp lệ.
- Compile cuối: cycle100, success, không lỗi C#.
- `layout-and-assets.txt`: 629 transform gốc còn đủ, 627 giữ nguyên local pose/parent; chỉ hai pose dining được chủ động nhận theo Đạt. Khu rửa chén/bếp và mẹ không bị dời hoặc scale.
- Đã xem ảnh thực từ Unity: `living-room.png`, `living-room-reverse.png`, `local-dishwashing.png`; ảnh minigame/mẹ trong các thư mục QA tương ứng cũng được tạo lại bởi suite.
- `preservation-final.json`: đủ 3.571 file local ban đầu; 3.257 file MotherPolish không đổi SHA256; Blender nguồn đúng hash bàn giao; scope checkpoint đã restore.

## Sửa fixture targeting cũ

Lần đầu bài cũ FAIL vì bắt buộc aim-assist lệch20° dành cho minigame vo gạo đã tắt. Sau cập nhật policy, fixture tiếp tục lộ thời gian chờ xuống giường cũ1,1s ngắn hơn animation mới (>=1,05s + fade0,18s), khiến nó thử tương tác khi giường còn giữ input. Đã chờ1,5s, chặn fixture còn modal trước khi thử, chuẩn bị progress không mở reward presentation, và gọi đúng lifecycle `Kitchen.End()` khi hủy hai minigame bếp. Không đổi gameplay/bố trí để chiều test. Sau sửa, toàn bộ127 assertions PASS; giữ các kiểm direct-ray/wall/distance/looking-away và mở rộng đủ bốn minigame.

## Chạy lại

Mở Home đã lưu ở Edit Mode, dùng Unity Skills đúng instance dự án. Các menu QA:

- `KuTy/Tests/Check four activities and supported cooking lid`
- `KuTy/Tests/Check user-arranged dishwashing simulator`
- `KuTy/Tests/Check mother walking and conversations in home`
- `KuTy/Tests/Check merged Scene 2 chapter handoff`
- `KuTy/Tests/Run precise Scene 2 targeting`
- `KuTy/Tests/Check Thuan merge routing and checkpoint safety`
- `KuTy/Tests/Check Scene 2 chore guidance (no checkpoint writes)`

Không chạy lại installer/importer hoặc `Apply dat living room only` trên scene đã tích hợp. Baseline và bản dự phòng riêng nằm trong ignored `Builds/Integration-20261109/`; helper bảo toàn yêu cầu baseline đó để tránh tự nhận một baseline mới.

## Giới hạn

- Chưa build Windows standalone, chưa user mouse-playtest toàn bộ luồng và chưa chứng nhận phát video cutscene mới từ đầu tới cuối. Video được xác nhận import có kích thước/thời lượng hợp lệ.
- Không chạy suite856/39 hoặc complete-flow/voice smoke legacy có giả định minigame cũ và model mẹ cũ. Không dùng PASS cũ để chứng nhận merge.
- Không nghiệm thu sửa tay/mỹ thuật mẹ hay candidate MotherPolish; không sửa/import Blender trong lượt này.
- Unity Console có lịch sử lỗi cũ và native assertion `Caller must complete domain reload` đã xuất hiện từ trước, còn xuất hiện giữa các lần chuyển Play/Edit; không tuyên bố Console hoàn toàn sạch. Các suite runtime mới có guard Error/Exception/Assert và đều PASS trong khoảng kiểm gameplay.
- `git diff --check` trên C#/Markdown sạch. YAML/meta do Unity và incoming có trailing spaces mặc định; không format lại toàn scene ending chỉ để loại cảnh báo whitespace.
