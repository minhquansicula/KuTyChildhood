# Kế hoạch tích hợp SFX cho KuTy

Ngày: 20/10/2026. Trạng thái: **SFX-A đã triển khai và kiểm tra; SFX-B/C chưa triển khai.**

## 0. Kết quả triển khai SFX-A

- Đã thêm `SfxId`, `SfxCatalog`, pool one-shot global, cooldown, chống lặp biến thể và migration adapter cho key chuỗi cũ.
- Đã chuyển toàn bộ call site gameplay hiện có sang typed ID; không còn `PlaySFX("...")` trong `Assets/_Project/Scripts` và lỗi im lặng của `ui_select` đã được loại bỏ.
- `GameManager` phát sự kiện pause; gameplay/ambience/voice chịu `AudioListener.pause`, còn UI source được khôi phục `ignoreListenerPause` lúc runtime để Resume/Back vẫn có tiếng.
- One-shot gameplay được dừng khi đổi scene. Pool/cooldown/repeat state chỉ tồn tại runtime và được dọn đúng lifecycle.
- Scene 1 có feedback cho ghế, Excel invalid/success, máy in, giấy, nước và các beat atmosphere hiện có. Tương tác bị khóa hoặc quá sớm không phát cue thành công.
- Catalog tại `Assets/_Project/Data/Audio/SfxCatalog.asset` có 14 cue. Prefab `KuTyAudio` đã được nối catalog và UI source.
- Manifest tại `Assets/_Project/Audio/SFX/sfx_manifest.json` đánh dấu toàn bộ 14 cue đang dùng generated fallback; đây là nội dung tạm, phải thay/nghe duyệt ở SFX-B.
- Validator nền pass về ID/reference/string call. Smoke Act 1 pass **75 assertions**; Unity Console có 0 error và 0 warning.
- Scene validation: 0 missing script, 0 missing reference, 0 error/warning; còn 146 `Info` về tên object trùng do asset/prefab Office và không phải lỗi SFX.

Lưu ý kỹ thuật: `AudioSource.ignoreListenerPause` không được serialize trong prefab Unity 2022.3. `AudioManager.InitializeSfxSources` luôn gán lại thuộc tính này; smoke runtime là cổng kiểm tra chính xác cho hành vi UI khi pause.

## 1. Phạm vi và quyết định kiến trúc

Project tier: **small-game**. Mục tiêu là làm hành động dễ hiểu hơn, tăng cảm xúc và tạo nhịp chuyển giữa đời thực/ký ức mà không xây một audio framework lớn.

Giữ hai phạm vi rõ ràng:

- `AudioManager`: âm thanh toàn game và không gian 2D như UI, hoàn thành nhiệm vụ, ký ức, shop, BGM và ambient toàn cục.
- Scene-local audio: âm thanh có vị trí hoặc vòng đời gắn với scene như máy in, cây nước, cửa, ghế, bàn phím, bếp và bi lăn. Scene/controller tương ứng sở hữu Start/Stop.
- `DialogueVoicePlayer`: tiếp tục sở hữu Voice/TTS. SFX không phát voice và không thay đổi thời lượng phụ đề.

Không dùng event bus mới, không gọi dịch vụ âm thanh khi runtime và không dùng tên chuỗi tự do. Gameplay gọi trực tiếp dependency đã gán hoặc `AudioManager` bằng ID có kiểu.

## 2. Baseline hiện tại và vấn đề cần sửa

- `AudioManager` có một nguồn BGM, một nguồn SFX và một nguồn ambient; `SoundLibrary` chứa reference cố định.
- `SoundLibrary.asset` vẫn thiếu nhiều clip. Một số clip hiện có là tone placeholder được sinh trong Editor, chưa phù hợp bản phát hành.
- Source hiện gọi 10 tên SFX khác nhau. `ui_select` được gọi ở bốn gameplay script nhưng không tồn tại trong `AudioManager.GetClipByName`, nên hiện chỉ log warning và không phát âm.
- `OfficeAtmosphere` đã có các entry point phù hợp cho bàn phím, máy in, nước, giấy, cốc, thở dài và memory bridge; khi thiếu asset nó sinh clip placeholder runtime.
- Scene 1 có asset thật cho desk slam, keyboard distant và traffic. Chưa có bộ hoàn chỉnh cho cửa, ghế, Excel, printer, paper, water và exhaustion.
- `GameManager.SetPaused` mới đổi `Time.timeScale`, chưa có chính sách pause thống nhất cho audio toàn game.
- Một số âm thanh có thể có hai chủ sở hữu nếu đồng thời dùng intro cũ, `OfficeOpeningSequence` và `OfficeAtmosphere`. Scene Blockout hiện dùng `OfficeIntroController`; `OfficeOpeningSequence` đang để null và phải tiếp tục như vậy.

