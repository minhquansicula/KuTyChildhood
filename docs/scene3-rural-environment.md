# Scene 3 — Đường làng, ruộng lúa và tiệm tạp hóa

## Bố cục làng quê gọn hiện tại (09/10/2026)

Scene `Act3_MemoryWorld_OutSide` dùng nhóm `LangQue_VietNam_Gon`. Nền được thu từ 180 × 210 m xuống 88 × 102 m, giới hạn đi lại tại X ±43 m, Z −12 đến 85 m. Lũy tre và gò đất thấp bao quanh ranh giới; bốn Box Collider chặn người chơi ra ngoài. Đây là phạm vi một xóm nhỏ, không mở thêm vùng đất ngoài làng.

Đường đất giữa làng nối năm nhà, tiệm tạp hóa và bãi chơi bi. Các nhà quay về đường, có sân đất khô, cây ổi, bàn ghế, hàng rào tre; lối ngõ ngắn và cầu ván bắc qua mương dẫn nước. Vườn rau và bụi chuối lấp khoảng đất nhỏ giữa khu ở và ruộng.

| Khu vực | Tọa độ X/Z (m) |
| --- | --- |
| Nhà 01 | −18 / 4 |
| Nhà 02 | 18 / 44 |
| Nhà 03 | −25 / 27 |
| Nhà 04 | 18 / 66 |
| Nhà 05 | −23 / 65 |
| Tiệm tạp hóa | 20 / 20 |
| Tâm bãi bi | −15 / 44 |

Bãi bi rộng 14 × 14 m, có cây bóng mát, ghế nghỉ và ba bạn nhỏ quanh vòng bi. Các bi, điểm bắn, bàn collider, vòng tính điểm và camera minigame được chuyển cùng nhau; các component gameplay và tham chiếu giữ nguyên. Tiệm giữ vị trí tương tác và nội dung bán hàng hiện có.

16 thửa ruộng có mặt nước nông và bờ đất, bố trí tránh sân nhà và sân bi. Ruộng dùng 3.641 cây từ bộ `RiceOptimized`, chia thành 82 ô để culling. `RuralRiceFieldRenderer` vẽ bằng GPU instancing, không tạo GameObject cho mỗi cây. Ba mức mesh có 5.986 / 380 / 48 tam giác; chuyển ở khoảng cách 6 m và 25 m. Ngưỡng hiển thị xa 180 m phủ toàn xóm. Shader `Lua_Ruong_Instanced` dùng chung atlas màu/normal, chuyển động gió theo vertex color và uốn nhẹ khi người chơi tới gần. Lúa không cast shadow để giảm chi phí; vẫn nhận bóng từ cảnh.

Muốn chỉnh mật độ, chọn `RuongLua_01` đến `RuongLua_16`, đổi **Spacing** trên component `RuralRiceFieldRenderer` (mặc định 0,78 m; giảm làm dày và tăng chi phí). **Plant Scale** mặc định 0,62. Chỉnh **Wind Amplitude**, **Wind Speed** trên material `Lua_Ruong_Chung`. Không cần chỉnh shader `Lua_Gio` cũ.

Lúa có gió nền nhẹ và từng dải gió mạnh chạy qua ruộng. Mỗi đợt lấy hướng lệch, độ mạnh và thời gian bắt đầu khác nhau từ một seed theo chu kỳ; dải gió lan theo tọa độ thế giới nên các ruộng phản ứng liên tục, không rung đồng loạt. Gốc đứng yên nhờ vertex color R = 0, thân/ngọn cong và lá rung nhanh hơn khi dải gió tới. Cùng công thức dùng cho Forward và Depth; cả ba LOD dùng chung gió. Bounds vẽ đã nới để ngọn lúa đang cong không bị culling ở mép camera.

Trong material `Lua_Ruong_Chung`, **Gust Strength** mặc định 0,28 m điều khiển độ cong khi gió mạnh; **Gust Interval** 24 giây là chu kỳ đợt gió; **Gust Travel Speed** 14 m/s là tốc độ dải gió chạy qua làng; **Gust Band Width** 10 m là nửa bề rộng vùng ảnh hưởng. **Wind Direction (XZ)** mặc định (0,8; 0,6) là hướng gió nền. Chu kỳ tự kéo dài nếu cần để dải gió thoát hết map trước đợt kế tiếp. Ngẫu nhiên ở đây là chuỗi giả ngẫu nhiên có thể tái hiện để kiểm tra. Không thêm Rigidbody hoặc GameObject riêng cho từng cây.

