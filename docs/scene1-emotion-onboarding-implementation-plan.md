# Scene 1 — plan implement hướng dẫn và cảm xúc

Ngày: 20/10/2026. Trạng thái: kế hoạch; chưa triển khai các thay đổi dưới đây.

## 1. Phạm vi và mục tiêu

Scene đích: `Assets/_Project/Scenes/Act1_Office_Blockout.unity`.
Dự án thuộc mức small-game. Phát triển trên luồng đã có:

`Intro phòng sếp → mở cửa → về ghế → Excel → PrintingReport → CollectPrint → DeliverReport → WaterBreak → Rest → Leaving → Act 2`.

Mục tiêu: người mới biết việc tiếp theo, hiểu vì sao Tý làm việc đó và cảm nhận được nhịp mệt mỏi trước khi trở về ký ức. Bản đầu ưu tiên hướng dẫn, nhịp thoại và phản hồi; mở rộng phân nhánh sau khi có playtest.

Chi tiết gia đình bản đầu dùng hồi tưởng như “Hồi nhỏ, mẹ luôn gọi mình vào ăn cơm giờ này…”. GDD đã có ký ức về Mẹ ở Act 2, nhưng chưa đủ dữ kiện để khẳng định mẹ hiện sống cùng Tý hoặc đang gửi tin nhắn. Điện thoại và tin nhắn thời hiện tại là lựa chọn nội dung cho đợt sau.

## 2. Hiện trạng code đã đối chiếu

| Thành phần | Đang có | Việc cần phát triển |
| --- | --- | --- |
| `OfficeSceneController` | Phase, objective, gợi ý 25 giây, lời thoại, tiến trình in/giao/nước/nghỉ | Phát event rõ ràng; tách hướng dẫn và trình bày khỏi luật tiến trình |
| `OfficeIntroController` + `OfficeBlockoutGameplayBridge` | Intro phòng sếp, mở cửa, ngồi ghế, bàn giao gameplay | Giữ điểm bàn giao; gợi ý đúng cửa/ghế trước khi handoff |
| `PlayerInteraction` | E, raycast có vật cản, crosshair và prompt | Phát event đổi focus để UI/highlight dùng lại raycast |
| `OfficeShiftHUD` | Hiển thị toàn bộ 6 bước; dựng chuỗi chữ trong LateUpdate | Hiện một mục tiêu chính, phản hồi hoàn thành, cập nhật theo event |
| `ExcelDataAuditorGame` | Hai sheet, công thức gợi ý, giữ dữ liệu/thời gian khi đóng, retry giữ dữ liệu | Tutorial trước timer; chế độ Thư thả; lời giải thích khi nhập sai |
| `OfficeAtmosphere` | Tiếng máy in, nước, giấy, thở dài, memory bridge | Nhận nhịp cảm xúc và phối âm; chống âm lặp do spam |
| Subtitle hiện tại | `ShowSubtitle` hủy coroutine cũ khi có lời mới | Ưu tiên nội dung, hàng chờ ngắn, loại gợi ý đã hết hiệu lực |

Smoke test gần nhất đã pass 42 assertions. Đây là baseline hồi quy; chưa chứng nhận người mới tự tìm đường bằng bàn phím/chuột.

## 3. Kiến trúc tối thiểu: 6 nhóm trách nhiệm

1. **Tiến trình:** `OfficeSceneController` tiếp tục là nguồn duy nhất quyết định phase và hoàn tất nhiệm vụ. Không để UI/lời thoại tự chuyển phase.
2. **Dữ liệu:** thêm `OfficeExperienceConfig : ScriptableObject` chứa nội dung theo phase, mốc gợi ý, thời lượng, cấu hình tutorial và nhịp kể chuyện. Asset chỉ chứa dữ liệu authoring.
3. **Hướng dẫn:** thêm `OfficeGuidanceController : MonoBehaviour` và `OfficeGuidanceState` C# thuần cho timer/cấp gợi ý. Thay thế `UpdateGuidanceHint` cũ để chỉ có một đồng hồ hướng dẫn.
4. **HUD và vật đích:** sửa `OfficeShiftHUD`; thêm `OfficeObjectiveMarker` và `OfficeInteractionHighlight`. Các view chỉ nhận dữ liệu; không sở hữu tiến trình/input.
5. **Câu chuyện và phụ đề:** thêm `OfficeNarrativeDirector` và `OfficeSubtitlePresenter`. Director chọn nhịp thoại; presenter điều phối hàng chờ và hiển thị. Dùng lại `OfficeAtmosphere` cho âm thanh.
6. **Hướng dẫn Excel:** thêm `ExcelTutorialController`, tích hợp các trạng thái tutorial vào mini-game hiện tại; giữ nguyên hai sheet thực tế.

