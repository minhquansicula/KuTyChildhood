# Act 1 Office Blockout — kế hoạch tích hợp tương tác từ Office ArtPass

Cập nhật: 19/10/2026. Trạng thái: kế hoạch, chưa triển khai gameplay.

## 1. Mục tiêu và phạm vi

Scene đích: `Assets/_Project/Scenes/Act1_Office_Blockout.unity`.
Scene tham chiếu: `Assets/_Project/Scenes/Act1_RealWorld_OfficeArtPass.unity`.

Giữ map và asset người dùng đang chỉnh trong Blockout; tích hợp hành vi tương tác, UI công việc và chuyển cảnh của office cũ. Không dựng lại kiến trúc, không chạy lại các builder nhập bàn/cửa/đèn/ngoại cảnh. Giữ nguyên scene ArtPass để đối chiếu.

Luồng hoàn chỉnh:

**Nghe sếp trong phòng → tự mở cửa → về bàn → ngồi → email → phân loại tài liệu → sửa báo cáo → kiểm toán Excel → nhận thông báo mới → nghỉ/gục xuống bàn → Act 2.**

Phạm vi mới mở rộng phần gameplay sau `ReadyAtDesk`. Giới hạn “chưa làm mini-game/chuyển cảnh” trong `scene1-office-plan.md` và hướng dẫn cũ chỉ còn mô tả giai đoạn blockout trước đây; tài liệu này là nguồn yêu cầu cho đợt tích hợp tương tác. Các quy tắc bảo toàn map/asset của tài liệu cũ vẫn áp dụng.

Không thêm hệ thống nhiệm vụ mới, NPC AI, save game, animation tay nhặt/uống, asset dịch vụ ngoài hoặc package mới. Tương tác phụ dùng âm thanh/phụ đề tương đương code hiện có.

## 2. Hiện trạng đã đối chiếu

| Hạng mục | Blockout hiện tại | ArtPass hiện tại / code dùng chung |
| --- | --- | --- |
| Intro | `OfficeIntroController`, thoại trong phòng sếp | `OfficeOpeningSequence` và `StoryPrologueController` của prototype |
| Cửa | `OfficeDoorInteractable`, kiểm tra vùng quét | Không cần bê cơ chế mở cửa cũ sang |
| Ghế | Chuyển camera và kết thúc tại `ReadyAtDesk` | `OfficeSceneController` có ngồi/đứng |
| Tương tác chung | Raycast cục bộ chỉ nhận cửa/ghế | `PlayerInteraction`, `IInteractable`, HUD |
| Máy tính | Chưa có luồng công việc | Có `OfficeWorkstationInteractable` |
| Công việc | Chưa có UI/gameplay | Có `OfficeReportMiniGame`, `ExcelDataAuditorGame` |
| Tương tác phụ đã gắn | Chưa có bộ tương đương | 1 `OfficeMicroInteractable` loại ColdCoffee; 2 `OfficeInteractable` loại Boss và Chair |
| Tương tác phụ mới cần gắn | Đồng hồ, nước, máy in, cửa sổ | Có code enum/handler, chưa phải toàn bộ đã được lắp trong ArtPass |
| Kết thúc | Dừng ở ghế | Có `OfficeExhaustionSequence` và đường chuyển Act 2 |
| Phụ thuộc toàn cục | Chạy độc lập | Công việc dùng `GameManager`, `SceneLoader`, `HUDController` |
| Luồng menu | Blockout chưa nằm trong Build Settings | `SceneNames.Act1` hiện là `Act1_RealWorld` |

ArtPass có `MemoryTrigger` liên quan prologue cũ. Không chuyển trigger này sang: đường tiến sang Act 2 của office mới phải đi qua hoàn tất công việc và gục xuống bàn.

Trong scene ArtPass, `screenWarning` và `clockText` của `OfficeSceneController` đang để trống; cần gắn thật trên bàn/map Blockout thay vì sao chép reference rỗng.

## 3. Trải nghiệm và thứ tự tương tác

