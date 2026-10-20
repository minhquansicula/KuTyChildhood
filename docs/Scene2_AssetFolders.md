# Hướng dẫn agent tạo và thêm asset vào Scene 2

Cập nhật theo cấu trúc project ngày 20/10/2026. Đọc toàn bộ file này trước khi tạo/import asset. Kiểm tra project thực tế trước khi thao tác vì người dùng có thể đã đổi vị trí hoặc thêm asset.

## 1. Phạm vi và đường dẫn

- Project hiện tại: `D:/ADMIN/Documents/FPT_Subject/PRU/KuTyChildhood`.
- Scene đích duy nhất: `Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity`.
- Gốc asset riêng của Scene 2: `Assets/_Project/Art/Scene2/`.
- Các đường dẫn `Assets/...` trong tài liệu đều tính từ gốc project; nếu clone ở máy khác thì dùng gốc clone tương ứng.
- Yêu cầu “thêm asset” chỉ bao gồm import, material/texture, tạo instance, đặt vị trí và lưu Scene 2. Không tự thay model cũ, đổi quest, thêm script gameplay, sửa camera/ánh sáng hoặc sắp xếp lại cả phòng.
- Không sửa Scene 1, Scene 3 hoặc di chuyển asset dùng chung. Không chuyển `Assets/_Project/Art` sang `Assets/_Project/Scenes/Art` — việc này từng gây conflict khi các nhánh cùng thêm asset.
- Không tự commit, push hoặc merge nếu người dùng chưa yêu cầu.

## 2. Cấu trúc phải dùng

```text
Assets/_Project/Art/Scene2/
├── Models/
│   ├── Architecture/
│   ├── Kitchen/
│   ├── Bedroom/
│   └── Yard/
├── Materials/
│   ├── Architecture/
│   ├── Kitchen/
│   ├── Bedroom/
│   └── Yard/
├── Textures/
│   ├── Architecture/
│   ├── Kitchen/
│   ├── Bedroom/
│   └── Yard/
├── Prefabs/
└── Shaders/
```

Model, material và texture của một asset dùng cùng nhóm. Không tạo lại các thư mục cũ `Models/Act2Home/`, `BedroomDecor/` ở gốc Art/Scene2, hoặc một thư mục Art khác cạnh file scene.

| Nhóm | Asset phù hợp | Ví dụ đang có |
| --- | --- | --- |
| Architecture | Tường, mái, cột, nền, vỏ nhà | `SM_Roof`, `SM_TimberColumn`, `SM_WallPanel_01` |
| Kitchen | Bếp, nồi, tủ chén, thau, lu gạo, tủ lạnh | `SM_RiceJar`, `SM_DishCabinet`, `SM_Refrigerator` |
| Bedroom | Giường, rương, tranh, kệ tivi, radio, giấy khen | `SM_Bed`, `SM_StorageChest`, `SM_Radio` |
| Yard | Bao thóc, cối xay, bàn tre, cây cảnh, chổi, cào, giỏ, dụng cụ | `SM_GrainPile`, `SM_GrainMill`, `SM_ToolSet`, `SM_BambooBasket` |

Phân loại theo chức năng và khu vực sử dụng trong scene hiện tại. `Yard` là tên nhóm đạo cụ, không có nghĩa mọi object phải đặt ngoài nhà. Nếu chưa rõ model là vật gì, xem model/texture trước; không đoán chỉ từ tên file viết tắt.

## 3. Đặt tên và tránh trùng asset

Dùng tên tiếng Anh có nghĩa, PascalCase, không dấu, không khoảng trắng, không dùng timestamp/hash làm tên đích khi đã biết vật thể.

| Loại | Quy tắc | Ví dụ |
| --- | --- | --- |
| Model FBX | `Models/<Group>/SM_<Name>.fbx` | `Models/Yard/SM_BambooBasket.fbx` |
| Material | `Materials/<Group>/MAT_<Name>.mat` | `Materials/Yard/MAT_BambooBasket.mat` |
| Nhiều material | `MAT_<Name>_<Part>.mat` | `MAT_Cabinet_Wood.mat` |
| Texture mới do agent tạo | `Textures/<Group>/<Name>/T_<Name>_<Channel>.png` | `Textures/Yard/BambooBasket/T_BambooBasket_BaseColor.png` |
| Prefab khi cần tái sử dụng | `Prefabs/PF_<Name>.prefab` | `Prefabs/PF_BedroomWall.prefab` |
| Shader riêng khi thực sự cần | `Shaders/SH_<Purpose>.shader` | Ưu tiên shader hiện có |
| GameObject gốc | `<Name>` | `BambooBasket` |

