# Kế hoạch cutscene Ku Tí từ Act 1 sang Act 2

Các đường dẫn trong tài liệu tới `Tools/Cutscene/` và `Tools/CutsceneReferences/` là nguồn làm việc cục bộ, được giữ trên máy và bỏ qua bằng `.gitignore`. Repository chỉ đưa lên bản video đã duyệt `Assets/_Project/Resources/Cutscenes/OfficeToHome_Final.mp4` cùng mã tích hợp và metadata Unity.

Ku Tí tan làm, chạy xe máy về căn hộ, nằm ngủ rồi tỉnh dậy trong căn phòng quê của Scene2. Dựng hai cảnh ở phố và căn hộ bằng Blender, xuất thành một clip 3D. Unity giữ cảnh văn phòng trước clip và cảnh mở mắt thật trong Scene2 sau clip.

## Kịch bản và nhịp dựng

| Mốc dự kiến | Hình ảnh | Âm thanh |
| --- | --- | --- |
| 0–2 giây · Unity | Sau khi công việc kết thúc, giữ góc nhìn tại bàn thêm một nhịp rồi mờ sang đen. Ku Tí rời văn phòng qua cú cắt. | Tiếng bàn phím và tiếng nền văn phòng lắng xuống. |
| 2–9 giây · Blender | Máy quay bám sau chếch một bên. Ku Tí chạy xe máy qua đoạn phố nhỏ với vài cửa tiệm đã đóng. Đèn hậu đỏ và những khoảng sáng từ đèn đường giúp thấy rõ dáng người lái. Cắt khi xe đi qua một khoảng tối. | Tiếng xe máy đều, nền phố thưa. |
| 9–13 giây · Blender | Góc máy cố định trong căn hộ. Ku Tí đặt chìa khóa vào đĩa nhỏ sứt mép cạnh cửa. Trong hậu cảnh thấy chiếc giường và đèn vàng. Có thể lấy khung từ vai trở xuống. | Tiếng cửa khép và tiếng chìa khóa chạm đĩa. |
| 13–22 giây · Blender | Ku Tí ngồi ở mép giường, ngả xuống gối. Cắt sang góc nhìn nằm ngửa hướng lên trần căn hộ rồi tối dần. Nếu động tác nằm tốn công, cắt từ tư thế ngồi sang góc nhìn từ gối và dùng tiếng nệm lún để nối. | Tiếng nệm lún, tiếng phố nhỏ dần. Kết clip ở màn đen. |
| Sau clip · Unity | Trong màn đen bắt đầu nghe tiếng chim. Scene2 mở mắt từ góc nằm trên giường tương tự nhưng khung hình là căn phòng quê. Hiện **Xuống giường**, chờ bấm **E**, rồi tiếp tục thoại mẹ và nhiệm vụ hiện có. | Tiếng chim vào trước hình khoảng nửa giây để thử, sau đó nối liền với âm thanh Scene2. |

Mốc 22 giây tính từ lúc kết thúc công việc đến cuối clip, trong đó video Blender khoảng 20 giây. Độ dài cảnh mở mắt và thời gian chờ bấm E tính riêng. Chốt nhịp sau khi xem bản nháp trong game.

## Phạm vi dựng Blender

- **Ku Tí trưởng thành.** Dựng theo [ảnh đã duyệt](../Tools/CutsceneReferences/KuTi_OfficeWorker_Concept.png). Áo sơ mi xanh nhạt xắn tay, quần than, túi vải nâu đeo chéo, mũ bảo hiểm xanh đậm khi chạy xe. Ưu tiên dáng nhìn từ sau và từ sau chếch một bên. Dùng vài tư thế đơn giản nếu chưa có model nhân vật với bộ xương phù hợp.
- **Xe và phố.** Một xe máy với bánh, yên, tay lái và đèn hậu dễ nhận ra. Dựng một đoạn đường thẳng, vài mặt tiền và 3–4 đèn đường. Cho xe và camera đi cùng nhau. Cú cắt bỏ qua cảnh đỗ xe và xuống xe.
- **Căn hộ.** Chỉ dựng phần thấy qua camera. Cần cửa, bàn nhỏ, chìa khóa, đĩa, giường, gối, đèn và trần. Giữ ánh đèn vàng trong phòng và tông xanh tối ngoài phố. Căn phòng quê dùng cảnh Unity hiện có.
- **Đầu ra.** Giữ file `.blend` chỉnh sửa được và xuất MP4 H.264. Dùng bản nháp nhẹ để thử bố cục và nhịp cắt trước khi xuất bản rõ hơn. Máy đã chạy thử Blender 5.2.2 và xuất thành công một clip MP4 thử chuyển động.