| Nhịp | Người chơi làm gì | Phản hồi và điều kiện hoàn thành |
| --- | --- | --- |
| 1. Nghe sếp | Nhìn nhẹ trong phòng; chưa đi/tương tác | Giữ thoại Blockout, tên Tý, fallback khi thiếu voice; hết thoại trả điều khiển |
| 2. Ra phòng | Nhìn cửa, nhấn E | Mở đúng bản lề hiện có; vẫn chặn khi đứng trong vùng quét; mở một lần |
| 3. Về bàn | Đi tới ghế Tý cạnh cửa sổ | Objective rõ; bàn đồng nghiệp không kích hoạt nhiệm vụ chính |
| 4. Ngồi | Nhìn ghế Tý, nhấn E | Chuyển camera êm tới `TySeatAnchor`, kích hoạt chế độ làm việc đúng một lần |
| 5. Email | Nhìn máy tính, E mở hộp thư; chọn đáp án bằng chuột | Kế thừa 3 email và phản hồi đúng/sai của `OfficeReportMiniGame` |
| 6. Tài liệu | Tiếp tục ở workstation, E mở phân loại | Kế thừa 4 tài liệu/khay; hoàn tất mới mở bước báo cáo |
| 7. Báo cáo | E mở đối chiếu số liệu rồi gửi | Kế thừa logic sửa/gửi hiện tại; xong chuyển Excel, chưa chuyển Act 2 |
| 8. Excel | E mở bảng kiểm toán | Kế thừa grid, nộp bài và chat sếp hiện có; kiểm tra đường hết giờ/nộp sai, không tự coi là hoàn tất |
| 9. Sau giờ làm | Có khoảng 15 giây nhìn quanh/đứng dậy | Đồng hồ 18:17, trạng thái đã gửi, thông báo mới; ánh chiều dịu xuống từ giá trị ban đầu |
| 10. Nghỉ | Khi cho phép nghỉ: đang ngồi thì E; đang đứng thì quay về ghế và E ngồi rồi E nghỉ | Không có gục xuống ngay khi còn đứng; không nhấn xuyên hai hành động trong một frame |
| 11. Gục/chuyển cảnh | Camera hạ về bàn, âm thanh mờ dần, màn hình đen | Gọi chuyển `Act2_MemoryWorld_Home` một lần qua `SceneLoader` |

Đích tài liệu có thể dùng một vùng tương tác trên workstation như ArtPass. Phân biệt prompt theo phase; không bắt người chơi tìm từng tờ giấy nhỏ. Mini-game tài liệu vẫn là UI hiện tại, chưa làm kéo thả vật thể 3D.

## 4. Tương tác phụ trong map

Các tương tác này mở sau thoại, dùng trong lúc đi lại hoặc khi ngồi nếu mục tiêu ở trong tầm. Không dùng khi UI công việc/pause/cutscene đang giữ input và không chặn tiến trình chính.

| Vị trí/vật | Prompt dự kiến | Hành vi |
| --- | --- | --- |
| Đồng hồ văn phòng | `[E] Xem giờ và deadline` | Hiện 17:42/18:00 trước khi hoàn tất; 18:17 sau công việc |
| Máy in | `[E] Lấy tài liệu từ máy in` | Tiếng máy in/giấy và câu thoại; một lần trong lượt chơi; không thay điều kiện phân loại tài liệu |
| Cây nước/cốc nước | `[E] Uống một ngụm nước` | Âm nước và độc thoại ngắn; có cooldown chống spam |
| Cốc cà phê bàn Tý | `[E] Cầm cốc cà phê` | Kế thừa thoại cà phê lạnh và âm cốc; không thay đổi trạng thái công việc |
| Cửa sổ | `[E] Nhìn ra phố` | Độc thoại/âm giao thông; không dịch chuyển camera cưỡng bức |
| Phía phòng sếp | `[E] Nhìn về phía phòng sếp` | Độc thoại căng thẳng, sửa câu “phòng họp” cho đúng map |
| Ghế Tý | `[E] Ngồi vào ghế` | Sau lần ngồi đầu cho phép Space đứng; trở lại ngồi không reset công việc |

Viên bi tuổi thơ là tùy chọn giai đoạn sau. Không đưa vào danh sách bắt buộc chỉ vì enum `ChildhoodMarble` đã tồn tại.

## 5. Thiết kế điều phối và quyền điều khiển