Ảnh trước/sau và báo cáo nằm trong `Tools/Scene3/VillageComposition`. Bản sao scene trước thay đổi là `Act3_MemoryWorld_OutSide.before-compact-village.unity`; nhóm bố cục/lúa cũ được giữ nhưng tắt. Công cụ `KuTy → Scene 3 → Compose compact Vietnamese village with new rice` từ chối tạo trùng khi nhóm làng gọn đã tồn tại.

Kiểm tra gồm biên dịch C#/shader trong Unity 2022.3.62f3, ảnh từ camera Unity, bố trí ruộng không chồng sân, tham chiếu gameplay và nền đi lại tại điểm xuất phát, đường, cầu, tiệm và sân bi. Đã chạy CharacterController qua cầu vào lối tiệm/sân bi và thử đi qua cả bốn ranh giới; tất cả đều đạt. Bản scene đã lưu được kiểm tra trong project tạm riêng; báo cáo là `verification_saved_scene.json`. Đây chưa phải phép đo FPS hay kiểm thử toàn bộ chuỗi nhiệm vụ; nhân vật trẻ/người dân và gia súc vẫn là model phác thảo có sẵn.

## Bờ ruộng có đất và cỏ (09/10/2026; cỏ đã xóa 10/10)

Theo yêu cầu ngày 10/10/2026, toàn bộ 17 object `CoBoRuong_Gop` / `CoBoKenh_Gop` đã xóa khỏi scene. Số cụm cỏ còn lại = 0; bộ cỏ trước đó có 46.730 lá / 140.190 tam giác, từng giảm 80% rồi được xóa hoàn toàn. Bờ đất, nền màu xanh, lúa và bố cục giữ nguyên. Công cụ dựng lại bờ không sinh cỏ nữa. Scene đã lưu; 39 component gameplay và kiểm tra lối đi đạt. Bản sao scene trước xóa là `Tools/Scene3/VillageComposition/before-remove-bank-grass.unity`; báo cáo `remove_grass_report.txt`. Các mesh cỏ cũ được giữ làm tài nguyên phục hồi, không còn renderer trong scene. Chưa đo lại FPS ở cùng góc camera; ảnh Stats ban đầu báo 84,7 FPS, main thread 11,8 ms, render thread 2,1 ms, chưa đủ xác định riêng phần nào gây tụt FPS.

Các đoạn bên dưới mô tả bộ cỏ trước khi xóa.

64 bờ thửa ruộng và 4 bờ mương đã thay khối hộp bằng mesh đất có sườn thoải, mặt trên hơi gồ ghề. Bờ ruộng rộng khoảng 0,72 m, cao 0,145 m; bờ mương rộng 0,82 m, cao 0,17 m. MeshCollider theo hình bờ, không dùng collider riêng cho cỏ. Cầu ván được chừa trống để người chơi đi qua.

Cỏ thấp mọc ở hai mép, lá mảnh cong và có độ cao/màu khác nhau; có gió nhẹ trên ngọn. Khoảng 46.730 lá cỏ được gộp thành 17 mesh (mỗi ruộng một mesh và một mesh ven mương), dùng chung material `CoBoRuong_Nham`; không tạo GameObject hay Rigidbody cho từng lá. Các mesh cỏ không cast shadow. Đây là bản cỏ đơn giản dựng từ geometry, chưa phải bộ texture cỏ ảnh chụp.

Ba material nằm tại `Assets/_Project/Art/Materials/Scene3/RiceOptimized`: `BoRuong_DatCo` cho bờ, `NenCo_Nham` cho nền xanh/gò đất quanh làng, `CoBoRuong_Nham` cho lá. Shader `Matte_Grass_Earth.shader` chỉ tính ánh sáng khuếch tán và ánh sáng môi trường, không có specular hoặc phản chiếu bóng. Màu cỏ loang theo tọa độ thế giới, có chi tiết nhỏ giảm dần ở xa; đất dùng texture `brown_mud_dry_diff_4k`. Vì vậy bề mặt bớt phẳng và bớt cảm giác nhựa, không chỉ giảm một giá trị Smoothness.

