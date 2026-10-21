# Theo dõi nâng cấp hình ảnh mini game Scene 2

Cập nhật: 22/10/2026. Tài liệu này ghi lại ảnh nguồn, sprite đã import và kết quả chơi thử. Các hình UGUI vẫn làm vùng thao tác, trạng thái và phương án dự phòng nếu thiếu sprite. Cần kiểm tra hình ảnh trực tiếp trong Game View khi mở được Unity Editor.

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
| 1 | Xếp đồ vào rương | Áo gốc; rương, nắp, khăn và hộp được tạo bổ sung | 5 sprite | Áo, khăn và hộp có alpha; tắt hoàn toàn Image nền của thẻ kéo và lớp màu phủ ba ngăn rương, kể cả sau khi đặt vật; giữ nhãn tên ngăn | Bài kiểm tra Editor qua; cần xem trong Play Mode sau khi chạy lại |
| 2 | Chỉnh radio | Radio và kim dò gốc đã tách nền | 2 sprite | Đã gắn | Chưa thử Play Mode |
| 3 | Xếp dụng cụ | Cào, cuốc, xẻng gốc đã tách nền; giá treo tạo bổ sung | 4 sprite | Đã gắn | Chưa thử Play Mode |
| 4 | Xay ngô | Tự tạo từ ảnh cối gốc: đế cố định, mặt đá nằm ngang và tay quay UI rời | `Corn_Mill_Base.png`, `Corn_Mill_UpperDisk.png` | Đế và mặt đá giữ đúng mặt phẳng ảnh; tay quay/dấu đá đi theo quỹ đạo elip ngang | Cần thử Play Mode bản mới |
| 5 | Gom củi | Tự tạo: nia tre trống, hai loại khúc củi, dây thừng và bó đã buộc | `Wood_Mat.png`, `Firewood_Log.png`, `Wood_Log_Dark.png`, `Wood_Rope.png`, `Wood_Bundle.png` | Kéo 3 khúc củi lên nia, sau đó kéo dây ngang; ảnh bó chỉ hiện sau khi buộc xong | Chưa thử Play Mode |
| 6 | Múc nước | Tự tạo từ bộ ảnh gốc: lu, thau và gáo tre tách riêng | `Water_Jar.png`, `Water_Basin.png`, `Water_Scoop.png` | Gáo tre là vật người chơi kéo để múc và chuyển nước | Người dùng đã thấy thiếu vật múc nước; cần thử lại bản sửa |
| 7 | Đong gạo | Tự tạo: lu gạo, gáo gạo và rá tre trống tách rời | `Rice_Jar.png`, `Rice_Scoop.png`, `Rice_Basket_Empty.png` | Kéo gáo từ lu vào rá ba lần; mức gạo tăng rõ sau mỗi lượt | Cần thử Play Mode bản mới |
| 8 | Vo gạo | `SM_RiceWashingPot.fbx` người dùng cung cấp; texture gốc của FBX và sprite tạo theo chính model nồi hai quai đặt trên kệ gỗ | `Rice_Washing_Pot.png` | Scene dùng model mới dưới `noivogao`; thêm vùng nhận E khi đứng gần và nhìn nồi; đếm hai vòng khuấy, gợn nước và hạt gạo chuyển động, sau đó bấm chắt nước với hiệu ứng giọt và tiến độ trước khi hoàn thành | Bài kiểm tra Editor qua; cần thử E và chuột trong Play Mode |
| 9 | Nhóm lửa | Tự tạo: bếp củi, bó củi và mồi khô | `Fire_Hearth.png`, `Wood_Bundle.png`, `Fire_Kindling.png` | Củi và mồi khô là hai vật kéo riêng | Chưa thử Play Mode |
| 10 | Nấu cơm | Tự tạo: nồi cơm và thanh củi | `Cook_Pot.png`, `Firewood_Log.png` | Thanh củi trượt để chỉnh lửa, nồi và hơi nước phản hồi | Chưa thử Play Mode |
| 11 | Dọn mâm cơm | Tự tạo: mâm tre trống, bát, đũa và nồi cơm riêng không kèm bếp lửa | `Meal_Tray_Empty.png`, `Dish_Bowl.png`, `Dish_Chopsticks.png`, `Rice_Pot_Served.png` | Kéo ba món vào đúng chỗ trên mâm; chỉ nhiệm vụ nấu cơm còn dùng `Cook_Pot.png` có bếp củi | Bài kiểm tra Editor qua; cần thử kéo thả trong Play Mode |
| 12 | Rửa chén | Tự tạo: chén, miếng bọt biển rửa chén, thau và giá tre | `Clean_Dish.png`, `Dish_Sponge.png`, `Water_Basin.png`, `Dish_Rack.png` | Chà từng vết đến sạch, kéo chén vào thau tráng 1,1 giây, kéo lên giá; có bọt xà phòng và chỉ báo 3 bước | Chưa thử Play Mode |
| 13 | Cất chén | Tự tạo: tủ gỗ, chén, đĩa và đũa tách rời | `Dish_Cabinet.png`, `Dish_Bowl.png`, `Dish_Plate.png`, `Dish_Chopsticks.png` | Kéo từng vật từ khay bên phải vào ngăn tủ cùng tên | Chưa thử Play Mode |
| 14 | Mở thư của mẹ | Sprite phong bì và trang giấy; `SM_Letter.fbx` người dùng cung cấp trên bàn `tripo_part_4` trong Scene 2 | `Letter_Envelope.png`, `Letter_Page.png` | Kéo sang phải để tháo dây, kéo lên để mở giấy, đọc đúng lời mẹ đã ghi trong `HouseholdChoreFlow.Memories`, rồi bấm ĐÃ ĐỌC; Enter là phím thay thế từng bước | Bài kiểm tra Editor qua; cần thử chuột và độ đọc trong Play Mode |

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