## 3. Các module đề xuất

1. **SfxCatalog (ScriptableObject):** danh sách cue theo `SfxId`; mỗi cue có 1–4 biến thể clip, volume, pitch range nhỏ, cooldown, kiểu 2D/3D và giới hạn lặp. Đây là dữ liệu tĩnh, không giữ trạng thái runtime.
2. **AudioManager/SfxPlayer:** chọn biến thể, áp volume/pitch, kiểm tra cooldown, phát global 2D one-shot và dừng transient SFX khi chuyển scene. Giữ singleton hiện có thay vì tạo service mới.
3. **Scene SFX sources:** các `AudioSource`/bridge mỏng nằm tại cửa, máy in, cây nước, bếp hoặc bàn bi. Chúng phát 3D one-shot/loop đúng vị trí và bị hủy cùng scene.
4. **Scene audio director:** tiếp tục dùng `OfficeAtmosphere` cho Act 1; Act 2/3 chỉ thêm director khi một scene thực sự cần phối nhiều loop. Không tạo một director chung khổng lồ.
5. **AudioMixer và settings:** Master, Music, Ambience, SFX, UI và Voice. Chỉ thêm ở đợt mix; Voice/TTS route riêng và có thể duck Music/Ambience nhẹ.
6. **Editor validator/manifest:** kiểm tra duplicate ID, clip thiếu, cue chưa dùng, string key cũ, import settings và metadata nguồn/license.

## 4. Scene/bootstrap và quyền sở hữu dữ liệu

- `GameBootstrap` tiếp tục tạo đúng một `KuTyAudio` prefab. Prefab giữ `AudioManager`, global sources, catalog và mixer reference.
- `SfxCatalog.asset` giữ clip/config. Runtime cooldown, biến thể gần nhất và pool state chỉ nằm trong `AudioManager`, không ghi ngược vào asset.
- Scene giữ transform, khoảng cách 3D, loop đang chạy và reference đến object gameplay.
- Gameplay controller phát SFX ngay sau khi state transition thành công. Tương tác sai/đang khóa không được phát cue thành công.
- UI phát từ UI source 2D. UI source có thể `ignoreListenerPause=true` để nút Resume/Back vẫn có tiếng; gameplay SFX, ambience và Voice phải dừng thời gian khi pause.
- Khi load scene, global BGM có thể tiếp tục/crossfade; global one-shot và scene-local loop cũ phải dừng để không rò âm sang scene mới.

## 5. Quy tắc giao tiếp

- Thay `PlaySFX("...")` bằng `PlaySFX(SfxId.X)` theo từng đợt. Giữ overload string tạm thời chỉ làm migration adapter và log warning một lần, sau đó xóa.
- Không dùng `FindObjectOfType` trong thao tác lặp. Scene source được gán Inspector/editor setup; global gọi `AudioManager.Instance` như convention hiện tại.
- Loop luôn có cặp Start/Stop rõ ràng: keyboard, water stream, marble roll, stove/boil. Không dùng `PlayOneShot` cho loop.
- Cue chỉ phát từ owner của hành động. Ví dụ intro sở hữu desk slam của câu thoại; `OfficeAtmosphere` không phát thêm cùng cue từ phase change.
- SFX không tự đổi quest/phase. Controller đổi state trước rồi mới yêu cầu phát âm thanh.
- Thiếu clip là lỗi nội dung, không được làm kẹt gameplay. Validator báo rõ; runtime bỏ qua và chỉ warning một lần mỗi ID.

## 6. Danh mục SFX ưu tiên

### Global/UI

| Cue | Mục đích | Ưu tiên |
| --- | --- | --- |
| UI Confirm / Back | Phản hồi click/nút đóng | P0 |
| UI Invalid | Tương tác sai hoặc submit sai | P0 |
| Task Progress | Một bước hợp lệ | P1 |
| Task Complete | Hoàn thành nhiệm vụ | P0 |
| Memory Collect / All Memories | Nhịp cảm xúc và tiến độ | P0 |
| Purchase / Purchase Fail | Shop | P1 |

### Scene 1 — Office vertical slice