Muốn chỉnh màu, chọn material và sửa **Grass colour**; **Grass coverage** giảm sẽ lộ thêm đất (hợp với bờ, nên giữ 1 cho lá). **Earth tint** đổi màu đất, **Earth scale in world metres** tăng sẽ lặp texture đất nhiều hơn. **Grass blade wind** chỉ dùng trên material lá, đang đặt 0,04 m. Nếu dùng URP/Lit cho cỏ khác, đặt Metallic = 0, Smoothness khoảng 0–0,15 và tắt Specular Highlights/Environment Reflections để giảm bóng; vẫn cần màu/texture biến thiên để tránh mặt xanh trơn.

Công cụ dựng bờ nằm trong `Assets/_Project/Editor/Scene3VillageComposition.Banks.cs`. Bản sao scene trước chỉnh bờ là `Tools/Scene3/VillageComposition/before-natural-banks.unity`. Kiểm tra shader bằng render Unity và kiểm tra CharacterController qua các cầu/lối tiệm/sân bi, bốn ranh giới map; các component gameplay giữ nguyên. Chưa đo FPS trên máy đích.

## Nhà Tencent 01 đã nhập vào game (09/10/2026)

Căn nhà tải từ Tencent thay nhà đầu tiên ở `LangQue_VietNam_Gon/NhaDan_VaSanVuon/SanNha_Gon_01/Nha01_NgoiDo_HienTon`, tọa độ X/Z −18 / 4, cửa hướng về đường làng. Sân, cây ổi, bàn ghế và cầu vào sân giữ vị trí; nhà cũ đổi tên có hậu tố `_Cu_DaTat` và tắt để phục hồi. Collider hộp cũ `VaChamNha` được tắt.

OBJ gốc có 1.500.012 tam giác, vẫn ở thư mục `BLENDER` bên ngoài Unity project. Script `Tools/Scene3/import_tencent_house.py` tạo FBX 3 LOD: 30.000 / 12.000 / 3.000 tam giác, giữ chiều rộng khoảng 7,2 m và đặt gốc tại mặt đất, sau đó chạy `repair_tencent_house.py` để tạo lại UV và bake atlas từ model gốc. Không dùng UV sau decimate vì texture cửa sổ bị kéo thành tam giác. Các mặt vữa gần mặt phẳng được chiếu lên mặt phẳng tường và dùng normal phẳng; mái ngói, tôn, cửa giữ normal chi tiết. Bản Blender đã sửa là `BLENDER/NhaQue_Tencent_01_SuaTuong.blend`. Bản gần đã giảm khoảng 98% tam giác so với file tải về.

FBX nằm tại `Assets/_Project/Art/Models/Scene3/TencentHouse01/NhaQue_Tencent_01.fbx`; prefab dùng trong scene là `Assets/_Project/Prefabs/Scene3/TencentHouse01/NhaQue_Tencent_01.prefab`. LODGroup chuyển theo tỷ lệ chiều cao vật thể trên màn hình, các ngưỡng 0,22 / 0,085 / 0,003; khoảng cách thực phụ thuộc camera/FOV và LOD Bias. Mesh trung bình có MeshCollider tĩnh, hoạt động độc lập với việc thay LOD của renderer. Không thêm Rigidbody.

Mỗi LOD dùng một material URP/Lit `Nha01_LOD0_Tencent_PBR.mat` / `Nha01_LOD1_Tencent_PBR.mat` / `Nha01_LOD2_Tencent_PBR.mat` trong `Assets/_Project/Art/Materials/Scene3/TencentHouse01`. Atlas riêng có kích thước 2048 / 1024 / 512; BaseColor giữ sRGB, Normal nhập đúng Normal Map, MetallicSmoothness dùng dữ liệu tuyến tính. Metallic ở R, Smoothness = 1 − Roughness ở A; Smoothness trên material nhân thêm 0,6. Bật mipmap, trilinear và anisotropic 4. Ảnh 4K gốc và material cũ vẫn được giữ để phục hồi.

Bản FBX/Blender trước sửa UV/normal nằm trong `Tools/Scene3/TencentHouse01/Repair/BeforeRepair`. Báo cáo planarity và atlas là `Tools/Scene3/TencentHouse01/Repair/repair_report.json`; ảnh ép từng LOD trong Unity nằm tại `Tools/Scene3/TencentHouse01/Repair/Unity`. Cả ba LOD đã áp vào scene và kiểm tra bằng camera Unity; cửa sổ không còn bị kéo thành tam giác và tường không còn các mảng normal tam giác lớn. Mức xa vẫn có texture mờ hơn do atlas nhỏ hơn. Kiểm tra bố cục, collider/lối đi và 39 component gameplay đạt; scene đã lưu.

