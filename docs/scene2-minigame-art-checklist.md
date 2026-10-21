# Theo dõi nâng cấp hình ảnh mini game Scene 2

Cập nhật: 22/10/2026. Tài liệu này ghi lại ảnh nguồn, sprite đã import và kết quả chơi thử. Các hình UGUI vẫn làm vùng thao tác, trạng thái và phương án dự phòng nếu thiếu sprite. Chưa xác nhận hình ảnh trong Play Mode.

## Tình trạng chung

- Scene: `Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity`.
- UI mini game: `Assets/_Project/Scripts/UI/HouseholdActionBoard.cs` (tạo giao diện và đọc chuột lúc chạy).
- Skill thiết kế giao diện game dùng cục bộ tại `.agents/skills/game-ui-design/` từ `jeremylongworth-source/AgentSkills` (MIT), không đưa vào Git. Bản thiết kế dùng các tiêu chí trong skill: phân cấp thông tin, trạng thái tương tác, độ đọc, vùng thao tác và kiểm tra ở độ phân giải đích.
- Layout Scene 2 hiện tách tiêu đề nhiệm vụ, câu hướng dẫn, khu vực tương tác có viền gỗ, thanh bước thao tác, phản hồi và tiến độ. Hướng dẫn P/Esc luôn ở chân bảng. Hai thao tác quay dùng tọa độ elip khớp phối cảnh mặt cối/mặt nước, không xoay nguyên ảnh theo trục màn hình.
- Thứ tự bắt buộc hiện tại lấy từ `HouseholdChoreFlow.Order`: xếp đồ vào rương → chỉnh radio → gom củi → múc nước → đong gạo → vo gạo → nhóm lửa → nấu cơm → dọn mâm → rửa chén → cất chén → mở thư của mẹ.
- Có 12 việc bắt buộc, mỗi việc thưởng 10 mảnh ký ức. Xếp dụng cụ và xay ngô là nhiệm vụ tùy chọn, mỗi việc thưởng 5 mảnh. `Gấp chăn` có code mini game nhưng không nằm trong thứ tự hiện tại.
- Sprite test nằm tại `Assets/_Project/Resources/Scene2/MiniGames/`. Ảnh rời là PNG có alpha thật; rương mở và giá treo là ảnh nền. Ảnh `Rương.jpg` có watermark và không được đưa vào game; `Xếp dụng cụ_Giá treo.jpg` chỉ được dùng làm tham chiếu chất liệu tường.
- Sprite mới cho nhiệm vụ 4–14 được tạo bằng công cụ tạo ảnh tích hợp, với cùng mô tả phong cách: `warm hand-painted storybook game art of a rural Vietnamese childhood home, isolated object, transparent background, no text, no watermark`. Danh sách chủ thể và tên file ở bảng dưới. Không dùng ảnh Google nên không có vấn đề giấy phép nguồn ngoài. Ảnh gốc toàn cối và các bộ lu/gáo/thau được giữ ở `docs/scene2-art-source/` để đối chiếu, không đưa vào Resources khi build.
- Ảnh gạo cũ gộp thành một tấm (`Rice_Set`) và ảnh thau nước cũ (`Wash_Rice`) đã chuyển sang `docs/scene2-art-source/` để đối chiếu; bản chạy dùng sprite rời và nồi gạo ngâm nước.