| Cue | Trigger/owner | Không được xảy ra |
| --- | --- | --- |
| Desk slam | Beat thoại trong `OfficeIntroController` | Không phát lại từ `OfficeAtmosphere` |
| Door handle/open | Cửa chuyển Closed → Opening | Spam khi E liên tục hoặc bị chặn |
| Chair roll/settle | Bắt đầu/kết thúc ngồi | Phát khi ngồi bị từ chối |
| Excel click/invalid/success | UI Excel nhận input/submission | Phát phía sau popup/pause |
| Work keyboard loop | Attempt Excel thực sự đang chạy | Tiếp tục khi tutorial, pause, cancel hoặc đóng UI |
| Printer start/feed/finish | Hoàn thành Excel và print progress | Nhiều loop do callback lặp |
| Paper pickup/rustle | Lấy bản in thành công | Phát khi kiểm tra máy in quá sớm |
| Report handoff | Sếp nhận báo cáo | Lặp sau khi đã giao |
| Water pour/cup down/swallow | Water break hợp lệ | Phát khi chưa tới phase |
| Tired sigh/cloth/chair | Bắt đầu exhaustion | Chồng Voice hoặc tiếp tục sau scene load |
| Memory bridge | Blackout gần chuyển Act 2 | Lặp callback load scene |

### Scene 2 — Home memory

- Vòi nước mở/đóng và water loop; chén chạm nhẹ, chà/rửa, dish clean.
- Bếp: bật lửa, xèo nhẹ, sôi, hoàn thành công đoạn; không phát cùng một cue mỗi frame của hold/mash.
- Cửa/giường/đồ vật gia đình và memory collect.
- Ưu tiên âm đời thường, ấm, gần; tránh “gamey ding” cho mọi bước nhỏ.

### Scene 3 — Outside memory

- Marble shoot, impact theo vật liệu, roll loop có ngưỡng vận tốc và stop khi ngủ.
- Gió/chim là ambience, không đi qua SFX one-shot.
- Shop và memory completion dùng global cues.
- Footstep chỉ làm sau khi ba vòng gameplay chính đã có feedback; cần ít nhất biến thể và kiểm tra tốc độ/bề mặt để tránh lặp máy.

## 7. Asset, naming và import

- Cấu trúc đề xuất: `Audio/SFX/{UI,Global,Office,Home,Village,Marbles}`.
- Tên file: `sfx_<scene>_<action>_<variant>.wav`, ví dụ `sfx_office_printer_feed_01.wav`.
- Manifest ghi `SfxId`, file, nguồn, tác giả/license, ngày lấy/tạo, chỉnh sửa, cue owner và trạng thái duyệt.
- Không đưa file không rõ license vào build. Giữ master ngoài build nếu dung lượng lớn; Assets chỉ chứa bản dùng trong game.
- One-shot ngắn ưu tiên mono khi phát 3D; UI có thể stereo. Loop phải cắt liền mạch.
- Không áp chung một compression setting: one-shot ngắn thường Decompress On Load; loop dài cân nhắc Compressed In Memory/Streaming sau khi profile.
- Các tone/runtime placeholder phải có marker rõ và validator chặn release build nếu còn được gán cho cue P0.

## 8. Mix, TTS và cảm xúc

- Thứ tự dễ nghe: Voice/TTS > cue nhiệm vụ quan trọng > tương tác vật thể > ambience/BGM.
- Mixer group: Master, Music, Ambience, SFX, UI, Voice. Các slider lưu bằng PlayerPrefs ở đợt mix.
- Khi có Voice, dùng snapshot duck Music/Ambience nhẹ; không duck UI và không tắt hoàn toàn môi trường.
- Desk slam được phép nổi bật nhưng không cắt/clip TTS. Printer/keyboard đặt thấp hơn Voice rõ rệt.
- Sau khi giao báo cáo, giảm mật độ keyboard/office noise; water/cup và sigh tạo khoảng thở trước exhaustion.
- Không dùng SFX “thành công” vui vẻ cho mọi hành động; hoàn thành công việc Scene 1 nên tiết chế, còn memory collect có thể ấm hơn.

## 9. Ngoại lệ và lifecycle bắt buộc