### Một luồng trạng thái chính

Tái sử dụng `OfficeSceneController` cho phase công việc và hai mini-game đang phụ thuộc trực tiếp kiểu controller này. Thêm chế độ mở đầu do bên ngoài điều khiển, mặc định giữ hành vi cũ cho scene prototype. Blockout bật chế độ này để controller không tự chạy intro/teleport/ngồi lần nữa.

`OfficeIntroController` giữ lời thoại, cửa và chuyển ngồi đầu. Bổ sung API bàn giao rõ ràng; không tắt component bằng `enabled=false` để bàn giao, vì `OnDisable` hiện đưa player về spawn, reset camera và dừng ambience.

Trình tự đồng bộ dự kiến:

1. Bootstrap kiểm tra/gán đủ reference trước khi hệ thống bắt đầu, theo Inspector, không chờ `Start` tình cờ đúng thứ tự.
2. Controller chính ở Opening, chưa tự phát thoại. Intro giữ camera/di chuyển; tất cả tương tác bị khóa.
3. Intro báo `OpeningFinished`: controller chính nhận `ReturnToDesk`; bật tương tác đi lại và các tương tác phụ.
4. Trong lúc đi, chỉ một bộ raycast đọc phím E. Ưu tiên mục tiêu gần nhất có xét vật cản và bỏ collider của chính player. Cửa/ghế được adapter định tuyến về Intro; props về controller chính.
5. Intro hoàn tất chuyển ngồi và phát `ReadyAtDesk`: đồng bộ `IsSeated`, vị trí, pitch và chiều cao mắt hiện tại vào controller chính. Intro ngừng ghi camera/HUD/input nhưng tiếp tục giữ các dịch vụ cần thiết cho tới bàn giao xong.
6. Controller chính quản lý ngồi/đứng, phase công việc và kết thúc. Dùng một camera, một player; không có đoạn teleport về tọa độ ArtPass.
7. Khi mở mini-game, một input owner giữ chuột cho UI. Khi đóng, trả đúng trạng thái ngồi/đứng và objective, không phát lại intro.

Ưu tiên điều chỉnh `PlayerInteraction` theo cấu hình tùy chọn hoặc một router riêng cho Blockout: giữ hành vi scene khác, xử lý occlusion/self-collider, cho phép gọi target từ intro thay vì hai `Update` cùng nhận E. Chọn cách thay ít code dùng chung nhất sau khi kiểm thử. Không để Intro raycast cục bộ và `PlayerInteraction` cùng hoạt động trên một phím.

### Pause và thời gian

Một chủ sở hữu pause cho toàn scene; UI mini-game nhận Esc đóng trước, Esc tiếp theo mới pause. Pause/mất focus đóng băng thoại, timer Excel, nhắc sếp, chuyển ngồi và gục xuống; đóng UI không đánh mất tiến độ. Rà lại các coroutine đang dùng `WaitForSecondsRealtime`/`unscaledDeltaTime`: chúng không tự dừng khi `timeScale=0`.

Chuột khóa khi nhìn quanh; mở khi dùng UI hoặc pause. Trả input theo owner, không gọi mở khóa toàn cục bất chấp modal khác. Mỗi event bàn giao phải đăng ký/hủy đúng vòng đời và chống phát lặp.

## 6. Hợp đồng scene và wiring

| Object/nhóm | Thành phần/reference bắt buộc |
| --- | --- |
| Player hiện có | `FirstPersonController`, CharacterController, camera hiện có, router tương tác duy nhất |
| OfficeIntroSystems hiện có | Intro, HUD intro, spawn/seat/chairRoot/door đúng reference hiện tại; API bàn giao |
| OfficeGameplaySystems mới | `OfficeSceneController`, bridge điều phối intro, `OfficeAtmosphere`, kiểm tra cấu hình trước khi chạy |
| Bootstrap toàn cục | `GameManager`, `SceneLoader` và thành phần phụ thuộc cần thiết; dùng lại khi từ MainMenu, không tạo singleton trùng |
| UI công việc | Objective, subtitle, prompt, crosshair, `OfficeReportMiniGame`, `ExcelDataAuditorGame`, một EventSystem, một pause menu |
| Bàn Tý | Workstation target/collider, `TySeatAnchor`, stand anchor, collapse anchor; screen warning và cốc có target riêng |
| Cửa phòng sếp | Hinge/cánh/collider hiện có, `OfficeDoorInteractable`, adapter sang router chung |
| Các props phụ | Target/collider đúng layer, component loại tương tác và reference controller |
| Hiệu ứng kết thúc | `OfficeExhaustionSequence`, blackout, volume riêng/có runtime instance, camera, collapse anchor, atmosphere |

