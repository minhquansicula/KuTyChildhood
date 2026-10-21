# Thay tre toàn làng bằng model Bambusa tải về

Ngày: 21/10/2026; mở rộng toàn làng ngày 22/10/2026 theo xác nhận của người dùng. Thực hiện [kế hoạch thay model](scene3-bamboo-model-replacement-plan.md).

## Cập nhật toàn làng — 22/10/2026

### Sửa chuyển LOD — 22/10/2026

Renderer tre nhập hiện giữ trạng thái LOD riêng theo camera, dùng khoảng đệm 0,75 m quanh ngưỡng 10/24 m và chuyển tiếp khoảng 0,4 giây. Khi đổi hướng giữa chừng, trọng số chuyển từ trạng thái đang có; không bắt đầu lại từ một mesh đầy đủ. Shader dùng các khoảng dithering bù nhau theo instance, đồng nhất ở pass màu/depth. Cỏ và lúa vẫn dùng cách chọn LOD cũ. Không thêm GameObject hoặc vật liệu riêng cho mỗi cây.

Pipeline giữ nguyên số cụm lá và kích thước lá giữa các LOD, giảm độ chia lưới trên cùng bề mặt UV; bỏ việc thưa nguyên cụm và bù kích thước 8–18%. Cành nhỏ vẫn được đơn giản hóa. Số tam giác mới (LOD0/1/2): nguồn 1 **19.758 / 3.965 / 1.693**, nguồn 2 **29.113 / 18.190 / 9.356**, nguồn 4 **23.129 / 13.940 / 6.841**. Các bảng bên dưới ghi lại số trước sửa. Đánh đổi là LOD xa có nhiều hình học hơn và thời gian chuyển tiếp có thể vẽ hai, hoặc tạm thời ba LOD khi đảo hướng nhanh.

Menu **7 Apply and validate smooth LOD** cập nhật mesh cùng GUID và kiểm tra khoảng đệm, đổi hướng khi đang chuyển, tổng độ phủ và kết thúc chuyển tiếp. Lượt kiểm tra render 360 bước camera 8→12→26→8 m đi qua cả hai ngưỡng theo hai chiều; ảnh và báo cáo ở `Tools/Scene3/BambooLod/`. Backup scene và chín mesh trước cập nhật nằm cùng thư mục. Vị trí/cấu hình cây, dữ liệu gameplay và checkpoint giữ nguyên. Các ảnh kiểm tra không thay thế benchmark CPU/GPU trong build.

Đã thay **110 bụi thủ tục còn lại**, giữ bảy cây mẫu: tổng **117 cây/bụi nhập** (nguồn 1: 19, nguồn 2: 45, nguồn 4: 53). Kiểm tra không còn instance hoặc MeshRenderer tre thủ tục đang hiển thị. Asset cũ giữ lại để phục hồi.

Giữ XZ, yaw và phân loại paddy từng vị trí; căn chân xuống địa hình 2,5 cm. Chiều cao mới theo dáng: nguồn 1 là 2,9–3,8 m, nguồn 2 là 4,2–5,5 m, nguồn 4 là 4,8–6,3 m. 1.413 instance cỏ và bảy cây mẫu khớp baseline; tổng cây/cỏ và khoảng cách LOD không đổi. Không thêm collider hay GameObject từng cây.

Backup ngay trước mở rộng: `Tools/Scene3/BambusaImported/WholeVillage/before.unity`, kèm `cover-before.json` và `checkpoint-before.txt`. Báo cáo `installation.txt`/`validation.txt` xác nhận 44 payload script khác, checkpoint, trời và mặt trời không đổi. Các kiểm tra CharacterController ở đường/cầu tham chiếu và đoạn mẫu đều đạt hai chiều.

Menu **5 Replace remaining village bamboo** áp dụng một lần có Undo và marker chống lặp. Menu **6 Validate and capture whole village** kiểm tra baseline, lối đi và chụp năm góc trước/sau tại `WholeVillage/Before` và `After`. Các mục dưới ghi lại giai đoạn mẫu trước khi mở rộng.

Lượt vẽ riêng cây/cỏ ở năm camera: trước **11 / 18 / 5 / 8 / 5**, sau **13 / 17 / 10 / 12 / 4**. Số instance lọt culling thay đổi theo bounds hình học mới. Đây không phải draw toàn scene hay CPU/GPU frame time; chưa đo FPS trong build. Cần đánh giá chuyển LOD liên tục khi chơi và hiệu năng toàn làng trên thiết bị đích.

Unity đã hoàn tất compile/import sau cập nhật công cụ kiểm tra; Console cuối lượt không có lỗi. Đã xem cả năm ảnh sau thay, không thấy material hồng hoặc mảng alpha hình chữ nhật. Góc tổng quan có sương dày và góc phía tây bị cây lá rộng che một phần; các góc đó được giữ cùng camera trước/sau để đối chiếu.

## Kết quả giai đoạn mẫu — 21/10/2026

