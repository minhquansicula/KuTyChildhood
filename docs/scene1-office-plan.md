# Scene 1 Office — Kế hoạch thiết kế và triển khai hoàn chỉnh

Cập nhật: 18/10/2026. Trạng thái tài liệu: sẵn sàng bàn giao cho model coding.

Đây là nguồn thiết kế chính của Scene 1 office. Tài liệu thay nội dung plan trước; [hướng dẫn blockout](office-blockout-guide.md) mô tả cách mở map và thay hình. Các mô tả Hồi 1 trong tài liệu ý tưởng cũ không được dùng để thay đổi luồng dưới đây.

## 1. Kết quả cần đạt và giới hạn phạm vi

Một đoạn mở đầu góc nhìn thứ nhất có thể chơi liên tục: **Tý đứng trong phòng sếp nghe mắng → được trả quyền điều khiển → tự mở cửa ra ngoài → đi qua văn phòng → về bàn cạnh cửa sổ → ngồi xuống để chuẩn bị làm việc**.

Ưu tiên một map rõ ràng, đúng tỷ lệ, tạo cảm giác văn phòng cuối ngày và thuận tiện thay asset. Toàn bộ phần hình có thể dùng khối mẫu; độ hoàn thiện phải đến từ bố cục, ánh sáng, góc nhìn, chuyển động và âm thanh trước.

| Trong phạm vi lần triển khai này | Để giai đoạn sau |
| --- | --- |
| Toàn bộ môi trường office và ngoại cảnh nhìn thấy qua cửa sổ | Email, phân loại hồ sơ, sửa báo cáo và các mini-game |
| Cuộc đối thoại trong phòng sếp, phụ đề và quản lý input | Kiệt sức, gục xuống bàn, chuyển về ký ức |
| Cửa mở được, hướng dẫn về bàn, camera ngồi | NPC đồng nghiệp đi lại, AI và lịch sinh hoạt |
| UI tiếng Việt tối thiểu, âm thanh môi trường | Rig/animation nhân vật đầy đủ và mỹ thuật cuối |
| Cấu trúc dễ thay model, kiểm thử và ảnh bàn giao | Thay Build Settings hoặc luồng MainMenu hiện hành |

Bỏ qua việc import/chuyển đổi `90s_retro_office_pack.glb`. Không tự chạy Tripo, tải asset hoặc thêm package để hoàn thành nhiệm vụ này. Dùng tài nguyên sẵn có phù hợp; thiếu hình hoặc voice thì dùng placeholder có chủ đích.

## 2. Hiện trạng đã xác minh từ project

- Unity 2022.3.62f3, URP 14.0.11, TextMesh Pro 3.0.6 và UGUI đã có.
- Scene làm việc: `Assets/_Project/Scenes/Act1_Office_Blockout.unity`. Chỉnh tiếp scene này, không sinh bản mới cho mỗi đợt.
- Map có 35 nhóm `_SLOT`, sàn khoảng 14 × 10 m, phòng sếp, ba bàn phụ, bàn Tý, khu tiện ích, cửa sổ và phông thành phố.
- `OfficeBlockoutWalkthrough` hiện chỉ phục vụ đi thử: tự xử lý cursor, raycast, cửa, ngồi/đứng tức thời và HUD `OnGUI` tiếng Anh. Chưa có cuộc đối thoại và trình tự trạng thái.
- `FirstPersonController` có di chuyển/nhìn, CharacterController, `IsMovementLocked`, `LockWalkingOnly`, `SetPitch` và `TeleportTo`. Hiện khóa toàn phần cũng khóa nhìn; khóa đi bộ không giới hạn xoay ngang.
- `OfficeOpeningSequence` của prototype phụ thuộc `OfficeSceneController`, HUD và nhiệm vụ cũ; có phần mở đen dài và gọi nhân vật là QUÂN. Chỉ tham khảo cách trình bày, không gắn nguyên chuỗi cũ vào map mới.
- `GameManager` hiện nhận diện scene bằng tên và quản lý cursor/input toàn cục; tên scene blockout chưa thuộc luồng Act1 chính.
- Font `Assets/_Project/Generated/Vietnamese.asset` đã có; cần kiểm tra glyph thực tế trước khi dùng.
- Có voice `Boss_Opening_Report.wav`, tiếng đập bàn, bàn phím xa và giao thông. Chưa xác minh nội dung voice khớp kịch bản mới.
- Đã có kiểm tra capsule tuyến đi báo không bị chắn và một lần quan sát runtime 5 giây không lỗi. Các kết quả này chưa chứng minh toàn bộ tương tác đã chạy đúng.

**Lưu ý công cụ hiện có:** `KuTyOfficeBlockoutBuilder.Capture()` còn di chuyển màn hình bàn sếp, thân placeholder và nhãn, bật lại trần/ngoại cảnh rồi lưu scene. Phải tách phần sửa bố cục khỏi chức năng chụp/kiểm tra trước khi dùng để nghiệm thu; không để lệnh chụp ảnh âm thầm đổi kết quả người dùng đang chỉnh.

