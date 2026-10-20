# Scene 2 — Một ngày mẹ còn gọi

## Kịch bản và phạm vi

Một buổi sáng trong ký ức, mẹ gọi con dậy phụ việc. Mỗi đồ vật gợi lại một việc nhỏ đã từng làm cùng gia đình. Người chơi đi qua phòng ngủ, phòng kho, gian nhà chính `TraditionalHouseInterior` và gian bếp; cuối ngày tìm được lá thư mẹ cất lại.

14 nhiệm vụ tuần tự, mỗi nhiệm vụ thưởng đúng một lần **10 mảnh ký ức**, tổng **140/140**. Giữ 3 ký ức lớn của cốt truyện độc lập: hoàn tất 14 việc mới trao `FilialLove` và 2.000 đồng mẹ cho đi mua kẹo, bi ve. Sau khi đóng lời thư, game chuyển sang `Act3_MemoryWorld_OutSide` để chơi quầy hàng, bi ve và đoạn kết. Tiến độ được lưu sau từng việc và từng bước ngoài sân; menu hiện **Tiếp tục** khi còn checkpoint. **Bắt đầu** xóa checkpoint để chơi mới. Esc hủy lượt mini-game đang làm, không mất nhiệm vụ đã hoàn tất; không nhận thưởng khi hủy.

## Nhiệm vụ và tương tác

Mỗi nhiệm vụ bắt đầu bằng nhìn vào đạo cụ và nhấn E. Chỉ bước hiện tại được chơi. HUD ghi rõ khu vực và vật cần tìm; checklist chỉ hiển thị một nhóm nhỏ quanh bước hiện tại, tránh nhiều dòng che màn hình.

| Bước | Gian | Nhiệm vụ / GameObject | Mini-game | Hiệu ứng và câu ký ức |
| --- | --- | --- | --- | --- |
| 1 | Phòng ngủ | Xếp đồ vào rương — `BedroomDecor/StorageChest` | Kéo áo, khăn, kỷ vật vào ba ngăn; kéo nắp rương xuống | Đạo cụ nhỏ nhún theo lần đặt; “Chiếc rương giữ cả những mùa áo cũ.” |
| 2 | Phòng ngủ | Chỉnh radio — `BedroomDecor/Radio` | Kéo núm quanh tâm tới vùng sóng rõ và giữ ổn định | Núm xoay mô phỏng và hạt sáng; “Tiếng radio mở đầu mỗi buổi sáng của nhà mình.” |
| 3 | Phòng kho | Xếp dụng cụ — `YardProps/ToolSet_02` | Kéo theo bóng dụng cụ trên móc; giữ R/lăn chuột khi kéo để xoay cuốc | Dụng cụ mô phỏng dịch chuyển; “Dùng xong nhớ cất, mai còn tìm được con nhé.” |
| 4 | Phòng kho | Xay ngô — `YardProps/GrainMill` | Xoay chuột quanh cối, sau đó kéo khay xuống thu bột | Tay cầm mô phỏng xoay, hạt bột; “Tiếng cối quay đều như nhịp tay mẹ.” |
| 5 | Phòng kho | Gom củi mang vào bếp — `YardProps/BambooBasket` | Xếp ba thanh củi vào bó rồi kéo dây buộc | Bó củi mô phỏng nâng lên; đây là thao tác chuẩn bị, chưa cầm bó củi khi đi |
| 6 | Gian bếp (lu thuộc TraditionalHouseInterior) | Múc nước — `WaterJarVisual` | Kéo gáo vào lu, giữ để múc đầy, kéo sang thau | Gáo chuyển động, hạt nước; “Gáo nước mát mẹ để dành từ sáng.” |
| 7 | Gian bếp | Đong gạo — `RiceJar/cailugao` | Kéo gáo vào lu, sang thau; kéo mép gáo xuống để nghiêng đổ. Dư thì giữ chuột trong thau để gạt bớt; đủ thì cất gáo | Gáo và hạt gạo; “Mẹ luôn đong thêm một chút cho con.” |
| 8 | Gian bếp | Vo gạo — `WallBasins/Placeholder_RiceWashingBowl` (thau cũ đã bị xóa) | Xoay chuột trong thau, sau đó kéo sang phải chắt nước | Thau lắc và hạt nước; “Nước đục đi, hạt gạo lại trắng.” |
| 9 | Gian bếp | Nhóm lửa — `FirewoodStove/StovePotVisual` | Kéo củi khô và mồi lửa vào bếp, thổi ba nhịp khi than sáng (mỗi nhịp tính một lần) | Tàn lửa, ánh sáng theo nhịp thổi; “Bếp bắt lửa, cả nhà bắt đầu ấm.” |
| 10 | Gian bếp | Canh cơm — `RiceCookingStove/beplua_naucom` | Đẩy/rút thanh củi trong bếp để giữ nhiệt vừa đủ 16 giây; nhìn hơi nước và trạng thái nồi | Hơi nước và ánh lửa; “Mùi cơm chín khiến con biết sắp được gọi về.” |
| 11 | TraditionalHouseInterior | Dọn mâm — điểm trên bàn thật nếu tìm được; nếu thiếu dùng Cube `Placeholder_FamilyMealTray` | Kéo bát, đũa, nồi cơm vào đúng chỗ trên mâm | Các món mô phỏng chuyển động; “Mâm cơm đủ người là bữa ngon nhất.” |
| 12 | Gian bếp | Rửa chén — `Sink/DishwashingStationVisual` | Chà từng vết bẩn riêng, kéo chén vào nước để tráng 1,2 giây rồi đưa lên giá | Miếng chà và nước bắn; “Mẹ rửa, con tráng, hai mẹ con chuyện trò.” |
| 13 | Gian bếp | Cất chén — `DishCabinet/DishCabinetVisual` | Kéo chén, đĩa, đũa vào đúng ngăn | Đạo cụ mô phỏng nhún theo lần đặt; “Tiếng chén khẽ chạm nhau, việc nhà đã xong.” |
| 14 | TraditionalHouseInterior | Mở lá thư của mẹ — Cube `Placeholder_MothersLetter` | Kéo tháo dây, mở nếp giấy, lật trang | Ánh sáng ký ức; hiện lời nhắn của mẹ không tự đóng, nhấn Space khi đọc xong; kết thúc 140/140 |

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
- Mini-game rửa chén cũ (`DishWashingGame`) không còn là đường hoàn thành quest hoặc kiếm tiền khi luồng 14 việc nhà hiện diện. `HouseholdActionBoard` tạo một mặt bàn thao tác chung bằng TMP/UGUI lúc chạy; đồ đặt sai tự trở lại chỗ cũ, không trừ thưởng. Phím 1–3, F, Enter hoặc mũi tên là phương án dự phòng cho các thao tác tương ứng. Sau 7 giây không thao tác, trò hiện thêm gợi ý; Esc hủy lượt hiện tại nhưng không xóa các việc đã hoàn tất.
- Mặt bàn dùng hình minh họa UGUI nhẹ cho áo, khăn, kỷ vật, dụng cụ, củi, đồ ăn/chén và các vật chính của thao tác khác; không cần atlas texture. Vật để xếp xuất hiện khác thứ tự ngăn đích. Khi kéo, ngăn đúng sáng xanh và ngăn sai đỏ; thả sai vật trượt về vị trí ban đầu. Núm radio có kim chỉ tần số, vùng bếp/than báo nhịp thổi bằng màu.

