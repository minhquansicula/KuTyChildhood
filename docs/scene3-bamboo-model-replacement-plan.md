# Kế hoạch thay tre Scene 3 bằng model Bambusa Vulgaris

Ngày: 21/10/2026. Phạm vi: kế hoạch nhập, tách, tối ưu và thay tre bằng bộ model người dùng vừa tải. Chưa nhập asset hoặc sửa scene trong lượt lập kế hoạch này.

Cập nhật 22/10/2026: đã thay toàn làng sau khi người dùng xác nhận phạm vi: 110 bụi còn lại cộng bảy cây mẫu, tổng 117 cây/bụi dùng nguồn 1/2/4. Xem [kết quả và giới hạn](scene3-bambusa-import-implementation.md).

## 1. Quyết định triển khai

**Dùng bộ Bambusa Vulgaris làm nguồn hình học và texture, tách từng bụi thành asset riêng; thay đoạn mẫu 15 m trước, sau đó mới mở rộng toàn làng.** Giữ bố cục thưa–dày và khoảng nhìn ra ruộng/núi đã thử, không kéo nguyên bộ trưng bày chín bụi vào scene.

Model mới sẽ thay cả thân, cành và lá của tre thủ tục trong vùng được chọn. Không chỉ chồng thêm model lên cây cũ; không dùng model cây nhỏ làm LOD của cây lớn khác dáng.

## 2. Nguồn và kết quả kiểm kê

Thư mục nguồn: `D:/FPT/Semester7/PRU/as01-bambusa-vulgaris-golden-bamboo/`.

File chính: `source/primary upload/primary upload/BambusaVulgaris-Master.fbx`, khoảng 128,5 MB. Kiểm tra cấu trúc FBX cho thấy:

- **9 nhóm bụi tre độc lập**, tên `ABC_AS01_BambusaVulgaris_1.001` đến `_9.001`.
- Tổng cộng **45 geometry**, chia thành `Bark`, `Branch`, các nhóm `Leaf01–04`; có thêm node nhóm/rỗng. Các phần cùng một bụi phải được giữ đúng transform khi tách.
- **6 material nguồn**: thân, cành và bốn loại lá. Chưa thấy nhóm LOD được đặt tên riêng trong hierarchy; chưa coi bộ này có LOD sẵn.
- Có diffuse, normal, glossiness, alpha và transmission map. Thư mục nguồn có diffuse mặt sau lá; thư mục `textures/` tải kèm không chứa đầy đủ các file mặt sau tương ứng. Đối chiếu UV/material và ảnh thực trước khi chọn bộ texture dùng trong game.

Số tam giác được tính từ polygon index của FBX theo quy tắc phân tam giác n−2; cần đối chiếu lại sau khi import:

| Nhóm nguồn | Tam giác | Hướng xem xét |
| --- | ---: | --- |
| 1 | 19.758 | Ứng viên nhẹ nhất, thử trước để kiểm tra material và tỷ lệ. |
| 2 | 177.282 | Xem silhouette trước khi chọn làm biến thể. |
| 3 | 142.922 | Xem silhouette trước khi chọn làm biến thể. |
| 4 | 98.084 | Ứng viên bổ sung sau khi xem ảnh tách riêng. |
| 5 | 159.652 | Chỉ lấy nếu bổ sung dáng bụi thực sự khác. |
| 6 | 522.524 | Không dùng nguyên bản để rải lặp. |
| 7 | 683.245 | Không dùng nguyên bản để rải lặp. |
| 8 | 1.268.295 | Cụm rất nặng; ưu tiên bỏ khỏi lượt đầu. |
| 9 | 466.938 | Cần xác định vai trò và giảm hình học trước khi dùng. |
| **Tổng** | **3.538.700** | Không nhập cả bộ thành một prefab trồng lặp. |

Những con số này mô tả độ nặng hình học nguồn, chưa chứng minh mức FPS hay chất lượng tối ưu. Không chọn ba model chỉ dựa trên số tam giác.

## 3. Chuẩn bị ba biến thể

### A. Tách và chuẩn hoá