## 3. Chỉ đạo mỹ thuật và cảm xúc

Văn phòng thành thị Việt Nam cuối buổi chiều, nhỏ, hơi cũ, ngăn nắp vừa đủ. Cảm giác căng thẳng đến từ khoảng cách với sếp, bàn làm việc lặp lại, tiếng máy móc và đồ vật cho thấy làm việc lâu. Tránh biến không gian thành phòng kinh dị hoặc phủ tối để che phần chưa làm.

- Tường xám nhạt hơi ngả xanh, thảm xám đậm, kim loại tối, bàn gỗ nhạt.
- Phòng sếp dùng gỗ sẫm hơn, ít đồ bừa; bàn Tý có giấy tờ và cốc cà phê.
- Ánh trần trắng lạnh vừa phải; nắng chiều vàng nhạt ở bàn Tý tạo điểm định hướng.
- Tỷ lệ đồ vật gần đời thực; hình khối đơn giản, cùng mức chi tiết.
- Chỉ một chi tiết nhận diện riêng tại bàn Tý: áo khoác màu đất vắt trên ghế. Không thêm nhiều điểm sáng cạnh tranh.
- Tối đa vài đạo cụ nhỏ trên mặt bàn; giữ khoảng trống cho góc nhìn ngồi và model sau này.

Ba khung hình cần được thiết kế có chủ đích: đối diện sếp; nhìn ra khu nhân viên từ cửa phòng; ngồi tại bàn Tý.

## 4. Mặt bằng và quy ước tọa độ

Giữ hệ tọa độ map hiện tại: Y hướng lên, X dương về phía cửa sổ, Z dương về phía phòng sếp/tủ hồ sơ. Một Unity unit tương ứng một mét.

```text
                         BẮC / Z+
   X=-7                                          X=+7
   ┌──────────────────┬────────────────────────────┐
   │    PHÒNG SẾP     │ TỦ HỒ SƠ   MÁY IN   NƯỚC  │
   │                  │                            │
   │       SẾP        │                            │
   │       BÀN        │                            │
   │       TÝ → [CỬA] →→→ LỐI ĐI →→→┐             │
   ├──────────────────┘             ↓             │
   │                                ↓        CỬA  │
   │ BÀN A     BÀN B     BÀN C       ↓        SỔ   │
   │ GHẾ       GHẾ       GHẾ         └→ GHẾ  BÀN  │
   │                                        TÝ   │
   └───────────────────────────────────────────────┘
                         NAM / Z-
```

Sơ đồ không đúng tỷ lệ. Đây là vị trí hiện có cần kiểm tra lại trong Editor, không phải tọa độ bắt buộc phải ghi đè:

| Mốc | Vị trí tham khảo (X, Y, Z) | Ghi chú |
| --- | --- | --- |
| Sàn | tâm (0, 0, 0), biên X ±7, Z ±5 | Trần khoảng 3 m |
| Phòng sếp | X từ -7 đến -2, Z từ 1 đến 5 | Hiện khoảng 5 × 4 m, không phải 4 × 4 m của ý tưởng ban đầu |
| Điểm xuất phát | (-4.8, 0.04, 2.05) | Mắt cao 1.65 m, nhìn về sếp ở Z dương |
| Bản lề cửa | (-2, 0, 2.8) | Mở vào trong phòng sếp; hiện góc mở 95° |
| Bàn Tý | (5.5, 0, -2.4), yaw 90° | Ngồi nhìn hướng X dương |
| Ghế Tý | (4.4, 0, -2.4), yaw 90° | Tiếp cận từ phía lối đi |
| TySeatAnchor | (4.55, 0, -2.4), yaw 90° | Mốc thân người chơi, không phải mốc mắt |
| Bàn phụ | X -5.5, -2.8, -0.1; Z -2 | Ba bàn cùng mẫu |
| Tiện ích | dọc tường Z khoảng 4.4–4.6 | Không chắn cửa hoặc tuyến đi |

Tuyến kiểm tra ban đầu: (-4.8,2.05) → (-2.7,2.05) → (-1.2,2.05) → (2.8,2.05) → (2.8,-2.4) → (3.55,-2.4), các cặp là X/Z. Tuyến dài khoảng 13 m; với tốc độ 2 m/s mất khoảng 6–8 giây khi đi thẳng, cộng thao tác và quan sát khoảng 10–20 giây. Không kéo dài đường đi để ép thời lượng.

Thông số khuyến nghị: lối chính rộng ít nhất 1.2 m; khoảng sau ghế khoảng 0.9–1.1 m; mặt bàn cao 0.74–0.8 m; mặt ghế khoảng 0.45–0.5 m. Cửa blockout hiện rộng khoảng 1.4 m; giữ nếu đi tốt, chỉ thu về khoảng 1–1.1 m sau khi kiểm tra capsule và hướng mở. Không thu hẹp chỉ để khớp một con số trong plan.