Công cụ cài nhà: `Assets/_Project/Editor/Scene3VillageComposition.TencentHouse.cs`, menu **KuTy → Scene 3 → Install downloaded Tencent house 01**. Bản sao scene trước đổi nhà nằm tại `Tools/Scene3/VillageComposition/before-tencent-house01.unity`. Đã kiểm tra 39 component gameplay giữ nguyên serialized data, kích thước/phạm vi sân, LOD mesh, normal map, bố cục ruộng và CharacterController qua các cầu/lối tiệm/sân bi. Ảnh Unity: `Tools/Scene3/VillageComposition/After/05_NhaTencent01.png`. Chưa đo FPS hoặc kiểm thử toàn bộ nhiệm vụ.

## Nhà Tencent chữ L 02 (10/10/2026)

Model gốc nằm tại `BLENDER/NhaQue_L/bf81dc6ffe8a6a79404b6ecf30e0dfa3.obj`, có 1.499.748 tam giác. Nhà thay mẫu số 2 tại X/Z (18, 44), hiên quay về đường làng; kích thước sau xoay là khoảng 6,28 × 4,45 × 7,80 m. Chỉ scale đồng đều, không ép riêng trục, remesh, làm phẳng hay smooth vị trí vertex. Nhà cũ giữ nhưng tắt; sân và đồ sân không dời.

`Tools/Scene3/prepare_tencent_house02.py` tạo từng LOD độc lập từ model gốc và kiểm tra sai lệch bề mặt hai chiều bằng BVH. Các mesh 40.000 / 16.000 / 5.000 tam giác đã vượt qua giới hạn kiểm tra. Trong khoảng 50.000 điểm của bản gần, sai lệch lớn nhất 5,03 mm, trung bình 0,75 mm; đây là kiểm tra lấy mẫu, không phải chứng minh mọi điểm trùng tuyệt đối. Sai lệch lớn nhất được lấy mẫu ở LOD giữa/xa là 12,07 / 57,37 mm. Bản xa chỉ hiện khi vật thể chiếm dưới 9% chiều cao màn hình. File gốc được xác nhận SHA256 giữ nguyên.

`bake_tencent_house02.py` tạo UV mới và bake màu, normal, metallic/smoothness từ model gốc, dùng atlas riêng 2048 / 1024 / 512. Không tái dùng UV sau collapse. FBX: `Assets/_Project/Art/Models/Scene3/TencentHouse02/NhaQue_Tencent_02.fbx`; prefab: `Assets/_Project/Prefabs/Scene3/TencentHouse02/NhaQue_Tencent_02.prefab`. Ba material URP/Lit nằm trong `Art/Materials/Scene3/TencentHouse02`. LODGroup có ngưỡng 0,24 / 0,09 / 0,003; MeshCollider tĩnh dùng bản xa và không thay theo LOD render. Bản Blender để chỉnh tiếp là `BLENDER/NhaQue_L/NhaQue_L_Game.blend`, kèm model gốc ẩn để đối chiếu; model gốc không được xuất vào FBX.

Công cụ cài: `Scene3VillageComposition.TencentHouse02.cs`, menu **KuTy → Scene 3 → Install downloaded L house 02**. Scene trước đổi nhà được lưu tại `Tools/Scene3/VillageComposition/before-tencent-house02.unity`. Đã biên dịch và áp trong Unity, kiểm tra 39 component gameplay giữ nguyên, nhà trong sân khô, lối đi/cầu/ranh giới đạt, scene đã lưu, cỏ ven bờ vẫn bằng 0. Ảnh ép từng LOD qua bốn camera Unity nằm trong `Tools/Scene3/TencentHouse02/Unity`. Báo cáo hình học là `geometry_report.json`; kiểm tra đường bao qua bốn góc nằm trong `silhouette_report.json`. Tỷ lệ giao/hợp của silhouette gần so với gốc đạt 99,955–99,972%; bản giữa đạt ít nhất 99,887%, bản xa ít nhất 99,663% trong bốn phép chiếu 1000 × 850 px. Chưa đo FPS mới hay thử toàn bộ nhiệm vụ.

## Nhà gạch quê 03, màu cũ (10/10/2026)

