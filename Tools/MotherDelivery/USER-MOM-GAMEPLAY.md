# Mẹ từ mom.fbx — bản gameplay cần duyệt

**Chưa phải bản game-ready. Chưa xuất FBX, chưa import lại FBX và chưa kiểm tra Unity.**

## File và nhận dạng

- File làm việc: `Source/Mom_User_Gameplay.blend`.
- Nguồn: `C:/Users/LENOVO/Downloads/mom.fbx`, đối chiếu trực tiếp bằng hash đỉnh/mặt và UV; khớp mesh gốc trong GarmentRepair và Household.
- Chiều cao trung tính đo lại: **1,14982199 m**; kích thước nguồn XYZ: **0,710065 × 0,219626 × 1,149822 m**. Không tự căn tỷ lệ người trưởng thành.
- Backup trạng thái đang mở trước sửa: `Source/Mom_User_BeforeGameplay_20261106_154911.blend`.
- Rig nguồn: `Mom_User_Deform` (23 xương). Bản làm việc dùng `Mom_User_Gameplay_Rig`, sao chép từ `Mom_User_Rig_Repaired` (28 xương, gồm 5 xương phụ vải đã bake).
- Mesh làm việc: `Mom_User_Gameplay_ShirtBody`, `Mom_User_Gameplay_PantsFeet`.
- Không thay model khác. Không thay topology/UV/material/texture/transform hoặc skin weights trong lượt gameplay này. Hai phần áo/quần được kế thừa nguyên trạng từ GarmentRepair, không tách thêm.
- Action cũ giữ nguyên; các thử nghiệm gameplay trước giữ hậu tố `_Trial01`/`_Trial02`, không dùng cho demo.

## Năm action riêng, 30 FPS

| Action | Frame | Thời lượng | Loại | Trạng thái |
| --- | --- | --- | --- | --- |
| `MomUser_Idle_Gameplay` | 1–151 | 5 s | Loop, in-place | Cần duyệt |
| `MomUser_CallChild_Gameplay` | 1–103 | 3,4 s | Một lần, in-place | Cần duyệt |
| `MomUser_TalkIdle_Gameplay` | 1–151 | 5 s | Loop, in-place | Cần duyệt |
| `MomUser_Nod_Gameplay` | 1–55 | 1,8 s | Một lần, in-place | Cần duyệt |
| `MomUser_Walk_InPlace_Gameplay` | 1–49 | 1,6 s | Loop, in-place | Cần duyệt thêm bước/chuyển trạng thái |

Thời lượng tính `(frame_end-frame_start)/30`; frame cuối của loop trùng đầu. Tất cả có fake user để giữ sau khi đóng/mở. Manifest: `Source/user_mom_gameplay_manifest.json`. `file=null` nghĩa là **chưa xuất**, không phải file FBX bị thiếu trong bộ đã bàn giao.

## Sửa tay và chuyển động

- Clip CallChild cũ có góc xoay bàn tay cục bộ lên tới khoảng 154,5°. Bản mới bỏ hướng bàn tay tuyệt đối: bàn tay đi theo cẳng tay, không ép bẻ cổ tay lên để làm dấu gọi.
- Duy trì hướng xoắn cẳng tay theo cánh tay trên. Nội suy động tác gọi theo góc khớp thay vì chạy mục tiêu IK qua vùng gần thẳng hàng với pole; tránh cú đảo khuỷu tay trong bản thử.
- Giơ tay thấp hơn, một lần gọi nhẹ rồi hạ xuống; không vẫy liên tục. TalkIdle là cử chỉ nhỏ riêng, không tái sử dụng CallChild làm loop.
- Nod có một nhịp nhẹ, không cúi cả thân. Idle đổi trọng tâm rất nhỏ, hai chân đứng yên.

## Đã kiểm tra / chưa kiểm tra

**Tự động, toàn bộ frame của từng action:** trọng số hợp lệ/tối đa 4 ảnh hưởng, root không trôi vị trí/hướng, không đổi scale theo clip, loop khớp, đầu/cuối bộ đứng dùng cùng tư thế, ma trận điểm nối bộ đứng sai khác 0. Kiểm tra sàn theo từng bàn chân, không chỉ điểm thấp nhất toàn mesh. Các phép đo không phải chứng nhận hết tự giao nhau của mesh.

**Bằng mắt:** đã xem các ảnh material/clay mẫu và contact sheet nhiều thời điểm, gồm trước/hông/sau CallChild, tư thế đứng và góc hông của Walk/Nod/TalkIdle. Đây **không** phải tuyên bố đã xem liên tục mọi frame ở 30 FPS. Bộ ảnh render và video đa góc được giữ để duyệt tiếp; cần duyệt động tác ở tốc độ thật trong Blender trước khi phê duyệt xuất.