Bổ sung cửa ra vào văn phòng dạng đóng, không tương tác, trên mảng tường phù hợp nếu cần giải thích công năng căn phòng. Đây là chi tiết môi trường; không mở thêm khu map hoặc nhiệm vụ.

## 5. Phân khu và bộ đồ vật tối thiểu

| Khu | Thành phần bắt buộc | Tiêu chí bố trí |
| --- | --- | --- |
| Kiến trúc | Sàn, trần, tường, chân tường, dãy cửa sổ, vách phòng sếp | Một phòng liên tục, không lộ mép nền/mặt sau từ góc chơi |
| Phòng sếp | Bàn, ghế sếp, placeholder sếp, tủ, báo cáo; ghế khách nếu còn khoảng trống | Thấy rõ đầu/thân sếp, màn hình lệch sang bên, không che cuộc đối thoại |
| Cửa phòng sếp | Khung, cánh riêng, tay nắm, bản lề, biển “PHÒNG GIÁM ĐỐC” | Mở vào khoảng trống trong phòng, không quét qua người hoặc đồ đạc |
| Khu nhân viên | Ba bàn phụ, ghế, máy tính, vách ngăn thấp và ít hồ sơ | Thống nhất mẫu; khác nhau bằng cách đặt giấy/ghế |
| Bàn Tý | Bàn, ghế, máy tính, giấy, cốc, chai nước, sticky note, áo khoác | Nhận ra từ lối đi; màn hình và đồ vật đọc rõ khi ngồi |
| Tiện ích | Hai tủ hồ sơ, máy in, cây nước, thùng rác, đồng hồ 17:42 | Đặt sát mép phòng; không cần interaction |
| Cửa sổ/ngoại cảnh | Khung kính, tòa nhà xa, mặt đường ở thấp hơn | Không thấy khoảng trống hoặc mép phông khi đứng sát kính |
| Âm thanh | Voice/fallback, đập tài liệu, điều hòa, bàn phím xa, giao thông, cửa, ghế | Mỗi nguồn có vị trí và vai trò rõ |

Đạo cụ phụ như rèm, cây nhỏ, ổ điện hoặc lịch chỉ thêm sau khi các phần bắt buộc đạt. Chưa cần vật kỷ niệm, ảnh gia đình hoặc viên bi trong phạm vi mở đầu này.

## 6. Kịch bản theo từng nhịp

### A. Bắt đầu trong phòng sếp — khoảng 20–30 giây

Mở trực tiếp tại vị trí Tý đứng; có thể fade rất ngắn 0.3–0.5 giây để tránh lóe khung hình khởi tạo. Không dùng màn đen nhiều giây. Sếp ở phía đối diện bàn, cửa đóng bên phải Tý.

| Thứ tự | Người nói/hành động | Nội dung đề xuất | Thời lượng fallback |
| --- | --- | --- | --- |
| 1 | Sếp | “Báo cáo này cậu làm kiểu gì vậy? Số liệu còn thiếu thế này mà cũng gửi lên?” | 6–7 giây |
| 2 | Sếp | “Khách hàng đang chờ. Tôi đã nhắc cậu từ sáng rồi.” | 4–5 giây |
| 3 | Hành động | Đặt mạnh tập tài liệu xuống bàn, phát một tiếng đập | khoảng 0.8 giây |
| 4 | Sếp | “Về sửa lại cho xong. Trước khi về gửi lại cho tôi.” | 4–5 giây |
| 5 | Tý | “Dạ… em sửa ngay ạ.” | 2–3 giây |
| 6 | Khoảng lặng | Tý hơi hạ mắt; kết thúc phụ đề, chuẩn bị trả điều khiển | khoảng 1 giây |

Khóa đi lại/tương tác. Cho nhìn quanh nhẹ quanh hướng ban đầu, khuyến nghị yaw ±30°, pitch khoảng -15° đến +20°. Không vừa chạy animation camera vừa để motor ghi đè góc nhìn. Khi trả điều khiển phải giữ góc hiện tại, không giật lại tâm.

Voice hiện có chỉ dùng khi đã nghe/đối chiếu lời thoại. Nếu clip không khớp, dùng phụ đề có thời lượng fallback; không ép chia clip cũ theo tỷ lệ để giả khớp câu mới. Không cần tạo voice mới.

### B. Mở cửa — khoảng 3–8 giây

Hiện nhẹ “Quay lại bàn làm việc.” sau cuộc đối thoại. Tý tự quay sang cửa, tiến lại và nhấn E khi nhìn trúng cánh/tay nắm. Prompt: “[E] Mở cửa”.

Cánh quay vào trong phòng, hết hành trình rồi giữ mở. Trong luồng chính không cần đóng lại. Không phát lại tiếng sếp hoặc khóa camera khi đi qua cửa.

