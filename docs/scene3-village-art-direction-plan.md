# Kế hoạch mỹ thuật Scene 3 — Ngõ làng trong nắng chiều

Ngày: 20/10/2026. Phạm vi: đề xuất bố cục và lộ trình triển khai, chưa thay đổi scene, gameplay hoặc save của người chơi.

## 1. Đích đến

Một đoạn ngõ quê Việt Nam thân thuộc: mái ngói đỏ nâu, tường vôi cũ, cửa gỗ xanh xám, cây che hiên, sân đất có trẻ chơi bi. Nắng chiều dẫn mắt từ sân bi tới cổng làng; ao và ruộng mở ra qua các khoảng hở giữa nhà, tạo cảm giác làng còn kéo dài ngoài vùng chơi.

Ảnh tham chiếu do người dùng cung cấp được lưu ở [references/scene3-village-target.png](references/scene3-village-target.png).

Giữ cách tạo hình mềm, màu dịu và vật liệu có dấu thời gian trong ảnh. Không cần tăng độ chân thực bằng rất nhiều chi tiết nhỏ. Cảm giác đẹp đến trước hết từ tỷ lệ, bố cục, ánh sáng và sự thống nhất của asset.

Ảnh là góc nhìn cao có hiệu ứng điện ảnh. Khi dựng game, phải kiểm tra từ camera thực tế của nhân vật: còn thấy đường, mặt bạn chơi và nơi tương tác. Chỉ dùng góc cao để chụp tổng thể; không áp dụng xóa phông mạnh cho lúc di chuyển hoặc bắn bi.

## 2. Hiện trạng cần giải quyết

Đối chiếu mã bố trí và tài liệu dự án, chưa phải đánh giá từ một lần chụp Scene 3 mới trong Editor:

- Đã có năm biến thể nhà, tiệm tạp hóa, ao, ruộng, cây, sân bi và nhiều đạo cụ. Cần tận dụng trước khi bổ sung asset.
- Tuyến đường hiện chạy từ Z khoảng -15 tới 63; bốn vùng ruộng lớn dễ khiến khu chơi có cảm giác trải rộng. Cần tạo một đoạn ngõ có nhịp kín–mở rõ hơn, tập trung chi tiết quanh đường đi.
- Nhóm hiện có 12 trẻ đứng khoác vai. Hình tham chiếu có nhóm nhỏ đang ngồi/xổm chơi, nên cần đổi cách dàn cảnh và tư thế ở sân bi để kể đúng hoạt động.
- Dự án đã tối ưu ruộng và cây bằng instancing/LOD. Phần làm đẹp phải giữ những tối ưu đó, đồng thời đo lại chi phí bóng cây, nước và hậu kỳ.

Nguồn: [tài liệu môi trường hiện tại](scene3-rural-environment.md), `Assets/_Project/Editor/Scene3VillageComposition.Reference.cs` và các phần dựng nhà, tiệm, cây, nhân vật liên quan.

## 3. Mặt bằng đề xuất

Sơ đồ là bố trí tương đối để thử trong blockout; trái/phải theo hướng nhân vật đi vào làng, không phải tọa độ thay thế trực tiếp cho scene hiện tại.

```text
                   RUỘNG XA / HÀNG DỪA / ĐƯỜNG CHÂN TRỜI
                                CỔNG LÀNG
                     Nhà nhỏ      │       Bụi chuối cuối làng
                                  │
                   Điểm ký ức ─ Ngõ cong ─ Ao + hàng rào thấp
                                  │
                     Tạp hóa ────┤       Hiên nhà / vườn
                                  │
                  Nhà mái ngói   SÂN BI ─ Khoảng đứng xem
                      + cây       │       Tường gạch / chum
                                  │
                           ĐIỂM VÀO TỪ SCENE 2
```

