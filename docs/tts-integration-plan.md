# Kế hoạch tích hợp TTS vào KuTy

Ngày cập nhật: 21/10/2026. Trạng thái: đã nhập và gán đủ 4 clip TTS tiếng Việt do người dùng cung cấp; còn bước người dùng nghe duyệt giọng/mix để nghiệm thu toàn bộ đợt A.

## Clip người dùng cung cấp — 21/10/2026

- Đã nhập ba câu sếp và một câu Tý từ Downloads, giữ nguyên WAV nguồn. Mỗi file mono PCM 16-bit, 22.050 Hz; không có mẫu clipping. Thời lượng lần lượt: sếp 001 = 8,404 s; sếp 002 = 7,113 s; sếp 003 = 10,734 s; Tý 001 = 2,168 s.
- Phụ đề và `spokenText` của `act1_intro_ty_001`: “Dạ dạ sếp, em kiểm tra lại ngay ạ”. Phần tạo dữ liệu pilot cũng được cập nhật để lần cài lại không trả về câu cũ.
- Import Unity: PCM, Decompress On Load, preload, mono, giữ sample rate nguồn. Bốn clip ngắn có tổng WAV khoảng 659 KiB; không cần streaming hoặc nén mất chất lượng thêm.
- Manifest đã ghi clip được gán và hash văn bản mới. Nhà cung cấp không được suy đoán từ tên file; ghi nhận là audio do người dùng cung cấp.
- Lệnh `KuTy/TTS/Assign supplied Act1 voice clips` chỉ cập nhật asset thoại và manifest, không lưu scene đang có chỉnh sửa chưa lưu. Runtime lấy thời lượng từng clip cộng post-delay để chuyển phụ đề.

Các mục dưới ghi lại nền triển khai và kết quả kiểm tra trước khi nhập giọng thật.

## Kết quả triển khai đợt A

- Đã thêm `DialogueSequence` với stable ID, spoken text, fallback/post-delay, silent beat, text hash ổn định và validator trùng ID/dữ liệu sai.
- Đã thêm `DialogueVoicePlayer` một kênh 2D, không loop/play-on-awake; hỗ trợ mute, volume, pause/resume, stop và playback token để vô hiệu lượt cũ.
- `OfficeIntroController` ưu tiên asset mới, vẫn fallback sang `Line[]` cũ; phụ đề/timing không kẹt khi thiếu clip, mute không làm ngắn câu, skip/handoff/disable đều dừng voice.
- Đã tạo `Assets/_Project/Data/Dialogue/Act1OfficeIntro.asset`: 6 beat gồm 4 câu có lời và 2 nhịp im lặng. Scene Act1 đã được gán asset/player.
- Manifest ở `Assets/_Project/Audio/Voice/vi/Act1/act1_tts_manifest.json` ghi text hash, chỉ dẫn cảm xúc, đường dẫn clip dự kiến và trạng thái thiếu clip. Chạy lại installer giữ clip đã gán theo stable ID.
- Máy phát triển hiện không có voice tiếng Việt và chưa chọn provider/API, nên không sinh clip giả bằng giọng Anh/Nhật. Bốn clip được đánh dấu `missing`; gameplay dùng fallback duration an toàn.
- Unity compile: 0 lỗi. Scene validation: 0 issue, 0 missing script, 0 missing reference. Smoke toàn tuyến: PASS 65 assertions, gồm intro tự chạy hết không skip, thiếu clip, mute, pause/resume, replace/stop token cũ và toàn bộ Excel → in → giao → nước → Act 2.

## Quyết định và phạm vi

Project tier: small-game. Bản đầu tạo giọng TTS trong quá trình làm nội dung, import AudioClip rồi đóng gói cùng game. Khi chơi không gọi dịch vụ TTS. Chọn nhà cung cấp sau khi nghe thử tiếng Việt; kiến trúc runtime không phụ thuộc nhà cung cấp.

Pilot: 3–5 câu có lời trong intro Scene 1, hai vai Sếp và Tý. Nhịp im lặng giữ nguyên dưới dạng beat có thời lượng. Chưa đọc toàn bộ HUD/hint tự động; giọng hướng dẫn là tùy chọn mở rộng sau pilot.

## Kết quả kiểm tra code ban đầu

