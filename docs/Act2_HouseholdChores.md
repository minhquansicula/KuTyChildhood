# Scene 2 — Phụ mẹ làm việc nhà

## Kịch bản

Sau khi xuống giường, người chơi nghe lời mẹ: “Con dậy rồi à? Giúp mẹ lo bữa cơm nhé.” Checklist dẫn qua tám việc nhà theo thứ tự chuẩn bị nước và gạo, nấu cơm, dùng bữa, rửa rồi cất chén. Mỗi việc hoàn thành **một lần** thưởng **10 mảnh ký ức**; tổng cộng **80/80**. Thao tác lặp lại không thưởng thêm. Hủy bằng `Esc` giữ nguyên những nhiệm vụ đã xong, không làm mất mảnh ký ức.

| Bước | Nhiệm vụ | GameObject trong Scene 2 | Cách chơi và điều kiện hoàn thành | Phản hồi đang làm việc |
| --- | --- | --- | --- | --- |
| 1 | Múc nước | `HouseLayout/lunuoc/tripo_part_10` | Nhấn `E`. Kim trên thước dọc chạy lên xuống; nhấn `Space` trong vùng giữa 3 lần. Trượt hụt chỉ phải làm lại lần múc đó. | Gáo chuyển động, hạt nước và tiếng nước khi múc trúng. |
| 2 | Đong gạo | `HouseLayout/KitchenArea/CaiLu/cailugao` | Nhấn `E`; giữ chuột trái để tăng lượng gạo, thả trong vùng đánh dấu. Đong đúng 3 gáo. | Gáo chuyển động, hạt gạo rơi. |
| 3 | Vo gạo | `HouseLayout/KitchenArea/ThauTreoTuong` | Nhấn `E`; xoay chuột 3 vòng rồi nhấn chuột phải chắt nước đục. | Thau lắc nhẹ theo thao tác, hạt nước bắn. |
| 4 | Nhóm lửa | `HouseLayout/KitchenArea/FirewoodStove/NoiBepLua` | Nhấn `E`; nhấn `Space` 15 lần. | Hạt tàn lửa và ánh lửa tăng theo nhịp bấm. |
| 5 | Nấu cơm | `HouseLayout/KitchenArea/RicePot` | Nhấn `E`; giữ `F` trong 4 giây, sau đó nhấn `Space` khi kim nhiệt đi vào vùng an toàn. | Hơi nước và ánh lửa tăng dần. |
| 6 | Dọn mâm cơm | `HouseLayout/KitchenArea/DiningTable` | Nhấn `E`; bấm lần lượt 3 vị trí bát, đũa, nồi cơm trên UI mâm. Đặt đủ 3 món. | Biểu tượng món nảy nhẹ, hạt sáng khi đặt đúng. |
| 7 | Rửa chén | `HouseLayout/KitchenArea/Sink/GocRuaChen` | Nhấn `E`; giữ chuột trái 3 giây/chén, rửa đủ 3 chén. | Miếng bọt biển chà qua lại, hạt nước và tiếng chén sạch. |
| 8 | Cất chén | `HouseLayout/KitchenArea/DishCabinet/TuChen` | Nhấn `E`; bấm đúng thứ tự 3 vị trí chén, đĩa, đũa được đánh dấu trong UI tủ. | Biểu tượng chén đĩa nảy nhẹ, hạt sáng khi đặt đúng. |

## Luồng và giao diện

- Chỉ nhiệm vụ đang được đánh dấu trên checklist mới nhận thao tác. Các vật khác giữ trong scene, nhưng không cho cộng điểm sai thứ tự.
- HUD hiển thị `Mảnh ký ức: N/80`; khi hoàn thành hiện `+10 mảnh ký ức`, hiệu ứng sáng ấm và gợi nhắc một khoảnh khắc với mẹ.
- Mỗi mini-game có hướng dẫn phím, tiến độ, phản hồi trúng/trượt và `Esc` để hủy. Thất bại ở một nhịp không xóa những nhiệm vụ đã hoàn thành.
- Kết thúc bước 8: “Ngày ấy, căn bếp nhỏ luôn ấm vì có mẹ.” Sau đó chuyển tiếp sang luồng ký ức hiện có; 80 mảnh việc nhà tách biệt với ba `MemoryType` lớn của cốt truyện.

## Ghi chú triển khai

- `lunuoc`, `CaiLu` và `ThauTreoTuong` hiện chủ yếu là model; cần collider/điểm tương tác phù hợp. `NoiBepLua`, `RicePot`, `DiningTable`, `GocRuaChen`, `TuChen` đã có tương tác cũ, cần tránh nhiều script cùng nhận một lần bấm.
- Các chuyển động thao tác được tạo bằng animation trong Unity (nhún, nghiêng, lắc, rung theo đầu vào) và Particle System; không cần sửa mesh FBX gốc. Có thể thay bằng clip xuất từ Blender sau mà vẫn giữ nguyên logic nhiệm vụ.
- Hiệu ứng hạt phải dừng khi hủy mini-game; transform vật mẫu trở lại vị trí ban đầu. Không dịch chuyển collider/tường hoặc phá bố cục người dùng đang chỉnh.
- Một nhiệm vụ chỉ phát phần thưởng một lần; tiến độ 80/80 là dữ liệu của flow việc nhà, không dùng `CurrencyManager`.

## Trạng thái triển khai

- Đã gắn tám nhiệm vụ vào các điểm tương tác của Scene 2. Múc nước gắn trực tiếp trên mesh chiếc lu `lunuoc/tripo_part_10`; `ThauTreoTuong` có trigger riêng. Script tương tác cũ trùng trên các đạo cụ bếp đã được thay bằng flow mới.
- Hiệu ứng thao tác hiện là chuyển động đạo cụ đơn giản, Particle System và ánh lửa tạo trực tiếp trong Unity lúc chơi. Đây là bản giả lập hành động; chưa có clip hoạt hình xuất từ Blender hoặc animation nhân vật cầm đạo cụ.
- Checklist hiện sau khi xuống giường. Kết thúc 80/80 chuyển sang bước cốt truyện kế tiếp và ghi nhận ký ức `FilialLove` hiện có.