Model đúng sau khi người dùng thay file là `BLENDER/Nhaque3/71df8728df7b6501c041c2a53af8b2c4.obj`, có 1.500.008 tam giác, SHA256 `890927cda7e3a54c9275807a3a83b975ae4bbd1a72372035e1599b22a4522623`. File OBJ cũ còn trong thư mục là bản trùng nhà 02 và không được dùng. Bản mới thay nhà số 3 tại X/Z (−25, 27), hiên hướng ra đường, kích thước khoảng 6,30 × 5,19 × 7,00 m. Sân và đồ sân giữ vị trí, nhà phác thảo cũ tắt để phục hồi.

`prepare_tencent_house03.py` tạo LOD 40.000 / 16.000 / 12.000 tam giác, giảm khoảng 97,3% tam giác ở bản gần. Bản xa 5.000 và 8.000 bị loại do sai lệch lấy mẫu vượt giới hạn. Chỉ scale đồng đều và collapse cạnh; không remesh, ép trục hay thay vị trí vertex để làm phẳng tường. 99% điểm kiểm tra bản gần có sai lệch dưới 2,53 mm; lớn nhất được lấy mẫu 28,83 mm, trung bình 0,67 mm. Bản giữa và xa có sai lệch lấy mẫu lớn nhất 41,68 mm. Đây là phép đo lấy mẫu; không khẳng định mọi chi tiết trùng tuyệt đối. Kiểm tra bốn phép chiếu 1000 × 850 px cho silhouette gần khớp gốc ít nhất 99,953%, giữa 99,846%, xa 99,813%.

`village_house_weathering.py` tạo vật liệu Blender từ atlas gốc: saturation 0,72, brightness nhân 0,88, loang màu nâu xám nhẹ, vệt mưa, chân tường ẩm và rêu thưa, tôn có vệt gỉ. Roughness tối thiểu 0,72, metallic nhân 0,5. Các hiệu ứng được bake vào atlas, không thêm noise shader, script hay object lúc chơi. Hình ảnh gốc không sửa. Vật liệu giảm cảm giác mới/bóng; dáng kiến trúc stylized của model vẫn giữ.

`bake_tencent_house03.py` tạo UV riêng và bake albedo bằng Emission (không lẫn ánh sáng), normal và metallic/smoothness. Split normal phẳng của mesh thấp chỉ dùng để ổn định hướng chiếu, không di chuyển vertex; normal bake phục hồi shading gốc ở hai LOD gần. Giảm cage extrusion đã sửa vệt đen trên mái. `refine_house03_far.py` bake lại atlas xa từ LOD0 sạch hơn, tránh ray lọt vào khe nhỏ của model gốc. LOD2 dùng normal hình học, tắt normal map chi tiết để giảm aliasing và công việc shader. Atlas 2048 / 1024 / 512 có mipmap, trilinear, anisotropic 4. Normal nhập đúng loại từ metadata trước lần import đầu tiên.

FBX: `Assets/_Project/Art/Models/Scene3/TencentHouse03/NhaQue_Tencent_03.fbx`; prefab: `Assets/_Project/Prefabs/Scene3/TencentHouse03/NhaQue_Tencent_03.prefab`. LODGroup có ngưỡng theo chiều cao màn hình 0,24 / 0,09 / 0,003, collider tĩnh dùng mesh 12.000 tam giác và không đổi theo LOD render. Ba material riêng nằm trong `Art/Materials/Scene3/TencentHouse03`. File chỉnh tiếp là `BLENDER/Nhaque3/NhaQue_03_Game.blend`, giữ model gốc ẩn; bản chuẩn bị chưa làm cũ là `NhaQue_03_Prepare.blend`. Chạy prepare → bake → refine_far → verify_silhouette → install_house03_assets để dựng lại bộ asset; menu Unity **KuTy → Scene 3 → Install weathered rural house 03** cài vào scene.

Đã cài và lưu scene trong Unity, kiểm tra nhà ở trong sân khô, 39 component gameplay giữ nguyên, CharacterController qua cầu/lối tiệm/sân bi và bốn ranh giới đạt; cỏ ven bờ vẫn bằng 0. Ảnh ép ba LOD qua bốn camera nằm trong `Tools/Scene3/TencentHouse03/Unity`. Báo cáo surface/silhouette/weathering trong `Tools/Scene3/TencentHouse03`; báo cáo cài đặt `Tools/Scene3/VillageComposition/tencent_house03_report.txt`. Backup scene trước đổi nhà: `before-tencent-house03.unity` trong cùng thư mục. SHA256 file OBJ gốc sau xử lý giữ nguyên. Chưa đo FPS mới hay thử toàn bộ chuỗi nhiệm vụ.