## Trình tự thực hiện

1. **Chốt khung hình nối.** Mở Scene2 trong Game View, xem và chụp góc đầu tiên từ giường. Dùng khung đó làm mẫu cho hướng nhìn từ gối ở căn hộ. Căn gần đúng độ nghiêng và vị trí điểm nhìn để cú nối dễ đọc.
2. **Dựng bản nháp.** Tạo xe, người lái, phố và góc phòng bằng hình khối. Dựng đủ ba cảnh Blender, ghép với đoạn mờ của Act 1 và đoạn mở mắt của Act 2 để xem trọn nhịp.
3. **Hoàn thiện hình và tiếng.** Dùng thiết kế Ku Tí đã duyệt, thêm đèn hậu, vài mặt tiền và chi tiết chìa khóa. Canh tiếng xe, chìa khóa, nệm và khoảng màn đen trước khi nghe tiếng chim.
4. **Tích hợp Unity.** Ở [OfficeSceneController](../Assets/_Project/Scripts/Systems/OfficeSceneController.cs), thay đoạn gục xuống bàn tại điểm kết thúc công việc bằng nhịp chờ, mờ hình và phát clip. Đồng bộ lời thoại cũ với bối cảnh tan làm nếu câu thoại không còn hợp. Tận dụng cơ chế tải trước trong [SceneLoader](../Assets/_Project/Scripts/Core/SceneLoader.cs), kích hoạt Scene2 sau khi clip kết thúc và che kín lúc chuyển. Giữ quyền điều khiển được khóa tới khi [BedWakeUpController](../Assets/_Project/Scripts/Systems/BedWakeUpController.cs) nhận luồng mở mắt. Nếu video thiếu hoặc lỗi, chuyển thẳng sang Scene2 qua màn đen để người chơi vẫn tiếp tục được.
5. **Chạy kiểm tra từ màn văn phòng.** Đi hết luồng tan làm bằng thao tác chơi thật. Kiểm tra clip chỉ chạy một lần, không nháy khung hình khi đổi cảnh, âm thanh không chồng hai lớp tiếng chim, nút E xuống giường hoạt động và lời mẹ phát đúng lúc. Thử cả trường hợp video lỗi và cập nhật kiểm thử chuyển cảnh theo luồng mới. Kiểm tra thêm một bản build để xác nhận video phát được ngoài Editor.

Bản đầu đạt yêu cầu khi người xem hiểu Ku Tí đã **về căn hộ rồi mới ngủ**, nhận ra cậu ở góc quay từ sau và thấy cú chuyển sang phòng quê rõ ràng. Sau khi bản nháp đạt ba điểm đó mới dành thời gian tăng chi tiết cho phố và căn hộ.

## Mở và chỉnh bản dựng

- Mở [OfficeToHome.blend](../Tools/Cutscene/OfficeToHome.blend) trong Blender 5.2. Timeline có các mốc **Road**, **Keys**, **Bed** và **Pillow**. Bản dựng nháp dài 20 giây ở 480 × 270, 16 fps.
- Sau khi chỉnh, render animation thành MP4 H.264 rồi ghép [âm thanh nháp](../Tools/Cutscene/OfficeToHomeAudio.wav) vào video với AAC. Nếu đổi mốc cắt, canh lại tiếng chìa khóa và tiếng nệm. Thay [video Unity](../Assets/_Project/Resources/Cutscenes/OfficeToHome.mp4) bằng bản đã ghép tiếng.
- Có thể dựng lại bản gốc bằng [script Blender](../Tools/Cutscene/build_office_to_home.py) và [script âm thanh](../Tools/Cutscene/build_office_to_home_audio.py). Chạy script Blender với `-- --render` để xuất MP4 cạnh file Blender. Dùng FFmpeg ghép hai đầu ra bằng `-map 0:v:0 -map 1:a:0 -c:v copy -c:a aac -movflags +faststart`.
- Trong Unity, vào Act 1, giao báo cáo, uống nước rồi quay lại ghế và bấm **E · Tan làm**. Để kiểm tra riêng luồng này trong Editor, dùng menu **KuTy → Tests → Run merged sleep handoff with checkpoint backup**.