Scene `Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity` dùng model tải về cho đúng **bảy cây/bụi trong vùng Z 28–43 m**:

- Nhóm nguồn **1**: một thân tre thưa, dùng làm lớp thấp/điểm chuyển giữa cụm.
- Nhóm nguồn **2**: bụi nhiều thân, tán tương đối dày.
- Nhóm nguồn **4**: bụi xòe, thân nghiêng và cành rủ.

Giữ XZ và yaw của bảy vị trí mẫu, điều chỉnh chiều cao theo dáng nguồn. Nhóm 1 khoảng 2,9–3,5 m, nhóm 2 khoảng 3,9–4,6 m, nhóm 4 khoảng 4,7–5,3 m. Chân model được căn theo vertex thực, vùi 2,5 cm theo địa hình. Bounds có khoảng dự phòng cho gió, không dùng phần bounds mở rộng để tính tỷ lệ cây.

**1.523 instance còn lại giữ nguyên**, gồm cỏ và tre ngoài vùng thử. Không chồng thêm tre mới lên các instance mẫu cũ: thay ba prototype mẫu nhận diện theo tên. Asset tre thủ tục vẫn giữ để phục hồi.

## Asset và vật liệu

- Mesh và ba prefab preview: `Assets/_Project/Art/Models/Scene3/BambooImported/`.
- Texture dẫn xuất và ba texture array: `Assets/_Project/Art/Textures/Scene3/BambooImported/`.
- Material chung: `Assets/_Project/Art/Materials/Scene3/BambusaImported.mat`.
- Shader: `Assets/_Project/Art/Materials/Scene3/VillageBambooImported.shader`.

Giữ texture diffuse mặt trước/mặt sau của lá từ thư mục nguồn đầy đủ; ghép alpha riêng vào các bản dẫn xuất. Thân/cành dùng diffuse tương ứng. Normal nguồn đọc ở không gian tuyến tính với cường độ nhẹ. Không dùng glossiness thành metallic; vật liệu hiện là matte với ánh xuyên lá đơn giản.

Sáu nhóm vật liệu nguồn được đưa vào sáu lớp của mỗi texture array Front/Back/Normal, giữ UV gốc và cho phép lặp UV trên thân. Các array có kích thước **512 × 2048 × 6**, DXT5, 12 mức mip, Read/Write tắt. Front/Back dùng sRGB; Normal dùng linear. Dữ liệu ảnh nén của cả ba array xấp xỉ 24 MiB, chưa tính overhead tài nguyên; file `.asset` dạng text lớn hơn do lưu dữ liệu nhị phân dưới dạng hex.

Mỗi mesh có **một submesh**, các biến thể dùng **một material instancing chung**. Không tạo GameObject cho từng bụi hoặc thêm script/collider lá. Renderer hiện tại vẫn thực hiện culling theo tile và chọn LOD theo khoảng cách. Prefab preview có LODGroup theo kích thước màn hình; đó không phải cách renderer cây trong scene chọn LOD.

Shader mới cần thiết bị hỗ trợ texture arrays (đã kiểm tra trên máy hiện tại). Lá có alpha clip và màu mặt sau riêng, nhận bóng nhưng không đổ bóng, giữ chính sách hiện tại của renderer.

## Chuẩn bị nguồn và LOD

Nguồn được đọc tại `D:/FPT/Semester7/PRU/as01-bambusa-vulgaris-golden-bamboo/source/primary upload/primary upload/`. File FBX/texture gốc không bị sửa, không đưa nguyên FBX 128,5 MB hoặc bộ chín mẫu trưng bày vào Assets.

Blender không có sẵn trong các vị trí đã kiểm tra. Công cụ `Tools/Scene3/import_bambusa.py` đọc cấu trúc binary FBX của bộ này, áp dụng transform, kiểm tra trục/đơn vị cm, giữ UV và normal theo polygon, rồi đưa pivot về chân bụi. Đây là recipe cho nguồn đã kiểm kê, không phải importer FBX tổng quát; các pivot hoặc kiểu rotation ngoài phạm vi được từ chối rõ ràng.

Tối ưu có hai phần:

- Thân chính được giữ. Cành phụ giảm bằng QEM; với các mối nối non-manifold hạn chế QEM, dùng vertex clustering trên phần cành phụ. Có thể mất chi tiết cành nhỏ, cần xem lại ở camera gameplay.
- Lá giảm số hàng điều khiển trên cùng bề mặt UV trước, giữ hình alpha và đường cong dọc lá. Sau đó chỉ thưa nguyên card nếu còn vượt ngân sách; không làm sập ngẫu nhiên các tam giác lá. LOD xa có bù diện tích lá nhẹ.

| Nguồn | Tam giác nguồn | LOD0 | LOD1 | LOD2 |
| --- | ---: | ---: | ---: | ---: |
| 1 | 19.758 | 19.758 | 3.965 | 1.693 |
| 2 | 177.282 | 26.173 | 11.230 | 4.750 |
| 4 | 98.084 | 23.129 | 10.448 | 3.969 |