### C. Đi về bàn — khoảng 10–20 giây có quan sát

Lối đi dẫn qua khu bàn phụ; máy in, tủ và cây nước là nền. Không xuất hiện nhiệm vụ phụ, popup quảng cáo tính năng hoặc dấu chỉ đường dày đặc. Dùng áo khoác và vệt nắng để nhận diện bàn Tý.

Nếu người chơi nhìn quanh hoặc quay lại, luồng vẫn chờ bình thường; không có timer phạt. Người chơi không mở được mini-game ở các máy tính khác.

### D. Ngồi vào chỗ — khoảng 1–2 giây

Khi nhìn trúng ghế Tý trong khoảng tương tác, hiện “[E] Ngồi xuống”. Camera chuyển êm khoảng 0.6–1 giây vào vị trí làm việc. Có thể phát tiếng ghế nhỏ một lần.

Kết thúc hiển thị “Đã về chỗ làm việc.” trong khoảng 2 giây rồi ẩn. Người chơi nhìn quanh giới hạn tại bàn, không đi bộ. Phát tín hiệu `ReadyAtDesk` một lần để gameplay tương lai nối vào. Không tự bật nhiệm vụ công việc, kiệt sức hoặc load scene.

## 7. Máy trạng thái, pause và quyền input

```text
Initialize → OpeningDialogue → ReturnToDesk → SeatingTransition → ReadyAtDesk
                │                  │                 │              │
                └──────────────────┴──── Pause ──────┴──────────────┘
                         Resume về đúng trạng thái trước đó
```

Pause là cờ phủ lên trạng thái chính, không làm mất tiến trình hoặc chạy lại thoại.

| Trạng thái | Đi bộ | Nhìn | E | Cursor / UI |
| --- | --- | --- | --- | --- |
| Initialize | Khóa | Khóa | Khóa | Chưa hiện prompt, xác minh reference |
| OpeningDialogue | Khóa | Giới hạn | Khóa | Khóa cursor, hiện phụ đề |
| ReturnToDesk | Cho phép | Bình thường | Cửa/ghế hợp lệ | Khóa cursor, objective/crosshair |
| SeatingTransition | Khóa | Do chuyển camera điều khiển | Khóa | Ẩn prompt |
| ReadyAtDesk | Khóa | Giới hạn quanh màn hình | Không có hành động mới | Ẩn objective, crosshair tùy ngữ cảnh |
| Paused | Khóa | Khóa | Khóa | Thả cursor, hiện panel “Tạm dừng / Tiếp tục” |

Esc mở/đóng pause tối thiểu. Pause phải dừng cả tiến độ phụ đề, voice và chuyển động cửa/camera; Resume tiếp tục đúng chỗ, không bỏ qua câu hoặc nhảy camera. Không dựa riêng vào `Time.timeScale` nếu coroutine đang dùng unscaled time hoặc audio vẫn phát.

Khi mất focus, dừng nhận input và pause; trở lại vẫn chờ Resume. Nếu tích hợp GameManager tồn tại, chỉ Acquire/Release input bằng owner của scene; không xóa owner của UI khác. Một thành phần duy nhất chịu trách nhiệm cursor/input của đoạn này.

## 8. Thiết kế tương tác và camera

### Cửa

- Tách trạng thái cửa `Closed → Opening → Open`; E trong lúc Opening bị bỏ qua. Luồng chính chỉ cần mở một lần.
- Lưu rotation đóng/mở từ anchor, không giả định mọi model mới có local rotation bằng 0.
- Trước khi mở, xét vùng quét: nếu người chơi chặn cánh thì hoãn mở và hiện “Lùi lại một chút để mở cửa.”; không đẩy người xuyên bàn. Có thể dừng chuyển động nếu phát hiện cản trong hành trình.
- Collider phải theo cánh, khung cửa không bị vô hiệu hóa để cho qua. Khi mở hết, chỗ thoát phải đủ rộng.
- Phát tiếng tay nắm/bản lề đúng một lần. Thiếu clip vẫn hoạt động.
- Raycast phải xét cả vật cản thực: không tương tác ghế/cửa xuyên tường khi chỉ raycast riêng layer Interactable.
- Chỉ xác nhận đã ra phòng bằng trigger đúng vùng cửa hoặc kiểm tra không gian tương đương, không chỉ dựa vào E đã được nhấn. Điều này dùng đổi ambience, không tạo objective phụ.

### Ghế và tư thế ngồi