**Chưa:** duyệt liên tục đầy đủ mọi góc tại 30 FPS, kiểm tra va chạm toàn bề mặt, kiểm tra FBX round-trip hoặc Unity/Generic avatar. Chưa có clip nào được phê duyệt đưa vào demo cuối.

## Walk

- Hướng tiến Blender: `-Y`. Root không có chuyển động tích lũy; hông được hạ 8 mm trong riêng clip đi để còn biên độ bước, không khóa toàn bộ pelvis.
- Chu kỳ hai bước 1,6 s; pha chân trụ 60%; chân trụ lùi 0,16 m trong không gian in-place.
- Tốc độ tiến lý thuyết tương ứng: **0,166667 m/s**, một sải cùng chân **0,266667 m**, một bước **0,133333 m**, theo đúng scale nguồn. Khi đổi tỷ lệ ở Unity, tốc độ khoảng cách phải đổi tương ứng.
- Sau sửa, đáy chân trụ ở các frame đã kiểm tra nằm gần sàn; sai lệch lớn nhất khoảng 0,105 mm ở bên phải. Chân vung nâng khoảng 18 mm. Đây không phải kiểm chứng foot locking trong Unity.
- Idle và Walk không cùng tư thế chân ở điểm cắt thẳng. Có preview crossfade 0,3 s trong Blender; các frame chuyển kiểm tra còn xuyên sàn tối đa khoảng **1,642 mm**, cần tinh chỉnh thêm trước khi duyệt. Không coi cắt thẳng Idle/Walk là đã đạt.
- Không tạo TurnLeft/TurnRight vì chưa kiểm chứng quy trình root-turn.

## Cách xem

File được mở trên rig **`Mom_User_Gameplay_Rig`**, action **`MomUser_CallChild_Gameplay`**, frame 52 để đối chiếu tay. Đừng chọn action `MomUser_CallChild` cũ hoặc các action `_Trial` rồi nhầm là bản mới.

- Preview nối bộ đứng: `PREVIEW_MomUser_Gameplay_StandingTransitions`, frame **1–1215**. Thứ tự Idle → CallChild → Idle → TalkIdle → Idle → TalkIdle → Nod → TalkIdle → Idle.
- Preview Idle/Walk: `PREVIEW_MomUser_Gameplay_IdleWalkTransitions`, frame **1–417**; vẫn thử nghiệm.
- Hai action preview không được xuất thành clip gameplay.
- Ảnh/video: `Preview/UserMomAnimation/Gameplay/`. Mỗi `*_Review_10fps.mp4` có 6 ô: material trước/hông/sau và clay trước/hông/sau. Video giữ đúng thời lượng, nhưng lấy mỗi frame thứ ba của nguồn 30 FPS, tức **10 FPS preview**, không phải chứng nhận 30 FPS.
- `StandingTransitions_Review_10fps.mp4` là chuỗi liên tục khoảng 40,5 giây của preview bộ đứng.

## Bộ ngồi và tương tác bếp

SitDown/Idle_Seated/StandUp cũ vẫn **thử nghiệm, chưa đạt**: giao nhau áo/quần, biến dạng nặng sau quần và lỗi tay ở các action ngồi chưa được xử lý bằng lượt sửa bộ đứng này. Không thêm ngồi xổm, nấu ăn hay động tác chạm đạo cụ giả.

## Xuất Unity — chưa thực hiện

Thư mục dự kiến `Assets/_Project/Characters/MomUserDemo/` chưa tồn tại khi kiểm tra và chưa được tạo. Không thay `Characters/Mother`. Không sửa Unity code, scene, Git; không commit/push `.blend`.

Phát hiện `MotherDeliveryImporter` trong `Assets/_Project/Scripts/Editor/MotherDeliveryBuilder.cs`, chỉ áp dụng cho `Assets/_Project/Characters/Mother/`, không áp dụng cho MomUserDemo. **Không chạy menu Build Mother Delivery cũ** để xuất bộ này. Chưa kiểm tra toàn bộ importer/plugin bên ngoài Assets.

Chưa có cấu hình export nào được thực thi hoặc xác nhận, chưa xuất asset. Sau khi bộ đứng được duyệt liên tục: dùng chung skeleton, Generic nếu chưa kiểm chứng Humanoid, bake 30 FPS từng action riêng, không NLA/preview/backup/camera/đèn, rồi import lại vào scene kiểm tra riêng trước bàn giao. Không tự đánh dấu game-ready.

## Báo cáo máy

Các file `Source/user_mom_gameplay_*.json`: `provenance`, `preservation`, `manifest`, `validation`, `foot_validation`, `preview_manifest`, `reopen_audit`, `status`. Báo cáo ngồi cũ vẫn giữ riêng, không đổi thành đạt.
