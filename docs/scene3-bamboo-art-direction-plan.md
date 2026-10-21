# Kế hoạch chỉnh lũy tre Scene 3

Ngày: 21/10/2026. Phạm vi: lập kế hoạch theo ảnh người dùng gửi sau khi đổi backdrop; chưa sửa scene, mesh, material hoặc gameplay.

Cập nhật triển khai cùng ngày: đã dựng ba biến thể và lưu đoạn thử 15 m, thay 17 instance cũ bằng 7 bụi mới. Xem [kết quả, kiểm tra và giới hạn](scene3-bamboo-implementation.md). Tre ngoài đoạn thử chưa thay.

## 1. Vấn đề và hướng chọn

Đối tượng trong ảnh là lũy tre được dựng bằng mesh thủ tục. Nền trời mới có mây mềm, núi xa và màu chiều dịu; tre tiền cảnh lại có lá sắc, mảng xanh phẳng và thân lặp đều nên hai lớp hình ảnh chưa thống nhất.

**Hướng đề xuất: giữ tre như điểm nhận diện làng quê, thay hình dáng và bố trí thành từng bụi; bỏ cảm giác hàng rào tre phủ kín quanh làng.** Hoàn thiện một đoạn thử trước khi thay cả hệ thống.

Nhận xét từ ảnh:

- Lá dài, nhọn và gần giống các thanh nhựa; cành ngang tạo dáng giống dương xỉ hơn một bụi tre tự nhiên.
- Thân mảnh và thẳng, thiếu cảm giác đốt tre và độ cong theo trọng lượng tán.
- Quá nhiều bụi chồng nhau; silhouette thành một mái gai, phần chân là nhiều đường thẳng giống nhau.
- Màu lá ít biến thiên và vùng tối khá bệt. Tăng sáng hoặc đổi xanh sang vàng đơn thuần sẽ không giải quyết hình dáng.
- Mật độ tán che nhiều mây và núi vốn vừa được thêm vào backdrop.

Đối chiếu mã nguồn hiện có:

- `Scene3VillageComposition.Reference.cs`, hàm `ReferenceBambooMesh`: một mẫu gồm 7 thân, mỗi thân 6 cành; lá tạo từ các tam giác hai mặt. Ba mức mesh thay số lá và độ rộng lá, vẫn giữ cấu trúc chính tương tự.
- `PolishReferenceDetails`: material `TreLang_Xanh` dùng shader `KuTy/Village/Maxtree Matte Wind` với `_HasTexture = 0`; đây là nguồn của lá màu phẳng, không phải lỗi texture chưa tải.
- `PlantReferenceVegetation`: đoạn giữa làng và ruộng có hai hàng cách nhau khoảng 1,3 m, bước trồng khoảng 1,7 m; các bụi cao dự kiến 5,8–7,5 m. Ngoài ra còn hàng phía tây và cuối làng. Đây là thông số của bộ dựng, cần kiểm kê số instance thực tế trước khi chỉnh.
- Tre nằm ở `VillageGroundCoverRenderer.prototypes[4]`, dùng chung bộ vẽ instancing với cỏ; renderer hiện không cho cây phủ đất đổ bóng. Khoảng đổi LOD chung mặc định là 10 m và 24 m, tầm xa riêng của tre được đặt 155 m.

## 2. Mẫu tre mới cần đạt

| Phần | Hướng làm |
| --- | --- |
| Dáng bụi | Gốc tụ thành bụi, thân xòe nhẹ ra ngoài; cao thấp lệch nhau, không cùng một tầng tán. |
| Thân | Xanh ôliu ngả vàng, có đốt đọc được ở gần; thân cong nhẹ, tránh nghiêng cả bụi như một tấm phẳng. |
| Cành | Cành nhỏ phân cấp, đầu cành và chùm lá rủ nhẹ; giảm dáng cành ngang lặp theo tầng. |
| Lá | Nhỏ và thon hơn mẫu hiện tại, gom thành chùm có khoảng rỗng; lá có độ cong và thay đổi góc, không rải đều như răng lược. |
| Màu | Xanh lá dịu, xanh ôliu ở vùng tối, một ít lá già vàng nhạt; giữ ánh chiều từ scene thay vì vẽ mọi lá thành vàng cam. |
| Chất liệu | Lì, có alpha clip theo hình lá nếu dùng leaf cards; độ sáng hai mặt và hướng normal phải được kiểm tra ngược nắng. |
| Chuyển động | Thân tương đối vững, ngọn và lá rung nhẹ; không rung toàn thân như cỏ hoặc dùng Animator cho mỗi bụi. |