Giao tiếp dùng reference serialize cho component cùng scene, event C# cho thay đổi trạng thái. Tránh event bus toàn cục, singleton mới và package mới.

### Contract cần bổ sung

- `OfficeSceneController.PhaseChanged(previous, current)` — phát sau khi phase/objective/flags đã nhất quán.
- `OfficeSceneController.WorkingChanged(bool)` và `SeatedChanged(bool)` — HUD và hướng dẫn nhận thay đổi mà không poll liên tục.
- `PlayerInteraction.FocusChanged(target)` — phát khi vật đang nhìn thay đổi, bị khóa hoặc component bị disable; không phát mỗi frame.
- `ExcelDataAuditorGame` cung cấp trạng thái đọc `IsTutorialActive`, `IsAttemptRunning` và sự kiện tiến bộ có ý nghĩa như hoàn thành một ô/sheet.
- `OfficeSubtitlePresenter.TryShow(request)` — request có ID, text, speaker, priority, duration và phase hợp lệ.
- Nếu cần tín hiệu audio đồng bộ thao tác, thêm event hành động `ReportCollected`, `ReportDelivered`, `WaterConsumed`; không suy diễn mọi âm thanh chỉ từ phase.

Event không replay lịch sử: subscriber đọc snapshot ban đầu khi khởi tạo. Bắt đầu lắng nghe và đọc snapshot trong cùng bước khởi tạo để tránh bỏ lỡ state. OnDisable tháo event, hủy timer/coroutine và dọn marker/highlight.

### Sở hữu dữ liệu và khởi tạo

- Transform cửa, ghế, máy tính, máy photo, bình nước, cửa phòng sếp: reference scene/anchor.
- Nội dung và tuning: `OfficeExperienceConfig`, gán trong Inspector.
- Phase và cờ nhiệm vụ: `OfficeSceneController`; tutorial progress: mini-game/tutorial controller; hint timer và seen beat: state runtime của controller tương ứng.
- Reset state khi bắt đầu lượt chơi mới. Đóng Excel vẫn giữ dữ liệu và vị trí tutorial trong lượt hiện tại. Chưa dùng PlayerPrefs cho tiến độ hoặc trạng thái truyện.
- Thêm bước binding vào bridge hiện có hoặc một `OfficeExperienceInstaller` nhỏ nếu cần. Trình tự: kiểm tra reference → khởi tạo presenter/view → gắn nguồn state → đọc snapshot → bật nhận input. Không thêm chuỗi execution-order attribute.

## 4. Thứ tự implement

### P1 — Event, objective và an toàn tích hợp

Sửa `OfficeSceneController`, `OfficeShiftHUD`, `OfficeBlockoutGameplayBridge`.

- Bổ sung contract phía trên; giữ nguyên giá trị enum hiện có vì scene đang serialize enum bằng số.
- HUD thu gọn thành tiêu đề chương, một mục tiêu hiện tại, chỉ dẫn thao tác phù hợp trạng thái ngồi/đứng. Không đồng thời hiển thị hai dòng objective trùng nhau từ HUD cũ và mới.
- Hoàn thành bước hiện phản hồi ngắn rồi cập nhật mục tiêu mới. Cập nhật chữ khi state đổi; hiển thị/ẩn theo pause, Excel và Leaving.
- Giữ tương thích public entry point và các hook smoke test hiện tại.

**Đạt khi:** cả luồng nhiệm vụ hoạt động như baseline, luôn chỉ có một mục tiêu chính, handoff intro chạy một lần.

### P2 — Dẫn đường theo mức mắc kẹt

Sửa `PlayerInteraction`; thêm guidance, state và marker.

- Bind đích theo state: cửa phòng sếp trong intro → ghế Tý → máy tính khi đã ngồi → máy photo → cửa nộp báo cáo → bình nước → ghế khi đứng hoặc máy tính khi đã ngồi nghỉ.
- Mốc đề xuất, chỉnh trong config: 20 giây hiện gợi ý chữ; 40 giây thêm marker; giữ H để xem hướng dẫn hiện tại. Đây là giá trị khởi đầu để playtest.
- Chỉ tăng timer khi người chơi thực sự điều khiển được. Dừng khi pause, cinematic, Excel, chuyển ghế hoặc modal khóa input.
- Reset khi phase đổi hoặc có tiến bộ được xác nhận. Di chuyển ngẫu nhiên/nhấn E vào đồ không liên quan không reset timer.
- Marker là chỉ hướng tới vị trí, không phải đường đi. Kiểm tra góc sau lưng/ngoài màn hình; không cho marker tạo khả năng tương tác xuyên tường.
- Intro chưa handoff cần nghe state/exit/seat của intro, không chỉ `OfficePhase.ReturnToDesk`.