Tái sử dụng HUD intro cho đoạn đầu hoặc nối qua presenter duy nhất; sau bàn giao không có hai objective/phụ đề chồng nhau. UI mini-game lấy từ cấu hình ArtPass qua prefab/clone chọn lọc, remap mọi reference về scene đích; không kéo nguyên `OfficeRoot` cũ vào map.

Target đặt trên nhóm logic/slot hoặc collider con ổn định. Không gắn hành vi vào tên mesh FBX dễ đổi. Giữ `Visual_REPLACE`, collision, model người dùng, hướng bản lề, material, city và ánh sáng đã chỉnh.

Stand anchor đặt ở lối đi và kiểm tra capsule trống trước khi đứng; giữ trạng thái ngồi nếu vị trí bị chặn. Collapse anchor dựa trên mặt bàn thực tế, không lấy tọa độ cũ hoặc hạ camera xuyên mesh. Chiều cao mắt lấy từ Blockout, không hardcode `.9`/`1.05` của ArtPass.

## 7. Nội dung, âm thanh và cảm xúc

- Dùng tên Tý thống nhất trong thoại, email và UI của Blockout. Code cũ còn chuỗi QUÂN/Quân: chuyển nội dung qua cấu hình hoặc sửa đúng phạm vi để không vô tình đổi cốt truyện scene khác.
- Giữ lời thoại/phòng sếp hiện có; không chạy prologue hoặc intro ArtPass lần hai.
- Tiếng bàn phím chỉ chạy khi mini-game đang mở và chưa pause; đóng/cancel/chuyển cảnh dừng đúng.
- Nhắc sếp khoảng 20–30 giây trong phase công việc, không chồng giọng/subtitle với phản hồi UI, pause hoặc đoạn mở đầu. Trì hoãn nếu đã có lời thoại quan trọng.
- Nhịp sau giờ làm dùng biến thể ánh sáng/ambience từ trạng thái đã lưu lúc bắt đầu. Không nhân cường độ lặp khiến ánh sáng tối dần qua mỗi event.
- Cửa sổ và cà phê tạo khoảng thở; không thêm nhiệm vụ bắt buộc khiến player phải chạy khắp map lấy đủ vật.

## 8. Các đợt triển khai

### Đợt 1 — Bảo toàn map và chuẩn bị

Lưu scene trong Editor, ghi nhận dirty state và thay đổi chưa commit; tạo bản sao vào thư mục backup đang ignore. Kiểm tra hierarchy thực tế, camera, layer, collider, anchor, audio và các reference mini-game của ArtPass. Chụp các góc trước khi lắp. Không reset Git hoặc tái chạy builder phá map.

### Đợt 2 — Bàn giao intro và tương tác chung

Thêm chế độ external opening trên controller, bridge/adapter, router duy nhất và presenter HUD. Kiểm tra hết thoại → mở cửa → về ghế → ngồi → quyền điều khiển được bàn giao, chưa lắp toàn bộ công việc. Sửa lifecycle/pause trước khi tiếp tục.

### Đợt 3 — Máy tính và công việc

Mang UI/config của 2 mini-game sang, gán controller và workstation hiện có. Nối email → tài liệu → báo cáo → Excel; kiểm tra đóng/mở lại và không nhảy phase. Gắn deadline, đồng hồ và trạng thái màn hình.

### Đợt 4 — Props và ngồi/đứng

Gắn 6 tương tác phụ bắt buộc ở mục 4, cooldown/one-shot và prompt theo phase. Kiểm tra đứng dậy, đi lại, quay về ghế và tiếp tục công việc. Tách cốc/máy tính khỏi collider workstation quá lớn nếu chúng che nhau.