Tạo **3 biến thể dùng chung atlas/material**: bụi đứng thưa, bụi xòe vừa và bụi rủ lệch. Khác nhau ở silhouette, không chỉ xoay cùng một mesh. Tỷ lệ thử ban đầu: khoảng 4–6 m cho bụi gần tuyến đi; bụi cao hơn chỉ đặt ở lớp sau. Chốt bằng camera người chơi và tỷ lệ nhà trong scene.

Kiểm kê model tre có sẵn trong dự án/nguồn asset trước. Nếu không có mẫu đạt hình dáng, dựng mới một bộ tre gọn. Không tự thay bằng một asset siêu thực nhiều polygon hoặc mua asset khi chưa có quyết định riêng.

## 3. Bố trí lại theo lớp

**Gần đường và sân bi:** giảm tre gần camera, giữ khoảng trống nhìn thấy người chơi cùng nhóm trẻ. Không đặt chùm lá ở tầm mặt, vùng camera bắn bi hoặc vị trí tương tác. Nếu cần khung hình, ưu tiên cây tán rộng hiện có; chất lượng tán cây đó là hạng mục riêng, không mặc định nó đã đạt.

**Giữa làng và ruộng:** thay hai hàng đều bằng các cụm lệch nhịp. Có đoạn kín để tạo chiều sâu, có đoạn mở nhìn qua ruộng và núi. Thử giảm khoảng 40–60% số bụi ở đoạn mẫu làm giả thuyết bố cục, sau đó quyết định qua ảnh; không dùng tỷ lệ này như chỉ tiêu bắt buộc toàn map.

**Phía tây và cuối làng:** tre tạo nền thấp–cao không đều, xen khoảng nghỉ; có thể phối một ít cây lá rộng nếu asset hiện có phù hợp. Giữ cụm chuối chính ở cuối làng theo bố trí đã thống nhất. Không thêm một hàng cây khác phủ kín đúng chỗ vừa dọn tre.

Khoảng hở phải được kiểm tra để không lộ mép địa hình, vùng trống hoặc đường nối không hợp lý với backdrop. Ranh giới va chạm map và lối qua cầu giữ nguyên; tre là hình ảnh che ranh giới, không được mặc định xoá collider theo tre.

## 4. Thứ tự triển khai và điểm xem lại

| Bước | Công việc | Kết quả cần xem |
| --- | --- | --- |
| 1. Chốt hiện trạng | Lưu bản sao scene, danh sách vị trí/scale/prototype của tre, mesh/material hiện tại; chụp cùng camera ở góc người dùng gửi, sân bi và hướng ruộng. Đo số instance thấy được, draw calls và frame time trong điều kiện ghi rõ. | Có dữ liệu để phục hồi và so sánh, không lấy số liệu lúc đứng yên trong Editor làm kết luận FPS. |
| 2. Làm một bụi chuẩn | Kiểm kê asset, chọn hoặc dựng một bụi tre; thử thân có đốt, chùm lá rủ, material và normal ở phía sáng/ngược sáng. | Xem gần khoảng 2–4 m, trung cảnh khoảng 10–15 m và silhouette xa. Chốt hình dáng trước khi nhân bản. |
| 3. Dựng đoạn mẫu | Làm đoạn dài khoảng 12–15 m ở khu trong ảnh; thử ba biến thể và bố trí cụm thưa–dày. | Từ tuyến đi nhìn được khoảng trời/núi qua các khe; không còn dáng hàng tre đều và mái gai. |
| 4. Hoàn thiện LOD | Làm LOD theo silhouette đã chốt, đo chi phí leaf cards/alpha clip và kiểm tra đổi LOD khi di chuyển. | Lá không biến thành gai, tán không bật/tắt lộ rõ ở tầm chơi. |
| 5. Mở rộng có kiểm soát | Chỉ sau khi đoạn mẫu đạt, thay các vùng tre còn lại theo vai trò gần–giữa–xa; giữ khoảng mở ở cầu và tuyến nhiệm vụ. | Bố cục thống nhất với backdrop, không kéo cùng một mật độ cho mọi vùng. |
| 6. Kiểm tra cuối | Chụp trước/sau cùng camera; đi hai chiều, kiểm tra ngược nắng, camera bi và hiệu năng sau warmup. | Không lỗi Console, không ảnh hưởng gameplay; số đo và giới hạn được ghi lại. |