**Đạt khi:** người đứng yên nhận gợi ý đúng thứ tự; pause không tiêu thời gian; marker không trỏ vào ghế sau khi đã ngồi hoặc trỏ vào phòng sếp khi chưa lấy giấy.

### P3 — Nhìn là nhận biết được vật tương tác

Thêm `OfficeInteractionHighlight`; dùng focus event của raycast đang có.

- Bản đầu dùng icon/vòng chỉ điểm nhẹ và crosshair; màu đi kèm chữ/icon để không phụ thuộc riêng màu sắc.
- Nếu cần viền mesh, đánh giá riêng shader/render pass đang dùng. Không đổi shader hay material nội thất hàng loạt chỉ để thêm highlight.
- Tách focus highlight khi ở gần với objective marker hỗ trợ tìm đường.
- Mất focus, mở UI, pause, disable hoặc chuyển scene phải xóa highlight.
- Tương tác sai thứ tự có lời giải thích và cooldown khoảng 2–3 giây; không đổi phase hay lặp âm thanh liên tục.

**Đạt khi:** đúng vật nhận prompt E; vật bị tường chắn không thể tương tác; đổi focus nhanh không để lại icon trên vật cũ.

### P4 — Tutorial Excel và chế độ dễ tiếp cận

Sửa `ExcelDataAuditorGame`; thêm `ExcelTutorialController`.

- Luồng nội bộ: `Closed → Tutorial → Attempt → SuccessFeedback/RetryFeedback`. Dữ liệu sheet và phase nhiệm vụ vẫn do hệ thống hiện tại sở hữu.
- Lần mở đầu: chỉ ô mẫu, giải thích “Lợi nhuận = Doanh thu − Chi phí” bằng một ví dụ riêng; người chơi tự nhập kết quả ở báo cáo thật.
- Có nút Tiếp tục/Bỏ qua hướng dẫn. Chỉ sau đó mới bắt đầu timer và lịch popup sếp.
- Sheet thứ hai có gợi ý cộng cột Thực tế. Nhập sai hiển thị lý do và focus ô sai; giữ các ô đã làm đúng.
- Đóng/mở giữa tutorial tiếp tục bước cũ; đóng/mở trong attempt giữ thời gian còn lại như hiện tại. Không tạo timer hoặc popup coroutine trùng.
- Thêm `Timed` và `Relaxed` qua config và lựa chọn rõ trước khi bắt đầu. Relaxed không có timeout; logic đúng/sai và diễn biến truyện giống nhau.
- Retry không phát lại toàn bộ tutorial. Chuẩn hóa font cho cả input, placeholder, tab, popup và text tạo runtime.

**Đạt khi:** timer/popup không chạy lúc đọc hướng dẫn, pause hoặc modal; cả hai chế độ hoàn thành được hai sheet; retry giữ dữ liệu.

### P5 — Kể chuyện và nhịp cảm xúc

Thêm narrative director, subtitle presenter và cấu hình beat; chuyển dần nội dung đang hardcode sang config.

| Trigger | Nội dung dự kiến | Điều khiển |
| --- | --- | --- |
| Handoff về bàn lần đầu | “Xong báo cáo này… mình về thôi.” | Phụ đề ngắn, không khóa di chuyển |
| Hoàn thành Excel | Thông báo gửi lệnh in rõ ràng | Phản hồi nhiệm vụ ưu tiên hơn độc thoại |
| Lấy bản in | Tiếng giấy và “Bản in còn ấm.” | Một lần trong lượt |
| Giao báo cáo | Câu trả lời ngắn của sếp, rồi khoảng yên | Không lặp nhắc deadline sau khi đã nộp |
| Uống nước đúng bước | “Hồi nhỏ, mẹ luôn gọi mình vào ăn cơm giờ này…” | Một lần; có thể đọc lại |
| Quay ghế nghỉ | “Chỉ ngồi một lát… rồi về.” | Theo cinematic gục xuống hiện có |

- Giữ intro phòng sếp hiện tại. Dừng các nguồn nhắc deadline khi đã nộp báo cáo.
- Ưu tiên phụ đề: cinematic bắt buộc → phản hồi nhiệm vụ → độc thoại → gợi ý. Hàng chờ giới hạn, chống trùng ID; gợi ý mất hiệu lực khi phase đổi phải bị bỏ.
- Chỉ có một owner ghi vào subtitle panel. Chuyển các call site cũ qua presenter để hàng chờ không bị bypass.
- Pause dừng thời lượng thoại; scene unload hủy queue. Phân biệt hoàn tất thoại với hủy thoại, không gọi callback hoàn tất khi đang teardown hoặc thay nội dung.
- Không tái sử dụng trực tiếp `DialogueUI` làm hệ thống phụ đề không khóa input: hiện component này acquire input và `HideDialogue()` còn gọi callback hoàn tất.
- Lưu các câu truyện quan trọng đã xem trong bộ nhớ lượt chơi; một nút đọc lại có thể dùng ở P6. Bỏ qua thoại không được bỏ qua nhiệm vụ.

