# Scene 3 — Phác thảo bối cảnh hàng rong tuổi thơ (bản cũ)

**Bố cục này đã được thay bằng không gian mở theo yêu cầu mới. Xem [scene3-rural-environment.md](scene3-rural-environment.md) để biết hiện trạng. Nội dung bên dưới chỉ lưu lại phương án trước.**

Ngày thực hiện: 27/09/2026. Scene: `Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity`.

## Phạm vi đã thống nhất

Chỉ dựng hình ảnh và bố cục môi trường. Không sửa script C# của nhóm, không triển khai chuyển cảnh, không thay đổi nhiệm vụ, tiền, inventory, điều kiện mua hàng hay luật chơi bi. Không sửa scene khác hoặc material dùng chung. Đây là bản phác thảo bằng khối hình đơn giản để nhóm duyệt và thay model sau, không phải bản mỹ thuật cuối.

## Căn cứ thiết kế

| Căn cứ | Yêu cầu | Cách thể hiện trong bản phác thảo |
| --- | --- | --- |
| `game-design-document.md`, mục 4.1 | Low-poly, hoài niệm, màu vàng ấm như chiều Việt Nam | Hình khối đơn giản, tường kem/vàng đất, gỗ, mái ngói, ánh nắng ấm |
| `game-design-document.md`, mục 4.2 | Sân đất/sân gạch, cây khế hoặc ổi, chỗ chơi bi | Sân gạch, cây ổi cách điệu, ghế sân; giữ nguyên khu chơi bi gốc |
| `game-design-document.md`, mục 4.2 | Quầy nhỏ, kệ kẹo, đồ chơi treo | Xe hàng mái sọc, hũ kẹo, kẹo mút, khay bi, diều treo |
| `game-design-document.md`, ghi chú NPC | Có thể dùng quầy tĩnh và radio, không cần NPC | Radio trang trí trên xe; chưa có nhân vật bán hàng hoặc âm thanh mới |
| `implementation-guide.md`, mục Nơi gắn asset | Giữ object gốc, collider, layer và logic; thêm phần hình ảnh làm con | `HangRong_Visual` là con của `ShopCounter`; giữ collider và `ShopInteractable` gốc |
| `implementation-guide.md`, mục Luồng đã nối code | Kẹo mút 500đ và hũ bi 1.500đ | Bảng giá trang trí theo đúng hai mức giá; không thay asset vật phẩm hoặc shopItems |
| `development-plan.md`, Quy tắc làm việc nhóm | Mỗi người branch riêng, merge qua PR | Làm trên `nhan-dev/feature/scene3-shop`; chưa push hoặc tạo PR |
| Tin nhắn phân công của leader do người dùng cung cấp | Scene 3 mua đồ chơi ở hàng rong | Thể hiện khu cửa hàng dưới dạng xe hàng rong thay vì tiệm cố định |

Các MD cũ mô tả bếp, sân và cửa hàng trong một scene. Việc chia scene hiện có và tin nhắn phân công là cơ sở để đặt bản phác thảo vào scene ngoài nhà. Tài liệu này không xác nhận luồng chuyển scene đã hoàn thành.

## Bố cục và hierarchy

- `Scene3_EnvironmentSketch`: nhóm môi trường mới, có thể tắt để so sánh với greybox.
  - `NgoTuoiTho_Architecture`: nhà nhỏ, cửa gỗ, cửa sổ, mái ngói, tường và hàng rào trang trí.
  - `CayXanh_Landscape`: cây ổi cách điệu và chậu cây.
  - `SanGach_Details`: sân gạch, mép sân, khu lát trước hàng rong, ghế và bảng gỗ.
  - Các bảng chữ mới dùng font riêng của scene.
- `ShopCounter/ HangRong_Visual`: thân xe, mái bạt, bánh xe, kẹo, bi, diều và radio trang trí.
- Khu chơi bi gốc giữ vị trí và các reference vật lý. Chỉ thay material hiển thị của `MarbleTable`.
- Điểm bắt đầu của Player trong scene dịch sang X = 4, Y = 0.05, Z = -5 để nhìn rõ bố cục. Không chỉnh prefab Player dùng chung hoặc script điều khiển.

Xe hàng nằm quanh vị trí gốc của `ShopCounter` (X = 7, Z = 4); sân chơi bi ở trung tâm. Nhà và cây đặt quanh rìa, để khoảng trống giữa các khu vực. Các món trên xe và bảng giá chỉ là đồ trang trí, không phải các interactable mới.

## Asset riêng của scene

`Assets/_Project/Art/Materials/Scene3/` chứa material màu và `Scene3_Vietnamese.asset`, bản font riêng để các bảng mới không mở rộng atlas của font dùng chung. Các mesh trang trí dùng primitive có sẵn của Unity; không cài package hoặc tải asset bên ngoài.

`Ground`, tường bao và `MarbleTable` trong scene dùng material mới. Renderer greybox của `ShopCounter` và các bảng chữ nổi `Sign` cũ được tắt; object, script và collider vẫn giữ nguyên. Chưa thêm nhạc, voice, NPC, animation, Bloom, Vignette hay hệ thống nhiệm vụ.

## Kiểm chứng và giới hạn

- Scene được mở và hiển thị trong Unity 2022.3.62f3, đã xem trong Scene View và Play Mode.
- Lỗi hierarchy khi nạp bản đầu đã được xử lý bằng cách lưu và chuẩn hóa scene trong Unity; bản sau nạp lại không xuất hiện lỗi đó trong lần mở được kiểm tra.
- Đối chiếu dữ liệu: giữ toàn bộ record gốc; 76 component MonoBehaviour gốc được giữ nguyên nội dung serialized sau bước hoàn thiện. Không có thay đổi file C#.
- Chỉ scene ngoài nhà là file scene được thay đổi; asset môi trường mới đặt riêng.
- Chưa kiểm chứng đường chơi từ menu đến ending, giao dịch shop hoặc vật lý minigame: các phần đó nằm ngoài phạm vi dựng bối cảnh.
- Các hệ thống bếp, nhật ký và nhiệm vụ cũ còn trong scene; không xóa để tránh phá logic. Việc phân chia lại gameplay giữa nhà và sân cần nhóm quyết định sau.
- Không lưu thay đổi trong Play Mode. Khi xem bản phác thảo, có thể dùng Scene View để quan sát toàn bộ, hoặc Play để UI mẫu tự được các script gốc ẩn/khởi tạo.

## Hướng phát triển sau khi duyệt

Thay từng nhóm khối trang trí bằng model low-poly tương ứng, giữ nguyên parent và vị trí tương tác. Thống nhất phần nối scene và điều kiện đầu vào với leader trong một nhiệm vụ riêng. Kiểm thử gameplay và tối ưu số mesh/material khi chuyển từ phác thảo sang bản cuối.