- OfficeIntroController.Line đã có speaker/text/duration/voice/slam. BeginLine phát clip cùng phụ đề; Update dùng clip.length khi có clip. SetPaused pause/unpause các AudioSource con.
- Scene Act1_Office_Blockout hiện chưa gán voice cho các câu intro. Một câu Sếp trong scene còn nội dung cũ và được thay chuỗi khi chạy. Phải chuẩn hóa trước khi tạo audio.
- OfficeOpeningSequence là luồng mở đầu khác; setup Blockout đặt openingSequence = null. Pilot tích hợp OfficeIntroController, tránh phát hai luồng mở đầu đồng thời. Xác minh lại cấu hình scene khi triển khai.
- AudioManager hiện quản lý BGM/SFX/ambient, chưa có kênh Voice. DialogueUI có typewriter/tiếp tục và input ownership riêng; tích hợp sau khi pilot đạt.

## Các module và quyền sở hữu dữ liệu

1. DialogueSequence (ScriptableObject): danh sách câu theo thứ tự. Mỗi câu có id ổn định, speakerId, text, voiceClip, fallbackDuration, postDelay và cờ slam/beat nếu cần. Dữ liệu tĩnh, không giữ index hoặc trạng thái phát.
2. DialogueVoicePlayer (MonoBehaviour tại scene): sở hữu một AudioSource Voice; Play/Pause/Resume/Stop; chặn clip chồng nhau. Không điều khiển tiến độ nhiệm vụ hay tự khóa input.
3. OfficeIntroController (adapter): giữ thứ tự câu, phụ đề, input và handoff hiện có; gọi VoicePlayer trực tiếp qua reference Inspector. Asset mới là tùy chọn trong giai đoạn migration; scene chưa chuyển đổi vẫn dùng Line[] cũ.
4. VoiceSettings: lưu âm lượng Voice và bật/tắt giọng; áp dụng vào player qua reference/bootstrap hiện có. Runtime state nằm ở component, chỉ preference lưu PlayerPrefs. Subtitles vẫn hoạt động khi mute.
5. Editor authoring/validation: xuất manifest tạo TTS, gán clip theo id, kiểm tra thiếu clip/trùng id/text đã đổi và metadata nguồn audio. Không chứa API key trong Assets.

Scene/bootstrap: đặt VoicePlayer cạnh OfficeIntroSystems, gán source và adapter bằng công cụ Editor. Khởi tạo và kiểm tra reference trước câu đầu. Không thêm singleton hay framework thoại mới. Voice source nằm dưới hierarchy được intro quản lý pause hoặc được pause trực tiếp bởi adapter, chỉ chọn một cơ chế.

Communication: controller gọi player trực tiếp; completion có token/id của lượt phát để bỏ callback cũ. Settings chỉ thông báo khi giá trị đổi. Không tìm object hoặc cấp phát audio mỗi frame. AudioMixer Voice là một hạng mục riêng ở đợt B, kiểm tra mixer có sẵn trước khi tạo mới.

## Đợt A — pilot thoại Scene 1

### A1. Chốt dữ liệu thoại

- Đối chiếu Line[] hiện có, nội dung người chơi thực sự thấy và nhiệm vụ in/giao báo cáo.
- Tạo DialogueSequence.asset cho intro với id như act1_intro_boss_001, act1_intro_ty_001. Tách câu theo nhịp diễn, không ghép cả đoạn dài rồi chia phụ đề bằng tỷ lệ thời gian.
- Chuẩn hóa tên Tý; chỉ bỏ đoạn thay text lúc chạy sau khi scene/asset và công cụ setup đã migrate đầy đủ.
- Manifest authoring chứa id, text hiển thị, spokenText nếu cần sửa cách phát âm, vai, chỉ dẫn cảm xúc và hash nội dung. Hash đổi thì đánh dấu clip cần tạo lại.

### A2. Tạo và chọn audio mẫu

- Hai giọng ứng viên mỗi vai; cùng văn bản để nghe so sánh. Sếp: điềm, khô, dứt câu; Tý: mệt, dè dặt, có ngắt tự nhiên.
- Nghe trong bối cảnh có tiếng điều hòa/đường phố; chọn phát âm tên Tý, Excel và cách ngắt câu phù hợp.
- Tạo một file mỗi câu, tên theo id. Lưu bản master ngoài đường build, clip dùng trong Assets/_Project/Audio/Voice/vi/Act1/.
- Ghi nguồn, voice/model, ngày tạo, thông số và điều kiện sử dụng trong manifest. Chi phí/nhà cung cấp cần được chọn trước khi gọi dịch vụ trả phí.
- Không hứa giọng có cảm xúc trước khi nghe thử; có thể phải chỉnh spokenText và tạo lại.

### A3. Nối playback và phụ đề