**Đạt khi:** không chồng lời, gợi ý không cắt câu cảm xúc, bấm lặp không phát lại beat một lần; bật/tắt director không thay đổi điều kiện hoàn thành nhiệm vụ.

### P6 — Âm thanh, UI đọc lại và authoring

- Dùng `OfficeAtmosphere` cho nhịp âm thanh: work → print → yên sau nộp → memory bridge. Giới hạn thay đổi vào source thuộc hệ thống này để tránh tranh quyền với AudioManager.
- Chuyển âm lượng mềm theo thời gian; tôn trọng pause. Không cần thay đổi layout/ánh sáng đã author để hoàn thành bản đầu.
- Thêm bảng đọc lại ngắn cho lời giao việc và beat đã xem; mở bảng phải khóa input bằng GameManager, đóng bảng chỉ release owner của bảng.
- Nếu bổ sung hiệu ứng chóng mặt, cung cấp mức giảm hiệu ứng; không dùng rung/mờ mạnh để buộc người chơi hiểu trạng thái mệt.
- Tạo công cụ `KuTy/Office/Apply guidance and narrative` chỉ upsert component/UI/reference liên quan, có Undo và chạy lặp không tạo bản sao. Không gọi full gameplay/environment builder để thêm tính năng này.
- Validate reference bắt buộc, đích phase, ID beat duy nhất, thời lượng dương, font đầy đủ glyph. Thiếu audio tùy chọn vẫn chạy được bằng text.

**Đạt khi:** chạy setup hai lần không nhân đôi UI/event; scene không thay pose nội thất/đèn; text Việt đọc được ở 1366×768 và 1920×1080.

## 5. Kiểm thử và tiêu chí nghiệm thu

Chạy baseline 42 assertions sau các mốc thay đổi hành vi. Bổ sung kiểm thử tập trung cho logic mới:

- C# state: mốc 20/40 giây, reset khi tiến bộ, dừng timer đúng điều kiện; queue ưu tiên/chống trùng/bỏ request hết hạn.
- Play Mode: intro → gameplay không đăng ký event đôi; tutorial không trừ giờ; đóng/mở không tạo coroutine đôi; Relaxed không timeout; Timed retry giữ dữ liệu.
- Narrative: spam E ở bình nước, phase đổi khi đang chờ thoại, mở bảng đọc lại rồi pause, thoát scene khi đang phát beat đều không kẹt input.
- Tương tác: vật cản raycast, đổi focus, target bị disable, marker ngoài màn hình; không biến marker thành đường tương tác xuyên tường.
- UI: font tiếng Việt, contrast, text dài, hai độ phân giải, prompt không bị objective/overlay che.
- Lượt chơi thật từ MainMenu bằng WASD/chuột: mở cửa, tìm ghế, ngồi/đứng, đi tới máy photo và phòng sếp, uống nước, nghỉ và vào Act 2.

Playtest 3–5 người mới. Mục tiêu đề xuất: đa số tự hoàn thành nhiệm vụ đầu, không cần giải thích trực tiếp; không mắc kẹt quá 60 giây mà chưa có gợi ý hữu ích; trả lời được “Tý đang muốn gì?” và “Vì sao đoạn cuối gợi về gia đình?”. Đây là tiêu chí cần đo, chưa phải kết quả hiện có.

## 6. Chia đợt bàn giao

1. **Đợt A — Dễ chơi:** P1 + P2 + P3 + P4. Bàn giao một lượt chơi từ MainMenu, mục tiêu/marker/tutorial rõ ràng và test hồi quy.
2. **Đợt B — Có cảm xúc:** P5 + P6. Bàn giao nội dung tiếng Việt, subtitle queue, âm thanh theo nhịp và ảnh QA.
3. **Đợt C — Phát triển tiếp:** dựa vào playtest mới thêm điện thoại, lựa chọn trả lời, phản hồi ở Act 2 hoặc save/checkpoint. Mỗi lựa chọn phải có phản hồi thấy được; tránh thêm nhánh chỉ đổi một câu chữ.

Chưa đưa vào bản đầu: framework quest tổng quát, NPC AI, branching lớn, animation tay nhặt/uống, save xuyên phiên, thay package render hay dựng lại scene.

## 7. Hồ sơ triển khai

Khi bắt đầu code, cập nhật `docs/scene1-office-implementation.md` theo luồng in/giao/nước hiện tại; phần đầu tài liệu đó còn mô tả phiên bản 37 assertions và nhịp nghỉ 15 giây cũ. Ghi rõ file mới/sửa, kết quả test mới, giới hạn kiểm chứng thủ công và cảnh đang mở khi bàn giao.