- Logic trên object ổn định của ghế Tý; các ghế phụ không được nhận nhầm.
- Chuẩn hóa camera đứng 1.65 m; tư thế ngồi khoảng 1.15–1.25 m trên mặt sàn, chỉnh từ hình thật.
- Khóa motor trước chuyển động; xử lý CharacterController an toàn khi chuyển đến seat anchor; reset vận tốc rơi và đồng bộ pitch sau chuyển động.
- Ghế/collider không được đẩy camera, không xuyên mặt bàn. Near clip hiện khoảng 0.05 m; không dùng near clip cực nhỏ để che sai vị trí.
- Chỉ một hệ thống ghi transform camera tại một thời điểm; sau transition motor nhận đúng góc cuối, không snap ở frame kế.
- ReadyAtDesk không cần đứng dậy trong luồng chính. Nếu giữ chức năng đứng để debug, mặc định tắt và kiểm tra vùng đứng còn trống.
- Không bật lại motor/CharacterController với vị trí đã nằm trong collider khi thoát trạng thái hoặc tắt component.

## 9. Kiến trúc code đề xuất

Quy mô dự án: game nhỏ. Dùng MonoBehaviour với reference trực tiếp, coroutine và event đơn giản; không thêm service locator, event bus, DI framework hoặc hệ nhiệm vụ tổng quát cho đoạn này.

Tên class dưới đây là đề xuất. Có thể gộp phần nhỏ nếu trách nhiệm và kiểm thử vẫn rõ; không tạo thêm lớp chỉ để khớp bảng.

| Thành phần | Sở hữu/trách nhiệm | Không sở hữu |
| --- | --- | --- |
| `OfficeIntroController` | Điểm vào duy nhất, trạng thái, init, pause, input policy và sự kiện ReadyAtDesk | Không tạo mesh, không chạy mini-game |
| `OfficeDialoguePresenter` | Tuần tự câu, subtitle, voice/fallback, pause/cancel | Không tự mở khóa player |
| `OfficeDoorInteractable` | Eligibility, chuyển động/cản cửa, SFX, báo mở/đi qua | Không load scene hoặc điều khiển objective |
| `OfficeSeatInteractable` | Eligibility, chuyển camera, báo hoàn thành ngồi | Không tự khởi động công việc |
| `OfficeIntroHUD` | Objective, prompt, subtitle, pause panel | Không tự bắt E hoặc quyết định tiến trình |
| `OfficeAmbience` | Loop môi trường, phối âm trong/ngoài phòng, pause | Không quyết định trạng thái cốt truyện |
| Motor/interaction hiện có | Đọc input, di chuyển, nhìn, xác định mục tiêu và gọi tương tác | Không thêm bản thứ hai hoạt động song song |

`OfficeIntroController` có thể giữ policy trên motor bằng API hẹp; chỉ sửa `FirstPersonController` khi cần giới hạn nhìn/đặt lại vận tốc. Hành vi mặc định trong scene khác phải giữ nguyên.

Đường tương tác ưu tiên dùng `PlayerInteraction` + `IInteractable` hiện có sau khi kiểm tra occlusion và cách tìm component trên cha collider. Nếu hệ cũ không phù hợp, dùng bộ tương tác cục bộ duy nhất trong scene này và ghi rõ lý do. Không vừa bật walkthrough bắt E vừa bật interaction mới.

### Data và cấu hình

- Một danh sách serializable chứa speaker, text, voice tùy chọn, thời lượng fallback và khoảng nghỉ là đủ cho một đoạn thoại.
- Tạo ScriptableObject config chỉ khi có lợi cho chỉnh nội dung/tái dùng; runtime state nằm trên controller, không lưu vào asset config.
- Inspector có các nhóm rõ: Player, Anchors, Dialogue, Door, Seat, HUD, Audio, Debug.
- Hiển thị tham số chỉnh được: walk speed, interaction distance, góc nhìn giới hạn, góc/tốc độ cửa, thời lượng ngồi, mức âm.
- Event chính: `OpeningFinished`, `DoorOpened`, `PlayerExitedBossRoom`, `Seated`, `ReadyAtDesk`; chỉ nối event thật sự được dùng, tránh gọi vòng lặp.
- Reference bắt buộc thiếu phải báo tên component/field một lần, dừng luồng an toàn; không im lặng bỏ qua.
- Voice, SFX, animator thiếu là tùy chọn; không làm hỏng đường chơi.

## 10. Hợp đồng scene và thứ tự khởi tạo

Giữ root `OFFICE_BLOCKOUT` cùng sáu nhóm map hiện có; thêm Systems/UI/Anchors có tổ chức. Không di chuyển toàn map chỉ để chụp đẹp.

```text
OFFICE_BLOCKOUT
├─ 01_Architecture
├─ 02_BossOffice
│  ├─ BossDesk_SLOT
│  ├─ BossPlaceholder_SLOT
│  └─ BossDoor_Hinge
│     └─ DoorLeaf_SLOT
├─ 03_Workstations
│  ├─ StaffDesk_* / StaffChair_*
│  ├─ TyDesk_SLOT / TyChair_SLOT
│  └─ TySeatAnchor
├─ 04_UtilityArea
├─ 05_Exterior_Backdrop
└─ 06_Lighting
Player
└─ PlayerCamera
OfficeIntroSystems
OfficeIntroUI
OfficeIntroAnchors
```