- Thêm DialogueSequence.cs và DialogueVoicePlayer.cs; sửa OfficeIntroController đọc asset hoặc fallback Line[] cũ, cập nhật setup Editor để không làm mất gán clip khi chạy lại.
- Tắt playOnAwake, loop=false, pitch=1; giọng intro dùng 2D cho dễ nghe trong pilot.
- Có clip: lấy thời lượng từ clip cộng postDelay. Không có clip hoặc load thất bại: phụ đề chạy đủ fallbackDuration, vẫn bàn giao gameplay bình thường.
- Mute không rút ngắn câu: vẫn dùng thời lượng clip; missing clip dùng thời lượng fallback. Nhịp không lời dùng fallbackDuration.
- Pause giữ thời điểm audio và phụ đề; Resume tiếp tục đúng câu. Stop/skip/handoff/unload phải dừng voice trước khi trả control và vô hiệu callback cũ.
- Chưa thêm phím Space skip ở intro: Space đang được dùng trong gameplay và DialogueUI khác. Pilot bảo toàn input hiện có; nếu thêm skip phải có chỉ dẫn UI và kiểm thử riêng.

### A4. Kiểm thử và tiêu chí nghiệm thu

- Compile, validator scene, chạy lại smoke task flow hiện có (mốc trước TTS: 51 assertions).
- Test intro riêng không dùng SkipToReadyAtDesk: phát chuỗi clip ngắn thật, kiểm tra audio source/line index/subtitle/handoff đúng một lần.
- Pause giữa câu, resume nhiều lần: audio time và elapsed không tăng khi pause, không phát lại từ đầu.
- Thiếu clip, voice mute/volume=0, nhịp im lặng và asset chưa migrate: không kẹt intro, không bỏ qua nhiệm vụ.
- Skip/handoff/disable/unload giữa câu: voice dừng, callback cũ không chuyển thêm câu hay khóa input.
- Stop/Play hai lần và mở trực tiếp Office: không sinh VoicePlayer trùng, không còn âm thanh lượt trước.
- Nghe thủ công trên tai nghe/loa: rõ chữ, không cắt đuôi câu, không bật âm quá lớn và không lệch phụ đề. Kiểm tra tự động không chứng minh được cảm xúc hoặc chất lượng giọng.
- Build Windows Development và chạy offline: audio vẫn phát, gameplay sang Act 2, không cần khóa API trong build.

Hoàn thành A khi toàn bộ clip pilot được nghe duyệt, intro có giọng và phụ đề khớp, pause/lifecycle đạt, smoke gameplay không regression và bản build offline chạy được. Nếu chưa có audio được chọn, chỉ hoàn thành nền code; chưa coi pilot TTS hoàn tất.

## Đợt B — cài đặt và mix âm thanh

- Thêm thanh Voice Volume và lưu preference; giữ phụ đề khi tắt giọng.
- Route Voice vào AudioMixer; hạ nhẹ BGM khi thoại rồi phục hồi sau câu, cancel và unload. Không sửa trực tiếp volume của ambience do intro đang quản lý nếu chưa thống nhất chủ sở hữu.
- Kiểm tra trạng thái duck khi pause, thoại nối tiếp, tắt voice và chuyển scene.
- Profile dung lượng/thời gian load rồi chọn compression/load type; clip thoại mono khi phù hợp. Không áp cùng một cấu hình cho mọi độ dài clip.

## Đợt C — mở rộng nội dung

- Adapter cho DialogueUI: Space lần đầu hiện hết chữ, lần tiếp dừng voice và đóng đúng một lần; không callback trùng.
- Thêm thoại nội tâm sau giao báo cáo/uống nước và thoại hồi ức Scene 2/3 qua asset. Chọn nội dung có tác dụng cảm xúc trước.
- Nếu cần đọc hint: thêm tùy chọn riêng, ưu tiên thoại cốt truyện, không chồng giọng hoặc đọc lại mỗi lần HUD refresh.

## Chưa làm trong các đợt trên

TTS sinh trực tiếp khi chơi, backend giữ API key, download/cache/retry audio, voice cloning và lip sync. Chỉ thiết kế thêm khi có nhu cầu nội dung động hoặc hình nhân vật đủ để dùng; chúng không cần cho pilot thoại cố định.

## Thứ tự triển khai và điểm chốt

A1 chuẩn hóa thoại → A2 nghe chọn giọng → A3 gán clip/playback → A4 kiểm thử/duyệt âm thanh → B settings/mix → C mở rộng. Có thể làm nền A3 bằng clip kiểm thử trong lúc chọn giọng, nhưng nghiệm thu A phải dùng audio TTS thật.

Mỗi đợt có diff code, asset và báo cáo test riêng. Bảo toàn .meta/GUID; sửa scene qua Unity Editor, tránh builder dựng lại nội thất/ánh sáng.