- Các channel thường gặp: `BaseColor`, `Normal`, `Metallic`, `Roughness`, `AO`.
- Texture trích từ FBX được giữ tên gốc trong thư mục riêng `Textures/<Group>/<Name>/`, ví dụ `texture_pbr_20250901.png`. Đây là cấu trúc đang dùng; không đổi hàng loạt texture cũ chỉ để đồng bộ tên.
- Biến thể thực sự khác mới dùng `_01`, `_02`. Trước khi thêm, tìm model và GameObject cùng tên; nếu đã tồn tại thì tái sử dụng theo yêu cầu, không tự sinh bản `Copy`, `New`, `Final2`.
- Không đổi tên node/mesh con trong FBX hoặc GameObject nhiệm vụ đang có chỉ để làm đẹp Hierarchy.

## 4. Tạo hoặc nhận asset

- Phong cách: nhà Việt Nam xưa, gỗ/tre/gốm mộc, màu phù hợp những đồ đang có. Mức độ cũ, ố và hình dáng theo yêu cầu người dùng.
- Nếu agent có khả năng tạo model: xuất FBX có UV, kèm texture cần thiết hoặc nhúng texture vào FBX. Kiểm tra hướng đứng, tỉ lệ và pivot trước khi import. Nếu không có công cụ tạo 3D, nói rõ phần còn thiếu; không coi ảnh minh họa là model hoàn chỉnh.
- Bản nháp, file tải gốc, ZIP, render thử và file Blender trung gian để ngoài `Assets` (thư mục làm việc tạm hoặc Downloads). Không nhập cả thư mục tải xuống vào Unity.
- Chỉ đưa bản model cuối cùng và các texture/material cần dùng vào các đường dẫn ở trên. Không lưu đồng thời nhiều bản FBX giống nhau.
- Chỉ giữ nguồn `.blend` trong repository khi người dùng yêu cầu hoặc có quy ước nguồn rõ ràng; không tự thêm thư mục `SourceAssets` mới cho mỗi lần import.

## 5. Quy trình import an toàn

1. Đọc `git status`, kiểm tra Scene 2 đang mở và Edit Mode. Ghi nhận thay đổi có sẵn của người dùng để không hoàn tác hoặc ghi đè.
2. Kiểm tra file nguồn tồn tại; tìm asset/GameObject trùng; xác định nhóm và lập ánh xạ nguồn → đường dẫn đích trước khi ghi.
3. Nếu Unity đang mở và scene có chỉnh sửa chưa lưu, thao tác trên scene đang mở; không nạp lại bản scene trên đĩa làm mất chỉnh sửa. Không mở Unity thứ hai trên cùng project. Nếu đang Play Mode thì chờ về Edit Mode trước khi sửa scene.
4. Dùng Unity Editor/AssetDatabase hoặc công cụ Unity của project để import. Dùng batch cho nhiều asset khi công cụ hỗ trợ. File mới để Unity sinh `.meta`; asset đã tồn tại cần di chuyển/đổi tên thì dùng AssetDatabase để giữ GUID.
5. Trích texture nhúng từ FBX vào `Textures/<Group>/<Name>/`. Tạo material bên ngoài ở `Materials/<Group>/` và remap các material slot của ModelImporter tới material đó.
6. Dùng shader phù hợp pipeline hiện tại (project đang dùng URP, thường là `Universal Render Pipeline/Lit`). Gắn BaseMap đúng ảnh màu, đặt texture normal là Normal Map. Nếu model có nhiều atlas/material thì giữ đúng từng slot; không gán một ảnh duy nhất cho mọi phần.
7. Không gắn trực tiếp Roughness vào Smoothness: hai giá trị ngược nhau. Chỉ chuyển/đóng gói map khi cần và công cụ hỗ trợ; nếu dùng giá trị hằng thay thế thì báo rõ. Giữ texture gốc để tránh mất dữ liệu.
8. Tạo instance của model trong Scene 2, đặt vào parent đúng khu vực, chỉnh tỉ lệ/hướng/vị trí dựa trên kích thước thực tế. Ghi nhận prefab overrides khi công cụ yêu cầu.
9. Kiểm tra hình ảnh trong Scene View/Game View: đủ màu, không hồng, không trắng do thiếu BaseMap, không nổi/lún sàn hoặc xuyên tường. Kiểm tra model có nhiều phần trước khi thêm collider.
10. Lưu asset và Scene 2, kiểm tra lỗi import/compile và `git diff`. Báo rõ đường dẫn asset, tên/path GameObject, vị trí tương đối và phần chưa kiểm tra được.

