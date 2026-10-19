# Scene 2 — Một ngày mẹ còn gọi

## Kịch bản và phạm vi

Một buổi sáng trong ký ức, mẹ gọi con dậy phụ việc. Mỗi đồ vật gợi lại một việc nhỏ đã từng làm cùng gia đình. Người chơi đi qua phòng ngủ, phòng kho, gian nhà chính `TraditionalHouseInterior` và gian bếp; cuối ngày tìm được lá thư mẹ cất lại.

14 nhiệm vụ tuần tự, mỗi nhiệm vụ thưởng đúng một lần **10 mảnh ký ức**, tổng **140/140**. Giữ 3 ký ức lớn của cốt truyện độc lập: hoàn tất 14 việc mới trao `FilialLove` và chuyển sang bước cốt truyện tiếp theo. Chưa lưu tiến độ qua lần thoát/chạy lại scene. Esc hủy lượt mini-game đang làm, không mất nhiệm vụ đã hoàn tất; không nhận thưởng khi hủy.

## Nhiệm vụ và tương tác

Mỗi nhiệm vụ bắt đầu bằng nhìn vào đạo cụ và nhấn E. Chỉ bước hiện tại được chơi. HUD ghi rõ khu vực và vật cần tìm; checklist chỉ hiển thị một nhóm nhỏ quanh bước hiện tại, tránh nhiều dòng che màn hình.

| Bước | Gian | Nhiệm vụ / GameObject | Mini-game | Hiệu ứng và câu ký ức |
| --- | --- | --- | --- | --- |
| 1 | Phòng ngủ | Xếp đồ vào rương — `BedroomDecor/StorageChest` | Chọn áo → khăn → đóng rương | Đạo cụ nhỏ nhún theo lần đặt; “Chiếc rương giữ cả những mùa áo cũ.” |
| 2 | Phòng ngủ | Chỉnh radio — `BedroomDecor/Radio` | Canh kim ở giữa, Space đúng 3 lần | Núm xoay mô phỏng và hạt sáng; “Tiếng radio mở đầu mỗi buổi sáng của nhà mình.” |
| 3 | Phòng kho | Xếp dụng cụ — `YardProps/ToolSet_02` | Chọn cào → cuốc → xẻng | Dụng cụ mô phỏng dịch chuyển; “Dùng xong nhớ cất, mai còn tìm được con nhé.” |
| 4 | Phòng kho | Xay ngô — `YardProps/GrainMill` | Xoay chuột 3 vòng quanh tâm màn hình, chuột phải thu bột | Tay cầm mô phỏng xoay, hạt bột; “Tiếng cối quay đều như nhịp tay mẹ.” |
| 5 | Phòng kho | Gom củi mang vào bếp — `YardProps/BambooBasket` | Giữ F 3 giây để gom bó củi | Bó củi mô phỏng nâng lên; đây là thao tác chuẩn bị, chưa có hệ thống kéo/thả hay cầm đồ khi đi | 
| 6 | Gian bếp (lu thuộc TraditionalHouseInterior) | Múc nước — `WaterJarVisual` | Space đúng vùng giữa 3 lần | Gáo chuyển động, hạt nước; “Gáo nước mát mẹ để dành từ sáng.” |
| 7 | Gian bếp | Đong gạo — `RiceJar/cailugao` | Giữ chuột trái rồi thả ở vùng xanh; 3 gáo | Gáo và hạt gạo; “Mẹ luôn đong thêm một chút cho con.” |
| 8 | Gian bếp | Vo gạo — `WallBasins/Placeholder_RiceWashingBowl` (thau cũ đã bị xóa) | Xoay chuột 3 vòng, chuột phải chắt nước | Thau lắc và hạt nước; “Nước đục đi, hạt gạo lại trắng.” |
| 9 | Gian bếp | Nhóm lửa — `FirewoodStove/StovePotVisual` | Space 15 nhịp | Tàn lửa, ánh sáng theo nhịp thổi; “Bếp bắt lửa, cả nhà bắt đầu ấm.” |
| 10 | Gian bếp | Canh cơm — `RiceCookingStove/beplua_naucom` | Giữ F 4 giây, sau đó canh kim Space 1 lần | Hơi nước và ánh lửa; “Mùi cơm chín khiến con biết sắp được gọi về.” |
| 11 | TraditionalHouseInterior | Dọn mâm — điểm trên bàn thật nếu tìm được; nếu thiếu dùng Cube `Placeholder_FamilyMealTray` | Chọn bát → đũa → nồi cơm | Các món mô phỏng chuyển động; “Mâm cơm đủ người là bữa ngon nhất.” |
| 12 | Gian bếp | Rửa chén — `Sink/DishwashingStationVisual` | Giữ chuột trái 3 giây/chén, đủ 3 chén | Miếng chà và nước bắn; “Mẹ rửa, con tráng, hai mẹ con chuyện trò.” |
| 13 | Gian bếp | Cất chén — `DishCabinet/DishCabinetVisual` | Chọn chén → đĩa → đũa | Đạo cụ mô phỏng nhún theo lần đặt; “Tiếng chén khẽ chạm nhau, việc nhà đã xong.” |
| 14 | TraditionalHouseInterior | Mở lá thư của mẹ — Cube `Placeholder_MothersLetter` | Giữ F 3 giây để mở thư | Ánh sáng ký ức; hiện lời nhắn của mẹ, kết thúc 140/140 |