1. Giữ nguyên thư mục tải về; làm việc trên bản sao trong vùng xử lý của project. Không đưa ZIP, `__MACOSX` hoặc các file `._` vào Assets.
2. Dùng Blender hoặc công cụ tương đương đọc FBX, xuất ảnh từng nhóm với cùng ánh sáng và tỷ lệ. Ghi bounds, số tam giác, UV, material slot, transform và số phần mesh.
3. Thử nhóm 1 trước; chọn thêm hai nhóm có dáng đứng/xòe/rủ khác nhau sau khi xem riêng. Nếu nhóm 1 quá nhỏ hoặc thưa khi căn tỷ lệ làng, thay ứng viên thay vì phóng đại tới mức lá/thân sai tỷ lệ.
4. Tách theo **cả nhóm bụi**: giữ liên kết Bark–Branch–Leaf, áp dụng transform nhất quán; đưa pivot về giữa chân bụi. Loại bỏ độ dời dùng để trưng bày các mẫu cạnh nhau. Kiểm tra đơn vị và trục, không giả định FBX đã dùng mét.
5. Giữ một bản xuất trung gian để so silhouette với nguồn. Bản dùng trong game có tên ổn định và truy được số nhóm nguồn.

Đầu ra dự kiến dưới `Assets/_Project/Art/Models/Scene3/BambooImported/`, texture dưới `Art/Textures/Scene3/BambooImported/`, material dưới `Art/Materials/Scene3/BambooImported/`. Lưu nguồn nhóm, công cụ và thiết lập xuất trong tài liệu đi kèm.

### B. Material đúng trước khi giảm mesh

- Nối diffuse cho thân/cành, diffuse lá và alpha riêng; alpha clip để lá không thành những tấm chữ nhật. Kiểm tra mép lá và mipmap từ xa.
- Kiểm tra hai mặt lá bằng ánh sáng thuận/ngược. Nếu cần màu mặt trước/sau riêng, dùng shader có hỗ trợ; không tạo hai lớp mesh trùng nhau chỉ để giả hai mặt.
- Normal map nhập đúng kiểu normal. Glossiness không cắm thẳng thành metallic; chọn mức lì phù hợp phong cách scene. Transmission chỉ dùng nếu shader hỗ trợ và kết quả có lợi ở camera gameplay.
- Shader tre mẫu hiện tại dựa vào màu vertex để phân biệt thân/lá và trọng số gió. Model tải về chưa được xác nhận có cùng quy ước, nên không gán trực tiếp material đó rồi coi là hoàn tất. Tạo material/shader tương thích UV/alpha hoặc bake lại mask cần thiết trên bản dẫn xuất.
- Dùng material chung giữa các instance. Lượt đầu ưu tiên bảo toàn hình ảnh; sau đó thử gộp atlas để giảm sáu slot về khoảng hai nhóm thân/cành và lá nếu có lợi. Không hứa một draw call khi vẫn còn nhiều submesh/material.
- Khởi đầu texture tối đa 2048 cho phần cần nhìn gần; giảm bản dùng ở xa theo kích thước hiển thị. Bật mipmap, tắt Read/Write sau khi xử lý xong; không nén lại hoặc sửa texture gốc bên ngoài project.

### C. Tối ưu và LOD

- Kiểm tra phần nào tốn tam giác: cành/thân ẩn trong tán, mặt trùng, chi tiết nhỏ và lớp lá chồng nhau. Ưu tiên bỏ chi tiết không đóng góp silhouette.
- Không decimate đồng loạt toàn bụi: phải bảo toàn UV, alpha leaf cards, hướng normal và khoảng rỗng của tán. So ảnh sau mỗi mức giảm.
- Tạo ba LOD **từ cùng một biến thể**. LOD gần giữ dáng cành và lá, LOD giữa giảm nhánh nhỏ/lớp lá, LOD xa giữ khối tán. Chỉ thử impostor khi có số đo cho thấy cần.
- Ngân sách thử ban đầu khoảng **12–25 nghìn / 4–8 nghìn / 1–3 nghìn tam giác mỗi bụi**; đây là khoảng để thử, không phải tỷ lệ giảm bắt buộc hoặc cam kết chất lượng. Nếu mẫu chưa đạt ở ngân sách này, ghi rõ lựa chọn thay model hay điều chỉnh ngân sách.
- Bắt đầu không đổ bóng như hệ tre hiện tại, vẫn nhận bóng; chưa bật bóng cho toàn lũy tre. Gió bằng shader, mask nhẹ ở lá/ngọn, không dùng Animator hoặc Update cho mỗi bụi.

## 4. Thay đoạn mẫu hiện tại

Đoạn đang thử nằm ở X khoảng 4–8,5 m, Z 28–43 m, giữa hai lối cầu; có **7 bụi tre thủ tục thuộc ba prototype đã thêm ở cuối danh sách**. Mẫu này đã được ghi lại trong [báo cáo triển khai](scene3-bamboo-implementation.md).

