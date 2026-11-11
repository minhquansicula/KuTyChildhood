# Con quay gỗ — kế hoạch triển khai đoạn mở đầu Scene 2

Tài liệu thiết kế và bàn giao cho nhóm làm gameplay, art và âm thanh. Các thông số là giá trị khởi điểm để chỉnh sau khi chơi thử. Dự án đang dùng Unity 2022.3.62f3 và URP.

## 1. Trải nghiệm người chơi

Sau cutscene văn phòng, Tý ngủ và thoáng thấy đồng quê trong mơ. Cảnh đồng quê mang phong cách 3D của game, có đường làng, đồng lúa và hàng tre đung đưa. Tý tỉnh dậy trong căn nhà cũ như hiện tại. Khi còn nằm trên giường, Tý liếc sang bàn cạnh cửa sổ và thấy con quay gỗ có dây. Người chơi bấm **E** để xuống giường, tự tới bàn, cầm con quay lên rồi kéo dây. Con quay chạy khỏi mép bàn và lăn qua cửa. Tý đi theo tiếng động tới bếp, nơi mẹ nhặt con quay rồi gọi Tý phụ nấu cơm.

### Diễn tiến bản đầu

0. **Ngủ ở cuối Scene 1.** Sau cutscene văn phòng, giữ khung tối khoảng nửa giây. Tiếng phòng nhỏ dần, tiếng gió qua lúa và chim vào trước hình. Chiếu một shot đồng quê 3D khoảng **5–7 giây**. Cắt về Scene 2 theo tiếng chim để không khí ngoài trời nối liền tới phòng ngủ.
1. **Tỉnh dậy và liếc sang bàn.** Giữ đoạn mở mắt, dụi mắt và ổn định góc nhìn hiện có. Ngay sau khi mắt rõ, camera liếc sang bàn khoảng **1–1,4 giây**. Bắt đầu bằng một tiếng dây hoặc tiếng cửa sổ khẽ động, xoay chậm về con quay, giữ khoảng nửa giây rồi ổn định để hiện **[E] Xuống giường**. Từ giường phải thấy được cả thân con quay và đầu dây trên nền bàn.
2. **Xuống giường và cầm lên chơi.** Tý xuống giường theo luồng hiện tại rồi tự bước đến bàn. Chỉ khi đứng gần và nhìn vào con quay mới hiện **[E] Cầm con quay**. Bấm E đưa con quay lên gần tầm mắt một nhịp ngắn để thấy vân gỗ và dây, rồi hạ mũi quay về điểm phóng trên bàn. Hiện **Giữ chuột trái, kéo dây rồi thả**. Esc trả vật về bàn và quyền điều khiển về cho người chơi.
3. **Kéo và thả.** Cầm đầu dây, kéo về phía mình. Các vòng dây trên thân tháo dần. Khi thả, con quay đáp mũi xuống, lắc nhẹ rồi xoay ổn định. Kéo mạnh cho tiếng giật rõ hơn và con quay nhanh hơn. Cú kéo quá ngắn chỉ trả dây về vị trí ban đầu, có thể thử ngay.
4. **Đuổi theo.** Cú phóng hợp lệ đầu tiên luôn dẫn tới tình huống con quay đi khỏi bàn. Lực kéo quyết định nhịp quay và độ lắc, còn đường ra cửa được căn theo cảnh để kể chuyện ổn định. Camera trả điều khiển khi con quay tới mép. Tiếng rơi và tiếng lăn cho biết hướng đi. Nếu Tý đứng yên, mẹ gọi nhẹ sau vài giây và đánh dấu chỗ đến bằng ánh sáng hoặc âm thanh.
5. **Gặp mẹ.** Mẹ ở cạnh chạn hoặc bếp, nhặt con quay rồi nói *“Mẹ kêu con xuống phụ bếp. Ai kêu con quay xuống trước?”* Sau một nhịp, mẹ gọi Tý phụ nhóm bếp. Đây là lúc hiện checklist hiện có và mở nhiệm vụ đầu tiên.
6. **Hồi đáp sau bữa cơm.** Khi đã dọn đủ bốn món, mẹ trả con quay và chỉ vết mẻ nhỏ. Nếu có thời gian, dùng cùng model với vết mẻ đặt cạnh bàn phím trong cảnh văn phòng cuối. Cảnh mở đầu và kết thúc nhận ra được nhau qua cùng màu sơn và vết mẻ.

