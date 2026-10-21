# Voice gameplay Scene 1

Nguồn: Downloads của người dùng, 21/10/2026. Giữ nguyên 4 voice intro đã có (hash trùng).

| File mới | Giây audio | Giây phụ đề/animation |
|---|---:|---:|
| `act1_intro_boss_004.wav` | 6.074 | 6.324 |
| `act1_ty_print_pickup_001.wav` | 2.562 | 2.812 |
| `act1_ty_print_ready_001.wav` | 3.630 | 3.880 |
| `act1_ty_rest_001.wav` | 2.353 | 2.603 |
| `act1_ty_water_after_report_001.wav` | 3.237 | 3.487 |

Người dùng đã xác nhận _004 là câu nhận báo cáo, không phải câu mở đầu thứ tư. File giữ nguyên tên nguồn nhưng chỉ phát ở bước giao báo cáo.

Asset Resources/OfficeGameplayVoices.asset gán đúng người nói và nội dung. OfficeSceneController phát qua DialogueVoicePlayer riêng cho gameplay: thay dòng sẽ dừng voice cũ, Esc pause/resume theo GameManager, hủy scene dừng phát. Phụ đề và sự kiện animation dùng độ dài AudioClip + 0,25 s. Các câu chưa có bản thu giữ thời lượng phụ đề cũ.

WAV mono PCM 16-bit, 22050 Hz, Decompress On Load, preload, không streaming; các câu ngắn cần phát ngay. Không đổi dữ liệu file nguồn và không cần ghi lại scene.

Kiểm tra: KuTy/Tests/Check office opening transition. Ngoài intro và animation sếp, kiểm tra luồng in/nhặt giấy/nộp/uống nước/nghỉ, đúng clip và thời lượng, pause/resume, không chồng tiếng và voice nghỉ kết thúc trước chuyển scene.
