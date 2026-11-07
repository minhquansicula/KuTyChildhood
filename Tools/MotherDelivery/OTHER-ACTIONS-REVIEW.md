# Kiểm tra các hành động khác — ArmAlignment

File kiểm tra: `Source/Mom_User_ArmAlignment.blend`, rig `Mom_User_Gameplay_Rig`. Không sửa action, mesh, weight hoặc ghi đè file .blend trong lượt kiểm tra này.

## Chọn xem trong Blender

Chọn rig `Mom_User_Gameplay_Rig`. Trong **Action Editor**, tìm đúng tên action bên dưới, đặt Start=1 và End tương ứng rồi nhấn **Space**. Không chọn các bản cũ `_Gameplay`, `_SoftArms`, `_Trial` rồi nhầm với bản đang kiểm tra.

| Action | End | Thời lượng | Nhận xét / giới hạn |
| --- | --- | --- | --- |
| `MomUser_Idle_Gameplay_ArmAlignment` | 151 | 5 s | Đứng yên, thở/đổi trọng tâm rất ít; loop khớp. Bàn tay nguồn còn mở và khá tĩnh, chưa có diễn hoạt ngón. |
| `MomUser_TalkIdle_Gameplay_ArmAlignment` | 151 | 5 s | Loop khớp. Cử chỉ tay phải chỉ khoảng 11 mm trước/sau và 7 mm lên/xuống; cần duyệt xem có quá giống Idle không. |
| `MomUser_Nod_Gameplay_ArmAlignment` | 55 | 1,8 s | Một nhịp gật nhẹ, trở về tư thế đứng; không loop. Cần xem video để duyệt nhịp và cảm giác. |
| `MomUser_Walk_InPlace_Gameplay_ArmAlignment` | 49 | 1,6 s | Loop/in-place khớp, nhưng **tay vẫn dùng hướng gập khuỷu trước lượt sửa ArmAlignment**. Chưa đạt để ghép trực tiếp với Idle mới. |

Tất cả action là 30 FPS. Thời lượng bảng tính `(End-1)/30`; video có thêm frame cuối để đối chiếu nên dài hơn 1/30 giây.

## Phạm vi kiểm tra

- **Tự động trên mọi frame của bốn action:** drift root, scale xương, bước quay giữa frame, đầu/cuối loop, đáy từng bàn chân. Root không dịch chuyển tích lũy; scale xương không thay đổi. Không thấy xuyên sàn đáng kể trong các clip riêng; chân vung Walk có nâng lên là chủ ý.
- Idle/Nod có chuyển động vị trí cổ tay rất nhỏ (dưới 1 mm); đó là chủ ý của bản hiện có, không chứng minh tay đã tự nhiên. Các ngón còn dáng nguồn.
- **Bằng mắt:** xem ảnh material/clay mẫu và contact sheet theo thời gian. Không tự nhận đã xem liên tục đầy đủ video ba góc ở tốc độ thật, không kết luận hết biến dạng chỉ từ số liệu.
- Video `Preview/UserMomAnimation/OtherActionsReview/*_ThreeViews_30fps.mp4` render từng frame của nguồn, gồm trước/hông/sau. Ảnh clay mẫu và contact sheet góc hông giữ cùng thư mục.

## Chưa đạt / chưa kiểm chứng

- Walk cần sửa đồng bộ tay, rồi kiểm tra lại chuyển Idle ↔ Walk; không cắt thẳng giữa hai tư thế hiện tại.
- Bộ SitDown/Idle_Seated/StandUp **vẫn thử nghiệm**: đây không phải lượt duyệt lại toàn bộ động tác ngồi hay kiểm tra với ghế. Các lỗi vặn tay/giao áo–quần/biến dạng sau quần đã ghi trước vẫn chưa được chứng nhận hết.
- Chưa có clip được phê duyệt game-ready, không xuất FBX, không chỉnh Unity hoặc Git.

Báo cáo máy: `Source/user_mom_other_actions_review.json`; danh sách video: `Source/user_mom_other_actions_videos.json`.