## Nhà quê 04 có bếp củi (10/10/2026)

Nguồn là `BLENDER/Nhaque4/2f79e2974f1488051a3e23b79cf6144f.obj`, 1.499.650 tam giác, SHA256 `04cb02b36b40d9d4b556efb06c5b0137956728a3a5437d8eadfa0f4d216a0d5b`. Cả sáu file OBJ/MTL/texture gốc giữ nguyên checksum sau xử lý. Nhà thay vị trí số 4 trong làng tại X/Z (18, 66), quay Y 270° ra đường; kích thước Unity khoảng 4,95 × 3,92 × 7,80 m. Giữ bếp gạch đốt củi, nồi đen, củi xếp, mái tôn vá và màu cũ của model; không thêm hiệu ứng thời tiết vào shader lúc chơi.

LOD là 40.000 / 16.000 / 8.000 tam giác, giảm khoảng 97,3% ở bản gần. Chỉ scale đồng đều và collapse cạnh, không remesh hoặc ép phẳng tường. Sai lệch lấy mẫu lớn nhất của bản gần 8,92 mm, 99% điểm dưới 4,62 mm; bản giữa 26,89 mm, bản xa 48,37 mm. Kiểm tra silhouette bốn phép chiếu 1000 × 850 px cho tỷ lệ giao/hợp thấp nhất lần lượt 99,850% / 99,690% / 99,380%. Đây là phép kiểm tra lấy mẫu và đường bao, không bảo đảm từng chi tiết trùng tuyệt đối.

UV và atlas PBR riêng 2048 / 1024 / 512, màu bake bằng Emission không lẫn đèn. Roughness tối thiểu 0,72. Atlas giữa/xa chiếu lại từ bản gần sạch để tránh ray lọt vào các khe nhỏ. Bản gần dùng normal map bake; bản giữa/xa dùng normal hình học để tránh mảng tam giác tối do chiếu normal và giảm chi phí shader. Bản giữa làm mượt normal qua góc 55°, giữ cạnh sắc; hash vị trí vertex và topology trước/sau bước này giống nhau. Không dùng normal map thử nghiệm từ cage trong material giữa cuối cùng. Texture có mipmap, trilinear, anisotropic 4, nén và metadata đúng loại Normal trước import đầu tiên.

FBX: `Assets/_Project/Art/Models/Scene3/TencentHouse04/NhaQue_Tencent_04.fbx`; prefab: `Assets/_Project/Prefabs/Scene3/TencentHouse04/NhaQue_Tencent_04.prefab`. Ba material URP/Lit trong `Art/Materials/Scene3/TencentHouse04`. LODGroup chuyển theo chiều cao màn hình 0,24 / 0,09 / 0,003; collider tĩnh dùng mesh 8.000 tam giác và giữ nguyên khi đổi LOD render. Bản Blender chỉnh tiếp là `BLENDER/Nhaque4/NhaQue_04_Game.blend`, giữ model gốc ẩn để đối chiếu. Chuỗi dựng asset: prepare → bake → refine_far → repair_mid_normals → finalize_shading → verify_silhouette → install_assets; các script mang hậu tố house04 trong `Tools/Scene3`.

Đã cài bằng `Scene3VillageComposition.TencentHouse04.cs`, lưu scene, kiểm tra nhà trong sân khô, 39 component gameplay giữ nguyên, lối đi/cầu/tiệm/sân bi và bốn ranh giới đạt; cỏ ven bờ còn 0. Ảnh ép ba LOD qua bốn camera Unity nằm trong `Tools/Scene3/TencentHouse04/Unity`. Báo cáo hình học/silhouette/shading nằm trong cùng thư mục; báo cáo cài `Tools/Scene3/VillageComposition/tencent_house04_report.txt`, bước shading cuối ghi `tencent_house04_shading_report.txt`. Backup trước đổi nhà là `before-tencent-house04.unity`. Chưa đo FPS mới hoặc thử toàn bộ chuỗi nhiệm vụ.

## Bố cục trước đây (05/10/2026, đã thay thế)

