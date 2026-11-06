# Review nhánh Thuận — 06/11/2026

## Phạm vi

- Nguồn: `origin/ThuanND_Scene_1`, tác giả `thuannd-dev`.
- Commit mới: `ddcbad0` (bật/tắt Scene 3, sửa vo gạo), `55f2d08` (sửa nhóm lửa).
- So sánh với `main` tại `4c647e2`: thêm `AGENTS.md`, sửa 6 file; **không xóa file nào**.
- Không thay scene, prefab nhân vật, package, project settings hoặc ID enum nhiệm vụ.
- Thay đổi local về bản demo mẹ không thuộc bản merge này và không được đưa lên remote.

## Nội dung được giữ

- F hỗ trợ vo gạo/đặt củi/thổi than, Space/Enter chắt nước và mở vung.
- A/D hoặc mũi tên điều chỉnh củi, F hỗ trợ giữ nhiệt.
- Gợi ý thao tác cập nhật và các kiểm tra null khi kết thúc thao tác/hiển thị hiệu ứng.
- Option `skipScene3` trên `QuestManager` để chủ động chạy bản demo ngắn.
- Font bổ sung ký tự mũi tên xuống; đủ 231 ký tự và glyph ID cũ vẫn được giữ.

## Sửa an toàn trước khi merge

1. `skipScene3` mặc định **false**: game vẫn đi qua Scene 3, không tự mất chương ngoài sân.
2. Bỏ cấu hình static ra khỏi checkpoint. Menu vẫn nhận diện save ngoài sân trước khi có `QuestManager`; chọn chế độ demo không làm ẩn save đã tồn tại.
3. Dùng chung `HouseholdDestination` cho kiểm tra scene đích và chuyển cảnh.
4. Giữ khoảng tương tác/góc nhìn mới nhưng yêu cầu raycast thật sự trúng nồi trước vật cản; truy vấn rỗng hoặc đầy buffer không được xem là nhìn thấy nồi.
5. Giữ bộ đếm vòng vo gạo có dấu, không cộng vòng bằng cách lắc qua lại. F vẫn là phương án hỗ trợ riêng.
6. Chuột vẫn bắt đầu trên quai nồi, mặt nước, ống thổi, thanh củi hoặc núm vung; không kích hoạt từ vùng màn hình trống.
7. Giữ giới hạn nhịp thổi; phím hỗ trợ tạo các nhịp hợp lệ. Trả phím xóa nhịp giữ còn dư.
8. Phục hồi caption trước khi truy cập label bị mất, tránh lỗi null và tạo nhiều canvas còn sống.

## Kiểm tra đã thực hiện

- **PASS:** biên dịch `Assembly-CSharp` và `Assembly-CSharp-Editor` từ worktree review bằng Roslyn của Unity 2022.3.62f3, sử dụng response file/reference của dự án. Output nằm riêng tại `Library/ThuanMergeReview`, không ghi đè assembly Editor đang mở.
- **PASS:** 22 kiểm tra cấu trúc/dữ liệu, gồm không mất scene/asset, bảo toàn font, enum, kiểm tra route/checkpoint và các điều kiện bảo vệ thao tác.
- **PASS:** `git diff --check`.
- Còn cảnh báo có sẵn: CS0252 trong test văn phòng và CS0184 trong helper radio.
- **Chưa chạy Play Mode:** Editor đang mở nhưng UnitySkills REST chưa hoạt động; không mở thêm Editor hoặc chạy batch vào dự án đang bị khóa.

Đã bổ sung menu `KuTy/Tests/Check Thuan merge routing and checkpoint safety` và mở rộng `Check Scene 2 complete household flow` để kiểm tra hai route khi Editor kết nối lại. Các test checkpoint có backup/restore save người chơi.
