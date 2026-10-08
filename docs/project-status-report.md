# Báo cáo hiện trạng — Chìa Khóa Ký Ức

Cập nhật: **04/10/2026**. Đối chiếu `docs/new_script.md`, source hiện tại, Build Settings và scene đang mở trong Unity 2022.3.62f3 / URP. Lượt này tập trung vào **scene2: Act2_MemoryWorld_Home**, phần người dùng phụ trách.

## Cấu trúc hiện tại

Build Settings hiện bật 5 scene, theo thứ tự:

1. `MainMenu`
2. `Act1_RealWorld`
3. `Act2_MemoryWorld_Home`
4. `Act3_MemoryWorld_OutSide`
5. `Act_Ending`

Tên cũ `Act2_MemoryWorld` và `Act3_Ending` trong báo cáo 24/09 không còn phản ánh cấu trúc project chính. Thư mục `UnityChairValidation` là bản kiểm tra riêng, không phải nguồn của lần chỉnh sửa này.

Source đã có các hệ thống core, điều khiển first-person, tương tác, quest, inventory, currency, memories, shop, rửa chén, bắn bi, UI và audio. Act 1 có thêm `OfficeOpeningSequence`, `OfficeExhaustionSequence` và `OfficeReportMiniGame`. Sự tồn tại của source không đồng nghĩa toàn bộ luồng đã hoàn thiện hoặc đã được kiểm thử lại ở phiên này; bỏ các tỷ lệ “100%” của báo cáo cũ vì chưa có bằng chứng kiểm thử hiện tại tương ứng.

## Scene2 — hiện trạng đã đọc và kiểm tra

| Phần | Hiện trạng |
| --- | --- |
| Nhà và phòng ngủ | Đã có `TraditionalHouseRoom_Visual` và `RusticBedroom_Visual`, kết hợp với tường và đồ bếp placeholder. Bố trí nhà, giường và Ground đã được người dùng di chuyển trước lượt chỉnh sửa này. |
| Giường | `BedWakeUpController` đưa nhân vật lên `BedBlock`, khóa input và hiện lời nhắc sau 1 giây. Xuống giường mở khóa input và hiện checklist. `BedInteractable` cho phép lên lại giường. |
| Camera | `Player/PlayerCamera` là camera con của Player; chiều cao local hiện **1,8** (nâng tiếp từ 1,5 theo yêu cầu mới). Chỉ override trong scene2. |
| Tường mới | Nhóm `Act2_RusticWalls` gồm 3 module: `Wall_01_AgedPlaster` (vữa cũ), `Wall_02_WoodenShutters` (cửa sổ gỗ đóng), `Wall_03_TimberWainscot` (chân ốp gỗ). Tường cao khoảng 3 m, rộng 3,1 m/module, đặt dọc mép sau nhà tại Z = 12,45. Dùng vùng texture vữa vàng kem và gỗ nâu từ chính model nhà cũ. Mỗi module có collider cho thân tường; các chi tiết gỗ là trang trí. |
| Ground | Giữ vị trí `(4.89, -0.15, 2)`, kích thước `17 × 0.3 × 22`, layer Ground và BoxCollider. Đổi mesh UV và material sang vùng gạch đỏ cũ của texture Floor. |
| Điểm xuống giường | Dùng Transform **godownbed** qua field `bedExitPoint` trên BedWakeUpManager, cộng offset thế giới `(0.55, 0, 0)` để nhân vật đứng cạnh collider. Đích luôn lấy theo vị trí hiện tại của marker; di chuyển marker sẽ đổi điểm xuống ở lần bấm E tiếp theo. |
| Checklist | `KitchenFlowManager` tạo 5 việc: múc nước, vo gạo, nhóm lửa, nấu cơm, dọn mâm. UI xuất hiện khi xuống giường. |
| Mini-game bếp | Có `MashToCompleteTask`, `HoldToCompleteTask`, `RiceWashingTask`; hoàn thành gọi `MarkTaskCompleted` theo tên object cha. |
| Quest chính | `QuestManager` ép bước `ReadJournal` sang `WashDishes`; rửa chén hoàn tất mới chuyển sang mua kẹo và bi. Checklist nấu ăn chưa gọi chuyển bước quest. |

## Thay đổi trong lượt này

