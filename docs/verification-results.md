# Kiểm chứng cảnh văn phòng — 2026-09-24

Unity 2022.3.62f3 chạy trên bản sao `.unity-validation`; scene Act1 và các asset tạo mới đã được đồng bộ về project chính.

- Dựng scene văn phòng, biên dịch Runtime/Editor scripts: **PASS**.
- Play Mode integration từ văn phòng → các scene hiện có → chơi lại: **PASS, 54 assertions**.
- Render GPU Direct3D 11: **PASS**; ảnh bố cục là [office-preview.png](office-preview.png).
- Build Windows x64: **PASS**; bản mới tại `Builds/Windows-Office/KuTy.exe`. Bản build cũ không bị ghi đè.

Các kiểm tra riêng Act1: khởi đầu không có màn đen hoặc khóa di chuyển; đủ chín vị trí asset; âm giao thông/đèn mẫu có clip; phụ đề sếp không khóa góc nhìn; raycast phím E chạm đúng laptop/cửa sổ; laptop → hồ sơ → sửa báo cáo đúng thứ tự; Esc dừng, tiến độ được giữ; hoàn tất mới mở ghế; ngồi nghỉ mới chuyển scene. Các kiểm tra cũ cho nhiệm vụ/ký ức/chìa khóa của các scene sau vẫn chạy qua.

Ảnh đã được rà lại để laptop, cửa sổ, tài liệu, vách ngăn và bóng sếp đều nhìn thấy. Đây vẫn là greybox. Cần chơi thủ công với model/ảnh/voice thật để chỉnh ánh sáng, âm lượng, góc nhìn và cảm giác di chuyển. Build chưa kiểm thử trên máy Windows khác.

Log cục bộ: `.unity-validation/office-upgrade.log`, `.unity-validation/office-polish.log`, `.unity-validation/office-raycast-test.log`, `.unity-validation/office-build.log`, `.unity-validation/smoke-results.txt`.
