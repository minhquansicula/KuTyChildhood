# Scene 2 — nhập voice

Đã nhập 15 WAV từ Downloads (21/10/2026). 13 câu Tý và 2 câu mẹ (gọi dậy, thư). Không có bản thu mom_resume và mom_tools; hai chỗ này giữ chữ.

Câu múc nước đã đổi thành: “Gáo nước mát nhờ mẹ để dành từ sáng.” trong code, dữ liệu voice và danh sách thu.

| File | Giây audio |
|---|---:|
| `act2_mom_letter_001.wav` | 5.553 |
| `act2_mom_wakeup_001.wav` | 8.349 |
| `act2_ty_cook_rice_001.wav` | 2.378 |
| `act2_ty_fetch_water_001.wav` | 2.169 |
| `act2_ty_firewood_001.wav` | 2.309 |
| `act2_ty_grind_corn_001.wav` | 1.925 |
| `act2_ty_keepsakes_001.wav` | 2.018 |
| `act2_ty_light_fire_001.wav` | 2.409 |
| `act2_ty_measure_rice_001.wav` | 1.879 |
| `act2_ty_mom_memory_001.wav` | 2.945 |
| `act2_ty_put_away_dishes_001.wav` | 2.726 |
| `act2_ty_radio_001.wav` | 2.378 |
| `act2_ty_set_table_001.wav` | 1.972 |
| `act2_ty_wash_dishes_001.wav` | 3.194 |
| `act2_ty_wash_rice_001.wav` | 2.223 |

Asset: Resources/HomeGameplayVoices.asset. WAV mono PCM 16-bit 22050 Hz, preload, Decompress On Load; giữ nguyên dữ liệu nguồn. HouseholdVoicePlayer dùng một nguồn thoại 2D và dừng/tiếp tục theo GameManager. Hoàn thành việc nhà phát một lần; tương tác hoàn thành lại không phát voice hoặc cộng thưởng. Thông báo giữ ít nhất độ dài clip + 0,25 s.

Cuối chương: mảnh ký ức về mẹ có lời Tý và modal riêng; kết thúc modal đó mới mở thư mẹ. Thư chờ người chơi đóng, không tự chuyển cảnh khi hết voice. Đóng thư dừng voice trước khi chuyển ra sân. DialogueUI tạm dừng cả tiến độ hiện chữ và bộ đếm khi game pause.

Kiểm tra: KuTy/Tests/Check home voices in isolation — 69 kiểm tra đạt. Kết quả Tools/HomeVoices/checks.txt; ảnh Tools/HomeVoices/mother-letter.png. Kiểm thử sao lưu checkpoint ra đĩa, sau đó tự khôi phục khi về Edit Mode. Không lưu đè scene đang mở.
