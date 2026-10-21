# Sếp: nhân vật và animation trong Scene 1

Nguồn: `E:/BLENDER/Boss_Unity_Pack`, ngày 21/10/2026.

Chỉ nhập `Boss_Character.fbx` (6 skinned meshes, rig Generic, 5 clip) và 6 texture sử dụng cho nhân vật. Không nhập model, prefab, vật liệu bàn ghế, scene demo hoặc bộ phát thoại độc lập của gói nguồn. Bàn ghế và các collider trong Scene 1 giữ nguyên.

Prefab: `Assets/_Project/Art/Characters/Boss/Prefabs/BossCharacter.prefab`. Instance đặt dưới `BossPlaceholder_SLOT/Visual_REPLACE`; hai hình khối thay thế cũ được tắt. Nhân vật giữ scale 1, root motion tắt; cao độ được bù 0,019 m theo mặt trên collider bàn đang có. Góc nhìn mở đầu dịch sang trái để màn hình CRT không che mặt sếp, và nhìn vào xương đầu của model mới.

`OfficeIntroController` vẫn sở hữu thoại, TTS, thời lượng và tiến trình game. `BossOfficeAnimation` nhận từng dòng thoại từ controller này:

- Fade: Idle (4 s, lặp).
- Câu sếp 1 và 2: Talk Stern (4,5 s, lặp, không reset giữa hai câu).
- Nhịp đập bàn: Desk Slam (0,8 s, một lần).
- Câu sếp 3: Talk Command (4,5 s, lặp).
- Tý trả lời: Return Idle (1,2 s), sau đó Idle.

Sau intro, animation theo sự kiện `OfficeSceneController.SpeakerChanged`: sếp nói/ra hiệu trong thời lượng phụ đề. Đặc biệt khi nhận báo cáo, câu “Để trên bàn đi. Được rồi, cậu về đi.” dùng Talk Command theo thời lượng voice act1_intro_boss_004.wav (6,074 s + 0,25 s nghỉ), rồi Return Idle → Idle; không đập bàn thêm. Nhắc việc trong lúc sửa Excel cũng có cử chỉ. Chuyển sang lời người khác hoặc ẩn phụ đề sẽ kết thúc cử chỉ nói. Đã gán voice nhận báo cáo và bốn độc thoại Tý trong OfficeGameplayVoices.asset; câu nhắc việc và các độc thoại khác vẫn giữ phụ đề khi chưa có bản thu.

Âm đập bàn cũ phát qua event `OnDeskImpact` của animation tại 14/30 s = 0,466667 s, thay cho âm ngay đầu nhịp. Chặn event lặp để mỗi cú đập chỉ phát một tiếng. Menu Esc đóng băng animation cùng giọng nói; tiếp tục giữ nguyên thời điểm. Vật liệu nhân vật dùng URP Lit, tóc và lông mày dùng alpha clip.

Kiểm tra: menu `KuTy/Tests/Check office opening transition`, kết quả `Tools/OfficeOpening/checks.txt`. Bao gồm rig/mesh không có đồ nội thất, root motion, pause animation, 4 file giọng nói, đúng mốc và số lần phát âm đập bàn, quay về Idle và UI/menu. Ảnh `Tools/OfficeOpening/after-fade.png` và `Tools/BossAnimation/desk-slam.png` ghi lại hình trong game. Chưa có lip-sync theo âm tiết; giữ nguyên animation cơ thể do người dùng cung cấp.

Scene trước khi lắp được sao lưu cục bộ tại `Tools/BossAnimation/before-boss.unity`.