Không cần dựng concept toàn làng hoặc thêm nhiều loại cây trước bước 2. Điểm quyết định đầu tiên là **một bụi tre đẹp ở camera gameplay**, tiếp theo là đoạn mẫu 12–15 m.

## 5. Phạm vi kỹ thuật

- Công cụ riêng chỉ thay prototype/instance của tre. Khi đổi từ một prototype sang ba biến thể, thêm và ánh xạ biến thể có kiểm soát; giữ nguyên các prototype và `Plant` của cỏ. Không gọi lại toàn bộ bộ dựng scene hoặc `PolishReferenceDetails`, vì nó còn đổi các nhóm khác.
- Dùng chung mesh/material giữa các instance; tiếp tục instancing và culling theo tile. Không tạo GameObject, collider lá hoặc vòng `Update` cho từng thân/lá.
- Bắt đầu với một atlas lá khoảng 1024; chỉ tăng lên 2048 nếu kiểm tra gần cho thấy cần. Texture lớn không sửa được silhouette sai.
- LOD gần giữ cành chính và chùm lá đọc được; LOD giữa giảm cành nhỏ; LOD xa giữ khối tán. Chốt ngân sách tam giác và khoảng đổi LOD sau khi đo mẫu, không coi số polygon cố định là bằng chứng tối ưu.
- `VillageGroundCoverRenderer` dùng khoảng LOD chung cho cả cỏ và tre: không sửa hai ngưỡng toàn cục để chữa riêng tre. Chỉ bổ sung cấu hình riêng cho prototype nếu đoạn mẫu chứng minh cần.
- Giữ lá không đổ bóng ở lượt đầu. Nếu thiếu chiều sâu, thử bóng trên rất ít bụi chủ đạo gần người chơi rồi đo; không bật bóng cho toàn lũy tre ngay.
- Alpha clip có thể cải thiện hình lá nhưng vẫn cần đo diện tích pixel chồng nhau. Giảm mật độ, kích thước card và lớp lá thừa trước khi tăng chất lượng shader.
- Không chỉnh lại trời, exposure toàn cảnh, màu mặt trời hoặc ruộng chỉ để che lỗi chất lượng tre.
- Giữ ID/tham chiếu nhiệm vụ, collider ranh giới, mặt sân bi và camera. Nếu cần Play Mode test, sao lưu checkpoint ra đĩa và phục hồi sau test; không xoá tiến độ thật.

## 6. Tiêu chí nghiệm thu đoạn mẫu

- Nhìn một bụi là nhận ra tre với thân tụ gốc, phân đốt và chùm lá; không còn cảm giác dương xỉ phóng lớn hoặc thanh nhựa ghép.
- Lá và thân hợp màu trời chiều nhưng vẫn giữ sắc xanh; phía ngược sáng không thành mảng đen đặc.
- Có ít nhất ba silhouette khác nhau, nhịp trồng tự nhiên và khoảng nhìn ra nền xa từ các camera đã chốt.
- Không có lá xuyên mặt/camera, bụi nổi hoặc chân xuyên nền rõ; không che người, bảng tiệm và điểm tương tác.
- Đi được hai chiều qua đoạn mẫu và các lối cầu liên quan; minigame giữ tầm ngắm cũ.
- Có ảnh và số đo trước/sau cùng điều kiện. Hiệu năng 60 FPS ở 1080p là mục tiêu kiểm chứng chung của scene, chưa phải kết quả được xác nhận cho phương án này.

**Ưu tiên:** thay hình dáng một bụi và giảm mật độ đoạn trong ảnh trước. Không chỉ đổi màu material, cũng không thay toàn bộ cây xanh của Scene 3 trong cùng lượt.
