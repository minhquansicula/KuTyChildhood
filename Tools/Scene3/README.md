# Năm mẫu nhà quê cho scene 3

Các mẫu là tài sản trang trí low-poly, hợp với đường làng và ruộng lúa nước. Cả năm căn có mái chính bằng ngói cũ, mái hiên bằng tôn gợn sóng thật, tường/gỗ bạc màu nhẹ, cửa, cửa sổ và bậc thềm. Mỗi sóng hiên là một **nửa đường tròn** kéo dài theo độ sâu hiên, nối với sóng kế bên thành một mesh liên tục, dày 6 mm. Texture tôn có những mảng rỉ nhẹ. Texture màu là PNG riêng nên mang sang Unity được.

| Prefab | Dáng nhà |
| --- | --- |
| `Nha01_NgoiDo_HienTon` | Nhà vôi vàng, ngói đất đỏ, hiên tôn đỏ cũ |
| `Nha02_NgoiNau_HienTon` | Nhà quét vôi sáng, ngói nâu, hiên tôn xám, chái bên |
| `Nha03_NgoiReu_HienTon` | Nhà vách đất, ngói cũ có rêu, sàn cao và cột tre |
| `Nha04_NgoiDoSam_HienTon` | Nhà rộng, ngói đỏ sẫm, hiên tôn đỏ cũ và chái bên |
| `Nha05_NgoiCu_HienTon` | Nhà vôi xanh nhạt, ngói nâu cũ, hiên tôn xám |

Prefab Unity: `Assets/_Project/Prefabs/Scene3/RuralHouses/`. Mỗi prefab có vật liệu URP và BoxCollider riêng. FBX nguồn: `Assets/_Project/Art/Models/Scene3/RuralHouses/`; texture: `Assets/_Project/Art/Textures/Scene3/RuralHouses/`.

Kéo prefab từ cửa sổ Project vào scene, đặt nền hiên ở cao độ mặt đất, rồi xoay cửa về phía đường. Các căn là ngoại thất trang trí; cửa tối là hình khối, chưa có nội thất hay gameplay.

File Blender sửa được: `Tools/Scene3/NamNhaQue_Source.blend`. Ảnh xem trước: `Tools/Scene3/NamNhaQue_Preview.png`. Khi sửa bộ dựng, chạy `generate_rural_houses.py` bằng Blender 5.x để xuất lại FBX, rồi trong Unity chọn **KuTy > Scene 3 > Build five rural house prefabs** để cập nhật prefab.

Năm căn đã được đặt trong `Act3_MemoryWorld_OutSide.unity`, dọc đường làng và xen kẽ trái/phải giữa các thửa ruộng. Vị trí lần lượt là `(-18, -9)`, `(18, 8)`, `(-18, 27)`, `(18, 65)`, `(-18, 84)` theo tọa độ X/Z; mặt hiên hướng ra đường. Mỗi căn có nền đất và lối đi nhỏ. Năm mesh lúa tương ứng dùng phiên bản đã chừa khoảng sân, còn OBJ lúa gốc vẫn giữ nguyên. Tiệm tạp hóa, sân bi và đường chính không bị dời.

Để dựng lại bố cục sau khi sửa nguồn: chạy `cut_house_lots.py`, mở Unity, chọn **KuTy > Scene 3 > Place five rural houses among rice fields**. Lệnh đặt nhà có thể chạy lại, chỉ thay nhóm `NhaDan_XenGiuaRuongLua` và năm tham chiếu mesh lúa của nhóm này.
