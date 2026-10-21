# Scene 2 — danh sách thu voice

Hai câu mẹ gọi dậy và lá thư đã được cập nhật theo văn bản người dùng chốt ngày 21/10/2026. Không đọc các hướng dẫn phím Tab/H đi kèm lời nhắc UI.

Đã nhập 15 WAV người dùng cung cấp và gán tại các mốc Scene 2. Hai file mẹ tiếp tục tiến độ và mẹ nhắc xếp dụng cụ chưa có, nên giữ chữ. Những lời hồi tưởng hiện có được phân vai để thu: mẹ đọc câu xếp dụng cụ; Tý đọc 12 lời hồi tưởng sau các việc khác và câu nhận mảnh ký ức về mẹ. Đây không phải các câu đáp mới được thêm vào gameplay.

## Mẹ

| File | Khi phát | Nội dung |
|---|---|---|
| `act2_mom_wakeup_001.wav` | Tý xuống giường | Con dậy rồi đấy à. Vào dọn đồ, phụ mẹ một tay nhé. |
| `act2_mom_resume_001.wav` | Tiếp tục tiến độ đã lưu | Mình tiếp tục việc đang làm nhé. |
| `act2_mom_tools_001.wav` | Xếp dụng cụ xong | Dùng xong nhớ cất, mai còn tìm được con nhé. |
| `act2_mom_letter_001.wav` | Đọc thư cuối Scene 2 | Sau này đi đâu làm gì, con cũng nhớ phải ăn cơm đúng bữa nhé. Nhà mình luôn có một chỗ chờ con. |

## Tý — 13 file

| File | Khi phát | Nội dung |
|---|---|---|
| `act2_ty_keepsakes_001.wav` | Xếp đồ vào rương | Chiếc rương giữ cả những mùa áo cũ. |
| `act2_ty_radio_001.wav` | Chỉnh radio | Tiếng radio mở đầu mỗi buổi sáng của nhà mình. |
| `act2_ty_grind_corn_001.wav` | Xay ngô | Tiếng cối quay đều như nhịp tay mẹ. |
| `act2_ty_firewood_001.wav` | Gom củi | Bó củi nhỏ mang theo hơi ấm cho cả nhà. |
| `act2_ty_fetch_water_001.wav` | Múc nước | Gáo nước mát nhờ mẹ để dành từ sáng. |
| `act2_ty_measure_rice_001.wav` | Đong gạo | Mẹ luôn đong thêm một chút cho con. |
| `act2_ty_wash_rice_001.wav` | Vo gạo | Nước đục đi, hạt gạo lại trắng. |
| `act2_ty_light_fire_001.wav` | Nhóm lửa | Bếp bắt lửa, cả nhà bắt đầu ấm. |
| `act2_ty_cook_rice_001.wav` | Nấu cơm | Mùi cơm chín khiến con biết sắp được gọi về. |
| `act2_ty_set_table_001.wav` | Dọn mâm cơm | Mâm cơm đủ người là bữa ngon nhất. |
| `act2_ty_wash_dishes_001.wav` | Rửa chén | Mẹ rửa, con tráng, hai mẹ con chuyện trò. |
| `act2_ty_put_away_dishes_001.wav` | Cất chén | Tiếng chén khẽ chạm nhau, việc nhà đã xong. |
| `act2_ty_mom_memory_001.wav` | Nhận mảnh ký ức về mẹ | Mẹ xoa đầu khen mình ngoan. Cảm giác thật bình yên. |

Giọng Tý: đọc tự nhiên như lời nghĩ thầm, ấm và nhẹ; mẹ: gần gũi, dịu, lá thư đọc chậm hơn. Xuất WAV mono PCM 16-bit, giữ nguyên tên file.

Không có dòng gấp chăn vì nhiệm vụ đó không nằm trong Order của luồng Scene 2 hiện tại. Nội dung nguyên gốc: HouseholdChoreFlow.Memories, thông báo ShowChecklist và MemoryCollectionManager.GetMemoryDescription(FilialLove).