| Thứ tự | Nhiệm vụ | Ảnh nguồn | Import | Gắn vào mini game | Chơi thử |
| --- | --- | --- | --- | --- | --- |
| 1 | Xếp đồ vào rương | Áo gốc; rương, nắp, khăn và hộp được tạo bổ sung | 5 sprite | Đã gắn | Chưa thử Play Mode |
| 2 | Chỉnh radio | Radio và kim dò gốc đã tách nền | 2 sprite | Đã gắn | Chưa thử Play Mode |
| 3 | Xếp dụng cụ | Cào, cuốc, xẻng gốc đã tách nền; giá treo tạo bổ sung | 4 sprite | Đã gắn | Chưa thử Play Mode |
| 4 | Xay ngô | Tự tạo từ ảnh cối gốc: đế cố định, mặt đá nằm ngang và tay quay UI rời | `Corn_Mill_Base.png`, `Corn_Mill_UpperDisk.png` | Đế và mặt đá giữ đúng mặt phẳng ảnh; tay quay/dấu đá đi theo quỹ đạo elip ngang | Cần thử Play Mode bản mới |
| 5 | Gom củi | Tự tạo: khúc củi và bó củi đã buộc | `Firewood_Log.png`, `Wood_Bundle.png` | Đã gắn vào đồ kéo và bước buộc bó | Chưa thử Play Mode |
| 6 | Múc nước | Tự tạo từ bộ ảnh gốc: lu, thau và gáo tre tách riêng | `Water_Jar.png`, `Water_Basin.png`, `Water_Scoop.png` | Gáo tre là vật người chơi kéo để múc và chuyển nước | Người dùng đã thấy thiếu vật múc nước; cần thử lại bản sửa |
| 7 | Đong gạo | Tự tạo: lu gạo, gáo gạo và rá tre trống tách rời | `Rice_Jar.png`, `Rice_Scoop.png`, `Rice_Basket_Empty.png` | Kéo gáo từ lu vào rá ba lần; mức gạo tăng rõ sau mỗi lượt | Cần thử Play Mode bản mới |
| 8 | Vo gạo | `SM_RiceWashingPot.fbx` người dùng cung cấp; texture gốc của FBX và sprite tạo theo chính model nồi hai quai đặt trên kệ gỗ | `Rice_Washing_Pot.png` | Scene dùng model mới dưới `noivogao`, giữ `ChoreTarget_WashRice`; nồi đứng yên, gợn nước và hạt gạo đi theo elip ngang, cuối cùng bấm chắt nước | Cần thử Play Mode trong Unity Editor |
| 9 | Nhóm lửa | Tự tạo: bếp củi và bó củi | `Fire_Hearth.png`, dùng lại `Wood_Bundle.png` | Đã gắn trên bếp và củi kéo | Chưa thử Play Mode |
| 10 | Nấu cơm | Tự tạo: nồi cơm bốc hơi | `Cook_Pot.png` | Đã gắn trên bếp canh lửa | Chưa thử Play Mode |
| 11 | Dọn mâm cơm | Tự tạo: mâm tre trống | `Meal_Tray_Empty.png` | Điểm tương tác trên mesh `tripo_part_3`; không dùng `BambooTable` | Chưa thử Play Mode |
| 12 | Rửa chén | Tự tạo: chén và bùi nhùi rời | `Clean_Dish.png`, `Dish_Scrubber.png`, `Water_Basin.png` | Kéo bùi nhùi chà từng vết dơ trong lòng chén, rồi kéo chén sạch sang thau tráng | Chưa thử Play Mode |
| 13 | Cất chén | Tự tạo: tủ gỗ, chén, đĩa và đũa tách rời | `Dish_Cabinet.png`, `Dish_Bowl.png`, `Dish_Plate.png`, `Dish_Chopsticks.png` | Kéo từng vật từ khay bên phải vào ngăn tủ cùng tên | Chưa thử Play Mode |
| 14 | Mở thư của mẹ | Sprite phong bì/thư; thêm `SM_Letter.fbx` người dùng cung cấp trên bàn `tripo_part_4` trong Scene 2 | `Letter_Envelope.png` | Model 3D `SM_Letter` nằm ngang trên bàn và giữ điểm tương tác nhiệm vụ; minigame dùng ảnh thư kéo | Cần thử Play Mode trong Unity Editor |

## Nhiệm vụ 1: Xếp đồ vào rương

### Người chơi sẽ làm gì

Đến rương `BedroomArea/BedroomDecor/StorageChest` và nhấn **E**. Trong mini game, kéo **áo**, **khăn** và **kỷ vật** vào đúng ba ngăn có nhãn `NGĂN ÁO`, `NGĂN KHĂN`, `HỘP KỶ NIỆM`; sau đó kéo **nắp rương xuống**. Đặt sai thì đồ trở về vị trí đầu. Phần chơi này đã có trong `HouseholdActionBoard.cs`; thay ảnh là công việc trình bày và animation, cần giữ nguyên điều kiện hoàn thành và thưởng.