### Đợt 5 — Kết thúc và luồng menu

Nối AfterWork → Rest → gục xuống → Act 2. Chỉ khi Play trực tiếp đạt mới đưa Blockout vào Build Settings và đổi đích `SceneNames.Act1` sang Blockout, cập nhật nhận diện `GameManager`. Giữ asset scene cũ để tham chiếu. Kiểm thử từ MainMenu và Play thẳng Blockout đều chạy, singleton không trùng.

### Đợt 6 — Nghiệm thu

Chạy luồng đầy đủ, thử input thật, pause/cancel và kiểm tra Console. Chụp góc ngồi/UI/gục, báo các ca PASS/FAIL/chưa thử rõ ràng. Cập nhật `scene1-office-implementation.md` và `office-blockout-guide.md` sau khi triển khai thực sự, không ghi trước là hoàn thành.

## 9. Tiêu chí nghiệm thu

| ID | Điều kiện đạt |
| --- | --- |
| O01 | Spawn đúng phòng sếp, một đoạn thoại, không mở mini-game/tương tác lúc thoại |
| O02 | Cửa đúng chiều hiện tại, không xuyên player, không mở qua tường |
| O03 | Đi tới đúng ghế Tý, bàn phụ không nhận nhiệm vụ chính |
| O04 | `ReadyAtDesk` một lần; không teleport lại spawn, camera không giật/đổi chiều cao sau bàn giao |
| O05 | Một prompt/crosshair/objective; một lần E chỉ chạy một hành động |
| O06 | Email 3 mục → tài liệu 4 mục → báo cáo → Excel; không bỏ bước bằng nhấn nhanh |
| O07 | Đóng/mở lại từng mini-game giữ đúng tiến độ; chọn sai/nộp sai/hết giờ xử lý hợp lệ |
| O08 | Cursor đúng ở gameplay, UI, pause và sau khi đóng UI |
| O09 | Từng tương tác phụ phản hồi; máy in một lần, nước/cà phê không spam; không tác động tiến độ chính |
| O10 | Space đứng được khi hợp lệ; điểm đứng bị chắn thì từ chối; quay về ghế không reset công việc |
| O11 | Pause/mất focus giữa thoại, ngồi, Excel, AfterWork và gục đều không chạy lén timer/chuyển cảnh |
| O12 | Chưa xong Excel không vào Rest; khi đứng phải ngồi trước khi gục |
| O13 | Camera gục không xuyên bàn; blackout/âm thanh chạy xong, load Act 2 đúng một lần |
| O14 | MainMenu → Blockout → Act 2 và Play thẳng Blockout đều chạy; không singleton/EventSystem trùng |
| O15 | Model/material/bố cục/city/đèn do người dùng chỉnh được giữ; không có object demo cũ chồng vào |
| O16 | Stop/Play lặp lại không còn event/lock input; không lưu camera gục hoặc màu volume runtime vào asset |
| O17 | UI tiếng Việt ở 1920×1080 và 1366×768 đọc được, không che nút, không thiếu glyph |
| O18 | Console không có lỗi mới; smoke test scene office cũ và Act 2 vì có chỉnh script dùng chung |

## 10. Hướng dẫn bàn giao cho model coding

> Triển khai theo `docs/scene1-office-interaction-parity-plan.md`, kiểm tra `docs/scene1-office-implementation.md` và `docs/office-blockout-guide.md` để hiểu những phần đã có. Làm trên Act1_Office_Blockout, lấy OfficeArtPass làm nguồn tham chiếu UI và code hành vi. Giữ intro phòng sếp và map/asset đang chỉnh; nối đầy đủ công việc sau ReadyAtDesk. Đặc biệt xử lý bàn giao input/camera, OnDisable đang trả spawn, phụ thuộc GameManager/SceneLoader, coroutine không tự pause và trigger ký ức cũ có thể bỏ qua công việc. Không chạy lại builder mỹ thuật hoặc copy nguyên scene. Thực hiện từng đợt và kiểm chứng O01–O18, báo rõ phần chưa kiểm thử. Không merge/push hoặc dọn asset ngoài phạm vi nhiệm vụ.
