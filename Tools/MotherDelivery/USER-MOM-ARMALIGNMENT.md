# Sửa hướng gập khuỷu — bản thử cần duyệt

File hiện tại: `Source/Mom_User_ArmAlignment.blend`.
Action đang mở: `MomUser_CallChild_Gameplay_ArmAlignment`, frame **72**, cùng frame đã chẩn đoán từ bản SoftArms. Nguồn vẫn là model mom.fbx đã xác minh; không thay nhân vật.

## Phát hiện

Làm mượt thời gian và giới hạn góc cổ tay không tự giải quyết dáng cánh tay. Ở frame 72 của SoftArms, tay thả bên trái có khuỷu nằm phía trước vai/cổ tay: Y khuỷu khoảng -0,0537 m, Y cổ tay -0,0446 m (phía trước Blender là -Y). Cẳng tay vì vậy hướng ngược ra sau khi buông tay. Đây là sai hướng gập khuỷu trong tư thế diễn hoạt, không thể kết luận đã sửa chỉ bằng góc cổ tay nhỏ.

Chẩn đoán trọng số các đỉnh quanh cổ tay chưa thấy ảnh hưởng nhầm từ chân, thân hoặc tay đối diện. Chưa có bằng chứng để tự ý sửa weight cả vùng. Không thay skin weights hoặc topology trong lượt thử này.

## Thay đổi đã giữ

- Đổi hướng tham chiếu khuỷu từ trước sang sau-bên cho **bốn action đứng**: Idle, CallChild, TalkIdle, Nod.
- Ở frame đối chiếu mới, khuỷu tay thả có Y khoảng +0,0201 m, cổ tay vẫn -0,0446 m: cẳng tay gập về trước, thay vì bẻ ngược.
- Bảo toàn nhịp mềm và độ cử động cổ tay nhỏ của SoftArms. Không đổi bone roll/rest skeleton, không Apply Transform, không đổi mesh/UV/material/texture/tỷ lệ.
- Các action trước vẫn giữ nguyên. Backup trạng thái trước sửa: `Source/Mom_User_BeforeArmAlignment_20261106_162904.blend`.
- Thử nghiệm định hướng trục hinge riêng **không được bake/chọn**; không coi ảnh `Diagnostic_HingeAxis` là kết quả sửa được giữ.

## Kiểm tra

- Đã render đối chiếu Before/After đúng frame 72 từ trước, hông và sau; ảnh trong `Preview/UserMomAnimation/ArmAlignment/`.
- Tự động kiểm tra toàn bộ frame của các clip, loop, điểm nối bộ đứng, root, sàn và trọng số. Xem `Source/user_mom_armalignment_validation.json`.
- Video gọi con mới render từng frame ở 30 FPS, trước/hông: `CallChild_ArmAlignment_30fps.mp4`. Ảnh clay và material giữ riêng để duyệt.
- Những phép đo và ảnh mẫu **không phải chứng nhận hết lỗi tay ở mọi góc**. Cần duyệt tiếp chuyển động và đặc biệt vùng khuỷu/cẳng tay/cổ tay.

## Không thuộc phần đã sửa

- Walk có action sao chép mang hậu tố ArmAlignment nhưng **chưa áp dụng hướng khuỷu mới**; không dùng để kết luận sửa tay khi đi đã đạt. Chưa sửa điểm nối Idle/Walk theo tư thế mới.
- Ngón vẫn giữ hình bàn tay nguồn, chưa có rig ngón.
- Bộ ngồi vẫn thử nghiệm với các lỗi đã ghi trước đó.
- Chưa xuất FBX, chưa thử Unity. Không dùng action/preview SoftArms cũ để đánh giá bản ArmAlignment.