Lời thư: “Sau này đi đâu, con cũng nhớ ăn cơm đúng bữa. Nhà mình vẫn có một chỗ chờ con.” Kết: “Hóa ra điều mình nhớ nhất không phải căn nhà, mà là tiếng mẹ gọi.”

## Placeholder cần thay asset

- `Placeholder_MothersLetter`: lá thư hoặc phong bì cũ, tại gian nhà chính. Đây là asset mới cần người dùng tạo.
- `Placeholder_FamilyMealTray`: chỉ dùng nếu chưa nhận diện được bàn/mâm tương tác trong gian nhà chính. Model gợi ý: mâm nhôm cùng bát và đũa. Không lấy `RicePot` làm mâm hoặc điểm nhiệm vụ vì đó từng là mốc xuống giường.
- Cube hiển thị rõ, tên dễ tìm, collider trigger không chặn đi lại. Khi thay, giữ object gốc/script/trigger và gắn model làm con; không xóa binding nhiệm vụ. Đặt collider khớp model mới nếu kích thước đổi.

- `Placeholder_RiceWashingBowl`: thau vo gạo tại gian bếp; dùng tạm vì object `thao` đặt sàn đã bị xóa, các thau treo tường vẫn giữ nguyên.

## Quy tắc triển khai

- Mở rộng `HouseholdChoreFlow`, `HouseholdChoreInteractable`, `HouseholdWorkFeedback` và utility binding hiện hữu. Giữ ID enum 0–7 của nhiệm vụ cũ, thêm ID mới ở cuối; thứ tự chơi được khai báo riêng để không làm sai serialized scene.
- Điểm tương tác làm con của đạo cụ hiển thị và dùng trigger vừa kích thước. Không tạo collider vật lý rộng quanh căn phòng. Giữ collider vật lý và vị trí người dùng đã sửa.
- Tránh hai Interactable cùng nhận E trên một điểm. Không xóa hành vi lên/xuống giường cũ.
- Hiệu ứng là animation thủ tục và particle trong Unity, không phải clip Blender hoặc bàn tay nhân vật hoàn chỉnh. Dọn hiệu ứng và trả input khi hủy/tắt object. Không di chuyển mesh kiến trúc để diễn hoạt.
- Không tạo asset nguồn hay script mới cho từng nhiệm vụ. Texture/model mới vẫn theo `docs/Scene2_AssetFolders.md`.
- Kiểm tra đủ 14 binding, trigger không chặn di chuyển, chống thưởng sai thứ tự/trùng, hủy không cộng điểm, hoàn tất 140/140 mới chuyển quest. Báo rõ phần đã kiểm tra tự động và phần cần chơi thử.