- **Đầu làng:** tán cây và góc mái tạo khung hình; căn nhà bên trái có hiên thấp, cửa gỗ, ghế, xe đạp và chum. Không che lối hoặc ép camera sát tường.
- **Sân bi:** là khoảng mở đầu tiên, rộng dự kiến 6–8 m; vòng bi lệch khỏi trục đường để người đi ngang không xuyên qua nhóm trẻ. Ba hoặc bốn bạn ở các tư thế ngồi/xổm khác nhau, có khoảng trống cho người chơi và camera minigame.
- **Tạp hóa:** ở gần sân bi nhưng có hiên riêng, nhìn thấy bà bán hàng từ đường vào. Đường đi phải hỗ trợ nhiệm vụ mua đồ rồi quay ra chơi bi.
- **Ngõ giữa:** rộng dự kiến 3–4 m, cong nhẹ; hai bên là tường thấp, cổng, hiên nhà và các khoảng hở. Thay đổi chiều sâu mặt tiền, tránh nhà xếp thẳng hàng giống nhau.
- **Ao:** nằm bên tuyến đi, nhìn thấy qua hàng rào thấp. Cần thử hướng nhìn trước khi quyết định giữ bên hiện tại hay chuyển vị trí; không di chuyển chỉ để khớp sơ đồ. Bờ ao rõ ràng và có va chạm phù hợp.
- **Cuối làng:** cổng làng làm điểm kết thị giác; ruộng và hàng cây ở sau, cụm chuối chính vẫn tập trung cuối làng theo bố trí đã thống nhất trước đó.
- **Nhật ký, chìa khóa và cửa nhiệm vụ:** đặt theo đúng điều kiện mở khóa hiện có, với lối tiếp cận dễ đọc và quãng quay lại ngắn. Vị trí cuối cùng chốt sau khi đi thử toàn tuyến.

Tập trung hoàn thiện đoạn 25–35 m quanh nhà–tiệm–sân bi trước. Giữ quy mô toàn tuyến khoảng 60–80 m nếu phù hợp gameplay hiện tại; không cần làm dày chi tiết toàn bản đồ. Các kích thước trên là giả thuyết thiết kế, cần xác nhận bằng CharacterController và camera thật.

## 4. Quy tắc hình ảnh

| Thành phần | Hướng dựng |
| --- | --- |
| Nhà | Tường vôi ngà, chân tường tối và loang nhẹ; mái ngói đỏ nâu có vài biến thể; cửa xanh xám/gỗ cũ. Đồng bộ tỷ lệ trước khi thêm vết bẩn. |
| Đường | Đất nén vàng nâu, mép đường mềm, ít sỏi và vệt bánh xe. Cỏ tập trung sát tường; mặt sân bi phẳng, dễ đọc và không có cỏ xuyên vòng. |
| Tường và cổng | Trụ xây, tường vôi xen gạch lộ, cổng gỗ hoặc tre. Tường thấp giữ tầm nhìn; vật liệu cũ nhưng không phủ rêu đồng đều mọi bề mặt. |
| Cây | Một cây có tán đẹp làm điểm chính gần nhà; cây nền tạo lớp. Lá có khoảng hở cho nắng, không thành khối xanh kín. Giữ LOD và vật liệu dùng chung. |
| Đạo cụ | Gom thành cụm có công năng: chum cạnh hiên, xe đạp dựa tường, chậu hoa cạnh cửa, củi cạnh bếp. Chừa khoảng nghỉ giữa các cụm. |
| Người và động vật | Nhóm trẻ tập trung vào vòng bi; bà ở quầy, trâu và người ở lớp xa nếu hợp tuyến. Chỉ thêm chuyển động nhỏ sau khi tư thế và tỷ lệ đã ổn. |
| Ao và ruộng | Nước phản chiếu trời vừa phải, bờ có đất/cỏ chuyển tiếp. Ruộng làm nền và khoảng mở; không để mọi cảnh nhìn đều bị chia thành ô chữ nhật lớn. |

Kiểm kê các món sau trước khi quyết định tạo thêm: cổng làng, tường/trụ/cổng ghép đoạn, xe đạp, chum, hàng rào tre, chậu hoa và cột điện. Ưu tiên đạo cụ thực sự xuất hiện trong những góc nhìn chính. Asset cận cảnh có thể dùng texture 2K khi cần; nền xa giảm độ phân giải theo kích thước hiển thị.

## 5. Ánh sáng và không khí

- Một mặt trời chiều chiếu xiên từ phía phải của góc nhìn chủ đạo, tạo bóng cây dài trên sân. Góc/hướng chốt bằng ba vị trí camera gameplay, tránh để mặt trẻ và bảng tiệm chìm trong bóng.
- Ánh nắng ấm, vùng râm hơi mát; vẫn giữ màu riêng của cửa, áo và lá. Tránh phủ màu cam toàn cảnh.
- Cân exposure trước, sau đó mới chỉnh color grading, bloom nhẹ và tương phản. AO chỉ bổ sung nếu pipeline hỗ trợ và kết quả đo cho phép.
- Sương xa nhẹ để tách hàng cây và đường chân trời; không phủ sương dày vào sân chơi. Nền núi chỉ thêm nếu hợp địa lý/bối cảnh truyện, không bắt buộc vì ảnh có núi.
- Tiếng gió, lá, gà và trò chuyện nhỏ tạo chiều sâu; âm thanh tiệm và sân bi có phạm vi, không cùng phát lớn khắp map.

## 6. Thứ tự thực hiện và mốc xem lại