## Cập nhật UI và thao tác 22/10/2026

Tham khảo bố cục nhiệm vụ của *Goose Goose Duck*: một mặt thao tác chiếm trọng tâm, vật kéo đủ lớn, hướng dẫn ngắn và phản hồi từng bước. Giữ màu gỗ, tre và đồ gốm của ngôi nhà Việt Nam; toàn bộ ảnh mới là ảnh riêng của dự án. Nguồn tham khảo: [trang Goose Goose Duck trên Steam](https://store.steampowered.com/app/1568590/Goose_Goose_Duck/), [ảnh cộng đồng Steam](https://steamcommunity.com/app/1568590/screenshots/), [Unity uGUI skill chính thức](https://github.com/Unity-Technologies/skills/blob/main/skills/ui-ugui/SKILL.md). Skill cục bộ `game-ui-design` phù hợp phần phân cấp thông tin; `unity-ui` và `unity-script` phù hợp với UI Canvas và logic C# đang dùng. Không cần cài thêm package vào game.

| Hạng mục | Sprite đã thêm | Cách dùng |
| --- | --- | --- |
| Nền chung cho mọi mini game | `Task_Worktop.png` | Mặt gỗ sáng dưới vật tương tác, giữ tiêu đề và chỉ dẫn dễ đọc. |
| Rửa chén | `Dish_Sponge.png`, `Dish_Rack.png` | Miếng bọt biển vàng–xanh thay bùi nhùi. Giá tre là đích cuối sau khi chà và tráng. |
| Nhóm lửa | `Fire_Kindling.png` | Mồi lửa có hình riêng, không mượn sprite khăn. |
| Mở thư | `Letter_Page.png` | Giấy mở xuất hiện sau khi tháo dây phong bì. |
| Gấp chăn (chưa kích hoạt trong danh sách nhiệm vụ) | `Fold_Blanket.png` | Chăn gấp riêng thay cho sprite khăn dùng tạm trước đây. |

Prompt tạo ảnh đã dùng: nền gỗ nhìn từ trên xuống, không vật và không chữ; miếng bọt biển vàng có mặt chà xanh trên nền trong suốt; giấy cũ mở ra để trống phần giữa; bó lá khô và rơm làm mồi lửa; giá úp chén bằng tre trống; chăn bông cũ màu chàm gấp gọn. Cùng yêu cầu phong cách minh họa 2D, đường viền rõ, dễ đọc khi thu nhỏ, không logo hoặc chữ. Ảnh được tạo bằng công cụ `imagegen` tích hợp và lưu bản dùng cuối tại `Assets/_Project/Resources/Scene2/MiniGames/`.

Rửa chén có ba trạng thái rõ: **chà sạch ba vết** → **tráng trong thau khoảng 1,1 giây** → **đặt lên giá**. Vết bẩn mờ theo đường kéo của miếng bọt biển; bọt xà phòng và chỉ báo bước đổi theo tiến độ. Hoàn thành chỉ sau khi thả chén lên giá. Mini game dọn mâm đã dùng sprite bát, đũa và nồi có sẵn; nấu cơm dùng sprite thanh củi. Mỗi mặt mini game có nền gỗ chung, còn đạo cụ chính dùng ảnh riêng.

Kiểm tra đã thực hiện: biên dịch lại `Assembly-CSharp` và `Assembly-CSharp-Editor` bằng Roslyn của Unity 2022.3.62f3, không có lỗi (một cảnh báo cũ ở `OfficeBlockoutGameplaySmokeRuntime.cs`). Đã đối chiếu 27 tên sprite tải trực tiếp trong code: tất cả đều có PNG tương ứng. Bài kiểm tra Editor tại menu `KuTy/Tests/Review all household artwork` đã dựng thành công 15 mặt thao tác và kiểm tra ảnh, các bước gom củi/rửa chén và hủy nhiệm vụ. Lần chụp tự động cũ bắt nhầm cảnh 3D nên ảnh/kết quả cũ đã được xóa. **Chưa xác nhận bố cục trực quan trong Game View và chưa chơi thử chuột trong scene**; hai mục đó vẫn là việc cần làm trước khi chốt bản UI.

- [ ] Mở từng mini game trong Game View tại 1920×1080 và 1280×720, kiểm tra chữ, vật và vùng kéo không đè nhau.
- [ ] Rửa chén: kéo bọt biển qua cả ba vết, thả sớm, tráng nước, đặt lên giá; thử Esc và P giữa mỗi bước.
- [ ] Thử đủ 12 nhiệm vụ bắt buộc và 2 nhiệm vụ tùy chọn bằng chuột; kiểm tra phần thưởng và tiến độ chỉ cộng một lần.

## Sửa gom củi và tương tác vo gạo 22/10/2026

- Gom củi dùng nia tre trống (`Wood_Mat`), khúc củi sáng/tối (`Firewood_Log`, `Wood_Log_Dark`) và dây thừng rời (`Wood_Rope`). Kéo đúng ba vị trí trên nia rồi kéo dây từ trái sang phải. `Wood_Bundle` chỉ xuất hiện sau khi dây đã đi đủ quãng; phần thưởng vẫn do `HouseholdChoreFlow` quyết định khi mini game báo hoàn thành.
- Ảnh mới tạo bằng `imagegen` tích hợp với prompt tương ứng: nia tre nhìn từ trên xuống và để trống giữa; dây thừng đay nằm ngang có vòng cầm bên trái; một khúc củi vỏ nâu sẫm khác khúc cũ. Cả ba đều tách nền, không chữ và cùng chất liệu minh họa với bộ mini game.
- `HouseholdChoreInteractable` thêm cách nhận diện riêng cho nồi vo gạo khi người chơi đứng gần, quay camera về nồi và nhiệm vụ đang là “Vo gạo”. Cách này dùng chung phím E của `PlayerInteraction` và không đổi điểm thưởng, checkpoint hoặc model trong scene.
- Hai assembly C# biên dịch thành công bằng Roslyn của Unity 2022.3.62f3; còn một cảnh báo cũ `CS0252` trong `OfficeBlockoutGameplaySmokeRuntime.cs`. UnitySkills Editor đã chạy bài review 15 mini game thành công, gồm bước gom củi không hiện bó trước khi buộc. Trong Play Mode, đã xác nhận nồi `SM_RiceWashingPot` bật, có `HouseholdChoreInteractable`, nằm trong danh sách mục tiêu gần của `PlayerInteraction` và trả `[E] Vo gạo` sau khi hiển thị checklist. Chưa kiểm tra thao tác nhấn E bằng tay trong Game View. Lần chụp Game View qua REST trả về backbuffer nhiễu và đã bị xóa, nên chưa đánh giá bố cục bằng ảnh đó. Play Mode đã thoát và Error Pause được khôi phục.
- [ ] Trong Scene 2, đứng cạnh `noivogao`, nhìn nồi để xác nhận hiện `[E] Vo gạo`; bấm E, hoàn tất/thoát bằng Esc rồi mở lại.
- [ ] Với gom củi, thử thả sai vị trí, kéo dây chưa đủ, kéo đủ và xem bó củi xuất hiện trước khi nhận thưởng.