### Bộ ảnh đã chuẩn bị cho bản test

Mini game dùng **5 tấm độc lập**. `Old_Shirt` được xử lý từ ảnh người dùng cung cấp; bốn ảnh còn lại được tạo bổ sung cho bản test. Bảng từ khóa bên dưới giữ lại để tìm bản thay thế nếu cần.

| Mức | Tên file đề xuất | Nội dung cần thấy | Từ khóa Google Images (sao chép để tìm) | Tách nền? |
| --- | --- | --- | --- | --- |
| Cần | `Chest_Open.png` | Rương gỗ cũ **mở**, nhìn từ trên xuống hoặc góc chéo nhẹ; thấy rõ ba ngăn trống. Đây là nền của mini game, không phải model 3D trong phòng. | `old wooden chest open top view empty compartments game asset`; `rương gỗ cũ mở nắp nhìn từ trên xuống ngăn đồ png` | Nên tách nền ngoài viền rương; phần trong ngăn vẫn giữ. |
| Cần | `Chest_Lid.png` | **Chỉ nắp rương** tương ứng với rương trên, đủ mép và chốt khóa để kéo xuống. | `wooden chest lid separate top view transparent png game sprite`; `nắp rương gỗ tách riêng png` | Có, nền trong suốt. |
| Cần | `Old_Shirt.png` | Một áo cũ gấp gọn, nhìn cùng góc với lòng rương. | `folded old shirt top view transparent png game asset`; `áo cũ gấp lại nhìn từ trên xuống png` | Có, nền trong suốt. |
| Cần | `Old_Towel.png` | Một khăn vải cũ gấp gọn, hình dáng khác áo. | `folded vintage cloth towel top view transparent png`; `khăn vải cũ gấp lại nhìn từ trên xuống png` | Có, nền trong suốt. |
| Cần | `Keepsake_Box.png` | Hộp kỷ vật nhỏ bằng gỗ hoặc thiếc, đọc rõ khi thu nhỏ. | `small vintage keepsake box top view transparent png game asset`; `hộp kỷ vật nhỏ cũ nhìn từ trên xuống png` | Có, nền trong suốt. |

Nếu khó tìm ảnh rương có đúng **ba ngăn**, có thể gửi `Chest_Open.png` với lòng rương trống; ba vùng nhận đồ sẽ được vẽ bằng UI ở phía trên ảnh. Đừng chọn ảnh có sẵn áo, khăn hoặc hộp trong rương vì các vật đó cần di chuyển độc lập.

### Ảnh tùy chọn để đẹp hơn

| Tên file đề xuất | Dùng để làm gì | Từ khóa tìm kiếm | Tách nền? |
| --- | --- | --- | --- |
| `Hand_Cursor.png` | Bàn tay chỉ dẫn kéo vật/nắp. Có thể dùng chung ở các mini game sau. | `illustrated hand cursor pointing transparent png game ui`; `bàn tay hoạt hình chỉ kéo đồ png trong suốt` | Có. |
| `Chest_Closed.png` | Khung kết thúc sau khi kéo nắp xuống, nếu ảnh nắp riêng không đủ diễn tả rương đóng. | `old wooden chest closed top view transparent png game sprite` | Nên có. |
| `Chest_Scratches.png` | Vân gỗ/vết xước phủ nhẹ lên nền rương. | `wood grain scratches transparent overlay png` | Có. Không cần tìm nếu rương nền đã có vân đẹp. |

### Quy cách ảnh để các tấm ghép được với nhau

