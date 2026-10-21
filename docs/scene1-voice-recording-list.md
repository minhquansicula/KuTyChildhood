# Scene 1: tương tác thực tế và danh sách thu voice

Đối chiếu scene đang mở trong Unity ngày 21/10/2026: Act1_Office_Blockout. Scene có 9 object InteractableBase đang active. Danh sách dưới đây ghi nhận component gắn trong scene và các nhánh có thể tới được trong luồng hiện tại; không phải kiểm thử lại toàn bộ thao tác bằng tay.

## Tương tác có trong scene

| Điểm tương tác | Component | Chức năng |
|---|---|---|
| `BossDoor_Hinge` | `OfficeBlockoutDoorInteractable` | Mở/đóng cửa; ở bước DeliverReport, E nộp báo cáo. |
| `BossPlaceholder_SLOT` | `OfficeInteractable (Boss)` | Nhìn/tương tác với sếp; nộp báo cáo khi tới đúng bước. |
| `TyChair_SLOT` | `OfficeBlockoutChairInteractable` | Ngồi vào ghế; Space đứng lên; ở bước Rest, E gục xuống nghỉ. |
| `TyWorkstationInteraction` | `OfficeWorkstationInteractable` | Mở/tiếp tục minigame đối chiếu Excel khi đã ngồi; kết thúc thành công tự bắt đầu in. |
| `Printer_SLOT` | `OfficeMicroInteractable (PrinterPaper)` | Kiểm tra máy photo; lấy bản in khi hoàn tất; có nhánh kiểm tra quá sớm hoặc sau khi đã lấy. |
| `WaterCooler_SLOT` | `OfficeMicroInteractable (WaterCooler)` | Uống nước; sau khi nộp báo cáo, uống nước chuyển sang bước Rest. |
| `Clock_SLOT` | `OfficeMicroInteractable (Clock)` | Xem đồng hồ; lời nghĩ thay đổi khi tới bước Rest. |
| `ColdCoffeeInteraction` | `OfficeMicroInteractable (ColdCoffee)` | Tương tác cốc cà phê lạnh. |
| `WindowCollision` | `OfficeInteractable (Window)` | Nhìn ra phố; hiển thị lời tả cảnh mang nhãn CỬA SỔ. |

Luồng chính: intro tự chạy → mở cửa → ngồi vào ghế → mở/sửa Excel → tự in → lấy bản in → nộp báo cáo → uống nước → quay về ghế và nghỉ → chuyển Scene 2.

## File voice cần chuẩn bị

Đã gán 4 voice Tý mới và câu sếp nhận báo cáo (act1_intro_boss_004.wav) từ Downloads. Không cần thu lại act1_intro_ty_001.wav. Những dòng còn ghi Cần thu là tên file đề xuất, chưa có bản thu.

| File | Người nói / lúc phát | Nội dung | Trạng thái |
|---|---|---|---|
| `act1_intro_ty_001.wav` | TÝ: Đáp sếp đầu scene | Dạ dạ sếp, em kiểm tra lại ngay ạ | Đã có file |
| `act1_ty_print_ready_001.wav` | TÝ: Máy photo in xong | Máy photo ở cạnh bình nước. Lấy bản in rồi mang cho sếp là xong. | Đã gán voice trong game |
| `act1_ty_print_pickup_001.wav` | TÝ: Lấy bản báo cáo | Bản in còn ấm. Mang đến phòng sếp thôi. | Đã gán voice trong game |
| `act1_ty_water_after_report_001.wav` | TÝ: Uống nước sau khi nộp báo cáo | Sao vẫn choáng thế này... về ghế ngồi một lát đã. | Đã gán voice trong game |
| `act1_ty_rest_001.wav` | TÝ: Ngồi nghỉ sau bước uống nước | Mệt quá... chỉ nghỉ một chút thôi... | Đã gán voice trong game |
| `act1_ty_bossroom_001.wav` | TÝ: Tương tác với sếp khi chưa đến bước nộp báo cáo | Bóng người sau lớp kính mờ vẫn khiến mình nghẹt thở. | Cần thu |
| `act1_ty_clock_early_001.wav` | TÝ: Xem đồng hồ trước bước nghỉ | Sắp hết giờ làm rồi. Mình phải chốt xong file Excel. | Cần thu |
| `act1_ty_clock_late_001.wav` | TÝ: Xem đồng hồ khi đã sang bước nghỉ | Hơn sáu giờ rồi... mọi người gần như đã về hết. | Cần thu |
| `act1_ty_water_001.wav` | TÝ: Uống nước ngoài bước uống nước bắt buộc | Một ngụm nước. Mình phải tỉnh táo thêm chút nữa. | Cần thu |
| `act1_ty_coffee_001.wav` | TÝ: Tương tác cốc cà phê | Cà phê lạnh ngắt rồi... mình quên uống từ lúc nào nhỉ? | Cần thu |
| `act1_ty_print_early_001.wav` | TÝ: Kiểm tra máy photo trước khi sửa xong Excel | Mình phải sửa xong file Excel trước khi in. | Cần thu |
| `act1_ty_print_done_001.wav` | TÝ: Kiểm tra lại máy photo sau khi đã lấy báo cáo | Không cần in thêm bản nào nữa. | Cần thu |
| `act1_boss_reminder_001.wav` | SẾP: Nhắc định kỳ khi đang ở bước sửa Excel và không mở minigame | Tôi cần bản sửa trước khi mọi người về hết! | Cần thu |
| `act1_intro_boss_004.wav` | SẾP: Nhận báo cáo | Để trên bàn đi. Được rồi, cậu về đi. | Đã gán voice trong game |
| `act1_window_narration_001.wav` | CỬA SỔ: Nhìn ra cửa sổ | Dòng người vẫn vội vã. Tiếng còi xe chẳng lúc nào ngớt. | Tùy chọn; hiện gắn nhãn CỬA SỔ |

Ba file sếp mở đầu đã có: act1_intro_boss_001.wav, act1_intro_boss_002.wav, act1_intro_boss_003.wav.

Đã gán 4 câu Tý mới + 1 câu sếp mới. Còn 7 câu Tý và 1 câu sếp nhắc việc chưa có bản thu; thêm 1 câu tả cảnh cửa sổ nếu muốn có giọng đọc. Các thông báo HỆ THỐNG, MÁY PHOTO và gợi ý phím H có thể giữ chữ, không tính vào số câu cần thu.

Loại khỏi danh sách trước: câu về viên bi cũ (không có object tương tác được gắn trong scene hiện tại); câu “Bản Excel đã gửi rồi. Hôm nay mình dừng ở đây thôi...” (nhánh AfterWork không được luồng hiện tại chuyển tới). Các tương tác mở cửa, ngồi, gõ máy và đưa giấy chưa có câu Tý riêng; dùng SFX tương ứng.

Định dạng đầu vào: WAV PCM, mono, 16-bit; 44,1 hoặc 48 kHz đều được. Không chèn nhạc nền, không cắt mất đầu/cuối từ. Khi nhập Unity sẽ tối ưu thiết lập âm thanh theo vai trò.