1. Chụp baseline mới và sao lưu scene **sau các thay đổi hiện tại**. Không dùng backup cũ trước mẫu tre để phục hồi lượt thay model mới, vì có thể làm mất các chỉnh sửa sau đó.
2. Lưu `VillageGroundCoverRenderer` trước thay: prototype, material, ma trận/vị trí cây, ngưỡng LOD; đọc checkpoint ra đĩa. Ghi snapshot các script gameplay liên quan.
3. Tạo prefab preview cho từng biến thể; mesh/material dùng để trồng vẫn đi qua renderer instancing hiện tại. Không nhân cả prefab nhiều renderer thành hàng trăm GameObject.
4. Nhận diện prototype mẫu theo tên/asset và dữ liệu thực tế; không giả định chỉ số 5–7 luôn cố định. Thay mapping của **bảy cây mẫu**, giữ nguyên các Plant khác, cỏ và prototype tre cũ ngoài vùng.
5. Bắt đầu giữ vị trí/yaw của bảy cây để so sánh rõ trước/sau. Căn scale theo bounds và chân bụi, thử chiều cao khoảng 4,5–5,7 m nhưng điều chỉnh nếu model cho thấy không hợp tỷ lệ. Chỉ đổi vị trí khi cần tránh tán chồng quá dày hoặc lá chạm camera.
6. Kiểm tra bounds culling có chứa đầy đủ thân/lá và biên độ gió. Không tăng LOD distance chung của cỏ để chữa riêng tre; renderer hiện chưa có ngưỡng gần/giữa riêng cho từng prototype.
7. Xem mẫu bằng camera gameplay rồi mới quyết định mở rộng. Tre thủ tục cũ giữ asset để phục hồi, không xoá ngay.

## 5. Mốc kiểm tra và mở rộng

| Mốc | Kết quả cần đạt |
| --- | --- |
| 1. Một bụi nguồn dùng được | Tách đúng cả bụi, pivot/đơn vị đúng, thân–cành–lá đầy đủ, alpha và hai mặt lá đúng trong Unity. |
| 2. Bộ ba biến thể | Ba dáng đọc được, có LOD, có ảnh so với nguồn và bảng tam giác/material/texture sau tối ưu. |
| 3. Đoạn 15 m | Bảy bụi mới vào đúng vùng, nhìn thoáng ra ruộng/núi, không đổi gameplay/cỏ/backdrop. |
| 4. Đánh giá hiệu năng | So trước/sau cùng camera và điều kiện; kiểm tra khi di chuyển và đổi LOD, không chỉ ảnh đứng yên. |
| 5. Mở rộng làng | Chỉ sau khi đoạn mẫu đạt, thay các vùng tre còn lại theo vai trò gần–giữa–xa; vẫn giữ khoảng trống, cầu và ranh giới map. |

Không đặt thời hạn theo ngày khi chưa thử import, material và mức giảm hình học của một bụi.

## 6. Tiêu chí nghiệm thu và đo hiệu năng

- Không thiếu thân/cành/lá; không hiện ô vuông quanh lá, mặt sau đen, normal ngược hoặc texture hồng.
- Tán tự nhiên và mềm hơn tre thủ tục; hợp phong cách làng và nền chiều. Không tăng độ chân thực quá mức chỉ ở một loại cây.
- Chân cây tiếp đất, pivot đúng; lá không che mặt trẻ, camera bi hoặc lối tương tác. Đi được hai chiều trên đường làng và các lối cầu liên quan.
- Tre không nhảy vị trí khi đổi LOD; không mất tán sớm do bounds sai. Cả ba LOD vẫn nhận ra cùng một bụi.
- Cỏ, backdrop, ánh sáng, collider ranh giới và dữ liệu gameplay ngoài phạm vi giữ nguyên; checkpoint không bị xoá. Nếu chạy Play Mode test thì sao lưu/khôi phục checkpoint theo quy trình sẵn có.
- Báo cáo tam giác thấy được, số material/submesh, draw calls riêng tre/cỏ, bộ nhớ texture và CPU/GPU frame time sau warmup. Kiểm tra alpha overdraw, không kết luận chỉ từ giảm polygon.
- Mẫu tre thủ tục hiện tại có lượt vẽ cỏ/tre **11, 15, 9, 18** ở bốn camera cố định. Đây là mốc so sánh cục bộ; việc đổi nhiều material của model mới có thể tăng batch dù dùng chung texture.
- Mục tiêu chung vẫn là 60 FPS ở 1080p trên máy người dùng; chưa xác nhận cho model này. Nếu chưa thể chạy build, ghi rõ chỉ có dữ liệu Editor, không quy đổi thành cam kết FPS.

**Việc đầu tiên khi bắt đầu triển khai:** tách nhóm 1, dựng đúng material trong Unity và chụp một bụi ở ánh sáng scene. Chỉ làm thêm hai biến thể và LOD sau khi bước này đạt hình ảnh mong muốn.