- Ground dùng `GroundTerracotta.asset` và `Act2GroundTerracotta.mat`, lấy texture từ Floor hiện có. Mesh chia UV thành các mảng gạch theo tỷ lệ thế giới để tránh trải cả atlas (có vùng đen, tường, vật dụng) lên sàn. Không chỉnh texture/material gốc của Floor.
- Camera scene2 nâng từ `1.3` lên `1.5`, sau đó lên **`1.8`** theo trục Y local. Menu dựng lại sàn cũng đã cập nhật mức 1,8 để không hạ camera khi chạy lại.
- Tạo 3 object tường dưới `Act2_RusticWalls`; có thể kéo riêng từng module trong Hierarchy. Ba material riêng `Act2Wall_AgedPlaster`, `Act2Wall_DarkTimber`, `Act2Wall_WeatheredBase` dùng lại atlas của `tripo_part_1`; không sửa material gốc. Menu `KuTy > Setup > Create Scene 2 Rustic Walls` tạo nhóm nếu chưa có, không nhân bản hoặc ghi đè các module đã tồn tại.
- `BedWakeUpController` ưu tiên `bedExitPoint` (đã gán `godownbed`) cộng `bedExitOffset`, cả hai chỉnh được trong Inspector. Khoảng cách cũ `exitRightDistance = 1.55` chỉ còn là fallback khi thiếu marker. Reset `canWakeUp` khi lên lại giường để chờ đúng thời điểm hiện prompt.
- Có menu `KuTy > Setup > Refresh Scene 2 Ground and Camera` để dựng lại sàn nếu cần. Menu chỉ chạy khi đang mở đúng scene2 ở Edit Mode; dùng lại asset và giữ collider/bố trí hiện có. Nếu đổi scale Ground, chạy lại menu để cập nhật mật độ gạch.

## Việc còn lại của scene2

1. **Đồng bộ checklist với QuestManager.** Hiện checklist chỉ đánh dấu UI và phát SFX, chưa có bước tổng kết để điều khiển nhiệm vụ chính.
2. **Kiểm tra việc vo gạo trong scene.** Source có `RiceWashingTask` và checklist có `RiceWashingBowl`, nhưng hierarchy đang mở không có object `RiceWashingBowl`.
3. **Dọn component gắn trùng.** Các `InteractTrigger` của FirewoodStove, WaterJar, RicePot, DishCabinet và DiningTable hiện có 3 component task cùng loại trên mỗi object; cần kiểm tra lại wiring trước khi nghiệm thu luồng bếp.
4. **Hoàn thiện bố trí và hình ảnh.** Vẫn còn tường, lu/bếp/bàn dạng placeholder; cần căn model phòng ngủ, giường và collider khi tiếp tục level design.
5. **Bám kịch bản mới.** `new_script.md` mô tả thức dậy trong nhà xưa, khám phá và ký ức rửa chén cùng mẹ, sau đó ra tiệm tạp hóa. Các việc nấu ăn hiện tại là phần gameplay mở rộng; cần nối mạch lời thoại, rửa chén và chuyển ra ngoài phù hợp kịch bản.

Các mục này được ghi nhận để cập nhật status, chưa sửa ngoài các yêu cầu sàn/camera/điểm xuống giường và thêm object tường.

## Kiểm chứng lượt này

- Unity biên dịch source thành công.
- Tường mới đã được kiểm tra bằng render trong Editor; camera local Y = 1,8 và ba module được lưu trong scene2. Các tường nằm dọc mép sau nhà, không đặt vào vùng `godownbed`.
- Đã render ảnh Ground trong Play Mode để kiểm tra vùng gạch của atlas và mật độ lát sàn.
- Trước khi chuyển sang marker: kiểm tra runtime có chờ prompt, lần xuống giường đầu và lần lên lại/xuống tiếp đều đạt. Đích teleport khi đó là `(11.84, 0.05, 2.11)`, camera local Y = `1.5`, input được trả lại và checklist xuất hiện. Kiểm tra gọi trực tiếp cùng handler mà phím E sử dụng, không mô phỏng bàn phím vật lý.
- Cập nhật marker `godownbed`: đã biên dịch và kiểm tra reference/offset được lưu trong scene. Chưa chạy lại Play Mode cho điểm xuống mới.
- Hai lần kiểm tra thủ công ban đầu bị ảnh hưởng bởi trạng thái Pause/Edit Mode; đã chạy lại hai chu kỳ tự động với chờ prompt và cả hai đều đạt. Phiên chạy lại không có log Error.
- Chưa chạy lại toàn bộ story smoke test, build Windows hoặc các mini-game bếp trong lượt này. Các kết quả build/smoke cũ ở `verification-results.md` là lịch sử, không phải kết quả xác nhận cho bản hiện tại.