- Missing/null clip, empty variants, duplicate ID, volume/pitch sai khoảng.
- Gọi cùng cue nhiều lần trong một frame, E spam, callback completion lặp và mở/đóng UI liên tục.
- Pause giữa one-shot/loop, resume nhiều lần, mất focus và `Time.timeScale == 0`.
- Scene unload khi printer/water/marble/voice đang phát; không còn audio “ma” sau load.
- `AudioManager` hoặc catalog chưa tồn tại khi Play trực tiếp scene; gameplay vẫn chạy và không NullReference.
- Hai singleton do mở từ MainMenu rồi chuyển scene; chỉ một prefab sống.
- Mute SFX không làm thay đổi timing/gameplay; Voice mute thuộc setting khác.
- Pitch random không áp cho cue kể chuyện/nhận diện quan trọng; không làm thay đổi độ dài logic của nhiệm vụ.
- Một loop chỉ có một owner/token; Start lặp không tạo thêm source, Stop lặp là idempotent.

## 10. Các đợt triển khai

### SFX-A — Nền kỹ thuật và Scene 1 P0

1. Tạo `SfxId`, `SfxCatalog`, migration adapter và validator.
2. Sửa key `ui_select`; thống kê toàn bộ call site và chuyển các cue P0 sang enum.
3. Chuẩn hóa pause/scene unload; UI source được phép phát trong menu pause.
4. Lắp cue P0 Scene 1: cửa, ghế, Excel invalid/success, printer, giấy, nước, exhaustion và memory bridge.
5. Giữ placeholder fallback nhưng manifest đánh dấu; chưa coi audio hoàn tất khi chưa có asset nghe duyệt.
6. Mở rộng smoke hiện tại để kiểm tra trigger đúng một lần, early interaction không phát, loop dừng và scene transition sạch.

**Cổng hoàn thành:** compile sạch; validator không duplicate/broken reference; smoke gameplay không regression; mọi hành động chính Scene 1 có feedback hoặc fallback; không warning `ui_select`; pause/unload không rò âm.

### SFX-B — Asset thật và mix với TTS

1. Thu thập/tạo 2–4 biến thể cho cue lặp nhiều; ghi license/source.
2. Thêm AudioMixer và route Music/Ambience/SFX/UI/Voice.
3. Chọn volume/pitch/cooldown bằng nghe thử cùng TTS và ambience.
4. Thay placeholder P0, validator chặn release nếu placeholder còn gán.
5. Nghe thủ công bằng tai nghe và loa laptop; kiểm tra không clip, không át lời, không mỏi tai.

**Cổng hoàn thành:** bốn câu TTS rõ trên nền Office; không cue P0 dùng placeholder; mixer setting lưu/khôi phục; build Windows offline phát đúng.

### SFX-C — Scene 2/3 và variation

1. Home: nước/chén/bếp/công đoạn/memory.
2. Village: marble shoot/hit/roll, shop, memory; ambience tách riêng.
3. Chỉ thêm footstep/surface variation sau khi core task cue đạt.
4. Profile Audio Profiler và chỉnh compression/load type theo clip thực tế.

**Cổng hoàn thành:** mỗi core loop có start/progress/complete feedback, không spam, không rò loop khi pause/load và tổng voice đồng thời nằm trong ngân sách.

## 11. Kiểm thử và ngân sách ban đầu

- Edit/validation: catalog ID duy nhất, cue P0 có clip, không còn string key lạ, reference/mixer group hợp lệ.
- Play smoke: missing clip, cooldown, spam, pause/resume, mute, scene unload và duplicate bootstrap.
- Gameplay smoke: Act 1 intro → Excel → print → handoff → water → exhaustion → Act 2; Act 2 chores; marble sequence.
- Listening review: kiểm tra riêng 2D UI, 3D position, Voice clarity, hai mức loa và tai nghe.
- Ngân sách khởi điểm: tối đa khoảng 8 one-shot nghe đồng thời, 4 loop scene và một Voice; chỉ tăng sau khi Audio Profiler chứng minh cần.
- Không tạo clip/AudioSource trong `Update`. Pool/source được dựng lúc init; hot path chỉ lookup cue, kiểm cooldown và phát.

## 12. Làm ngay / chưa làm

### Làm ngay

- Sửa `ui_select` và loại bỏ string key dần.
- Tách rõ global 2D, scene-local 3D và Voice.
- Scene 1 P0 + pause/unload + validator/smoke.
- Manifest/license và quy ước tên/import.

### Chưa làm

- Middleware ngoài, Addressables cho audio, procedural footstep phức tạp, occlusion/reverb zone toàn game.
- Dynamic music nhiều layer, lip sync, runtime download/generation hoặc backend audio.
- Pool lớn/generic event bus nếu profiler và nội dung chưa yêu cầu.

Thứ tự đề xuất: **SFX-A foundation → Scene 1 vertical slice → SFX-B asset/mix cùng TTS → Scene 2/3 → profiler/polish**.