| Giai đoạn | Công việc | Kết quả cần xem |
| --- | --- | --- |
| 1. Chốt nền | Chụp scene hiện tại từ điểm vào, sân bi, cuối làng; đo frame time; kiểm kê asset; sao lưu scene và tiến độ test ra đĩa. | Bộ ảnh trước sửa, danh sách giữ/sửa/thêm và số đo ban đầu. |
| 2. Dựng mẫu nhỏ | Hoàn thiện 12–15 m gồm một mặt tiền, đường, tường, tán cây, sân bi và nhóm trẻ; thử nắng chiều. | Đi thử ở camera nhân vật và chụp một góc đối chiếu ảnh. Chốt tỷ lệ, màu và ánh sáng tại đây. |
| 3. Mở rộng bố cục | Sắp nhà, hiên tiệm, ngõ, ao, cổng và lớp ruộng theo mẫu đã chốt; kiểm tra tuyến nhiệm vụ. | Toàn tuyến có các điểm nhìn rõ, đi lại thuận tiện và khoảng nghỉ thị giác. |
| 4. Hoàn thiện đời sống | Thêm cụm đạo cụ, vật liệu đất/tường, hoa, tư thế nhân vật, âm thanh và chuyển động nhẹ cần thiết. | Các cụm kể chuyện rõ từ vị trí người chơi; không lặp đạo cụ dày đặc. |
| 5. Kiểm tra cuối | So sánh ảnh cùng camera; kiểm tra va chạm, nhiệm vụ và minigame; đo CPU/GPU trong build sau warmup. | Báo cáo trước/sau, gameplay hoạt động và hiệu năng đạt mục tiêu đã đo. |

Không ước lượng thời gian bằng số ngày khi chưa kiểm kê phần asset thiếu và chất lượng model hiện tại. Mốc quyết định quan trọng nhất là đoạn mẫu ở giai đoạn 2; chỉ mở rộng khi đoạn đó đã đạt hướng hình ảnh mong muốn.

## 7. Điều kiện kỹ thuật khi triển khai

- Giữ các object tương tác, component, ID và tham chiếu hiện có của tiệm, sân bi, nhật ký, chìa khóa và cửa. Nếu đổi vị trí, di chuyển trọn nhóm logic và căn lại trigger/collider, không chỉ kéo mesh.
- Giữ luật, lực bắn và collider tính điểm của bi; đổi mỹ thuật sân không được làm che hướng ngắm hoặc thay đổi mặt phẳng va chạm ngoài dự kiến.
- Không gọi lại toàn bộ bộ dựng scene tự động chỉ để đổi một chi tiết. Các công cụ hiện có có thể thay nhiều nhóm và lưu scene; mỗi thay đổi cần phạm vi rõ và bản sao phục hồi.
- Tiếp tục dùng LOD/instancing của ruộng và cây, chia sẻ mesh/material cho vật lặp. Một nguồn sáng chính có bóng; đo tác động trước khi tăng khoảng bóng hoặc thêm hiệu ứng nước.
- Mục tiêu ban đầu: hướng tới 60 FPS ở 1080p trên máy người dùng sau warmup, tương đương ngân sách khung hình khoảng 16,7 ms. Đây là mục tiêu kiểm chứng, chưa phải kết quả hay cam kết. Đo cả CPU/GPU và độ ổn định khi di chuyển, không chỉ nhìn FPS lúc đứng yên.
- Test phải lưu checkpoint ra đĩa và phục hồi sau test; không xóa tiến độ thật của người chơi.

## 8. Tiêu chí hoàn thành

- Từ điểm vào, người chơi đọc được đường đi và một điểm thu hút; khi đến sân, nhóm trẻ và vòng bi là tâm điểm.
- Ba góc nhìn cố định thể hiện rõ lớp gần–giữa–xa; không còn cảm giác nhà và đạo cụ rải trên một mặt phẳng trống.
- Vật liệu nhà, cây, người và tiệm cùng phong cách; chân vật tiếp đất, không xuyên tường hoặc nổi trên nền.
- Đi được trọn tuyến ở cả hai chiều; không có collider vô hình, cành che camera hoặc NPC chắn vùng tương tác.
- Mua hàng, chơi bi, đọc nhật ký và các bước chìa khóa/cửa hoạt động đúng điều kiện nhiệm vụ, phần thưởng và lưu tiến độ hiện tại.
- Có ảnh và số đo trước/sau từ cùng điều kiện; hiệu năng ổn định và không phát sinh lỗi Console liên quan tới thay đổi.

**Ưu tiên đầu tiên:** hoàn thiện một góc nhà–ngõ–sân bi đủ đẹp để đi vào cảm nhận ngay. Đây là mẫu chuẩn để dựng phần còn lại của làng.
