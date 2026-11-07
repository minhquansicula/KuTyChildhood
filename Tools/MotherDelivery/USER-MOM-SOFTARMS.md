# Tay mềm hơn — bản cần duyệt

- File mới: `Source/Mom_User_Gameplay_SoftArms.blend`; giữ nguyên `Mom_User_Gameplay.blend` để so sánh.
- Rig vẫn `Mom_User_Gameplay_Rig`, cùng mesh từ `mom.fbx`; không chỉnh hình học, UV, texture, material, skin weights, skeleton hoặc tỷ lệ.
- Chọn các action hậu tố **`_Gameplay_SoftArms`**, không chọn action gameplay cũ hoặc `_Trial01`.

## Điều chỉnh

1. Tư thế nghỉ có khuỷu chùng thêm một chút, không duỗi tay thành một khối.
2. CallChild dùng nhịp nâng/hạ liên tục thay vì giữ nguyên độ nâng ở đoạn giữa.
3. Cẳng tay theo sau cánh tay trên nhẹ; cổ tay có độ gập/lệch nhỏ thay vì luôn khóa cứng với cẳng tay. Không ép hướng lòng bàn tay tuyệt đối và không thêm xoắn dọc cổ tay.
4. Idle, TalkIdle, Nod có cử động cổ tay rất nhỏ, không khoa tay liên tục. Walk giữ chân/root của bản đã kiểm tra, chỉ thêm chuyển động tay nhỏ.

Đo trên mọi frame của CallChild mới: góc xoay cục bộ cổ tay tối đa khoảng **4,9°**, bước quay xương lớn nhất khoảng **4,23°/frame**, so với khoảng **6,36°/frame** ở bản gameplay trước. Đây chỉ là phép đo, không tự chứng minh chuyển động đẹp hơn.

## Xem thử

- Action gọi mới: `MomUser_CallChild_Gameplay_SoftArms`, frame **1–103**, 30 FPS. File mở ở frame 43; nhấn Space để xem.
- Bốn action còn lại giữ thời lượng cũ: Idle/TalkIdle 1–151, Nod 1–55, Walk 1–49.
- Chuỗi bộ đứng mới: `PREVIEW_MomUser_Gameplay_SoftArms_StandingTransitions`, frame **1–1215**. Các preview không có chữ SoftArms vẫn là chuyển động cũ.
- Thư mục preview mới: `Preview/UserMomAnimation/SoftArms/`.
- `CallChild_SoftArms_30fps.mp4`: trước và bên hông, render **từng frame**, phát 30 FPS. Có kèm frame cuối nên video dài 3,433 s; khoảng keyframe của clip là 3,4 s.

## Giới hạn

Các ngón vẫn giữ dáng bàn tay mở của nguồn vì rig chưa có điều khiển ngón riêng. Lần sửa này làm mềm nhịp cánh tay/cẳng tay/cổ tay, không tuyên bố đã diễn hoạt ngón.

Kiểm tra tự động toàn bộ frame: trọng số, sàn, drift root, loop và điểm nối bộ đứng; xem `Source/user_mom_softarms_validation.json`. Không đánh đồng phép đo với duyệt bằng mắt liên tục. Ảnh material/clay đa góc và video được cung cấp để duyệt; chưa xuất FBX hoặc kiểm chứng Unity. Bộ ngồi cũ và chuyển Idle/Walk vẫn giữ các giới hạn đã ghi trong `USER-MOM-GAMEPLAY.md`.