## Tương tác trực tiếp — cập nhật 20/10/2026

- Mặt thao tác cận cảnh minh họa UGUI ở giữa màn hình, kích thước theo CanvasScaler; thanh tiến độ tổng lớn đã ẩn. Chưa phải camera cận cảnh model 3D hoặc bàn tay nhân vật 3D.
- Rương và tủ có khung gỗ, ngăn gợi ý bằng bóng vật. Đặt theo thứ tự bất kỳ; sai thì vật trở về. Nắp rương/dây buộc theo chuột khi kéo, cần kéo đủ hành trình.
- Bàn tay minh họa xuất hiện lúc bắt đầu, sau 7 giây không thao tác hoặc sau hai lần sai. Hướng dẫn cụ thể bổ sung khi đứng yên/sai; các phím dự phòng vẫn được giữ.
- Cối xay/vo gạo cộng góc có dấu, yêu cầu ba vòng thật quanh tâm; không tính lắc qua lại hoặc xoay quá nhanh. Nước trong thau đổi màu theo mức sạch; nghiêng thau để chắt.
- Gáo nước được kéo vào lu và giữ tại đó để đầy; di chuyển nhanh khiến nước sóng sánh và giảm nhẹ, còn tối thiểu 65%. Không có trạng thái thua vì đổ nước.
- Radio trộn tiếng rè và giai điệu theo độ gần tần số. Âm thanh tạo thủ tục là bản tạm, chưa có bản thu giọng hát/thoại mẹ riêng. Âm thanh và clip tạm được dọn khi hủy.
- Lá thư có dây trượt theo thao tác tháo và giấy nghiêng khi mở/lật. Lời thư chờ người chơi nhấn Space, không có thời hạn.
- `HouseholdChoreFlow` vẫn là nơi duy nhất trao thưởng. Không đổi ID enum, thứ tự 14 việc, checkpoint, binding scene hoặc vật lý bi ve. Hủy chỉ mất thao tác của việc đang làm; việc đã xong vẫn được giữ.

Kiểm tra: `KuTy/Tests/Check household interactions` chạy trong Play Mode và tự thoát. Đã PASS 70 assertions trên Unity 2022.3.62f3, bao gồm mở/hủy cả 14 mặt thao tác, đặt bất kỳ thứ tự và chống đặt trùng, chờ nắp rương, callback hoàn thành một lần, gạo quá vạch không reset, bắt buộc tráng chén, trộn và dọn âm thanh radio. Đã kiểm tra thêm scene: đủ 14 binding duy nhất đang bật, toàn bộ collider tương tác là trigger. Các kiểm tra này không gọi hoàn thành quest hoặc sửa checkpoint. Cần chơi thử chuột liên tục để cân thời lượng mục tiêu 15–35 giây và cảm giác kéo/xoay.

Xem riêng rương: vào Play Mode rồi chọn `KuTy/Tests/Preview household chest in Play Mode`; bản xem thử không trao thưởng, Esc đóng.