## Nâng cấp cảnh chạy xe

Ưu tiên hiện tại là nhân vật, xe và đường phố. File chỉnh sửa mới nằm ở [OfficeToHome_RoadRefined.blend](../Tools/Cutscene/OfficeToHome_RoadRefined.blend). [Ảnh render](../Tools/Cutscene/Road_Refined_Preview.png) dùng để kiểm tra dáng lái và bố cục.

- Ku Tí dùng lại mesh và bộ xương từ model nam Boss trong project, đổi chất liệu áo sang xanh và quần sang than. Mũ xanh đậm và túi nâu giữ theo mẫu đã duyệt. Đây là lựa chọn tận dụng nguồn hiện có, khuôn mặt và trang phục chưa phải model Ku Tí độc lập.
- Xe có lốp tròn, vành, nan hoa, phuộc, gương, đèn pha, đèn hậu, xi nhan và biển số. Bề mặt sơn, cao su và kim loại có độ nhám khác nhau.
- Phố thêm vỉa hè, cửa kính, biển tiệm, máy lạnh, dây điện và khu nhà xa. Khu nhà xa dùng model City_Exterior của project. Hàng mặt tiền vẫn có độ lặp, cần thêm biến thể nếu tiếp tục nâng chất lượng.
- [render_refined_road.py](../Tools/Cutscene/render_refined_road.py) xuất riêng bảy giây chạy xe bằng Cycles GPU. [assemble_refined_road.py](../Tools/Cutscene/assemble_refined_road.py) ghép đoạn đó với phần còn lại và giữ âm thanh gốc. Bản trước được sao lưu cạnh file Blender trước khi thay video Unity.

Video ghép xuất ở 960 × 540, 24 fps, nằm ở [OfficeToHome_RoadRefined.mp4](../Assets/_Project/Resources/Cutscenes/OfficeToHome_RoadRefined.mp4). Luồng tan làm ưu tiên bản mới này. Đoạn chạy xe được render mới ở kích thước này. Phần căn hộ vẫn lấy từ bản nháp cũ và được phóng lên để ghép cùng khung hình, nên chất lượng hai phần còn khác nhau. Đã kiểm tra giải mã toàn bộ video và biên dịch C#, chưa xác nhận bản video mới trong Play Mode.

## Bản duyệt phố Việt Nam

**Bản đang tích hợp sau lần duyệt cuối** nằm ở [OfficeToHome_Final.mp4](../Assets/_Project/Resources/Cutscenes/OfficeToHome_Final.mp4), được sao chép nguyên bản từ [clip 13,5 giây đã duyệt](../Tools/Cutscene/DriveHome_Sleep_Trimmed_Preview.mp4). Luồng **E · Tan làm** ưu tiên clip này. Gồm chạy xe, một giây đứng ở cửa phòng dưới dạng bóng dáng bên phải, rồi động tác ngồi xuống nằm đã sửa tiếp xúc gối. Đã bỏ đoạn 8–8,5 giây theo yêu cầu. Đã kiểm tra bản sao, giải mã hình/tiếng và biên dịch C#, chưa xác nhận lại trong Play Mode.

Model người, xe và túi hiện dùng FBX `kyti_laixe.fbx` được người dùng cung cấp, lưu trong `Tools/Cutscene/RidingSource/`. Phố dựa theo ảnh đêm người dùng gửi, đa số cửa hàng đóng, chỉ vài tiệm còn sáng.

Bản Blender mới ở [OfficeToHome_VietnamNight_Varied.blend](../Tools/Cutscene/OfficeToHome_VietnamNight_Varied.blend). Đã bổ sung bốn kiểu mặt tiền vào hai mẫu đóng cửa trước đó, xen kẽ nhà một đến ba tầng, thay chiều rộng và độ lùi, giảm các gờ cột đồng dạng. Tim đường có vạch vàng đứt nét. Đây là lựa chọn hình ảnh để duyệt cutscene.