Bắt buộc: đúng một camera người chơi bật và một AudioListener; Player có CharacterController và motor; controller nối HUD/dialogue/door/seat; cửa có pivot/collider; ghế có seat anchor; HUD có font hỗ trợ tiếng Việt.

Trình tự: xác minh reference → cache pose ban đầu và khóa input → reset cửa/ghế/UI → khởi động ambience → chạy thoại → chuyển trạng thái. Chỉ controller bắt đầu luồng; các component khác không tự chạy nội dung trong Start.

OnDisable/OnDestroy: dừng coroutine thuộc mình, bỏ đăng ký event, dừng voice của mình, trả đúng input owner và khôi phục các trạng thái tạm mình đã thay. Không phát event thành công khi đang hủy scene.

Chạy Play trực tiếp phải hoạt động khi không có GameManager. Không tự tạo thêm GameManager toàn cục. Trường hợp đã có GameManager do vào từ MainMenu cần kiểm tra riêng trước khi tích hợp chính thức, vì scene blockout chưa được nhận diện là Act1.

## 11. HUD, chữ và khả năng đọc

Thay OnGUI debug bằng UGUI/TMP gọn, dùng font Việt đã có nếu glyph đạt. Giữ scale theo độ phân giải tham chiếu 1920 × 1080, kiểm tra thêm 1366 × 768.

| UI | Nội dung/quy tắc |
| --- | --- |
| Subtitle | Tên “SẾP” hoặc “TÝ”, tối đa khoảng hai dòng; nền tối trong suốt đủ tương phản |
| Objective | “Quay lại bàn làm việc.”, nhỏ ở góc; hiện sau thoại |
| Crosshair | Chấm nhỏ giữa màn hình, đổi nhẹ khi mục tiêu tương tác hợp lệ |
| Prompt | “[E] Mở cửa” / “[E] Ngồi xuống”, chỉ hiện một prompt |
| Pause | “Tạm dừng”, nút “Tiếp tục”, không cần menu cấu hình mới |
| Hoàn thành | “Đã về chỗ làm việc.” ngắn rồi ẩn |

Không hiện identifier, state enum, tọa độ, tên script hoặc hướng dẫn debug dài trên màn hình chơi. Chỉ giữ thông tin đó trong Inspector/Console khi bật debug. Kiểm tra các chữ “phòng”, “sếp”, “ngồi”, “đã”, dấu ba chấm và tên Tý không thành ô vuông.

## 12. Ánh sáng, material và âm thanh

### Ánh sáng/material

- Xác lập độ sáng trong Game View trước; ảnh Scene View từ trên chỉ giúp đọc mặt bằng.
- Sếp được nhìn rõ ở khung mở đầu; màn hình dịch lệch vừa đủ, thân/đầu liền nhau; placeholder vẫn có hình thể.
- Vách kính có màu/truyền sáng hợp lý; nếu shader kính chưa có, giữ mảng mẫu và ghi rõ chờ asset. Không giả kính bằng mảng đen làm tường bị lỗi thị giác.
- Tắt/giảm các point light không cần thiết; hạn chế đèn realtime đổ bóng, không tăng giới hạn render pipeline toàn project chỉ để sửa một góc.
- Tắt DOF trong bản nghiệm thu đầu, tránh mờ phụ đề/góc làm việc. Bloom nhẹ nếu cần; không yêu cầu postprocess để map sử dụng được.
- Bàn Tý có vệt sáng chiều, nhưng trắng giấy không cháy và màn hình không mất chữ.
- Hiện chưa có texture/material cuối; không cần bake lighting trước khi bố cục ổn định.

### Âm thanh

- Giọng sếp gần trong phòng; âm máy móc bên ngoài giảm khi cửa đóng, tăng nhẹ khi cửa mở/người chơi ra ngoài.
- Giao thông đặt phía cửa sổ; bàn phím xa đặt ở khu nhân viên; cây nước và máy in chưa cần sự kiện in/rót.
- Một loop cho mỗi nguồn cần thiết, không nhân bản ambience qua nhiều lần pause/restart.
- Mức tham khảo tương đối: voice nổi nhất, ambience thấp hơn rõ rệt, tiếng cửa/ghế nghe được nhưng không át thoại.
- Chưa có AudioMixer thì có thể điều chỉnh volume qua component; chỉ thêm mixer khi cần xử lý lọc/phối nhiều nguồn.
- Không phát “tiếng uống nước” hoặc âm ký ức trong giai đoạn này.

## 13. Quy trình thay asset của người dùng

Giữ mỗi món đồ là `_SLOT` với transform ổn định và các con `Visual_REPLACE` / `Collision`. Reference tương tác và anchor nằm trên phần ổn định, không nằm trên mesh sẽ bị xóa.