Mục tiêu nhịp chơi là **15–25 giây từ lúc chạm vào đồ chơi tới lúc mẹ gọi**, tùy thời gian người chơi kéo và chạy theo. Bản đầu chỉ cần một lần phóng hợp lệ. Sau khi mẹ trả đồ chơi, có thể cho quay tự do ở bản sau.

## 2. MISS và phần tái sử dụng

- [Demo MISS](https://catheart97.github.io/miss/) để đối chiếu dáng con quay xoay, nghiêng và tiếp xúc sàn. [Bài báo](https://doi.org/10.1002/cav.70176) mô tả phương pháp mô phỏng.
- [Thư mục mô phỏng](https://github.com/catheart97/miss/tree/main/src/components/miss) chứa `simulation.tsx`, `utility.ts` và thư mục `spinning-toys`. `simulation.tsx` là phần dựng cảnh và điều khiển demo React/Babylon.js. Dùng nó để xem đầu vào và cách cập nhật demo.
- [utility.ts](https://github.com/catheart97/miss/blob/main/src/components/miss/utility.ts) chứa trạng thái mô phỏng, các phép toán vector/ma trận, chuyển quaternion và bộ tích phân. Phần đáng chuyển trước là cách biểu diễn vị trí, hướng và vận tốc góc, cùng phép cập nhật theo bước thời gian cố định. Nếu cần port, bóc đúng các phép toán phục vụ một con quay, kiểm thử kết quả với một bộ trạng thái đầu vào giống demo.
- [ellipsoid.ts](https://github.com/catheart97/miss/blob/main/src/components/miss/spinning-toys/ellipsoid.ts) là ví dụ để đọc tiếp về thân quay đối xứng. Dùng làm điểm đối chiếu cho chuyển động nghiêng và tiếp xúc, rồi dựng hình con quay gỗ riêng. `utility.ts` có phần `vern98` ghi nguồn từ thư viện khác. Dùng bộ tích phân Unity hoặc bộ tích phân đơn giản trong prototype và kiểm tra điều khoản thư viện đó nếu sau này cần lấy đoạn này.
- [Giấy phép MIT](https://raw.githubusercontent.com/catheart97/miss/main/LICENSE) ghi bản quyền của Ronja Schnur. Nếu lấy hoặc dịch phần mã đáng kể, ghi lại commit nguồn, đường dẫn và các hàm đã dùng. Kèm nguyên văn LICENSE và thông báo bản quyền trong thư mục thông báo bên thứ ba của repo và gói phân phối có phần mã đó. Kiểm tra nguồn riêng nếu muốn dùng texture, model hoặc footage bên ngoài.

### Cách chọn mức port

**Bản chơi thử** dùng MISS làm mẫu chuyển động, với một thân quay trong Unity. Đo góc nghiêng, thời gian xoay, khoảng trôi và nhịp chạm mép từ bản prototype. Sau khi kéo dây đã thú vị, port phần toán học cụ thể nếu Rigidbody hiện tại thường xuyên giật, ngã sớm hoặc khó kiểm soát điểm tiếp xúc. Giữ giao diện mô phỏng nhỏ với hai thao tác `Launch` và `Reset`, để đổi cách mô phỏng mà phần nhiệm vụ vẫn gọi cùng một chỗ. Đây là điểm dừng đủ cho một minigame ngắn trong Scene 2.

## 3. Gameplay và kỹ thuật Unity

### Thành phần đề xuất

- `Scene2ToyIntro` quản lý tiến độ đoạn mở đầu, kích hoạt mẹ và bàn giao sang checklist. Kết nối các object bằng tham chiếu Inspector.
- `SpinningTopInteractable` dùng `InteractableBase` và luồng `PlayerInteraction` hiện có. Chỉ hiện prompt khi Tý đã đứng dậy, đồ chơi sẵn sàng và không có phiên tương tác khác giữ input.
- `SpinningTopMotion` nhận lực phóng, cập nhật con quay, báo lúc tới mép hoặc tới điểm dừng. Quản lý duy nhất đối tượng thực sự chuyển động.
- `SpinningTopPresentation` cập nhật dây, âm thanh, prompt, camera và pose khi kéo. Nhận dữ liệu từ motion và controller.

### Trạng thái và điều khiển

- **Sẵn sàng → Đang giữ dây → Đang quay trên bàn → Đã qua mép → Đến bếp → Đã bàn giao.** Lần thả hụt trở về Sẵn sàng. Từ Đang giữ dây, Esc đóng phiên và cho thử lại. Sau khi đã phóng, Esc xử lý theo pause thông thường sau khi quyền điều khiển đã trả về.
- Đo quãng kéo dọc theo hướng dây trên màn hình, chuẩn hóa theo chiều cao viewport. Dùng ngưỡng khởi điểm khoảng **8% chiều cao màn hình**, lực đạt tối đa ở khoảng **25%**. Độ dài là đầu vào chính, tốc độ kéo chỉ tinh chỉnh. Giữ giới hạn để chuột nhạy, trackpad và fps thấp vẫn chơi được.
- Cho một cách điều khiển tương đương bằng phím, chẳng hạn giữ Space rồi thả khi phiên đã mở. Chặn input mở lại ngay trong frame phóng. Khi mất focus hoặc pause giữa cú kéo, đưa dây về trạng thái an toàn và cho thử lại.
- Dùng camera hiện có và phiên giữ input theo mẫu [HouseholdWorkSession.cs](../Assets/_Project/Scripts/Systems/HouseholdWorkSession.cs). Khi kết thúc, hủy, đổi cảnh hoặc object bị disable, trả camera, trạng thái chuột, quyền di chuyển và tương tác đúng một lần. Nếu phiên hiện tại cần con trỏ ẩn để kéo, kiểm tra tham số `GameManager.AcquireInput` thay vì chồng thêm một bộ khóa.

### Cảm giác quay và đường chạy

- Prefab có trục quay **Y**, mũi quay và trọng tâm được đánh dấu. Rigidbody dùng collider đơn giản theo hình thân và mũi, ma sát vừa và xoay nhanh có giới hạn. Cập nhật lực trong FixedUpdate, bật nội suy hình ảnh và thử phát hiện va chạm liên tục ở tốc độ cao. Giữ thân ở tỉ lệ 1 trong Unity, tune khối lượng và lực sau khi xác nhận kích thước thực.
- Khi phát triển mô phỏng từ MISS, đối chiếu thứ tự nhân ma trận/quaternion, quy ước trục, hệ tọa độ và đơn vị radian. Dùng bước cố định, giới hạn số bước con mỗi frame, chuẩn hóa quaternion và kiểm tra giá trị hữu hạn. Mỗi giai đoạn chỉ có một bộ điều khiển transform. Khi chuyển quyền từ mô phỏng sang Rigidbody hoặc đường dẫn, truyền pose và vận tốc tại đúng frame chuyển.
- Cú phóng nên có **0,2–0,4 giây lắc lúc đáp**, **1–2 giây xoay rõ**, rồi trôi ra mép. Dùng hai điểm trên bàn `LaunchPoint` và `TableEdgePoint`. Khi qua mép, nội suy qua các điểm `FloorImpactPoint`, `DoorwayPoint`, `KitchenRestPoint`. Cho quỹ đạo cong nhẹ, thêm một nhịp nảy khi chạm sàn. Giữ xoay tiếp khi di chuyển để người chơi nhận ra cùng một vật.
- Thiết kế đường đi không cắt qua chân bàn, chân mẹ hay cánh cửa. Nếu vật ra khỏi vùng chạy hoặc quá thời gian, đưa nó về điểm dừng ngoài tầm nhìn và phát tiếng chạm cuối. Khoảnh khắc mẹ gọi dùng tín hiệu kết thúc hoặc đường dự phòng, để không kẹt nếu mất audio hoặc bỏ lỡ va chạm.
- Khi mẹ cầm con quay, dùng cùng mesh, tắt chuyển động và gắn vào một điểm tay. Nếu animation tay chưa sẵn, để con quay dừng trên mặt chạn hoặc mẹ nhặt ngoài tầm nhìn, đồng bộ bằng tiếng chạm và lời thoại.

### Nối vào dự án hiện tại

- [Act1CutsceneAct2Controller.cs](../Assets/_Project/Scripts/Systems/Act1CutsceneAct2Controller.cs) đang phát clip chuyển cảnh rồi gọi luồng ngủ để vào Scene 2. Nối shot đồng quê sau clip hiện có, trước lần hoàn tất chuyển cảnh duy nhất. Có thể ghép trực tiếp vào cuối file video cầu nối để giữ cùng một luồng phát và bỏ qua. Giữ phương án đi thẳng vào Scene 2 nếu video thiếu hoặc phát lỗi.
- [BedWakeUpController.cs](../Assets/_Project/Scripts/Systems/BedWakeUpController.cs) đã có một khoảng sau khi tháo hiệu ứng dụi mắt và trước khi bật prompt xuống giường. Chèn nhịp liếc vào khoảng đó, dùng một Transform `ToyGlanceTarget` cạnh con quay làm điểm nhìn. Camera tỉnh dậy sở hữu chuyển động đến hết nhịp liếc. Khi bàn giao, đồng bộ pitch/yaw với controller để tránh giật ở lần rê chuột đầu tiên. Nếu không có điểm nhìn, hiển thị prompt xuống giường như hiện tại. Với lượt mở đầu mới, đợi `Scene2ToyIntro` bàn giao rồi mới gọi checklist hiện có qua `KitchenFlowManager`. Lượt chơi tiếp vẫn thấy và chơi được con quay, đồng thời mở checklist ngay sau khi xuống giường.
- Dùng một cờ mở đầu hoàn tất theo lượt chơi, tách khỏi thứ tự và phần thưởng công việc nhà. Khi đọc save cũ đã có tiến độ việc nhà, con quay là hoạt động tùy chọn và checklist mở ngay. Reset chương mới thì xóa cờ mở đầu. Lưu mốc sau khi bàn giao, không lưu vật lý giữa cú quay. Thoát giữa chừng có thể bắt đầu lại từ bàn.
- Tiếp tục chuỗi nhóm bếp, nấu cơm, dọn mâm và các bước sau qua [HouseholdChoreFlow.cs](../Assets/_Project/Scripts/Systems/HouseholdChoreFlow.cs). Tận dụng logic hiện có trong [Scene2KitchenHandsOn.cs](../Assets/_Project/Scripts/Systems/Scene2KitchenHandsOn.cs), [Scene2FoodCarry.cs](../Assets/_Project/Scripts/Interaction/Scene2FoodCarry.cs) và hệ thoại mẹ. Cảnh mẹ trả đồ chơi, nếu làm, nghe sự kiện hoàn tất `SetTable` một lần.
- Đặt prefab và các mốc trong `Act2_MemoryWorld_Home`. Đặt bàn cạnh cửa sổ hoặc tận dụng bàn sẵn có nếu đường từ bàn tới cửa đủ sạch. Mốc chạn và đường đi nên chỉnh được trong Scene View mà không sửa C#.

## 4. Danh sách asset

### Bắt buộc cho bản chơi thử

- **Con quay gỗ** — một model FBX cao khoảng **6–8 cm**, thân rộng **4–6 cm**. Thân tiện tròn, đầu quấn dây, mũi nhọn bo nhẹ, các vòng sơn màu phai và vân gỗ. Cho một nét sơn lệch để nhìn thấy tốc độ quay. Tách mesh trực quan khỏi collider. Đặt pivot chính trên trục, marker `Tip` đúng mũi và marker `StringExit` ở chỗ dây rời thân. Dựng một vết mẻ nhỏ bằng mesh hoặc material variant nếu dùng đoạn hồi đáp.
- **Dây con quay** — dây cotton ngà dài khoảng **35–50 cm**, đường kính khoảng **2–3 mm**. Dựng hai phần nhìn thấy được là vòng dây quấn và đoạn dây tự do có nút nhỏ. Dùng curve/tube bake cho vòng quấn. Đoạn tự do dùng LineRenderer hoặc mesh tube ít điểm từ `StringExit` đến `PullGrip`. Giảm số vòng hiển thị theo phần trăm kéo. Sau khi thả, dây chùng xuống bàn và ở lại, tạo cảm giác con quay đã thoát khỏi dây.
- **Bàn và đường chạy** — ưu tiên bàn gỗ hiện có. Nếu góc bàn không dùng được, dựng một bàn con có cạnh mòn và mặt đủ rộng để đọc rõ vật nhỏ. Cần collider mặt bàn riêng, mốc cạnh bàn, mốc nền và điểm dừng nhìn thấy được dưới chạn. Một vệt ánh sáng từ cửa sổ giúp người chơi nhận ra đồ chơi trong vài giây đầu.
- **Vật liệu** — một material URP Lit cho gỗ và sơn, một material sợi dây. Base Color, Normal nhẹ và độ nhám rõ ở góc nhìn gần. Texture **1K** là điểm bắt đầu hợp lý. Test con quay dưới cả vùng nắng và vùng tối trong Scene 2 để tránh mất chi tiết hoặc lóe sáng.
- **Âm thanh** — tiếng dây kéo rít nhẹ, giật dây, mũi gỗ chạm bàn, vòng lặp quay, chạm mép, rơi xuống nền và va nhẹ vào chạn. Có ít nhất hai biến thể cho cú chạm để lần chơi lại bớt lặp. Tiếng quay đổi pitch/volume theo tốc độ. Gắn nguồn âm vào đồ chơi và một nguồn ambient ngoài cửa.
- **Thoại** — thu hai câu đầu của mẹ là câu trêu khi nhặt đồ chơi và câu gọi phụ nấu cơm. Chuẩn bị subtitle đồng bộ. Có thể kiểm thử bằng subtitle trước khi có voice. Ưu tiên một animation cúi hoặc đưa tay ngắn nếu rig mẹ đã hỗ trợ.
- **UI** — prompt E hiện có, hướng dẫn kéo và một chỉ báo lực ngắn khi giữ chuột. Ẩn chỉ báo sau khi thả. Có phản hồi nhẹ khi kéo hụt. Dùng bố cục và font sẵn của Scene 2.

### Cảnh đồng quê trong giấc ngủ

- Dựng một shot bằng asset 3D cùng phong cách với nhà của Tý. Dùng bố cục ba lớp. Tiền cảnh có vài lá tre và cọc rào, trung cảnh có lối đất cong cạnh ruộng, hậu cảnh có mái nhà thấp, hàng cây và trời rộng. Lúa và tre chuyển động nhẹ để cảnh có gió. Một chiếc diều nhỏ ở xa là điểm nhấn tùy chọn, với một chuyển động đủ đọc trong shot ngắn.
- Máy quay ngang tầm trẻ em, tiến nhẹ về phía lối đất. Đặt ánh sáng ấm, màu gỗ và màu lá cùng tông Scene 2. Dùng hình khối rõ, texture vừa phải và tiết chế hiệu ứng ống kính để shot trông như hình ảnh của game.
- Bàn giao tối thiểu gồm mảng lúa, cụm tre, đoạn rào, lối đất, một mái nhà xa, sky và hai lớp âm thanh là gió cùng chim. Có thể dựng trong một scene render riêng rồi xuất clip để ghép vào cầu nối Scene 1 → Scene 2. Người chơi xem shot trong lúc Tý ngủ. Scene 3 sân cũ được thay bằng clip này.
- Dùng [cảnh đồng quê Việt Nam trên Pexels](https://www.pexels.com/video/serene-vietnamese-countryside-with-rice-fields-35695628/) để tham khảo cách xếp ruộng, mái nhà và đường làng. Bản xuất chính dùng hình ảnh 3D của game. Chuẩn bị clip trước, thử bỏ qua và thử trường hợp phát lỗi để luôn vào được cảnh tỉnh dậy.

### Phần tăng chất lượng sau khi gameplay ổn

- Một pose tay nắm đầu dây và một animation giật dây ngắn. Bản đầu có thể chỉ dùng chuyển động sợi dây và âm thanh, giúp tránh phải chờ rig tay.
- Pose mẹ nhặt con quay, cầm lên xem rồi đặt cạnh chỗ ngồi của Tý. Nếu làm cảnh văn phòng cuối, dùng lại variant vết mẻ.

### Quy cách bàn giao asset

- Bàn giao file Blender gốc, FBX đã apply rotation/scale, texture tách kênh và ảnh chụp kiểm tra kích thước trong Unity. Kiểm tra trục Y khi import, vật liệu, UV, pivot và điểm chạm mũi.
- Đặt art mới trong `Assets/_Project/Art/Scene2`, prefab trong thư mục prefab Scene 2. Giữ file `.meta` khi di chuyển. Ghi nguồn, giấy phép và người tạo cho bất kỳ asset tải ngoài nào.
- Bàn giao voice/SFX bản gốc và bản import Unity. Chuẩn hóa tên theo hành động, giữ tiếng quay và dây có thể nghe trên nền ambient. Để người chơi vẫn hiểu hướng đi khi tắt tiếng bằng ánh sáng, chuyển động và subtitle.

## 5. Thứ tự triển khai và kiểm thử

1. **Gameplay thô.** Dùng con quay tạm, dây LineRenderer và bàn hiện có. Hoàn thành thao tác E, kéo, thả, quay, chạy đến bếp và mở checklist. Chạy thử từ đầu Scene 2 trước khi làm asset đẹp.
2. **Cảm giác vật lý.** So prototype với demo MISS. Tune lực, ma sát, độ nghiêng, tiếng va và điểm bàn giao camera. Port phần solver đã chọn nếu thật sự cần, ghi nguồn và test riêng một trường hợp đầu vào cố định.
3. **Art và âm thanh.** Thay model con quay và dây, gắn mốc vào prefab, chốt đường chạy, thu thoại và thêm SFX. Chỉnh ánh sáng từ phòng ngủ qua cửa tới bếp. Quay một clip nội bộ của đoạn 20 giây để cả nhóm duyệt.
4. **Tích hợp câu chuyện.** Chạy liền từ cutscene văn phòng qua shot đồng quê, đoạn tỉnh dậy, liếc thấy con quay, xuống giường và chơi đến lúc mẹ gọi. Thêm đoạn mẹ trả đồ chơi sau khi luồng chính ổn.
5. **Kiểm thử bàn giao.** Thử save cũ, lượt mới, thoát giữa cú kéo, pause/resume, mất focus, bấm E liên tục, 30/60 fps và hai tỷ lệ màn hình. Kiểm tra build Windows với chuột và trackpad, phụ đề khi tắt tiếng, quyền điều khiển sau khi thoát, và nhiệm vụ nấu cơm tiếp tục đúng thứ tự.

### Tiêu chí đạt cho bản đưa thầy chơi thử

- Người chơi tìm được món đồ chơi và hiểu kéo dây trong lần thử đầu mà không cần người đứng bên chỉ.
- Con quay có ba khoảnh khắc đọc được bằng mắt và tai là dây bung, mũi đứng trục, chạm sàn. Tý có quyền di chuyển lại trước khi cần đuổi theo.
- Mẹ gọi đúng một lần sau khi đồ chơi tới bếp hoặc chạy nhánh dự phòng. Việc nhóm bếp và dọn cơm vẫn hoàn thành được khi chơi tiếp.
- Đoạn đồng quê phát đúng sau cutscene văn phòng và trước khi Tý mở mắt. Nếu bỏ qua clip, Tý vẫn tỉnh dậy đúng luồng. Nhịp liếc cho thấy con quay trước prompt xuống giường, rồi người chơi tự tới bàn để cầm lên.
- Sau khi chơi, hỏi người thử **“Bạn nhớ cảnh nào với mẹ?”** và **“Lúc nào bạn thấy mình đã rời khỏi không khí văn phòng?”**. Ghi câu trả lời trước khi quyết định thêm tính năng.