Các LOD giữa/xa của nhóm 2 và 4 cao hơn khoảng thử ban đầu để giữ thân và độ phủ tán. Chưa coi đây là ngân sách tối ưu cuối cùng. Cả ba LOD của từng biến thể xuất phát từ cùng nguồn và cùng pivot; asset GUID được giữ khi cập nhật dữ liệu, nhận diện bản dẫn xuất bằng hash nội dung.

## Công cụ chạy lại

Python cần numpy, scipy, Pillow; lượt này thêm `fast-simplification 0.2.0` vào thư mục local bị Git bỏ qua `.unity-validation/bamboo-python`, không cài toàn máy:

```powershell
python -m pip install --target .unity-validation/bamboo-python --no-deps fast-simplification==0.2.0
python Tools/Scene3/import_bambusa.py --stage inspect --groups 1,2,4
python Tools/Scene3/import_bambusa.py --stage textures
python Tools/Scene3/import_bambusa.py --stage build --groups 1,2,4
```

Các file trung gian/báo cáo trong `Tools/Scene3/BambusaImported/` được Git bỏ qua. Mesh/material/texture thực dùng trong game được lưu trong Assets, không phụ thuộc thư mục tải nguồn khi chạy game.

Trong Unity, mở Scene 3 ở Edit Mode rồi dùng **KuTy > Scene 3 > Bamboo imported**:

1. **Prepare source 1 preview**: dựng vật liệu và prefab nguồn 1, chụp các LOD.
2. **Preview candidate variants**: chuẩn bị nguồn 1/2/4 và chụp từng LOD cùng mặt đối diện.
3. **Replace seven sample plants**: sao lưu trạng thái hiện tại, thay mẫu có kiểm tra và Undo khi lỗi. Marker chống áp dụng lặp.
4. **Capture installed sample**: kiểm tra lại trạng thái từ baseline trên đĩa, đi thử và chụp ảnh trước/sau.

## Bằng chứng và kiểm tra

- Backup mới ngay trước thay: `Tools/Scene3/BambusaImported/before-imported.unity`. Cấu hình cây/cỏ và checkpoint cùng thời điểm nằm ở `cover-before-imported.json` và `checkpoint-before-imported.txt`. Không dùng backup trước lượt tre thủ tục để phục hồi lượt này.
- Ảnh từng biến thể/LOD/mặt đối diện: `Tools/Scene3/BambusaImported/Variants/`.
- Ảnh scene cùng bốn camera: `Tools/Scene3/VillageComposition/BambooSample/BeforeImported/` và `AfterImported/`.
- Báo cáo: `Tools/Scene3/BambusaImported/installation.txt`, `validation.txt`, `build.json`.

Đã kiểm tra:

- Unity 2022.3.62f3 compile/import và shader thành công; Console cuối lượt không có lỗi. Kiểm tra asset GUID/meta/conflict/manifest đạt.
- Bảy cây tiếp đất; vị trí XZ/yaw giữ nguyên; 1.523 cây/cỏ khác khớp baseline trên đĩa; ngưỡng LOD chung giữ nguyên.
- Cả chín mesh có vertex/normal hữu hạn, bounds chứa hình học, số tam giác giảm qua LOD; mỗi mesh một submesh. Texture array nén đúng định dạng và không giữ bản CPU đọc/ghi.
- Khi áp dụng, 44 script gameplay/môi trường khác giữ nguyên dữ liệu serialize. Checkpoint, backdrop và hướng mặt trời không đổi.
- CharacterController đi được hai chiều qua đoạn đường làng tương ứng; các bài kiểm tra đường/cầu tham chiếu cũng đạt.
- Đã xem các ảnh gameplay, ảnh từng LOD và mặt đối diện. Lá ngược sáng tối hơn theo ánh sáng; không thấy ô alpha hay material hồng trong các ảnh đã xem. Chưa đánh giá chuyển LOD liên tục bằng một lượt người chơi đi thử.

## Hiệu năng và phạm vi còn lại

Lượt vẽ riêng nhóm cỏ/tre ở bốn camera **giữ nguyên 11 / 15 / 9 / 18** so với mẫu thủ tục, nhờ giữ một material và một submesh mỗi mesh. Đây không phải số draw toàn scene.

Nếu cả bảy cây đều dùng LOD0, tổng tam giác là **157.878**, so với **72.324** của bảy bụi thủ tục trước đó. Hình học và shader/alpha có chi phí lớn hơn; số draw không tăng không có nghĩa frame time giữ nguyên. Chưa đo CPU/GPU trong build sau warmup, chưa xác nhận 60 FPS. Không có thay đổi cấu hình build/CLI để chạy benchmark trong lượt này.

Các ảnh giai đoạn mẫu còn tre thủ tục ngoài vùng thử. Phạm vi này đã được mở rộng ngày 22/10/2026 như cập nhật đầu tài liệu; ảnh mới nằm trong `WholeVillage/After`.