Không coi “import thành công” hoặc “không missing reference” là đủ: material vẫn có thể bị trống BaseMap. Project từng mất màu vì đổi tên texture trích xuất; phải kiểm tra hình ảnh và remap bền vững.

## 6. Đặt GameObject ở đâu trong Scene 2

Các parent đang dùng, cần xác minh trong scene đang mở trước khi thao tác:

| Loại | Parent / mốc tham chiếu | Cách đặt |
| --- | --- | --- |
| Đạo cụ sân/kho | `HouseLayout/YardProps` | Gần nhóm đồ cùng chức năng; dùng `GrainPile`, `GrainMill`, `BambooTable` làm mốc khi phù hợp |
| Đồ bếp | `HouseLayout/KitchenArea` | Gần bếp, chạn hoặc nơi rửa theo chức năng; để lối đi và điểm tương tác thông thoáng |
| Trang trí phòng ngủ | `BedroomArea/BedroomDecor` | Gần giường/kệ/tường phù hợp; kiểm tra kích thước thực tế |
| Kết cấu nhà | Nhóm kết cấu tương ứng trong scene, ví dụ `Act2_RusticWalls` | Đọc Hierarchy và bounds rồi mới chọn parent; không mặc định mọi model nhà vào một nhóm |

- Không dùng tọa độ cố định từ lần import trước: người dùng có thể đã di chuyển căn phòng. Đọc transform/bounds hiện tại của vật làm mốc.
- Đồ đặt sàn: đáy bounds chạm mặt sàn. Đồ đặt bàn/kệ: đáy chạm mặt đỡ. Tranh/đồ treo: hướng đúng mặt nhìn, không chìm trong tường.
- Không đặt model che cửa, chắn điểm xuống giường hoặc chặn raycast tới các nhiệm vụ. Không di chuyển đồ cũ để nhường chỗ khi chưa cần thiết.
- Collider theo hình dạng và yêu cầu sử dụng. Đồ rắn có thể dùng collider đơn giản vừa khít. Rèm hoặc vật được yêu cầu đi xuyên thì không thêm collider cản người chơi. Tránh BoxCollider lớn bao kín cả bộ model rỗng/nhiều vật rời.
- Asset trang trí không tự có nhiệm vụ. Không tự gắn script tương tác, đổi layer/tag hoặc sửa binding nhiệm vụ chỉ vì vừa thêm model.
- Nếu người dùng chỉ yêu cầu import file chưa cần đặt scene thì chỉ import. Nếu yêu cầu thêm vào Scene 2 thì phải tạo instance và lưu scene, không chỉ copy FBX.

## 7. Không tạo file rác hoặc gây conflict không cần thiết