Quy trình: tắt khối mẫu → thêm model con → chỉnh local position/rotation/scale → kiểm tra mặt sàn/mặt bàn/pivot → chỉnh collider nếu hình thay đổi → đi thử → xác nhận rồi mới bỏ hình mẫu. Giữ ít nhất một phiên bản khối mẫu có thể bật lại trong lúc ráp.

Cửa thay asset phải trùng cạnh bản lề; ghế thay asset phải giữ mốc mắt/tư thế ngồi. Không scale Player để khớp model; chỉnh model về tỷ lệ mét. Material dùng shader URP và gán texture đúng loại sau khi asset thật được cung cấp.

Chưa yêu cầu tách pack thành prefab hàng loạt. Có thể tạo prefab cho món đã ổn định; không gộp toàn office thành một mesh khiến cửa và nội thất khó thay.

## 14. Các đợt coding và cổng kiểm tra

| Đợt | Công việc cụ thể | Đủ điều kiện sang đợt tiếp theo |
| --- | --- | --- |
| 0. Đọc và bảo toàn | Đọc plan, scene, script liên quan, git status; ghi baseline, xác định bridge/editor đúng project | Biết phần hiện có và thay đổi của người dùng; không chỉnh scene khác |
| 1. Chốt không gian | So bốn góc nhìn, tỷ lệ, cửa và tuyến đi; hoàn thiện chỗ trống/biển phòng | Đi được toàn tuyến, camera không lộ khoảng trống; ảnh trước/sau |
| 2. Tổ chức scene/code | Tách input owner, controller/HUD và reference; gỡ vai trò runtime của walkthrough khi thay bằng hệ mới | Compile sạch; chỉ một đường E/cursor/motor; scene vẫn Play độc lập |
| 3. Mở đầu | Data thoại, TMP subtitle, voice fallback, giới hạn nhìn và trả input | Thoại đúng thứ tự, không đi/mở cửa sớm, hết thoại tự nhiên |
| 4. Cửa và về bàn | Trạng thái cửa, vùng quét/collider, occlusion, objective và ambience | E không tạo animation chồng, mở và đi qua cửa được bằng input |
| 5. Ngồi/kết thúc | Seat eligibility, camera transition, ReadyAtDesk một lần | Không xuyên bàn, snap, rơi hoặc khởi động mini-game |
| 6. Hoàn thiện và nghiệm thu | Pause/focus/reload, font, audio, ánh sáng, cải thiện công cụ chụp | Qua ma trận kiểm tra, ảnh và báo cáo kết quả cụ thể |

Sau mỗi đợt: compile, kiểm tra đúng hành vi vừa thay; không đợi cuối mới Play. Không hứa xong trong số giờ cố định khi chưa biết tình trạng Editor và asset. Thứ tự ưu tiên: chơi liên tục được → đúng không gian/cảm xúc → polish.

## 15. Ma trận kiểm thử tối thiểu

| Ca | Thao tác | Kết quả đạt |
| --- | --- | --- |
| T01 | Mở scene mới rồi Play trực tiếp | Spawn đúng trong phòng sếp, cửa đóng, không null reference |
| T02 | WASD/E khi sếp đang nói | Không đi hoặc tương tác sớm; nhìn chỉ trong giới hạn |
| T03 | Chạy hết thoại với voice | Thứ tự đúng, subtitle khớp clip thực sự dùng |
| T04 | Bỏ voice/Animator tùy chọn | Fallback hoàn tất, không kẹt |
| T05 | Pause giữa câu và Resume | Audio/phụ đề tiếp tục đúng chỗ, không chạy lại từ đầu |
| T06 | Kết thúc thoại | Input/objective trả đúng một lần, góc nhìn không giật |
| T07 | Nhìn ghế/cửa qua tường | Không có prompt hoặc tương tác xuyên vật cản |
| T08 | Nhấn E liên tục ở cửa | Chỉ một hành trình mở và một lượt SFX |
| T09 | Đứng trong vùng quét của cửa | Hoãn/dừng mở an toàn; không đẩy xuyên vật |
| T10 | Đi qua cửa mở ở nhiều hướng | Không mắc khung, không xuyên cửa đóng |
| T11 | Đi về bàn, dừng/đổi hướng/quay lại | Không mất objective hoặc soft-lock |
| T12 | Nhìn các ghế nhân viên khác | Không nhận prompt ngồi nhầm |
| T13 | Nhấn E nhiều lần khi bắt đầu ngồi | Một transition, một sự kiện hoàn thành |
| T14 | Pause giữa transition ngồi | Tiếp tục êm, không teleport/snap khi Resume |
| T15 | Nhìn quanh sau khi ngồi | Trong giới hạn, không xuyên mặt bàn/thân ghế |
| T16 | Sẵn sàng ở bàn | ReadyAtDesk phát một lần, không tự mở công việc/chuyển cảnh |
| T17 | Mất focus rồi trở lại | Chờ Resume, cursor/input nhất quán |
| T18 | Stop/Play hoặc reload scene | Reset spawn/cửa/thoại, không chồng loop âm hoặc event |
| T19 | Tạm thay visual cửa hoặc ghế | Reference tương tác và anchor vẫn hoạt động |
| T20 | 1920×1080 và 1366×768 | Tiếng Việt đủ glyph, UI không tràn/chồng |
| T21 | Các scene khác dùng motor chung | Hành vi mặc định không bị thay đổi bởi tùy chọn office |
| T22 | Chụp/validate map | Không đổi transform, active state hoặc lưu scene ngoài ý muốn |