Nhóm `LangQue_BoCucMoi` trong scene 3 là bố cục đang dùng. Năm nhà được chuyển ra nền đất khô ở hai mép ngoài ruộng, lần lượt tại X/Z `(-56,-9)`, `(56,10)`, `(-56,33)`, `(56,66)`, `(-56,92)`. Mỗi sân rộng 23×20 m có một cây ổi low-poly, một bàn và hai ghế gỗ; lối đất rộng 3,2 m nối ra đường. Khuôn viên nhà cách mặt nước/ruộng gần nhất ít nhất 5 m. Nhóm nhà cũ được giữ nhưng tắt để có thể so sánh hoặc phục hồi.

Lúa cũ và các mặt nước ruộng cũ được tắt, không xoá. Tám mảng ruộng mới hiện có 310 cụm lúa nhẹ, mỗi cụm gồm 9 cây (2.790 cây tổng cộng), lấy từ mẫu lúa mới khoảng 5.986 tam giác/cây. Prefab `CayLuaNhe_Cum3x3` xoay phần mesh đúng trục để cây đứng thẳng, đưa điểm thấp nhất của rễ về mặt đất; các instance chỉ xoay ngẫu nhiên quanh trục Y nên không bị nằm ngang. Có thể cập nhật bằng menu Unity **KuTy → Scene 3 → Plant new light rice across fields**; script bỏ qua cụm đã tồn tại. `caylua.fbx` gốc và 30 cụm cũ vẫn được giữ nhưng không hiển thị. Đường đất đến các sân chừa khe khô trong lúa và mặt nước. Bãi bi nằm trên dải đất khô phía trái, thay ba ô ruộng cũ tại Z=27/46/65, với nền chơi 24×22 m, không có ruộng hay nước ngay quanh sân. Gameplay bắn bi vẫn giữ các object/component cũ.

Dựng lại bằng menu Unity **KuTy → Scene 3 → Rebuild village, rice fields and marble clearing** trên bản scene chưa có nhóm mới. Bản scene trước khi đổi bố cục được lưu tại `Tools/Scene3/Act3_MemoryWorld_OutSide.before-village-layout.unity`. Các mô tả bên dưới ghi lại bố cục trước đợt chỉnh này.

Cập nhật 27/09/2026, bản sửa bố cục đất trống và hoàng hôn. Scene: `Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity`.

## Phạm vi và căn cứ

Chỉ dựng môi trường scene 3; không sửa C# của nhóm, scene khác, nhiệm vụ, inventory hay chuyển cảnh. GDD mục 4.1–4.2 định hướng low-poly/chibi, chiều Việt Nam, quầy nhỏ/kệ kẹo/đồ chơi treo/radio và sân bi có cây ổi. Implementation guide yêu cầu giữ object, layer, component và tham chiếu gameplay gốc.

Yêu cầu mới của người dùng thay bố cục sân kín/xe hàng trước đó bằng tiệm cố định, đường làng giữa ruộng lúa nước và sân bi với bạn bè ở đoạn sau tiệm. Ruộng lúa và bạn bè là bổ sung theo yêu cầu này, không phải nội dung được khẳng định đã có trong MD gốc.

## Bố cục

Tuyến hình ảnh: **đường làng → tiệm tạp hóa → đi tiếp → sân bi ve**.

| Khu | Vị trí gần đúng | Nội dung |
| --- | --- | --- |
| Xuất phát | (0, 0.05, -5), hướng +Z | Đường đất cong nhẹ rộng 5.2m, ruộng nước hai bên |
| Tiệm bên phải | Quầy (20, 0.6, 20), nhà kéo về Z=27 | Biển “TẠP HÓA CÔ BA”, mái ngói, hiên mở, kệ hàng, kẹo/bi, diều, radio; bãi đất X=12–28, Z=14–32 |
| Sân bi đoạn sau, bên trái | Tâm (-17, 0.08, 46) | Bãi đất X=-26–-9, Z=36–57; ba bạn bè và biển sân bi, không nằm trên đường chính |
| Người dân về làng | Hai nhóm trên đường, gần Z=29 và Z=63 | Người đội nón dẫn trâu/bò bằng dây; animation cảnh nền đi qua theo hướng -Z |

Đường kéo dài từ Z=-15 tới Z=105. Có mương, bờ ruộng và cầu ván vào tiệm/sân bi. Lúa được cắt khỏi hai bãi đất và lối vào, mặt nước được chia nhỏ để không nằm dưới bãi; không chỉ che ruộng bằng một lớp nền. Ground khoảng 180×210m. Đây là một không gian mở trong một scene, chưa có streaming thế giới.