- Một lần thêm đạo cụ thông thường chỉ cần: FBX + `.meta`, texture + `.meta`, material + `.meta`, metadata thư mục mới và thay đổi file Scene 2. Prefab chỉ tạo khi cần tái sử dụng hoặc có cấu hình đáng lưu riêng.
- Không tạo một C# Editor helper mới cho mỗi asset. Ưu tiên API Unity có sẵn; chỉ mở rộng utility hiện hữu nếu cần. Không chạy lại setup toàn cảnh vì có thể sinh object trùng hoặc ghi đè bố trí đã chỉnh tay.
- Không tạo `asset_skills.json`, `go_skills.json`, `scene_skills.json`, log, screenshot, báo cáo hoặc bản sao scene ở gốc repository.
- Log/kết quả kiểm tra tạm để trong `.unity-validation/` (đã ignore) hoặc thư mục tạm hệ điều hành. Nếu công cụ chụp ảnh bắt buộc lưu trong Unity thì dùng `Assets/Screenshots/` (đã ignore). Kiểm tra ignore thực tế trước khi lưu file tạm.
- Không tự tạo bản sao `Scene2_New`, `Scene2_Final`, `Backup_*` trong thư mục scene chính. Không thêm tài liệu hướng dẫn trùng file này sau mỗi lần import.
- Không xóa file người dùng hoặc file chưa rõ nguồn gốc để “dọn rác”. Chỉ dọn file tạm do chính tác vụ tạo và đã xác nhận không còn cần.
- Không bỏ `.meta` của asset cần version control; không sinh lại GUID của asset cũ; không di chuyển cả thư mục dùng chung.
- Xem danh sách thay đổi cuối cùng, phân biệt thay đổi có sẵn và thay đổi của tác vụ. Không dùng reset/checkout để làm sạch thay đổi không thuộc mình.
- Bố trí đúng folder giúp giảm xung đột do di chuyển file; vẫn có thể conflict nếu hai người cùng sửa Scene 2. Không hứa “chắc chắn không conflict”.

## 8. Ví dụ: thêm giỏ tre

Nguồn: `D:/Downloads/caigiotre.fbx`.

```text
Model:      Assets/_Project/Art/Scene2/Models/Yard/SM_BambooBasket.fbx
Material:   Assets/_Project/Art/Scene2/Materials/Yard/MAT_BambooBasket.mat
Textures:   Assets/_Project/Art/Scene2/Textures/Yard/BambooBasket/
Scene:      Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity
GameObject: HouseLayout/YardProps/BambooBasket
Placement:  Cạnh các bao thóc, đáy trên sàn, không chặn lối đi.
```

Asset này đã tồn tại: đây là ví dụ ánh xạ, không phải yêu cầu tạo thêm bản sao. Với asset mới, thay `<Name>` và chọn đúng `<Group>`; giữ nguyên quy trình.

## 9. Khi đầu vào là một ảnh có nhiều asset

Ảnh là tham chiếu hình dáng, màu sắc và bố cục; không phải model 3D sẵn để import. Áp dụng quy trình dưới đây khi người dùng yêu cầu tạo các đồ vật trong ảnh và thêm vào Scene 2.