Test tự động ưu tiên các rủi ro logic: thứ tự trạng thái, hoàn thành một lần, thiếu voice, pause và teardown. Collision/góc camera cần Play và quan sát hình; capsule cast chỉ là kiểm tra bổ trợ, không thay thế đi bằng CharacterController.

Bản thử thiếu reference bắt buộc: báo lỗi cấu hình rõ ràng một lần và ngăn luồng chạy sai. Đây là ca chẩn đoán có kiểm soát; bản scene bàn giao phải đủ reference và không có lỗi Console mới.

## 16. Tiêu chí nghiệm thu và bằng chứng

Hoàn thành khi người kiểm tra có thể mở đúng một scene, Play và đi trọn đoạn trong khoảng 35–60 giây nếu không dừng khám phá; không cần thao tác Inspector để tiến trình chạy tiếp.

Bàn giao gồm:
1. Scene đã lưu cùng script/material/UI cần thiết; không sửa nội dung scene khác ngoài phần dùng chung thực sự cần.
2. Bốn ảnh: toàn map có trần tạm ẩn, camera mở đầu trong phòng sếp, cửa nhìn ra khu nhân viên và góc ngồi bàn Tý.
3. Bảng T01–T22 với Đã đạt / Chưa thử / Còn lỗi; ghi cách thử và giới hạn thực tế. Không trình bày test gọi hàm như test đã bấm phím thật.
4. Tóm tắt file thay đổi, cách Play và các mốc thay asset; cập nhật hướng dẫn blockout theo hành vi mới.
5. Danh sách phần chưa có asset/voice cuối; không lấy thiếu mỹ thuật cuối làm lý do bỏ dở luồng chơi.

Chưa chuyển MainMenu/Build Settings sang map mới trong đợt này. Khi người dùng yêu cầu tích hợp game chính, đó là bước tiếp theo có kiểm tra SceneNames, GameManager, SceneLoader và đường chuyển cảnh riêng.

## 17. Rủi ro cụ thể và cách xử lý

| Rủi ro đã thấy | Cách giải quyết trong triển khai |
| --- | --- |
| Walkthrough và interaction mới cùng bắt E/cursor | Gỡ vai trò runtime cũ khi hệ mới sẵn sàng; một owner duy nhất |
| Script thoại cũ kéo theo task system/QUÂN | Luồng intro riêng theo Tý, chỉ tận dụng phần độc lập phù hợp |
| GameManager coi blockout là MainMenu | Play độc lập trước; không tự thêm manager toàn cục hoặc sửa routing game chính |
| Camera đứng/ngồi bị motor ghi đè | Khóa ghi transform trong transition, đồng bộ pitch/rotation khi bàn giao |
| Cửa mở đẹp nhưng đẩy người hoặc mắc collider | Kiểm tra vùng quét và capsule thật; không tắt collision để qua test |
| Voice cũ khác phụ đề | Đối chiếu trước; fallback text khi chưa khớp |
| Capture chỉnh và lưu scene | Tách lệnh sửa khỏi chụp; cache/restore active state và tài nguyên tạm |
| Model thật thay kích thước/pivot | Giữ slot/anchor, chỉ chỉnh model con; kiểm lại collider và tuyến đi |
| Scope tăng thành cả game | Dừng tại ReadyAtDesk; ghi việc sau vào backlog |

## 18. Prompt bàn giao cho model coding

> Hãy đọc toàn bộ `docs/scene1-office-plan.md` và `docs/office-blockout-guide.md`, kiểm tra scene và code hiện có, rồi triển khai theo các đợt trong mục 14. Làm trên `Assets/_Project/Scenes/Act1_Office_Blockout.unity`, dùng khối mẫu dễ thay asset. Hoàn thiện đoạn trong phòng sếp nghe mắng → tự mở cửa → về bàn Tý → ngồi xuống. Giữ phạm vi tới ReadyAtDesk; chưa làm mini-game, kiệt sức, chuyển cảnh hoặc tích hợp MainMenu. Bỏ qua pack GLB và không gọi dịch vụ tạo asset. Tôn trọng các thay đổi chưa commit. Xác minh qua Play, kiểm tra Console, chụp bốn góc và báo kết quả theo T01–T22; tiếp tục sửa các lỗi trong phạm vi trước khi bàn giao.