## Hierarchy và tài nguyên

Năm nhà dân mới được đặt xen kẽ hai bên đường trong nhóm `Scene3_DuongLang_RuongLua/NhaDan_XenGiuaRuongLua`. Mỗi nhà có nền đất và lối vào; năm ô lúa được chừa đúng diện tích sân nhà và lối đi. Prefab nhà, mesh lúa đã chừa và cách dựng lại nằm trong `Tools/Scene3/README.md`. Tiệm, sân bi và đường làng giữ vị trí cũ.

- `Scene3_DuongLang_RuongLua`: cảnh quan mới, chia thành nhóm kiến trúc, ruộng/cây/mương và đường/sân/bạn bè.
- `ShopCounter/TiemTapHoa_Visual`: hình ảnh tiệm làm con quầy tương tác gốc.
- `Art/Materials/Scene3`: vật liệu và font biển hiệu riêng.
- `Art/Models/Scene3`: 12 mesh lúa gộp theo ô ruộng và một mesh đường đất, tránh tạo hàng chục nghìn GameObject.
- `Art/Textures/Scene3`: texture đất riêng, có UV lặp trên đường.
- `Art/Animations/Scene3`: clip di chuyển hai nhóm người/gia súc, chạy vòng 40 giây bằng Animation component riêng.
- `Art/Materials/Scene3/Scene3_Sunset.shader` và `.mat`: skybox hoàng hôn riêng cho scene này, không thay shader/pipeline của team.

## Thay đổi presentation trong scene

Tắt bốn tường bao và mở rộng Ground; giữ object/layer gốc. Quầy ở X=20, Z=20, giữ collider và ShopInteractable. Toàn bộ cụm bi/camera/điểm bắn/điểm nhìn/trigger được dời cùng nhau tới bãi riêng, hạ mặt bi về khoảng Y=0.08; vòng LineRenderer world space đi cùng cụm. Xóa cây, nhà xa, ghế, chòi và cầu trang trí của bản trước. Ẩn phần hình ảnh kitchen/journal/key/door và khối PlayMarbles xanh không liên quan; giữ các object/component gốc để không phá tham chiếu của nhóm. Chưa thiết kế lại chuỗi nhiệm vụ nên các object legacy ẩn chưa phải luồng chơi hoàn chỉnh.

Skybox cam tím có đĩa mặt trời thấp, sương và nắng ấm được đặt riêng trong scene. Không sửa cấu hình pipeline chung.

Bảng giá trang trí giữ kẹo mút 500đ và hũ bi 1.500đ. Các item asset và luật mua không đổi.

## Giới hạn và xác minh

Đây là bản bối cảnh low-poly để duyệt, chưa phải mỹ thuật cuối. Ba bạn bè là mô hình tĩnh. Người dân và gia súc là model ghép khối có chuyển động vị trí đơn giản chạy vòng, chưa có dáng đi khớp chân, AI/pathfinding, thoại hoặc tương tác. Lúa/nước/mương là trang trí, nền đi lại vẫn phẳng. Bi giữ bàn collider, vòng và physics gốc dưới lớp nền, không đổi luật chơi. Chưa thêm âm thanh, chuyển cảnh hay post-processing mới.

Đã nhập và xem Play mode trong Unity 2022.3.62f3: trời hoàng hôn, tiệm và người/gia súc hiển thị. Xác nhận animation chạy bằng tọa độ Z của nhóm thay đổi trong Inspector. Đã kiểm tra từ trên cao cả nền tiệm và bãi bi không có lúa xuyên vào. Mesh OBJ được bù trục X vì Unity chuyển hệ tọa độ khi nhập; kiểm tra các face theo tọa độ sau nhập không có vertex trong hai bãi hoặc lối vào. Mesh có normals riêng. Lần reload cuối không có lỗi tải Transform mới trong Editor.log. Kiểm tra Git: không đổi C# hoặc scene khác; object gốc giữ đầy đủ, 76 MonoBehaviour gốc giữ nguyên serialized data. Chưa kiểm thử toàn bộ chuỗi nhiệm vụ/mua hàng/chơi bi.

UI nâu cũ có thể hiện trong Edit/Scene view; script của nhóm ẩn khi Play. Xem bối cảnh bằng Play/Game view hoặc Frame Selected vào object cảnh quan.

Branch local `nhan-dev/feature/scene3-shop`; chưa commit/push/PR.