1. Chọn **cùng một phong cách** cho cả bộ: minh họa 2D màu mộc, gỗ nâu ấm, vải ngà, ít bóng đổ giả. Tránh trộn ảnh chụp thật với tranh hoạt hình.
2. Chọn **cùng góc nhìn**. Ưu tiên nhìn gần từ trên xuống để thấy ba ngăn. Nắp phải cùng phối cảnh với thân rương. Nếu dùng hai nguồn khác nhau, gửi cả hai để kiểm tra trước khi tách nền.
3. Vật rời là **PNG RGBA** có nền trong suốt, mép sạch; không giữ ô caro giả hay viền trắng. Nếu Google chỉ có JPG, gửi bản gốc để xử lý tách nền sau.
4. Kích thước tham khảo: nền rương khoảng **1024 × 512 px** trở lên; nắp khoảng **1024 × 256 px**; mỗi vật rời khoảng **256 × 256 px**. Đây là mục tiêu chất lượng ảnh nguồn, không phải bắt buộc đúng kích thước.
5. Không gộp 5 món vào một ảnh. Không đưa chữ, nút E hay nhãn ngăn vào ảnh; chữ sẽ do Unity UI hiển thị để giữ tiếng Việt rõ nét.
6. Nên tìm ảnh cho phép sử dụng trong game hoặc tự tạo. Lưu lại link trang nguồn/điều kiện sử dụng; ảnh trên Google Images không tự động được phép tái sử dụng.

### Nơi đặt ảnh và cách gắn vào Unity

- Sprite test nằm tại `Assets/_Project/Resources/Scene2/MiniGames/`. Ảnh nguồn gốc vẫn ở folder người dùng cung cấp; rương 3D trong phòng không bị thay đổi.
- Metadata đã đặt `Texture Type = Sprite (2D and UI)` và bật alpha. Cần kiểm tra viền ảnh sau khi Unity import.
- `HouseholdActionBoard.cs` tải `Chest_Open` làm nền; ba sprite vật vào các `MovableItem`; bóng mờ của chúng ở ba `Compartment`; `Chest_Lid` vào `FinalGesture`.
- Animation đề xuất: đồ nhấc lên hơi phóng to khi kéo, thả đúng thì thu nhỏ và đặt vào ngăn; thả sai thì trượt về; ngăn đúng sáng nhẹ; nắp kéo theo chuột rồi đóng với một nhịp ngắn. Animation chỉ phản hồi trạng thái, không tự đánh dấu hoàn thành khi chưa đủ ba vật và thao tác đóng nắp.
- Sprite được tải qua `Resources.Load` theo tên file, không cần kéo thả vào Inspector. Nếu thiếu file, UI dùng hình UGUI cũ làm dự phòng.

### Checklist để lần sau tiếp tục

- [x] Xác nhận nhiệm vụ bắt buộc đầu tiên và thao tác thực tế trong code.
- [x] Lập danh sách ảnh, tên file, từ khóa tìm kiếm và yêu cầu tách nền.
- [x] Chuẩn bị đủ 5 ảnh test cho rương.
- [x] Kiểm tra hình và chọn bộ ảnh test.
- [x] Tạo PNG RGBA cho vật rời và nắp.
- [x] Đưa sprite và metadata import vào `Resources/Scene2/MiniGames`.
- [x] Gắn sprite vào UI mini game; giữ vùng kéo/thả theo logic cũ.
- [ ] Hoàn thiện animation kéo, thả đúng/sai, đóng nắp.
- [ ] Chơi thử trong Scene 2: nhấn E, hoàn tất bằng chuột, thả sai, Esc hủy, chơi lại; kiểm tra không thưởng trùng và không làm mất hoặc đổi cỡ rương 3D.
- [ ] Chụp ảnh mini game sau khi nâng cấp và cập nhật bảng tình trạng chung.

**Nhật ký:**

| Ngày | Thay đổi | Ảnh nguồn/đường dẫn | Kết quả kiểm tra | Việc tiếp theo |
| --- | --- | --- | --- | --- |
| 21/10/2026 | Tạo tài liệu; chưa import ảnh hoặc sửa UI | Chưa có | Đã đối chiếu thứ tự nhiệm vụ và thao tác với code | Nhận/chọn 5 ảnh chính cho rương |
| 21/10/2026 | Chuẩn bị và gắn 11 sprite cho rương, radio, dụng cụ | `Assets/_Project/Resources/Scene2/MiniGames/` | Kiểm tra file/alpha và `git diff --check`; chưa chạy Unity Play Mode | Chơi thử 3 mini game, kiểm tra kích thước ảnh và vùng thao tác |

Sau khi chơi thử ba nhiệm vụ đầu, thêm mục chi tiết cho **nhiệm vụ 4: Xay ngô**.
