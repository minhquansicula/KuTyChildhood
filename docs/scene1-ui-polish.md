# Scene 1 — UI thoại, nhiệm vụ và tạm dừng

Ngày: 21/10/2026.

- Bảng pause khi nhấn ESC: dùng chung GameUiTheme với Main Menu (khung giấy vàng, viền nâu, font và sprite nút); có Tiếp tục, Về menu chính và nhắc phím ESC. Hộp thoại, phụ đề và bảng nhiệm vụ giữ nền xanh than, chữ kem, điểm nhấn vàng theo yêu cầu. Intro và gameplay dùng chung cách dựng; kích thước thẻ được quy về cùng tỷ lệ màn hình dù hai Canvas có reference resolution khác nhau.
- Phụ đề: tên người nói tách thành nhãn vàng, nội dung căn trái, font tiếng Việt 26–34 theo canvas 1920 × 1080. Khung neo theo chiều ngang, giữ khoảng cách với mép dưới.
- Khi pause, ẩn phụ đề, dấu ngắm và dòng phím điều khiển; khôi phục các trạng thái trước đó khi tiếp tục. Canvas của modal ở trên lớp fade và có raycaster chặn tương tác xuyên nền.
- Thẻ nhiệm vụ: cùng bảng màu, nhấn vào hành động hiện tại; tự tăng chiều cao theo nội dung. Ẩn dòng nhiệm vụ intro trùng với thẻ nhiệm vụ chung.
- Gợi ý tương tác: nền gọn, font và khoảng cách thống nhất. Dấu ngắm chung ẩn trong đoạn thoại mở đầu và hiện khi quay lại điều khiển nhân vật.
- Giữ các callback, SFX nút, TTS và thời lượng phụ đề. Thay đổi trình bày được áp dụng khi khởi tạo runtime, không lưu đè scene đang có chỉnh sửa chưa lưu.

Theme menu: `Assets/_Project/Resources/GameUiTheme.asset`, lấy sprite/font trực tiếp từ Main Menu. Chỉ menu tạm dừng dùng khung giấy; không áp theme giấy vào text box.

Mã bố cục: `Assets/_Project/Scripts/UI/OfficeUiStyle.cs`. Phần áp dụng: `OfficeIntroHUD`, `OfficeShiftHUD`, nhánh Scene 1 của `HUDController` và `PauseUI` và bàn giao HUD trong `OfficeBlockoutGameplayBridge`.

Kiểm tra: `KuTy/Tests/Check office opening transition`. Kết quả và ảnh thực tế được lưu ở `Tools/OfficeOpening/`: `checks.txt`, `after-fade.png`, `pause-ui.png`, `objective-ui.png`, `gameplay-pause-ui.png`. Bộ kiểm tra bao gồm tràn chữ, thẻ nằm trong viewport, hit test nút Tiếp tục, khôi phục phụ đề, TTS và pause sau bàn giao gameplay; không ghi checkpoint của người chơi.