1. Xem ảnh và lập danh sách các đồ vật nhìn thấy rõ. Phân biệt đồ vật cần tạo với nền, tường, sàn, bóng đổ và đồ trang trí phụ. Nếu người dùng đã chỉ định món nào thì chỉ làm các món đó; nếu yêu cầu toàn bộ asset trong ảnh thì mặc định lấy các đạo cụ nhận diện được, không tự dựng lại cả căn phòng.
2. Trình bày một bảng ngắn ngay trong chat gồm: vật trong ảnh, tên asset dự kiến, nhóm thư mục, tạo mới/tái sử dụng và khu vực đặt. Không tạo thêm file manifest/JSON/Markdown cho mỗi ảnh. Với món bị che khuất hoặc khó nhận diện, nêu giả định; chỉ hỏi khi sự mơ hồ ảnh hưởng đáng kể kết quả, vẫn tiếp tục các món đã rõ.
3. Tìm asset tương ứng đã có trong project. Tái sử dụng model phù hợp và tạo instance khi cần bố trí thêm; không tạo FBX giống hệt. Chỉ tạo biến thể khi hình dáng/màu khác có ý nghĩa hoặc người dùng yêu cầu. Không ghi đè asset cũ để khớp ảnh mới.
4. Mặc định mỗi đồ vật độc lập là một FBX và một GameObject riêng, có thể chọn, di chuyển và chỉnh scale riêng trong Unity. Ví dụ ảnh có bàn, giỏ và bình thì xuất ba model; không gộp thành một mesh cả ảnh. Các bộ phận cấu thành một vật như chân bàn, mặt bàn, tay nắm tủ có thể nằm chung model. Chỉ gộp cả bộ khi người dùng yêu cầu hoặc đó là một vật thể liền khối.
5. Dùng công cụ tạo 3D thực sự nếu có. Nếu cần ảnh crop/tham chiếu riêng cho từng món, lưu chúng ở thư mục tạm ngoài `Assets`. Phần phía sau không thấy trong ảnh phải được dựng hợp lý và ghi nhận là suy đoán; không khẳng định tái tạo chính xác từ một ảnh. Nếu công cụ chỉ tạo ảnh 2D thì báo rõ giới hạn, không coi PNG hoặc ảnh billboard là model 3D hoàn chỉnh.
6. Giữ tương quan kích thước giữa các món, hướng đứng và phong cách thống nhất. Tạo UV và material/texture cho từng model; tránh đưa bóng đổ, nền hoặc đồ vật lân cận trong ảnh vào texture của món cần tạo. Không tự đổi ánh sáng Scene 2 để che lỗi texture.
7. Phân loại từng món riêng theo mục 2, dù chúng xuất hiện chung một ảnh. Ví dụ bàn tre/giỏ dùng nhóm `Yard`, nồi/bếp dùng `Kitchen`, tranh/radio dùng `Bedroom`. Không tạo thư mục `Image01`, `GeneratedBatch` hoặc một folder ở gốc chứa tất cả đầu ra.
8. Import và kiểm tra theo mục 5. Nếu công cụ chỉ xuất được một FBX chứa nhiều vật, kiểm tra xem có thể tách thành các model độc lập với UV/material nguyên vẹn; không tự cắt mesh tùy tiện. Nếu chưa tách được thì báo rõ giới hạn và phần còn thiếu, không tuyên bố đã có các asset độc lập.
9. Đặt từng GameObject vào parent phù hợp theo mục 6. Khi người dùng yêu cầu bố cục giống ảnh, giữ quan hệ tương đối giữa các món nhưng điều chỉnh theo kích thước phòng thực tế và lối đi. Nếu không yêu cầu sao chép bố cục, chọn chỗ theo chức năng trong Scene 2 hiện tại; không di chuyển cả phòng hoặc thay đồ cũ chỉ vì ảnh có bố cục khác.
10. Kiểm tra đủ từng món trong danh sách ban đầu. Cuối tác vụ báo món đã tạo/tái sử dụng, tên GameObject, vị trí và món chưa hoàn thành nếu có. Chỉ giữ đầu ra cuối cùng cần dùng; dọn bản nháp do tác vụ tạo, không xóa ảnh nguồn của người dùng.

Nếu nhiều instance là cùng một món giống nhau, dùng chung model/material/texture. Nếu các món khác nhau thật sự dùng chung một atlas do công cụ xuất, giữ một atlas duy nhất ở thư mục mang tên bộ có nghĩa trong `Textures/<Group>/` và tham chiếu chung; không nhân bản ảnh cho từng instance. Với bộ trải qua nhiều nhóm, chọn nhóm chính và ghi rõ nơi lưu texture dùng chung trong phần báo cáo chat.

## 10. Câu giao việc dùng lại

> Đọc `docs/Scene2_AssetFolders.md`, kiểm tra cấu trúc hiện tại rồi tạo/import asset [mô tả hoặc đường dẫn nguồn] vào Scene 2 theo hướng dẫn. Giữ texture, đặt ở [vị trí mong muốn; nếu bỏ trống thì chọn vị trí theo chức năng]. Không tạo file rác, không thay đổi asset/scene khác, chưa commit hoặc push. Khi xong hãy báo tên GameObject và đường dẫn để tôi tìm trong Unity.

Với ảnh có nhiều đồ vật:

> Đọc `docs/Scene2_AssetFolders.md`, đặc biệt mục 9. Tạo các đạo cụ trong ảnh đính kèm thành từng asset 3D riêng, có texture, rồi thêm vào Scene 2 đúng cấu trúc. Kiểm tra để tái sử dụng asset phù hợp đã có. Đặt theo chức năng trong phòng hiện tại, không tạo lại nền/cả căn phòng, không tạo file rác. Báo danh sách GameObject và vị trí khi xong; chưa commit hoặc push.

Đọc tài liệu này không tự cho phép chỉnh sửa mọi nội dung được nhắc tới; chỉ thực hiện phạm vi người dùng giao trong tác vụ hiện tại.
