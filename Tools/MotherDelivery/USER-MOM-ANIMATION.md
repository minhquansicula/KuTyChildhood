# Animation cho mom.fbx của người dùng

Nguồn: `C:\Users\LENOVO\Downloads\mom.fbx`. File mới nhất: `Source/Mom_User_Household.blend`, scene `USER_Mom_FBX_Review`. Bộ ba clip ban đầu giữ riêng trong `Source/Mom_User_Animated.blend`; bản trước bổ sung động tác: `Source/Mom_User_BeforeHousehold.blend`.

- Giữ nguyên đỉnh, mặt, UV, vật liệu, texture và transform của mesh nguồn. Tạo rig mới, không lấy mẫu mẹ cũ thay vào.
- Bản trước rig: `Source/Mom_User_PreRig.blend`; collection ẩn: `BACKUP_UserMom_BeforeRig`.
- Rig: `Mom_User_Deform`, 23 xương thân người. Auto weights, chuẩn hóa tối đa 4 ảnh hưởng/đỉnh. Chưa có điều khiển ngón tay/mặt riêng.
- Các clip riêng có fake user, 30 FPS:

| Action | Giây | Frame | Lặp |
|---|---:|---|---|
| `MomUser_Idle` | 4 | 1–121 | Có |
| `MomUser_Walk_InPlace` | 1,2 | 1–37 | Có |
| `MomUser_Nod` | 2,4 | 1–73 | Không |
| `MomUser_SitDown` | 2,8 | 1–85 | Không |
| `MomUser_Idle_Seated` | 4 | 1–121 | Có |
| `MomUser_StandUp` | 2,8 | 1–85 | Không |
| `MomUser_CallChild` | 3,6 | 1–109 | Không |

- Chuỗi xem thử `PREVIEW_MomUser_HouseholdSequence`: ngồi xuống → ngồi nghỉ → đứng lên → giơ tay gọi con, khoảng 13,2 giây. Đây là action xem thử riêng, không phải clip gameplay thứ tám. Space để chạy/dừng. Chọn từng action trong Action Editor trên rig `Mom_User_Deform` nếu muốn xem riêng; đổi End theo bảng trên.
- Đi bộ tại chỗ: chưa điều khiển di chuyển nhân vật trong game. Chưa retarget Humanoid hoặc xuất FBX.
- Xem `Source/user_mom_rig_validation.json` và `Source/user_mom_animation_review.json` để biết kết quả kiểm tra. Ảnh đối chiếu: `Preview/UserMomAnimation`.

## Bổ sung động tác sinh hoạt

- Chỉ thêm bốn action mới; ba action gốc được kiểm tra bằng fingerprint, không ghi đè đường cong keyframe của chúng.
- Sửa trọng số ảnh hưởng nhầm của xương chân/tay lên thân áo để giảm kéo giãn khi gập hông và đặt tay trên gối. Dữ liệu đỉnh/mặt/UV/texture không đổi. Vì skin weights được cải thiện trên mesh chung, vẫn cần xem lại biến dạng toàn bộ thư viện clip trước khi dùng trong game.
- `Source/user_mom_household_preservation.json`: kiểm tra bảo toàn.
- `Source/user_mom_household_review.json`: điểm nối, loop và kiểm tra biến dạng lấy mẫu.
- `Source/user_mom_household_manifest.json`: thông số cả bảy clip.
- Ảnh mới: `Preview/UserMomAnimation/Household`.
- Động tác ngồi chưa kèm ghế hoặc ràng buộc tự căn vào nội thất. Cần đặt nhân vật/ghế theo scene game. `CallChild` là giơ/vẫy cả bàn tay, chưa có co duỗi ngón tay độc lập hay lip-sync.
- Điểm nối đứng → ngồi xuống → ngồi nghỉ → đứng lên → đứng và gọi con → đứng đã được kiểm tra bằng ma trận xương, sai khác dưới 0,001. Seated Idle lặp khớp đầu/cuối.
- **Còn hạn chế:** các mẫu tư thế ngồi vẫn có một số cạnh vải/da kéo dài trên 2 lần chiều dài ban đầu, tập trung gần gấu áo/hông và nách. Đây là cảnh báo biến dạng còn lại, không phải bản đã được chứng nhận hết lỗi vải. Chưa kiểm chứng va chạm vải toàn bộ từng frame hoặc khi ngồi vào ghế thật.
- Tư thế ngồi hiện được căn cho ghế cao vừa phải, không phải ngồi xổm hay ghế đẩu thấp. Cần căn ghế theo đúng tỷ lệ model nguồn.
- Đã thử corrective vải ngồi nhưng kết quả kéo giãn cục bộ kém hơn, nên **không giữ corrective đó trong bản đang mở**. Bản thử được lưu riêng ở `Source/Mom_User_ExperimentalClothSupport.blend`, không phải bản bàn giao. Model hoạt động hiện không phụ thuộc driver Shape Key đó.

Đây là bộ animation khởi đầu để duyệt biến dạng, chưa phải chứng nhận game-ready. Cần duyệt chuyển động/va chạm vải, bổ sung trọng số sửa nếu cần trước khi triển khai các động tác ngồi và làm bếp. Chiều cao nguồn hiện khoảng 1,15 m; giữ nguyên scale theo file người dùng, chưa tự đổi chiều cao cho game.