[Clip duyệt bảy giây](../Tools/Cutscene/VietnamNight_Varied_Preview.mp4) xuất 720p, 24 fps bằng Eevee với âm thanh xe, nằm riêng và chưa được đưa vào video Unity. [Script dựng phố](../Tools/Cutscene/StreetReferences/build_vietnam_street.py) và [script xuất clip](../Tools/Cutscene/StreetReferences/export_varied_preview.py) giữ lại để chỉnh tiếp. Các mẫu mặt tiền vẫn được tái sử dụng dọc phố.

Sau khi người dùng duyệt, đoạn phố này đã được ghép vào bảy giây đầu của [cutscene Unity mới](../Assets/_Project/Resources/Cutscenes/OfficeToHome_VietnamNight.mp4). Luồng **E · Tan làm** ưu tiên video mới. Clip đầy đủ dài 20 giây, 720p, 24 fps, giữ phần căn hộ và âm thanh từ bản trước. Đã kiểm tra giải mã video và biên dịch C#, chưa xác nhận lại trong Play Mode.

## Cập nhật lời suy nghĩ ngày 07/11/2026

Video đang dùng trong Unity vẫn là `Assets/_Project/Resources/Cutscenes/OfficeToHome_Final.mp4`, nay dài 20 giây, 720p / 24 fps. Không đổi metadata/import settings hiện có. Bản trước được giữ cục bộ tại `Tools/Cutscene/StoryAudio/OfficeToHome_BeforeThoughts.mp4`.

- 1,50–4,50 giây: chữ nhỏ nghiêng bên dưới, “Lại hết một ngày…”.
- 11,25–15,20 giây: sau khi tựa gối, “Giá mà sáng mai… được nghe mẹ gọi dậy như hồi đó.”. Giữ thêm chuyển động thở để đọc đủ câu.
- 15,20–16,00 giây: tối dần; giữ một nhịp im lặng. Tiếng chim nhỏ bắt đầu ở 17,20 giây.
- Đã ghép bản giọng mẹ người dùng cung cấp (`c81000fb5b0bdefe0422c2eecb4c407e.mp3`, dài 1,805 giây) từ mốc 16,60 giây. Tiếng chim vào phía sau ở 17,20 giây. Giữ clip dài 20 giây trước khi chuyển Scene2.

Đã bỏ toàn bộ âm thanh xe, chìa khóa và vải tổng hợp từ video cũ. Tiếng chim lấy từ bản thu [Morning Bird của SieuAmThanh](https://freesound.org/people/SieuAmThanh/sounds/765711/), giấy phép CC0 1.0, đoạn nguồn 38–44 giây; lọc nhẹ và phát nhỏ. Chưa đánh giá chủ quan bằng nghe trong game.

Dựng lại bằng `python Tools/Cutscene/StoryAudio/build_story_preview.py`. Khi có đúng bản thu mẹ, thêm `--mother <đường dẫn WAV/MP3>`; xem lại nhịp trước khi thay video runtime. Các nguồn và script dựng nằm trong thư mục Tools đã gitignore. Video đã giải mã toàn bộ không báo lỗi; đã kiểm tra ảnh ở hai mốc chữ. Chưa xác nhận trong Unity Play Mode. Những mục dựng nháp phía trên là lịch sử, không dùng lại soundtrack tổng hợp.

`mix_exact_voice.py` ghép PCM theo vị trí mẫu để bảo đảm im lặng trước 16,60 giây, sau đó mux vào hình; chạy bước này sau script dựng preview có giọng.

### Chỉnh nhịp giọng mẹ

Giọng mẹ dùng `atempo=1.7` để tăng tốc và giữ cao độ, còn 1,063 giây. Chuyển sang mốc 18,587–19,650 giây, gần cuối video 20 giây. Luồng hiện tại có 0,45 giây che chuyển Scene2 và khoảng 0,18 giây trước khi mí mắt bắt đầu mở; khoảng từ hết file giọng đến mở mắt dự kiến 0,98 giây cộng thời gian kích hoạt scene. Không đổi mã chuyển scene hoặc độ dài video. Tiếng chim dời theo về cuối clip. Đã giải mã toàn bộ video mới và kiểm tra vùng có giọng trong bản AAC; chưa đo thực tế trong Play Mode.
