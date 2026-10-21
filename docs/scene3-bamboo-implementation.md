# Triển khai mẫu tre Scene 3

Ngày: 21/10/2026. Thực hiện mốc bụi mẫu và đoạn thử của [kế hoạch chỉnh lũy tre](scene3-bamboo-art-direction-plan.md). Chưa thay tre toàn làng.

## Thay đổi đã lưu

Scene `Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity` có đoạn thử dọc ranh làng–ruộng, vùng X khoảng 4–8,5 m, Z 28–43 m, nằm giữa hai lối cầu:

- Thay **17 instance tre cũ bằng 7 bụi mới**, chia ba cụm lệch nhịp, chiều cao 4,5–5,7 m. Giảm khoảng 59% số bụi trong đoạn thử để tạo khoảng nhìn ra ruộng và núi.
- Ba dáng bụi có 5/6/7 thân, độ xòe khác nhau; thân thuôn, cong nhẹ, có đốt nổi. Cành và nhánh phụ cong/rủ, lá ngắn hơn, có độ cong và thay đổi hướng.
- Chín mesh mới: ba biến thể × ba LOD. Một material instancing chung với sắc xanh ôliu, biến thiên màu lá, gân lá nhẹ và ánh xuyên lá đơn giản. Gió tác động chủ yếu lên ngọn/lá.
- Mẫu này dùng hình học lá cong, không dùng atlas hoặc alpha cards. Không tạo texture mới để che hình dáng cũ. Đây vẫn là mẫu thủ tục cần xem lại chất lượng, chưa phải asset mỹ thuật cuối cùng.

**1.523 instance còn lại giữ nguyên**, gồm toàn bộ cỏ và tre ngoài đoạn mẫu. Năm prototype cũ không bị thay; thêm ba prototype ở cuối danh sách. Giữ bộ vẽ `VillageGroundCoverRenderer`, culling theo tile, khoảng LOD chung, khoảng vẽ tre 155 m và chế độ không đổ bóng. Không thêm script runtime, GameObject cho từng bụi hoặc collider lá.

Tre cũ sát hai đầu đoạn thử vẫn có thể lọt vào ảnh, với lá lớn và sắc hơn. Đây là phần chưa được mở rộng, không phải model mới bị đổi ngược ở khoảng cách xa.

## Công cụ và phục hồi

- Công cụ: `Assets/_Project/Editor/Scene3VillageComposition.BambooSample.cs`.
- Shader: `Assets/_Project/Art/Materials/Scene3/VillageBambooSoft.shader`.
- Mesh/material: `Assets/_Project/Art/Models/Scene3/BambooSample/`.
- Menu **KuTy > Scene 3 > Bamboo > 1 Apply 15m bamboo sample**: lưu baseline, dựng mẫu, kiểm tra rồi lưu scene. Có marker chống áp dụng lặp. Lỗi trong bước thay scene sẽ Undo và dựng lại buffer cũ.
- Menu **2 Validate and capture bamboo sample**: đối chiếu dữ liệu đang lưu với baseline trên đĩa, kiểm tra mesh/LOD/tiếp đất, đi thử và chụp ảnh.

Baseline và bằng chứng nằm ở `Tools/Scene3/VillageComposition/BambooSample/` (output cục bộ, được Git bỏ qua):

- `before.unity`: bản sao trước sửa; `cover-before.json`: cấu hình renderer và toàn bộ vị trí cây/cỏ trước sửa; `checkpoint-before.txt`: checkpoint đọc ra đĩa, không xoá/ghi lại PlayerPrefs.
- `Before/01.png` đến `04.png` và `After/01.png` đến `04.png`: cùng camera gần, dọc tuyến, trung cảnh và nhìn cao.
- `report.txt`, `validation.txt`, `Before/render-stats.txt`, `After/render-stats.txt`: kết quả kiểm tra và thống kê nhóm cỏ/tre.

Dùng Undo của lượt áp dụng để phục hồi ngay trong cùng phiên nếu lịch sử còn nguyên. Khôi phục toàn scene từ `before.unity` sẽ đưa cả scene về thời điểm đó, gồm cả những sửa khác nếu có sau này; không thay `.meta`/GUID của scene gốc.

## Kiểm tra đã chạy

- Unity 2022.3.62f3 biên dịch/import thành công; Console cuối lượt không có lỗi.
- Đối chiếu lại từ baseline trên đĩa: tất cả instance ngoài đoạn mẫu giữ nguyên giá trị và thứ tự; ngưỡng LOD chung giữ nguyên.
- Cả chín mesh có toạ độ hữu hạn, số tam giác giảm qua từng LOD. Chân bảy bụi mới vùi 2,5 cm theo mặt địa hình.
- Khi áp dụng: giữ nguyên dữ liệu serialize của 44 script gameplay/môi trường khác; chỉ renderer cỏ/tre được đổi dữ liệu. Backdrop, hướng mặt trời và checkpoint không đổi.
- CharacterController cao 1,6 m, bán kính 0,24 m đi được hai chiều theo đoạn đường làng từ khoảng Z 27 tới 44. Các đường kiểm tra tham chiếu, gồm lối cầu và đường vào sân bi, cũng đạt.
- Đã xem ảnh trước/sau ở bốn camera. Tán mới nhỏ và rủ hơn, có khoảng thấy ruộng/núi. Đây chưa thay thế việc đi thử tương tác bằng người chơi và xem chuyển LOD liên tục.

## Hiệu năng và giới hạn

| Biến thể | LOD gần | LOD giữa | LOD xa |
| --- | ---: | ---: | ---: |
| Đứng thưa | 8.820 tam giác | 3.850 | 1.720 |
| Xòe vừa | 10.584 | 4.620 | 2.064 |
| Rủ lệch | 12.348 | 5.390 | 2.408 |

Các mesh mới chi tiết hơn bụi cũ; số bụi giảm không tự động chứng minh render nhanh hơn. Với cùng bốn camera, lượt vẽ **riêng nhóm cỏ/tre** trước/sau là **8→11, 9→15, 6→9, 12→18**. Thêm prototype làm chia batch; chưa sửa kiến trúc renderer để gộp ba biến thể. Đây là chi phí cần cân nhắc khi mở rộng.

Chưa đo CPU/GPU trong build sau warmup, chưa xác nhận 60 FPS hoặc chi phí gió bằng số đo riêng. Chưa kiểm tra đầy đủ các nhiệm vụ trong Play Mode; lượt này không tạo/xoá tiến độ test. Giữ nguyên backdrop và ánh sáng, không chỉnh ruộng hoặc các cây tán rộng trong lượt thay tre.

Mốc tiếp theo: xem mẫu ở khu Z 28–43 bằng camera người chơi, chốt độ rủ và độ đầy của tán; kiểm tra chuyển LOD. Sau đó mới quyết định mở rộng bố cục cụm tre sang các vùng còn lại và xử lý số batch nếu số đo cho thấy cần.
